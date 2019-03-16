using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;
using MatFileHandler.Hdf;
using Array = MatFileHandler.Hdf.Array;
using Attribute = MatFileHandler.Hdf.Attribute;

namespace MatFileHandler
{
    internal class HdfFileReader
    {
        private long fileId;

        private List<IVariable> variables;

        internal HdfFileReader(long fileId)
        {
            this.fileId = fileId;
        }

        internal IMatFile Read()
        {
            variables = new List<IVariable>();
            H5G.info_t group_info = default(H5G.info_t);
            var result = H5G.get_info(fileId, ref group_info);
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
                        var value = ReadDataset(dataset.Id);
                        variables.Add(new MatVariable(value, variableName, false));
                    }
                    break;
                case H5O.type_t.GROUP:
                    if (variableName == "#refs#")
                    {
                        return 0;
                    }
                    using (var subGroup = new Group(group, variableName))
                    {
                        var groupValue = ReadGroup(subGroup.Id);
                        variables.Add(new MatVariable(groupValue, variableName, false));
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            return 0;
        }

        private static string GetMatlabClassOfDataset(long datasetId)
        {
            using (var attribute = new Attribute(datasetId, "MATLAB_class"))
            {
                var typeId = H5A.get_type(attribute.Id);
                var cl = H5T.get_class(typeId);
                if (cl != H5T.class_t.STRING)
                {
                    throw new NotImplementedException();
                }
                var classId = H5T.copy(H5T.C_S1);
                var typeIdSize = (int)H5T.get_size(typeId);
                H5T.set_size(classId, (IntPtr)typeIdSize);
                var matlabClassNameBytes = new byte[typeIdSize];
                using (var buf = new MemoryHandle(typeIdSize))
                {
                    H5A.read(attribute.Id, classId, buf.Handle);
                    Marshal.Copy(buf.Handle, matlabClassNameBytes, 0, typeIdSize);
                }

                return Encoding.ASCII.GetString(matlabClassNameBytes);
            }
        }

