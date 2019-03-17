using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct Class : IEquatable<Class>
    {
        public Class(H5T.class_t c)
        {
            C = c;
        }

        public static Class String => new Class(H5T.class_t.STRING);

        public static Class Reference => new Class(H5T.class_t.REFERENCE);

        public static Class Compound => new Class(H5T.class_t.COMPOUND);

        public H5T.class_t C { get; }

        public static bool operator ==(Class one, Class other)
        {
            return one.Equals(other);
        }

        public static bool operator !=(Class one, Class other)
        {
            return !one.Equals(other);
        }

        public bool Equals(Class other)
        {
            return C == other.C;
        }

        public override bool Equals(object obj)
        {
            return obj is Class other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)C;
        }
    }
}