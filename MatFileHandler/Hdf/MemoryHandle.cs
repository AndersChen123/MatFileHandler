using System;
using System.Runtime.InteropServices;

namespace MatFileHandler.Hdf
{
    internal sealed class MemoryHandle : IDisposable
    {
        internal MemoryHandle(int sizeInBytes)
        {
            Handle = Marshal.AllocHGlobal(sizeInBytes);
        }

        internal IntPtr Handle { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}