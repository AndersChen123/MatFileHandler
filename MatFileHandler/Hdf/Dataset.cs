using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct Dataset : IDisposable
    {
        public long Id { get; private set; }

        public Dataset(long datasetId)
        {
            Id = datasetId;
        }

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

        public Attribute GetAttribute(string name)
        {
            return new Attribute(Id, name);
        }

        public bool AttributeExists(string name)
        {
            return H5A.exists_by_name(Id, ".", name) != 0;
        }

        public Type GetHdfType()
        {
            return new Type(H5D.get_type(Id));
        }

        public int GetStorageSize()
        {
            return (int)H5D.get_storage_size(Id);
        }

        public Space GetHdfSpace()
        {
            return new Space(H5D.get_space(Id));
        }

        public void ReadToHandle(Type type, MemoryHandle handle)
        {
            H5D.read(Id, type.Id, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.Handle);
        }
    }
}