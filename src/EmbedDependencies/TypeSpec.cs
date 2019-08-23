using System;
using System.Collections.Generic;
using System.Linq;

namespace Techsola.EmbedDependencies
{
    internal readonly struct TypeSpec
    {
        private readonly PrimitiveType? primitiveType;
        public AssemblySpec Assembly { get; }
        public string Namespace { get; }
        public string Name { get; }
        public bool IsValueType { get; }
        public IReadOnlyList<TypeSpec> GenericArguments { get; }

        private TypeSpec(PrimitiveType? primitiveType, AssemblySpec assembly, string @namespace, string name, bool isValueType, IReadOnlyList<TypeSpec> genericArguments)
        {
            this.primitiveType = primitiveType;
            Assembly = assembly;
            Namespace = @namespace;
            Name = name;
            IsValueType = isValueType;
            GenericArguments = genericArguments;
        }

        public static TypeSpec Primitive(PrimitiveType primitiveType)
        {
            return new TypeSpec(primitiveType, assembly: default, @namespace: default, name: default, isValueType: default, genericArguments: default);
        }

        public bool IsPrimitive(out PrimitiveType primitiveType)
        {
            primitiveType = this.primitiveType ?? default;
            return this.primitiveType is { };
        }

        public static TypeSpec ReferenceType(AssemblySpec assembly, string @namespace, string name)
        {
            return new TypeSpec(primitiveType: null, assembly, @namespace, name, isValueType: false, genericArguments: Array.Empty<TypeSpec>());
        }

        public static TypeSpec ValueType(AssemblySpec assembly, string @namespace, string name)
        {
            return new TypeSpec(primitiveType: null, assembly, @namespace, name, isValueType: true, genericArguments: Array.Empty<TypeSpec>());
        }

        public TypeSpec WithGenericArguments(params TypeSpec[] genericArguments)
        {
            if (primitiveType is { })
                throw new InvalidOperationException("Primitive types cannot be generic.");

            return new TypeSpec(primitiveType: null, Assembly, Namespace, Name, IsValueType, genericArguments?.ToArray() ?? Array.Empty<TypeSpec>());
        }
    }
}
