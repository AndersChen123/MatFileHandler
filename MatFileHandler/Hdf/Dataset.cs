// Copyright 2017-2018 Alexander Luzgarev

using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// HDF dataset.
    /// </summary>
    internal struct Dataset : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Dataset"/> struct.
        /// </summary>
        /// <param name="datasetId">Dataset id.</param>
        public Dataset(long datasetId)
        {
            Id = datasetId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dataset"/> struct.
        /// </summary>
        /// <param name="groupId">Containing group id.</param>
        /// <param name="name">Name of the dataset in the group.</param>
        public Dataset(long groupId, string name)
        {
            Id = H5D.open(groupId, name);
        }

        /// <summary>
        /// Gets dataset id.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Check if dataset attribute with the given name exists.
        /// </summary>
        /// <param name="name">Attribute name.</param>
        /// <returns>True iff dataset has an attribute with this name.</returns>
        public bool AttributeExists(string name)
        {
            return H5A.exists_by_name(Id, ".", name) != 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Id != -1)
            {
                H5D.close(Id);
                Id = -1;
            }
        }

        /// <summary>
        /// Open attribute with given name.
        /// </summary>
        /// <param name="name">Attribute name.</param>
        /// <returns>Attribute.</returns>
        public Attribute GetAttribute(string name)
        {
            return new Attribute(Id, name);
        }

        /// <summary>
        /// Get HDF space of the dataset.
        /// </summary>
        /// <returns>HDF space.</returns>
        public Space GetHdfSpace()
        {
            return new Space(H5D.get_space(Id));
        }

        /// <summary>
        /// Get HDF type of the dataset.
        /// </summary>
        /// <returns>HDF type.</returns>
        public Type GetHdfType()
        {
            return new Type(H5D.get_type(Id));
        }

        /// <summary>
        /// Get storage size of the dataset.
        /// </summary>
        /// <returns>Storage size.</returns>
        public int GetStorageSize()
        {
            return (int)H5D.get_storage_size(Id);
        }

        /// <summary>
        /// Read the contents of the dataset into the memory handle.
        /// </summary>
        /// <param name="type">HDF type of the data to read.</param>
        /// <param name="handle">Memory handle.</param>
        public void ReadToHandle(Type type, MemoryHandle handle)
        {
            H5D.read(Id, type.Id, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.Handle);
        }
    }
}