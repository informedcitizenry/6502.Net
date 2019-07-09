using DotNetAsm;
using NUnit.Framework;
using System.Collections.Generic;

namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture]
    public abstract class NUnitHandlerTestBase
    {
        protected NUnitHandlerTestBase()
        {
            Assembler.Initialize();

            Assembler.Evaluator.DefineParser((arg) =>
                Assembler.Symbols.TranslateExpressionSymbols(new SourceLine(), arg, string.Empty, false));
        }

        protected IBlockHandler Handler { get; set; }

        protected IEnumerable<SourceLine> ProcessBlock(IEnumerable<SourceLine> testSource)
        {
            var testProcessed = new List<SourceLine>();
            foreach (var line in testSource)
            {
                if (Handler.Processes(line.Instruction) ||
                    Handler.IsProcessing())
                {
                    Handler.Process(line);
                    if (Handler.IsProcessing() == false)
                        testProcessed.AddRange(Handler.GetProcessedLines());
                }
                else
                {
                    testProcessed.Add(line);
                }
            }
            Handler.Reset();
            return testProcessed;
        }
    }
}
