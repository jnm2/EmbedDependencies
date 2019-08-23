namespace Techsola.EmbedDependencies
{
    internal sealed class NamedTypeSpec : TypeSpec
    {
        public AssemblySpec Assembly { get; }
        public string Namespace { get; }
        public string Name { get; }
        public bool IsValueType { get; }

        private NamedTypeSpec(AssemblySpec assembly, string @namespace, string name, bool isValueType)
        {
            Assembly = assembly;
            Namespace = @namespace;
            Name = name;
            IsValueType = isValueType;
        }

        public static NamedTypeSpec ReferenceType(AssemblySpec assembly, string @namespace, string name)
        {
            return new NamedTypeSpec(assembly, @namespace, name, isValueType: false);
        }

        public static NamedTypeSpec ValueType(AssemblySpec assembly, string @namespace, string name)
        {
            return new NamedTypeSpec(assembly, @namespace, name, isValueType: true);
        }

        public GenericInstantiationTypeSpec WithGenericArguments(params TypeSpec[] genericArguments)
        {
            return new GenericInstantiationTypeSpec(this, genericArguments);
        }
    }
}
