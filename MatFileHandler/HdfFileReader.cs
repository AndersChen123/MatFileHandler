// Copyright 2017-2018 Alexander Luzgarev

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;
using MatFileHandler.Hdf;

namespace MatFileHandler
{
    /// <summary>
    /// Reader of HDF files containing MATLAB data.
    /// </summary>
    internal class HdfFileReader
    {
        private const string ClassAttributeName = "MATLAB_class";

        private const string GlobalAttributeName = "MATLAB_global";

        private const string SparseAttributeName = "MATLAB_sparse";

        private readonly long fileId;

        private List<IVariable> variables;

        /// <summary>
        /// Initializes a new instance of the <see cref="HdfFileReader"/> class.
        /// </summary>
        /// <param name="fileId">File id to read data from.</param>
        internal HdfFileReader(long fileId)
        {
            this.fileId = fileId;
        }

        /// <summary>
        /// Read MATLAB data from the HDF file.
        /// </summary>
        /// <returns>MATLAB data file contents.</returns>
        internal IMatFile Read()
        {
            variables = new List<IVariable>();
            var group_info = default(H5G.info_t);
            H5G.get_info(fileId, ref group_info);
            var numberOfVariables = group_info.nlinks;
            ulong idx = 0;
            while (idx < numberOfVariables)
            {
                H5L.iterate(
                    fileId,
                    H5.index_t.NAME,
                    H5.iter_order_t.NATIVE,
                    ref idx,
                    VariableIterator,
                    IntPtr.Zero);
            }

            return new MatFile(variables);
        }

        private static MatlabClass ArrayTypeFromMatlabClassName(string matlabClassName)
        {
            switch (matlabClassName)
            {
                case "canonical empty":
                    return MatlabClass.MEmpty;
                case "logical":
                    return MatlabClass.MLogical;
                case "char":
                    return MatlabClass.MChar;
                case "int8":
                    return MatlabClass.MInt8;
                case "uint8":
                    return MatlabClass.MUInt8;
                case "int16":
                    return MatlabClass.MInt16;
                case "uint16":
                    return MatlabClass.MUInt16;
                case "int32":
                    return MatlabClass.MInt32;
                case "uint32":
                    return MatlabClass.MUInt32;
                case "int64":
                    return MatlabClass.MInt64;
                case "uint64":
                    return MatlabClass.MUInt64;
                case "single":
                    return MatlabClass.MSingle;
                case "double":
                    return MatlabClass.MDouble;
                case "cell":
                    return MatlabClass.MCell;
            }

            throw new NotImplementedException();
        }

        private static T[] ConvertDataToProperType<T>(byte[] bytes, MatlabClass arrayType)
            where T : struct
        {
            var length = bytes.Length;
            var arrayElementSize = SizeOfArrayElement(arrayType);
            var data = new T[length / arrayElementSize];
            Buffer.BlockCopy(bytes, 0, data, 0, length);
            return data;
        }

        private static int[] GetDimensionsOfDataset(Dataset dataset)
        {
            return dataset.GetHdfSpace().GetDimensions();
        }

        private static string GetMatlabClassFromAttribute(Hdf.Attribute attribute)
        {
            var type = attribute.GetHdfType();
            var cl = type.GetClass();
            if (cl != Class.String)
            {
                throw new NotImplementedException();
            }

            var typeIdSize = type.GetSize();
            var copiedType = Hdf.Type.CS1.WithSize(type.GetSize());
            var matlabClassNameBytes = new byte[typeIdSize];
            using (var buf = new MemoryHandle(typeIdSize))
            {
                attribute.ReadToHandle(buf, copiedType);
                Marshal.Copy(buf.Handle, matlabClassNameBytes, 0, typeIdSize);
            }

            var length = typeIdSize;
            for (var i = 0; i < typeIdSize; i++)
            {
                if (matlabClassNameBytes[i] == 0)
                {
                    length = i;
                    break;
                }
            }

            return Encoding.ASCII.GetString(matlabClassNameBytes, 0, length);
        }

        private static string GetMatlabClassOfDataset(Dataset dataset)
        {
            using (var attribute = dataset.GetAttribute(ClassAttributeName))
            {
                return GetMatlabClassFromAttribute(attribute);
            }
        }

        private static string GetMatlabClassOfGroup(Group group)
        {
            using (var attribute = group.GetAttribute(ClassAttributeName))
            {
                return GetMatlabClassFromAttribute(attribute);
            }
        }

