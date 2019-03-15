using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    public struct Group : IDisposable
    {
        public long Id { get; private set; }

        public Group(long groupId, string name)
        {
            Id = H5G.open(groupId, name);
        }

        public void Dispose()
        {
            if (Id != -1)
            {
                H5G.close(Id);
                Id = -1;
            }
        }
    }
}