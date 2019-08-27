using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Techsola.EmbedDependencies
{
    internal readonly partial struct EmitHelper
    {
        private readonly MethodBody methodBody;

        public EmitHelper(MetadataHelper metadata, MethodBody methodBody)
        {
            if (methodBody is null) throw new ArgumentNullException(nameof(methodBody));

            Metadata = metadata;
            this.methodBody = methodBody;
            IL = methodBody.GetILProcessor();
        }

        public MetadataHelper Metadata { get; }
        public ILProcessor IL { get; }

        public VariableDefinition CreateLocal(string ilasmTypeReferenceSyntax)
        {
            var local = new VariableDefinition(Metadata.GetTypeReference(ilasmTypeReferenceSyntax));
            methodBody.Variables.Add(local);
            return local;
        }

        public void Brfalse_S(Instruction target)
        {
            IL.Emit(OpCodes.Brfalse_S, target);
        }

        public void Brtrue_S(Instruction target)
        {
            IL.Emit(OpCodes.Brtrue_S, target);
        }

        public void Call(string ilasmMethodReferenceSyntax)
        {
            var method = Metadata.GetMethodReference(ilasmMethodReferenceSyntax);

            IL.Emit(OpCodes.Call, method);
        }

        public void Call(MethodReference method)
        {
            IL.Emit(OpCodes.Call, method);
        }

        public void Callvirt(string ilasmMethodReferenceSyntax)
        {
            var method = Metadata.GetMethodReference(ilasmMethodReferenceSyntax);

            if (!method.HasThis) throw new ArgumentException(
                "Callvirt doesn't make sense with a static method. Did you mean to use 'Call' or forget the 'instance' keyword?",
                nameof(ilasmMethodReferenceSyntax));

            IL.Emit(OpCodes.Callvirt, method);
        }

        public void Conv_Ovf_I4()
        {
            IL.Emit(OpCodes.Conv_Ovf_I4);
        }

        public void Dup()
        {
            IL.Emit(OpCodes.Dup);
        }

        public void Endfinally()
        {
            IL.Emit(OpCodes.Endfinally);
        }

        public void Ldarg(int index)
        {
            switch (index)
            {
                case 0:
                    IL.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    IL.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    IL.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    IL.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index <= byte.MaxValue)
                        IL.Emit(OpCodes.Ldarg_S, (byte)index);
                    else
                        IL.Emit(OpCodes.Ldarg, index);
                    break;
            }
        }

        public void Ldftn(MethodReference method)
        {
            IL.Emit(OpCodes.Ldftn, method);
        }

        public void Ldloc(VariableDefinition variable)
        {
            var index = variable.Index;

            switch (index)
            {
                case 0:
                    IL.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    IL.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    IL.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    IL.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    if (index <= byte.MaxValue)
                        IL.Emit(OpCodes.Ldloc_S, (byte)index);
                    else
                        IL.Emit(OpCodes.Ldloc, index);
                    break;
            }
        }

        public void Ldloca(VariableDefinition variable)
        {
            var index = variable.Index;

            if (index <= byte.MaxValue)
                IL.Emit(OpCodes.Ldloca_S, (byte)index);
            else
                IL.Emit(OpCodes.Ldloca, index);
        }

        public void Ldnull()
        {
            IL.Emit(OpCodes.Ldnull);
        }

        public void Ldsfld(FieldReference field)
        {
            IL.Emit(OpCodes.Ldsfld, field);
        }

        public void Ldstr(string value)
        {
            IL.Emit(OpCodes.Ldstr, value);
        }

        public void Leave_S(Instruction target)
        {
            IL.Emit(OpCodes.Leave_S, target);
        }

        public void Newobj(string ilasmMethodReferenceSyntax)
        {
            var method = Metadata.GetMethodReference(ilasmMethodReferenceSyntax);

            if (!method.HasThis) throw new ArgumentException(
                "Newobj doesn't make sense with a static constructor. Did you forget the 'instance' keyword?",
                nameof(ilasmMethodReferenceSyntax));

            IL.Emit(OpCodes.Newobj, method);
        }

        public void Ret()
        {
            IL.Emit(OpCodes.Ret);
        }

        public void Stloc(VariableDefinition variable)
        {
            var index = variable.Index;

            switch (index)
            {
                case 0:
                    IL.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    IL.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    IL.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    IL.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    if (index <= byte.MaxValue)
                        IL.Emit(OpCodes.Stloc_S, (byte)index);
                    else
                        IL.Emit(OpCodes.Stloc, index);
                    break;
            }
        }

        public void Stsfld(FieldReference field)
        {
            IL.Emit(OpCodes.Stsfld, field);
        }
    }
}
