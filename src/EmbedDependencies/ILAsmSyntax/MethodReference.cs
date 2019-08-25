using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies.ILAsmSyntax
{
    public readonly struct MethodReference<TType>
    {
        public MethodReference(bool instance, bool instanceExplicit, TType returnType, TType declaringType, string methodName, IReadOnlyList<TType> genericArguments, IReadOnlyList<TType> parameters)
        {
            Instance = instance;
            InstanceExplicit = instanceExplicit;
            ReturnType = returnType;
            DeclaringType = declaringType;
            MethodName = methodName;
            GenericArguments = genericArguments ?? Array.Empty<TType>();
            Parameters = parameters ?? Array.Empty<TType>();
        }

        public bool Instance { get; }
        public bool InstanceExplicit { get; }
        public TType ReturnType { get; }
        public TType DeclaringType { get; }
        public string MethodName { get; }
        public IReadOnlyList<TType> GenericArguments { get; }
        public IReadOnlyList<TType> Parameters { get; }
    }
}
