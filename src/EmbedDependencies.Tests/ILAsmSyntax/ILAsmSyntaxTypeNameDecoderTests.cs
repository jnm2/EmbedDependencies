using NUnit.Framework;
using Shouldly;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies.Tests.ILAsmSyntax
{
    public static class ILAsmSyntaxTypeNameDecoderTests
    {
        private static void AssertCallTree(string syntax, string expected)
        {
            ILAsmSyntaxTypeNameDecoder.Decode(syntax, TestFormattingTypeProvider.Instance).ShouldBe(expected);
        }

        [Test]
        public static void Generic_type_parameter([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree($"!{index}", $"GetGenericTypeParameter({index})");
        }

        [Test]
        public static void Generic_type_parameter_using_lowercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree($"!0x{index:x}", $"GetGenericTypeParameter({index})");
        }

        [Test]
        public static void Generic_type_parameter_using_uppercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree($"!0x{index:X}", $"GetGenericTypeParameter({index})");
        }

        [Test]
        public static void Generic_method_parameter([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree($"!!{index}", $"GetGenericMethodParameter({index})");
        }

        [Test]
        public static void Generic_method_parameter_using_lowercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree($"!!0x{index:x}", $"GetGenericMethodParameter({index})");
        }

        [Test]
        public static void Generic_method_parameter_using_uppercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree($"!!0x{index:X}", $"GetGenericMethodParameter({index})");
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
            AssertCallTree(primitiveSyntax, $"GetPrimitiveType({typeCode})");
        }

        [Test]
        public static void By_reference()
        {
            AssertCallTree("bool&", "GetByReferenceType(GetPrimitiveType(Boolean))");
        }

        [Test]
        public static void Pointer()
        {
            AssertCallTree("bool*", "GetPointerType(GetPrimitiveType(Boolean))");
        }

        [Test]
        public static void Pinned()
        {
            AssertCallTree("bool pinned", "GetPinnedType(GetPrimitiveType(Boolean))");
        }

        [Test]
        public static void Class_reference()
        {
            AssertCallTree("class Foo", "GetUserDefinedType(isValueType: false, assemblyName: null, namespaceName: \"\", topLevelTypeName: \"Foo\", nestedTypeNames: Array.Empty<string>())");
        }
    }
}
