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

        public IMetadataScope this[AssemblySpec assemblySpec]
        {
            get => scopesByAssemblySpec[assemblySpec];
        }

        public TypeReference this[TypeSpec typeSpec]
        {
            get => module.ImportReference(new TypeReference(
                typeSpec.Namespace,
                typeSpec.Name,
                module,
                scope: this[typeSpec.Assembly],
                typeSpec.IsValueType));
        }
    }
}
