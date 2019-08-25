using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies
{
    internal sealed class MonoCecilTypeProvider : IILAsmTypeSyntaxTypeProvider<TypeReference>
    {
        private readonly ModuleDefinition module;
        private readonly Func<string, IMetadataScope> getScopeForAssemblyName;

        public MonoCecilTypeProvider(ModuleDefinition module, Func<string, IMetadataScope> getScopeForAssemblyName)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.getScopeForAssemblyName = getScopeForAssemblyName ?? throw new ArgumentNullException(nameof(getScopeForAssemblyName));
        }

        public TypeReference GetArrayType(TypeReference elementType, int rank)
        {
            return new ArrayType(elementType, rank);
        }

        public TypeReference GetByReferenceType(TypeReference elementType)
        {
            return new ByReferenceType(elementType);
        }

        public TypeReference GetGenericInstantiation(TypeReference genericType, IReadOnlyList<TypeReference> typeArguments)
        {
            var type = new GenericInstanceType(genericType);

            foreach (var argument in typeArguments)
                type.GenericArguments.Add(argument);

            return type;
        }

        public TypeReference GetGenericMethodParameter(int index)
        {
            return CreateGenericParameter(index, GenericParameterType.Method, module);
        }

        public TypeReference GetGenericTypeParameter(int index)
        {
            return CreateGenericParameter(index, GenericParameterType.Type, module);
        }

        private static GenericParameter CreateGenericParameter(int position, GenericParameterType type, ModuleDefinition module)
        {
            return (GenericParameter)typeof(GenericParameter)
                .GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(GenericParameterType), typeof(ModuleDefinition) },
                    null)
                .Invoke(new object[] { position, type, module });
        }

        public TypeReference GetPinnedType(TypeReference elementType)
        {
            return new PinnedType(elementType);
        }

        public TypeReference GetPointerType(TypeReference elementType)
        {
            return new PointerType(elementType);
        }

        public TypeReference GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Void:
                    return module.TypeSystem.Void;
                case PrimitiveTypeCode.Boolean:
                    return module.TypeSystem.Boolean;
                case PrimitiveTypeCode.Char:
                    return module.TypeSystem.Char;
                case PrimitiveTypeCode.SByte:
                    return module.TypeSystem.SByte;
                case PrimitiveTypeCode.Byte:
                    return module.TypeSystem.Byte;
                case PrimitiveTypeCode.Int16:
                    return module.TypeSystem.Int16;
                case PrimitiveTypeCode.UInt16:
                    return module.TypeSystem.UInt16;
                case PrimitiveTypeCode.Int32:
                    return module.TypeSystem.Int32;
                case PrimitiveTypeCode.UInt32:
                    return module.TypeSystem.UInt32;
                case PrimitiveTypeCode.Int64:
                    return module.TypeSystem.Int64;
                case PrimitiveTypeCode.UInt64:
                    return module.TypeSystem.UInt64;
                case PrimitiveTypeCode.Single:
                    return module.TypeSystem.Single;
                case PrimitiveTypeCode.Double:
                    return module.TypeSystem.Double;
                case PrimitiveTypeCode.String:
                    return module.TypeSystem.String;
                case PrimitiveTypeCode.TypedReference:
                    return module.TypeSystem.TypedReference;
                case PrimitiveTypeCode.IntPtr:
                    return module.TypeSystem.IntPtr;
                case PrimitiveTypeCode.UIntPtr:
                    return module.TypeSystem.UIntPtr;
                case PrimitiveTypeCode.Object:
                    return module.TypeSystem.Object;
                default:
                    throw new NotImplementedException();
            }
        }

        public TypeReference GetTypeFromReference(bool? isValueType, string assemblyName, string namespaceName, string topLevelTypeName, IReadOnlyList<string> nestedTypeNames)
        {
            var scope = assemblyName is null ? null : getScopeForAssemblyName.Invoke(assemblyName);

            var type = new TypeReference(namespaceName, topLevelTypeName, module, scope);

            foreach (var nestedName in nestedTypeNames)
            {
                type = new TypeReference(@namespace: string.Empty, nestedName, module, scope: null)
                {
                    DeclaringType = type
                };
            }

            if (isValueType != null)
                type.IsValueType = isValueType.Value;

            return type;
        }
    }
}
