namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    public readonly struct FieldReference<TType>
    {
        public FieldReference(TType fieldType, TType declaringType, string fieldName)
        {
            FieldType = fieldType;
            DeclaringType = declaringType;
            FieldName = fieldName;
        }

        public TType FieldType { get; }
        public TType DeclaringType { get; }
        public string FieldName { get; }
    }
}
