using System;
using System.Runtime.InteropServices;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct Attribute : IDisposable
    {
        public long Id { get; private set; }

        public Attribute(long locationId, string name)
        {
            Id = H5A.open_by_name(locationId, ".", name);
        }

        public void Dispose()
        {
            if (Id != -1)
            {
                H5A.close(Id);
                Id = -1;
            }
        }

        public bool ReadBool()
        {
            using (var h = new MemoryHandle(sizeof(int)))
            {
                H5A.read(Id, H5T.NATIVE_INT, h.Handle);
                var result = Marshal.ReadInt32(h.Handle);
                return result != 0;
            }
        }

        public void ReadToHandle(MemoryHandle handle, Type type)
        {
            H5A.read(Id, type.Id, handle.Handle);
        }

        public Type GetHdfType()
        {
            return new Type(H5A.get_type(Id));
        }

        public Space GetSpace()
        {
            return new Space(H5A.get_space(Id));
        }
    }
}