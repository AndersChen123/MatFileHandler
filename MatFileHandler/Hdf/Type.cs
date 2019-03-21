// Copyright 2017-2018 Alexander Luzgarev

using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// HDF type.
    /// </summary>
    internal struct Type
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Type"/> struct.
        /// </summary>
        /// <param name="id">Type id.</param>
        public Type(long id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets HDF string type.
        /// </summary>
        public static Type CS1 => new Type(H5T.C_S1);

        /// <summary>
        /// Gets HDF double type.
        /// </summary>
        public static Type NativeDouble => new Type(H5T.NATIVE_DOUBLE);

        /// <summary>
        /// Gets HDF float (single) type.
        /// </summary>
        public static Type NativeFloat => new Type(H5T.NATIVE_FLOAT);

        /// <summary>
        /// Gets HDF int type.
        /// </summary>
        public static Type NativeInt => new Type(H5T.NATIVE_INT);

        /// <summary>
        /// Gets HDF int16 type.
        /// </summary>
        public static Type NativeInt16 => new Type(H5T.NATIVE_INT16);

        /// <summary>
        /// Gets HDF int32 type.
        /// </summary>
        public static Type NativeInt32 => new Type(H5T.NATIVE_INT32);

        /// <summary>
        /// Gets HDF int64 type.
        /// </summary>
        public static Type NativeInt64 => new Type(H5T.NATIVE_INT64);

        /// <summary>
        /// Gets HDF int8 type.
        /// </summary>
        public static Type NativeInt8 => new Type(H5T.NATIVE_INT8);

        /// <summary>
        /// Gets HDF uint type.
        /// </summary>
        public static Type NativeUInt => new Type(H5T.NATIVE_UINT);

        /// <summary>
        /// Gets HDF uint16 type
        /// </summary>
        public static Type NativeUInt16 => new Type(H5T.NATIVE_UINT16);

        /// <summary>
        /// Gets HDF uint32 type.
        /// </summary>
        public static Type NativeUInt32 => new Type(H5T.NATIVE_UINT32);

        /// <summary>
        /// Gets HDF uint64 type.
        /// </summary>
        public static Type NativeUInt64 => new Type(H5T.NATIVE_UINT64);

        /// <summary>
        /// Gets HDF uint8 type.
        /// </summary>
        public static Type NativeUInt8 => new Type(H5T.NATIVE_UINT8);

        /// <summary>
        /// Gets HDF reference type.
        /// </summary>
        public static Type Reference => new Type(H5T.STD_REF_OBJ);

        /// <summary>
        /// Gets type id.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Create compound type of given size.
        /// </summary>
        /// <param name="size">Size of the type.</param>
        /// <returns>The created type.</returns>
        public static Type CreateCompound(int size)
        {
            return new Type(H5T.create(H5T.class_t.COMPOUND, (IntPtr)size));
        }

        /// <summary>
        /// Get class of the type.
        /// </summary>
        /// <returns>Class of the type.</returns>
        public Class GetClass()
        {
            return new Class(H5T.get_class(Id));
        }

        /// <summary>
        /// Get size of the type.
        /// </summary>
        /// <returns>Size of the type.</returns>
        public int GetSize()
        {
            return (int)H5T.get_size(Id);
        }

        /// <summary>
        /// Insert a field into the type.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="fieldType">Field type.</param>
        public void InsertField(string name, Type fieldType)
        {
            H5T.insert(Id, name, IntPtr.Zero, fieldType.Id);
        }

        /// <summary>
        /// Create type copy with same class and given size.
        /// </summary>
        /// <param name="size">New size.</param>
        /// <returns>New type.</returns>
        public Type WithSize(int size)
        {
            var classId = H5T.copy(Id);
            H5T.set_size(classId, (IntPtr)size);
            return new Type(classId);
        }
    }
}