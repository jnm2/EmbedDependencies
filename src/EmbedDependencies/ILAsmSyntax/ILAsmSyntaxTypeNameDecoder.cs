using System;
using System.Globalization;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    public static class ILAsmSyntaxTypeNameDecoder
    {
        public static TType Decode<TType>(string typeNameSyntax, IILAsmTypeNameSyntaxTypeProvider<TType> provider)
        {
            return new ILAsmSyntaxTypeNameDecoder<TType>(provider).Decode(typeNameSyntax);
        }
    }

    internal sealed class ILAsmSyntaxTypeNameDecoder<TType>
    {
        private readonly IILAsmTypeNameSyntaxTypeProvider<TType> provider;

        public ILAsmSyntaxTypeNameDecoder(IILAsmTypeNameSyntaxTypeProvider<TType> provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public TType Decode(string typeNameSyntax)
        {
            var span = (StringSpan)typeNameSyntax;
            return ParseType(ref span);
        }

        private TType ParseType(ref StringSpan span)
        {
            var type = ParseBeginning(ref span);

            while (TryRead(ref span, out var c))
            {
                switch (c)
                {
                    case '&':
                        type = provider.GetByReferenceType(type);
                        break;

                    case '*':
                        type = provider.GetPointerType(type);
                        break;

                    case ' ':
                        if (TryRead(ref span, "pinned"))
                        {
                            type = provider.GetPinnedType(type);
                            break;
                        }
                        goto default;

                    default:
                        throw new NotImplementedException();
                }
            }

            return type;
        }

        private TType ParseBeginning(ref StringSpan span)
        {
            if (!TryRead(ref span, out var c))
                throw new FormatException("Expected type.");

            switch (c)
            {
                case '!':
                    var isGenericMethodParameter = TryRead(ref span, '!');
                    var index = ReadInt32(ref span);

                    return isGenericMethodParameter
                        ? provider.GetGenericMethodParameter(index)
                        : provider.GetGenericTypeParameter(index);

                case 'b':
                    // Now I know why tokenization makes sense as a first step :D
                    if (TryRead(ref span, "ool"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Boolean);
                    goto default;

                case 'c':
                    if (TryRead(ref span, "har"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Char);
                    goto default;

                case 'f':
                    if (TryRead(ref span, "loat32"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Single);
                    if (TryRead(ref span, "loat64"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Double);
                    goto default;

                case 'i':
                    if (TryRead(ref span, "nt8"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.SByte);
                    if (TryRead(ref span, "nt16"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Int16);
                    if (TryRead(ref span, "nt32"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Int32);
                    if (TryRead(ref span, "nt64"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Int64);
                    goto default;

                case 'n':
                    if (TryRead(ref span, "ative int"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.IntPtr);
                    if (TryRead(ref span, "ative unsigned int"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.UIntPtr);
                    goto default;

                case 'o':
                    if (TryRead(ref span, "bject"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Object);
                    goto default;

                case 's':
                    if (TryRead(ref span, "tring"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.String);
                    goto default;

                case 't':
                    if (TryRead(ref span, "ypedref"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.TypedReference);
                    goto default;

                case 'u':
                    if (TryRead(ref span, "nsigned int8"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Byte);
                    if (TryRead(ref span, "nsigned int16"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.UInt16);
                    if (TryRead(ref span, "nsigned int32"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.UInt32);
                    if (TryRead(ref span, "nsigned int64"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.UInt64);
                    goto default;

                case 'v':
                    if (TryRead(ref span, "oid"))
                        return provider.GetPrimitiveType(PrimitiveTypeCode.Void);
                    goto default;

                default:
                    throw new NotImplementedException();
            }
        }

        private static bool TryRead(ref StringSpan span, out char value)
        {
            if (!span.IsEmpty)
            {
                value = span[0];
                span = span.Slice(1);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryRead(ref StringSpan span, char value)
        {
            if (!span.StartsWith(value)) return false;
            span = span.Slice(1);
            return true;
        }

        private static bool TryRead(ref StringSpan span, string value)
        {
            if (!span.StartsWith(value)) return false;
            span = span.Slice(value.Length);
            return true;
        }

        private static int ReadInt32(ref StringSpan span)
        {
            if (TryRead(ref span, "0x"))
            {
                if (!TryReadHexCharSpan(ref span, out var hexSpan))
                    throw new FormatException("Expected hexadecimal characters.");

                return int.Parse(hexSpan.ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            }

            if (!TryReadDecimalCharSpan(ref span, out var decimalSpan))
                throw new FormatException("Expected decimal characters.");

            return int.Parse(decimalSpan.ToString(), NumberStyles.None, CultureInfo.InvariantCulture);
        }

        private static bool TryReadDecimalCharSpan(ref StringSpan span, out StringSpan value)
        {
            var index = 0;

            while (index < span.Length && IsDecimalChar(span[index]))
            {
                index++;
            }

            if (index > 0)
            {
                value = span.Slice(0, index);
                span = span.Slice(index);
                return true;
            }

            value = default;
            return false;
        }

        private static bool IsDecimalChar(char value)
        {
            return '0' <= value && value <= '9';
        }

        private static bool TryReadHexCharSpan(ref StringSpan span, out StringSpan value)
        {
            var index = 0;

            while (index < span.Length && IsHexChar(span[index]))
            {
                index++;
            }

            if (index > 0)
            {
                value = span.Slice(0, index);
                span = span.Slice(index);
                return true;
            }

            value = default;
            return false;
        }

        private static bool IsHexChar(char value)
        {
            return ('0' <= value && value <= '9')
                || ('A' <= value && value <= 'F')
                || ('a' <= value && value <= 'f');
        }
    }
}
