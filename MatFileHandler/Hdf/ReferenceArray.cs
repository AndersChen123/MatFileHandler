// Copyright 2017-2019 Alexander Luzgarev

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// Array of HDF references stored in an HDF dataset.
    /// </summary>
    internal struct ReferenceArray : IDisposable, IEnumerable<Dataset>
    {
        private readonly Dataset[] references;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceArray"/> struct.
        /// </summary>
        /// <param name="dataset">Containing dataset.</param>
        /// <param name="size">Array size.</param>
        public ReferenceArray(Dataset dataset, int size)
        {
            Dataset = dataset;
            Size = size;
            Buf = new MemoryHandle(Marshal.SizeOf(default(IntPtr)) * size);
            Dataset.ReadToHandle(Type.Reference, Buf);
            references = new Dataset[size];
            for (var i = 0; i < size; i++)
            {
                references[i] =
                    new Dataset(H5R.dereference(
                        dataset.Id,
                        H5P.DEFAULT,
                        H5R.type_t.OBJECT,
                        Buf.Handle + (i * Marshal.SizeOf(default(IntPtr)))));
            }
        }

        /// <summary>
        /// Gets containing dataset.
        /// </summary>
        public Dataset Dataset { get; }

        /// <summary>
        /// Gets references.
        /// </summary>
        public IReadOnlyList<Dataset> References => references;

        /// <summary>
        /// Gets array size.
        /// </summary>
        public int Size { get; }

        private MemoryHandle Buf { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Buf?.Dispose();
            if (References is null)
            {
                return;
            }

            foreach (var reference in References)
            {
                reference.Dispose();
            }
        }

        /// <inheritdoc />
        public IEnumerator<Dataset> GetEnumerator()
        {
            return ((IEnumerable<Dataset>)References).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return References.GetEnumerator();
        }
    }
}