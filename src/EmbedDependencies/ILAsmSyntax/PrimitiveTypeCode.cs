namespace Techsola.EmbedDependencies
{
    public enum PrimitiveTypeCode : byte
    {
        Void = 0x01,
        Boolean = 0x02,
        Char = 0x03,
        SByte = 0x04,
        Byte = 0x05,
        Int16 = 0x06,
        UInt16 = 0x07,
        Int32 = 0x08,
        UInt32 = 0x09,
        Int64 = 0x0A,
        UInt64 = 0x0B,
        Single = 0x0C,
        Double = 0x0D,
        String = 0x0E,
        TypedReference = 0x16,
        IntPtr = 0x18,
        UIntPtr = 0x19,
        Object = 0x1C
    }
}
