using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    public struct Dataset : IDisposable
    {
        public long Id { get; private set; }

        public Dataset(long groupId, string name)
        {
            Id = H5D.open(groupId, name);
        }

        public void Dispose()
        {
            if (Id != -1)
            {
                H5D.close(Id);
                Id = -1;
            }
        }
    }
}