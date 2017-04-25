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
    public class DisasmTestVerbose
    {
        private IAssemblyController test_;
        private ILineDisassembler disasm_;

        public DisasmTestVerbose()
        {
            test_ = new TestController();


            test_.Assemble(new string[] { "--verbose-asm" });
            disasm_ = new Disasm6502(test_);
        }

        [Test]
        public void TestVerboseDisasmSimple()
        {
            SourceLine line = new SourceLine();
            line.SourceString = "    ldx #$00 ; comment here!";
            line.Instruction = "ldx";
            line.Operand = "#$00";
            line.Assembly.AddRange(new byte[] { 0xa2, 0x00 });
            line.Disassembly = "ldx #$00";
            line.PC = 0xc025;
            line.Filename = "testdisasm.a65";
            line.LineNumber = 30;

            string disasm = disasm_.DisassembleLine(line);

            string expected = @"testdisasm.a65(30)  :.c025   a2 00       ldx #$00        " +
                                line.SourceString +
                                Environment.NewLine;

            Assert.AreEqual(expected, disasm);
        }

        [Test]
        public void TestVerboseDisasmEqu()
        {
            SourceLine line = new SourceLine();
            line.SourceString = "               FLAG         =   $34             ";
            line.Instruction = "=";
            line.Operand = "$34";
            line.Disassembly = string.Empty;
            line.PC = 0x080b;
            line.Filename = "testdisasm.a65";
            line.LineNumber = 73;

            string expected =
@"testdisasm.a65(73)  :=$34                                               FLAG         =   $34             " + Environment.NewLine;
            string result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestVerboseDisasmLabel()
        {
            SourceLine line = new SourceLine();
            line.PC = 0x080b;
            line.Label = "stdout";
            line.SourceString = "stdout      .block";
            line.Instruction = ".block";
            line.Operand = string.Empty;
            line.Disassembly = string.Empty;
            line.IsDefinition = true;
            line.Filename = "testdisasm.a65";
            line.LineNumber = 32;

            string expected = "testdisasm.a65(32)  :                                    stdout      .block" + Environment.NewLine;
            string result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);

            line.Label = "thisguy";
            line.SourceString = "thisguy";
            line.Instruction = string.Empty;
            line.LineNumber = 33;

            expected = "testdisasm.a65(33)  :                                    thisguy   " + Environment.NewLine;
            result = disasm_.DisassembleLine(line);
            
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestVerboseDisasmMultiByteFill()
        {
            SourceLine line = new SourceLine();
            line.Filename = "testdisasm.a65";
            line.LineNumber = 68;
            line.SourceString = "                            .fill 9*3,%........";
            line.PC = 0x093e;
            line.Assembly.AddRange(Enumerable.Repeat(Convert.ToByte(0), 9 * 3));
            line.Disassembly = string.Empty;
            line.Instruction = ".fill";
            line.Operand = "9*3,%........";

            string expected =
"testdisasm.a65(68)  :>093e   00 00 00 00 00 00 00 00                                 .fill 9*3,%........" + Environment.NewLine +
"                    :>0946   00 00 00 00 00 00 00 00" + Environment.NewLine +
"                    :>094e   00 00 00 00 00 00 00 00" + Environment.NewLine +
"                    :>0956   00 00 00" + Environment.NewLine;

            string result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);

            line.SourceString = "somelabel   .fill 8,0";
            line.Operand = "8,0";
            line.PC = 0xc03c;
            line.LineNumber = 41;
            line.Filename = "testdisasm.a65";
            line.Assembly.Clear();
            line.Assembly.AddRange(Enumerable.Repeat(Convert.ToByte(0), 8));

            expected = "testdisasm.a65(41)  :>c03c   00 00 00 00 00 00 00 00     somelabel   .fill 8,0" + Environment.NewLine;

            result = disasm_.DisassembleLine(line);
            Assert.AreEqual(expected, result);

            line.SourceString = "somelabel   .fill 9,0";
            line.Operand = "9,0";
            line.Assembly.Add(0);

            expected =
"testdisasm.a65(41)  :>c03c   00 00 00 00 00 00 00 00     somelabel   .fill 9,0" + Environment.NewLine +
"                    :>c044   00" + Environment.NewLine;
            result = disasm_.DisassembleLine(line);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestVerboseDisasmMultiByteString()
        {
            string teststring = "HELLO, WORLD! MY NAME IS OPPENHEIMER I HAVE BECOME DESTROYER OF WORDS!!";
            var ascbytes = Encoding.ASCII.GetBytes(teststring.ToLower());
            SourceLine line = new SourceLine();
            line.SourceString = string.Format("            .string \"{0}\"", teststring);
            line.Instruction = ".string";
            line.Operand = string.Format("\"{0}\"", teststring);
            line.Disassembly = string.Empty;
            line.Assembly.AddRange(ascbytes);
            line.Filename = "testdisasm.a65";
            line.LineNumber = 41;
            line.PC = 0xc03c;

            string expected =
"testdisasm.a65(41)  :>c03c   68 65 6c 6c 6f 2c 20 77                 .string \"HELLO, WORLD! MY NAME IS OPPENHEIMER I HAVE BECOME DESTROYER OF WORDS!!\"" + Environment.NewLine +
"                    :>c044   6f 72 6c 64 21 20 6d 79" + Environment.NewLine +
"                    :>c04c   20 6e 61 6d 65 20 69 73" + Environment.NewLine +
"                    :>c054   20 6f 70 70 65 6e 68 65" + Environment.NewLine +
"                    :>c05c   69 6d 65 72 20 69 20 68" + Environment.NewLine +
"                    :>c064   61 76 65 20 62 65 63 6f" + Environment.NewLine +
"                    :>c06c   6d 65 20 64 65 73 74 72" + Environment.NewLine +
"                    :>c074   6f 79 65 72 20 6f 66 20" + Environment.NewLine +
"                    :>c07c   77 6f 72 64 73 21 21" + Environment.NewLine;

            string result = disasm_.DisassembleLine(line);
            Assert.AreEqual(expected, result);
        }
    }

    [TestFixture]
    public class DisasmTest
    {
        private IAssemblyController test_;
        private ILineDisassembler disasm_;

        public DisasmTest()
        {
            test_ = new TestController();
            disasm_ = new Disasm6502(test_);

        }

        [Test]
        public void TestDisasmMultiByteFill()
        {
            SourceLine line = new SourceLine();
            line.SourceString = "                            .fill 9*3,%........";
            line.PC = 0x093e;
            line.Assembly.AddRange(Enumerable.Repeat(Convert.ToByte(0), 9*3));
            line.Disassembly = string.Empty;
            line.Instruction = ".fill";
            line.Operand = "9*3,%........";

            string expected =
">093e   00 00 00 00 00 00 00 00                                 .fill 9*3,%........" + Environment.NewLine +
">0946   00 00 00 00 00 00 00 00" + Environment.NewLine +
">094e   00 00 00 00 00 00 00 00" + Environment.NewLine +
">0956   00 00 00" + Environment.NewLine;

            string result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);

            line.SourceString = "somelabel   .fill 8,0";
            line.Operand = "8,0";
            line.PC = 0xc03c;
            line.Assembly.Clear();
            line.Assembly.AddRange(Enumerable.Repeat(Convert.ToByte(0), 8));

            expected = ">c03c   00 00 00 00 00 00 00 00     somelabel   .fill 8,0" + Environment.NewLine;
            result = disasm_.DisassembleLine(line);
            Assert.AreEqual(expected, result);
            

            line.SourceString = "somelabel   .fill 9,0";
            line.Operand = "9,0";
            line.Assembly.Add(0);

            expected =
">c03c   00 00 00 00 00 00 00 00     somelabel   .fill 9,0" + Environment.NewLine +
">c044   00" + Environment.NewLine;
            result = disasm_.DisassembleLine(line);            
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestDisasmMultiByteString()
        {
            string teststring = "HELLO, WORLD! MY NAME IS OPPENHEIMER I HAVE BECOME DESTROYER OF WORDS!!";
            var ascbytes = Encoding.ASCII.GetBytes(teststring.ToLower());
            SourceLine line = new SourceLine();
            line.SourceString = string.Format("            .string \"{0}\"", teststring);
            line.Instruction = ".string";
            line.Operand = string.Format("\"{0}\"", teststring);
            line.Disassembly = string.Empty;
            line.Assembly.AddRange(ascbytes);
            line.PC = 0xc03c;

            string expected =
">c03c   68 65 6c 6c 6f 2c 20 77                 .string \"HELLO, WORLD! MY NAME IS OPPENHEIMER I HAVE BECOME DESTROYER OF WORDS!!\"" + Environment.NewLine +
">c044   6f 72 6c 64 21 20 6d 79" + Environment.NewLine +
">c04c   20 6e 61 6d 65 20 69 73" + Environment.NewLine +
">c054   20 6f 70 70 65 6e 68 65" + Environment.NewLine +
">c05c   69 6d 65 72 20 69 20 68" + Environment.NewLine +
">c064   61 76 65 20 62 65 63 6f" + Environment.NewLine +
">c06c   6d 65 20 64 65 73 74 72" + Environment.NewLine +
">c074   6f 79 65 72 20 6f 66 20" + Environment.NewLine +
">c07c   77 6f 72 64 73 21 21" + Environment.NewLine;

            string result = disasm_.DisassembleLine(line);
            Assert.AreEqual(expected, result);

        }

        [Test]
        public void TestDisasmLabel()
        {
            SourceLine line = new SourceLine();
            line.PC = 0x080b;
            line.Label = "stdout";
            line.SourceString = "stdout      .block";
            line.Instruction = ".block";
            line.Operand = string.Empty;
            line.Disassembly = string.Empty;
            line.IsDefinition = true;

            string expected = "                                    stdout    " + Environment.NewLine;
            string result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);

            line.Label = "thisguy";
            line.SourceString = "thisguy";
            line.Instruction = string.Empty;

            expected = "                                    thisguy   " + Environment.NewLine;
            result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestDisasmEqu()
        {
            SourceLine line = new SourceLine();
            line.SourceString = "               FLAG         =   $34 ";
            line.Instruction = "=";
            line.Operand = "$34";
            line.Disassembly = string.Empty;
            line.PC = 0x080b;

            string expected =
@"=$34                                               FLAG         =   $34 " + Environment.NewLine;
            string result = disasm_.DisassembleLine(line);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestDisasmSimple()
        {
            SourceLine line = new SourceLine();
            line.SourceString = "    ldx #$00 ; comment here!";
            line.Instruction = "ldx";
            line.Operand = "#$00";
            line.Assembly.AddRange(new byte[] { 0xa2, 0x00 });
            line.Disassembly = "ldx #$00";
            line.PC = 0xc000;

            string disasm = disasm_.DisassembleLine(line);

            string expected = @".c000   a2 00       ldx #$00        " + 
                                line.SourceString + 
                                Environment.NewLine;

            Assert.AreEqual(expected, disasm);
        }
    }
}
