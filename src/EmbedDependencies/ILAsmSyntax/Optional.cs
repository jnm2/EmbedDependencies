using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    internal static class Optional
    {
        public static Optional<T> Some<T>(T value) => new Optional<T>(value);
        public static Optional<T> None<T>() => default;
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal readonly struct Optional<T> : IEquatable<Optional<T>>
    {
        private readonly bool isSome;
        private readonly T value;

        public Optional(T value)
        {
            isSome = true;
            this.value = value;
        }

        public bool IsNone => !IsNone;

        public bool IsSome(out T value)
        {
            value = this.value;
            return isSome;
        }

        public T Value => isSome ? value : throw new InvalidOperationException("This optional instance is None.");

        public override bool Equals(object obj)
        {
            return obj is Optional<T> optional && Equals(optional);
        }

        public bool Equals(Optional<T> other)
        {
            return isSome == other.isSome && EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            var hashCode = -934799437;
            hashCode = hashCode * -1521134295 + isSome.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(value);
            return hashCode;
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return isSome ? $"Some({value})" : "None";
        }
    }
}
