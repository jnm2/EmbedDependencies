namespace Techsola.EmbedDependencies
{
    internal static class TypeSpecs
    {
        public static TypeSpec SystemAppDomain { get; } = NamedTypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemAppDomain, "System", "AppDomain");
        public static TypeSpec SystemResolveEventArgs { get; } = NamedTypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemAppDomain, "System", "ResolveEventArgs");
        public static TypeSpec SystemResolveEventHandler { get; } = NamedTypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemAppDomain, "System", "ResolveEventHandler");
        public static TypeSpec SystemString { get; } = new PrimitiveTypeSpec(PrimitiveType.String);
        public static TypeSpec SystemStringComparer { get; } = NamedTypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemStringComparer, "System", "StringComparer");

        public static TypeSpec SystemCollectionsGenericDictionary(TypeSpec tKey, TypeSpec tValue)
        {
            return NamedTypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemCollections, "System.Collections.Generic", "Dictionary`2")
                .WithGenericArguments(tKey, tValue);
        }

        public static TypeSpec SystemCollectionsGenericIEqualityComparer(TypeSpec t)
        {
            return NamedTypeSpec.ReferenceType(AssemblySpec.AssemblyContainingSystemCollections, "System.Collections.Generic", "IEqualityComparer`1")
                .WithGenericArguments(t);
        }

        public static TypeSpec SystemReflectionAssembly { get; } = NamedTypeSpec.ReferenceType(AssemblySpec.CoreLibrary, "System.Reflection", "Assembly");
    }
}
