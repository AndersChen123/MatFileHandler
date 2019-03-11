using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace MatFileHandler
{
    internal enum HdfMatlabClass
    {
        MEmpty,
        MChar,
        MInt8,
        MUInt8,
        MInt16,
        MUInt16,
        MInt32,
        MUInt32,
        MInt64,
        MUInt64,
        MSingle,
        MDouble,
        MCell,
    }

    internal class HdfArray : IArray
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HdfArray"/> class.
        /// </summary>
        /// <param name="dimensions">Dimensions of the array.</param>
        protected HdfArray(
            int[] dimensions)
        {
            Dimensions = dimensions;
        }

        /// <inheritdoc />
        public int[] Dimensions { get; }

        /// <inheritdoc />
        public int Count => Dimensions.NumberOfElements();

        /// <summary>
        /// Returns a new empty array.
        /// </summary>
        /// <returns>Empty array.</returns>
        public static HdfArray Empty()
        {
            return new HdfArray(Array.Empty<int>());
        }

        public virtual double[] ConvertToDoubleArray()
        {
            return null;
        }

        public virtual Complex[] ConvertToComplexArray()
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEmpty => Dimensions.Length == 0;
    }

    internal class HdfCellArray : HdfArray, ICellArray
    {
        public HdfCellArray(int[] dimensions, IEnumerable<IArray> elements)
            : base(dimensions)
        {
            Data = elements.ToArray();
        }

        /// <inheritdoc />
        public IArray[] Data { get; }

        /// <inheritdoc />
        public IArray this[params int[] indices]
        {
            get => Data[Dimensions.DimFlatten(indices)];
            set => Data[Dimensions.DimFlatten(indices)] = value;
        }
    }

    /// <summary>
    /// A numerical array.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    internal class HdfNumericalArrayOf<T> : HdfArray, IArrayOf<T>
      where T : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HdfNumericalArrayOf{T}"/> class.
        /// </summary>
        /// <param name="dimensions">Dimensions of the array.</param>
        /// <param name="name">Array name.</param>
        /// <param name="data">Array contents.</param>
        public HdfNumericalArrayOf(int[] dimensions, T[] data)
            : base(dimensions)
        {
            Data = data;
        }

        /// <inheritdoc />
        public T[] Data { get; }

        /// <inheritdoc />
        public T this[params int[] list]
        {
            get => Data[Dimensions.DimFlatten(list)];
            set => Data[Dimensions.DimFlatten(list)] = value;
        }

        /// <summary>
        /// Tries to convert the array to an array of Double values.
        /// </summary>
        /// <returns>Array of values of the array, converted to Double, or null if the conversion is not possible.</returns>
        public override double[] ConvertToDoubleArray()
        {
            return Data as double[] ?? Data.Select(x => Convert.ToDouble(x)).ToArray();
        }

        /// <summary>
        /// Tries to convert the array to an array of Complex values.
        /// </summary>
        /// <returns>Array of values of the array, converted to Complex, or null if the conversion is not possible.</returns>
        public override Complex[] ConvertToComplexArray()
        {
            if (Data is Complex[])
            {
                return Data as Complex[];
            }
            if (Data is ComplexOf<sbyte>[])
            {
                return ConvertToComplex(Data as ComplexOf<sbyte>[]);
            }
            if (Data is ComplexOf<byte>[])
            {
                return ConvertToComplex(Data as ComplexOf<byte>[]);
            }
            if (Data is ComplexOf<short>[])
            {
                return ConvertToComplex(Data as ComplexOf<short>[]);
            }
            if (Data is ComplexOf<ushort>[])
            {
                return ConvertToComplex(Data as ComplexOf<ushort>[]);
            }
            if (Data is ComplexOf<int>[])
            {
                return ConvertToComplex(Data as ComplexOf<int>[]);
            }
            if (Data is ComplexOf<uint>[])
            {
                return ConvertToComplex(Data as ComplexOf<uint>[]);
            }
            if (Data is ComplexOf<long>[])
            {
                return ConvertToComplex(Data as ComplexOf<long>[]);
            }
            if (Data is ComplexOf<ulong>[])
            {
                return ConvertToComplex(Data as ComplexOf<ulong>[]);
            }
            return ConvertToDoubleArray().Select(x => new Complex(x, 0.0)).ToArray();
        }

        private static Complex[] ConvertToComplex<TS>(IEnumerable<ComplexOf<TS>> array)
          where TS : struct
        {
            return array.Select(x => new Complex(Convert.ToDouble(x.Real), Convert.ToDouble(x.Imaginary))).ToArray();
        }
    }

    internal class HdfCharArray : HdfArray, ICharArray
    {
        public HdfCharArray(int[] dimensions, string data)
        : base(dimensions)
        {
            StringData = data;
        }

        public override double[] ConvertToDoubleArray()
        {
            return Data.Select(Convert.ToDouble).ToArray();
        }

        public override Complex[] ConvertToComplexArray()
        {
            return ConvertToDoubleArray().Select(x => new Complex(x, 0.0)).ToArray();
        }

        public char[] Data => StringData.ToCharArray();

        public char this[params int[] list]
        {
            get => StringData[Dimensions.DimFlatten(list)];
            set {
                var chars = StringData.ToCharArray();
                chars[Dimensions.DimFlatten(list)] = value;
                StringData = chars.ToString();
            }
        }

        public string String => StringData;

        private string StringData { get; set; }
    }

    internal class HdfStructureArray : HdfArray, IStructureArray
    {
        public HdfStructureArray(
            int[] dimensions,
            Dictionary<string, List<IArray>> fields)
            : base(dimensions)
        {
            Fields = fields;
        }

        /// <inheritdoc />
        public IEnumerable<string> FieldNames => Fields.Keys;

        /// <summary>
        /// Gets null: not implemented.
        /// </summary>
        public IReadOnlyDictionary<string, IArray>[] Data => null;

        /// <summary>
        /// Gets a dictionary that maps field names to lists of values.
        /// </summary>
        internal Dictionary<string, List<IArray>> Fields { get; }

        /// <inheritdoc />
        public IArray this[string field, params int[] list]
        {
            get => Fields[field][Dimensions.DimFlatten(list)];
            set => Fields[field][Dimensions.DimFlatten(list)] = value;
        }

        /// <inheritdoc />
        IReadOnlyDictionary<string, IArray> IArrayOf<IReadOnlyDictionary<string, IArray>>.this[params int[] list]
        {
            get => ExtractStructure(Dimensions.DimFlatten(list));
            set => throw new NotSupportedException(
                "Cannot set structure elements via this[params int[]] indexer. Use this[string, int[]] instead.");
        }

        private IReadOnlyDictionary<string, IArray> ExtractStructure(int i)
        {
            return new HdfStructureArrayElement(this, i);
        }

        /// <summary>
        /// Provides access to an element of a structure array by fields.
        /// </summary>
        internal class HdfStructureArrayElement : IReadOnlyDictionary<string, IArray>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="HdfStructureArrayElement"/> class.
            /// </summary>
            /// <param name="parent">Parent structure array.</param>
            /// <param name="index">Index in the structure array.</param>
            internal HdfStructureArrayElement(HdfStructureArray parent, int index)
            {
                Parent = parent;
                Index = index;
            }

            /// <summary>
            /// Gets the number of fields.
            /// </summary>
            public int Count => Parent.Fields.Count;

            /// <summary>
            /// Gets a list of all fields.
            /// </summary>
            public IEnumerable<string> Keys => Parent.Fields.Keys;

            /// <summary>
            /// Gets a list of all values.
            /// </summary>
            public IEnumerable<IArray> Values => Parent.Fields.Values.Select(array => array[Index]);

            private HdfStructureArray Parent { get; }

            private int Index { get; }

            /// <summary>
            /// Gets the value of a given field.
            /// </summary>
            /// <param name="key">Field name.</param>
            /// <returns>The corresponding value.</returns>
            public IArray this[string key] => Parent.Fields[key][Index];

            /// <summary>
            /// Enumerates fieldstructure/value pairs of the dictionary.
            /// </summary>
            /// <returns>All field/value pairs in the structure.</returns>
            public IEnumerator<KeyValuePair<string, IArray>> GetEnumerator()
            {
                foreach (var field in Parent.Fields)
                {
                    yield return new KeyValuePair<string, IArray>(field.Key, field.Value[Index]);
                }
            }

            /// <summary>
            /// Enumerates field/value pairs of the structure.
            /// </summary>
            /// <returns>All field/value pairs in the structure.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Checks if the structure has a given field.
            /// </summary>
            /// <param name="key">Field name</param>
            /// <returns>True iff the structure has a given field.</returns>
            public bool ContainsKey(string key) => Parent.Fields.ContainsKey(key);

            /// <summary>
            /// Tries to get the value of a given field.
            /// </summary>
            /// <param name="key">Field name.</param>
            /// <param name="value">Value (or null if the field is not present).</param>
            /// <returns>Success status of the query.</returns>
            public bool TryGetValue(string key, out IArray value)
            {
                var success = Parent.Fields.TryGetValue(key, out var array);
                if (!success)
                {
                    value = default(IArray);
                    return false;
                }
                value = array[Index];
                return true;
            }
        }
    }

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
                    var datasetId = H5D.open(group, variableName);
                    var value = ReadDataset(datasetId);
                    variables.Add(new MatVariable(value, variableName, false));
                    break;
                case H5O.type_t.GROUP:
                    if (variableName == "#refs#")
                    {
                        return 0;
                    }
                    var groupId = H5G.open(group, variableName);
                    var groupValue = ReadGroup(groupId);
                    variables.Add(new MatVariable(groupValue, variableName, false));
                    break;
                default:
                    throw new NotImplementedException();
            }
            return 0;
        }

        private static string GetMatlabClassOfDataset(long datasetId)
        {
            var attributeId = H5A.open_by_name(datasetId, ".", "MATLAB_class");

            var typeId = H5A.get_type(attributeId);
            var cl = H5T.get_class(typeId);
            if (cl != H5T.class_t.STRING)
            {
                throw new NotImplementedException();
            }
            var classId = H5T.copy(H5T.C_S1);
            var typeIdSize = H5T.get_size(typeId);
            H5T.set_size(classId, typeIdSize);
            var buf = Marshal.AllocHGlobal(typeIdSize);
            H5A.read(attributeId, classId, buf);
            var matlabClassNameBytes = new byte[(int)typeIdSize];
            Marshal.Copy(buf, matlabClassNameBytes, 0, (int)typeIdSize);
            Marshal.FreeHGlobal(buf);
            return Encoding.ASCII.GetString(matlabClassNameBytes);
        }

        private static int[] GetDimensionsOfDataset(long datasetId)
        {
            var spaceId = H5D.get_space(datasetId);
            var rank = H5S.get_simple_extent_ndims(spaceId);
            var dims = new ulong[rank];
            H5S.get_simple_extent_dims(spaceId, dims, null);
            Array.Reverse(dims);
            return dims.Select(x => (int)x).ToArray();
        }

        private static HdfMatlabClass ArrayTypeFromMatlabClassName(string matlabClassName)
        {
            switch (matlabClassName)
            {
                case "canonical empty":
                    return HdfMatlabClass.MEmpty;
                case "char":
                    return HdfMatlabClass.MChar;
                case "int8":
                    return HdfMatlabClass.MInt8;
                case "uint8":
                    return HdfMatlabClass.MUInt8;
                case "int16":
                    return HdfMatlabClass.MInt16;
                case "uint16":
                    return HdfMatlabClass.MUInt16;
                case "int32":
                    return HdfMatlabClass.MInt32;
                case "uint32":
                    return HdfMatlabClass.MUInt32;
                case "int64":
                    return HdfMatlabClass.MInt64;
                case "uint64":
                    return HdfMatlabClass.MUInt64;
                case "single":
                    return HdfMatlabClass.MSingle;
                case "double":
                    return HdfMatlabClass.MDouble;
                case "cell":
                    return HdfMatlabClass.MCell;
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

        private static IArray ReadGroup(long groupId)
        {
            var matlabClass = GetMatlabClassOfDataset(groupId);
            if (matlabClass == "struct")
            {
                return ReadStruct(groupId);
            }
            throw new NotImplementedException();
        }

        private static string[] ReadFieldNames(long groupId)
        {
            // Try to read fields from MATLAB_fields.
            var attrId = H5A.open_by_name(groupId, ".", "MATLAB_fields");
            if (attrId == 0)
            {
                throw new NotImplementedException();
            }
            var spaceId = H5A.get_space(attrId);
            var rank = H5S.get_simple_extent_ndims(spaceId);
            var dims = new ulong[rank];
            H5S.get_simple_extent_dims(spaceId, dims, null);
            Array.Reverse(dims);
            var dimensions = dims.Select(x => (int)x).ToArray();
            var numberOfFields = dimensions.NumberOfElements();

            var field_id = H5A.get_type(attrId);

            var fieldNamePointersSizeInBytes = numberOfFields * Marshal.SizeOf(default(H5T.hvl_t));
            var fieldNamesBuf = Marshal.AllocHGlobal(fieldNamePointersSizeInBytes);
            H5A.read(attrId, field_id, fieldNamesBuf);

            var fieldNamePointers = new IntPtr[numberOfFields * 2];
            Marshal.Copy(fieldNamesBuf, fieldNamePointers, 0, numberOfFields * 2);
            Marshal.FreeHGlobal(fieldNamesBuf);
            var fieldNames = new string[numberOfFields];
            for (var i = 0; i < numberOfFields; i++)
            {
                var stringLength = fieldNamePointers[i * 2];
                var stringPointer = fieldNamePointers[i * 2 + 1];
                fieldNames[i] = Marshal.PtrToStringAnsi(stringPointer, (int)stringLength);
            }
            return fieldNames;
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
                var firstFieldId = H5D.open(groupId, fieldNames[0]);
                var firstFieldTypeId = H5D.get_type(firstFieldId);
                if (H5T.get_class(firstFieldTypeId) == H5T.class_t.REFERENCE)
                {
                    if (H5A.exists_by_name(firstFieldId, ".", "MATLAB_class") != 0)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var dimensions = GetDimensionsOfDataset(firstFieldId);
                        var numberOfElements = dimensions.NumberOfElements();
                        var dictionary = new Dictionary<string, List<IArray>>();
                        foreach (var fieldName in fieldNames)
                        {
                            var fieldType = GetObjectType(groupId, fieldName);
                            dictionary[fieldName] = new List<IArray>();
                            switch (fieldType)
                            {
                                case H5O.type_t.DATASET:
                                    var fieldId = H5D.open(groupId, fieldName);
                                    var buf = Marshal.AllocHGlobal(Marshal.SizeOf(default(IntPtr)) * numberOfElements);
                                    H5D.read(fieldId, H5T.STD_REF_OBJ, H5S.ALL, H5S.ALL, H5P.DEFAULT, buf);
                                    for (var i = 0; i < numberOfElements; i++)
                                    {
                                        var fieldDataSet = H5R.dereference(
                                            fieldId,
                                            H5P.DEFAULT,
                                            H5R.type_t.OBJECT,
                                            buf + (i * Marshal.SizeOf(default(IntPtr))));
                                        var dataset = ReadDataset(fieldDataSet);
                                        dictionary[fieldName].Add(dataset);
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        return new HdfStructureArray(dimensions, dictionary);
                    }
                }
                else
                {
                    throw new NotImplementedException();
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
                case HdfMatlabClass.MEmpty:
                    return HdfArray.Empty();
                case HdfMatlabClass.MChar:
                    return ReadCharArray(datasetId, dims);
                case HdfMatlabClass.MInt8:
                    return ReadNumericalArray<sbyte>(datasetId, dims, arrayType);
                case HdfMatlabClass.MUInt8:
                    return ReadNumericalArray<byte>(datasetId, dims, arrayType);
                case HdfMatlabClass.MInt16:
                    return ReadNumericalArray<short>(datasetId, dims, arrayType);
                case HdfMatlabClass.MUInt16:
                    return ReadNumericalArray<ushort>(datasetId, dims, arrayType);
                case HdfMatlabClass.MInt32:
                    return ReadNumericalArray<int>(datasetId, dims, arrayType);
                case HdfMatlabClass.MUInt32:
                    return ReadNumericalArray<uint>(datasetId, dims, arrayType);
                case HdfMatlabClass.MInt64:
                    return ReadNumericalArray<long>(datasetId, dims, arrayType);
                case HdfMatlabClass.MUInt64:
                    return ReadNumericalArray<ulong>(datasetId, dims, arrayType);
                case HdfMatlabClass.MSingle:
                    return ReadNumericalArray<float>(datasetId, dims, arrayType);
                case HdfMatlabClass.MDouble:
                    return ReadNumericalArray<double>(datasetId, dims, arrayType);
                case HdfMatlabClass.MCell:
                    return ReadCellArray(datasetId, dims);
            }
            throw new NotImplementedException($"Unknown array type: {arrayType}.");
        }

        private static IArray ReadCellArray(long datasetId, int[] dims)
        {
            var numberOfElements = dims.NumberOfElements();
            var buf = Marshal.AllocHGlobal(Marshal.SizeOf(default(IntPtr)) * numberOfElements);
            H5D.read(datasetId, H5T.STD_REF_OBJ, H5S.ALL, H5S.ALL, H5P.DEFAULT, buf);
            var elements = new IArray[numberOfElements];
            for (var i = 0; i < numberOfElements; i++)
            {
                var fieldDataSet = H5R.dereference(
                    datasetId,
                    H5P.DEFAULT,
                    H5R.type_t.OBJECT,
                    buf + (i * Marshal.SizeOf(default(IntPtr))));
                var dataset = ReadDataset(fieldDataSet);
                elements[i] = dataset;
            }
            return new HdfCellArray(dims, elements);
        }

        private static int SizeOfArrayElement(HdfMatlabClass arrayType)
        {
            switch (arrayType)
            {
                case HdfMatlabClass.MInt8:
                case HdfMatlabClass.MUInt8:
                    return 1;
                case HdfMatlabClass.MInt16:
                case HdfMatlabClass.MUInt16:
                    return 2;
                case HdfMatlabClass.MInt32:
                case HdfMatlabClass.MUInt32:
                case HdfMatlabClass.MSingle:
                    return 4;
                case HdfMatlabClass.MInt64:
                case HdfMatlabClass.MUInt64:
                case HdfMatlabClass.MDouble:
                    return 8;
            }

            throw new NotImplementedException();
        }

        private static long H5tTypeFromHdfMatlabClass(HdfMatlabClass arrayType)
        {
            switch (arrayType)
            {
                case HdfMatlabClass.MInt8:
                    return H5T.NATIVE_INT8;
                case HdfMatlabClass.MUInt8:
                    return H5T.NATIVE_UINT8;
                case HdfMatlabClass.MInt16:
                    return H5T.NATIVE_INT16;
                case HdfMatlabClass.MUInt16:
                    return H5T.NATIVE_UINT16;
                case HdfMatlabClass.MInt32:
                    return H5T.NATIVE_INT32;
                case HdfMatlabClass.MUInt32:
                    return H5T.NATIVE_UINT32;
                case HdfMatlabClass.MInt64:
                    return H5T.NATIVE_INT64;
                case HdfMatlabClass.MUInt64:
                    return H5T.NATIVE_UINT64;
                case HdfMatlabClass.MSingle:
                    return H5T.NATIVE_FLOAT;
                case HdfMatlabClass.MDouble:
                    return H5T.NATIVE_DOUBLE;
            }
            throw new NotImplementedException();
        }

        private static T[] ConvertDataToProperType<T>(byte[] bytes, HdfMatlabClass arrayType)
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
            var dataBuffer = Marshal.AllocHGlobal(dataSize);
            H5D.read(datasetId, elementType, H5S.ALL, H5S.ALL, H5P.DEFAULT, dataBuffer);
            var data = new byte[dataSize];
            Marshal.Copy(dataBuffer, data, 0, dataSize);
            Marshal.FreeHGlobal(dataBuffer);
            return data;
        }

        private static IArray ReadNumericalArray<T>(long datasetId, int[] dims, HdfMatlabClass arrayType)
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
                if (arrayType == HdfMatlabClass.MDouble)
                {
                    var complexData =
                        (convertedRealData as double[])
                            .Zip(convertedImaginaryData as double[], (x, y) => new Complex(x, y))
                            .ToArray();
                    return new HdfNumericalArrayOf<Complex>(dims, complexData);
                }
                else
                {
                    var complexData =
                        convertedRealData
                            .Zip(convertedImaginaryData, (x, y) => new ComplexOf<T>(x, y))
                            .ToArray();
                    return new HdfNumericalArrayOf<ComplexOf<T>>(dims, complexData);
                }
            }
            if (dataSize != storageSize)
            {
                throw new Exception("Data size mismatch.");
            }
            var data = ReadDataset(datasetId, H5tTypeFromHdfMatlabClass(arrayType), dataSize);
            var convertedData = ConvertDataToProperType<T>(data, arrayType);
            return new HdfNumericalArrayOf<T>(dims, convertedData);
        }

        private static IArray ReadCharArray(long datasetId, int[] dims)
        {
            var storageSize = (int)H5D.get_storage_size(datasetId);
            var data = ReadDataset(datasetId, H5T.NATIVE_UINT16, storageSize);
            var str = Encoding.Unicode.GetString(data);
            return new HdfCharArray(dims, str);
        }
    }
}
