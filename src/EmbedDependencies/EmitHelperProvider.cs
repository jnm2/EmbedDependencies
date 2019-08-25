using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies
{
    internal readonly struct EmitHelperProvider
    {
        private readonly ModuleDefinition module;
        private readonly IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker;

        public EmitHelperProvider(ModuleDefinition module, IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.scopesByAssemblyMoniker = scopesByAssemblyMoniker ?? throw new ArgumentNullException(nameof(scopesByAssemblyMoniker));
        }

        public EmitHelper this[MethodDefinition methodDefinition]
        {
            get => new EmitHelper(module, scopesByAssemblyMoniker, methodDefinition.Body.GetILProcessor());
        }
    }
}
