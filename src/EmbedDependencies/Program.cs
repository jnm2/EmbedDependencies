using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Techsola.EmbedDependencies
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 4 || !"injectresolver".Equals(args[0], StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Usage: injectresolver <file-path> <resource-name-prefix> <assembly-name> [ <assembly-name> ... ]");
                return 1;
            }

            var filePath = args[1];
            var resourceNamePrefix = args[2];

            var assemblyNames = ImmutableHashSet.CreateBuilder<string>();

            foreach (var arg in args.Skip(3))
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    Console.WriteLine("Assembly names must not be empty or whitespace.");
                    return 1;
                }

                if (!assemblyNames.Add(arg))
                {
                    Console.WriteLine("The same assembly name was specified twice.");
                    return 1;
                }
            }

            InjectResolver(filePath, resourceNamePrefix, assemblyNames.ToImmutable());
            return 0;
        }

        public static void InjectResolver(string filePath, string resourceNamePrefix, ImmutableHashSet<string> assemblyNames)
        {
            var embeddedResourceNamesByAssemblyName = assemblyNames.ToImmutableDictionary(name => name, name => resourceNamePrefix + name);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            AssemblyRewriters.InsertEmbeddedAssemblyResolver(
                stream,
                embeddedResourceNamesByAssemblyName);
        }
    }
}
