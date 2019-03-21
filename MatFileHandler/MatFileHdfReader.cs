// Copyright 2017-2018 Alexander Luzgarev

using System;
using System.IO;
using System.Runtime.InteropServices;
using HDF.PInvoke;

namespace MatFileHandler
{
    /// <summary>
    /// Reader for MATLAB HDF (-v7.3) files.
    /// </summary>
    internal static class MatFileHdfReader
    {
        /// <summary>
        /// Continue reading MATLAB HDF file after reading the MATLAB header.
        /// </summary>
        /// <param name="header">MATLAB header that was read.</param>
        /// <param name="stream">Stream to read the rest of the file from.</param>
        /// <returns>MATLAB data file contents.</returns>
        internal static IMatFile ContinueReadingHdfFile(Header header, Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var headerStream = new MemoryStream(header.RawBytes))
                {
                    headerStream.CopyTo(memoryStream);
                }
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                return ReadFromByteArray(bytes);
            }
        }

        private static IMatFile ReadFromByteArray(byte[] bytes)
        {
            var fileAccessPropertyList = H5P.create(H5P.FILE_ACCESS);
            H5P.set_fapl_core(fileAccessPropertyList, IntPtr.Add(IntPtr.Zero, 1024), 0);
            var ptr = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            H5P.set_file_image(fileAccessPropertyList, ptr, IntPtr.Add(IntPtr.Zero, bytes.Length));
            var fileId = H5F.open(Guid.NewGuid().ToString(), H5F.ACC_RDONLY, fileAccessPropertyList);
            var hdfFileReader = new HdfFileReader(fileId);
            var result = hdfFileReader.Read();
            H5F.close(fileId);
            H5F.clear_elink_file_cache(fileId);
            return result;
        }
    }
}
