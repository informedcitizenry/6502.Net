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
    public class PseudoOpsTest
    {
        IAssemblyController test_;
        ILineAssembler pseudo_;

        public PseudoOpsTest()
        {
            test_ = new TestController();
            pseudo_ = new PseudoOps6502(test_);
        }

        private void TestInstruction(SourceLine line, int pc, IEnumerable<byte> expected, bool positive = true)
        {
            pseudo_.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(test_.Log.HasErrors);
                Assert.AreEqual(pc, test_.Output.GetPC());

                if (expected != null)
                    Assert.IsTrue(test_.Output.GetCompilation().SequenceEqual(expected));
                else
                    Assert.IsTrue(test_.Output.GetCompilation().Count == 0);
            }
            else
            {
                Assert.IsTrue(test_.Log.HasErrors);
                
            }
            test_.Output.Reset();
            test_.Log.ClearErrors();
        }

        [Test]
        public void TestMultiByte()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".byte";
            line.Operand = "$01,$02 , $03, $04, $05";
            TestInstruction(line, 0x0005, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            line.Instruction = ".word";
            line.Operand = "$01,$02, $1234 , $ffff, $7323";
            TestInstruction(line, 0x000a, new byte[] { 0x01, 0x00, 0x02, 0x00, 0x34, 0x12,
                                                       0xff, 0xff, 0x23, 0x73});

            line.Instruction = ".lint";
            line.Operand = "78332, ?, -33000";
            TestInstruction(line, 0x0009, new byte[] { 0xfc, 0x31, 0x01,
                                                       0x00, 0x00, 0x00,
                                                       0x18, 0x7f, 0xff});

            line.Instruction = ".dword";
            line.Operand = "1,2,3,$ffd2";
            TestInstruction(line, 0x0010, new byte[] { 0x01, 0x00, 0x00, 0x00,
                                                       0x02, 0x00, 0x00, 0x00,
                                                       0x03, 0x00, 0x00, 0x00,
                                                       0xd2, 0xff, 0x00, 0x00});

            line.Instruction = ".dint";
            line.Operand = "? , ?,? ,?";
            TestInstruction(line, 0x0010, null);
        }

        [Test]
        public void TestByteChar()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".byte";
            line.Operand = "0,255";
            TestInstruction(line, 0x0002, new byte[] { 0x00, 0xff });

            line.Operand = "-123";
            TestInstruction(line, 0, null, false);

            line.Instruction = ".char";
            line.Operand = "-128,127";
            TestInstruction(line, 0x0002, new byte[] { 0x80, 0x7f });

            line.Operand = "192";
            TestInstruction(line, 0, null, false);
        }

        [Test]
        public void TestWordSint()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".sint";
            line.Operand = "-32768,32767";
            TestInstruction(line, 0x0004, new byte[] { 0x00, 0x80, 0xff, 0x7f });

            line.Operand = "$ffd2";
            TestInstruction(line, 0, null, false);

            line.Instruction = ".word";
            TestInstruction(line, 0x0002, new byte[] { 0xd2, 0xff });

            line.Operand = "-1";
            TestInstruction(line, 0, null, false);

            line.Instruction = ".addr";
            TestInstruction(line, 0, null, false);

            line.Operand = "65232 , $c000";
            TestInstruction(line, 0x0004, new byte[] { 0xd0, 0xfe, 0x00, 0xc0 });

            line.Operand = "$010000";
            TestInstruction(line, 0, null, false);

            line.Instruction = ".word";
            TestInstruction(line, 0, null, false);

            line.Instruction = ".sint";
            TestInstruction(line, 0, null, false);
        }

        [Test]
        public void TestDwordDint()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".dint";
            line.Operand = int.MinValue.ToString() + ", " + int.MaxValue.ToString();
            TestInstruction(line, 0x0008, new byte[] { 0x00, 0x00, 0x00, 0x80, 
                                                       0xff, 0xff, 0xff, 0x7f});
            line.Operand = uint.MaxValue.ToString();
            TestInstruction(line, 0, null, false);

            line.Instruction = ".dword";
            TestInstruction(line, 0x0004, new byte[] { 0xff, 0xff, 0xff, 0xff });

            line.Operand = "-32342";
            TestInstruction(line, 0, null, false);
        }

        [Test]
        public void TestLintLong()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".lint";
            line.Operand = Int24.MinValue.ToString() + " , " + Int24.MaxValue.ToString();
            TestInstruction(line, 0x0006, new byte[] { 0x00, 0x00, 0x80, 
                                                       0xff, 0xff, 0x7f});

            line.Operand = "16777215";
            TestInstruction(line, 0, null, false);

            line.Instruction = ".long";
            TestInstruction(line, 0x0003, new byte[] { 0xff, 0xff, 0xff });

            line.Operand = "$01000000";
            TestInstruction(line, 0, null, false);
        }

        [Test]
        public void TestRta()
        {
            SourceLine line = new SourceLine();
            line.Instruction = ".rta";
            line.Operand = "$0000";
            TestInstruction(line, 0x0002, new byte[] { 0xff, 0xff });

            line.Operand = "$ffff";
            TestInstruction(line, 0x0002, new byte[] { 0xfe, 0xff });

            line.Operand = "?";
            TestInstruction(line, 0x0002, null);
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
            TestInstruction(line, 0x0000, null);

            line.Instruction = ".string";
            line.Operand = teststring;
            TestInstruction(line, ascbytes.Count(), ascbytes);

            line.Instruction = ".enc";
            line.Operand = "petscii";
            TestInstruction(line, 0x0000, null);

            line.Instruction = ".string";
            line.Operand = teststring;
            TestInstruction(line, petbytes.Count(), petbytes);

            line.Instruction = ".enc";
            line.Operand = "screen";
            TestInstruction(line, 0x0000, null);

            line.Instruction = ".string";
            line.Operand = teststring.ToUpper();
            TestInstruction(line, screenbytes.Count(), screenbytes);

            line.Instruction = ".enc";
            line.Operand = "none";
            TestInstruction(line, 0x0000, null);
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
            TestInstruction(line, 0x000f, test);

            test.Clear();
            test.AddRange(BitConverter.GetBytes(-32768).Take(2));
            test.Add(0x0d);
            test.AddRange(ascbytes);
            test.Add(0);

            line.Instruction = ".cstring";
            line.Operand = string.Format("-32768, 13, \"{0}\"", teststring);
            TestInstruction(line, test.Count, test);

            test.Clear();
            test.Add(Convert.ToByte(ascbytes.Count() + 2));
            test.AddRange(ascbytes);

            int instructionsize = 1     // total operand size byte
                                  + ascbytes.Count() // string size
                                  + 2;  // two uninitialized bytes
              

            line.Instruction = ".pstring";
            line.Operand = string.Format("\"{0}\", ?, ?", teststring);
            TestInstruction(line, instructionsize, test);

            test.Clear();
            test.Add(42);
            test.Add(0);
            test.Add(0);
            test.AddRange(ascbytes);
            test[test.Count - 1] |= 0x80;

            line.Instruction = ".nstring";
            line.Operand = string.Format("42, ?, ?, \"{0}\"", teststring);
            TestInstruction(line, test.Count(), test);

            line.Operand = line.Operand + "$80";
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() =>
                TestInstruction(line, 0, null, false));

            line.Operand = string.Format("42, ?, ?, \"{0}\", $80", teststring);
            TestInstruction(line, 0, null, false);

            test.Clear();
            test.AddRange(BitConverter.GetBytes(0x0d0d).Take(2));
            test.AddRange(ascbytes);
            test = test.Select(b => { b <<= 1; return b; }).ToList();
            test[test.Count - 1] += 1;

            Assert.IsTrue(test_.Output.GetCompilation().Count == 0);

            line.Instruction = ".lsstring";
            line.Operand = string.Format("$0d0d, \"{0}\"", teststring);
            TestInstruction(line, test.Count(), test);

            line.Operand += ",$80";
            TestInstruction(line, 0, null, false);
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
            TestInstruction(line, 0x000b, new byte[] { 0x00 });

            // test uninitialized another way
            line.Operand = "10, ?";
            TestInstruction(line, 0x000a, null);

            // test fill bytes
            line.Operand = "10, $ea";
            TestInstruction(line, 0x000a, new byte[] { 0xea, 0xea, 0xea, 0xea, 0xea,
                                                       0xea, 0xea, 0xea, 0xea, 0xea});

            line.Operand = string.Empty;
            Assert.Throws<InvalidOperationException>(() => TestInstruction(line, 0, null, false));

            line.Operand = "10, $ea, $20";
            TestInstruction(line, 0, null, false);

        }

        [Test]
        public void TestAlign()
        {
            SourceLine line = new SourceLine();

            line.Instruction = ".byte";
            line.Operand = "0";
            pseudo_.AssembleLine(line);
            Assert.IsFalse(test_.Log.HasErrors);
            Assert.AreEqual(0x0001, test_.Output.GetPC());
            Assert.IsTrue(test_.Output.GetCompilation().Count == 1);

            // align to nearest page, uninitialized
            line.Instruction = ".align";
            line.Operand = "$100";
            TestInstruction(line, 0x0100, new byte[] { 0x00 });

            // align to nearest page, uninitialized
            test_.Output.SetPC(1);
            line.Operand = "$100, ?";
            TestInstruction(line, 0x0100, null);

            // align to nearest 0x10, filled
            test_.Output.SetPC(0x0006);
            line.Operand = "$10, $ea";
            TestInstruction(line, 0x0010, new byte[] { 0xea, 0xea, 0xea, 0xea, 0xea,
                                                       0xea, 0xea, 0xea, 0xea, 0xea});


            line.Operand = string.Empty;
            Assert.Throws<InvalidOperationException>(() => TestInstruction(line, 0, null, false));
            
            line.Operand = "$100, $10, $02";
            TestInstruction(line, 0, null, false);
        }

        [Test]
        public void TestRepeat()
        {
            SourceLine line = new SourceLine();

            line.Instruction = ".repeat";
            line.Operand = "4, $ffd220";
            TestInstruction(line, 0x000c, new byte[] { 0x20, 0xd2, 0xff,
                                                       0x20, 0xd2, 0xff,
                                                       0x20, 0xd2, 0xff,
                                                       0x20, 0xd2, 0xff});

            // can't use repeat for uninitialized
            line.Operand = "4, ?";
            TestInstruction(line, 0, null, false);

            // can't have less than two args
            line.Operand = "4";
            TestInstruction(line, 0, null, false);

            line.Operand = string.Empty;
            Assert.Throws<InvalidOperationException>(() => TestInstruction(line, 0, null, false));
        }
    }
}
