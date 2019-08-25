using System;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    internal readonly struct ParseResult<T>
    {
        private ParseResult(Optional<T> value, string peekedTokenMessage)
        {
            Value = value;
            PeekedTokenMessage = peekedTokenMessage;
        }

        public ParseResult(T value)
            : this(Optional.Some(value), peekedTokenMessage: null)
        {
        }

        public ParseResult(T value, string peekedTokenMessage)
            : this(Optional.Some(value), peekedTokenMessage)
        {
            if (string.IsNullOrWhiteSpace(peekedTokenMessage))
                throw new ArgumentException("Peeked token message must be specified.", nameof(peekedTokenMessage));
        }

        public ParseResult(string peekedTokenMessage)
            : this(Optional.None<T>(), peekedTokenMessage)
        {
            if (string.IsNullOrWhiteSpace(peekedTokenMessage))
                throw new ArgumentException("Peeked token message must be specified.", nameof(peekedTokenMessage));
        }

        public Optional<T> Value { get; }
        public string PeekedTokenMessage { get; }

        public static implicit operator ParseResult<T>(T value) => new ParseResult<T>(value);
    }
}
