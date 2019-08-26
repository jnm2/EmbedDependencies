using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies
{
    internal readonly struct MetadataHelper
    {
        private readonly IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker;
        private readonly IILAsmTypeSyntaxTypeProvider<TypeReference> typeProvider;

        public MetadataHelper(
            ModuleDefinition module,
            IReadOnlyDictionary<string, IMetadataScope> scopesByAssemblyMoniker,
            IMetadataScope overrideImplicitScope = null)
        {
            if (module is null) throw new ArgumentNullException(nameof(module));
            this.scopesByAssemblyMoniker = scopesByAssemblyMoniker ?? throw new ArgumentNullException(nameof(scopesByAssemblyMoniker));

            typeProvider = null;
            typeProvider = new MonoCecilTypeProvider(module, GetScopeForAssemblyName, overrideImplicitScope);
        }

        public EmitHelper GetEmitHelper(MethodDefinition methodDefinition)
        {
            return new EmitHelper(this, methodDefinition.Body);
        }

        public TypeReference GetTypeReference(string ilasmSyntax)
        {
            return ILAsmParser.ParseType(ilasmSyntax, typeProvider);
        }

        public FieldReference GetFieldReference(string ilasmSyntax)
        {
            var field = ILAsmParser.ParseFieldReference(ilasmSyntax, typeProvider);

            return new FieldReference(field.FieldName, field.FieldType, field.DeclaringType);
        }

        public MethodReference GetMethodReference(string ilasmSyntax)
        {
            var result = ILAsmParser.ParseMethodReference(ilasmSyntax, typeProvider);

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

        public MethodDefinition DefineMethod(
            string name,
            MethodAttributes attributes,
            string returnType,
            IEnumerable<string> parameterTypes = null)
        {
            var method = new MethodDefinition(name, attributes, GetTypeReference(returnType));

            if (parameterTypes != null)
            {
                foreach (var parameterType in parameterTypes)
                {
                    method.Parameters.Add(new ParameterDefinition(GetTypeReference(parameterType)));
                }
            }

            return method;
        }

        private IMetadataScope GetScopeForAssemblyName(string assemblyName)
        {
            return scopesByAssemblyMoniker.TryGetValue(assemblyName, out var scope)
                ? scope
                : throw new InvalidOperationException($"Assembly moniker '{assemblyName}' must be added to the dictionary before parsing.");
        }
    }
}
