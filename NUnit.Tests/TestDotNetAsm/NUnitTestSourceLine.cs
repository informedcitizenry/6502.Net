using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    public class SourceLineTest
    {
        
        [Test]
        public void TestParse()
        {
            var line = new SourceLine("myfile", 1, ".text \"He said, \",'\"',\"Hello, World!\"");
            line.Parse(s => s.Equals(".text"));
            Assert.AreEqual(".text", line.Instruction);
            Assert.AreEqual("\"He said, \",'\"',\"Hello, World!\"", line.Operand);

            line.SourceString = "*=something";
            line.Parse(s => false);

            Assert.AreEqual("*", line.Label);
            Assert.AreEqual("=", line.Instruction);
            Assert.AreEqual("something", line.Operand);

            line.SourceString = "*= something";
            line.Parse(s => false);

            Assert.AreEqual("*", line.Label);
            Assert.AreEqual("=", line.Instruction);
            Assert.AreEqual("something", line.Operand);

            line.SourceString = "mylabel =something";
            line.Parse(s => false);

            Assert.AreEqual("mylabel", line.Label);
            Assert.AreEqual("=", line.Instruction);
            Assert.AreEqual("something", line.Operand);

            line.SourceString = "   .block";
            line.Parse(s => s.Equals(".block"));
            Assert.IsTrue(string.IsNullOrEmpty(line.Label));
            Assert.AreEqual(".block", line.Instruction);
            Assert.IsTrue(string.IsNullOrEmpty(line.Operand));

            line.SourceString = "label .block";
            line.Parse(s => s.Equals(".block"));

            Assert.AreEqual("label", line.Label);
            Assert.AreEqual(".block", line.Instruction);
            Assert.IsTrue(string.IsNullOrEmpty(line.Operand));

            line.SourceString = "    .segment   code";
            line.Parse(s => s.Equals(".segment"));

            Assert.IsTrue(string.IsNullOrEmpty(line.Label));
            Assert.AreEqual(".segment", line.Instruction);
            Assert.AreEqual("code", line.Operand);

            line.SourceString = "            .BYTE TASKS.PRNTMAZE    ; DRAW MAZE";
            line.Parse(s => s.Equals(".BYTE") ||
                System.Text.RegularExpressions.Regex.IsMatch(s, @"^\.[a-zA-Z][a-zA-Z0-9]*$"));

            Assert.IsTrue(string.IsNullOrEmpty(line.Label));
            Assert.AreEqual(".BYTE", line.Instruction);
            Assert.AreEqual("TASKS.PRNTMAZE", line.Operand);

            line.SourceString = "            .BYTE %10000000 | MESSAGES.READY";
            line.Parse(s => s.Equals(".BYTE") ||
                System.Text.RegularExpressions.Regex.IsMatch(s, @"^\.[a-zA-Z][a-zA-Z0-9]*$"));

            Assert.IsTrue(string.IsNullOrEmpty(line.Label));
            Assert.AreEqual(".BYTE", line.Instruction);
            Assert.AreEqual("%10000000 | MESSAGES.READY", line.Operand);

            line.SourceString = "            and a               ; is a == 0?";
            line.Parse(s => s.Equals("and") ||
                System.Text.RegularExpressions.Regex.IsMatch(s, @"^\.[a-zA-Z][a-zA-Z0-9]*$"));

            Assert.IsTrue(string.IsNullOrEmpty(line.Label));
            Assert.AreEqual("and", line.Instruction);
            Assert.AreEqual("a", line.Operand);

            line.SourceString = " - ";
            line.Parse(s => false);

            Assert.IsFalse(string.IsNullOrEmpty(line.Label));
            Assert.AreEqual("-", line.Label);
            Assert.IsTrue(string.IsNullOrEmpty(line.Instruction));
            Assert.IsTrue(string.IsNullOrEmpty(line.Operand));
        }

        [Test]
        public void TestParseComment()
        {
            var line = new SourceLine("myfile", 1, "mylabel .byte 1,2,3;,4,5");
            line.Parse(s => s.Equals(".byte"));
            Assert.AreEqual(line.Label, "mylabel");
            Assert.AreEqual(line.Instruction, ".byte");
            Assert.AreEqual(line.Operand, "1,2,3");

            // reset
            line.SourceString = "mylabel .byte 1,2,';',3";

            line.Parse(instr => instr.Equals(".byte"));
            Assert.AreEqual(line.Operand, "1,2,';',3");

            line.SourceString = "    clc    ; Clear carry first!";
            line.Label = line.Instruction = line.Operand = string.Empty;
            line.Parse(s => s.Equals("clc"));

            Assert.IsTrue(string.IsNullOrEmpty(line.Label));
            Assert.IsTrue(string.IsNullOrEmpty(line.Operand));
            Assert.AreEqual("clc", line.Instruction);
        }

        [Test]
        public void TestParseCompound()
        {
            var line = new SourceLine();
            line.SourceString = "lda #$41:jsr $ffd2";
            var compounds = line.Parse(instr => instr.Equals("lda") || instr.Equals("jsr"));
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$41", line.Operand);
            Assert.AreEqual(2, compounds.Count());
            var firstCompound = compounds.Last();
            Assert.AreEqual("jsr", firstCompound.Instruction);
            Assert.AreEqual("$ffd2", firstCompound.Operand);

            line.Instruction = line.Operand = string.Empty;

            line.SourceString = "mylabel lda #$41 : jsr $ffd2";
            compounds = line.Parse(instr => instr.Equals("lda") || instr.Equals("jsr"));
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$41", line.Operand);
            Assert.AreEqual(2, compounds.Count());
            firstCompound = compounds.Last();
            Assert.AreEqual("jsr", firstCompound.Instruction);
            Assert.AreEqual("$ffd2", firstCompound.Operand);
            Assert.AreEqual("mylabel", line.Label);
            Assert.IsTrue(string.IsNullOrEmpty(firstCompound.Label));

            line.Instruction = line.Operand = line.Label = string.Empty;
            line.SourceString = "mylabel lda #$41; jsr $ffd2:rts";
            compounds = line.Parse(instr => instr.Equals("lda") || instr.Equals("jsr"));
            Assert.AreEqual("mylabel", line.Label);
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$41", line.Operand);
            Assert.IsTrue(compounds.Count() == 1);

            line.Instruction = line.Operand = line.Label = string.Empty;
            line.SourceString = "mylabel lda #$41: jsr $ffd2: rts";
            compounds = line.Parse(instr => instr.Equals("lda") || instr.Equals("jsr") || instr.Equals("rts"));
            Assert.AreEqual("mylabel", line.Label);
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$41", line.Operand);
            Assert.AreEqual(3, compounds.Count());
            firstCompound = compounds.ToList().ElementAt(1);
            Assert.AreEqual("jsr", firstCompound.Instruction);
            Assert.AreEqual("$ffd2", firstCompound.Operand);
            var lastCompound = compounds.Last();
            Assert.AreEqual("rts", lastCompound.Instruction);
            Assert.IsTrue(string.IsNullOrEmpty(lastCompound.Operand));

            line.Instruction = line.Operand = line.Label = string.Empty;
            line.SourceString = "  mylabel   lda   #':'  jsr   $ffd2:rts";
            compounds = line.Parse(instr => instr.Equals("lda") || instr.Equals("jsr") || instr.Equals("rts"));
            Assert.AreEqual("mylabel", line.Label);
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#':'  jsr   $ffd2", line.Operand);
            Assert.AreEqual(2, compounds.Count());
            firstCompound = compounds.Last();
            Assert.AreEqual("rts", firstCompound.Instruction);
            Assert.IsTrue(string.IsNullOrEmpty(firstCompound.Operand));

            line.Instruction = line.Operand = line.Label = string.Empty;
            line.SourceString = "mylabel:    jsr $ffd2";
            compounds = line.Parse(instr => instr.Equals("lda"));
            Assert.AreEqual("mylabel", line.Label);
            Assert.IsTrue(string.IsNullOrEmpty(line.Instruction));
            Assert.IsTrue(string.IsNullOrEmpty(line.Operand));
            Assert.AreEqual(2, compounds.Count());
            firstCompound = compounds.Last();
            Assert.AreEqual("jsr", firstCompound.Instruction);
            Assert.AreEqual("$ffd2", firstCompound.Operand);

            line.Instruction = line.Operand = line.Label = string.Empty;
            line.SourceString = "mylabel:    rts:jsr $ffd2";
            compounds = line.Parse(instr => instr.Equals("lda") || instr.Equals("jsr") || instr.Equals("rts"));
            Assert.AreEqual("mylabel", line.Label);
            Assert.IsTrue(string.IsNullOrEmpty(line.Instruction));
            Assert.IsTrue(string.IsNullOrEmpty(line.Operand));
            Assert.AreEqual(3, compounds.Count());
            firstCompound = compounds.ToList().ElementAt(1);
            Assert.AreEqual("rts", firstCompound.Instruction);
            Assert.IsTrue(string.IsNullOrEmpty(firstCompound.Operand));
            lastCompound = compounds.Last();
            Assert.AreEqual("jsr", lastCompound.Instruction);
            Assert.AreEqual("$ffd2", lastCompound.Operand);
        }

        [Test]
        public void TestCsv()
        {
            var line = new SourceLine();
            line.Operand = "147, \"He said, \",'\"',\"Hello, World!\",'\"', $0d, ','";
            var csv = line.Operand.CommaSeparate();

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
            var line = new SourceLine();

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
