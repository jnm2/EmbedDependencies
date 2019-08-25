using System.Collections.Generic;
using System.Linq;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies.Tests.ILAsmSyntax
{
    internal sealed class TestFormattingTypeProvider : IILAsmTypeNameSyntaxTypeProvider<string>
    {
        public static TestFormattingTypeProvider Instance { get; } = new TestFormattingTypeProvider();

        private TestFormattingTypeProvider() { }

        public string GetGenericTypeParameter(int index)
        {
            return $"GetGenericTypeParameter({index})";
        }

        public string GetGenericMethodParameter(int index)
        {
            return $"GetGenericMethodParameter({index})";
        }

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return $"GetPrimitiveType({typeCode})";
        }

        public string GetUserDefinedType(bool isValueType, string assemblyName, string namespaceName, string topLevelTypeName, IReadOnlyList<string> nestedTypeNames)
        {
            return $"GetUserDefinedType(isValueType: {FormatBooleanLiteral(isValueType)}, assemblyName: {FormatStringLiteral(assemblyName)}, namespaceName: {FormatStringLiteral(namespaceName)}, topLevelTypeName: {FormatStringLiteral(topLevelTypeName)}, nestedTypeNames: {FormatArrayLiteral(nestedTypeNames)})";
        }

        public string GetByReferenceType(string elementType)
        {
            return $"GetByReferenceType({elementType})";
        }

        public string GetPointerType(string elementType)
        {
            return $"GetPointerType({elementType})";
        }

        public string GetGenericInstantiation(string genericType, IReadOnlyList<string> typeArguments)
        {
            return $"GetGenericInstantiation({genericType}, {FormatArrayLiteral(typeArguments)})";
        }

        public string GetArrayType(string elementType, int rank)
        {
            return $"GetArrayType({elementType}, rank: {rank})";
        }

        public string GetPinnedType(string elementType)
        {
            return $"GetPinnedType({elementType})";
        }

        private static string FormatBooleanLiteral(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatStringLiteral(string value)
        {
            return value is null ? "null" : '"' + value + '"';
        }

        private static string FormatArrayLiteral(IReadOnlyList<string> elements)
        {
            return elements.Any()
                ? "new[] { " + string.Join(", ", elements.Select(FormatStringLiteral)) + " }"
                : "Array.Empty<string>()";
        }
    }
}
