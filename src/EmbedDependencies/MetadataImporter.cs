using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies
{
    internal sealed class MetadataImporter
    {
        private readonly ModuleDefinition module;
        private readonly IReadOnlyDictionary<AssemblySpec, IMetadataScope> scopesByAssemblySpec;

        public MetadataImporter(ModuleDefinition module, IReadOnlyDictionary<AssemblySpec, IMetadataScope> scopesByAssemblySpec)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.scopesByAssemblySpec = scopesByAssemblySpec ?? throw new ArgumentNullException(nameof(scopesByAssemblySpec));
        }

        public IMetadataScope this[AssemblySpec assemblySpec] => scopesByAssemblySpec[assemblySpec];

        public TypeReference this[TypeSpec typeSpec] => module.ImportReference(GetTypeReference(typeSpec));

        private TypeReference GetTypeReference(TypeSpec typeSpec)
        {
            switch (typeSpec)
            {
                case PrimitiveTypeSpec spec:
                    return spec.PrimitiveType switch
                    {
                        PrimitiveType.String => module.TypeSystem.String,
                        _ => throw new NotImplementedException()
                    };

                case NamedTypeSpec spec:
                    return new TypeReference(
                        spec.Namespace,
                        spec.Name,
                        module,
                        scope: this[spec.Assembly],
                        spec.IsValueType);

                case GenericInstantiationTypeSpec spec:
                    var genericInstantiation = new GenericInstanceType(GetTypeReference(spec.TypeDefinition));

                    foreach (var argument in spec.GenericArguments)
                        genericInstantiation.GenericArguments.Add(GetTypeReference(argument));

                    return genericInstantiation;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
