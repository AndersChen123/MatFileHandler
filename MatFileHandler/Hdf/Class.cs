// Copyright 2017-2019 Alexander Luzgarev

using System;
using HDF.PInvoke;

namespace MatFileHandler.Hdf
{
    /// <summary>
    /// HDF class.
    /// </summary>
    internal struct Class : IEquatable<Class>
    {
        /// <summary>
        /// Compound class.
        /// </summary>
        public static readonly Class Compound = new Class(H5T.class_t.COMPOUND);

        /// <summary>
        /// Reference class.
        /// </summary>
        public static readonly Class Reference = new Class(H5T.class_t.REFERENCE);

        /// <summary>
        /// String class.
        /// </summary>
        public static readonly Class String = new Class(H5T.class_t.STRING);

        private readonly H5T.class_t classT;

        /// <summary>
        /// Initializes a new instance of the <see cref="Class"/> struct.
        /// </summary>
        /// <param name="classT">HDF class_t.</param>
        public Class(H5T.class_t classT)
        {
            this.classT = classT;
        }

        public static bool operator ==(Class one, Class other)
        {
            return one.Equals(other);
        }

        public static bool operator !=(Class one, Class other)
        {
            return !one.Equals(other);
        }

        /// <summary>
        /// Check if the class is equal to the other class.
        /// </summary>
        /// <param name="other">Other class.</param>
        /// <returns>True iff the classes are equal.</returns>
        public bool Equals(Class other)
        {
            return classT == other.classT;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Class other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)classT;
        }
    }
}