using System.Collections.Generic;
using System.Linq;

namespace MatFileHandler.Hdf
{
    internal class CellArray : Array, ICellArray
    {
        public CellArray(int[] dimensions, IEnumerable<IArray> elements)
            : base(dimensions)
        {
            Data = elements.ToArray();
        }

        /// <inheritdoc />
        public IArray[] Data { get; }

        /// <inheritdoc />
        public IArray this[params int[] indices]
        {
            get => Data[Dimensions.DimFlatten(indices)];
            set => Data[Dimensions.DimFlatten(indices)] = value;
        }
    }
}