using System;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    internal readonly struct ParseResult<T>
    {
        private ParseResult(Optional<T> value, SyntaxToken? unusedToken, string unusedTokenMessage)
        {
            Value = value;
            UnusedToken = unusedToken;
            UnusedTokenMessage = unusedTokenMessage;
        }

        public ParseResult(T value)
            : this(Optional.Some(value), unusedToken: null, unusedTokenMessage: null)
        {
        }

        public ParseResult(T value, SyntaxToken unusedToken, string unusedTokenMessage)
            : this(Optional.Some(value), unusedToken, unusedTokenMessage)
        {
            if (string.IsNullOrWhiteSpace(unusedTokenMessage))
                throw new ArgumentException("Unused token message must be specified.", nameof(unusedTokenMessage));
        }

        public ParseResult(SyntaxToken unusedToken, string unusedTokenMessage)
            : this(Optional.None<T>(), unusedToken, unusedTokenMessage)
        {
            if (string.IsNullOrWhiteSpace(unusedTokenMessage))
                throw new ArgumentException("Unused token message must be specified.", nameof(unusedTokenMessage));
        }

        public Optional<T> Value { get; }
        public SyntaxToken? UnusedToken { get; }
        public string UnusedTokenMessage { get; }

        public static implicit operator ParseResult<T>(T value) => new ParseResult<T>(value);
    }
}
