using System.Collections.Generic;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    public interface IILAsmTypeNameSyntaxTypeProvider<TType>
    {
        TType GetGenericTypeParameter(int index);
        TType GetGenericMethodParameter(int index);
        TType GetPrimitiveType(PrimitiveTypeCode typeCode);
        TType GetUserDefinedType(bool isValueType, string assemblyName, string namespaceName, string topLevelTypeName, IReadOnlyList<string> nestedTypeNames);
        TType GetByReferenceType(TType elementType);
        TType GetPointerType(TType elementType);
        TType GetGenericInstantiation(TType genericType, IReadOnlyList<TType> typeArguments);
        TType GetArrayType(TType elementType, int rank);
        TType GetPinnedType(TType elementType);
    }
}