        private static H5O.type_t GetObjectType(long groupId, string fieldName)
        {
            var objectInfo = default(H5O.info_t);
            H5O.get_info_by_name(groupId, fieldName, ref objectInfo);
            return objectInfo.type;
        }

        private static Hdf.Type H5tTypeFromHdfMatlabClass(MatlabClass arrayType)
        {
            switch (arrayType)
            {
                case MatlabClass.MInt8:
                    return Hdf.Type.NativeInt8;
                case MatlabClass.MUInt8:
                case MatlabClass.MLogical:
                    return Hdf.Type.NativeUInt8;
                case MatlabClass.MInt16:
                    return Hdf.Type.NativeInt16;
                case MatlabClass.MUInt16:
                    return Hdf.Type.NativeUInt16;
                case MatlabClass.MInt32:
                    return Hdf.Type.NativeInt32;
                case MatlabClass.MUInt32:
                    return Hdf.Type.NativeUInt32;
                case MatlabClass.MInt64:
                    return Hdf.Type.NativeInt64;
                case MatlabClass.MUInt64:
                    return Hdf.Type.NativeUInt64;
                case MatlabClass.MSingle:
                    return Hdf.Type.NativeFloat;
                case MatlabClass.MDouble:
                    return Hdf.Type.NativeDouble;
            }

            throw new NotImplementedException();
        }

        private static IArray ReadCellArray(Dataset dataset, int[] dims)
        {
            var numberOfElements = dims.NumberOfElements();
            var elements = new IArray[numberOfElements];
            using (var array = new ReferenceArray(dataset, numberOfElements))
            {
                var i = 0;
                foreach (var reference in array)
                {
                    elements[i++] = ReadDataset(reference);
                }
            }

            return new MatCellArray(dims, elements);
        }

        private static IArray ReadCharArray(Dataset dataset, int[] dims)
        {
            var storageSize = dataset.GetStorageSize();
            var data = ReadDataset(dataset, Hdf.Type.NativeUInt16, storageSize);
            var uInt16Data = new ushort[data.Length / sizeof(ushort)];
            Buffer.BlockCopy(data, 0, uInt16Data, 0, data.Length);
            var str = Encoding.Unicode.GetString(data);
            return new MatCharArrayOf<ushort>(dims, uInt16Data, str);
        }

        private static (T[] real, T[] imaginary) ReadComplexData<T>(
            Dataset dataset,
            int dataSize,
            MatlabClass arrayType)
            where T : struct
        {
            var h5Type = H5tTypeFromHdfMatlabClass(arrayType);
            var h5Size = h5Type.GetSize();
            var h5tComplexReal = Hdf.Type.CreateCompound(h5Size);
            h5tComplexReal.InsertField("real", h5Type);
            var realData = ReadDataset(dataset, h5tComplexReal, dataSize);
            var h5tComplexImaginary = Hdf.Type.CreateCompound(h5Size);
            h5tComplexImaginary.InsertField("imag", h5Type);
            var imaginaryData = ReadDataset(dataset, h5tComplexImaginary, dataSize);
            var convertedRealData = ConvertDataToProperType<T>(realData, arrayType);
            var convertedImaginaryData = ConvertDataToProperType<T>(imaginaryData, arrayType);
            return (convertedRealData, convertedImaginaryData);
        }

        private static IArray ReadDataset(Dataset dataset)
        {
            var dims = GetDimensionsOfDataset(dataset);

            var matlabClass = GetMatlabClassOfDataset(dataset);
            var arrayType = ArrayTypeFromMatlabClassName(matlabClass);

            switch (arrayType)
            {
                case MatlabClass.MEmpty:
                    return MatArray.Empty();
                case MatlabClass.MLogical:
                    return ReadNumericalArray<bool>(dataset, dims, arrayType);
                case MatlabClass.MChar:
                    return ReadCharArray(dataset, dims);
                case MatlabClass.MInt8:
                    return ReadNumericalArray<sbyte>(dataset, dims, arrayType);
                case MatlabClass.MUInt8:
                    return ReadNumericalArray<byte>(dataset, dims, arrayType);
                case MatlabClass.MInt16:
                    return ReadNumericalArray<short>(dataset, dims, arrayType);
                case MatlabClass.MUInt16:
                    return ReadNumericalArray<ushort>(dataset, dims, arrayType);
                case MatlabClass.MInt32:
                    return ReadNumericalArray<int>(dataset, dims, arrayType);
                case MatlabClass.MUInt32:
                    return ReadNumericalArray<uint>(dataset, dims, arrayType);
                case MatlabClass.MInt64:
                    return ReadNumericalArray<long>(dataset, dims, arrayType);
                case MatlabClass.MUInt64:
                    return ReadNumericalArray<ulong>(dataset, dims, arrayType);
                case MatlabClass.MSingle:
                    return ReadNumericalArray<float>(dataset, dims, arrayType);
                case MatlabClass.MDouble:
                    return ReadNumericalArray<double>(dataset, dims, arrayType);
                case MatlabClass.MCell:
                    return ReadCellArray(dataset, dims);
            }

            throw new NotImplementedException($"Unknown array type: {arrayType}.");
        }

