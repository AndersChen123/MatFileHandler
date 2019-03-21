// Copyright 2017-2018 Alexander Luzgarev

using System;
using System.Runtime.InteropServices;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// Wrapper around IntPtr to array in unmanaged memory.
    /// </summary>
    internal sealed class MemoryHandle : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryHandle"/> class.
        /// </summary>
        /// <param name="sizeInBytes">Size of the memory to be allocated.</param>
        internal MemoryHandle(int sizeInBytes)
        {
            Handle = Marshal.AllocHGlobal(sizeInBytes);
        }

        /// <summary>
        /// Gets wrapped IntPtr.
        /// </summary>
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