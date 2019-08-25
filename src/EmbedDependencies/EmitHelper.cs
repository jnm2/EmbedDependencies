using System;
using Mono.Cecil.Cil;

namespace Techsola.EmbedDependencies
{
    internal readonly partial struct EmitHelper
    {
        public EmitHelper(MetadataHelper metadata, ILProcessor il)
        {
            Metadata = metadata;
            IL = il ?? throw new System.ArgumentNullException(nameof(il));
        }

        public MetadataHelper Metadata { get; }
        public ILProcessor IL { get; }

        public void Call(string ilasmMethodReferenceSyntax)
        {
            var method = Metadata.GetMethodReference(ilasmMethodReferenceSyntax);

            IL.Emit(OpCodes.Call, method);
        }

        public void Callvirt(string ilasmMethodReferenceSyntax)
        {
            var method = Metadata.GetMethodReference(ilasmMethodReferenceSyntax);

            if (!method.HasThis) throw new ArgumentException(
                "Callvirt doesn't make sense with a static method. Did you mean to use 'Call' or forget the 'instance' keyword?",
                nameof(ilasmMethodReferenceSyntax));

            IL.Emit(OpCodes.Callvirt, method);
        }

        public void Newobj(string ilasmMethodReferenceSyntax)
        {
            var method = Metadata.GetMethodReference(ilasmMethodReferenceSyntax);

            if (!method.HasThis) throw new ArgumentException(
                "Newobj doesn't make sense with a static constructor. Did you forget the 'instance' keyword?",
                nameof(ilasmMethodReferenceSyntax));

            IL.Emit(OpCodes.Newobj, method);
        }
    }
}
