using Mono.Cecil.Cil;
using System;

namespace Techsola.EmbedDependencies.Emit
{
    public sealed class DirectInstruction : IProgramElement
    {
        public DirectInstruction(Instruction instruction)
        {
            Instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
        }

        public Instruction Instruction { get; }
    }
}
