using System;

namespace Techsola.EmbedDependencies
{
    internal readonly struct TypeSpec
    {
        public AssemblySpec Assembly { get; }
        public string Namespace { get; }
        public string Name { get; }
        public bool IsValueType { get; }

        private TypeSpec(AssemblySpec assembly, string @namespace, string name, bool isValueType)
        {
            Assembly = assembly;
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsValueType = isValueType;
        }

        public static TypeSpec ReferenceType(AssemblySpec assembly, string @namespace, string name)
        {
            return new TypeSpec(assembly, @namespace, name, isValueType: false);
        }

        public static TypeSpec ValueType(AssemblySpec assembly, string @namespace, string name)
        {
            return new TypeSpec(assembly, @namespace, name, isValueType: true);
        }
    }
}