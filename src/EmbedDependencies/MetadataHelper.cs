using Mono.Cecil;
using System;
using System.Collections.Generic;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies
{
    internal readonly struct MetadataHelper
    {
        private readonly ModuleDefinition module;
        private readonly IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker;

        public MetadataHelper(ModuleDefinition module, IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.scopesByAssemblyMoniker = scopesByAssemblyMoniker ?? throw new ArgumentNullException(nameof(scopesByAssemblyMoniker));
        }

        public TypeReference GetTypeReference(string ilasmSyntax)
        {
            return ILAsmParser.ParseType(ilasmSyntax, new MonoCecilTypeProvider(module, GetScopeForAssemblyName));
        }

        private IMetadataScope GetScopeForAssemblyName(string assemblyName)
        {
            return scopesByAssemblyMoniker.TryGetValue(assemblyName, out var scope)
                ? scope
                : throw new InvalidOperationException($"Assembly moniker '{assemblyName}' must be added to the dictionary before parsing.");
        }

        public EmitHelper this[MethodDefinition methodDefinition]
        {
            get => new EmitHelper(this, methodDefinition.Body.GetILProcessor());
        }
    }
}
