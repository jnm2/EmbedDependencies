using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies.Emit
{
    public sealed class TryFinallyBlock : IProgramElement
    {
        public TryFinallyBlock(IReadOnlyList<IProgramElement> tryContents, IReadOnlyList<IProgramElement> finallyContents)
        {
            TryContents = tryContents ?? throw new ArgumentNullException(nameof(tryContents));
            FinallyContents = finallyContents ?? throw new ArgumentNullException(nameof(finallyContents));
        }

        public IReadOnlyList<IProgramElement> TryContents { get; }
        public IReadOnlyList<IProgramElement> FinallyContents { get; }
    }
}
