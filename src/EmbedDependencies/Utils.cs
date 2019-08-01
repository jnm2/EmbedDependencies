using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Techsola.EmbedDependencies
{
    internal static class Utils
    {
        public static void InsertEmbeddedAssemblyResolver(Stream stream)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            using (var peReader = new PEReader(stream))
            {
                var builder = new MetadataBuilder();

                new InsertModuleInitializerMetadataVisitor(peReader.GetMetadataReader(), builder).VisitAll();


            }
        }
    }
}
