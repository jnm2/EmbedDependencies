using Mono.Cecil;

namespace Techsola.EmbedDependencies.Emit
{
    public interface ISyntaxProvider
    {
        TypeReference GetTypeReference(string syntax);
        FieldReference GetFieldReference(string syntax);
        MethodReference GetMethodReference(string syntax);
    }
}
