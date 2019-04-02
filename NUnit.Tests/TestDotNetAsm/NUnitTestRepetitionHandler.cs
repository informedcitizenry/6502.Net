using DotNetAsm;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestRepetitionHandler : NUnitHandlerTestBase
    {
        [Test]
        public void TestSimple()
        {
            var source = new List<SourceLine>();
            source.Add(new SourceLine());
            source.First().Instruction = ".repeat";
            source.First().Operand = "5";
            source.Add(new SourceLine());
            source.Last().Instruction = "nop";
            source.Add(new SourceLine());
            source.Last().Instruction = ".endrepeat";

            var handler = new RepetitionHandler();

            foreach(SourceLine line in source)
            {
                handler.Process(line);
            }

            var processed = new List<SourceLine>();
            processed.AddRange(handler.GetProcessedLines());

            Assert.AreEqual(5, processed.Count);
        }

        [Test]
        public void TestMixed()
        {
            var source = new List<SourceLine>();
            source.Add(new SourceLine { Instruction = "lda", Operand = "#$41" });
            source.Add(new SourceLine { Instruction = ".repeat", Operand = "2" });
            source.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2 " });
            source.Add(new SourceLine { Instruction = "tax" });
            source.Add(new SourceLine { Instruction = "inx" });
            source.Add(new SourceLine { Instruction = "txa" });
            source.Add(new SourceLine { Instruction = ".endrepeat" });
            source.Add(new SourceLine { Instruction = "rts" });

            var processed = new List<SourceLine>();
            var handler = new RepetitionHandler();
            
            foreach(SourceLine line in source)
            {
                if (handler.Processes(line.Instruction) || handler.IsProcessing())
                {
                    handler.Process(line);
                    if (handler.IsProcessing() == false)
                    {
                        processed.AddRange(handler.GetProcessedLines());
                    }
                }
                else
                {
                    processed.Add(line);
                }
            }

            Assert.AreEqual(10, processed.Count);
            Assert.AreEqual(2, processed.Count(s => s.Instruction.Equals("jsr")));
        }

        [Test]
        public void TestNestedRepititions()
        {
            var source = new List<SourceLine>();
            
            source.Add(new SourceLine { Instruction = ".repeat", Operand = "2" });
            source.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2 " });
            source.Add(new SourceLine { Instruction = "tax" });
            source.Add(new SourceLine { Instruction = ".repeat", Operand = "3" });
            source.Add(new SourceLine { Instruction = "inx" });
            source.Add(new SourceLine { Instruction = ".endrepeat" });
            source.Add(new SourceLine { Instruction = "txa" });
            source.Add(new SourceLine { Instruction = ".endrepeat" });
            source.Add(new SourceLine { Instruction = "rts" });

            var processed = new List<SourceLine>();
            var handler = new RepetitionHandler();

            foreach (SourceLine line in source)
            {
                if (handler.Processes(line.Instruction) || handler.IsProcessing())
                {
                    handler.Process(line);
                    if (handler.IsProcessing() == false)
                    {
                        processed.AddRange(handler.GetProcessedLines());
                    }
                }
                else
                {
                    processed.Add(line);
                }
            }

            Assert.AreEqual(13, processed.Count);

            processed.Clear();
            handler.Reset();
            source.Clear();

            source.Add(new SourceLine { Instruction = ".repeat", Operand = "2" });
            source.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2 " });
            source.Add(new SourceLine { Instruction = "tax" });
            source.Add(new SourceLine { Instruction = ".repeat", Operand = "3" });
            source.Add(new SourceLine { Instruction = "inx" });
            source.Add(new SourceLine { Instruction = ".repeat", Operand = "4" });
            source.Add(new SourceLine { Instruction = "nop" });
            source.Add(new SourceLine { Instruction = ".endrepeat" });
            source.Add(new SourceLine { Instruction = ".endrepeat" });
            source.Add(new SourceLine { Instruction = "txa" });
            source.Add(new SourceLine { Instruction = ".endrepeat" });

            /*
                jsr $ffd2   1
                tax         2
                inx         3
                nop         4
                nop         5
                nop         6
                nop         7
                inx         8
                nop         9
                nop         10
                nop         11
                nop         12
                inx         13
                nop         14
                nop         15
                nop         16
                nop         17
                txa         18
               
                repeat =    36
            */

            foreach (SourceLine line in source)
            {
                if (handler.Processes(line.Instruction) || handler.IsProcessing())
                {
                    handler.Process(line);
                    if (handler.IsProcessing() == false)
                    {
                        processed.AddRange(handler.GetProcessedLines());
                    }
                }
                else
                {
                    processed.Add(line);
                }
            }

            Assert.AreEqual(36, processed.Count);
        }

        [Test]
        public void TestErrors()
        {
            var line = new SourceLine { Instruction = ".repeat" };
            var handler = new RepetitionHandler();
            handler.Process(line);

            Assert.IsTrue(Assembler.Log.HasErrors);
        }
    }
}
