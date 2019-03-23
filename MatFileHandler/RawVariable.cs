// Copyright 2017-2019 Alexander Luzgarev

using System;

namespace MatFileHandler
{
    /// <summary>
    /// Raw variable read from the file.
    /// This gives a way to deal with "subsystem data" which looks like
    /// a variable and can only be detected by comparing its offset with
    /// the value stored in the file's header.
    /// </summary>
    internal class RawVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RawVariable"/> class.
        /// </summary>
        /// <param name="offset">Offset of the variable in the source file.</param>
        /// <param name="dataElement">Data element parsed from the file.</param>
        /// <param name="flags">Array flags.</param>
        /// <param name="name">Variable name.</param>
        internal RawVariable(long offset, DataElement dataElement, ArrayFlags flags, string name)
        {
            Offset = offset;
            DataElement = dataElement ?? throw new ArgumentNullException(nameof(dataElement));
            Flags = flags;
            Name = name;
        }

        /// <summary>
        /// Gets data element with the variable's contents.
        /// </summary>
        public DataElement DataElement { get; }

        /// <summary>
        /// Gets array flags.
        /// </summary>
        public ArrayFlags Flags { get; }

        /// <summary>
        /// Gets variable name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets offset of the variable in the .mat file.
        /// </summary>
        public long Offset { get; }
    }
}