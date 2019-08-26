using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Techsola.EmbedDependencies
{
    // Template: https://sharplab.io/#v2:EYLgxg9gTgpgtADwGwBYA0AXEBLANgHwAEAmARgFgAoQgBgAJDSUBuK2h0gOgEkB5V6vUacASjABmuGGAzYIAOwHthIgK7zZAWxicAMhACGAExhQBbUkgbE6AWQhHVUqgG8qdD3QD0Xut3F0nAByAKIAKnQAwtAw7p4ADlDYAG4GGDAcVgCCAM45MJrAuACedAAiEgZOGAD6YjkQuMnY8gDmABS5+YUl+sbRGjAIGHSQg8NodF0FRcVBBtp08gswAJRxHm6UnjsM9O2pUHQ5GLALdAC8dGFQxQDiMBj1EKpQYDDTPcUAyqcwC+1ltpVuttrtNhtwZ5sAF2iczpo6NgcksnLhVgwAOyo3C4ARQ3aEbFjdLDPSGIwAMSgEE0vwRcL+C1W+KhAF9IRywR5IT46LwMAALUwAd2RsW5dESKTSGUY2TyMxK/Pkn1mzyaMHaEGAACtpCN8vITFBJhrkjAQhaNFkoK0UWtIVsCewDgYjvD/oirjd7o9nq93mqSvSvYCYCKpoqvvNtO0dLG1iDITtnQTobDPedkTj0Vjc6z03QU1DXYc6MBVOJxKZLksI3YCtAfkzNO0wAZ4gYwNgMMUQKNhWAANYwIztdotDCrLOaPQwNpCkGgosQyWr46tzjReLFMIQdqV6umFkl8EASCJUe6s3JxkPVZrUE4+9tUAMxXaIML6a5BLZnicpCkLSqk6SZHQobnL6DxPDADSBh80azFBbbBnMKx0AYyElImK6eGm4JXiEhRjiYRgBm8MCJjkABCxToYmL63LBABqBi4KoWrYTeuErMEKyTC8IzlrACFUXhZ67AA/HQfbxDAEDiO09iOFIqycOhnCwbYBjyDC8FweJ7yoe0YkvBJKz4UWA7yGiP5/oBkqgbKEFnEYCjKmU2AyHIyy3AAPIwNCTMFAB8dAkcAZFjpR7w0fRjErFJVzyA23m+Qo7rFEFpAhRwNBhe09ItK00SaF2sDPrwUBGC0HHcK08gxJE2GOuuBFSTsADaABEYSGehvUALp1oQhC9cG2DwQAOgNJxaUYuK9VJbICFyQA=

    public sealed class InsertEmbeddedAssemblyResolver : Task
    {
        [Required]
        public ITaskItem TargetAssembly { get; set; }

        public override bool Execute()
        {
            using var stream = new FileStream(TargetAssembly.ItemSpec, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Execute(stream);
            return true;
        }

        public static void Execute(Stream stream)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(stream, new ReaderParameters { ReadSymbols = false });

            var embeddedResourceNamesByAssemblyName = new Dictionary<string, string>
            {
                ["TestAssembly"] = @"Assemblies\TestAssembly.dll",
                ["Foo"] = @"Assemblies\Foo.dll"
            };

            var module = assemblyDefinition.MainModule;
            var helper = InitializeMetadataHelper(module);

            var moduleType = module.GetType("<Module>");

            var dictionaryField = CreateDictionaryField(helper);
            moduleType.Fields.Add(dictionaryField);

            var getResourceAssemblyStreamOrNullMethod = CreateGetResourceAssemblyStreamOrNullMethod(dictionaryField, moduleType, helper);
            moduleType.Methods.Add(getResourceAssemblyStreamOrNullMethod);

            var assemblyResolveHandler = CreateAppDomainAssemblyResolveHandler(helper);
            moduleType.Methods.Add(assemblyResolveHandler);

            var moduleInitializer = CreateModuleInitializer(dictionaryField, embeddedResourceNamesByAssemblyName, assemblyResolveHandler, helper);
            moduleType.Methods.Add(moduleInitializer);

            assemblyDefinition.Write(stream);
        }

        private static MetadataHelper InitializeMetadataHelper(ModuleDefinition module)
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
                new Dictionary<string, IMetadataScope> { ["_"] = baselineScope });
        }

        private static MethodDefinition CreateModuleInitializer(
            FieldReference dictionaryField,
            IReadOnlyDictionary<string, string> embeddedResourceNamesByAssemblyName,
            MethodReference assemblyResolveHandler,
            MetadataHelper helper)
        {
            var moduleInitializer = new MethodDefinition(
                ".cctor",
                MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                helper.GetTypeReference("void"));

            var emit = helper.GetEmitHelper(moduleInitializer);

            GenerateDictionaryInitializationIL(emit, dictionaryField, embeddedResourceNamesByAssemblyName);

            GenerateAppDomainModuleInitializerIL(emit, assemblyResolveHandler);

            return moduleInitializer;
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

        private static string GetTargetFramework(AssemblyDefinition assembly)
        {
            var targetFrameworkAttribute = assembly.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");

            return (string)targetFrameworkAttribute?.ConstructorArguments.First().Value;
        }

        private static void GenerateDictionaryInitializationIL(
            EmitHelper emit,
            FieldReference dictionaryField,
            IReadOnlyDictionary<string, string> embeddedResourceNamesByAssemblyName)
        {
            emit.Call("class [_]System.StringComparer class [_]System.StringComparer::get_OrdinalIgnoreCase()");
            emit.Newobj(@"
                instance void class [_]System.Collections.Generic.Dictionary`2<string, string>::.ctor(
                    class [_]System.Collections.Generic.IEqualityComparer`1<!0>)");

            foreach (var entry in embeddedResourceNamesByAssemblyName)
            {
                emit.Dup();
                emit.Ldstr(entry.Key);
                emit.Ldstr(entry.Value);
                emit.Callvirt("instance void class [_]System.Collections.Generic.Dictionary`2<string, string>::set_Item(!0, !1)");
            }

            emit.Stsfld(dictionaryField);
        }

        private static void GenerateAppDomainModuleInitializerIL(EmitHelper emit, MethodReference assemblyResolveHandler)
        {
            emit.Call("class [_]System.AppDomain class [_]System.AppDomain::get_CurrentDomain()");
            emit.Ldnull();
            emit.Ldftn(assemblyResolveHandler);
            emit.Newobj("instance void class [_]System.ResolveEventHandler::.ctor(object, native int)");
            emit.Callvirt("instance void class [_]System.AppDomain::add_AssemblyResolve(class [_]System.ResolveEventHandler)");
            emit.Ret();
        }

        private static FieldDefinition CreateDictionaryField(MetadataHelper helper)
        {
            return new FieldDefinition(
                "EmbeddedResourceNamesByAssemblyName",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
                helper.GetTypeReference("class [_]System.Collections.Generic.Dictionary`2<string, string>"));
        }

        private static MethodDefinition CreateAppDomainAssemblyResolveHandler(MetadataHelper helper)
        {
            var handler = new MethodDefinition(
                "OnAssemblyResolve",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType: helper.GetTypeReference("class System.Reflection.Assembly"))
            {
                Parameters =
                {
                    new ParameterDefinition(helper.GetTypeReference("object")),
                    new ParameterDefinition(helper.GetTypeReference("class [_]System.ResolveEventArgs"))
                }
            };

            var emit = helper.GetEmitHelper(handler);

            // TODO

            emit.Ldnull();
            emit.Ret();

            return handler;
        }

        private static MethodDefinition CreateGetResourceAssemblyStreamOrNullMethod(FieldReference dictionaryField, TypeReference moduleType, MetadataHelper helper)
        {
            var method = new MethodDefinition(
                "GetResourceAssemblyStreamOrNull",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType: helper.GetTypeReference("class [_]System.IO.Stream"))
            {
                Parameters = { new ParameterDefinition(helper.GetTypeReference("class System.Reflection.AssemblyName")) }
            };

            var emit = helper.GetEmitHelper(method);

            emit.Ldsfld(dictionaryField);
            emit.Ldarg(0);
            emit.Callvirt("instance string class System.Reflection.AssemblyName::get_Name()");

            var resourceNameVariable = emit.CreateLocal("string");
            emit.Ldloca(resourceNameVariable);
            emit.Callvirt("instance bool class [_]System.Collections.Generic.Dictionary`2<string, string>::TryGetValue(!0, !1&)");

            var successBranch = emit.IL.Create(OpCodes.Ldtoken, moduleType);

            emit.Brtrue_S(successBranch);
            emit.Ldnull();
            emit.Ret();

            emit.IL.Append(successBranch);

            emit.Call("class System.Type System.Type::GetTypeFromHandle(valuetype System.RuntimeTypeHandle)");
            emit.Callvirt("instance class System.Reflection.Assembly System.Type::get_Assembly()");
            emit.Ldloc(resourceNameVariable);
            emit.Callvirt("instance class System.IO.Stream System.Reflection.Assembly::GetManifestResourceStream(string)");
            emit.Ret();

            return method;
        }
    }
}
