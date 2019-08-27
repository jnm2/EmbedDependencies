using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Techsola.EmbedDependencies.Emit
{
    public readonly partial struct Emitter
    {
        private readonly MethodBody body;
        private readonly ISyntaxProvider syntaxProvider;

        public Emitter(MethodBody body, ISyntaxProvider syntaxProvider)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.syntaxProvider = syntaxProvider;
        }

        public void Emit(IEnumerable<IProgramElement> programElements)
        {
            if (programElements is null) throw new ArgumentNullException(nameof(programElements));

            var exceptionHandlers = new List<ExceptionHandlerSpec>();

            var lowered = (IReadOnlyList<IProgramElement>)programElements.ToArray();

            lowered = LowerExceptionHandlers(lowered, exceptionHandlers.Add);
            lowered = ResolveReferenceSyntax(lowered);
            var instructions = LowerLabelBranching(lowered, out var targetsByLabel);

            var il = body.GetILProcessor();

            foreach (var instruction in instructions)
                il.Append(instruction);

            foreach (var handler in exceptionHandlers)
            {
                handler.Generate(body, label => targetsByLabel[label]);
            }
        }

        public void Emit(params IProgramElement[] programElements)
        {
            Emit((IEnumerable<IProgramElement>)programElements);
        }

        private static readonly Instruction DummyBranchTarget = Instruction.Create(OpCodes.Nop);

        private static IReadOnlyList<Instruction> LowerLabelBranching(
            IReadOnlyList<IProgramElement> elements,
            out IReadOnlyDictionary<Label, Instruction> targetsByLabel)
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

            targetsByLabel = targetInstructionsByLabel;
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

        private IReadOnlyList<IProgramElement> LowerExceptionHandlers(
            IReadOnlyList<IProgramElement> elements,
            Action<ExceptionHandlerSpec> addExceptionHandler)
        {
            var lowered = new List<IProgramElement>();

            foreach (var element in elements)
            {
                if (element is TryFinallyBlock tryFinally)
                {
                    var tryStartLabel = new Label();
                    lowered.Add(tryStartLabel);

                    lowered.AddRange(LowerExceptionHandlers(tryFinally.TryContents, addExceptionHandler));

                    var finallyStartLabel = new Label();
                    lowered.Add(finallyStartLabel);

                    lowered.AddRange(LowerExceptionHandlers(tryFinally.FinallyContents, addExceptionHandler));

                    var finallyEndLabel = new Label();
                    lowered.Add(finallyEndLabel);

                    addExceptionHandler.Invoke(new ExceptionHandlerSpec(
                        new ExceptionHandler(ExceptionHandlerType.Finally),
                        tryStartLabel,
                        tryEnd: finallyStartLabel,
                        finallyStartLabel,
                        finallyEndLabel));
                }
                else
                {
                    lowered.Add(element);
                }
            }

            return lowered;
        }
    }
}
