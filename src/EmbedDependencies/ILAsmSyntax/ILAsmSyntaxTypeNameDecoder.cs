﻿using System;
using System.Collections.Generic;

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
        private readonly ILAsmLexer lexer = new ILAsmLexer();

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
                var next = lexer.Lex(ref span);

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
            var next = lexer.Lex(ref span);

            switch (next.Kind)
            {
                case SyntaxKind.ExclamationToken:
                case SyntaxKind.DoubleExclamationToken:
                    var isMethod = next.Kind == SyntaxKind.DoubleExclamationToken;

                    next = lexer.Lex(ref span);
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
                    return ParseUserDefinedType(ref span, next.Kind == SyntaxKind.ValueTypeKeyword);

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
                    next = lexer.Lex(ref span);
                    switch (next.Kind)
                    {
                        case SyntaxKind.IntKeyword:
                            return provider.GetPrimitiveType(PrimitiveTypeCode.IntPtr);

                        case SyntaxKind.UnsignedKeyword:
                            next = lexer.Lex(ref span);
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
                    next = lexer.Lex(ref span);
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

        private TType ParseUserDefinedType(ref StringSpan span, bool isValueType)
        {
            ParseTopLevelName(ref span,
                isValueType,
                out var assemblyName,
                out var namespaceName,
                out var topLevelTypeName,
                out var isSlashTokenPeeked);

            var nestedNames = isSlashTokenPeeked
                ? ParseNestedNames(ref span)
                : Array.Empty<string>();

            return provider.GetUserDefinedType(
                isValueType,
                assemblyName,
                namespaceName,
                topLevelTypeName,
                nestedNames ?? Array.Empty<string>());
        }

        private void ParseTopLevelName(ref StringSpan span, bool isValueType, out string assemblyName, out string namespaceName, out string topLevelTypeName, out bool isSlashTokenPeeked)
        {
            var namespaceSegments = new List<string>();
            var next = lexer.Lex(ref span);
            switch (next.Kind)
            {
                case SyntaxKind.OpenSquareToken:
                    next = lexer.Lex(ref span);
                    switch (next.Kind)
                    {
                        case SyntaxKind.Identifier:
                            break;

                        case SyntaxKind.DotModuleKeyword:
                            throw new NotSupportedException($"'.module' syntax is not currently supported by {nameof(IILAsmTypeNameSyntaxTypeProvider<TType>)}.");

                        default:
                            throw new FormatException("Expected identifier or '.module' to follow '['.");
                    }

                    var assemblyNameParts = new List<string> { (string)next.Value };

                    while (true)
                    {
                        next = lexer.Lex(ref span);
                        switch (next.Kind)
                        {
                            case SyntaxKind.CloseSquareToken:
                                break;

                            case SyntaxKind.DotToken:
                                next = lexer.Lex(ref span);
                                if (next.Kind != SyntaxKind.Identifier)
                                    throw new FormatException("Expected identifier to follow '.'.");

                                assemblyNameParts.Add((string)next.Value);
                                continue;

                            default:
                                throw new FormatException("Expected '.' or ']' to follow identifier.");
                        }

                        break;
                    }

                    assemblyName = string.Join(".", assemblyNameParts);

                    next = lexer.Lex(ref span);
                    if (next.Kind != SyntaxKind.Identifier)
                        throw new FormatException("Expected identifier to follow ']'.");

                    topLevelTypeName = (string)next.Value;
                    break;

                case SyntaxKind.Identifier:
                    assemblyName = null;
                    topLevelTypeName = (string)next.Value;
                    break;

                default:
                    var syntax = isValueType ? "valuetype" : "class";
                    throw new FormatException($"Expected identifier or '[' to follow '{syntax}'.");
            }

            isSlashTokenPeeked = false;

            while (true)
            {
                switch (lexer.PeekKind(ref span))
                {
                    case SyntaxKind.DotToken:
                        lexer.DiscardPeekedToken();

                        next = lexer.Lex(ref span);
                        if (next.Kind != SyntaxKind.Identifier)
                            throw new FormatException($"Expected identifier to follow '.'.");

                        namespaceSegments.Add(topLevelTypeName);
                        topLevelTypeName = (string)next.Value;
                        continue;

                    case SyntaxKind.SlashToken:
                        isSlashTokenPeeked = true;
                        break;
                }

                break;
            }

            namespaceName = string.Join(".", namespaceSegments);
        }

        private IReadOnlyList<string> ParseNestedNames(ref StringSpan span)
        {
            var isSlashTokenPeeked = true;
            var nestedNames = new List<string>();

            while (isSlashTokenPeeked)
            {
                lexer.DiscardPeekedToken();
                isSlashTokenPeeked = false;

                var next = lexer.Lex(ref span);
                if (next.Kind != SyntaxKind.Identifier)
                    throw new FormatException("Expected identifier to follow '/'.");

                if (nestedNames is null) nestedNames = new List<string>();
                var dottedSegments = new List<string> { (string)next.Value };

                while (true)
                {
                    switch (lexer.PeekKind(ref span))
                    {
                        case SyntaxKind.DotToken:
                            lexer.DiscardPeekedToken();
                            next = lexer.Lex(ref span);
                            if (next.Kind != SyntaxKind.Identifier)
                                throw new FormatException("Expected identifier to follow '.'.");

                            dottedSegments.Add((string)next.Value);
                            continue;

                        case SyntaxKind.SlashToken:
                            isSlashTokenPeeked = true;
                            break;
                    }

                    break;
                }

                nestedNames.Add(string.Join(".", dottedSegments));
            }

            return nestedNames;
        }
    }
}
