using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.IO;

namespace Techsola.EmbedDependencies
{
    // Template: https://sharplab.io/#v2:EYLgxg9gTgpgtADwGwBYA0AXEBLANgHwAEAmARgFgAoQgBgAJDSUBuK2h0gOgEkB5V6vUacASjABmuGGAzYIAOwHthIgK7zZAWxicAMhACGAExhQBbUkgbE6AWQhHVUqgG8qdDxyv3HUgBQAlO6ebpSe4XQAggAO0QAiEJoG2PKcAMKqULAaCUkpnJEAzoUwmsC4AJ5ihRC4AG4wdADUALx0vPJFJWWV1bUNAuEAvsEeo3QA9BN03OJ0nAByAKIAKnRp0DDj0VDYdQYYjYxWXaXlFXRxEgZOGAD6ffUpAOZ+pz0V+sYbGjAIGHRIL9/mgosUzpUFgZtHR5NCYEEwiFxuF2H59lA6IUMLBoXQ2gBxGAYPqZMAwd7nADKOJg0N4UAWTlwfjh2gCiIiyKRXPC2DmfmxuM0dGwhVhzICDAA7BLcLhBrzUbKgYd/npDEYAGJQRI04WC2nQgKKrkjHl0c3hcZTdoYAAWpgA7mKthadnsDkdLGDuud2p1wR9Hg0/BBgAAraQAkryExQUEhmBLBoaSJQZ7ihHjUJKtEYrFGkWE4mkqDkymVfV0zQMpny1kwJ2+iEVKHaPw6dsIjko7lKvkCoU10Xi+SSmVyhV9rkziL5gyY4CqcTiUz42FNuylaAVavQvxgAzRAxgbAYCogQGOsAAaxgRj8fhSGACw+hehg8meDo5nIHHi5gBETvpo6QQNEFQrBAfjLqupgmnO4QAJCELKlYVBqxiwSua5QJw0HplABgVIEiEWkqVq8kMnjjFRYzurs+yHF4dD7iKRIkjANRkhSQbUkWdbMm8/GQvCdAGKJbbwv+gFzmhdBLGUD4mEYZbkt2hQAEIVBh3YEVAFScQAagYuCqDAfiSX6YnaIs8KghAqgAgWsA8eWMDdrJA4APx0Be0QwBA4h+D4TgIgUUmcJxtgGPI/LcVx7nkuxfhuU5HleUhnhXuO8qmpa4zbExXqsbiRgKJUlzYDIchwoZAA8jA0KCzUAHyKcpRiqepnnwtpulSd2c5tPIW5xDVsgKIuFRNaQLUcDQbV+PqLwbJoJ6wPhDJGCkZncM88ibGkknZhR/bAQA2gARFSiQ6Bh9naNdAC6G6EIQt33QAOkpwAqQ+329d9AAKBz2tdaBzshN3EmAr0btdcPXXOQwCOaQA===

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
            var helper = TargetMetadataSetup.InitializeMetadataHelper(module);

            var moduleType = module.GetType("<Module>");

            var dictionaryField = CreateDictionaryField(helper);
            moduleType.Fields.Add(dictionaryField);

            var getResourceAssemblyStreamOrNullMethod = CreateGetResourceAssemblyStreamOrNullMethod(dictionaryField, moduleType, helper);
            moduleType.Methods.Add(getResourceAssemblyStreamOrNullMethod);

            var assemblyResolveHandler = CreateAppDomainAssemblyResolveHandler(getResourceAssemblyStreamOrNullMethod, helper);
            moduleType.Methods.Add(assemblyResolveHandler);

            var moduleInitializer = CreateModuleInitializer(dictionaryField, embeddedResourceNamesByAssemblyName, assemblyResolveHandler, helper);
            moduleType.Methods.Add(moduleInitializer);

