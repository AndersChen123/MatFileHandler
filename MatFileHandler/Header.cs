// Copyright 2017-2018 Alexander Luzgarev

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MatFileHandler
{
    /// <summary>
    /// Header of a .mat file.
    /// </summary>
    internal class Header
    {
        private Header(byte[] rawBytes, string text, long subsystemDataOffset, int version)
        {
            RawBytes = rawBytes;
            Text = text;
            SubsystemDataOffset = subsystemDataOffset;
            Version = version;
        }

        /// <summary>
        /// Gets raw byte contents of the header.
        /// </summary>
        public byte[] RawBytes { get; }

        /// <summary>
        /// Gets the header text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets subsystem data offset.
        /// </summary>
        public long SubsystemDataOffset { get; }

        /// <summary>
        /// Gets file version.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Creates a new header for a .mat file.
        /// </summary>
        /// <returns>A header.</returns>
        public static Header CreateNewHeader()
        {
            var subsystemDataOffset = Enumerable.Repeat((byte)0, 8).ToArray();
            var dateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            var length = 116 - "MATLAB 5.0 MAT-file, Platform: , Created on: ".Length - dateTime.Length;
            var platform = GetOperatingSystem();
            var padding = string.Empty;
            if (platform.Length < length)
            {
                padding = padding.PadRight(length - platform.Length);
            }
            else
            {
                platform = platform.Remove(length);
            }
            var text = $"MATLAB 5.0 MAT-file, Platform: {platform}, Created on: {dateTime}{padding}";
            return new Header(null, text, 0, 256);
        }

        /// <summary>
        /// Read header from file.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>The header read.</returns>
        public static Header Read(BinaryReader reader)
        {
            var rawBytes = reader.ReadBytes(128);
            using (var stream = new MemoryStream(rawBytes))
            {
                using (var newReader = new BinaryReader(stream))
                {
                    var textBytes = newReader.ReadBytes(116);
                    var text = System.Text.Encoding.UTF8.GetString(textBytes);
                    var subsystemDataOffsetBytes = newReader.ReadBytes(8);
                    var subsystemDataOffset = BitConverter.ToInt64(subsystemDataOffsetBytes, 0);
                    var version = newReader.ReadInt16();
                    var endian = newReader.ReadInt16();
                    var isLittleEndian = endian == 19785;
                    if (!isLittleEndian)
                    {
                        throw new NotSupportedException("Big-endian files are not supported.");
                    }

                    return new Header(rawBytes, text, subsystemDataOffset, version);
                }
            }
        }

        private static string GetOperatingSystem()
        {
#if NET461
            return "Windows";
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macOS";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }
            return "Unknown";
#endif
        }
    }
}