using Mono.Cecil.Cil;
using System;

namespace Techsola.EmbedDependencies.Emit
{
    public sealed class LabelBranch : IProgramElement
    {
        public LabelBranch(OpCode opCode, Label target)
        {
            OpCode = opCode;
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public OpCode OpCode { get; }
        public Label Target { get; }
    }
}
