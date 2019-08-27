using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies.Emit
{
    public readonly struct Emitter
    {
        private readonly MethodBody body;
        private readonly ISyntaxProvider syntaxProvider;

        public Emitter(MethodBody body, ISyntaxProvider syntaxProvider)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.syntaxProvider = syntaxProvider;
        }

        public VariableDefinition CreateLocal(string typeReferenceSyntax)
        {
            var local = new VariableDefinition(syntaxProvider.GetTypeReference(typeReferenceSyntax));
            body.Variables.Add(local);
            return local;
        }

        public void Emit(params IProgramElement[] programElements)
        {
            if (programElements is null) throw new ArgumentNullException(nameof(programElements));

            var lowered = ResolveReferenceSyntax(programElements);
            var instructions = LowerLabelBranching(lowered);

            var il = body.GetILProcessor();

            foreach (var instruction in instructions)
                il.Append(instruction);
        }

        private static readonly Instruction DummyBranchTarget = Instruction.Create(OpCodes.Nop);

        private static IReadOnlyList<Instruction> LowerLabelBranching(IReadOnlyList<IProgramElement> elements)
        {
            var lowered = new List<Instruction>();

            var targetLabelsByBranchInstruction = new List<KeyValuePair<Instruction, Label>>();
            var targetInstructionsByLabel = new Dictionary<Label, Instruction>();
            var labelsToMarkWithNextInstruction = new List<Label>();

            using (var en = elements.GetEnumerator())
            {
                while (en.MoveNext())
                {
                    while (en.Current is Label label)
                    {
                        labelsToMarkWithNextInstruction.Add(label);

                        if (!en.MoveNext()) throw new InvalidOperationException("Labels must not appear after the final instruction.");
                    }

                    Instruction nextInstruction;

                    switch (en.Current)
                    {
                        case DirectInstruction direct:
                            nextInstruction = direct.Instruction;
                            break;

                        case LabelBranch branch:
                            nextInstruction = Instruction.Create(branch.OpCode, DummyBranchTarget);

                            targetLabelsByBranchInstruction.Add(new KeyValuePair<Instruction, Label>(nextInstruction, branch.Target));
                            break;

                        default:
                            throw new InvalidOperationException("Invalid program element type for this phase.");
                    }

                    lowered.Add(nextInstruction);

                    foreach (var label in labelsToMarkWithNextInstruction)
                    {
                        if (targetInstructionsByLabel.ContainsKey(label))
                            throw new InvalidOperationException("The same label must not appear in the program more than once.");

                        targetInstructionsByLabel.Add(label, nextInstruction);
                    }

                    labelsToMarkWithNextInstruction.Clear();
                }
            }

            foreach (var (branchInstruction, label) in targetLabelsByBranchInstruction)
            {
                if (targetInstructionsByLabel.TryGetValue(label, out var targetInstruction))
                    branchInstruction.Operand = targetInstruction;
                else
                    throw new InvalidOperationException("The label of a branch instruction was not added to the program.");
            }

            return lowered;
        }

        private IReadOnlyList<IProgramElement> ResolveReferenceSyntax(IReadOnlyList<IProgramElement> elements)
        {
            var lowered = new List<IProgramElement>();

            foreach (var element in elements)
            {
                lowered.Add(element is SyntaxBasedCall call
                    ? new DirectInstruction(Instruction.Create(
                        call.OpCode,
                        syntaxProvider.GetMethodReference(call.MethodReferenceSyntax)))
                    : element);
            }

            return lowered;
        }
    }
}
