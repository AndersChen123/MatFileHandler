// Copyright 2017-2019 Alexander Luzgarev

using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// Hdf group.
    /// </summary>
    internal struct Group : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> struct.
        /// </summary>
        /// <param name="groupId">Containing group id.</param>
        /// <param name="name">Name of the subgroup in the containing group.</param>
        public Group(long groupId, string name)
        {
            Id = H5G.open(groupId, name);
        }

        /// <summary>
        /// Gets group id.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Check if group attribute with the given name exists.
        /// </summary>
        /// <param name="name">Attribute name.</param>
        /// <returns>True iff group has an attribute with this name.</returns>
        public bool AttributeExists(string name)
        {
            return H5A.exists_by_name(Id, ".", name) != 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Id != -1)
            {
                H5G.close(Id);
                Id = -1;
            }
        }

        /// <summary>
        /// Get group attribute.
        /// </summary>
        /// <param name="name">Attribute name.</param>
        /// <returns>Attribute.</returns>
        public Attribute GetAttribute(string name)
        {
            return new Attribute(Id, name);
        }
    }
}