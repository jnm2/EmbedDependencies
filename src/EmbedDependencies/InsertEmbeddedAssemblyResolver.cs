using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;

namespace Techsola.EmbedDependencies
{
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

            CreateModuleInitializer(assemblyDefinition.MainModule);

            assemblyDefinition.Write(stream);
        }

        private static void CreateModuleInitializer(ModuleDefinition module)
        {
            var moduleType = module.GetType("<Module>");

            var moduleInitializer = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            var il = moduleInitializer.Body.GetILProcessor();

            var parts = GetTargetFramework(module.Assembly)?.Split(',');
            var frameworkName = parts?[0];
            var version = parts is null ? null : Version.Parse(parts[1].Substring("Version=v".Length));

            var appDomainAssemblyScope = module.TypeSystem.CoreLibrary;

            if (frameworkName == ".NETCoreApp")
            {
                if (version < new Version(2, 0))
                    throw new NotSupportedException("Versions of .NET Core older than 2.0 are not supported.");

                appDomainAssemblyScope = module.AssemblyReferences.Single(r => r.Name == "System.Runtime.Extensions");
            }
            else if (frameworkName == ".NETStandard")
            {
                if (version < new Version(2, 0))
                    throw new NotSupportedException("Versions of .NET Standard older than 2.0 are not supported.");
            }

            GenerateAppDomainModuleInitializerIL(module, moduleType, il, appDomainAssemblyScope);

            moduleType.Methods.Add(moduleInitializer);
        }

        private static string GetTargetFramework(AssemblyDefinition assembly)
        {
            var targetFrameworkAttribute = assembly.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");

            return (string)targetFrameworkAttribute?.ConstructorArguments.First().Value;
        }

        private static void GenerateAppDomainModuleInitializerIL(ModuleDefinition module, TypeDefinition moduleType, ILProcessor il, IMetadataScope appDomainAssemblyScope)
        {
            var appDomainType = module.ImportReference(new TypeReference(
                "System", "AppDomain", module: null, appDomainAssemblyScope, valueType: false));

            var resolveEventHandlerType = module.ImportReference(new TypeReference(
                "System", "ResolveEventHandler", module: null, appDomainAssemblyScope, valueType: false));

            var assemblyResolveHandler = CreateAppDomainAssemblyResolveHandler(module);
            moduleType.Methods.Add(assemblyResolveHandler);

            il.Emit(OpCodes.Call, new MethodReference(
                "get_CurrentDomain",
                returnType: appDomainType,
                declaringType: appDomainType));

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, assemblyResolveHandler);
            il.Emit(OpCodes.Newobj, new MethodReference(
                ".ctor",
                returnType: module.TypeSystem.Void,
                declaringType: resolveEventHandlerType)
            {
                HasThis = true,
                Parameters =
                {
                    new ParameterDefinition(module.TypeSystem.Object),
                    new ParameterDefinition(module.TypeSystem.IntPtr)
                }
            });

            il.Emit(OpCodes.Callvirt, new MethodReference(
                "add_AssemblyResolve",
                returnType: module.TypeSystem.Void,
                declaringType: appDomainType)
            {
                HasThis = true,
                Parameters = { new ParameterDefinition(resolveEventHandlerType) }
            });

            il.Emit(OpCodes.Ret);
        }

        private static MethodDefinition CreateAppDomainAssemblyResolveHandler(ModuleDefinition module)
        {
            var assemblyType = new TypeReference("System.Reflection", "Assembly", module, scope: null, valueType: false);

            var handler = new MethodDefinition("OnAssemblyResolve", MethodAttributes.Static, assemblyType);
            var il = handler.Body.GetILProcessor();

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            return handler;
        }
    }
}
