using Mono.Cecil.Cil;
using System;

namespace Techsola.EmbedDependencies.Emit
{
    public sealed class SyntaxBasedCall : IProgramElement
    {
        public SyntaxBasedCall(OpCode opCode, string methodReferenceSyntax)
        {
            if (string.IsNullOrWhiteSpace(methodReferenceSyntax))
                throw new ArgumentException("Method reference syntax must be specified.", nameof(methodReferenceSyntax));

            OpCode = opCode;
            MethodReferenceSyntax = methodReferenceSyntax;
        }

        public OpCode OpCode { get; }
        public string MethodReferenceSyntax { get; }
    }
}
