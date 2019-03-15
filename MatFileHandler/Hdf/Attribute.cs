using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    public struct Attribute : IDisposable
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
    }
}