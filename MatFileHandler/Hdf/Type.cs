using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    internal struct Type
    {
        public Type(long id)
        {
            Id = id;
        }

        public long Id { get; }

        public Class GetClass()
        {
            return new Class(H5T.get_class(Id));
        }

        public int GetSize()
        {
            return (int)H5T.get_size(Id);
        }

        public static Type NativeInt8 => new Type(H5T.NATIVE_INT8);

        public static Type NativeUInt8 => new Type(H5T.NATIVE_UINT8);

        public static Type NativeInt16 => new Type(H5T.NATIVE_INT16);

        public static Type NativeUInt16 => new Type(H5T.NATIVE_UINT16);

        public static Type NativeInt32 => new Type(H5T.NATIVE_INT32);

        public static Type NativeUInt32 => new Type(H5T.NATIVE_UINT32);

        public static Type NativeInt64 => new Type(H5T.NATIVE_INT64);

        public static Type NativeUInt64 => new Type(H5T.NATIVE_UINT64);

        public static Type NativeFloat => new Type(H5T.NATIVE_FLOAT);

        public static Type NativeDouble => new Type(H5T.NATIVE_DOUBLE);

        public static Type NativeInt => new Type(H5T.NATIVE_INT);

        public static Type NativeUInt => new Type(H5T.NATIVE_UINT);

        public static Type CS1 => new Type(H5T.C_S1);

        public static Type Reference => new Type(H5T.STD_REF_OBJ);

        public Type WithSize(int size)
        {
            var classId = H5T.copy(Id);
            H5T.set_size(classId, (IntPtr)size);
            return new Type(classId);
        }

        public static Type CreateCompound(int size)
        {
            return new Type(H5T.create(H5T.class_t.COMPOUND, (IntPtr)size));
        }

        public void InsertField(string name, Type fieldType)
        {
            H5T.insert(Id, name, IntPtr.Zero, fieldType.Id);
        }
    }
}