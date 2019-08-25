using NUnit.Framework;
using Shouldly;
using System;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies.Tests.ILAsmSyntax
{
    public static class ILAsmSyntaxTypeNameDecoderTests
    {
        private static readonly TestFormattingTypeProvider P = TestFormattingTypeProvider.Instance;
        private static readonly string[] Empty = Array.Empty<string>();

        private static void AssertCallTree(string syntax, string expected)
        {
            ILAsmSyntaxTypeNameDecoder.Decode(syntax, P).ShouldBe(expected);
        }

        [Test]
        public static void Generic_type_parameter([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!{index}",
                P.GetGenericTypeParameter(index));
        }

        [Test]
        public static void Generic_type_parameter_using_lowercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!0x{index:x}",
                P.GetGenericTypeParameter(index));
        }

        [Test]
        public static void Generic_type_parameter_using_uppercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!0x{index:X}",
                P.GetGenericTypeParameter(index));
        }

        [Test]
        public static void Generic_method_parameter([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!!{index}",
                P.GetGenericMethodParameter(index));
        }

        [Test]
        public static void Generic_method_parameter_using_lowercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!!0x{index:x}",
                P.GetGenericMethodParameter(index));
        }

        [Test]
        public static void Generic_method_parameter_using_uppercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!!0x{index:X}",
                P.GetGenericMethodParameter(index));
        }

        [TestCase("bool", PrimitiveTypeCode.Boolean)]
        [TestCase("char", PrimitiveTypeCode.Char)]
        [TestCase("float32", PrimitiveTypeCode.Single)]
        [TestCase("float64", PrimitiveTypeCode.Double)]
        [TestCase("int8", PrimitiveTypeCode.SByte)]
        [TestCase("int16", PrimitiveTypeCode.Int16)]
        [TestCase("int32", PrimitiveTypeCode.Int32)]
        [TestCase("int64", PrimitiveTypeCode.Int64)]
        [TestCase("native int", PrimitiveTypeCode.IntPtr)]
        [TestCase("native unsigned int", PrimitiveTypeCode.UIntPtr)]
        [TestCase("object", PrimitiveTypeCode.Object)]
        [TestCase("string", PrimitiveTypeCode.String)]
        [TestCase("typedref", PrimitiveTypeCode.TypedReference)]
        [TestCase("unsigned int8", PrimitiveTypeCode.Byte)]
        [TestCase("unsigned int16", PrimitiveTypeCode.UInt16)]
        [TestCase("unsigned int32", PrimitiveTypeCode.UInt32)]
        [TestCase("unsigned int64", PrimitiveTypeCode.UInt64)]
        [TestCase("void", PrimitiveTypeCode.Void)]
        public static void Primitives(string primitiveSyntax, PrimitiveTypeCode typeCode)
        {
            AssertCallTree(
                primitiveSyntax,
                P.GetPrimitiveType(typeCode));
        }

        [Test]
        public static void By_reference()
        {
            AssertCallTree(
                "bool&",
                P.GetByReferenceType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean)));
        }

        [Test]
        public static void Pointer()
        {
            AssertCallTree(
                "bool*",
                P.GetPointerType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean)));
        }

        [Test]
        public static void Pinned()
        {
            AssertCallTree(
                "bool pinned",
                P.GetPinnedType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean)));
        }

        [Test]
        public static void Class_reference()
        {
            AssertCallTree(
                "class Foo",
                P.GetUserDefinedType(isValueType: false, null, "", "Foo"));
        }
    }
}
