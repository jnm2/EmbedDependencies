using Mono.Cecil.Cil;

namespace Techsola.EmbedDependencies
{
    internal readonly partial struct EmitHelper
    {
        public EmitHelper(MetadataHelper metadata, ILProcessor il)
        {
            Metadata = metadata;
            IL = il ?? throw new System.ArgumentNullException(nameof(il));
        }

        public MetadataHelper Metadata { get; }
        public ILProcessor IL { get; }
    }
}
