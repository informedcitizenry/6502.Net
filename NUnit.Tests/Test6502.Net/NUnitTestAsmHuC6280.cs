using DotNetAsm;
using NUnit.Framework;

namespace NUnit.Tests.Test6502.Net
{
    [TestFixture]
    public class NUnitTestAsmHuC6280 : NUnitTestAsm6502Base
    {
        [Test]
        public void TestSt1()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "st1",
                Operand = "#$42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x13, 0x42 }, "st1 #$42");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestSt2()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "st2",
                Operand = "#$42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x23, 0x42 }, "st2 #$42");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestSxy()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "sxy"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x02 }, "sxy");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestSax()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "sax"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x22 }, "sax");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestSay()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "say"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x42 }, "say");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestCla()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "cla"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x62 }, "cla");

            line.Operand = "$42";
            TestForFailure(line);
        }
        [Test]
        public void TestClx()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "clx"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x82 }, "clx");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestCly()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "cly"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xC2 }, "cly");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestCsh()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "csh"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xd4 }, "csh");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestSet()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "set"
            };
            TestInstruction(line, 0x0001, new byte[] { 0xf4 }, "set");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestTma()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tma",
                Operand = "#$42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x43, 0x42 }, "tma #$42");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestTam()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tam",
                Operand = "#$42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x53, 0x42 }, "tam #$42");

            line.Operand = "$42";
            TestForFailure(line);
        }

        [Test]
        public void TestOraInd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "ora",
                Operand = "($42,x)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x01, 0x42 }, "ora ($42,x)");

            line.Operand = "($42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x11, 0x42 }, "ora ($42),y");

            line.Operand = "($42)";
            TestInstruction(line, 0x0003, new byte[] { 0x12, 0x42, 0x00 }, "ora ($0042)");

            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0x12, 0xd2, 0xff }, "ora ($ffd2)");
        }

        [Test]
        public void TestAndInd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "and",
                Operand = "($42,x)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x21, 0x42 }, "and ($42,x)");

            line.Operand = "($42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x31, 0x42 }, "and ($42),y");

            line.Operand = "($42)";
            TestInstruction(line, 0x0003, new byte[] { 0x32, 0x42, 0x00 }, "and ($0042)");

            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0x32, 0xd2, 0xff }, "and ($ffd2)");
        }

        [Test]
        public void TestEorInd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "eor",
                Operand = "($42,x)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x41, 0x42 }, "eor ($42,x)");

            line.Operand = "($42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x51, 0x42 }, "eor ($42),y");

            line.Operand = "($42)";
            TestInstruction(line, 0x0003, new byte[] { 0x52, 0x42, 0x00 }, "eor ($0042)");

            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0x52, 0xd2, 0xff }, "eor ($ffd2)");
        }

        [Test]
        public void TestAdcInd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "adc",
                Operand = "($42,x)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x61, 0x42 }, "adc ($42,x)");

            line.Operand = "($42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x71, 0x42 }, "adc ($42),y");

            line.Operand = "($42)";
            TestInstruction(line, 0x0003, new byte[] { 0x72, 0x42, 0x00 }, "adc ($0042)");

            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0x72, 0xd2, 0xff }, "adc ($ffd2)");
        }

        [Test]
        public void TestCmpInd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "cmp",
                Operand = "($42,x)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xc1, 0x42 }, "cmp ($42,x)");

            line.Operand = "($42),y";
            TestInstruction(line, 0x0002, new byte[] { 0xd1, 0x42 }, "cmp ($42),y");

            line.Operand = "($42)";
            TestInstruction(line, 0x0003, new byte[] { 0xd2, 0x42, 0x00 }, "cmp ($0042)");

            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0xd2, 0xd2, 0xff }, "cmp ($ffd2)");
        }

        [Test]
        public void TestSbcInd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "sbc",
                Operand = "($42,x)"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xe1, 0x42 }, "sbc ($42,x)");

            line.Operand = "($42),y";
            TestInstruction(line, 0x0002, new byte[] { 0xf1, 0x42 }, "sbc ($42),y");

            line.Operand = "($42)";
            TestInstruction(line, 0x0003, new byte[] { 0xf2, 0x42, 0x00 }, "sbc ($0042)");

            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0xf2, 0xd2, 0xff }, "sbc ($ffd2)");
        }

        [Test]
        public void TestTii()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tii",
                Operand = "$42,$d2,$01"
            };
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x42, 0x00, 0xd2, 0x00, 0x01, 0x00 }, "tii $0042,$00d2,$0001");

            line.Operand = "$1000,$d2,$01";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x00, 0x10, 0xd2, 0x00, 0x01, 0x00 }, "tii $1000,$00d2,$0001");

            line.Operand = "$42,$1000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x42, 0x00, 0x00, 0x10, 0x01, 0x00 }, "tii $0042,$1000,$0001");

            line.Operand = "$42,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x42, 0x00, 0xd2, 0x00, 0x00, 0x10 }, "tii $0042,$00d2,$1000");

            line.Operand = "$1000,$2000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x00, 0x10, 0x00, 0x20, 0x01, 0x00 }, "tii $1000,$2000,$0001");

            line.Operand = "$1000,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x00, 0x10, 0xd2, 0x00, 0x00, 0x10 }, "tii $1000,$00d2,$1000");

            line.Operand = "$42,$1000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x42, 0x00, 0x00, 0x10, 0x00, 0x10 }, "tii $0042,$1000,$1000");

            line.Operand = "$1000,$2000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0x73, 0x00, 0x10, 0x00, 0x20, 0x00, 0x10 }, "tii $1000,$2000,$1000");
        }

        [Test]
        public void TestTdd()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tdd",
                Operand = "$42,$d2,$01"
            };
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x42, 0x00, 0xd2, 0x00, 0x01, 0x00 }, "tdd $0042,$00d2,$0001");

            line.Operand = "$1000,$d2,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x00, 0x10, 0xd2, 0x00, 0x01, 0x00 }, "tdd $1000,$00d2,$0001");

            line.Operand = "$42,$1000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x42, 0x00, 0x00, 0x10, 0x01, 0x00 }, "tdd $0042,$1000,$0001");

            line.Operand = "$42,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x42, 0x00, 0xd2, 0x00, 0x00, 0x10 }, "tdd $0042,$00d2,$1000");

            line.Operand = "$1000,$2000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x00, 0x10, 0x00, 0x20, 0x01, 0x00 }, "tdd $1000,$2000,$0001");

            line.Operand = "$1000,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x00, 0x10, 0xd2, 0x00, 0x00, 0x10 }, "tdd $1000,$00d2,$1000");

            line.Operand = "$42,$1000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x42, 0x00, 0x00, 0x10, 0x00, 0x10 }, "tdd $0042,$1000,$1000");

            line.Operand = "$1000,$2000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xc3, 0x00, 0x10, 0x00, 0x20, 0x00, 0x10 }, "tdd $1000,$2000,$1000");
        }

        [Test]
        public void TestTin()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tin",
                Operand = "$42,$d2,$01"
            };
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x42, 0x00, 0xd2, 0x00, 0x01, 0x00 }, "tin $0042,$00d2,$0001");

            line.Operand = "$1000,$d2,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x00, 0x10, 0xd2, 0x00, 0x01, 0x00 }, "tin $1000,$00d2,$0001");

            line.Operand = "$42,$1000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x42, 0x00, 0x00, 0x10, 0x01, 0x00 }, "tin $0042,$1000,$0001");

            line.Operand = "$42,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x42, 0x00, 0xd2, 0x00, 0x00, 0x10 }, "tin $0042,$00d2,$1000");

            line.Operand = "$1000,$2000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x00, 0x10, 0x00, 0x20, 0x01, 0x00 }, "tin $1000,$2000,$0001");

            line.Operand = "$1000,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x00, 0x10, 0xd2, 0x00, 0x00, 0x10 }, "tin $1000,$00d2,$1000");

            line.Operand = "$42,$1000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x42, 0x00, 0x00, 0x10, 0x00, 0x10 }, "tin $0042,$1000,$1000");

            line.Operand = "$1000,$2000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xd3, 0x00, 0x10, 0x00, 0x20, 0x00, 0x10 }, "tin $1000,$2000,$1000");
        }

        [Test]
        public void TestTia()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tia",
                Operand = "$42,$d2,$01"
            };
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x42, 0x00, 0xd2, 0x00, 0x01, 0x00 }, "tia $0042,$00d2,$0001");

            line.Operand = "$1000,$d2,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x00, 0x10, 0xd2, 0x00, 0x01, 0x00 }, "tia $1000,$00d2,$0001");

            line.Operand = "$42,$1000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x42, 0x00, 0x00, 0x10, 0x01, 0x00 }, "tia $0042,$1000,$0001");

            line.Operand = "$42,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x42, 0x00, 0xd2, 0x00, 0x00, 0x10 }, "tia $0042,$00d2,$1000");

            line.Operand = "$1000,$2000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x00, 0x10, 0x00, 0x20, 0x01, 0x00 }, "tia $1000,$2000,$0001");

            line.Operand = "$1000,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x00, 0x10, 0xd2, 0x00, 0x00, 0x10 }, "tia $1000,$00d2,$1000");

            line.Operand = "$42,$1000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x42, 0x00, 0x00, 0x10, 0x00, 0x10 }, "tia $0042,$1000,$1000");

            line.Operand = "$1000,$2000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xe3, 0x00, 0x10, 0x00, 0x20, 0x00, 0x10 }, "tia $1000,$2000,$1000");
        }

        [Test]
        public void TestTai()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tai",
                Operand = "$42,$d2,$01"
            };
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x42, 0x00, 0xd2, 0x00, 0x01, 0x00 }, "tai $0042,$00d2,$0001");

            line.Operand = "$1000,$d2,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x00, 0x10, 0xd2, 0x00, 0x01, 0x00 }, "tai $1000,$00d2,$0001");

            line.Operand = "$42,$1000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x42, 0x00, 0x00, 0x10, 0x01, 0x00 }, "tai $0042,$1000,$0001");

            line.Operand = "$42,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x42, 0x00, 0xd2, 0x00, 0x00, 0x10 }, "tai $0042,$00d2,$1000");

            line.Operand = "$1000,$2000,$01";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x00, 0x10, 0x00, 0x20, 0x01, 0x00 }, "tai $1000,$2000,$0001");

            line.Operand = "$1000,$d2,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x00, 0x10, 0xd2, 0x00, 0x00, 0x10 }, "tai $1000,$00d2,$1000");

            line.Operand = "$42,$1000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x42, 0x00, 0x00, 0x10, 0x00, 0x10 }, "tai $0042,$1000,$1000");

            line.Operand = "$1000,$2000,$1000";
            TestInstruction(line, 0x0007, new byte[] { 0xf3, 0x00, 0x10, 0x00, 0x20, 0x00, 0x10 }, "tai $1000,$2000,$1000");
        }

        [Test]
        public void TestTst()
        {
            SetCpu("HuC6280");

            var line = new SourceLine
            {
                Instruction = "tst",
                Operand = "#$42,$d2"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x83, 0x42, 0xd2 }, "tst #$42,$d2");

            line.Operand = "#$42,$d2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x93, 0x42, 0xd2 }, "tst #$42,$d2,x");

            line.Operand = "#$42,$1000";
            TestInstruction(line, 0x0004, new byte[] { 0xa3, 0x42, 0x00, 0x10 }, "tst #$42,$1000");

            line.Operand = "#$42,$1000,x";
            TestInstruction(line, 0x0004, new byte[] { 0xb3, 0x42, 0x00, 0x10 }, "tst #$42,$1000,x");

            line.Operand = "#$1000,$42";
            TestForFailure(line);

            line.Operand = "#$1000,$1000";
            TestForFailure(line);
        }
    }
}
