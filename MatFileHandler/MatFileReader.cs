// Copyright 2017-2019 Alexander Luzgarev

using System;
using System.IO;

namespace MatFileHandler
{
    /// <summary>
    /// Class for reading .mat files.
    /// </summary>
    public class MatFileReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatFileReader"/> class with a stream.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        public MatFileReader(Stream stream)
        {
            Stream = stream;
        }

        private Stream Stream { get; }

        /// <summary>
        /// Reads the contents of a .mat file from the stream.
        /// </summary>
        /// <returns>Contents of the file.</returns>
        public IMatFile Read()
        {
            using (var reader = new BinaryReader(Stream))
            {
                return Read(reader);
            }
        }

        private static Header ReadHeader(BinaryReader reader)
        {
            return Header.Read(reader);
        }

        private IMatFile Read(BinaryReader reader)
        {
            var header = ReadHeader(reader);
            switch (header.Version)
            {
                case 256:
                    return MatFileLevel5Reader.ContinueReadingLevel5File(header, reader);
                case 512:
                    return MatFileHdfReader.ContinueReadingHdfFile(header, reader.BaseStream);
                default:
                    throw new NotSupportedException($"Unknown file format.");
            }
        }
    }
}