using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture]
    public class NUnitTestConditionHandler : NUnitHandlerTestBase
    {
        public NUnitTestConditionHandler()
        {
            Controller = new TestController();
            Handler = new ConditionHandler(Controller);
        }

        [Test]
        public void TestConditionHandlerBasic()
        {
            var testSource = new List<SourceLine>();
            testSource.Add(new SourceLine { Instruction = ".if",    Operand = "3 == 3" });
            testSource.Add(new SourceLine { Instruction = "lda",    Operand = "#$30" });
            testSource.Add(new SourceLine { Instruction = ".if",    Operand = "2 == 6" });
            testSource.Add(new SourceLine { Instruction = "tax",    Operand = string.Empty });
            testSource.Add(new SourceLine { Instruction = ".elif",  Operand = "9 != 1" });
            testSource.Add(new SourceLine { Instruction = "jsr",    Operand = "$ffd2" });
            testSource.Add(new SourceLine { Instruction = ".endif", Operand = string.Empty });
            testSource.Add(new SourceLine { Instruction = ".else",  Operand = string.Empty });
            testSource.Add(new SourceLine { Instruction = "ldx",    Operand = "#$30" });
            testSource.Add(new SourceLine { Instruction = ".if",    Operand = "1 < 2" });
            testSource.Add(new SourceLine { Instruction = "iny",    Operand = string.Empty });
            testSource.Add(new SourceLine { Instruction = ".endif", Operand = string.Empty });
            testSource.Add(new SourceLine { Instruction = ".endif", Operand = string.Empty });

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
