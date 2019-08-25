using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies
{
    internal readonly partial struct EmitHelper
    {
        private readonly ModuleDefinition module;
        private readonly IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker;

        public ILProcessor IL { get; }

        public EmitHelper(ModuleDefinition module, IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker, ILProcessor il)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.scopesByAssemblyMoniker = scopesByAssemblyMoniker ?? throw new ArgumentNullException(nameof(scopesByAssemblyMoniker));
            IL = il ?? throw new ArgumentNullException(nameof(il));
        }

        public TypeReference GetTypeReference(string serializedName)
        {
            return ILAsmSyntaxTypeNameDecoder.Decode(serializedName, new MonoCecilTypeProvider(module, GetScopeForAssemblyName));
        }

        private IMetadataScope GetScopeForAssemblyName(string assemblyName)
        {
            return scopesByAssemblyMoniker.TryGetValue(assemblyName, out var scope)
                ? scope
                : throw new InvalidOperationException($"Assembly moniker '{assemblyName}' must be added to the dictionary before parsing.");
        }
    }
}
