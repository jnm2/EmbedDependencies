using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Techsola.EmbedDependencies.Tests
{
    public static class InsertEmbeddedAssemblyResolverTests
    {
        private static MemoryStream ExecuteOnTestAssemblyStream()
        {
            var stream = new MemoryStream();

            using (var file = File.OpenRead(typeof(InsertEmbeddedAssemblyResolverTests).Assembly.Location))
                file.CopyTo(stream);

            stream.Position = 0;

            InsertEmbeddedAssemblyResolver.Execute(stream, new Dictionary<string, string>
            {
                ["Foo"] = @"Assemblies\Foo.dll"
            });

            stream.Position = 0;

            return stream;
        }

        [Test]
        public static void Injected_module_initializer_should_be_valid()
        {
            using (var stream = ExecuteOnTestAssemblyStream())
            {
                var assembly = AppDomain.CurrentDomain.Load(stream.ToArray());

                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);

                AppDomain.CurrentDomain.Load(new AssemblyName { Name = "Foo" });
            }
        }
    }
}
