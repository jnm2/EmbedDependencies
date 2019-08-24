namespace Techsola.EmbedDependencies
{
    internal sealed class GenericParameterTypeSpec : TypeSpec
    {
        public GenericParameterTypeSpec(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }
}
