using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public EmitHelper GetEmitHelper(MethodDefinition methodDefinition)
        {
            return new EmitHelper(this, methodDefinition.Body.GetILProcessor());
        }

        public TypeReference GetTypeReference(string ilasmSyntax)
        {
            return ILAsmParser.ParseType(ilasmSyntax, new MonoCecilTypeProvider(module, GetScopeForAssemblyName));
        }

        public FieldReference GetFieldReference(string ilasmSyntax)
        {
            var field = ILAsmParser.ParseFieldReference(ilasmSyntax, new MonoCecilTypeProvider(module, GetScopeForAssemblyName));

            return new FieldReference(field.FieldName, field.FieldType, field.DeclaringType);
        }

        public MethodReference GetMethodReference(string ilasmSyntax)
        {
            var result = ILAsmParser.ParseMethodReference(ilasmSyntax, new MonoCecilTypeProvider(module, GetScopeForAssemblyName));

            var method = new MethodReference(result.MethodName, result.ReturnType, result.DeclaringType)
            {
                HasThis = result.Instance,
                ExplicitThis = result.InstanceExplicit
            };

            foreach (var parameter in result.Parameters)
                method.Parameters.Add(new ParameterDefinition(parameter));

            if (!result.GenericArguments.Any()) return method;

            var genericInstantiation = new GenericInstanceMethod(method);

            foreach (var argument in result.GenericArguments)
                genericInstantiation.GenericArguments.Add(argument);

            return genericInstantiation;
        }

        private IMetadataScope GetScopeForAssemblyName(string assemblyName)
        {
            return scopesByAssemblyMoniker.TryGetValue(assemblyName, out var scope)
                ? scope
                : throw new InvalidOperationException($"Assembly moniker '{assemblyName}' must be added to the dictionary before parsing.");
        }
    }
}
