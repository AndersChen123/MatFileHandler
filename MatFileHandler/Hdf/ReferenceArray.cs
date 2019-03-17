using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct ReferenceArray : IDisposable, IEnumerable<Dataset>
    {
        public Dataset Dataset { get; }

        public int Size { get; }

        public MemoryHandle Buf { get; }

        public Dataset[] References { get; }

        public ReferenceArray(Dataset dataset, int size)
        {
            Dataset = dataset;
            Size = size;
            Buf = new MemoryHandle(Marshal.SizeOf(default(IntPtr)) * size);
            Dataset.ReadToHandle(Type.Reference, Buf);
            References = new Dataset[size];
            for (var i = 0; i < size; i++)
            {
                References[i] =
                    new Dataset(H5R.dereference(
                        dataset.Id,
                        H5P.DEFAULT,
                        H5R.type_t.OBJECT,
                        Buf.Handle + (i * Marshal.SizeOf(default(IntPtr)))));
            }
        }

        public void Dispose()
        {
            Buf?.Dispose();
            if (!(References is null))
            {
                foreach (var reference in References)
                {
                    reference.Dispose();
                }
            }
        }

        public IEnumerator<Dataset> GetEnumerator()
        {
            return ((IEnumerable<Dataset>)References).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return References.GetEnumerator();
        }
    }
}
