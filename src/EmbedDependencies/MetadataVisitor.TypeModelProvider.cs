using System;
using System.Reflection.Metadata;

namespace Techsola.EmbedDependencies
{
    public partial class MetadataVisitor
    {
        private sealed class TypeModelProvider : ICustomAttributeTypeProvider<TypeModel>
        {
            public TypeModel GetPrimitiveType(PrimitiveTypeCode typeCode) => TypeModel.PrimitiveType(typeCode);

            public TypeModel GetSystemType() => TypeModel.SystemType;

            public TypeModel GetSZArrayType(TypeModel elementType) => TypeModel.SZArrayType(elementType);

            public TypeModel GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => TypeModel.TypeFromDefinition(handle, rawTypeKind);

            public TypeModel GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => TypeModel.TypeFromReference(handle, rawTypeKind);

            public TypeModel GetTypeFromSerializedName(string name) => TypeModel.TypeFromSerializedName(name);

            public PrimitiveTypeCode GetUnderlyingEnumType(TypeModel type)
            {
                if (type.IsPrimitiveType(out var code)) return code;

                throw new NotImplementedException();
            }

            public bool IsSystemType(TypeModel type)
            {
                if (type.IsSystemType) return true;
                if (type.IsPrimitiveType(out _) || type.IsSZArrayType(out _)) return false;

                throw new NotImplementedException();
            }
        }
    }
}
