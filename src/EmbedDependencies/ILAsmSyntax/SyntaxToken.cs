namespace Techsola.EmbedDependencies.ILAsmSyntax
{
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
    }
}
