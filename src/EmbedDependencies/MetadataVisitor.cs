using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Techsola.EmbedDependencies
{
    public partial class MetadataVisitor
    {
        private readonly Dictionary<Handle, Handle> handleMap = new Dictionary<Handle, Handle>();

        protected MetadataVisitor(MetadataReader reader, MetadataBuilder builder)
        {
            Reader = reader;
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        protected MetadataReader Reader { get; }
        protected MetadataBuilder Builder { get; }

        public void VisitAll()
        {
            VisitAssemblyDefinition();

            foreach (var handle in Reader.AssemblyFiles)
                VisitAssemblyFile(handle);

            foreach (var handle in Reader.AssemblyReferences)
                VisitAssemblyReference(handle);

            foreach (var handle in Reader.CustomAttributes)
                VisitCustomAttribute(handle);

            foreach (var handle in Reader.CustomDebugInformation)
                VisitCustomDebugInformation(handle);

            // ...
        }

        private Handle MapHandle(Handle readerHandle, Func<Handle, Handle> visitor)
        {
            if (readerHandle.IsNil) return default;

            if (!handleMap.TryGetValue(readerHandle, out var mappedHandle))
                handleMap.Add(readerHandle, mappedHandle = visitor.Invoke(mappedHandle));

            return mappedHandle;
        }

        public StringHandle MapString(StringHandle readerHandle)
        {
            return (StringHandle)MapHandle(readerHandle, h =>
                Builder.GetOrAddString(Reader.GetString((StringHandle)h)));
        }

        public GuidHandle MapGuid(GuidHandle readerHandle)
        {
            return (GuidHandle)MapHandle(readerHandle, h =>
                Builder.GetOrAddGuid(Reader.GetGuid((GuidHandle)h)));
        }

        public BlobHandle MapBlob(BlobHandle readerHandle)
        {
            return (BlobHandle)MapHandle(readerHandle, h =>
                Builder.GetOrAddBlob(Reader.GetBlobBytes((BlobHandle)h)));
        }

        public EntityHandle MapEntityHandle(EntityHandle readerHandle)
        {
            return readerHandle.Kind switch
            {
                HandleKind.AssemblyDefinition => (EntityHandle)EntityHandle.AssemblyDefinition,
                HandleKind.AssemblyFile => MapAssemblyFile((AssemblyFileHandle)readerHandle),
                HandleKind.AssemblyReference => MapAssemblyReference((AssemblyReferenceHandle)readerHandle),
                HandleKind.CustomAttribute => MapCustomAttribute((CustomAttributeHandle)readerHandle),
                HandleKind.CustomDebugInformation => MapCustomDebugInformation((CustomDebugInformationHandle)readerHandle),
                _ => throw new NotImplementedException()
            };
        }

        protected virtual AssemblyDefinitionHandle VisitAssemblyDefinition()
        {
            var assemblyDefinition = Reader.GetAssemblyDefinition();

            return Builder.AddAssembly(
                MapString(assemblyDefinition.Name),
                assemblyDefinition.Version,
                MapString(assemblyDefinition.Culture),
                MapBlob(assemblyDefinition.PublicKey),
                assemblyDefinition.Flags,
                assemblyDefinition.HashAlgorithm);
        }

        public AssemblyFileHandle MapAssemblyFile(AssemblyFileHandle readerHandle)
        {
            return (AssemblyFileHandle)MapHandle(readerHandle, h => VisitAssemblyFile((AssemblyFileHandle)h));
        }

        protected virtual AssemblyFileHandle VisitAssemblyFile(AssemblyFileHandle readerHandle)
        {
            var assemblyFile = Reader.GetAssemblyFile(readerHandle);

            return Builder.AddAssemblyFile(
                MapString(assemblyFile.Name),
                MapBlob(assemblyFile.HashValue),
                assemblyFile.ContainsMetadata);
        }

        public AssemblyReferenceHandle MapAssemblyReference(AssemblyReferenceHandle readerHandle)
        {
            return (AssemblyReferenceHandle)MapHandle(readerHandle, h => VisitAssemblyReference((AssemblyReferenceHandle)h));
        }

        protected virtual AssemblyReferenceHandle VisitAssemblyReference(AssemblyReferenceHandle readerHandle)
        {
            var assemblyReference = Reader.GetAssemblyReference(readerHandle);

            return Builder.AddAssemblyReference(
                MapString(assemblyReference.Name),
                assemblyReference.Version,
                MapString(assemblyReference.Culture),
                MapBlob(assemblyReference.PublicKeyOrToken),
                assemblyReference.Flags,
                MapBlob(assemblyReference.HashValue));
        }

        public CustomAttributeHandle MapCustomAttribute(CustomAttributeHandle readerHandle)
        {
            return (CustomAttributeHandle)MapHandle(readerHandle, h => VisitCustomAttribute((CustomAttributeHandle)h));
        }

        protected virtual CustomAttributeHandle VisitCustomAttribute(CustomAttributeHandle readerHandle)
        {
            var customAttribute = Reader.GetCustomAttribute(readerHandle);

            var decoded = customAttribute.DecodeValue(new TypeModelProvider());
            var blobBuilder = new BlobBuilder();
            EncodeAttributeValue(decoded, blobBuilder);

            return Builder.AddCustomAttribute(
                MapEntityHandle(customAttribute.Parent),
                MapEntityHandle(customAttribute.Constructor),
                Builder.GetOrAddBlob(blobBuilder));
        }

        private static void EncodeAttributeValue(CustomAttributeValue<TypeModel> decoded, BlobBuilder blobBuilder)
        {
            new BlobEncoder(blobBuilder).CustomAttributeSignature(out var fixedArguments, out var namedArguments);

            foreach (var argument in decoded.FixedArguments)
            {
                var literalEncoder = fixedArguments.AddArgument();

                EncodeLiteral(literalEncoder, argument.Type, argument.Value);
            }

            if (decoded.NamedArguments.Any())
            {
                var namedArgumentsEncoder = namedArguments.Count(decoded.NamedArguments.Length);
                foreach (var argument in decoded.NamedArguments)
                {
                    namedArgumentsEncoder.AddArgument(argument.Kind == CustomAttributeNamedArgumentKind.Field, out var typeEncoder, out var nameEncoder, out var literalEncoder);

                    if (argument.Type.IsPrimitiveType(out var code))
                    {
                        typeEncoder.ScalarType().PrimitiveType(PrimitiveTypeCode code);
                    }
                    else if (argument.Type.IsSystemType)
                    {
                        typeEncoder.ScalarType().SystemType();
                    }
                    else if (argument.Type.IsSZArrayType(out var elementType))
                    {
                        throw new NotImplementedException();
                    }

                    nameEncoder.Name(argument.Name);

                    EncodeLiteral(literalEncoder, argument.Type, argument.Value);
                }
            }
        }

        private static void EncodeLiteral(LiteralEncoder literalEncoder, TypeModel type, object value)
        {
            if (type.IsPrimitiveType(out _))
            {
                literalEncoder.Scalar().Constant(value);
            }
            else if (type.IsSystemType)
            {
                literalEncoder.Scalar().SystemType((string)value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public CustomDebugInformationHandle MapCustomDebugInformation(CustomDebugInformationHandle readerHandle)
        {
            return (CustomDebugInformationHandle)MapHandle(readerHandle, h => VisitCustomDebugInformation((CustomDebugInformationHandle)h));
        }

        protected virtual CustomDebugInformationHandle VisitCustomDebugInformation(CustomDebugInformationHandle readerHandle)
        {
            var customDebugInformation = Reader.GetCustomDebugInformation(readerHandle);

            return Builder.AddCustomDebugInformation(
                MapEntityHandle(customDebugInformation.Parent),
                MapGuid(customDebugInformation.Kind),
                MapBlob(customDebugInformation.Value)); // Maybe this becomes invalid and should be dropped if not parsed?
        }
    }
}
