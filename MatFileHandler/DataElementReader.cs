// Copyright 2017-2019 Alexander Luzgarev

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MatFileHandler
{
    /// <summary>
    /// Functions for reading data elements from a .mat file.
    /// </summary>
    internal class DataElementReader
    {
        private readonly SubsystemData subsystemData;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataElementReader"/> class.
        /// </summary>
        /// <param name="subsystemData">Reference to file's SubsystemData.</param>
        public DataElementReader(SubsystemData subsystemData)
        {
            this.subsystemData = subsystemData ?? throw new ArgumentNullException(nameof(subsystemData));
        }

        /// <summary>
        /// Read a data element.
        /// </summary>
        /// <param name="reader">Input reader.</param>
        /// <returns>Data element.</returns>
        public DataElementWithMetadata Read(BinaryReader reader)
        {
            var (dataReader, tag) = ReadTag(reader);
            DataElementWithMetadata result = ReadElementWithFlags(tag, dataReader);
            if (tag.Type != DataType.MiCompressed)
            {
                var position = reader.BaseStream.Position;
                if (position % 8 != 0)
                {
                    reader.ReadBytes(8 - (int)(position % 8));
                }
            }

            return result;
        }

        /// <summary>
        /// Parse opaque link data.
        /// </summary>
        /// <param name="data">Opaque link data.</param>
        /// <returns>Dimensions array, links array, class index.</returns>
        internal static (int[] dimensions, int[] links, int classIndex) ParseOpaqueData(uint[] data)
        {
            var nDims = data[1];
            var dimensions = new int[nDims];
            var position = 2;
            for (var i = 0; i < nDims; i++)
            {
                dimensions[i] = (int)data[position];
                position++;
            }

            var count = dimensions.NumberOfElements();
            var links = new int[count];
            for (var i = 0; i < count; i++)
            {
                links[i] = (int)data[position];
                position++;
            }

            var classIndex = (int)data[position];

            return (dimensions, links, classIndex);
        }

        private static ArrayFlags ReadArrayFlags(DataElement element)
        {
            var flagData = (element as MiNum<uint>)?.Data ??
                           throw new HandlerException("Unexpected type in array flags.");
            var class_ = (ArrayType)(flagData[0] & 0xff);
            var variableFlags = (flagData[0] >> 8) & 0x0e;
            return new ArrayFlags
            {
                Class = class_,
                Variable = (Variable)variableFlags,
            };
        }

        private static DataElement ReadData(DataElement element)
        {
            return element;
        }

        private static int[] ReadDimensionsArray(MiNum<int> element)
        {
            return element.Data;
        }

        private static string[] ReadFieldNames(MiNum<sbyte> element, int fieldNameLength)
        {
            var numberOfFields = element.Data.Length / fieldNameLength;
            var result = new string[numberOfFields];
            for (var i = 0; i < numberOfFields; i++)
            {
                var list = new List<byte>();
                var position = i * fieldNameLength;
                while (element.Data[position] != 0)
                {
                    list.Add((byte)element.Data[position]);
                    position++;
                }

                result[i] = Encoding.ASCII.GetString(list.ToArray());
            }

            return result;
        }

        private static string ReadName(MiNum<sbyte> element)
        {
            return Encoding.ASCII.GetString(element.Data.Select(x => (byte)x).ToArray());
        }

        private static DataElement ReadNum<T>(Tag tag, BinaryReader reader)
            where T : struct
        {
            var bytes = reader.ReadBytes(tag.Length);
            if (tag.Type == DataType.MiUInt8)
            {
                return new MiNum<byte>(bytes);
            }

            var result = new T[bytes.Length / tag.ElementSize];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return new MiNum<T>(result);
        }

        private static (ArrayFlags flags, uint nzMax) ReadSparseArrayFlags(DataElement element)
        {
            var arrayFlags = ReadArrayFlags(element);
            var flagData = (element as MiNum<uint>)?.Data ??
                           throw new HandlerException("Unexpected type in sparse array flags.");
            var nzMax = flagData[1];
            return (arrayFlags, nzMax);
        }

        private static (BinaryReader, Tag) ReadTag(BinaryReader reader)
        {
            var type = reader.ReadInt32();
            var typeHi = type >> 16;
            if (typeHi == 0)
            {
                var length = reader.ReadInt32();
                return (reader, new Tag((DataType)type, length));
            }
            else
            {
                var length = typeHi;
                type = type & 0xffff;
                var smallReader = new BinaryReader(new MemoryStream(reader.ReadBytes(4)));
                return (smallReader, new Tag((DataType)type, length));
            }
        }

        private DataElement ContinueReadingCellArray(
            BinaryReader reader,
            int[] dimensions)
        {
            var numberOfElements = dimensions.NumberOfElements();
            var elements = new List<IArray>();
            for (var i = 0; i < numberOfElements; i++)
            {
                var element = Read(reader).Element as IArray;
                elements.Add(element);
            }

            return new MatCellArray(dimensions, elements);
        }

        private DataElementWithMetadata ContinueReadingOpaque(BinaryReader reader)
        {
            var nameElement = Read(reader).Element as MiNum<sbyte> ??
                              throw new HandlerException("Unexpected type in object name.");
            var name = ReadName(nameElement);
            var anotherElement = Read(reader).Element as MiNum<sbyte> ??
                                 throw new HandlerException("Unexpected type in object type description.");
            var typeDescription = ReadName(anotherElement);
            var classNameElement = Read(reader).Element as MiNum<sbyte> ??
                                   throw new HandlerException("Unexpected type in class name.");
            var className = ReadName(classNameElement);
            var dataElement = Read(reader).Element;
            var data = ReadData(dataElement);
            if (data is MatNumericalArrayOf<uint> linkElement)
            {
                var (dimensions, indexToObjectId, classIndex) = ParseOpaqueData(linkElement.Data);
                return new DataElementWithMetadata(
                    new OpaqueLink(
                        typeDescription,
                        className,
                        dimensions,
                        data,
                        indexToObjectId,
                        classIndex,
                        subsystemData),
                    default,
                    name);
            }
            else
            {
                return new DataElementWithMetadata(
                    new Opaque(
                        typeDescription,
                        className,
                        new int[] { },
                        data),
                    default,
                    name);
            }
        }

        private DataElementWithMetadata ContinueReadingSparseArray(
            BinaryReader reader,
            DataElement firstElement,
            int[] dimensions,
            string name)
        {
            var (arrayFlags, nzMax) = ReadSparseArrayFlags(firstElement);
            var rowIndex = Read(reader).Element as MiNum<int> ??
                           throw new HandlerException("Unexpected type in row indices of a sparse array.");
            var columnIndex = Read(reader).Element as MiNum<int> ??
                              throw new HandlerException("Unexpected type in column indices of a sparse array.");
            var data = Read(reader).Element;
            if (arrayFlags.Variable.HasFlag(Variable.IsLogical))
            {
                return new DataElementWithMetadata(
                    DataElementConverter.ConvertToMatSparseArrayOf<bool>(
                        arrayFlags,
                        dimensions,
                        rowIndex.Data,
                        columnIndex.Data,
                        data),
                    arrayFlags,
                    name,
                    nzMax);
            }

            if (arrayFlags.Variable.HasFlag(Variable.IsComplex))
            {
                var imaginaryData = Read(reader).Element;
                return new DataElementWithMetadata(
                    DataElementConverter.ConvertToMatSparseArrayOfComplex(
                        dimensions,
                        rowIndex.Data,
                        columnIndex.Data,
                        data,
                        imaginaryData),
                    arrayFlags,
                    name,
                    nzMax);
            }

            switch (data)
            {
                case MiNum<double> _:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatSparseArrayOf<double>(
                            arrayFlags,
                            dimensions,
                            rowIndex.Data,
                            columnIndex.Data,
                            data),
                        arrayFlags,
                        name,
                        nzMax);
                default:
                    throw new NotSupportedException("Only double and logical sparse arrays are supported.");
            }
        }

        private DataElement ContinueReadingStructure(
            BinaryReader reader,
            int[] dimensions,
            int fieldNameLength)
        {
            var element = Read(reader).Element;
            var fieldNames = ReadFieldNames(element as MiNum<sbyte>, fieldNameLength);
            var fields = new Dictionary<string, List<IArray>>();
            foreach (var fieldName in fieldNames)
            {
                fields[fieldName] = new List<IArray>();
            }

            var numberOfElements = dimensions.NumberOfElements();
            for (var i = 0; i < numberOfElements; i++)
            {
                foreach (var fieldName in fieldNames)
                {
                    var field = Read(reader).Element as IArray;
                    fields[fieldName].Add(field);
                }
            }

            return new MatStructureArray(dimensions, fields);
        }

        private DataElementWithMetadata Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                return Read(reader);
            }
        }

        private DataElementWithMetadata ReadCompressed(Tag tag, BinaryReader reader)
        {
            reader.ReadBytes(2);
            var compressedData = new byte[tag.Length - 6];
            reader.BaseStream.Read(compressedData, 0, tag.Length - 6);
            reader.ReadBytes(4);
            var resultStream = new MemoryStream();
            using (var compressedStream = new MemoryStream(compressedData))
            {
                using (var stream = new DeflateStream(compressedStream, CompressionMode.Decompress, leaveOpen: true))
                {
                    stream.CopyTo(resultStream);
                }
            }

            resultStream.Position = 0;
            return Read(resultStream);
        }

        private DataElementWithMetadata ReadElementWithFlags(Tag tag, BinaryReader reader)
        {
            switch (tag.Type)
            {
                case DataType.MiMatrix:
                    return ReadMatrix(tag, reader);
                case DataType.MiCompressed:
                    return ReadCompressed(tag, reader);
                default:
                    var element = ReadElementWithoutFlags(tag, reader);
                    return new DataElementWithMetadata(element);
            }
        }

        private DataElement ReadElementWithoutFlags(Tag tag, BinaryReader reader)
        {
            switch (tag.Type)
            {
                case DataType.MiInt8:
                    return ReadNum<sbyte>(tag, reader);
                case DataType.MiUInt8:
                case DataType.MiUtf8:
                    return ReadNum<byte>(tag, reader);
                case DataType.MiInt16:
                    return ReadNum<short>(tag, reader);
                case DataType.MiUInt16:
                case DataType.MiUtf16:
                    return ReadNum<ushort>(tag, reader);
                case DataType.MiInt32:
                    return ReadNum<int>(tag, reader);
                case DataType.MiUInt32:
                    return ReadNum<uint>(tag, reader);
                case DataType.MiSingle:
                    return ReadNum<float>(tag, reader);
                case DataType.MiDouble:
                    return ReadNum<double>(tag, reader);
                case DataType.MiInt64:
                    return ReadNum<long>(tag, reader);
                case DataType.MiUInt64:
                    return ReadNum<ulong>(tag, reader);
                default:
                    throw new NotSupportedException("Unknown element.");
            }
        }

        private DataElementWithMetadata ReadMatrix(Tag tag, BinaryReader reader)
        {
            if (tag.Length == 0)
            {
                return new DataElementWithMetadata(MatArray.Empty());
            }

            var element1 = Read(reader).Element;
            var flags = ReadArrayFlags(element1);
            if (flags.Class == ArrayType.MxOpaque)
            {
                return ContinueReadingOpaque(reader);
            }

            var element2 =
                Read(reader).Element as MiNum<int>
                ?? throw new HandlerException("Unexpected type in array dimensions data.");
            var dimensions = ReadDimensionsArray(element2);
            var element3 =
                Read(reader).Element as MiNum<sbyte>
                ?? throw new HandlerException("Unexpected type in array name.");
            var name = ReadName(element3);
            if (flags.Class == ArrayType.MxCell)
            {
                return new DataElementWithMetadata(ContinueReadingCellArray(reader, dimensions));
            }

            if (flags.Class == ArrayType.MxSparse)
            {
                return ContinueReadingSparseArray(reader, element1, dimensions, name);
            }

            var element4 = Read(reader).Element;
            var data = ReadData(element4);
            DataElement imaginaryData = null;
            if (flags.Variable.HasFlag(Variable.IsComplex))
            {
                var element5 = Read(reader).Element;
                imaginaryData = ReadData(element5);
            }

            if (flags.Class == ArrayType.MxStruct)
            {
                var fieldNameLengthElement = data as MiNum<int> ??
                                             throw new HandlerException(
                                                 "Unexpected type in structure field name length.");
                return new DataElementWithMetadata(
                    ContinueReadingStructure(reader, dimensions, fieldNameLengthElement.Data[0]),
                    flags,
                    name);
            }

            switch (flags.Class)
            {
                case ArrayType.MxChar:
                    switch (data)
                    {
                        case MiNum<byte> _:
                            return new DataElementWithMetadata(
                                DataElementConverter.ConvertToMatNumericalArrayOf<byte>(
                                    flags,
                                    dimensions,
                                    data,
                                    imaginaryData),
                                flags,
                                name);
                        case MiNum<ushort> _:
                            return new DataElementWithMetadata(
                                DataElementConverter.ConvertToMatNumericalArrayOf<ushort>(
                                    flags,
                                    dimensions,
                                    data,
                                    imaginaryData),
                                flags,
                                name);
                        default:
                            throw new NotSupportedException(
                                $"This type of char array ({data.GetType()}) is not supported.");
                    }
                case ArrayType.MxInt8:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<sbyte>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxUInt8:
                    if (flags.Variable.HasFlag(Variable.IsLogical))
                    {
                        return new DataElementWithMetadata(
                            DataElementConverter.ConvertToMatNumericalArrayOf<bool>(
                                flags,
                                dimensions,
                                data,
                                imaginaryData),
                            flags,
                            name);
                    }

                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<byte>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxInt16:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<short>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxUInt16:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<ushort>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxInt32:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<int>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxUInt32:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<uint>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxInt64:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<long>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxUInt64:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<ulong>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxSingle:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<float>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                case ArrayType.MxDouble:
                    return new DataElementWithMetadata(
                        DataElementConverter.ConvertToMatNumericalArrayOf<double>(
                            flags,
                            dimensions,
                            data,
                            imaginaryData),
                        flags,
                        name);
                default:
                    throw new HandlerException("Unknown data type.");
            }
        }
    }
}