// Copyright 2017-2019 Alexander Luzgarev

using System.Numerics;

namespace MatFileHandler
{
    /// <summary>
    /// Base class for various Matlab arrays.
    /// </summary>
    internal class MatArray : DataElement, IArray
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatArray"/> class.
        /// </summary>
        /// <param name="dimensions">Dimensions of the array.</param>
        protected MatArray(int[] dimensions)
        {
            Dimensions = dimensions;
        }

        /// <inheritdoc />
        public int[] Dimensions { get; }

        /// <inheritdoc />
        public int Count => Dimensions.NumberOfElements();

        /// <inheritdoc />
        public bool IsEmpty => Dimensions.Length == 0;

        /// <summary>
        /// Returns a new empty array.
        /// </summary>
        /// <returns>Empty array.</returns>
        public static MatArray Empty()
        {
            return new MatArray(new int[] { });
        }

        /// <inheritdoc />
        public virtual double[] ConvertToDoubleArray()
        {
            return null;
        }

        /// <inheritdoc />
        public virtual Complex[] ConvertToComplexArray()
        {
            return null;
        }
    }
}