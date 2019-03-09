using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace MatFileHandler
{
    internal static class MatFileHdfReader
    {
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
