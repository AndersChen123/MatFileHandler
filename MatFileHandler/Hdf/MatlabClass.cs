// Copyright 2017-2018 Alexander Luzgarev

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// Matlab classes as they appear in HDF files.
    /// </summary>
    internal enum MatlabClass
    {
        /// <summary>
        /// Empty array.
        /// </summary>
        MEmpty,

        /// <summary>
        /// Char array.
        /// </summary>
        MChar,

        /// <summary>
        /// Int8 array.
        /// </summary>
        MInt8,

        /// <summary>
        /// UInt8 array.
        /// </summary>
        MUInt8,

        /// <summary>
        /// Int16 array.
        /// </summary>
        MInt16,

        /// <summary>
        /// UInt16 array.
        /// </summary>
        MUInt16,

        /// <summary>
        /// Int32 array.
        /// </summary>
        MInt32,

        /// <summary>
        /// UInt32 array.
        /// </summary>
        MUInt32,

        /// <summary>
        /// Int64 array.
        /// </summary>
        MInt64,

        /// <summary>
        /// UInt64 array.
        /// </summary>
        MUInt64,

        /// <summary>
        /// Single-precision floating point array.
        /// </summary>
        MSingle,

        /// <summary>
        /// Double-precision floating point array.
        /// </summary>
        MDouble,

        /// <summary>
        /// Cell array.
        /// </summary>
        MCell,

        /// <summary>
        /// Logical array.
        /// </summary>
        MLogical,
    }
}