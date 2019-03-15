using System;
using System.Linq;
using System.Numerics;

namespace MatFileHandler.Hdf
{
    internal class CharArray : Array, ICharArray
    {
        public CharArray(int[] dimensions, string data)
            : base(dimensions)
        {
            StringData = data;
        }

        public override double[] ConvertToDoubleArray()
        {
            return Data.Select(Convert.ToDouble).ToArray();
        }

        public override Complex[] ConvertToComplexArray()
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
}