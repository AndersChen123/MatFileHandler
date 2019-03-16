using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// Sparse array.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <remarks>Possible values of T: Double, Complex, Boolean.</remarks>
    internal class SparseArrayOf<T> : Array, ISparseArrayOf<T>
      where T : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SparseArrayOf{T}"/> class.
        /// </summary>
        /// <param name="dimensions">Dimensions of the array.</param>
        /// <param name="data">Array contents.</param>
        public SparseArrayOf(
            int[] dimensions,
            Dictionary<(int, int), T> data)
            : base(dimensions)
        {
            DataDictionary = data;
        }

        /// <inheritdoc />
        T[] IArrayOf<T>.Data =>
            Enumerable.Range(0, Dimensions[0] * Dimensions[1])
                .Select(i => this[i])
                .ToArray();

        /// <inheritdoc />
        public IReadOnlyDictionary<(int, int), T> Data => DataDictionary;

        private Dictionary<(int, int), T> DataDictionary { get; }

        /// <inheritdoc />
        public T this[params int[] list]
        {
            get
            {
                var rowAndColumn = GetRowAndColumn(list);
                return DataDictionary.ContainsKey(rowAndColumn) ? DataDictionary[rowAndColumn] : default(T);
            }
            set => DataDictionary[GetRowAndColumn(list)] = value;
        }

        /// <summary>
        /// Tries to convert the array to an array of Double values.
        /// </summary>
        /// <returns>Array of values of the array, converted to Double, or null if the conversion is not possible.</returns>
        public override double[] ConvertToDoubleArray()
        {
            var data = ((IArrayOf<T>)this).Data;
            return data as double[] ?? data.Select(x => Convert.ToDouble(x)).ToArray();
        }

        /// <summary>
        /// Tries to convert the array to an array of Complex values.
        /// </summary>
        /// <returns>Array of values of the array, converted to Complex, or null if the conversion is not possible.</returns>
        public override Complex[] ConvertToComplexArray()
        {
            var data = ((IArrayOf<T>)this).Data;
            return data as Complex[] ?? ConvertToDoubleArray().Select(x => new Complex(x, 0.0)).ToArray();
        }

        private (int row, int column) GetRowAndColumn(int[] indices)
        {
            switch (indices.Length)
            {
                case 1:
                    return (indices[0] % Dimensions[0], indices[0] / Dimensions[0]);
                case 2:
                    return (indices[0], indices[1]);
                default:
                    throw new NotSupportedException("Invalid index for sparse array.");
            }
        }
    }
}
