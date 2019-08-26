using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Techsola.EmbedDependencies
{
    internal static class TargetMetadataSetup
    {
        public static MetadataHelper InitializeMetadataHelper(ModuleDefinition module)
        {
            var parts = GetTargetFramework(module.Assembly)?.Split(',');
            var frameworkName = parts?[0];
            var version = parts is null ? null : Version.Parse(parts[1].Substring("Version=v".Length));

            var baselineScope = module.TypeSystem.CoreLibrary;

            if (frameworkName == ".NETCoreApp")
            {
                if (version < new Version(2, 0))
                    throw new NotSupportedException("Versions of .NET Core older than 2.0 are not supported.");

                baselineScope = GetOrAddAssemblyReference(module, "netstandard");
            }
            else if (frameworkName == ".NETStandard")
            {
                if (version < new Version(2, 0))
                    throw new NotSupportedException("Versions of .NET Standard older than 2.0 are not supported.");
            }

            return new MetadataHelper(
                module,
                new Dictionary<string, IMetadataScope>(),
                baselineScope);
        }

        private static string GetTargetFramework(AssemblyDefinition assembly)
        {
            var targetFrameworkAttribute = assembly.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");

            return (string)targetFrameworkAttribute?.ConstructorArguments.First().Value;
        }

        private static AssemblyNameReference GetOrAddAssemblyReference(ModuleDefinition module, string assemblyName)
        {
            var reference = module.AssemblyReferences.SingleOrDefault(r => r.Name == assemblyName);
            if (reference is null)
            {
                reference = new AssemblyNameReference(assemblyName, version: null);
                module.AssemblyReferences.Add(reference);
            }

            return reference;
        }
    }
}
