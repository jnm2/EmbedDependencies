using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Techsola.EmbedDependencies.Emit
{
    public static class Elements
    {
        public static IProgramElement Br(Label target) => new LabelBranch(OpCodes.Br, target);

        public static IProgramElement Br_S(Label target) => new LabelBranch(OpCodes.Br_S, target);

        public static IProgramElement Brfalse(Label target) => new LabelBranch(OpCodes.Brfalse, target);

        public static IProgramElement Brfalse_S(Label target) => new LabelBranch(OpCodes.Brfalse_S, target);

        public static IProgramElement Brtrue(Label target) => new LabelBranch(OpCodes.Brtrue, target);

        public static IProgramElement Brtrue_S(Label target) => new LabelBranch(OpCodes.Brtrue_S, target);

        public static IProgramElement Call(string methodReferenceSyntax) => new SyntaxBasedCall(OpCodes.Call, methodReferenceSyntax);

        public static IProgramElement Callvirt(string methodReferenceSyntax) => new SyntaxBasedCall(OpCodes.Callvirt, methodReferenceSyntax);

        public static IProgramElement Ldarg(int index)
        {
            switch (index)
            {
                case 0:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldarg_0));
                case 1:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldarg_1));
                case 2:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldarg_2));
                case 3:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldarg_3));
            }

            if (index <= byte.MaxValue)
                return new DirectInstruction(Instruction.Create(OpCodes.Ldarg_S, (byte)index));

            return new DirectInstruction(Instruction.Create(OpCodes.Ldarg, index));
        }

        public static IProgramElement Ldarga(int index)
        {
            if (index <= byte.MaxValue)
                return new DirectInstruction(Instruction.Create(OpCodes.Ldarga_S, (byte)index));

            return new DirectInstruction(Instruction.Create(OpCodes.Ldarga, index));
        }

        public static IProgramElement Ldfld(FieldReference field) => new DirectInstruction(Instruction.Create(OpCodes.Ldfld, field));

        public static IProgramElement Ldflda(FieldReference field) => new DirectInstruction(Instruction.Create(OpCodes.Ldflda, field));

        public static IProgramElement Ldloc(VariableDefinition variable)
        {
            switch (variable.Index)
            {
                case 0:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldloc_0));
                case 1:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldloc_1));
                case 2:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldloc_2));
                case 3:
                    return new DirectInstruction(Instruction.Create(OpCodes.Ldloc_3));
            }

            if (variable.Index <= byte.MaxValue)
                return new DirectInstruction(Instruction.Create(OpCodes.Ldloc_S, variable));

            return new DirectInstruction(Instruction.Create(OpCodes.Ldloc, variable));
        }

        public static IProgramElement Ldloca(VariableDefinition variable)
        {
            if (variable.Index <= byte.MaxValue)
                return new DirectInstruction(Instruction.Create(OpCodes.Ldloca_S, variable));

            return new DirectInstruction(Instruction.Create(OpCodes.Ldloca, variable));
        }

        public static IProgramElement Ldsfld(FieldReference field) => new DirectInstruction(Instruction.Create(OpCodes.Ldsfld, field));

        public static IProgramElement Ldsflda(FieldReference field) => new DirectInstruction(Instruction.Create(OpCodes.Ldsflda, field));

        public static IProgramElement Ldnull() => new DirectInstruction(Instruction.Create(OpCodes.Ldnull));

        public static IProgramElement Ldtoken(TypeReference type) => new DirectInstruction(Instruction.Create(OpCodes.Ldtoken, type));

        public static IProgramElement Leave(Label target) => new LabelBranch(OpCodes.Leave, target);

        public static IProgramElement Leave_S(Label target) => new LabelBranch(OpCodes.Leave_S, target);

        public static IProgramElement Newobj(string methodReferenceSyntax) => new SyntaxBasedCall(OpCodes.Newobj, methodReferenceSyntax);

        public static IProgramElement Ret() => new DirectInstruction(Instruction.Create(OpCodes.Ret));

        public static IProgramElement Starg(int index)
        {
            if (index <= byte.MaxValue)
                return new DirectInstruction(Instruction.Create(OpCodes.Starg_S, (byte)index));

            return new DirectInstruction(Instruction.Create(OpCodes.Starg, index));
        }

        public static IProgramElement Stfld(FieldReference field) => new DirectInstruction(Instruction.Create(OpCodes.Stfld, field));

        public static IProgramElement Stloc(VariableDefinition variable)
        {
            if (variable.Index <= byte.MaxValue)
                return new DirectInstruction(Instruction.Create(OpCodes.Stloc_S, variable));

            return new DirectInstruction(Instruction.Create(OpCodes.Stloc, variable));
        }

        public static IProgramElement Stsfld(FieldReference field) => new DirectInstruction(Instruction.Create(OpCodes.Stsfld, field));
    }
}
