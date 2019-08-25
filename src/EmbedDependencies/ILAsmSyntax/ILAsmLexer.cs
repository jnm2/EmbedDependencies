using System;
using System.Collections.Generic;
using System.Globalization;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    internal sealed class ILAsmLexer
    {
        // If entries change, update the switch in the Lex method so that the appropriate first letters are assigned to
        // ReadIdentifier and ReadKeywordOrIdentifier.
        private static readonly Dictionary<string, SyntaxKind> KeywordsStartingWithPossibleIdentifierChar = new Dictionary<string, SyntaxKind>
        {
            ["bool"] = SyntaxKind.BoolKeyword,
            ["char"] = SyntaxKind.CharKeyword,
            ["class"] = SyntaxKind.ClassKeyword,
            ["default"] = SyntaxKind.DefaultKeyword,
            ["explicit"] = SyntaxKind.ExplicitKeyword,
            ["float32"] = SyntaxKind.Float32Keyword,
            ["float64"] = SyntaxKind.Float64Keyword,
            ["instance"] = SyntaxKind.InstanceKeyword,
            ["int"] = SyntaxKind.IntKeyword,
            ["int8"] = SyntaxKind.Int8Keyword,
            ["int16"] = SyntaxKind.Int16Keyword,
            ["int32"] = SyntaxKind.Int32Keyword,
            ["int64"] = SyntaxKind.Int64Keyword,
            ["method"] = SyntaxKind.MethodKeyword,
            ["modopt"] = SyntaxKind.ModoptKeyword,
            ["modreq"] = SyntaxKind.ModreqKeyword,
            ["native"] = SyntaxKind.NativeKeyword,
            ["object"] = SyntaxKind.ObjectKeyword,
            ["pinned"] = SyntaxKind.PinnedKeyword,
            ["string"] = SyntaxKind.StringKeyword,
            ["typedref"] = SyntaxKind.TypedReferenceKeyword,
            ["unmanaged"] = SyntaxKind.UnmanagedKeyword,
            ["unsigned"] = SyntaxKind.UnsignedKeyword,
            ["valuetype"] = SyntaxKind.ValueTypeKeyword,
            ["vararg"] = SyntaxKind.VarargKeyword,
            ["void"] = SyntaxKind.VoidKeyword
        };

        private SyntaxToken? peekedToken;

        public SyntaxKind PeekedTokenKind
        {
            get => peekedToken?.Kind ?? throw new InvalidOperationException("No token is currently peeked.");
        }

        public SyntaxKind PeekKind(ref StringSpan span)
        {
            if (peekedToken != null) return peekedToken.Value.Kind;

            var token = Lex(ref span);
            peekedToken = token;
            return token.Kind;
        }

        public void DiscardPeekedToken()
        {
            if (peekedToken is null) throw new InvalidOperationException("There is no peeked token.");
            peekedToken = null;
        }

        public SyntaxToken Lex(ref StringSpan span)
        {
            if (peekedToken != null)
            {
                var value = peekedToken.Value;
                peekedToken = null;
                return value;
            }

            while (span.Length > 0)
            {
                var c = span[0];
                switch (c)
                {
                    case '!':
                        span = span.Slice(1);
                        return TryRead(ref span, '!')
                            ? SyntaxKind.DoubleExclamationToken
                            : SyntaxKind.ExclamationToken;

                    case '&':
                        span = span.Slice(1);
                        return SyntaxKind.AmpersandToken;

                    case '*':
                        span = span.Slice(1);
                        return SyntaxKind.AsteriskToken;

                    case '(':
                        span = span.Slice(1);
                        return SyntaxKind.OpenParenToken;

                    case ')':
                        span = span.Slice(1);
                        return SyntaxKind.CloseParenToken;

                    case '<':
                        span = span.Slice(1);
                        return SyntaxKind.OpenAngleToken;

                    case '>':
                        span = span.Slice(1);
                        return SyntaxKind.CloseAngleToken;

                    case '[':
                        span = span.Slice(1);
                        return SyntaxKind.OpenSquareToken;

                    case '.':
                        span = span.Slice(1);
                        return
                            TryRead(ref span, "..") ? SyntaxKind.EllipsisToken :
                            TryRead(ref span, "cctor") ? SyntaxKind.DotCctorKeyword :
                            TryRead(ref span, "ctor") ? SyntaxKind.DotCtorKeyword :
                            TryRead(ref span, "module") ? SyntaxKind.DotModuleKeyword :
                            SyntaxKind.DotToken;

                    case ',':
                        span = span.Slice(1);
                        return SyntaxKind.CommaToken;

                    case ']':
                        span = span.Slice(1);
                        return SyntaxKind.CloseSquareToken;

                    case '/':
                        span = span.Slice(1);
                        return SyntaxKind.SlashToken;

                    case ':':
                        var advanced = span.Slice(1);
                        if (TryRead(ref advanced, ':'))
                        {
                            span = advanced;
                            return SyntaxKind.DoubleColonToken;
                        }
                        goto default;

                    case '0':
                        advanced = span.Slice(1);
                        if (TryRead(ref advanced, 'x'))
                        {
                            span = advanced;
                            return ReadHexValue(ref span);
                        }
                        goto case '1';
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return ReadDecimalValue(ref span);

                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'i':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                        return ReadKeywordOrIdentifier(ref span);

                    case 'a':
                    case 'g':
                    case 'h':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'q':
                    case 'r':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case '_':
                    case '$':
                    case '@':
                    case '`':
                    case '?':
                        return ReadIdentifier(ref span);

                    case '"':
                    case '\'':
                        throw new NotImplementedException();

                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                    case '\v':
                        span = span.Slice(1);
                        break;

                    default:
                        return SyntaxKind.Error;
                }
            }

            return SyntaxKind.End;
        }

        private static SyntaxToken ReadKeywordOrIdentifier(ref StringSpan span)
        {
            var value = ReadKeywordOrIdentifierValue(ref span);

            return KeywordsStartingWithPossibleIdentifierChar.TryGetValue(value, out var kind)
                ? kind
                : new SyntaxToken(SyntaxKind.Identifier, value);
        }

        private static SyntaxToken ReadIdentifier(ref StringSpan span)
        {
            return new SyntaxToken(SyntaxKind.Identifier, ReadKeywordOrIdentifierValue(ref span));
        }

        private static string ReadKeywordOrIdentifierValue(ref StringSpan span)
        {
            // Assume the only reason this method has been called is that span[0] is a valid first char for an identifier.
            var index = 1;

            while (index < span.Length && SyntaxFacts.IsIdentifierChar(span[index]))
            {
                index++;
            }

            var value = span.Slice(0, index).ToString();
            span = span.Slice(index);
            return value;
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

        private static SyntaxToken ReadDecimalValue(ref StringSpan span)
        {
            var index = 0;

            while (index < span.Length && SyntaxFacts.IsDecimalChar(span[index]))
            {
                index++;
            }

            if (index > 0)
            {
                var value = uint.Parse(span.Slice(0, index).ToString(), NumberStyles.None, CultureInfo.InvariantCulture);
                span = span.Slice(index);
                return new SyntaxToken(SyntaxKind.NumericLiteralToken, value);
            }

            return SyntaxKind.Error;
        }

        private static SyntaxToken ReadHexValue(ref StringSpan span)
        {
            var index = 0;

            while (index < span.Length && SyntaxFacts.IsHexChar(span[index]))
            {
                index++;
            }

            if (index > 0)
            {
                var value = uint.Parse(span.Slice(0, index).ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                span = span.Slice(index);
                return new SyntaxToken(SyntaxKind.NumericLiteralToken, value);
            }

            return SyntaxKind.Error;
        }
    }
}
