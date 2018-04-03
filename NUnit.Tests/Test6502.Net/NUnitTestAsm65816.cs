using DotNetAsm;
using NUnit.Framework;

namespace NUnit.Tests.Test6502.Net
{
    public class NUnitTestAsm65816 : TestDotNetAsm.NUnitAsmTestBase
    {
        public NUnitTestAsm65816()
        {
            Controller = new TestDotNetAsm.TestController();
            LineAssembler = new Asm6502.Net.Asm6502(Controller);
        }

        void SetCpu(string cpu)
        {
            var line = new SourceLine
            {
                Instruction = ".cpu",
                Operand = "\"" + cpu + "\""
            };
            var test = Controller as TestDotNetAsm.TestController;
            test.AssembleLine(line);
        }

        [Test]
        public void TestSetCpu()
        {
            SetCpu("65816");
            Assert.IsFalse(Controller.Log.HasErrors);

            var line = new SourceLine
            {
                Instruction = "sep",
                Operand = "#%10000000"
            };

            TestInstruction(line, 0x0002, new byte[] { 0xe2, 0x80 }, "sep #$80");

            SetCpu("6502");
            Assert.IsFalse(Controller.Log.HasErrors);

            TestForFailure(line);
            Controller.Log.ClearAll();

            SetCpu("GenuineIntel");
            Assert.IsTrue(Controller.Log.HasErrors);
            Controller.Log.ClearAll();
        }

        [Test]
        public void TestCompatibility()
        {
            SetCpu("6502");
            var line = new SourceLine
            {
                Instruction = "jsr",
                Operand = "$ffd2"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x20, 0xd2, 0xff }, "jsr $ffd2");

            SetCpu("65C02");
            TestInstruction(line, 0x0003, new byte[] { 0x20, 0xd2, 0xff }, "jsr $ffd2");

            SetCpu("65816");
            TestInstruction(line, 0x0003, new byte[] { 0x20, 0xd2, 0xff }, "jsr $ffd2");

            line.Instruction = "bra";
            TestInstruction(line, 0x0002, new byte[] { 0x80, 0xd0 }, "bra $ffd2");

            SetCpu("65C02");
            TestInstruction(line, 0x0002, new byte[] { 0x80, 0xd0 }, "bra $ffd2");

            SetCpu("6502");
            TestForFailure(line);

            line.Instruction = "ora";
            line.Operand = "(42,s),y";
            TestForFailure(line);

            SetCpu("65C02");
            TestForFailure(line);

            SetCpu("65816");
            TestInstruction(line, 0x0002, new byte[] { 0x13, 0x2a }, "ora ($2a,s),y");
        }

