using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (typeSpec.IsPrimitive(out var primitiveType))
            {
                return primitiveType switch
                {
                    PrimitiveType.String => module.TypeSystem.String,
                    _ => throw new NotImplementedException()
                };
            }

            var type = new TypeReference(
                typeSpec.Namespace,
                typeSpec.Name,
                module,
                scope: this[typeSpec.Assembly],
                typeSpec.IsValueType);

            if (!typeSpec.GenericArguments.Any()) return type;

            var genericInstantiation = new GenericInstanceType(type);

            foreach (var argument in typeSpec.GenericArguments)
            {
                genericInstantiation.GenericArguments.Add(GetTypeReference(argument));
            }

            return genericInstantiation;
        }
    }
}
