using NUnit.Framework;
using Asm6502.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitTest6502.Net
{
    [TestFixture]
    public class SourceLineTest
    {
        [Test]
        public void TestParse()
        {
            SourceLine line = new SourceLine("myfile", 1, ".text \"He said, \",'\"',\"Hello, World!\"");
            line.Parse(s => s.Equals(".text"), s => false);
            Assert.AreEqual(".text", line.Instruction);
            Assert.AreEqual("\"He said, \",'\"',\"Hello, World!\"", line.Operand);

            line.SourceString = "\"hello\" = \"32\"";
            line.Label = line.Instruction = line.Operand = string.Empty;
            line.Parse(s => false, s => false);

            Assert.AreEqual(string.Empty, line.Label);
        }

        [Test]
        public void TestParseComment()
        {
            SourceLine line = new SourceLine("myfile", 1, "mylabel .byte 1,2,3;,4,5");
            line.Parse(s => s.Equals(".byte"), s => s.Equals("mylabel"));
            Assert.AreEqual(line.Label, "mylabel");
            Assert.AreEqual(line.Instruction, ".byte");
            Assert.AreEqual(line.Operand, "1,2,3");

            // reset
            line.SourceString = "mylabel .byte 1,2,';',3";
            line.Label = line.Instruction = line.Operand = string.Empty;

            line.Parse(instr => instr.Equals(".byte"), lbl => lbl.Equals("mylabel"));
            Assert.AreEqual(line.Operand, "1,2,';',3");
        }

        [Test]
        public void TestCsv()
        {
            SourceLine line = new SourceLine();
            line.Operand = "147, \"He said, \",'\"',\"Hello, World!\",'\"', $0d, ','";
            var csv = line.CommaSeparateOperand();

            Assert.IsTrue(csv.Count == 7);
            Assert.AreEqual(csv[0], "147");
            Assert.AreEqual(csv[1], "\"He said, \"");
            Assert.AreEqual(csv[2], "'\"'");
            Assert.AreEqual(csv[3], "\"Hello, World!\"");
            Assert.AreEqual(csv[4], "'\"'");
            Assert.AreEqual(csv[5], "$0d");
            Assert.AreEqual(csv[6], "','");
        }

        [Test]
        public void TestFlags()
        {
            SourceLine line = new SourceLine();

            line.IsComment = true;
            // setting comment flag sets DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            // resetting DoNotAssemble has no effect if comment flag is true
            line.DoNotAssemble = false;
            Assert.AreEqual(true, line.DoNotAssemble);

            line.IsComment = false;
            // resetting comment flag should not reset DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            // reset DoNotAssemble
            line.DoNotAssemble = false;
            // and check!
            Assert.AreEqual(false, line.DoNotAssemble);

            line.IsDefinition = true;
            // setting definition flag sets DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            line.IsDefinition = false;
            // resetting definition flag should not reset DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            // reset all flags
            line.DoNotAssemble = false;
            // and check!
            Assert.AreEqual(false, line.IsComment);
            Assert.AreEqual(false, line.IsDefinition);
            Assert.AreEqual(false, line.DoNotAssemble);

            line.IsDefinition = true;
            line.IsComment = true;
            line.IsDefinition = false;
            // cannot reset definition flag if comment flag is set
            Assert.AreEqual(true, line.IsDefinition);
        }

        [Test]
        public void TestScope()
        {
            SourceLine line = new SourceLine();
            line.Scope = "MYOUTERBLOCK.MYINNERBLOCK.MYNORMAL@";

            string scope = line.GetScope(false);
            Assert.AreEqual(scope, line.Scope);
            Assert.IsTrue(scope.Contains("MYNORMAL"));

            string modscope = line.GetScope(true);
            Assert.AreNotEqual(scope, modscope);
            Assert.IsFalse(modscope.Contains("MYNORMAL"));
        }
    }
}
