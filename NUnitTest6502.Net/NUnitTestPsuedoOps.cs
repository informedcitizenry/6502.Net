using Asm6502.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUnitTest6502.Net
{
    [TestFixture]
    public class PseudoOpsTest
    {
        IAssemblyController test_;
        ILineAssembler pseudo_;

        public PseudoOpsTest()
        {
            test_ = new TestController();
            pseudo_ = new PseudoOps6502(test_);
        }

        private void TestInstruction(SourceLine line, int pc, int expectedsize, IEnumerable<byte> expected, bool positive = true)
        {
            int size = pseudo_.GetInstructionSize(line);
            pseudo_.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(test_.Log.HasErrors);
                Assert.AreEqual(pc, test_.Output.GetPC());

                if (expected != null)
                    Assert.IsTrue(test_.Output.GetCompilation().SequenceEqual(expected));
                else
                    Assert.IsTrue(test_.Output.GetCompilation().Count == 0);
                Assert.AreEqual(expectedsize, size);
            }
            else
            {
                Assert.IsTrue(test_.Log.HasErrors);
                
            }
            test_.Output.Reset();
            test_.Log.ClearErrors();
        }

        private void TestForFailure(SourceLine line)
        {
            TestInstruction(line, 0, 0, null, false);
        }

        [Test]
        public void TestMultiByte()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".byte";
            line.Operand = "$01,$02 , $03, $04, $05";
            TestInstruction(line, 0x0005, 5, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            line.Instruction = ".word";
            line.Operand = "$01,$02, $1234 , $ffff, $7323";
            TestInstruction(line, 0x000a, 10, new byte[] { 0x01, 0x00, 0x02, 0x00, 0x34, 0x12,
                                                           0xff, 0xff, 0x23, 0x73});

            line.Instruction = ".lint";
            line.Operand = "78332, ?, -33000";
            TestInstruction(line, 0x0009, 9, new byte[] { 0xfc, 0x31, 0x01,
                                                          0x00, 0x00, 0x00,
                                                          0x18, 0x7f, 0xff});

            line.Instruction = ".dword";
            line.Operand = "1,2,3,$ffd2";
            TestInstruction(line, 0x0010, 16, new byte[] { 0x01, 0x00, 0x00, 0x00,
                                                           0x02, 0x00, 0x00, 0x00,
                                                           0x03, 0x00, 0x00, 0x00,
                                                           0xd2, 0xff, 0x00, 0x00});

            line.Instruction = ".dint";
            line.Operand = "? , ?,? ,?";
            TestInstruction(line, 0x0010, 16, null);
        }

        [Test]
        public void TestByteChar()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".byte";
            line.Operand = "0,255";
            TestInstruction(line, 0x0002, 2, new byte[] { 0x00, 0xff });

            line.Operand = "-123";
            TestForFailure(line);

            line.Instruction = ".char";
            line.Operand = "-128,127";
            TestInstruction(line, 0x0002, 2, new byte[] { 0x80, 0x7f });

            line.Operand = "192";
            TestForFailure(line);
        }

        [Test]
        public void TestWordSint()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".sint";
            line.Operand = "-32768,32767";
            TestInstruction(line, 0x0004, 4, new byte[] { 0x00, 0x80, 0xff, 0x7f });

            line.Operand = "$ffd2";
            TestForFailure(line);

            line.Instruction = ".word";
            TestInstruction(line, 0x0002, 2, new byte[] { 0xd2, 0xff });

            line.Operand = "-1";
            TestForFailure(line);

            line.Instruction = ".addr";
            TestForFailure(line);

            line.Operand = "65232 , $c000";
            TestInstruction(line, 0x0004, 4, new byte[] { 0xd0, 0xfe, 0x00, 0xc0 });

            line.Operand = "$010000";
            TestForFailure(line);

            line.Instruction = ".word";
            TestForFailure(line);

            line.Instruction = ".sint";
            TestForFailure(line);
        }

        [Test]
        public void TestDwordDint()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".dint";
            line.Operand = int.MinValue.ToString() + ", " + int.MaxValue.ToString();
            TestInstruction(line, 0x0008, 8, new byte[] { 0x00, 0x00, 0x00, 0x80, 
                                                          0xff, 0xff, 0xff, 0x7f});
            line.Operand = uint.MaxValue.ToString();
            TestForFailure(line);

            line.Instruction = ".dword";
            TestInstruction(line, 0x0004, 4, new byte[] { 0xff, 0xff, 0xff, 0xff });

            line.Operand = "-32342";
            TestForFailure(line);
        }

        [Test]
        public void TestLintLong()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".lint";
            line.Operand = Int24.MinValue.ToString() + " , " + Int24.MaxValue.ToString();
            TestInstruction(line, 0x0006, 6, new byte[] { 0x00, 0x00, 0x80, 
                                                          0xff, 0xff, 0x7f});

            line.Operand = "16777215";
            TestForFailure(line);

            line.Instruction = ".long";
            TestInstruction(line, 0x0003, 3, new byte[] { 0xff, 0xff, 0xff });

            line.Operand = "$01000000";
            TestForFailure(line);
        }

        [Test]
        public void TestRta()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".rta";
            line.Operand = "$0000";
            TestInstruction(line, 0x0002, 2, new byte[] { 0xff, 0xff });

            line.Operand = "$ffff";
            TestInstruction(line, 0x0002, 2, new byte[] { 0xfe, 0xff });

            line.Operand = "?";
            TestInstruction(line, 0x0002, 2, null);
        }

        [Test]
        public void TestEnc()
        {
            string teststring = "\"hello, world\"";

            var ascbytes = Encoding.ASCII.GetBytes(teststring.Trim('"'));
            var petbytes = Encoding.ASCII.GetBytes(teststring.Trim('"').ToUpper());
            var screenbytes = petbytes.Select(b =>
                {
                    if (b >= '@' && b <= 'Z')
                        b -= 0x40;
                    return b;
                });

            SourceLine line = new SourceLine();
            line.Instruction = ".enc";
            line.Operand = "none";
            TestInstruction(line, 0x0000, 0, null);

            line.Instruction = ".string";
            line.Operand = teststring;
            TestInstruction(line, ascbytes.Count(), ascbytes.Count(), ascbytes);

            line.Instruction = ".enc";
            line.Operand = "petscii";
            TestInstruction(line, 0x0000, 0, null);

            line.Instruction = ".string";
            line.Operand = teststring;
            TestInstruction(line, petbytes.Count(), petbytes.Count(), petbytes);

            line.Instruction = ".enc";
            line.Operand = "screen";
            TestInstruction(line, 0x0000, 0, null);

            line.Instruction = ".string";
            line.Operand = teststring.ToUpper();
            TestInstruction(line, screenbytes.Count(), screenbytes.Count(), screenbytes);

            line.Instruction = ".enc";
            line.Operand = "none";
            TestInstruction(line, 0x0000, 0, null);
        }

        [Test]
        public void TestStringFormats()
        {
            string teststring = "HELLO, WORLD";
            var ascbytes = Encoding.ASCII.GetBytes(teststring);

            List<byte> test = new List<byte>();

            SourceLine line = new SourceLine();

            test.Add(0x0c);
            test.AddRange(ascbytes);
            test.AddRange(BitConverter.GetBytes(0xffd2).Take(2));

            line.Instruction = ".string";
            line.Operand = string.Format("12, \"{0}\", $ffd2", teststring);
            TestInstruction(line, 0x000f, 15, test);

            test.Clear();
            test.AddRange(BitConverter.GetBytes(-32768).Take(2));
            test.Add(0x0d);
            test.AddRange(ascbytes);
            test.Add(0);

            line.Instruction = ".cstring";
            line.Operand = string.Format("-32768, 13, \"{0}\"", teststring);
            TestInstruction(line, test.Count, test.Count, test);

            test.Clear();
            test.Add(Convert.ToByte(ascbytes.Count() + 2));
            test.AddRange(ascbytes);

            int instructionsize = 1     // total operand size byte
                                  + ascbytes.Count() // string size
                                  + 2;  // two uninitialized bytes
              

            line.Instruction = ".pstring";
            line.Operand = string.Format("\"{0}\", ?, ?", teststring);
            TestInstruction(line, instructionsize, instructionsize, test);

            test.Clear();
            test.Add(42);
            test.Add(0);
            test.Add(0);
            test.AddRange(ascbytes);
            test[test.Count - 1] |= 0x80;

            line.Instruction = ".nstring";
            line.Operand = string.Format("42, ?, ?, \"{0}\"", teststring);
            TestInstruction(line, test.Count(), test.Count(), test);

            line.Operand = line.Operand + "$80";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() =>
                TestForFailure(line));
            test_.Output.Reset();

            line.Operand = string.Format("42, ?, ?, \"{0}\", $80", teststring);
            TestForFailure(line);

            test.Clear();
            test.AddRange(BitConverter.GetBytes(0x0d0d).Take(2));
            test.AddRange(ascbytes);
            test = test.Select(b => { b <<= 1; return b; }).ToList();
            test[test.Count - 1] += 1;

            Assert.IsTrue(test_.Output.GetCompilation().Count == 0);

            line.Instruction = ".lsstring";
            line.Operand = string.Format("$0d0d, \"{0}\"", teststring);
            TestInstruction(line, test.Count(), test.Count(), test);

            line.Operand += ",$80";
            TestForFailure(line);
        }

        [Test]
        public void TestFill()
        {
            SourceLine line = new SourceLine();

            line.Instruction = ".byte";
            line.Operand = "0";
            pseudo_.AssembleLine(line);
            Assert.IsFalse(test_.Log.HasErrors);
            Assert.AreEqual(0x0001, test_.Output.GetPC());
            Assert.IsTrue(test_.Output.GetCompilation().Count == 1);

            // test uninitialized
            line.Instruction = ".fill";
            line.Operand = "10";
            TestInstruction(line, 0x000b, 10, new byte[] { 0x00 });

            // test uninitialized another way
            line.Operand = "10, ?";
            TestInstruction(line, 0x000a, 10, null);

            // test fill bytes
            line.Operand = "10, $ea";
            TestInstruction(line, 0x000a, 10, new byte[] { 0xea, 0xea, 0xea, 0xea, 0xea,
                                                           0xea, 0xea, 0xea, 0xea, 0xea});

            line.Operand = string.Empty;
            Assert.Throws<InvalidOperationException>(() => TestForFailure(line));
            test_.Output.Reset();

            line.Operand = "10, $ea, $20";
            TestForFailure(line);

        }

        [Test]
        public void TestAlign()
        {
            SourceLine line = new SourceLine();

            line.Instruction = ".byte";
            line.Operand = "0";
            line.PC = 1;
            pseudo_.AssembleLine(line);
            Assert.IsFalse(test_.Log.HasErrors);
            Assert.AreEqual(0x0001, test_.Output.GetPC());
            Assert.IsTrue(test_.Output.GetCompilation().Count == 1);

            // align to nearest page, uninitialized
            line.Instruction = ".align";
            line.Operand = "$100";
            TestInstruction(line, 0x0100, 255, new byte[] { 0x00 });

            // align to nearest page, uninitialized
            test_.Output.SetPC(1);
            line.PC = 1;
            line.Operand = "$100, ?";
            TestInstruction(line, 0x0100, 255, null);

            // align to nearest 0x10, filled
            test_.Output.SetPC(0x0006);
            line.PC = 0x0006;
            line.Operand = "$10, $ea";
            TestInstruction(line, 0x0010, 10, new byte[] { 0xea, 0xea, 0xea, 0xea, 0xea,
                                                           0xea, 0xea, 0xea, 0xea, 0xea});


            line.Operand = string.Empty;
            Assert.Throws<InvalidOperationException>(() => TestForFailure(line));
            test_.Output.Reset();

            line.Operand = "$100, $10, $02";
            TestForFailure(line);
        }

        [Test]
        public void TestRepeat()
        {
            SourceLine line = new SourceLine();

            line.Instruction = ".repeat";
            line.Operand = "4, $ffd220";
            TestInstruction(line, 0x000c, 12, new byte[] { 0x20, 0xd2, 0xff,
                                                           0x20, 0xd2, 0xff,
                                                           0x20, 0xd2, 0xff,
                                                           0x20, 0xd2, 0xff});

            // can't use repeat for uninitialized
            line.Operand = "4, ?";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() => TestForFailure(line));
            test_.Output.Reset();

            // can't have less than two args
            line.Operand = "4";
            TestForFailure(line);

            line.Operand = string.Empty;
            Assert.Throws<InvalidOperationException>(() => TestForFailure(line));
            test_.Output.Reset();
            test_.Log.ClearErrors();
        }

        [Test]
        public void TestSyntaxErrors()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".byte";
            line.Operand = "3,";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() => TestForFailure(line));

            line.Operand = "3,,2";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() => TestForFailure(line));
            test_.Output.Reset();

            line.Operand = "pow(3,2)";
            TestInstruction(line, 0x0001, 1, new byte[] { 0x09 });

            line.Instruction = ".enc";
            line.Operand = "ascii";
            TestForFailure(line);

            line.Instruction = ".char";
            line.Operand = "%34";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() => TestForFailure(line));
            test_.Output.Reset();

            line.Operand = "256%34";
            TestInstruction(line, 0x0001, 1, new byte[] { 0x12 });

            line.Operand = "%0101";
            TestInstruction(line, 0x0001, 1, new byte[] { 0x05 });

            line.Operand = "'''";
            TestInstruction(line, 0x0001, 1, new byte[] { 0x27 });

            line.Operand = "-?";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() => TestForFailure(line));
            test_.Output.Reset();

            line.Operand = "-1,?";
            TestInstruction(line, 0x0002, 2, new byte[] { 0xff });
        }
    }
}
