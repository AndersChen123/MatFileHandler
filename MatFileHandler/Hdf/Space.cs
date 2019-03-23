// Copyright 2017-2019 Alexander Luzgarev

using System.Linq;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// HDF space.
    /// </summary>
    internal struct Space
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Space"/> struct.
        /// </summary>
        /// <param name="id">Space id.</param>
        public Space(long id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets space id.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Get space rank.
        /// </summary>
        /// <returns>Space rank.</returns>
        public int GetRank()
        {
            return H5S.get_simple_extent_ndims(Id);
        }

        /// <summary>
        /// Get dimensions of the space.
        /// </summary>
        /// <returns>Space dimensions.</returns>
        public int[] GetDimensions()
        {
            var dims = new ulong[GetRank()];
            H5S.get_simple_extent_dims(Id, dims, null);
            System.Array.Reverse(dims);
            return dims.Select(x => (int)x).ToArray();
        }
    }
}