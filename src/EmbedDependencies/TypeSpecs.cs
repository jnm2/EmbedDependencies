namespace Techsola.EmbedDependencies
{
    internal static class TypeSpecs
    {
        public static TypeSpec SystemAppDomain { get; } = TypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemAppDomain, "System", "AppDomain");
        public static TypeSpec SystemResolveEventArgs { get; } = TypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemAppDomain, "System", "ResolveEventArgs");
        public static TypeSpec SystemResolveEventHandler { get; } = TypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemAppDomain, "System", "ResolveEventHandler");
        public static TypeSpec SystemString { get; } = TypeSpec.Primitive(PrimitiveType.String);
        public static TypeSpec SystemStringComparer { get; } = TypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemStringComparer, "System", "StringComparer");

        public static TypeSpec SystemCollectionsGenericDictionary(TypeSpec tKey, TypeSpec tValue)
        {
            return TypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemCollections, "System.Collections.Generic", "Dictionary`2")
                .WithGenericArguments(tKey, tValue);
        }

        public static TypeSpec SystemCollectionsGenericIEqualityComparer(TypeSpec t)
        {
            return TypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemCollections, "System.Collections.Generic", "IEqualityComparer`1")
                .WithGenericArguments(t);
        }

        public static TypeSpec SystemReflectionAssembly { get; } = TypeSpec.ReferenceType(AssemblySpec.CoreLibrary, "System.Reflection", "Assembly");
    }
}
