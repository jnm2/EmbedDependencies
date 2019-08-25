using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    public static class ILAsmParser
    {
        public static TType ParseType<TType>(string typeSyntax, IILAsmTypeSyntaxTypeProvider<TType> provider)
        {
            return new ILAsmParser<TType>(provider).ParseType(typeSyntax);
        }

        public static FieldReference<TType> ParseFieldReference<TType>(string fieldReferenceSyntax, IILAsmTypeSyntaxTypeProvider<TType> provider)
        {
            return new ILAsmParser<TType>(provider).ParseFieldReference(fieldReferenceSyntax);
        }

        public static MethodReference<TType> ParseMethodReference<TType>(string methodReferenceSyntax, IILAsmTypeSyntaxTypeProvider<TType> provider)
        {
            return new ILAsmParser<TType>(provider).ParseMethodReference(methodReferenceSyntax);
        }
    }

    internal sealed class ILAsmParser<TType>
    {
        private readonly IILAsmTypeSyntaxTypeProvider<TType> provider;
        private readonly ILAsmLexer lexer = new ILAsmLexer();

        public ILAsmParser(IILAsmTypeSyntaxTypeProvider<TType> provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public TType ParseType(string typeSyntax)
        {
            var span = (StringSpan)typeSyntax;

            var result = ParseType(ref span);

            if (result.PeekedTokenMessage != null)
            {
                if (lexer.PeekedTokenKind == SyntaxKind.End)
                    throw new ArgumentException("Type syntax must be specified.", nameof(typeSyntax));

                throw new FormatException(result.PeekedTokenMessage);
            }

            return result.Value.Value;
        }

        public FieldReference<TType> ParseFieldReference(string fieldReferenceSyntax)
        {
            // See ECMA 335 page 513, VI.C.4.9 'Instructions that take a field of a class as an argument'

            // <type> <typeSpec> :: <id>

            var span = (StringSpan)fieldReferenceSyntax;

            var result = ParseType(ref span);
            if (!result.Value.IsSome(out var fieldType))
            {
                if (lexer.PeekedTokenKind == SyntaxKind.End)
                    throw new ArgumentException("Field reference syntax must be specified.", nameof(fieldReferenceSyntax));

                throw new FormatException(result.PeekedTokenMessage);
            }

            result = ParseTypeSpec(ref span);
            if (!result.Value.IsSome(out var declaringType))
                throw new FormatException(result.PeekedTokenMessage);

            var next = lexer.Lex(ref span);
            if (next.Kind != SyntaxKind.DoubleColonToken)
                throw new FormatException("Expected '::' following type spec.");

            next = lexer.Lex(ref span);
            if (next.Kind != SyntaxKind.Identifier)
                throw new FormatException("Expected identifier following '::'.");

            return new FieldReference<TType>(fieldType, declaringType, (string)next.Value);
        }

        public MethodReference<TType> ParseMethodReference(string methodReferenceSyntax)
        {
            // See ECMA 335 page 513, VI.C.4.8 'Instructions that take a method as an argument'

            // <callConv> <type> [ <typeSpec> :: ] <methodName> ( <parameters> )

            var span = (StringSpan)methodReferenceSyntax;

            if (lexer.PeekKind(ref span) == SyntaxKind.End)
                throw new ArgumentException("Method reference syntax must be specified.", nameof(methodReferenceSyntax));

            ParseCallingConvention(ref span, out var instance, out var instanceExplicit);

            var result = ParseType(ref span);
            if (!result.Value.IsSome(out var returnType))
                throw new FormatException(result.PeekedTokenMessage);

            result = ParseTypeSpec(ref span);
            if (!result.Value.IsSome(out var declaringType))
                throw new FormatException(result.PeekedTokenMessage);

            var next = lexer.Lex(ref span);
            if (next.Kind != SyntaxKind.DoubleColonToken)
            {
                throw new NotSupportedException($"The {nameof(MethodReference<TType>)} type does not support method references without the type spec and '::' token.");
            }

            var methodName = ParseMethodName(ref span);

            return new MethodReference<TType>(
                instance,
                instanceExplicit,
                returnType,
                declaringType,
                methodName,
                genericArguments: Array.Empty<TType>(),
                parameters: Array.Empty<TType>());
        }

        private string ParseMethodName(ref StringSpan span)
        {
            // See ECMA 335 page 179, II.15.4 'Defining methods'

            // MethodName ::=
            //   .cctor
            // | .ctor
            // | <DottedName>

            switch (lexer.PeekKind(ref span))
            {
                case SyntaxKind.DotCctorKeyword:
                    lexer.DiscardPeekedToken();
                    return ".cctor";

                case SyntaxKind.DotCtorKeyword:
                    lexer.DiscardPeekedToken();
                    return ".ctor";

                case SyntaxKind.Identifier:
                    return ParseDottedName(ref span);

                default:
                    throw new FormatException("Expected '.cctor', '.ctor', or identifier following '::'.");
            }
        }

        private string ParseDottedName(ref StringSpan span)
        {
            // See ECMA 335 page 110, II.5.3 'Identifiers'

            // DottedName ::= <Id> [ '.' <Id> ]*

            var parts = new List<string>();

            while (true)
            {
                var next = lexer.Lex(ref span);
                if (next.Kind != SyntaxKind.Identifier)
                    throw new FormatException("Expected identifier.");

                parts.Add((string)next.Value);

                if (lexer.PeekKind(ref span) != SyntaxKind.DotToken) break;
                lexer.DiscardPeekedToken();
            }

            return string.Join(".", parts);
        }

        private void ParseCallingConvention(ref StringSpan span, out bool isInstance, out bool isInstanceExplicit)
        {
            // See ECMA 335 page 178, II.15.3 'Calling convention'

            // CallConv ::= [ instance [ explicit ] ] [ CallKind ]

            // CallKind ::=
            //   default
            // | unmanaged cdecl
            // | unmanaged fastcall
            // | unmanaged stdcall
            // | unmanaged thiscall
            // | vararg

            var nextKind = lexer.PeekKind(ref span);

            isInstance = nextKind == SyntaxKind.InstanceKeyword;
            if (isInstance)
            {
                lexer.DiscardPeekedToken();

                isInstanceExplicit = lexer.PeekKind(ref span) == SyntaxKind.ExplicitKeyword;
                if (isInstanceExplicit)
                    lexer.DiscardPeekedToken();

                nextKind = lexer.PeekKind(ref span);
            }
            else
            {
                isInstanceExplicit = false;
            }

            switch (nextKind)
            {
                case SyntaxKind.DefaultKeyword:
                    lexer.DiscardPeekedToken();
                    break;

                case SyntaxKind.VarargKeyword:
                case SyntaxKind.UnmanagedKeyword:
                    throw new NotSupportedException($"The {nameof(MethodReference<TType>)} type does not support calling conventions other than 'default'.");
            }
        }

        private ParseResult<TType> ParseTypeSpec(ref StringSpan span)
        {
            // See ECMA 335 page 123, II.7.1 'Types'

            // <TypeSpec> ::=
            //   '[' [ .module ] <DottedName> ']'
            // | <TypeReference>
            // | <Type>

            switch (lexer.PeekKind(ref span))
            {
                case SyntaxKind.OpenSquareToken:
                    throw new NotSupportedException($"Scope type spec is not currently supported by {nameof(IILAsmTypeSyntaxTypeProvider<TType>)}.");

                case SyntaxKind.Identifier:
                    return ParseTypeReference(ref span, isValueType: null);

                default:
                    return ParseType(ref span);
            }
        }

        private ParseResult<TType> ParseType(ref StringSpan span)
        {
            // See ECMA 335 page 122, II.7.1 'Types'

            var result = ParseTypeKeyword(ref span);
            if (!result.Value.IsSome(out var type)) return result;

            while (true)
            {
                switch (lexer.PeekKind(ref span))
                {
                    case SyntaxKind.End:
                        lexer.DiscardPeekedToken();
                        return type;

                    case SyntaxKind.AmpersandToken:
                        lexer.DiscardPeekedToken();
                        type = provider.GetByReferenceType(type);
                        break;

                    case SyntaxKind.AsteriskToken:
                        lexer.DiscardPeekedToken();
                        type = provider.GetPointerType(type);
                        break;

                    case SyntaxKind.PinnedKeyword:
                        lexer.DiscardPeekedToken();
                        type = provider.GetPinnedType(type);
                        break;

                    case SyntaxKind.OpenSquareToken:
                        lexer.DiscardPeekedToken();
                        var rank = ReadArrayRank(ref span);
                        type = provider.GetArrayType(type, rank);
                        break;

                    case SyntaxKind.OpenAngleToken:
                        lexer.DiscardPeekedToken();
                        var arguments = new List<TType>();

                        while (true)
                        {
                            result = ParseType(ref span);
                            if (!result.Value.IsSome(out var argument))
                                throw new FormatException(result.PeekedTokenMessage);

                            arguments.Add(argument);

                            var next = lexer.Lex(ref span);
                            switch (next.Kind)
                            {
                                case SyntaxKind.CloseAngleToken:
                                    break;

                                case SyntaxKind.CommaToken:
                                    continue;

                                default:
                                    throw new FormatException("Expected ',' or '>'.");
                            }

                            break;
                        }

                        type = provider.GetGenericInstantiation(type, arguments);
                        break;

                    default:
                        return new ParseResult<TType>(type, "Expected '&', '*', 'pinned', '[', '<', or end to follow type.");
                }
            }
        }

        private int ReadArrayRank(ref StringSpan span)
        {
            var rank = 1;

            while (true)
            {
                var next = lexer.Lex(ref span);
                switch (next.Kind)
                {
                    case SyntaxKind.CloseSquareToken:
                        break;

                    case SyntaxKind.CommaToken:
                        rank++;
                        continue;

                    case SyntaxKind.EllipsisToken:
                    case SyntaxKind.NumericLiteralToken:
                        throw new NotSupportedException($"Specifying array bounds is not supported by {nameof(IILAsmTypeSyntaxTypeProvider<TType>)}.");

                    default:
                        throw new FormatException("Expected ',', ']', '...', or Int32 literal.");
                }

                break;
            }

            return rank;
        }

        private ParseResult<TType> ParseTypeKeyword(ref StringSpan span)
        {
            switch (lexer.PeekKind(ref span))
            {
                case SyntaxKind.ExclamationToken:
                case SyntaxKind.DoubleExclamationToken:
                    var isMethod = lexer.PeekedTokenKind == SyntaxKind.DoubleExclamationToken;
                    lexer.DiscardPeekedToken();

                    var next = lexer.Lex(ref span);
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
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Boolean);

                case SyntaxKind.CharKeyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Char);

                case SyntaxKind.ClassKeyword:
                case SyntaxKind.ValueTypeKeyword:
                    var isValueType = lexer.PeekedTokenKind == SyntaxKind.ValueTypeKeyword;
                    lexer.DiscardPeekedToken();
                    return ParseTypeReference(ref span, isValueType);

                case SyntaxKind.Float32Keyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Single);

                case SyntaxKind.Float64Keyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Double);

                case SyntaxKind.Int8Keyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.SByte);

                case SyntaxKind.Int16Keyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Int16);

                case SyntaxKind.Int32Keyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Int32);

                case SyntaxKind.Int64Keyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Int64);

                case SyntaxKind.NativeKeyword:
                    lexer.DiscardPeekedToken();

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
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Object);

                case SyntaxKind.StringKeyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.String);

                case SyntaxKind.TypedReferenceKeyword:
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.TypedReference);

                case SyntaxKind.UnsignedKeyword:
                    lexer.DiscardPeekedToken();

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
                    lexer.DiscardPeekedToken();
                    return provider.GetPrimitiveType(PrimitiveTypeCode.Void);

                case SyntaxKind.ModoptKeyword:
                case SyntaxKind.ModreqKeyword:
                    throw new NotSupportedException($"Custom modifiers are not currently supported by {nameof(IILAsmTypeSyntaxTypeProvider<TType>)}.");

                case SyntaxKind.MethodKeyword:
                    throw new NotSupportedException($"Method pointers are not currently supported by {nameof(IILAsmTypeSyntaxTypeProvider<TType>)}.");

                default:
                    return new ParseResult<TType>("Expected valid type keyword.");
            }
        }

        private TType ParseTypeReference(ref StringSpan span, bool? isValueType)
        {
            ParseTopLevelName(ref span,
                out var assemblyName,
                out var namespaceName,
                out var topLevelTypeName,
                out var isSlashTokenPeeked);

            var nestedNames = isSlashTokenPeeked
                ? ParseNestedNames(ref span)
                : Array.Empty<string>();

            return provider.GetTypeFromReference(
                isValueType,
                assemblyName,
                namespaceName,
                topLevelTypeName,
                nestedNames ?? Array.Empty<string>());
        }

        private void ParseTopLevelName(ref StringSpan span, out string assemblyName, out string namespaceName, out string topLevelTypeName, out bool isSlashTokenPeeked)
        {
            var namespaceSegments = new List<string>();
            var next = lexer.Lex(ref span);
            switch (next.Kind)
            {
                case SyntaxKind.OpenSquareToken:
                    switch (lexer.PeekKind(ref span))
                    {
                        case SyntaxKind.Identifier:
                            break;

                        case SyntaxKind.DotModuleKeyword:
                            throw new NotSupportedException($"'.module' syntax is not currently supported by {nameof(IILAsmTypeSyntaxTypeProvider<TType>)}.");

                        default:
                            throw new FormatException("Expected identifier or '.module' to follow '['.");
                    }

                    assemblyName = ParseDottedName(ref span);

                    next = lexer.Lex(ref span);
                    if (next.Kind != SyntaxKind.CloseSquareToken)
                        throw new FormatException("Expected '.' or ']' to follow identifier.");

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
                    throw new FormatException($"Expected identifier or '['.");
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
            var nestedNames = new List<string>();

            do
            {
                lexer.DiscardPeekedToken();

                if (nestedNames is null) nestedNames = new List<string>();
                nestedNames.Add(ParseDottedName(ref span));
            }
            while (lexer.PeekKind(ref span) == SyntaxKind.SlashToken);

            return nestedNames;
        }
    }
}