        private static int[] GetDimensionsOfDataset(long datasetId)
        {
            var spaceId = H5D.get_space(datasetId);
            var rank = H5S.get_simple_extent_ndims(spaceId);
            var dims = new ulong[rank];
            H5S.get_simple_extent_dims(spaceId, dims, null);
            System.Array.Reverse(dims);
            return dims.Select(x => (int)x).ToArray();
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

        private static int GroupFieldNamesIterator(long group, IntPtr name, ref H5L.info_t info, IntPtr data)
        {
            var nameString = Marshal.PtrToStringAnsi(name);
            H5O.info_t objectInfo = default(H5O.info_t);
            H5O.get_info_by_name(group, nameString, ref objectInfo, H5P.DEFAULT);
            return 0;
        }

        private static IArray ReadSparseArray<T>(long groupId, MatlabClass arrayType)
            where T : struct
        {
            using (var sparseAttribute = new Attribute(groupId, "MATLAB_sparse"))
            {
                using (var numberOfRowsHandle = new MemoryHandle(sizeof(uint)))
                {
                    H5A.read(sparseAttribute.Id, H5T.NATIVE_UINT, numberOfRowsHandle.Handle);
                    var numberOfRows = Marshal.ReadInt32(numberOfRowsHandle.Handle);
                    int[] rowIndex;
                    int[] columnIndex;
                    using (var irData = new Dataset(groupId, "ir"))
                    {
                        var ds = GetDimensionsOfDataset(irData.Id);
                        var numberOfIr = ds.NumberOfElements();
                        var irBytes = ReadDataset(irData.Id, H5T.NATIVE_INT, numberOfIr * sizeof(int));
                        rowIndex = new int[numberOfIr];
                        Buffer.BlockCopy(irBytes, 0, rowIndex, 0, irBytes.Length);
                    }
                    using (var jcData = new Dataset(groupId, "jc"))
                    {
                        var ds = GetDimensionsOfDataset(jcData.Id);
                        var numberOfJc = ds.NumberOfElements();
                        var jcBytes = ReadDataset(jcData.Id, H5T.NATIVE_INT, numberOfJc * sizeof(int));
                        columnIndex = new int[numberOfJc];
                        Buffer.BlockCopy(jcBytes, 0, columnIndex, 0, jcBytes.Length);
                    }

                    using (var data = new Dataset(groupId, "data"))
                    {
                        var ds = GetDimensionsOfDataset(data.Id);
                        var dims = new int[2];
                        dims[0] = numberOfRows;
                        dims[1] = columnIndex.Length - 1;
                        var dataSize = ds.NumberOfElements() * SizeOfArrayElement(arrayType);
                        var storageSize = (int)H5D.get_storage_size(data.Id);
                        var dataSetType = H5D.get_type(data.Id);
                        var dataSetTypeClass = H5T.get_class(dataSetType);
                        var isCompound = dataSetTypeClass == H5T.class_t.COMPOUND;
                        if (isCompound)
                        {
                            var (convertedRealData, convertedImaginaryData) = ReadComplexData<T>(data.Id, dataSize, arrayType);
                            if (arrayType == MatlabClass.MDouble)
                            {
                                var complexData =
                                    (convertedRealData as double[])
                                    .Zip(convertedImaginaryData as double[], (x, y) => new Complex(x, y))
                                    .ToArray();
                                var complexDataDictionary = DataExtraction.ConvertMatlabSparseToDictionary(rowIndex, columnIndex, j => complexData[j]);
                                return new SparseArrayOf<Complex>(dims, complexDataDictionary);
                            }
                            else
                            {
                                var complexData =
                                    convertedRealData
                                        .Zip(convertedImaginaryData, (x, y) => new ComplexOf<T>(x, y))
                                        .ToArray();
                                var complexDataDictionary = DataExtraction.ConvertMatlabSparseToDictionary(rowIndex, columnIndex, j => complexData[j]);
                                return new SparseArrayOf<ComplexOf<T>>(dims, complexDataDictionary);
                            }
                        }
                        if (dataSize != storageSize)
                        {
                            throw new Exception("Data size mismatch.");
                        }
                        var d = ReadDataset(data.Id, H5tTypeFromHdfMatlabClass(arrayType), dataSize);
                        var elements = ConvertDataToProperType<T>(d, arrayType);
                        var dataDictionary = DataExtraction.ConvertMatlabSparseToDictionary(rowIndex, columnIndex, j => elements[j]);
                        return new SparseArrayOf<T>(dims, dataDictionary);
                    }
                }
            }
        }

        private static IArray ReadGroup(long groupId)
        {
            var matlabClass = GetMatlabClassOfDataset(groupId);
            if (matlabClass == "struct")
            {
                return ReadStruct(groupId);
            }

            if (H5A.exists_by_name(groupId, ".", "MATLAB_sparse") != 0)
            {
                var dims = new int[0];
                var arrayType = ArrayTypeFromMatlabClassName(matlabClass);

                switch (arrayType)
                {
                    case MatlabClass.MEmpty:
                        return Array.Empty();
                    case MatlabClass.MLogical:
                        return ReadSparseArray<bool>(groupId, arrayType);
                    case MatlabClass.MInt8:
                        return ReadSparseArray<sbyte>(groupId, arrayType);
                    case MatlabClass.MUInt8:
                        return ReadSparseArray<byte>(groupId, arrayType);
                    case MatlabClass.MInt16:
                        return ReadSparseArray<short>(groupId, arrayType);
                    case MatlabClass.MUInt16:
                        return ReadSparseArray<ushort>(groupId, arrayType);
                    case MatlabClass.MInt32:
                        return ReadSparseArray<int>(groupId, arrayType);
                    case MatlabClass.MUInt32:
                        return ReadSparseArray<uint>(groupId, arrayType);
                    case MatlabClass.MInt64:
                        return ReadSparseArray<long>(groupId, arrayType);
                    case MatlabClass.MUInt64:
                        return ReadSparseArray<ulong>(groupId, arrayType);
                    case MatlabClass.MSingle:
                        return ReadSparseArray<float>(groupId, arrayType);
                    case MatlabClass.MDouble:
                        return ReadSparseArray<double>(groupId, arrayType);
                    default:
                        throw new NotSupportedException();
                }
            }

            throw new NotImplementedException();
        }

        private static string[] ReadFieldNames(long groupId)
        {
            // Try to read fields from MATLAB_fields.
            using (var attr = new Attribute(groupId, "MATLAB_fields"))
            {
                if (attr.Id == 0)
                {
                    throw new NotImplementedException();
                }
                var spaceId = H5A.get_space(attr.Id);
                var rank = H5S.get_simple_extent_ndims(spaceId);
                var dims = new ulong[rank];
                H5S.get_simple_extent_dims(spaceId, dims, null);
                System.Array.Reverse(dims);
                var dimensions = dims.Select(x => (int)x).ToArray();
                var numberOfFields = dimensions.NumberOfElements();

                var field_id = H5A.get_type(attr.Id);

                var fieldNamePointersSizeInBytes = numberOfFields * Marshal.SizeOf(default(H5T.hvl_t));
                var fieldNamePointers = new IntPtr[numberOfFields * 2];
                using (var fieldNamesBuf = new MemoryHandle(fieldNamePointersSizeInBytes))
                {
                    H5A.read(attr.Id, field_id, fieldNamesBuf.Handle);
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

        private static H5O.type_t GetObjectType(long groupId, string fieldName)
        {
            var objectInfo = default(H5O.info_t);
            H5O.get_info_by_name(groupId, fieldName, ref objectInfo);
            return objectInfo.type;
        }

        private static IArray ReadStruct(long groupId)
        {
            var fieldNames = ReadFieldNames(groupId);
            var firstObjectType = GetObjectType(groupId, fieldNames[0]);
            if (firstObjectType == H5O.type_t.DATASET)
            {
                using (var firstField = new Dataset(groupId, fieldNames[0]))
                {
                    var firstFieldTypeId = H5D.get_type(firstField.Id);
                    if (H5T.get_class(firstFieldTypeId) == H5T.class_t.REFERENCE)
                    {
                        if (H5A.exists_by_name(firstField.Id, ".", "MATLAB_class") != 0)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            var dimensions = GetDimensionsOfDataset(firstField.Id);
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
                                            using (var buf = new MemoryHandle(Marshal.SizeOf(default(IntPtr)) * numberOfElements))
                                            {
                                                H5D.read(field.Id, H5T.STD_REF_OBJ, H5S.ALL, H5S.ALL, H5P.DEFAULT, buf.Handle);
                                                for (var i = 0; i < numberOfElements; i++)
                                                {
                                                    var fieldDataSet = H5R.dereference(
                                                        field.Id,
                                                        H5P.DEFAULT,
                                                        H5R.type_t.OBJECT,
                                                        buf.Handle + (i * Marshal.SizeOf(default(IntPtr))));
                                                    var dataset = ReadDataset(fieldDataSet);
                                                    dictionary[fieldName].Add(dataset);
                                                }
                                            }
                                        }
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            return new StructureArray(dimensions, dictionary);
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

        private static IArray ReadDataset(long datasetId)
        {
            var dims = GetDimensionsOfDataset(datasetId);

            var matlabClass = GetMatlabClassOfDataset(datasetId);
            var arrayType = ArrayTypeFromMatlabClassName(matlabClass);

            switch (arrayType)
            {
                case MatlabClass.MEmpty:
                    return Array.Empty();
                case MatlabClass.MLogical:
                    return ReadNumericalArray<bool>(datasetId, dims, arrayType);
                case MatlabClass.MChar:
                    return ReadCharArray(datasetId, dims);
                case MatlabClass.MInt8:
                    return ReadNumericalArray<sbyte>(datasetId, dims, arrayType);
                case MatlabClass.MUInt8:
                    return ReadNumericalArray<byte>(datasetId, dims, arrayType);
                case MatlabClass.MInt16:
                    return ReadNumericalArray<short>(datasetId, dims, arrayType);
                case MatlabClass.MUInt16:
                    return ReadNumericalArray<ushort>(datasetId, dims, arrayType);
                case MatlabClass.MInt32:
                    return ReadNumericalArray<int>(datasetId, dims, arrayType);
                case MatlabClass.MUInt32:
                    return ReadNumericalArray<uint>(datasetId, dims, arrayType);
                case MatlabClass.MInt64:
                    return ReadNumericalArray<long>(datasetId, dims, arrayType);
                case MatlabClass.MUInt64:
                    return ReadNumericalArray<ulong>(datasetId, dims, arrayType);
                case MatlabClass.MSingle:
                    return ReadNumericalArray<float>(datasetId, dims, arrayType);
                case MatlabClass.MDouble:
                    return ReadNumericalArray<double>(datasetId, dims, arrayType);
                case MatlabClass.MCell:
                    return ReadCellArray(datasetId, dims);
            }
            throw new NotImplementedException($"Unknown array type: {arrayType}.");
        }

        private static IArray ReadCellArray(long datasetId, int[] dims)
        {
            var numberOfElements = dims.NumberOfElements();
            var elements = new IArray[numberOfElements];
            using (var buf = new MemoryHandle(Marshal.SizeOf(default(IntPtr)) * numberOfElements))
            {
                H5D.read(datasetId, H5T.STD_REF_OBJ, H5S.ALL, H5S.ALL, H5P.DEFAULT, buf.Handle);
                for (var i = 0; i < numberOfElements; i++)
                {
                    var fieldDataSet =
                        H5R.dereference(
                            datasetId,
                            H5P.DEFAULT,
                            H5R.type_t.OBJECT,
                            buf.Handle + (i * Marshal.SizeOf(default(IntPtr))));
                    var dataset = ReadDataset(fieldDataSet);
                    elements[i] = dataset;
                }
            }
            return new CellArray(dims, elements);
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

        private static long H5tTypeFromHdfMatlabClass(MatlabClass arrayType)
        {
            switch (arrayType)
            {
                case MatlabClass.MInt8:
                    return H5T.NATIVE_INT8;
                case MatlabClass.MUInt8:
                case MatlabClass.MLogical:
                    return H5T.NATIVE_UINT8;
                case MatlabClass.MInt16:
                    return H5T.NATIVE_INT16;
                case MatlabClass.MUInt16:
                    return H5T.NATIVE_UINT16;
                case MatlabClass.MInt32:
                    return H5T.NATIVE_INT32;
                case MatlabClass.MUInt32:
                    return H5T.NATIVE_UINT32;
                case MatlabClass.MInt64:
                    return H5T.NATIVE_INT64;
                case MatlabClass.MUInt64:
                    return H5T.NATIVE_UINT64;
                case MatlabClass.MSingle:
                    return H5T.NATIVE_FLOAT;
                case MatlabClass.MDouble:
                    return H5T.NATIVE_DOUBLE;
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

        private static byte[] ReadDataset(long datasetId, long elementType, int dataSize)
        {
            var data = new byte[dataSize];
            using (var dataBuffer = new MemoryHandle(dataSize))
            {
                H5D.read(datasetId, elementType, H5S.ALL, H5S.ALL, H5P.DEFAULT, dataBuffer.Handle);
                Marshal.Copy(dataBuffer.Handle, data, 0, dataSize);
            }
            return data;
        }

        private static (T[] real, T[] imaginary) ReadComplexData<T>(
            long datasetId,
            int dataSize,
            MatlabClass arrayType)
            where T : struct
        {
            var h5Type = H5tTypeFromHdfMatlabClass(arrayType);
            var h5Size = H5T.get_size(h5Type);
            var h5tComplexReal = H5T.create(H5T.class_t.COMPOUND, h5Size);
            H5T.insert(h5tComplexReal, "real", IntPtr.Zero, h5Type);
            var realData = ReadDataset(datasetId, h5tComplexReal, dataSize);
            var convertedRealData = ConvertDataToProperType<T>(realData, arrayType);
            var h5tComplexImaginary = H5T.create(H5T.class_t.COMPOUND, h5Size);
            H5T.insert(h5tComplexImaginary, "imag", IntPtr.Zero, h5Type);
            var imaginaryData = ReadDataset(datasetId, h5tComplexImaginary, dataSize);
            var convertedImaginaryData = ConvertDataToProperType<T>(imaginaryData, arrayType);
            return (convertedRealData, convertedImaginaryData);
        }

        private static IArray ReadNumericalArray<T>(long datasetId, int[] dims, MatlabClass arrayType)
            where T : struct
        {
            var numberOfElements = dims.NumberOfElements();
            var dataSize = numberOfElements * SizeOfArrayElement(arrayType);
            var storageSize = (int)H5D.get_storage_size(datasetId);
            var dataSetType = H5D.get_type(datasetId);
            var dataSetTypeClass = H5T.get_class(dataSetType);
            var isCompound = dataSetTypeClass == H5T.class_t.COMPOUND;
            if (isCompound)
            {
                var (convertedRealData, convertedImaginaryData) = ReadComplexData<T>(datasetId, dataSize, arrayType);
                if (arrayType == MatlabClass.MDouble)
                {
                    var complexData =
                        (convertedRealData as double[])
                            .Zip(convertedImaginaryData as double[], (x, y) => new Complex(x, y))
                            .ToArray();
                    return new NumericalArrayOf<Complex>(dims, complexData);
                }
                else
                {
                    var complexData =
                        convertedRealData
                            .Zip(convertedImaginaryData, (x, y) => new ComplexOf<T>(x, y))
                            .ToArray();
                    return new NumericalArrayOf<ComplexOf<T>>(dims, complexData);
                }
            }
            if (dataSize != storageSize)
            {
                throw new Exception("Data size mismatch.");
            }
            var data = ReadDataset(datasetId, H5tTypeFromHdfMatlabClass(arrayType), dataSize);
            var convertedData = ConvertDataToProperType<T>(data, arrayType);
            return new NumericalArrayOf<T>(dims, convertedData);
        }

        private static IArray ReadCharArray(long datasetId, int[] dims)
        {
            var storageSize = (int)H5D.get_storage_size(datasetId);
            var data = ReadDataset(datasetId, H5T.NATIVE_UINT16, storageSize);
            var str = Encoding.Unicode.GetString(data);
            return new CharArray(dims, str);
        }
    }
}
