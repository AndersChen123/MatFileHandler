// Copyright 2017-2018 Alexander Luzgarev

namespace MatFileHandler
{
    /// <summary>
    /// Data element together with array flags, variable name, and sparse array's nzMax value.
    /// </summary>
    internal class DataElementWithMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataElementWithMetadata"/> class.
        /// </summary>
        /// <param name="element">Data element.</param>
        /// <param name="flags">Array flags.</param>
        /// <param name="name">Variable name.</param>
        /// <param name="nzMax">nzMax (for sparse arrays).</param>
        public DataElementWithMetadata(DataElement element, ArrayFlags flags, string name, uint nzMax = 0)
        {
            Element = element;
            Flags = flags;
            Name = name;
            NzMax = nzMax;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataElementWithMetadata"/> class.
        /// </summary>
        /// <param name="element">Data element.</param>
        public DataElementWithMetadata(DataElement element)
        {
            Element = element;
        }

        /// <summary>
        /// Gets data element.
        /// </summary>
        public DataElement Element { get; }

        /// <summary>
        /// Gets array flags.
        /// </summary>
        public ArrayFlags Flags { get; }

        /// <summary>
        /// Gets variable name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets nzMax (for sparse arrays).
        /// </summary>
        public uint NzMax { get; }
    }
}