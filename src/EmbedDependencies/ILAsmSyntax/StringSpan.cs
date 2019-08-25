using System;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    internal readonly struct StringSpan
    {
        private readonly string value;
        private readonly int start;

        public int Length { get; }

        public bool IsEmpty => Length == 0;

        private StringSpan(string value, int start, int length)
        {
            this.value = value;
            this.start = start;
            Length = length;
        }

        public StringSpan Slice(int start)
        {
            if (start < 0 || start > Length) throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be greater than or equal to zero and less than or equal to the current length.");
            return new StringSpan(value, this.start + start, Length - start);
        }

        public StringSpan Slice(int start, int length)
        {
            if (start < 0 || start > Length) throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be greater than or equal to zero and less than or equal to the current length.");
            if (length < 0 || start + length > Length) throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be greater than or equal to zero and less than or equal to the current length minus the start.");
            return new StringSpan(value, this.start + start, length);
        }

        public static implicit operator StringSpan(string value) => new StringSpan(value, 0, value?.Length ?? 0);

        public static explicit operator string(StringSpan value) => value.ToString();

        public override string ToString() => value.Substring(start, Length);

        public int IndexOf(char value)
        {
            var r = this.value.IndexOf(value, start, Length);
            return r == -1 ? -1 : r - start;
        }

        public int LastIndexOf(char value)
        {
            var r = this.value.LastIndexOf(value, start + Length - 1, Length);
            return r == -1 ? -1 : r - start;
        }

        public bool StartsWith(char value)
        {
            return Length > 0 && this.value[start] == value;
        }

        public bool StartsWith(string value)
        {
            return value.Length <= Length
                && string.CompareOrdinal(this.value, start, value, 0, value.Length) == 0;
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (sourceIndex < 0 || sourceIndex > Length) throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Source index must be greater than or equal to zero and less than or equal to the current length.");
            if (count < 0 || sourceIndex + count > Length) throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be greater than or equal to zero and less than or equal to the current length minus the source index.");
            value.CopyTo(start + sourceIndex, destination, destinationIndex, count);
        }

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be greater than or equal to zero and less than the current length.");
                return value[index + start];
            }
        }
    }

    internal static class StringSpanExtensions
    {
        public static StringSpan Slice(this string value, int start)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return ((StringSpan)value).Slice(start);
        }

        public static StringSpan Slice(this string value, int start, int length)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return ((StringSpan)value).Slice(start, length);
        }
    }
}
