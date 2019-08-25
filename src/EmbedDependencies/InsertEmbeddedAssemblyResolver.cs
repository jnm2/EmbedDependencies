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

            CreateModuleInitializer(assemblyDefinition.MainModule, embeddedResourceNamesByAssemblyName);

            assemblyDefinition.Write(stream);
        }

        private static void CreateModuleInitializer(ModuleDefinition module, IReadOnlyDictionary<string, string> embeddedResourceNamesByAssemblyName)
        {
            var moduleType = module.GetType("<Module>");

            var moduleInitializer = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            var il = moduleInitializer.Body.GetILProcessor();

            var parts = GetTargetFramework(module.Assembly)?.Split(',');
            var frameworkName = parts?[0];
            var version = parts is null ? null : Version.Parse(parts[1].Substring("Version=v".Length));

            var scopesByAssemblyMoniker = AssemblyMonikers.GetAll()
                .ToDictionary(m => m, m => module.TypeSystem.CoreLibrary);

            if (frameworkName == ".NETCoreApp")
            {
                if (version < new Version(2, 0))
                    throw new NotSupportedException("Versions of .NET Core older than 2.0 are not supported.");

                var runtimeExtensions = GetOrAddAssemblyReference(module, "System.Runtime.Extensions");
                var collections = GetOrAddAssemblyReference(module, "System.Collections");

                scopesByAssemblyMoniker[AssemblyMonikers.HasAppDomain] = runtimeExtensions;
                scopesByAssemblyMoniker[AssemblyMonikers.HasStringComparer] = runtimeExtensions;
                scopesByAssemblyMoniker[AssemblyMonikers.HasCollections] = collections;
            }
            else if (frameworkName == ".NETStandard")
            {
                if (version < new Version(2, 0))
                    throw new NotSupportedException("Versions of .NET Standard older than 2.0 are not supported.");
            }

            var helper = new MetadataHelper(module, scopesByAssemblyMoniker);
            var dictionaryField = CreateDictionaryField(moduleType, helper);
            GenerateDictionaryInitializationIL(il, dictionaryField, embeddedResourceNamesByAssemblyName, helper);

            var getResourceAssemblyStreamOrNullMethod = CreateGetResourceAssemblyStreamOrNullMethod(dictionaryField, moduleType, helper);
            moduleType.Methods.Add(getResourceAssemblyStreamOrNullMethod);

            GenerateAppDomainModuleInitializerIL(module, moduleType, il, helper);

            moduleType.Methods.Add(moduleInitializer);
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
            ILProcessor il,
            FieldDefinition dictionaryField,
            IReadOnlyDictionary<string, string> embeddedResourceNamesByAssemblyName,
            MetadataHelper helper)
        {
            il.Emit(OpCodes.Call, helper.GetMethodReference(
                "class [HasStringComparer]System.StringComparer class [HasStringComparer]System.StringComparer::get_OrdinalIgnoreCase()"));

            il.Emit(OpCodes.Newobj, helper.GetMethodReference(
                @"instance void class [HasCollections]System.Collections.Generic.Dictionary`2<string, string>::.ctor(
                    class [CoreLibrary]System.Collections.Generic.IEqualityComparer`1<!0>)"));

            foreach (var entry in embeddedResourceNamesByAssemblyName)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldstr, entry.Key);
                il.Emit(OpCodes.Ldstr, entry.Value);

                il.Emit(OpCodes.Callvirt, helper.GetMethodReference(
                    "instance void class [HasCollections]System.Collections.Generic.Dictionary`2<string, string>::set_Item(!0, !1)"));
            }

            il.Emit(OpCodes.Stsfld, dictionaryField);
        }

        private static void GenerateAppDomainModuleInitializerIL(ModuleDefinition module, TypeDefinition moduleType, ILProcessor il, MetadataHelper helper)
        {
            var assemblyResolveHandler = CreateAppDomainAssemblyResolveHandler(module, helper);
            moduleType.Methods.Add(assemblyResolveHandler);

            il.Emit(OpCodes.Call, helper.GetMethodReference(
                "class [HasAppDomain]System.AppDomain class [HasAppDomain]System.AppDomain::get_CurrentDomain()"));

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, assemblyResolveHandler);
            il.Emit(OpCodes.Newobj, helper.GetMethodReference(
                "instance void class [HasAppDomain]System.ResolveEventHandler::.ctor(object, native int)"));

            il.Emit(OpCodes.Callvirt, helper.GetMethodReference(
                "instance void class [HasAppDomain]System.AppDomain::add_AssemblyResolve(class [HasAppDomain]System.ResolveEventHandler)"));

            il.Emit(OpCodes.Ret);
        }

        private static FieldDefinition CreateDictionaryField(TypeDefinition moduleType, MetadataHelper helper)
        {
            var field = new FieldDefinition(
                "EmbeddedResourceNamesByAssemblyName",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
                helper.GetTypeReference("class [HasCollections]System.Collections.Generic.Dictionary`2<string, string>"));

            moduleType.Fields.Add(field);
            return field;
        }

        private static MethodDefinition CreateAppDomainAssemblyResolveHandler(ModuleDefinition module, MetadataHelper helper)
        {
            var handler = new MethodDefinition(
                "OnAssemblyResolve",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType: helper.GetTypeReference("class System.Reflection.Assembly"))
            {
                Parameters =
                {
                    new ParameterDefinition(module.TypeSystem.Object),
                    new ParameterDefinition(helper.GetTypeReference("class [HasAppDomain]System.ResolveEventArgs"))
                }
            };

            var il = handler.Body.GetILProcessor();

            // TODO

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            return handler;
        }

        private static MethodDefinition CreateGetResourceAssemblyStreamOrNullMethod(FieldDefinition dictionaryField, TypeDefinition moduleType, MetadataHelper helper)
        {
            var method = new MethodDefinition(
                "GetResourceAssemblyStreamOrNull",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType: helper.GetTypeReference("class [HasStream]System.IO.Stream"))
            {
                Parameters = { new ParameterDefinition(helper.GetTypeReference("class System.Reflection.AssemblyName")) }
            };

            var il = method.Body.GetILProcessor();


            il.Emit(OpCodes.Ldsfld, dictionaryField);
            il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Callvirt, helper.GetMethodReference(
                "instance string class System.Reflection.AssemblyName::get_Name()"));

            var resourceNameVariable = new VariableDefinition(dictionaryField.Module.TypeSystem.String);
            method.Body.Variables.Add(resourceNameVariable);
            il.Emit(OpCodes.Ldloca_S, resourceNameVariable);

            il.Emit(OpCodes.Callvirt, helper.GetMethodReference(
                "instance bool class [HasCollections]System.Collections.Generic.Dictionary`2<string, string>::TryGetValue(!0, !1&)"));

            var successBranch = il.Create(OpCodes.Ldtoken, moduleType);

            il.Emit(OpCodes.Brtrue_S, successBranch);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            il.Append(successBranch);

            il.Emit(OpCodes.Call, helper.GetMethodReference(
                "class System.Type System.Type::GetTypeFromHandle(valuetype System.RuntimeTypeHandle)"));

            il.Emit(OpCodes.Callvirt, helper.GetMethodReference(
                "instance class System.Reflection.Assembly System.Type::get_Assembly()"));

            il.Emit(OpCodes.Ldloc_0);

            il.Emit(OpCodes.Callvirt, helper.GetMethodReference(
                "instance class System.IO.Stream System.Reflection.Assembly::GetManifestResourceStream(string)"));

            il.Emit(OpCodes.Ret);

            return method;
        }
    }
}