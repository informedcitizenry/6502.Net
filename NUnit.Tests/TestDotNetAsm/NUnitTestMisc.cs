using DotNetAsm;
using NUnit.Framework;
using System;
using System.Linq;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestMisc : NUnitAsmTestBase
    {
        public NUnitTestMisc() => LineAssembler = new MiscAssembler();

        [Test]
        public void TestEnclosedInQuotes()
        {
            var test = "\"hello, world!\"";
            Assert.IsTrue(test.EnclosedInQuotes());

            test = "\"\"hello, world!";
            Assert.IsFalse(test.EnclosedInQuotes());

            test = "\"\"hello, world!\"\"";
            Assert.IsFalse(test.EnclosedInQuotes());
        }

        [Test]
        public void TestGetQuotes()
        {
            var test = ".string \"he said hello world to me\", $00";
            Assert.AreEqual("he said hello world to me", test.GetNextQuotedString());

            test = ".string \"quote 1\", \"quote 2\", 0";
            var quote2ix = test.IndexOf(',');
            Assert.AreEqual("quote 2", test.GetNextQuotedString(quote2ix));

            test = ".string \"he said, \\\"hello, world!\\\" to me\", $00";
            Assert.AreEqual("he said, \"hello, world!\" to me", test.GetNextQuotedString());
        }

        [Test]
        public void TestGetParentheses()
        {
            var test = "function(3,2,3)";
            Assert.AreEqual("(3,2,3)", test.GetNextParenEnclosure());

            test = "function(\"some string)\", ')')";
            Assert.AreEqual("(\"some string)\", ')')", test.GetNextParenEnclosure());
        }

        [Test]
        public void TestAssert()
        {
            var line = new SourceLine();
            line.LineNumber = 1;
            line.Filename = "test";
            line.Instruction = ".assert";
            line.Operand = "5 == 6";
            LineAssembler.AssembleLine(line);

            Assert.IsTrue(Assembler.Log.HasErrors);

            var error = Assembler.Log.Entries.Last();
            Assert.AreEqual("Error in file 'test' at line 1: Assertion failed: '5 == 6'", error);

            Assembler.Log.ClearAll();

            line.Operand = "5 == 6, \"My custom error!\"";
            LineAssembler.AssembleLine(line);

            Assert.IsTrue(Assembler.Log.HasErrors);
            error = Assembler.Log.Entries.Last();
            Assert.AreEqual("Error in file 'test' at line 1: My custom error!", error);

            Assembler.Log.ClearAll();

            line.Operand = "5 == 5";
            LineAssembler.AssembleLine(line);
            Assert.IsFalse(Assembler.Log.HasErrors);
        }

        [Test]
        public void TestEor()
        {
            var pseudoAsm = new PseudoAssembler(m => m.Equals(".eor") || m.Equals(".byte"), m => false);
            var line = new SourceLine
            {
                Instruction = ".eor",
                Operand = "$ff"
            };
            LineAssembler.AssembleLine(line);
            Assert.IsFalse(Assembler.Log.HasErrors);

            line.Instruction = ".byte";
            line.Operand = "$00";
            TestInstruction(line, pseudoAsm, 0x0001, 0x0001, new byte[] { 0xff });

            line.Instruction = ".eor";
            line.Operand = "-129";
            TestForFailure<OverflowException>(line);

            line.Operand = "256";
            TestForFailure<OverflowException>(line);
        }

        [Test]
        public void TestConditionals()
        {
            var line = new SourceLine();
            line.LineNumber = 1;
            line.Filename = "test";
            line.Instruction = ".errorif";
            line.Operand = "5 != 6, \"5 doesn't equal 6!\"";
            LineAssembler.AssembleLine(line);

            Assert.IsTrue(Assembler.Log.HasErrors);
            var error = Assembler.Log.Entries.Last();

            Assert.AreEqual("Error in file 'test' at line 1: 5 doesn't equal 6!", error);
            Assembler.Log.ClearAll();

            line.Operand = "5 == 6, \"5 equals 6!\"";
            LineAssembler.AssembleLine(line);

            Assert.IsFalse(Assembler.Log.HasErrors);
        }
    }
}
