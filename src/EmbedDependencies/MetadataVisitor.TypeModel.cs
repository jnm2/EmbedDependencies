using System.Reflection.Metadata;

namespace Techsola.EmbedDependencies
{
    public partial class MetadataVisitor
    {
        /// <summary>
        /// Discriminated union representing the hierarchy of calls to <see cref="ICustomAttributeTypeProvider{TType}"/>.
        /// </summary>
        private sealed class TypeModel
        {
            private readonly int kind;
            private readonly PrimitiveTypeCode typeCode;
            private readonly TypeModel elementType;
            private readonly TypeDefinitionHandle typeDefinitionHandle;
            private readonly TypeReferenceHandle typeReferenceHandle;
            private readonly byte rawTypeKind;
            private readonly string name;

            private TypeModel(
                int kind,
                PrimitiveTypeCode typeCode = default,
                TypeModel elementType = default,
                TypeDefinitionHandle typeDefinitionHandle = default,
                TypeReferenceHandle typeReferenceHandle = default,
                byte rawTypeKind = default,
                string name = default)
            {
                this.kind = kind;
                this.typeCode = typeCode;
                this.elementType = elementType;
                this.typeDefinitionHandle = typeDefinitionHandle;
                this.typeReferenceHandle = typeReferenceHandle;
                this.rawTypeKind = rawTypeKind;
                this.name = name;
            }

            public static TypeModel PrimitiveType(PrimitiveTypeCode typeCode)
            {
                return new TypeModel(kind: 0, typeCode);
            }

            public static TypeModel SystemType { get; } = new TypeModel(kind: 1);

            public static TypeModel SZArrayType(TypeModel elementType) => new TypeModel(kind: 2, elementType: elementType);

            public static TypeModel TypeFromDefinition(TypeDefinitionHandle handle, byte rawTypeKind) => new TypeModel(kind: 3, typeDefinitionHandle: handle, rawTypeKind: rawTypeKind);

            public static TypeModel TypeFromReference(TypeReferenceHandle handle, byte rawTypeKind) => new TypeModel(kind: 4, typeReferenceHandle: handle, rawTypeKind: rawTypeKind);

            public static TypeModel TypeFromSerializedName(string name) => new TypeModel(kind: 5, name: name);

            public bool IsPrimitiveType(out PrimitiveTypeCode typeCode)
            {
                typeCode = this.typeCode;
                return kind == 0;
            }

            public bool IsSystemType => kind == 1;

            public bool IsSZArrayType(out TypeModel elementType)
            {
                elementType = this.elementType;
                return kind == 2;
            }

            public bool IsTypeFromDefinition(out TypeDefinitionHandle handle, out byte rawTypeKind)
            {
                handle = typeDefinitionHandle;
                rawTypeKind = this.rawTypeKind;
                return kind == 3;
            }

            public bool IsTypeFromReference(out TypeReferenceHandle handle, out byte rawTypeKind)
            {
                handle = typeReferenceHandle;
                rawTypeKind = this.rawTypeKind;
                return kind == 4;
            }

            public bool IsTypeFromSerializedName(out string name)
            {
                name = this.name;
                return kind == 5;
            }
        }
    }
}
