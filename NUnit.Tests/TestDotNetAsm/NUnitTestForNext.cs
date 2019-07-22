using DotNetAsm;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestForNext : NUnitHandlerTestBase
    {
        public NUnitTestForNext()
        {
            Handler = new ForNextHandler();
        }

        IEnumerable<SourceLine> HandleLines(List<SourceLine> unprocessed)
        {
            for (int i = 0; i < unprocessed.Count; i++)
            {
                var line = unprocessed[i];
                if (Handler.Processes(line.Instruction) || Handler.IsProcessing())
                {
                    unprocessed.RemoveAt(i--);
                    Handler.Process(line);
                    if (!Handler.IsProcessing())
                    {
                        unprocessed.InsertRange(i + 1, Handler.GetProcessedLines());
                        Handler.Reset();
                    }
                }
            }
            return unprocessed;
        }

        [Test]
        public void TestForNextSimple()
        {
            var unprocessed = new List<SourceLine>
            {
                new SourceLine { Instruction = ".for", Operand = "i = 0, i < 3, i = i + 1" },
                new SourceLine { Instruction = "nop" },
                new SourceLine { Instruction = ".next" }
            };

            unprocessed = HandleLines(unprocessed).ToList();

            Assert.AreEqual(3, unprocessed.Count(l => l.Instruction.Equals("nop")));
        }

        [Test]
        public void TestForNextNested()
        {
            var unprocessed = new List<SourceLine>
            {
                new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" },
                new SourceLine { Instruction = "lda", Operand = "#$41+i" },
                new SourceLine { Instruction = "jsr", Operand = "$ffd2" },
                new SourceLine { Instruction = ".next" }
            };

            unprocessed = HandleLines(unprocessed).ToList();

            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("lda") && l.Operand.Equals("#$41+i")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("jsr") && l.Operand.Equals("$ffd2")));

            unprocessed.Clear();
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" });
            unprocessed.Add(new SourceLine { Instruction = "lda", Operand = "#$41+i" });
            unprocessed.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "x = 0, x < 3, x = x + 1" });
            unprocessed.Add(new SourceLine { Instruction = "nop" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = "iny" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });

            unprocessed = HandleLines(unprocessed).ToList();

            Assert.AreEqual(15, unprocessed.Count(l => l.Instruction.Equals("nop")));
        }

        [Test]
        public void TestForNextMultiNested()
        {
            var unprocessed = new List<SourceLine>();
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" });
            unprocessed.Add(new SourceLine { Instruction = "lda", Operand = "#$41+i" });
            unprocessed.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "x = 0, x < 3, x = x + 1" });
            unprocessed.Add(new SourceLine { Instruction = "nop" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = "iny" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "x = 0, x < 5, x = x + 1" });
            unprocessed.Add(new SourceLine { Instruction = "inx" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });

            unprocessed = HandleLines(unprocessed).ToList();

            Assert.AreEqual(3 * 5, unprocessed.Count(l => l.Instruction.Equals("nop")));
            Assert.AreEqual(5 * 5, unprocessed.Count(l => l.Instruction.Equals("inx")));
            Assert.AreEqual(1 * 5, unprocessed.Count(l => l.Instruction.Equals("iny")));
            Assert.AreEqual(1 * 5, unprocessed.Count(l => l.Instruction.Equals("lda")));
            Assert.AreEqual(1 * 5, unprocessed.Count(l => l.Instruction.Equals("jsr")));
            //Assert.AreEqual(11 * 5, unprocessed.Count);
        }

        [Test]
        public void TestForNextBreak()
        {
            var unprocessed = new List<SourceLine>();
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" });
            unprocessed.Add(new SourceLine { Instruction = "lda", Operand = "#$41+i" });
            unprocessed.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            unprocessed.Add(new SourceLine { Instruction = ".break" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });

            unprocessed = HandleLines(unprocessed).ToList();

            //Assert.AreEqual(2, unprocessed.Count);

            unprocessed.Clear();
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" });
            unprocessed.Add(new SourceLine { Instruction = "lda", Operand = "#$41+i" });
            unprocessed.Add(new SourceLine { Instruction = ".break" });
            unprocessed.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });

            unprocessed = HandleLines(unprocessed).ToList();

            unprocessed = new List<SourceLine>();
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" });
            unprocessed.Add(new SourceLine { Instruction = "lda", Operand = "#$41+i" });
            unprocessed.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "x = 0, x < 3, x = x + 1" });
            unprocessed.Add(new SourceLine { Instruction = "nop" });
            unprocessed.Add(new SourceLine { Instruction = ".break" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "j = 0, j < 4, j = j + 1" });
            unprocessed.Add(new SourceLine { Instruction = "inx" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = "iny" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = "tax" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });

            unprocessed = HandleLines(unprocessed).ToList();

            Assert.AreEqual(0, unprocessed.Count(l => l.Instruction.Equals("inx")));
            Assert.AreEqual(0, unprocessed.Count(l => l.Instruction.Equals("iny")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("nop")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("lda")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("jsr")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("tax")));
        }

        [Ignore("Not ready yet")]
        public void TestForNextContinue()
        {
            var unprocessed = new List<SourceLine>();
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "i = 1, i < 6, i = i + 1" });
            unprocessed.Add(new SourceLine { Instruction = "lda", Operand = "#$41+i" });
            unprocessed.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "x = 0, x < 3, x = x + 1" });
            unprocessed.Add(new SourceLine { Instruction = "nop" });
            unprocessed.Add(new SourceLine { Instruction = ".continue" });
            unprocessed.Add(new SourceLine { Instruction = ".for", Operand = "j = 0, j < 4, j = j + 1" });
            unprocessed.Add(new SourceLine { Instruction = "inx" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = "iny" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });
            unprocessed.Add(new SourceLine { Instruction = "tax" });
            unprocessed.Add(new SourceLine { Instruction = ".next" });

            unprocessed = HandleLines(unprocessed).ToList();

            Assert.AreEqual(0, unprocessed.Count(l => l.Instruction.Equals("inx")));
            Assert.AreEqual(0, unprocessed.Count(l => l.Instruction.Equals("iny")));
            Assert.AreEqual(3 * 5, unprocessed.Count(l => l.Instruction.Equals("nop")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("lda")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("jsr")));
            Assert.AreEqual(5, unprocessed.Count(l => l.Instruction.Equals("tax")));
        }
    }
}
