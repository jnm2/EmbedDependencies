using System.Diagnostics;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    [DebuggerDisplay("{ToString(),nq}")]
    internal readonly struct SyntaxToken
    {
        public SyntaxToken(SyntaxKind kind, object value = null)
        {
            Kind = kind;
            Value = value;
        }

        public SyntaxKind Kind { get; }
        public object Value { get; }

        public static implicit operator SyntaxToken(SyntaxKind kind) => new SyntaxToken(kind);

        public override string ToString()
        {
            var kind = Kind.ToString();
            return Value is null ? kind : kind + ' ' + Value;
        }
    }
}
