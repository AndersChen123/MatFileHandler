// Copyright 2017-2018 Alexander Luzgarev

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MatFileHandler
{
    /// <summary>
    /// Static class for constructing various arrays from raw data elements read from .mat files.
    /// </summary>
    internal static class DataElementConverter
    {
        /// <summary>
        /// Construct a complex sparse array.
        /// </summary>
        /// <param name="dimensions">Array dimensions.</param>
        /// <param name="rowIndex">Row indices.</param>
        /// <param name="columnIndex">Denotes index ranges for each column.</param>
        /// <param name="data">Real parts of the values.</param>
        /// <param name="imaginaryData">Imaginary parts of the values.</param>
        /// <returns>A constructed array.</returns>
        public static MatArray ConvertToMatSparseArrayOfComplex(
            int[] dimensions,
            int[] rowIndex,
            int[] columnIndex,
            DataElement data,
            DataElement imaginaryData)
        {
            var realParts = DataExtraction.GetDataAsDouble(data).ToArrayLazily();
            var imaginaryParts = DataExtraction.GetDataAsDouble(imaginaryData).ToArrayLazily();
            if (realParts == null)
            {
                throw new HandlerException("Couldn't read sparse array.");
            }
            var dataDictionary =
                DataExtraction.ConvertMatlabSparseToDictionary(
                    rowIndex,
                    columnIndex,
                    j => new Complex(realParts[j], imaginaryParts[j]));
            return new MatSparseArrayOf<Complex>(dimensions, dataDictionary);
        }

        /// <summary>
        /// Construct a double sparse array or a logical sparse array.
        /// </summary>
        /// <typeparam name="T">Element type (Double or Boolean).</typeparam>
        /// <param name="flags">Array flags.</param>
        /// <param name="dimensions">Array dimensions.</param>
        /// <param name="rowIndex">Row indices.</param>
        /// <param name="columnIndex">Denotes index ranges for each column.</param>
        /// <param name="data">The values.</param>
        /// <returns>A constructed array.</returns>
        public static MatArray ConvertToMatSparseArrayOf<T>(
            ArrayFlags flags,
            int[] dimensions,
            int[] rowIndex,
            int[] columnIndex,
            DataElement data)
            where T : struct
        {
            if (dimensions.Length != 2)
            {
                throw new NotSupportedException("Only 2-dimensional sparse arrays are supported");
            }
            if (data == null)
            {
                throw new ArgumentException("Null data found.", "data");
            }
            var elements =
                ConvertDataToSparseProperType<T>(data, flags.Variable.HasFlag(Variable.IsLogical));
            if (elements == null)
            {
                throw new HandlerException("Couldn't read sparse array.");
            }
            var dataDictionary =
                DataExtraction.ConvertMatlabSparseToDictionary(rowIndex, columnIndex, j => elements[j]);
            return new MatSparseArrayOf<T>(dimensions, dataDictionary);
        }

        /// <summary>
        /// Construct a numerical array.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="flags">Array flags.</param>
        /// <param name="dimensions">Array dimensions.</param>
        /// <param name="realData">Real parts of the values.</param>
        /// <param name="imaginaryData">Imaginary parts of the values.</param>
        /// <returns>A constructed array.</returns>
        /// <remarks>
        /// Possible values for T: Int8, UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double,
        ///   ComplexOf&lt;TReal&gt; (where TReal is one of Int8, UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single),
        ///   Complex.
        /// </remarks>
        public static MatArray ConvertToMatNumericalArrayOf<T>(
            ArrayFlags flags,
            int[] dimensions,
            DataElement realData,
            DataElement imaginaryData)
            where T : struct
        {
            if (flags.Variable.HasFlag(Variable.IsLogical))
            {
                var data = DataExtraction.GetDataAsUInt8(realData).ToArrayLazily().Select(x => x != 0).ToArray();
                return new MatNumericalArrayOf<bool>(dimensions, data);
            }
            switch (flags.Class)
            {
                case ArrayType.MxChar:
                    switch (realData)
                    {
                        case MiNum<byte> dataByte:
                            return ConvertToMatCharArray(dimensions, dataByte);
                        case MiNum<ushort> dataUshort:
                            return ConvertToMatCharArray(dimensions, dataUshort);
                        default:
                            throw new NotSupportedException("Only utf8, utf16 or ushort char arrays are supported.");
                    }
                case ArrayType.MxDouble:
                case ArrayType.MxSingle:
                case ArrayType.MxInt8:
                case ArrayType.MxUInt8:
                case ArrayType.MxInt16:
                case ArrayType.MxUInt16:
                case ArrayType.MxInt32:
                case ArrayType.MxUInt32:
                case ArrayType.MxInt64:
                case ArrayType.MxUInt64:
                    var dataArray = ConvertDataToProperType<T>(realData, flags.Class);
                    if (flags.Variable.HasFlag(Variable.IsComplex))
                    {
                        var dataArray2 = ConvertDataToProperType<T>(imaginaryData, flags.Class);
                        if (flags.Class == ArrayType.MxDouble)
                        {
                            var complexArray =
                                (dataArray as double[])
                                .Zip(dataArray2 as double[], (x, y) => new Complex(x, y))
                                .ToArray();
                            return new MatNumericalArrayOf<Complex>(dimensions, complexArray);
                        }
                        var complexDataArray = dataArray.Zip(dataArray2, (x, y) => new ComplexOf<T>(x, y)).ToArray();
                        return new MatNumericalArrayOf<ComplexOf<T>>(dimensions, complexDataArray);
                    }
                    return new MatNumericalArrayOf<T>(dimensions, dataArray);
                default:
                    throw new NotSupportedException();
            }
        }

        private static MatCharArrayOf<byte> ConvertToMatCharArray(
            int[] dimensions,
            MiNum<byte> dataElement)
        {
            var data = dataElement?.Data;
            return new MatCharArrayOf<byte>(dimensions, data, Encoding.UTF8.GetString(data));
        }

        private static T[] ConvertDataToProperType<T>(DataElement data, ArrayType arrayType)
        {
            switch (arrayType)
            {
                case ArrayType.MxDouble:
                    return DataExtraction.GetDataAsDouble(data).ToArrayLazily() as T[];
                case ArrayType.MxSingle:
                    return DataExtraction.GetDataAsSingle(data).ToArrayLazily() as T[];
                case ArrayType.MxInt8:
                    return DataExtraction.GetDataAsInt8(data).ToArrayLazily() as T[];
                case ArrayType.MxUInt8:
                    return DataExtraction.GetDataAsUInt8(data).ToArrayLazily() as T[];
                case ArrayType.MxInt16:
                    return DataExtraction.GetDataAsInt16(data).ToArrayLazily() as T[];
                case ArrayType.MxUInt16:
                    return DataExtraction.GetDataAsUInt16(data).ToArrayLazily() as T[];
                case ArrayType.MxInt32:
                    return DataExtraction.GetDataAsInt32(data).ToArrayLazily() as T[];
                case ArrayType.MxUInt32:
                    return DataExtraction.GetDataAsUInt32(data).ToArrayLazily() as T[];
                case ArrayType.MxInt64:
                    return DataExtraction.GetDataAsInt64(data).ToArrayLazily() as T[];
                case ArrayType.MxUInt64:
                    return DataExtraction.GetDataAsUInt64(data).ToArrayLazily() as T[];
                default:
                    throw new NotSupportedException();
            }
        }

        private static T[] ConvertDataToSparseProperType<T>(DataElement data, bool isLogical)
        {
            if (isLogical)
            {
                return DataExtraction.GetDataAsUInt8(data).ToArrayLazily().Select(x => x != 0).ToArray() as T[];
            }
            switch (data)
            {
                case MiNum<double> _:
                    return DataExtraction.GetDataAsDouble(data).ToArrayLazily() as T[];
                default:
                    throw new NotSupportedException();
            }
        }

        private static MatCharArrayOf<ushort> ConvertToMatCharArray(
            int[] dimensions,
            MiNum<ushort> dataElement)
        {
            var data = dataElement?.Data;
            return new MatCharArrayOf<ushort>(
                dimensions,
                data,
                new string(data.Select(x => (char)x).ToArray()));
        }
    }
}