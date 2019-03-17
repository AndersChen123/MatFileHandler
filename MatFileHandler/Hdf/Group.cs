using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct Group : IDisposable
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

        public Attribute GetAttribute(string name)
        {
            return new Attribute(Id, name);
        }

        public bool AttributeExists(string name)
        {
            return H5A.exists_by_name(Id, ".", name) != 0;
        }
    }
}