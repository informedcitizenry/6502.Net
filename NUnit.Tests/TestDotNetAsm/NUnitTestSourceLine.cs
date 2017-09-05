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

            line.SourceString = "*=something";
            line.Label = line.Instruction = line.Operand = string.Empty;
            line.Parse(s => s.Equals("="), s => s.Equals("*"));

            Assert.AreEqual("*", line.Label);
            Assert.AreEqual("=", line.Instruction);
            Assert.AreEqual("something", line.Operand);

            line.Label = line.Instruction = line.Operand = string.Empty;
            line.SourceString = "*= something";
            line.Parse(s => false, s => false);

            Assert.AreEqual("*", line.Label);
            Assert.AreEqual("=", line.Instruction);
            Assert.AreEqual("something", line.Operand);

            line.Label = line.Instruction = line.Operand = string.Empty;
            line.SourceString = "mylabel =something";
            line.Parse(s => false, s => s.Equals("mylabel"));

            Assert.AreEqual("mylabel", line.Label);
            Assert.AreEqual("=", line.Instruction);
            Assert.AreEqual("something", line.Operand);
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

            // reset all flags
            line.DoNotAssemble = false;
            // and check!
            Assert.AreEqual(false, line.IsComment);
            Assert.AreEqual(false, line.DoNotAssemble);
        }
    }
}
