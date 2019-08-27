using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies.Emit
{
    public sealed class ProgramBuilder
    {
        private readonly MethodBody body;
        private readonly ISyntaxProvider syntaxProvider;
        private readonly List<IProgramElement> program = new List<IProgramElement>();

        public ProgramBuilder(MethodBody body, ISyntaxProvider syntaxProvider)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.syntaxProvider = syntaxProvider ?? throw new ArgumentNullException(nameof(syntaxProvider));
        }

        public VariableDefinition CreateLocal(string typeReferenceSyntax)
        {
            var local = new VariableDefinition(syntaxProvider.GetTypeReference(typeReferenceSyntax));
            body.Variables.Add(local);
            return local;
        }

        public void Append(IProgramElement first, params IProgramElement[] elements)
        {
            program.Add(first);
            program.AddRange(elements);
        }

        public void Append(IEnumerable<IProgramElement> elements)
        {
            program.AddRange(elements);
        }

        public void Emit()
        {
            new Emitter(body, syntaxProvider).Emit(program);
            program.Clear();
        }
    }
}