        [Test]
        public void TestAdc16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "adc",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x63, 0x2a }, "adc $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0x67, 0x2a }, "adc [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0x6f, 0x56, 0x34, 0x12 }, "adc $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0x72, 0x2a }, "adc ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0x73, 0x2a }, "adc ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0x77, 0x2a }, "adc [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0x7f, 0x56, 0x34, 0x12 }, "adc $123456,x");
        }

        [Test]
        public void TestAnd16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "and",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x23, 0x2a }, "and $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0x27, 0x2a }, "and [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0x2f, 0x56, 0x34, 0x12 }, "and $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0x32, 0x2a }, "and ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0x33, 0x2a }, "and ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0x37, 0x2a }, "and [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0x3f, 0x56, 0x34, 0x12 }, "and $123456,x");
        }

        [Test]
        public void TestBitC02()
        {
            SetCpu("65C02");
            var line = new SourceLine
            {
                Instruction = "bit",
                Operand = "42,x"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x34, 0x2a }, "bit $2a,x");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x3c, 0xd2, 0xff }, "bit $ffd2,x");

            line.Operand = "#42";
            TestInstruction(line, 0x0002, new byte[] { 0x89, 0x2a }, "bit #$2a");
        }

        [Test]
        public void TestBra()
        {
            SetCpu("65C02");
            var line = new SourceLine
            {
                Instruction = "bra",
                Operand = "$ffd2"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x80, 0xd0 }, "bra $ffd2");
        }

        [Test]
        public void TestBrl()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "brl",
                Operand = "$ffd2"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x82, 0xcf, 0xff }, "brl $ffd2");

            line.Operand = "32767";
            TestInstruction(line, 0x0003, new byte[] { 0x82, 0xfc, 0x7f }, "brl $7fff");

            line.Operand = "0";
            TestInstruction(line, 0x0003, new byte[] { 0x82, 0xfd, 0xff }, "brl $0000");
        }

        [Test]
        public void TestCmp16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "cmp",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xc3, 0x2a }, "cmp $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0xc7, 0x2a }, "cmp [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0xcf, 0x56, 0x34, 0x12 }, "cmp $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0xd2, 0x2a }, "cmp ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0xd3, 0x2a }, "cmp ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0xd7, 0x2a }, "cmp [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0xdf, 0x56, 0x34, 0x12 }, "cmp $123456,x");
        }

        [Test]
        public void TestCop()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "cop"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x02 }, "cop");

            line.Operand = "#42";
            TestInstruction(line, 0x0002, new byte[] { 0x02, 0x2a }, "cop #$2a");
        }

        [Test]
        public void TestDeca()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "dec"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x3a }, "dec");

            line.Operand = "a";
            TestInstruction(line, 0x0001, new byte[] { 0x3a }, "dec");
        }

        [Test]
        public void TestEor16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "eor",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x43, 0x2a }, "eor $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0x47, 0x2a }, "eor [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0x4f, 0x56, 0x34, 0x12 }, "eor $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0x52, 0x2a }, "eor ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0x53, 0x2a }, "eor ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0x57, 0x2a }, "eor [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0x5f, 0x56, 0x34, 0x12 }, "eor $123456,x");
        }

        [Test]
        public void TestIncA()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "inc"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x1a }, "inc");

            line.Operand = "a";
            TestInstruction(line, 0x0001, new byte[] { 0x1a }, "inc");
        }

        [Test]
        public void TestJmpC02()
        {
            SetCpu("65C02");
            var line = new SourceLine
            {
                Instruction = "jmp",
                Operand = "($ffd2,x)"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x7c, 0xd2, 0xff }, "jmp ($ffd2,x)");
        }

        [Test]
        public void TestJml()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "jml",
                Operand = "42"
            };
            TestInstruction(line, 0x0004, new byte[] { 0x5c, 0x2a, 0x00, 0x00 }, "jml $00002a");

            line.Instruction = "jmp";
            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0x5c, 0x56, 0x34, 0x12 }, "jmp $123456");

            line.Operand = "[$ffd2]";
            TestInstruction(line, 0x0003, new byte[] { 0xdc, 0xd2, 0xff }, "jmp [$ffd2]");
        }

        [Test]
        public void TestJsl()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "jsl",
                Operand = "$123456"
            };
            TestInstruction(line, 0x0004, new byte[] { 0x22, 0x56, 0x34, 0x12 }, "jsl $123456");

            line.Instruction = "jsr";
            TestInstruction(line, 0x0004, new byte[] { 0x22, 0x56, 0x34, 0x12 }, "jsr $123456");
        }

        [Test]
        public void TestLda16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "lda",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xa3, 0x2a }, "lda $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0xa7, 0x2a }, "lda [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0xaf, 0x56, 0x34, 0x12 }, "lda $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0xb2, 0x2a }, "lda ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0xb3, 0x2a }, "lda ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0xb7, 0x2a }, "lda [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0xbf, 0x56, 0x34, 0x12 }, "lda $123456,x");
        }

        [Test]
        public void TestMisc16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "lda",
                Operand = "(   42  , s  )  , y"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xb3, 0x2a }, "lda ($2a,s),y");

            line.Operand = "[   42+(32/5) ] , y";
            TestInstruction(line, 0x0002, new byte[] { 0xb7, 42 + (32 / 5) }, "lda [$30],y");
        }

        [Test]
        public void TestMvn()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "mvn",
                Operand = "42,21"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x54, 0x15, 0x2a }, "mvn $2a,$15");

            line.Instruction = "mvp";
            TestInstruction(line, 0x0003, new byte[] { 0x44, 0x15, 0x2a }, "mvp $2a,$15");
        }

        [Test]
        public void TestOra16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "ora",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x03, 0x2a }, "ora $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0x07, 0x2a }, "ora [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0x0f, 0x56, 0x34, 0x12 }, "ora $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0x12, 0x2a }, "ora ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0x13, 0x2a }, "ora ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0x17, 0x2a }, "ora [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0x1f, 0x56, 0x34, 0x12 }, "ora $123456,x");
        }

        [Test]
        public void TestPea()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "pea",
                Operand = "$ffd2"
            };
            TestInstruction(line, 0x0003, new byte[] { 0xf4, 0xd2, 0xff }, "pea $ffd2");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestPei()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "pei",
                Operand = "(42)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xd4, 0x2a }, "pei ($2a)");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestPer()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "per",
                Operand = "$ffd2"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x62, 0xcf, 0xff }, "per $ffd2");
        }

        [Test]
        public void TestPushToStack()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "phb"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x8b }, "phb");

            line.Instruction = "phd";
            TestInstruction(line, 0x0001, new byte[] { 0x0b }, "phd");

            line.Instruction = "phk";
            TestInstruction(line, 0x0001, new byte[] { 0x4b }, "phk");

            line.Instruction = "phx";
            TestInstruction(line, 0x0001, new byte[] { 0xda }, "phx");

            line.Instruction = "phy";
            TestInstruction(line, 0x0001, new byte[] { 0x5a }, "phy");
        }

        [Test]
        public void TestPopFromStack()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "plb"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xab }, "plb");

            line.Instruction = "pld";
            TestInstruction(line, 0x0001, new byte[] { 0x2b }, "pld");

            line.Instruction = "plx";
            TestInstruction(line, 0x0001, new byte[] { 0xfa }, "plx");

            line.Instruction = "ply";
            TestInstruction(line, 0x0001, new byte[] { 0x7a }, "ply");
        }

        [Test]
        public void TestRep()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "rep",
                Operand = "#%00000001"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xc2, 0x01 }, "rep #$01");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestRtl()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "rtl"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x6b }, "rtl");
        }

        [Test]
        public void TestSbc16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "sbc",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xe3, 0x2a }, "sbc $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0xe7, 0x2a }, "sbc [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0xef, 0x56, 0x34, 0x12 }, "sbc $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0xf2, 0x2a }, "sbc ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0xf3, 0x2a }, "sbc ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0xf7, 0x2a }, "sbc [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0xff, 0x56, 0x34, 0x12 }, "sbc $123456,x");
        }

        [Test]
        public void TestSep()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "sep",
                Operand = "#%00000001"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xe2, 0x01 }, "sep #$01");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestSta16()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "sta",
                Operand = "42,s"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x83, 0x2a }, "sta $2a,s");

            line.Operand = "[42]";
            TestInstruction(line, 0x0002, new byte[] { 0x87, 0x2a }, "sta [$2a]");

            line.Operand = "$123456";
            TestInstruction(line, 0x0004, new byte[] { 0x8f, 0x56, 0x34, 0x12 }, "sta $123456");

            line.Operand = "(42)";
            TestInstruction(line, 0x0002, new byte[] { 0x92, 0x2a }, "sta ($2a)");

            line.Operand = "(42,s),y";
            TestInstruction(line, 0x0002, new byte[] { 0x93, 0x2a }, "sta ($2a,s),y");

            line.Operand = "[42],y";
            TestInstruction(line, 0x0002, new byte[] { 0x97, 0x2a }, "sta [$2a],y");

            line.Operand = "$123456,x";
            TestInstruction(line, 0x0004, new byte[] { 0x9f, 0x56, 0x34, 0x12 }, "sta $123456,x");
        }

        [Test]
        public void TestStp()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "stp"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xdb }, "stp");
        }

        [Test]
        public void TestStz()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "stz",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x64, 0x2a }, "stz $2a");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0x74, 0x2a }, "stz $2a,x");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x9c, 0xd2, 0xff }, "stz $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x9e, 0xd2, 0xff }, "stz $ffd2,x");
        }

        [Test]
        public void TestTcd()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "tcd"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x5b }, "tcd");
        }

        [Test]
        public void TestTcs()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "tcs"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x1b }, "tcs");
        }

        [Test]
        public void TestTdc()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "tdc"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x7b }, "tdc");
        }

        [Test]
        public void TestTrb()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "trb",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x14, 0x2a }, "trb $2a");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x1c, 0xd2, 0xff }, "trb $ffd2");
        }

        [Test]
        public void TestTsb()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "tsb",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x04, 0x2a }, "tsb $2a");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x0c, 0xd2, 0xff }, "tsb $ffd2");
        }

        [Test]
        public void TestTsc()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "tsc"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x3b }, "tsc");
        }

        [Test]
        public void TestTxy()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "txy"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x9b }, "txy");
        }

        [Test]
        public void TestTyx()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "tyx"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xbb }, "tyx");
        }

        [Test]
        public void TestWai()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "wai"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xcb }, "wai");
        }

        [Test]
        public void TestXba()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "xba"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xeb }, "xba");
        }

        [Test]
        public void TestXce()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "xce"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xfb }, "xce");
        }

        [Test]
        public void TestForceWidth()
        {
            SetCpu("65816");
            var line = new SourceLine
            {
                Instruction = "lda",
                Operand = "[16] $12"
            };
            TestInstruction(line, 0x0003, new byte[] { 0xad, 0x12, 0x00 }, "lda $0012");

            line.Operand = "[24] $12";
            TestInstruction(line, 0x0004, new byte[] { 0xaf, 0x12, 0x00, 0x00 }, "lda $000012");

            line.Operand = "[16] ,y";
            TestInstruction(line, 0x0002, new byte[] { 0xb7, 0x10 }, "lda [$10],y");

            line.Operand = "[24]";
            TestInstruction(line, 0x0002, new byte[] { 0xa7, 0x18 }, "lda [$18]");
        }
    }
}
