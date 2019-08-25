using System;

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

            while (true)
            {
                var next = ILAsmLexer.Lex(ref span);

                switch (next.Kind)
                {
                    case SyntaxKind.End:
                        return type;

                    case SyntaxKind.AmpersandToken:
                        type = provider.GetByReferenceType(type);
                        break;

                    case SyntaxKind.AsteriskToken:
                        type = provider.GetPointerType(type);
                        break;

                    case SyntaxKind.PinnedKeyword:
                        type = provider.GetPinnedType(type);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private TType ParseBeginning(ref StringSpan span)
        {
            var next = ILAsmLexer.Lex(ref span);

            switch (next.Kind)
            {
                case SyntaxKind.ExclamationToken:
                case SyntaxKind.DoubleExclamationToken:
                    var isMethod = next.Kind == SyntaxKind.DoubleExclamationToken;

                    next = ILAsmLexer.Lex(ref span);
                    if (next.Kind != SyntaxKind.NumericLiteralToken)
                    {
                        var syntax = isMethod ? "!!" : "!";
                        throw new FormatException($"Expected numeric literal to follow '{syntax}'.");
                    }

                    var index = checked((int)(uint)next.Value);

                    return isMethod
                        ? provider.GetGenericMethodParameter(index)
                        : provider.GetGenericTypeParameter(index);

                case SyntaxKind.BoolKeyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Boolean);

                case SyntaxKind.CharKeyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Char);

                case SyntaxKind.ClassKeyword:
                case SyntaxKind.ValueTypeKeyword:
                    var isValueType = next.Kind == SyntaxKind.ValueTypeKeyword;

                    next = ILAsmLexer.Lex(ref span);
                    if (next.Kind != SyntaxKind.Identifier)
                    {
                        var syntax = isValueType ? "valuetype" : "class";
                        throw new FormatException($"Expected identifier to follow '{syntax}'.");
                    }

                    return provider.GetUserDefinedType(isValueType, null, "", (string)next.Value, Array.Empty<string>());

                case SyntaxKind.Float32Keyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Single);

                case SyntaxKind.Float64Keyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Double);

                case SyntaxKind.Int8Keyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.SByte);

                case SyntaxKind.Int16Keyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Int16);

                case SyntaxKind.Int32Keyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Int32);

                case SyntaxKind.Int64Keyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Int64);

                case SyntaxKind.NativeKeyword:
                    next = ILAsmLexer.Lex(ref span);
                    switch (next.Kind)
                    {
                        case SyntaxKind.IntKeyword:
                            return provider.GetPrimitiveType(PrimitiveTypeCode.IntPtr);

                        case SyntaxKind.UnsignedKeyword:
                            next = ILAsmLexer.Lex(ref span);
                            if (next.Kind != SyntaxKind.IntKeyword)
                                throw new FormatException("Expected 'int' to follow 'native unsigned'.");

                            return provider.GetPrimitiveType(PrimitiveTypeCode.UIntPtr);

                        default:
                            throw new FormatException("Expected 'int' or 'unsigned int' to follow 'native'.");
                    }

                case SyntaxKind.ObjectKeyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Object);

                case SyntaxKind.StringKeyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.String);

                case SyntaxKind.TypedReferenceKeyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.TypedReference);

                case SyntaxKind.UnsignedKeyword:
                    next = ILAsmLexer.Lex(ref span);
                    switch (next.Kind)
                    {
                        case SyntaxKind.Int8Keyword:
                            return provider.GetPrimitiveType(PrimitiveTypeCode.Byte);

                        case SyntaxKind.Int16Keyword:
                            return provider.GetPrimitiveType(PrimitiveTypeCode.UInt16);

                        case SyntaxKind.Int32Keyword:
                            return provider.GetPrimitiveType(PrimitiveTypeCode.UInt32);

                        case SyntaxKind.Int64Keyword:
                            return provider.GetPrimitiveType(PrimitiveTypeCode.UInt64);

                        default:
                            throw new FormatException("Expected 'int8', 'int16', 'int32', or 'int64' to follow 'unsigned'.");
                    }

                case SyntaxKind.VoidKeyword:
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Void);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
