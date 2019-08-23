namespace Techsola.EmbedDependencies
{
    internal sealed class PrimitiveTypeSpec : TypeSpec
    {
        public PrimitiveTypeSpec(PrimitiveType primitiveType)
        {
            PrimitiveType = primitiveType;
        }

        public PrimitiveType PrimitiveType { get; }
    }
}
