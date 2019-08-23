using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies
{
    internal sealed class GenericInstantiationTypeSpec : TypeSpec
    {
        public GenericInstantiationTypeSpec(TypeSpec typeDefinition, IReadOnlyList<TypeSpec> genericArguments)
        {
            if (genericArguments is null || genericArguments.Count < 1)
                throw new ArgumentException("At least one generic argument must be specified.", nameof(genericArguments));

            TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
            GenericArguments = genericArguments;
        }

        public TypeSpec TypeDefinition { get; }
        public IReadOnlyList<TypeSpec> GenericArguments { get; }
    }
}
