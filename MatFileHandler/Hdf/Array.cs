using System.Numerics;

namespace MatFileHandler.Hdf
{
    internal class Array : IArray
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Array"/> class.
        /// </summary>
        /// <param name="dimensions">Dimensions of the array.</param>
        protected Array(
            int[] dimensions)
        {
            Dimensions = dimensions;
        }

        /// <inheritdoc />
        public int[] Dimensions { get; }

        /// <inheritdoc />
        public int Count => Dimensions.NumberOfElements();

        /// <summary>
        /// Returns a new empty array.
        /// </summary>
        /// <returns>Empty array.</returns>
        public static Array Empty()
        {
            return new Array(System.Array.Empty<int>());
        }

        public virtual double[] ConvertToDoubleArray()
        {
            return null;
        }

        public virtual Complex[] ConvertToComplexArray()
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEmpty => Dimensions.Length == 0;
    }
}