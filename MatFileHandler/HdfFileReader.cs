using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace MatFileHandler
{
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

        private static ArrayType ArrayTypeFromMatlabClassName(string matlabClassName)
        {
            switch (matlabClassName)
            {
                case "char":
                    return ArrayType.MxChar;
                case "int8":
                    return ArrayType.MxInt8;
                case "uint8":
                    return ArrayType.MxUInt8;
                case "int16":
                    return ArrayType.MxInt16;
                case "uint16":
                    return ArrayType.MxUInt16;
                case "int32":
                    return ArrayType.MxInt32;
                case "uint32":
                    return ArrayType.MxUInt32;
                case "int64":
                    return ArrayType.MxInt64;
                case "uint64":
                    return ArrayType.MxUInt64;
                case "double":
                    return ArrayType.MxDouble;
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
                case ArrayType.MxChar:
                    return ReadCharArray(datasetId, dims);
                case ArrayType.MxInt8:
                    return ReadNumericalArray<sbyte>(datasetId, dims, arrayType);
                case ArrayType.MxUInt8:
                    return ReadNumericalArray<byte>(datasetId, dims, arrayType);
                case ArrayType.MxInt16:
                    return ReadNumericalArray<short>(datasetId, dims, arrayType);
                case ArrayType.MxUInt16:
                    return ReadNumericalArray<ushort>(datasetId, dims, arrayType);
                case ArrayType.MxInt32:
                    return ReadNumericalArray<int>(datasetId, dims, arrayType);
                case ArrayType.MxUInt32:
                    return ReadNumericalArray<uint>(datasetId, dims, arrayType);
                case ArrayType.MxInt64:
                    return ReadNumericalArray<long>(datasetId, dims, arrayType);
                case ArrayType.MxUInt64:
                    return ReadNumericalArray<ulong>(datasetId, dims, arrayType);
                case ArrayType.MxSingle:
                    return ReadNumericalArray<float>(datasetId, dims, arrayType);
                case ArrayType.MxDouble:
                    return ReadNumericalArray<double>(datasetId, dims, arrayType);
            }
            throw new NotImplementedException($"Unknown array type: {arrayType}.");
        }

        private static int SizeOfArrayElement(ArrayType arrayType)
        {
            switch (arrayType)
            {
                case ArrayType.MxInt8:
                case ArrayType.MxUInt8:
                    return 1;
                case ArrayType.MxInt16:
                case ArrayType.MxUInt16:
                    return 2;
                case ArrayType.MxInt32:
                case ArrayType.MxUInt32:
                case ArrayType.MxSingle:
                    return 4;
                case ArrayType.MxInt64:
                case ArrayType.MxUInt64:
                case ArrayType.MxDouble:
                    return 8;
            }

            throw new NotImplementedException();
        }

        private static long H5tTypeFromArrayType(ArrayType arrayType)
        {
            switch (arrayType)
            {
                case ArrayType.MxInt8:
                    return H5T.NATIVE_INT8;
                case ArrayType.MxUInt8:
                    return H5T.NATIVE_UINT8;
                case ArrayType.MxInt16:
                    return H5T.NATIVE_INT16;
                case ArrayType.MxUInt16:
                    return H5T.NATIVE_UINT16;
                case ArrayType.MxInt32:
                    return H5T.NATIVE_INT32;
                case ArrayType.MxUInt32:
                    return H5T.NATIVE_UINT32;
                case ArrayType.MxInt64:
                    return H5T.NATIVE_INT64;
                case ArrayType.MxUInt64:
                    return H5T.NATIVE_UINT64;
                case ArrayType.MxSingle:
                    return H5T.NATIVE_FLOAT;
                case ArrayType.MxDouble:
                    return H5T.NATIVE_DOUBLE;
            }
            throw new NotImplementedException();
        }

        private static T[] ConvertDataToProperType<T>(byte[] bytes, ArrayType arrayType)
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
            return data;
        }

        private static IArray ReadNumericalArray<T>(long datasetId, int[] dims, ArrayType arrayType)
            where T : struct
        {
            var numberOfElements = dims.NumberOfElements();
            var dataSize = numberOfElements * SizeOfArrayElement(arrayType);
            var storageSize = (int)H5D.get_storage_size(datasetId);
            if (dataSize != storageSize)
            {
                throw new Exception("Data size mismatch.");
            }
            var data = ReadDataset(datasetId, H5tTypeFromArrayType(arrayType), dataSize);
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