        private static byte[] ReadDataset(Dataset dataset, Hdf.Type elementType, int dataSize)
        {
            var data = new byte[dataSize];
            using (var dataBuffer = new MemoryHandle(dataSize))
            {
                dataset.ReadToHandle(elementType, dataBuffer);
                Marshal.Copy(dataBuffer.Handle, data, 0, dataSize);
            }

            return data;
        }

        private static string[] ReadFieldNames(long groupId)
        {
            // Try to read fields from MATLAB_fields.
            using (var attr = new Hdf.Attribute(groupId, "MATLAB_fields"))
            {
                if (attr.Id == 0)
                {
                    throw new NotImplementedException();
                }

                var dimensions = attr.GetSpace().GetDimensions();
                var numberOfFields = dimensions.NumberOfElements();

                var fieldType = attr.GetHdfType();

                var fieldNamePointersSizeInBytes = numberOfFields * Marshal.SizeOf(default(H5T.hvl_t));
                var fieldNamePointers = new IntPtr[numberOfFields * 2];
                using (var fieldNamesBuf = new MemoryHandle(fieldNamePointersSizeInBytes))
                {
                    attr.ReadToHandle(fieldNamesBuf, fieldType);
                    Marshal.Copy(fieldNamesBuf.Handle, fieldNamePointers, 0, numberOfFields * 2);
                }

                var fieldNames = new string[numberOfFields];
                for (var i = 0; i < numberOfFields; i++)
                {
                    var stringLength = fieldNamePointers[i * 2];
                    var stringPointer = fieldNamePointers[(i * 2) + 1];
                    fieldNames[i] = Marshal.PtrToStringAnsi(stringPointer, (int)stringLength);
                }

                return fieldNames;
            }
        }

        private static IArray ReadGroup(Group group)
        {
            var matlabClass = GetMatlabClassOfGroup(group);
            if (matlabClass == "struct")
            {
                return ReadStruct(group.Id);
            }

            if (group.AttributeExists(SparseAttributeName))
            {
                var arrayType = ArrayTypeFromMatlabClassName(matlabClass);

                switch (arrayType)
                {
                    case MatlabClass.MEmpty:
                        return MatArray.Empty();
                    case MatlabClass.MLogical:
                        return ReadSparseArray<bool>(group.Id, arrayType);
                    case MatlabClass.MInt8:
                        return ReadSparseArray<sbyte>(group.Id, arrayType);
                    case MatlabClass.MUInt8:
                        return ReadSparseArray<byte>(group.Id, arrayType);
                    case MatlabClass.MInt16:
                        return ReadSparseArray<short>(group.Id, arrayType);
                    case MatlabClass.MUInt16:
                        return ReadSparseArray<ushort>(group.Id, arrayType);
                    case MatlabClass.MInt32:
                        return ReadSparseArray<int>(group.Id, arrayType);
                    case MatlabClass.MUInt32:
                        return ReadSparseArray<uint>(group.Id, arrayType);
                    case MatlabClass.MInt64:
                        return ReadSparseArray<long>(group.Id, arrayType);
                    case MatlabClass.MUInt64:
                        return ReadSparseArray<ulong>(group.Id, arrayType);
                    case MatlabClass.MSingle:
                        return ReadSparseArray<float>(group.Id, arrayType);
                    case MatlabClass.MDouble:
                        return ReadSparseArray<double>(group.Id, arrayType);
                    default:
                        throw new NotSupportedException();
                }
            }

            throw new NotImplementedException();
        }

        private static IEnumerable<ComplexOf<T>> CombineComplexOfData<T>(
            IEnumerable<T> realData,
            IEnumerable<T> imaginaryData)
            where T : struct
        {
            return realData.Zip(
                imaginaryData,
                (x, y) => new ComplexOf<T>(x, y));
        }

