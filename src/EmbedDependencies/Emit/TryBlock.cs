using System;
using System.Collections.Generic;

namespace Techsola.EmbedDependencies.Emit
{
    public sealed class TryBlock
    {
        public IReadOnlyList<IProgramElement> TryContents { get; }

        public TryBlock(params IProgramElement[] tryContents)
        {
            TryContents = tryContents ?? throw new ArgumentNullException(nameof(tryContents));
        }

        public TryFinallyBlock Finally(params IProgramElement[] finallyContents)
        {
            return new TryFinallyBlock(TryContents, finallyContents);
        }
    }
}
