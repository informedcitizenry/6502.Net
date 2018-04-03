using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
     public class NUnitTestMacros : NUnitHandlerTestBase
    {
        public NUnitTestMacros()
        {
            Controller = new TestController();
            Handler = new MacroHandler(Controller, s =>
            {
                return s.Equals("lda") || s.Equals("ldy") || s.Equals("ldx") ||
                       s.Equals("inc") || s.Equals("jsr");
            });
        }

        [Test]
        public void TestMacroBasic()
        {
            var source = new List<SourceLine>();
            source.Add(new SourceLine { Label = "mymacro", Instruction = ".macro" });
            source.Add(new SourceLine { Instruction = "inc", Operand = "$d000" });
            source.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            source.Add(new SourceLine { Instruction = ".endmacro" });

            source.Add(new SourceLine { Instruction = "lda", Operand = "#$30" });
            source.Add(new SourceLine { Instruction = ".mymacro" });
            source.Add(new SourceLine { Instruction = "inc", Operand = "$d001" });

            var expected = new List<SourceLine>();
            expected.Add(new SourceLine { Instruction = "lda", Operand = "#$30" });
            expected.Add(new SourceLine { Instruction = ".block" });
            expected.Add(new SourceLine { Instruction = "inc", Operand = "$d000" });
            expected.Add(new SourceLine { Instruction = "jsr", Operand = "$ffd2" });
            expected.Add(new SourceLine { Instruction = ".endblock" });
            expected.Add(new SourceLine { Instruction = "inc", Operand = "$d001" });
            var processed = ProcessBlock(source).ToList();

            Assert.AreEqual(expected.Count, processed.Count);
            Assert.IsTrue(processed.SequenceEqual(expected));
        }

        [Test]
        public void TestMacroParameterized()
        {
            var source = new List<SourceLine>();
            source.Add(new SourceLine { Label = "mymacro", Instruction = ".macro", Operand = "incr, sub" });
            source.Add(new SourceLine { Instruction = "inc", Operand = "\\incr" });
            source.Add(new SourceLine { Instruction = "jsr", Operand = "\\sub" });
            source.Add(new SourceLine { Instruction = ".endmacro" });

            source.Add(new SourceLine { Instruction = "lda", Operand = "#$30" });
            source.Add(new SourceLine { Instruction = ".mymacro", Operand = "$d000, $ffd2" });
            source.Add(new SourceLine { Instruction = "inc", Operand = "$d001" });

            var expected = new List<SourceLine>();
            expected.Add(new SourceLine { LineNumber = 4, Instruction = "lda", Operand = "#$30" });
            expected.Add(new SourceLine { LineNumber = 0, Instruction = ".block" });
            expected.Add(new SourceLine { LineNumber = 1, Instruction = "inc", Operand = "$d000" });
            expected.Add(new SourceLine { LineNumber = 2, Instruction = "jsr", Operand = "$ffd2" });
            expected.Add(new SourceLine { LineNumber = 3, Instruction = ".endblock" });
            expected.Add(new SourceLine { LineNumber = 6, Instruction = "inc", Operand = "$d001" });

            int i = 0;
            source.ForEach(l => l.LineNumber = i++);

            var processed = ProcessBlock(source).ToList();

            Assert.AreEqual(expected.Count, processed.Count);
            Assert.IsTrue(processed.SequenceEqual(expected));

            source.Add(new SourceLine { LineNumber = i, Instruction = ".mymacro", Operand = "$c000, $0801" });

            expected.Add(new SourceLine { LineNumber = 0, Instruction = ".block" });
            expected.Add(new SourceLine { LineNumber = 1, Instruction = "inc", Operand = "$c000" });
            expected.Add(new SourceLine { LineNumber = 2, Instruction = "jsr", Operand = "$0801" });
            expected.Add(new SourceLine { LineNumber = 3, Instruction = ".endblock" });
            processed = ProcessBlock(source).ToList();

            Assert.AreEqual(expected.Count, processed.Count);
            Assert.IsTrue(processed.SequenceEqual(expected));
        }
    }
}
