using Mono.Cecil.Cil;
using System;

namespace Techsola.EmbedDependencies.Emit
{
    public readonly partial struct Emitter
    {
        private readonly struct ExceptionHandlerSpec
        {
            private readonly ExceptionHandler exceptionHandler;
            private readonly Label tryStart, tryEnd, handlerStart, handlerEnd;

            public ExceptionHandlerSpec(ExceptionHandler exceptionHandler, Label tryStart, Label tryEnd, Label handlerStart, Label handlerEnd)
            {
                this.exceptionHandler = exceptionHandler;
                this.tryStart = tryStart;
                this.tryEnd = tryEnd;
                this.handlerStart = handlerStart;
                this.handlerEnd = handlerEnd;
            }

            public void Generate(MethodBody body, Func<Label, Instruction> labelResolver)
            {
                exceptionHandler.TryStart = labelResolver.Invoke(tryStart);
                exceptionHandler.TryEnd = labelResolver.Invoke(tryEnd);
                exceptionHandler.HandlerStart = labelResolver.Invoke(handlerStart);
                exceptionHandler.HandlerEnd = labelResolver.Invoke(handlerEnd);

                body.ExceptionHandlers.Add(exceptionHandler);
            }
        }
    }
}
