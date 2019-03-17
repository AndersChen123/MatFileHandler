using System.Linq;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct Space
    {
        public Space(long id)
        {
            Id = id;
        }

        public long Id { get; }

        public int GetRank()
        {
            return H5S.get_simple_extent_ndims(Id);
        }

        public int[] GetDimensions()
        {
            var dims = new ulong[GetRank()];
            H5S.get_simple_extent_dims(Id, dims, null);
            System.Array.Reverse(dims);
            return dims.Select(x => (int)x).ToArray();
        }
    }
}