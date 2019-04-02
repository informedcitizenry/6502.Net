using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestConditionHandler : NUnitHandlerTestBase
    {
        public NUnitTestConditionHandler()
        {
            Handler = new ConditionHandler();
        }

        [Test]
        public void TestConditionHandlerBasic()
        {
            var testSource = new List<SourceLine>
            {
                new SourceLine { Instruction = ".if", Operand = "3 == 3" },
                new SourceLine { Instruction = "lda", Operand = "#$30" },
                new SourceLine { Instruction = ".if", Operand = "2 == 6" },
                new SourceLine { Instruction = "tax", Operand = string.Empty },
                new SourceLine { Instruction = ".elif", Operand = "9 != 1" },
                new SourceLine { Instruction = "jsr", Operand = "$ffd2" },
                new SourceLine { Instruction = ".endif", Operand = string.Empty },
                new SourceLine { Instruction = ".else", Operand = string.Empty },
                new SourceLine { Instruction = "ldx", Operand = "#$30" },
                new SourceLine { Instruction = ".if", Operand = "1 < 2" },
                new SourceLine { Instruction = "iny", Operand = string.Empty },
                new SourceLine { Instruction = ".endif", Operand = string.Empty },
                new SourceLine { Instruction = ".endif", Operand = string.Empty }
            };

            var expected = new List<SourceLine>();
            expected.Add(new SourceLine { Instruction = "lda", Operand = "#$30" });
            expected.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });

            var testProcessed = ProcessBlock(testSource).ToList();

            Assert.AreEqual(expected.Count, testProcessed.Count);
            Assert.IsTrue(expected.SequenceEqual(testProcessed));

            testSource.First().Operand = "3 == 4";
            expected.RemoveAt(1);
            expected.First().Instruction = "ldx";
            expected.Add(new SourceLine { Instruction = "iny" });

            testProcessed = ProcessBlock(testSource).ToList();

            Assert.AreEqual(expected.Count, testProcessed.Count);
            Assert.IsTrue(expected.SequenceEqual(testProcessed));
        }
    }
}
