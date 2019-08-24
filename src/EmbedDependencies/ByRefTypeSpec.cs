using System;

namespace Techsola.EmbedDependencies
{
    internal sealed class ByRefTypeSpec : TypeSpec
    {
        public ByRefTypeSpec(TypeSpec elementType)
        {
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }

        public TypeSpec ElementType { get; }
    }
}