        private static IEnumerable<Complex> CombineComplexData(
            IEnumerable<double> realData,
            IEnumerable<double> imaginaryData)
        {
            return realData.Zip(
                imaginaryData,
                (x, y) => new Complex(x, y));
        }

        private static IArray ReadNumericalArray<T>(Dataset dataset, int[] dims, MatlabClass arrayType)
            where T : struct
        {
            var numberOfElements = dims.NumberOfElements();
            var dataSize = numberOfElements * SizeOfArrayElement(arrayType);
            var dataSetType = dataset.GetHdfType();
            var dataSetTypeClass = dataSetType.GetClass();
            var isCompound = dataSetTypeClass == Class.Compound;
            if (isCompound)
            {
                var (convertedRealData, convertedImaginaryData) = ReadComplexData<T>(dataset, dataSize, arrayType);
                if (arrayType == MatlabClass.MDouble)
                {
                    var complexData =
                        CombineComplexData(
                            convertedRealData as double[],
                            convertedImaginaryData as double[])
                        .ToArray();
                    return new MatNumericalArrayOf<Complex>(dims, complexData);
                }
                else
                {
                    var complexData =
                        CombineComplexOfData(
                            convertedRealData,
                            convertedImaginaryData)
                        .ToArray();
                    return new MatNumericalArrayOf<ComplexOf<T>>(dims, complexData);
                }
            }

            var data = ReadDataset(dataset, H5tTypeFromHdfMatlabClass(arrayType), dataSize);
            var convertedData = ConvertDataToProperType<T>(data, arrayType);
            return new MatNumericalArrayOf<T>(dims, convertedData);
        }

        private static IArray ReadSparseArray<T>(long groupId, MatlabClass arrayType)
            where T : struct
        {
            using (var sparseAttribute = new Hdf.Attribute(groupId, SparseAttributeName))
            {
                using (var numberOfRowsHandle = new MemoryHandle(sizeof(uint)))
                {
                    sparseAttribute.ReadToHandle(numberOfRowsHandle, Hdf.Type.NativeUInt);
                    var numberOfRows = Marshal.ReadInt32(numberOfRowsHandle.Handle);
                    int[] rowIndex;
                    int[] columnIndex;
                    using (var irData = new Dataset(groupId, "ir"))
                    {
                        var ds = GetDimensionsOfDataset(irData);
                        var numberOfIr = ds.NumberOfElements();
                        var irBytes = ReadDataset(irData, Hdf.Type.NativeInt, numberOfIr * sizeof(int));
                        rowIndex = new int[numberOfIr];
                        Buffer.BlockCopy(irBytes, 0, rowIndex, 0, irBytes.Length);
                    }

                    using (var jcData = new Dataset(groupId, "jc"))
                    {
                        var ds = GetDimensionsOfDataset(jcData);
                        var numberOfJc = ds.NumberOfElements();
                        var jcBytes = ReadDataset(jcData, Hdf.Type.NativeInt, numberOfJc * sizeof(int));
                        columnIndex = new int[numberOfJc];
                        Buffer.BlockCopy(jcBytes, 0, columnIndex, 0, jcBytes.Length);
                    }

                    using (var data = new Dataset(groupId, "data"))
                    {
                        var ds = GetDimensionsOfDataset(data);
                        var dims = new int[2];
                        dims[0] = numberOfRows;
                        dims[1] = columnIndex.Length - 1;
                        var dataSize = ds.NumberOfElements() * SizeOfArrayElement(arrayType);
                        var storageSize = data.GetStorageSize();
                        var dataSetType = data.GetHdfType();
                        var dataSetTypeClass = dataSetType.GetClass();
                        var isCompound = dataSetTypeClass == Class.Compound;
                        if (isCompound)
                        {
                            var (convertedRealData, convertedImaginaryData) =
                                ReadComplexData<T>(data, dataSize, arrayType);
                            if (arrayType == MatlabClass.MDouble)
                            {
                                var complexData =
                                    CombineComplexData(
                                        convertedRealData as double[],
                                        convertedImaginaryData as double[])
                                    .ToArray();
                                var complexDataDictionary =
                                    DataExtraction.ConvertMatlabSparseToDictionary(
                                        rowIndex,
                                        columnIndex,
                                        j => complexData[j]);
                                return new MatSparseArrayOf<Complex>(dims, complexDataDictionary);
                            }
                            else
                            {
                                var complexData =
                                    CombineComplexOfData<T>(
                                        convertedRealData,
                                        convertedImaginaryData)
                                    .ToArray();
                                var complexDataDictionary =
                                    DataExtraction.ConvertMatlabSparseToDictionary(
                                        rowIndex,
                                        columnIndex,
                                        j => complexData[j]);
                                return new MatSparseArrayOf<ComplexOf<T>>(dims, complexDataDictionary);
                            }
                        }

                        var d = ReadDataset(data, H5tTypeFromHdfMatlabClass(arrayType), dataSize);
                        var elements = ConvertDataToProperType<T>(d, arrayType);
                        var dataDictionary =
                            DataExtraction.ConvertMatlabSparseToDictionary(rowIndex, columnIndex, j => elements[j]);
                        return new MatSparseArrayOf<T>(dims, dataDictionary);
                    }
                }
            }
        }

