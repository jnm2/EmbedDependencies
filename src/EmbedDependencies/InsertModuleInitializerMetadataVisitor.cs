using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Techsola.EmbedDependencies
{
    internal sealed class InsertModuleInitializerMetadataVisitor : MetadataVisitor
    {
        public InsertModuleInitializerMetadataVisitor(MetadataReader reader, MetadataBuilder builder)
            : base(reader, builder)
        {
        }
    }
}
