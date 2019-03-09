using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace MatFileHandler
{
    public class HdfCharArray : ICharArray
    {
        public HdfCharArray(int[] dimensions, string data)
        {
            Dimensions = dimensions;
            StringData = data;
        }

        public bool IsEmpty => Dimensions.Length == 0;

        public int[] Dimensions { get; }

        public int Count => Dimensions.NumberOfElements();

        public double[] ConvertToDoubleArray()
        {
            return Data.Select(Convert.ToDouble).ToArray();
        }

        public Complex[] ConvertToComplexArray()
        {
            return ConvertToDoubleArray().Select(x => new Complex(x, 0.0)).ToArray();
        }

        public char[] Data => StringData.ToCharArray();

        public char this[params int[] list]
        {
            get => StringData[Dimensions.DimFlatten(list)];
            set {
                var chars = StringData.ToCharArray();
                chars[Dimensions.DimFlatten(list)] = value;
                StringData = chars.ToString();
            }
        }

        public string String => StringData;

        private string StringData { get; set; }
    }

    internal class HdfFileReader
    {
        private long fileId;

        private List<IVariable> variables;

        internal HdfFileReader(long fileId)
        {
            this.fileId = fileId;
        }

        internal IMatFile Read()
        {
            variables = new List<IVariable>();
            H5G.info_t group_info = default(H5G.info_t);
            var result = H5G.get_info(fileId, ref group_info);
            var numberOfVariables = group_info.nlinks;

            ulong idx = 0;
            while (idx < numberOfVariables)
            {
                H5L.iterate(
                    fileId,
                    H5.index_t.NAME,
                    H5.iter_order_t.NATIVE,
                    ref idx,
                    VariableIterator,
                    IntPtr.Zero);
            }
            return new MatFile(variables);
        }

        private int VariableIterator(long group, IntPtr name, ref H5L.info_t info, IntPtr op_data)
        {
            var variableName = Marshal.PtrToStringAnsi(name);
            var object_info = default(H5O.info_t);
            H5O.get_info_by_name(group, variableName, ref object_info);
            switch (object_info.type)
            {
                case H5O.type_t.DATASET:
                    var datasetId = H5D.open(group, variableName);
                    var value = ReadDataset(datasetId);
                    variables.Add(new MatVariable(value, variableName, false));
                    break;
                case H5O.type_t.GROUP:
                    throw new NotImplementedException();
            }
            return 0;
        }

        private static string GetMatlabClassOfDataset(long datasetId)
        {
            var attributeId = H5A.open_by_name(datasetId, ".", "MATLAB_class");

            var typeId = H5A.get_type(attributeId);
            var cl = H5T.get_class(typeId);
            if (cl != H5T.class_t.STRING)
            {
                throw new NotImplementedException();
            }
            var classId = H5T.copy(H5T.C_S1);
            var typeIdSize = H5T.get_size(typeId);
            H5T.set_size(classId, typeIdSize);
            var buf = Marshal.AllocHGlobal(typeIdSize);
            H5A.read(attributeId, classId, buf);
            var matlabClassNameBytes = new byte[(int)typeIdSize];
            Marshal.Copy(buf, matlabClassNameBytes, 0, (int)typeIdSize);
            return Encoding.ASCII.GetString(matlabClassNameBytes);
        }

        private static int[] GetDimensionsOfDataset(long datasetId)
        {
            var spaceId = H5D.get_space(datasetId);
            var rank = H5S.get_simple_extent_ndims(spaceId);
            var dims = new ulong[rank];
            H5S.get_simple_extent_dims(spaceId, dims, null);
            Array.Reverse(dims);
            return dims.Select(x => (int)x).ToArray();
        }

        private static IArray ReadDataset(long datasetId)
        {
            var dims = GetDimensionsOfDataset(datasetId);

            var matlabClass = GetMatlabClassOfDataset(datasetId);

            if (matlabClass == "char")
            {
                return ReadCharArray(datasetId, dims);
            }
            throw new NotImplementedException();
        }

        private static IArray ReadCharArray(long datasetId, int[] dims)
        {
            var storageSize = (int)H5D.get_storage_size(datasetId);
            var data = new byte[storageSize];
            var dataBuffer = Marshal.AllocHGlobal(storageSize);
            H5D.read(datasetId, H5T.NATIVE_UINT16, H5S.ALL, H5S.ALL, H5P.DEFAULT, dataBuffer);
            Marshal.Copy(dataBuffer, data, 0, storageSize);
            var str = Encoding.Unicode.GetString(data);
            return new HdfCharArray(dims, str);
        }
    }
}