        private static IArray ReadStruct(long groupId)
        {
            var fieldNames = ReadFieldNames(groupId);
            var firstObjectType = GetObjectType(groupId, fieldNames[0]);
            if (firstObjectType == H5O.type_t.DATASET)
            {
                using (var firstField = new Dataset(groupId, fieldNames[0]))
                {
                    var firstFieldType = firstField.GetHdfType();
                    if (firstFieldType.GetClass() == Class.Reference)
                    {
                        if (firstField.AttributeExists(ClassAttributeName))
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            var dimensions = GetDimensionsOfDataset(firstField);
                            var numberOfElements = dimensions.NumberOfElements();
                            var dictionary = new Dictionary<string, List<IArray>>();
                            foreach (var fieldName in fieldNames)
                            {
                                var fieldType = GetObjectType(groupId, fieldName);
                                dictionary[fieldName] = new List<IArray>();
                                switch (fieldType)
                                {
                                    case H5O.type_t.DATASET:
                                        using (var field = new Dataset(groupId, fieldName))
                                        {
                                            using (var array = new ReferenceArray(field, numberOfElements))
                                            {
                                                foreach (var reference in array)
                                                {
                                                    var value = ReadDataset(reference);
                                                    dictionary[fieldName].Add(value);
                                                }
                                            }
                                        }

                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }

                            return new MatStructureArray(dimensions, dictionary);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        private static int SizeOfArrayElement(MatlabClass arrayType)
        {
            switch (arrayType)
            {
                case MatlabClass.MInt8:
                case MatlabClass.MUInt8:
                case MatlabClass.MLogical:
                    return 1;
                case MatlabClass.MInt16:
                case MatlabClass.MUInt16:
                    return 2;
                case MatlabClass.MInt32:
                case MatlabClass.MUInt32:
                case MatlabClass.MSingle:
                    return 4;
                case MatlabClass.MInt64:
                case MatlabClass.MUInt64:
                case MatlabClass.MDouble:
                    return 8;
            }

            throw new NotImplementedException();
        }

        private bool ReadGlobalFlag(Group group)
        {
            if (!group.AttributeExists(GlobalAttributeName))
            {
                return false;
            }

            using (var globalAttribute = group.GetAttribute(GlobalAttributeName))
            {
                return globalAttribute.ReadBool();
            }
        }

        private bool ReadGlobalFlag(Dataset dataset)
        {
            if (!dataset.AttributeExists(GlobalAttributeName))
            {
                return false;
            }

            using (var globalAttribute = dataset.GetAttribute(GlobalAttributeName))
            {
                return globalAttribute.ReadBool();
            }
        }

        private int VariableIterator(long group, IntPtr name, ref H5L.info_t info, IntPtr op_data)
        {
            var variableName = Marshal.PtrToStringAnsi(name);
            var object_info = default(H5O.info_t);
            H5O.get_info_by_name(group, variableName, ref object_info);
            switch (object_info.type)
            {
                case H5O.type_t.DATASET:
                    using (var dataset = new Dataset(group, variableName))
                    {
                        var isGlobal = ReadGlobalFlag(dataset);
                        var value = ReadDataset(dataset);
                        variables.Add(new MatVariable(value, variableName, isGlobal));
                    }

                    break;
                case H5O.type_t.GROUP:
                    if (variableName == "#refs#")
                    {
                        return 0;
                    }

                    using (var subGroup = new Group(group, variableName))
                    {
                        var isGlobal = ReadGlobalFlag(subGroup);
                        var groupValue = ReadGroup(subGroup);
                        variables.Add(new MatVariable(groupValue, variableName, isGlobal));
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            return 0;
        }
    }
}