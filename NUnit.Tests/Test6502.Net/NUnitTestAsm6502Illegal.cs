using DotNetAsm;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text;

namespace NUnit.Tests.Test6502.Net
{
    [TestFixture]
    public class NUnitTestAsm6502Illegal : TestDotNetAsm.NUnitAsmTestBase
    {
        public NUnitTestAsm6502Illegal()
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
        public void TestAnc()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "anc",
                Operand = "#$ff"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x2b, 0xff }, "anc #$ff");

            line.Operand = string.Empty;

            TestForFailure(line);
        }

        [Test]
        public void TestAne()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "ane",
                Operand = "#$ff"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x8b, 0xff }, "ane #$ff");

            line.Operand = string.Empty;

            TestForFailure(line);
        }

        [Test]
        public void TestArr()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "arr",
                Operand = "#$ff"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x6b, 0xff }, "arr #$ff");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestAsr()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "asr",
                Operand = "#$ff"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x4b, 0xff }, "asr #$ff");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestDcp()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "dcp",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xc7, 42 }, "dcp $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0xc3, 0x2a }, "dcp ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0xd3, 0x2a }, "dcp ($2a),y");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0xd7, 0x2a }, "dcp $2a,x");

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0xdb, 0x2a, 0x00 }, "dcp $002a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0xcf, 0xd2, 0xff }, "dcp $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0xdf, 0xd2, 0xff }, "dcp $ffd2,x");

        }

        [Test]
        public void TestDop()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "dop"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x80 }, "dop");

            line.Operand = "#42";
            TestInstruction(line, 0x0002, new byte[] { 0x80, 0x2a }, "dop #$2a");

            line.Operand = "42";
            TestInstruction(line, 0x0002, new byte[] { 0x04, 0x2a }, "dop $2a");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0x14, 0x2a }, "dop $2a,x");

            line.Operand = "(42,x)";
            TestForFailure(line);

            line.Operand = "(42),y";
            TestForFailure(line);

            line.Operand = "$ffd2";
            TestForFailure(line);

            line.Operand = "$ffd2,x";
            TestForFailure(line);

            line.Operand = "$ffd2,y";
            TestForFailure(line);
        }

        [Test]
        public void TestIsb()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "isb",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xe7, 42 }, "isb $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0xe3, 0x2a }, "isb ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0xf3, 0x2a }, "isb ($2a),y");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0xf7, 0x2a }, "isb $2a,x");

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0xfb, 0x2a, 0x00 }, "isb $002a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0xef, 0xd2, 0xff }, "isb $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0xff, 0xd2, 0xff }, "isb $ffd2,x");
        }

        [Test]
        public void TestJam()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "jam"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x02 }, "jam");

            line.Operand = "#42";
            TestForFailure(line);
        }

        [Test]
        public void TestLas()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "las",
                Operand = "42,y"
            };
            TestInstruction(line, 0x0003, new byte[] { 0xbb, 0x2a, 0x00 }, "las $002a,y");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestLax()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "lax",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0xa7, 42 }, "lax $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0xa3, 0x2a }, "lax ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0xb3, 0x2a }, "lax ($2a),y");

            line.Operand = "42,x";
            TestForFailure(line);

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0002, new byte[] { 0xb7, 0x2a }, "lax $2a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0xaf, 0xd2, 0xff }, "lax $ffd2");

            line.Operand = "$ffd2,y";
            TestInstruction(line, 0x0003, new byte[] { 0xbf, 0xd2, 0xff }, "lax $ffd2,y");

            line.Operand = "$ffd2,x";
            TestForFailure(line);
        }

        [Test]
        public void TestRla()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "rla",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x27, 42 }, "rla $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x23, 0x2a }, "rla ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x33, 0x2a }, "rla ($2a),y");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0x37, 0x2a }, "rla $2a,x");

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0x3b, 0x2a, 0x00 }, "rla $002a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x2f, 0xd2, 0xff }, "rla $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x3f, 0xd2, 0xff }, "rla $ffd2,x");
        }

        [Test]
        public void TestRra()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "rra",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x67, 42 }, "rra $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x63, 0x2a }, "rra ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x73, 0x2a }, "rra ($2a),y");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0x77, 0x2a }, "rra $2a,x");

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0x7b, 0x2a, 0x00 }, "rra $002a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x6f, 0xd2, 0xff }, "rra $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x7f, 0xd2, 0xff }, "rra $ffd2,x");
        }

        [Test]
        public void TestSax()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "sax",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x87, 42 }, "sax $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x83, 0x2a }, "sax ($2a,x)");

            line.Operand = "(42),y";
            TestForFailure(line);

            line.Operand = "42,x";
            TestForFailure(line);

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestInstruction(line, 0x0002, new byte[] { 0xcb, 0x2a }, "sax #$2a");

            line.Operand = "42,y";
            TestInstruction(line, 0x0002, new byte[] { 0x97, 0x2a }, "sax $2a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x8f, 0xd2, 0xff }, "sax $ffd2");

            line.Operand = "$ffd2,x";
            TestForFailure(line);

            line.Operand = "$ffd2,y";
            TestForFailure(line);
        }

        [Test]
        public void TestSha()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "sha",
                Operand = "(42),y"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x93, 0x2a }, "sha ($2a),y");

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0x9f, 0x2a, 0x00 }, "sha $002a,y");
        }


        [Test]
        public void TestShx()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "shx",
                Operand = "42,y"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x9e, 0x2a, 0x00 }, "shx $002a,y");

            line.Operand = string.Empty;
            TestForFailure(line);
        }


        [Test]
        public void TestShy()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "shy",
                Operand = "42,x"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x9c, 0x2a, 0x00 }, "shy $002a,x");

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestSlo()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "slo",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x07, 42 }, "slo $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x03, 0x2a }, "slo ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x13, 0x2a }, "slo ($2a),y");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0x17, 0x2a }, "slo $2a,x");

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0x1b, 0x2a, 0x00 }, "slo $002a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x0f, 0xd2, 0xff }, "slo $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x1f, 0xd2, 0xff }, "slo $ffd2,x");
        }

        [Test]
        public void TestSre()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "sre",
                Operand = "42"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x47, 42 }, "sre $2a");

            line.Operand = "(42,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x43, 0x2a }, "sre ($2a,x)");

            line.Operand = "(42),y";
            TestInstruction(line, 0x0002, new byte[] { 0x53, 0x2a }, "sre ($2a),y");

            line.Operand = "42,x";
            TestInstruction(line, 0x0002, new byte[] { 0x57, 0x2a }, "sre $2a,x");

            line.Operand = string.Empty;
            TestForFailure(line);

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestInstruction(line, 0x0003, new byte[] { 0x5b, 0x2a, 0x00 }, "sre $002a,y");

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x4f, 0xd2, 0xff }, "sre $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x5f, 0xd2, 0xff }, "sre $ffd2,x");
        }

        [Test]
        public void TestTas()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "tas",
                Operand = "42,y"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x9b, 0x2a, 0x00 }, "tas $002a,y");

            line.Operand = string.Empty;
            TestForFailure(line);
        }

        [Test]
        public void TestTop()
        {
            SetCpu("6502i");
            var line = new SourceLine
            {
                Instruction = "top"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x0c }, "top");

            line.Operand = "42";
            TestInstruction(line, 0x0003, new byte[] { 0x0c, 0x2a, 0x00 }, "top $002a");

            line.Operand = "(42,x)";
            TestForFailure(line);

            line.Operand = "(42),y";
            TestForFailure(line);

            line.Operand = "42,x";
            TestInstruction(line, 0x0003, new byte[] { 0x1c, 0x2a, 0x00 }, "top $002a,x");

            line.Operand = "#42";
            TestForFailure(line);

            line.Operand = "42,y";
            TestForFailure(line);

            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x0c, 0xd2, 0xff }, "top $ffd2");

            line.Operand = "$ffd2,x";
            TestInstruction(line, 0x0003, new byte[] { 0x1c, 0xd2, 0xff }, "top $ffd2,x");
        }
    }
}