            assemblyDefinition.Write(stream);
        }

        private static MethodDefinition CreateModuleInitializer(
            FieldReference dictionaryField,
            IReadOnlyDictionary<string, string> embeddedResourceNamesByAssemblyName,
            MethodReference assemblyResolveHandler,
            MetadataHelper helper)
        {
            // C#:
            // static <Module>()
            // {
            //     EmbeddedResourceNamesByAssemblyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            //     {
            //         ["Some.Assembly.Name"] = @"Some\Embedded\Resource\Path",
            //         ["etc"] = "etc"
            //     };
            //
            //     AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            // }

            var moduleInitializer = helper.DefineMethod(
                ".cctor",
                MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                returnType: "void");

            var emit = helper.GetEmitHelper(moduleInitializer);

            // Initialize dictionary field
            emit.Call("class System.StringComparer class System.StringComparer::get_OrdinalIgnoreCase()");
            emit.Newobj(@"
                instance void class System.Collections.Generic.Dictionary`2<string, string>::.ctor(
                    class System.Collections.Generic.IEqualityComparer`1<!0>)");

            foreach (var entry in embeddedResourceNamesByAssemblyName)
            {
                emit.Dup();
                emit.Ldstr(entry.Key);
                emit.Ldstr(entry.Value);
                emit.Callvirt("instance void class System.Collections.Generic.Dictionary`2<string, string>::set_Item(!0, !1)");
            }

            emit.Stsfld(dictionaryField);

            // Add AssemblyResolve handler
            emit.Call("class System.AppDomain class System.AppDomain::get_CurrentDomain()");
            emit.Ldnull();
            emit.Ldftn(assemblyResolveHandler);
            emit.Newobj("instance void class System.ResolveEventHandler::.ctor(object, native int)");
            emit.Callvirt("instance void class System.AppDomain::add_AssemblyResolve(class System.ResolveEventHandler)");

            emit.Ret();

            return moduleInitializer;
        }

        private static FieldDefinition CreateDictionaryField(MetadataHelper helper)
        {
            // C#:
            // private static readonly Dictionary<string, string> EmbeddedResourceNamesByAssemblyName;

            return new FieldDefinition(
                "EmbeddedResourceNamesByAssemblyName",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
                helper.GetTypeReference("class System.Collections.Generic.Dictionary`2<string, string>"));
        }

        private static MethodDefinition CreateAppDomainAssemblyResolveHandler(MethodReference getResourceAssemblyStreamOrNullMethod, MetadataHelper helper)
        {
            // C#:
            // private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
            // {
            //     using (var stream = GetResourceAssemblyStreamOrNull(new AssemblyName(e.Name)))
            //     {
            //         if (stream is null) return null;
            //
            //         using (var buffer = new MemoryStream(capacity: checked((int)stream.Length)))
            //         {
            //             stream.CopyTo(buffer);
            //             return Assembly.Load(buffer.ToArray());
            //         }
            //     }
            // }

            var method = helper.DefineMethod(
                "OnAssemblyResolve",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType: "class System.Reflection.Assembly",
                parameterTypes: new[] { "object", "class System.ResolveEventArgs" });

            var emit = helper.GetEmitHelper(method);
            var streamLocal = emit.CreateLocal("class System.IO.Stream");
            var assemblyLocal = emit.CreateLocal("classSystem.Reflection.Assembly");
            var bufferLocal = emit.CreateLocal("class System.IO.MemoryStream");

            emit.Ldarg(1);
            emit.Callvirt("instance string System.ResolveEventArgs::get_Name()");
            emit.Newobj("instance void System.Reflection.AssemblyName::.ctor(string)");
            emit.Call(getResourceAssemblyStreamOrNullMethod);
            emit.Stloc(streamLocal);

            var loadReturnValue = emit.IL.Create(OpCodes.Ldloc, assemblyLocal);
            var skipReturningNull = emit.IL.Create(OpCodes.Ldloc, streamLocal);

            // try
            emit.Ldloc(streamLocal);
            emit.Brtrue_S(skipReturningNull);

            emit.Ldnull();
            emit.Stloc(assemblyLocal);
            emit.Leave_S(loadReturnValue);

            emit.IL.Append(skipReturningNull);
            emit.Callvirt("instance int64 System.IO.Stream::get_Length()");
            emit.Conv_Ovf_I4();
            emit.Newobj("instance void System.IO.MemoryStream::.ctor(int32)");
            emit.Stloc(bufferLocal);

            // try
            emit.Ldloc(streamLocal);
            emit.Ldloc(bufferLocal);
            emit.Callvirt("instance void System.IO.Stream::CopyTo(class System.IO.Stream)");
            emit.Ldloc(bufferLocal);
            emit.Callvirt("instance unsigned int8[] System.IO.MemoryStream::ToArray()");
            emit.Call("class System.Reflection.Assembly System.Reflection.Assembly::Load(unsigned int8[])");
            emit.Stloc(assemblyLocal);
            emit.Leave_S(loadReturnValue);

            // finally
            var endFinally = emit.IL.Create(OpCodes.Endfinally);
            emit.Ldloc(bufferLocal);
            emit.Brfalse_S(endFinally);
            emit.Ldloc(bufferLocal);
            emit.Callvirt("instance void System.IDisposable::Dispose()");
            emit.IL.Append(endFinally);

            // finally
            endFinally = emit.IL.Create(OpCodes.Endfinally);
            emit.Ldloc(streamLocal);
            emit.Brfalse_S(endFinally);
            emit.Ldloc(streamLocal);
            emit.Callvirt("instance void System.IDisposable::Dispose()");
            emit.IL.Append(endFinally);

            emit.IL.Append(loadReturnValue);
            emit.Ret();

            /*
            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
               TryStart = ,
               TryEnd = ,
               HandlerStart = ,
               HandlerEnd =
            });*/

            return method;
        }

        private static MethodDefinition CreateGetResourceAssemblyStreamOrNullMethod(FieldReference dictionaryField, TypeReference moduleType, MetadataHelper helper)
        {
            // C#:
            // private static Stream GetResourceAssemblyStreamOrNull(AssemblyName assemblyName)
            // {
            //     return EmbeddedResourceNamesByAssemblyName.TryGetValue(assemblyName.Name, out var resourceName)
            //         ? typeof(Module).Assembly.GetManifestResourceStream(resourceName)
            //         : null;
            // }

            var method = helper.DefineMethod(
                "GetResourceAssemblyStreamOrNull",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType: "class System.IO.Stream",
                parameterTypes: new[] { "class System.Reflection.AssemblyName" });

            var emit = helper.GetEmitHelper(method);

            emit.Ldsfld(dictionaryField);
            emit.Ldarg(0);
            emit.Callvirt("instance string class System.Reflection.AssemblyName::get_Name()");

            var resourceNameVariable = emit.CreateLocal("string");
            emit.Ldloca(resourceNameVariable);
            emit.Callvirt("instance bool class System.Collections.Generic.Dictionary`2<string, string>::TryGetValue(!0, !1&)");

            var successBranch = emit.IL.Create(OpCodes.Ldtoken, moduleType);

            emit.Brtrue_S(successBranch);
            emit.Ldnull();
            emit.Ret();

            return method;
        }
    }
}
