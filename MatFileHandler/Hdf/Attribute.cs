// Copyright 2017-2018 Alexander Luzgarev

using System;
using System.Runtime.InteropServices;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// Wrapper for HDF attribute.
    /// </summary>
    internal struct Attribute : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Attribute"/> struct.
        /// </summary>
        /// <param name="locationId">Containing location id.</param>
        /// <param name="name">Attribute name.</param>
        public Attribute(long locationId, string name)
        {
            Id = H5A.open_by_name(locationId, ".", name);
        }

        /// <summary>
        /// Gets attribute id.
        /// </summary>
        public long Id { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Id != -1)
            {
                H5A.close(Id);
                Id = -1;
            }
        }

        /// <summary>
        /// Get HDF type of the attribute.
        /// </summary>
        /// <returns>HDF type.</returns>
        public Type GetHdfType()
        {
            return new Type(H5A.get_type(Id));
        }

        /// <summary>
        /// Get HDF space of the attribute.
        /// </summary>
        /// <returns>HDF space.</returns>
        public Space GetSpace()
        {
            return new Space(H5A.get_space(Id));
        }

        /// <summary>
        /// Read attribute value as boolean.
        /// </summary>
        /// <returns>Attribute value.</returns>
        public bool ReadBool()
        {
            using (var h = new MemoryHandle(sizeof(int)))
            {
                H5A.read(Id, H5T.NATIVE_INT, h.Handle);
                var result = Marshal.ReadInt32(h.Handle);
                return result != 0;
            }
        }

        /// <summary>
        /// Read attribute value to the provided memory handle.
        /// </summary>
        /// <param name="handle">Target memory handle.</param>
        /// <param name="type">HDF type to read from the attribute.</param>
        public void ReadToHandle(MemoryHandle handle, Type type)
        {
            H5A.read(Id, type.Id, handle.Handle);
        }
    }
}