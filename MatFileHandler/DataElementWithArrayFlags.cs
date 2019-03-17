namespace MatFileHandler
{
    internal class DataElementWithArrayFlags
    {
        public DataElementWithArrayFlags(DataElement element, ArrayFlags flags, string name, uint nzMax = 0)
        {
            Element = element;
            Flags = flags;
            Name = name;
            NzMax = nzMax;
        }

        public DataElementWithArrayFlags(DataElement element)
        {
            Element = element;
            Flags = default;
            Name = default;
        }

        public DataElement Element { get; }

        public ArrayFlags Flags { get; }

        public string Name { get; }

        public uint NzMax { get; }
    }
}