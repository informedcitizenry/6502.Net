using DotNetAsm;
using Asm6502.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.Test6502.Net
{
    [TestFixture]
    public class NUnitTestAsm6502 : TestDotNetAsm.NUnitAsmTestBase
    {
        public NUnitTestAsm6502()
        {
            Controller = new TestDotNetAsm.TestController();
            LineAssembler = new Asm6502.Net.Asm6502(Controller);
        }

        private void TestRelativeBranch(string mnemonic, byte opcode)
        {
            SourceLine line = new SourceLine();

            Controller.Output.SetPC(0xfffe);
            line.PC = 0xfffe;
            line.Instruction = mnemonic;
            line.Operand = "$0002";
            TestInstruction(line, 0x0000, new byte[] { opcode, 0x02 }, mnemonic + " " + line.Operand);
            
            line.Operand = "$fffe";
            TestInstruction(line, 0x0002, new byte[] { opcode, 0xfc }, mnemonic + " " + line.Operand);
            Controller.Output.Reset();

            Controller.Output.SetPC(0x0002);
            line.Operand = "$0000";
            TestInstruction(line, 0x0004, new byte[] { opcode, 0xfc }, mnemonic + " " + line.Operand);
            Controller.Output.Reset();

            line.Operand = "$ff82";
            TestInstruction(line, 0x0002, new byte[] { opcode, 0x80 }, mnemonic + " " + line.Operand);
            
            line.Operand = "$0081";
            TestInstruction(line, 0x0002, new byte[] { opcode, 0x7f }, mnemonic + " " + line.Operand);
            
            line.Operand = "$ff81";
            TestForFailure<OverflowException>(line);

            line.Operand = "$0082";
            TestForFailure<OverflowException>(line);

            line.Operand = "$1000";
            TestForFailure<OverflowException>(line);
        }

        private void TestImplied(string mnemonic, byte opcode)
        {
            SourceLine line = new SourceLine();

            line.Instruction = mnemonic;
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { opcode }, mnemonic);

            line.Operand = "$34";
            TestForFailure(line);

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$34,x";
            TestForFailure(line);

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);
        }

        [Test]
        public void TestAdc()
        {
            /*
            "adc (${0:x2},x)",  // 61
            "adc ${0:x2}",      // 65
            "adc #${0:x2}",     // 69
            "adc ${0:x4}",      // 6d
            "adc (${0:x2}),y",  // 71
            "adc ${0:x2},x",    // 75
            "adc ${0:x4},y",    // 79
            "adc ${0:x4},x",    // 7d
            */
            SourceLine line = new SourceLine();
            line.Instruction = "adc";
            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0x69, 0x34 }, "adc #$34");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x65, 0x34 }, "adc $34");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x75, 0x34 }, "adc $34,x");

            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x61, 0x34 }, "adc ($34,x)");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0x79, 0x34, 0x00 }, "adc $0034,y");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0x71, 0x34 }, "adc ($34),y");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x6d, 0x34, 0x12 }, "adc $1234");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x7d, 0x34, 0x12 }, "adc $1234,x");
        }

        [Test]
        public void TestAnd()
        {
            /*
            "and (${0:x2},x)",  // 21
            "and ${0:x2}",      // 25
            "and #${0:x2}",     // 29
            "and ${0:x4}",      // 2d
            "and (${0:x2}),y",  // 31
            "and ${0:x2},x",    // 35
            "and ${0:x4},y",    // 39
            "and ${0:x4},x",    // 3d*/
            SourceLine line = new SourceLine();
            line.Instruction = "and";
            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x21, 0x34 }, "and ($34,x)");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x25, 0x34 }, "and $34");

            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0x29, 0x34 }, "and #$34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x2d, 0x34, 0x12 }, "and $1234");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0x31, 0x34 }, "and ($34),y");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x35, 0x34 }, "and $34,x");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0x39, 0x34, 0x00 }, "and $0034,y");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x3d, 0x34, 0x12 }, "and $1234,x");
        }

        [Test]
        public void TestAsl()
        {
            /*
            "asl ${0:x2}",      // 06
            "asl",              // 0a
            "asl ${0:x4}",      // 0e
            "asl ${0:x2},x",    // 16
            "asl ${0:x4},x",    // 1e*/
            SourceLine line = new SourceLine();
            line.Instruction = "asl";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x06, 0x34 }, "asl $34");

            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x0a }, "asl");

            line.Operand = "a";
            TestInstruction(line, 0x0001, new byte[] { 0x0a }, "asl");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x0e, 0x34, 0x12 }, "asl $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x16, 0x34 }, "asl $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x1e, 0x34, 0x12 }, "asl $1234,x");

            // fails
            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestBcc()
        {
            TestRelativeBranch("bcc", 0x90);
        }

        [Test]
        public void TestBcs()
        {
            TestRelativeBranch("bcs", 0xb0);
        }

        [Test]
        public void TestBeq()
        {
            TestRelativeBranch("beq", 0xf0);
        }

        [Test]
        public void TestBit()
        {
            /*
            "bit ${0:x2}",      // 24
            "bit ${0:x4}",      // 2c*/

            SourceLine line = new SourceLine();
            line.Instruction = "bit";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x24, 0x34 }, "bit $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x2c, 0x34, 0x12 }, "bit $1234");

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$34,x";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestBmi()
        {
            TestRelativeBranch("bmi", 0x30);
        }

        [Test]
        public void TestBne()
        {
            TestRelativeBranch("bne", 0xd0);
        }

        [Test]
        public void TestBpl()
        {
            TestRelativeBranch("bpl", 0x10);
        }

        [Test]
        public void TestBrk()
        {
            TestImplied("brk", 0x00);
        }

        [Test]
        public void TestBvc()
        {
            TestRelativeBranch("bvc", 0x50);
        }

        [Test]
        public void TestBvs()
        {
            TestRelativeBranch("bvs", 0x70);
        }

        [Test]
        public void TestClc()
        {
            TestImplied("clc", 0x18);
        }

        [Test]
        public void TestCld()
        {
            TestImplied("cld", 0xd8);
        }

        [Test]
        public void TestCli()
        {
            TestImplied("cli", 0x58);
        }

        [Test]
        public void TestClv()
        {
            TestImplied("clv", 0xb8);
        }

        [Test]
        public void TestCmp()
        {
            /*
            "cmp (${0:x2},x)",  // c1
            "cmp ${0:x2}",      // c5
            "cmp #${0:x2}",     // c9
            "cmp ${0:x4}",      // cd
            "cmp (${0:x2}),y",  // d1
            "cmp ${0:x2},x",    // d5
            "cmp ${0:x4},y",    // d9
            "cmp ${0:x4},x",    // dd*/

            SourceLine line = new SourceLine();
            line.Instruction = "cmp";
            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0xc1, 0x34 }, "cmp ($34,x)");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xc5, 0x34 }, "cmp $34");

            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0xc9, 0x34 }, "cmp #$34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xcd, 0x34, 0x12 }, "cmp $1234");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0xd1, 0x34 }, "cmp ($34),y");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0xd5, 0x34 }, "cmp $34,x");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0xd9, 0x34, 0x00 }, "cmp $0034,y");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0xdd, 0x34, 0x12 }, "cmp $1234,x");
        }

        [Test]
        public void TestCpx()
        {
            /*
            "cpx #${0:x2}",     // e0
            "cpx ${0:x2}",      // e4
            "cpx ${0:x4}",      // ec
             */
            SourceLine line = new SourceLine();

            line.Instruction = "cpx";
            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0xe0, 0x34 }, "cpx #$34");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xe4, 0x34 }, "cpx $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xec, 0x34, 0x12 }, "cpx $1234");

            line.Operand = "$34,x";
            TestForFailure(line);

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestCpy()
        {
            /*
            "cpy #${0:x2}",     // c0
            "cpy ${0:x2}",      // c4
            "cpy ${0:x4}",      // cc*/
            SourceLine line = new SourceLine();

            line.Instruction = "cpy";
            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0xc0, 0x34 }, "cpy #$34");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xc4, 0x34 }, "cpy $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xcc, 0x34, 0x12 }, "cpy $1234");

            line.Operand = "$34,x";
            TestForFailure(line);

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestDec()
        {
            /*
            "dec ${0:x2}",      // c6
            "dec ${0:x4}",      // ce
            "dec ${0:x2},x",    // d6
            "dec ${0:x4},x",    // de*/

            SourceLine line = new SourceLine();
            line.Instruction = "dec";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xc6, 0x34 }, "dec $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xce, 0x34, 0x12 }, "dec $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0xd6, 0x34 }, "dec $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0xde, 0x34, 0x12 }, "dec $1234,x");

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestDex()
        {
            TestImplied("dex", 0xca);
        }

        [Test]
        public void TestDey()
        {
            TestImplied("dey", 0x88);
        }

        [Test]
        public void TestEor()
        {
            /*
            "eor (${0:x2},x)",  // 41
            "eor ${0:x2}",      // 45
            "eor #${0:x2}",     // 49
            "eor ${0:x4}",      // 4d
            "eor (${0:x2}),y",  // 51
            "eor ${0:x2},x",    // 55
            "eor ${0:x4},y",    // 59
            "eor ${0:x4},x",    // 5d*/
            SourceLine line = new SourceLine();

            line.Instruction = "eor";
            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x41, 0x34 }, "eor ($34,x)");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x45, 0x34 }, "eor $34");

            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0x49, 0x34 }, "eor #$34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x4d, 0x34, 0x12 }, "eor $1234");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0x51, 0x34 }, "eor ($34),y");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x55, 0x34 }, "eor $34,x");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0x59, 0x34, 0x00 }, "eor $0034,y");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x5d, 0x34, 0x12 }, "eor $1234,x");
        }

        [Test]
        public void TestInc()
        {
            /*
            "inc ${0:x2}",      // e6
            "inc ${0:x4}",      // ee
            "inc ${0:x2},x",    // f6
            "inc ${0:x4},x",    // fe*/
            SourceLine line = new SourceLine();
            line.Instruction = "inc";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xe6, 0x34 }, "inc $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xee, 0x34, 0x12 }, "inc $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0xf6, 0x34 }, "inc $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0xfe, 0x34, 0x12 }, "inc $1234,x");

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestInx()
        {
            TestImplied("inx", 0xe8);
        }

        [Test]
        public void TestIny()
        {
            TestImplied("iny", 0xc8);
        }

        [Test]
        public void TestJmp()
        {
            /*
            "jmp ${0:x4}",      // 4c
            "jmp (${0:x4})",    // 6c*/
            SourceLine line = new SourceLine();
            line.Instruction = "jmp";
            line.Operand = "$34";
            TestInstruction(line, 0x0003, new byte[] { 0x4c, 0x34, 0x00 }, "jmp $0034");

            line.Operand = "($34)";
            TestInstruction(line, 0x0003, new byte[] { 0x6c, 0x34, 0x00 }, "jmp ($0034)");

            line.Operand = "$34,x";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "#$34";
            TestForFailure(line);
        }

        [Test]
        public void TestJsr()
        {
            SourceLine line = new SourceLine();
            line.Instruction = "jsr";
            line.Operand = "$34";
            TestInstruction(line, 0x0003, new byte[] { 0x20, 0x34, 0x00 }, "jsr $0034");

            line.Operand = "$34,x";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$34,y";
            TestForFailure(line);

            line.Operand = "#$34";
            TestForFailure(line);
        }

        [Test]
        public void TestLda()
        {
            /*
            "lda (${0:x2},x)",  // a1
            "lda ${0:x2}",      // a5
            "lda #${0:x2}",     // a9
            "lda ${0:x4}",      // ad
            "lda (${0:x2}),y",  // b1
            "lda ${0:x2},x",    // b5
            "lda ${0:x4},y",    // b9
            "lda ${0:x4},x",    // bd*/
            SourceLine line = new SourceLine();
            line.Instruction = "lda";
            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0xa1, 0x34 }, "lda ($34,x)");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xa5, 0x34 }, "lda $34");

            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0xa9, 0x34 }, "lda #$34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xad, 0x34, 0x12 }, "lda $1234");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0xb1, 0x34 }, "lda ($34),y");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0xb5, 0x34 }, "lda $34,x");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0xb9, 0x34, 0x00 }, "lda $0034,y");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0xbd, 0x34, 0x12 }, "lda $1234,x");
        }

        [Test]
        public void TestLdx()
        {
            /*
            "ldx #${0:x2}",     // a2
            "ldx ${0:x2}",      // a6
            "ldx ${0:x4}",      // ae
            "ldx ${0:x2},y",    // b6
            "ldx ${0:x4},y",    // be*/
            SourceLine line = new SourceLine();
            line.Instruction = "ldx";
            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0xa2, 0x34 }, "ldx #$34");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xa6, 0x34 }, "ldx $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xae, 0x34, 0x12 }, "ldx $1234");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0002, new byte[] { 0xb6, 0x34 }, "ldx $34,y");

            line.Operand = "$1234,y";
            TestInstruction(line, 0x0003, new byte[] { 0xbe, 0x34, 0x12 }, "ldx $1234,y");

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);
        }

        [Test]
        public void TestLdy()
        {
            /*
            "ldy #${0:x2}",     // a0
            "ldy ${0:x2}",      // a4
            "ldy ${0:x4}",      // ac
            "ldy ${0:x2},x",    // b4
            "ldy ${0:x4},x",    // bc*/
            SourceLine line = new SourceLine();
            line.Instruction = "ldy";
            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0xa0, 0x34 }, "ldy #$34");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0xa4, 0x34 }, "ldy $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0xac, 0x34, 0x12 }, "ldy $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0xb4, 0x34 }, "ldy $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0xbc, 0x34, 0x12 }, "ldy $1234,x");

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);
        }

        [Test]
        public void TestLsr()
        {
            /*
            "lsr ${0:x2}",      // 46
            "lsr",              // 4a
            "lsr ${0:x4}",      // 4e
            "lsr ${0:x2},x",    // 56
            "lsr ${0:x4},x",    // 5e*/

            SourceLine line = new SourceLine();
            line.Instruction = "lsr";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x46, 0x34 }, "lsr $34");

            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x4a }, "lsr");

            line.Operand = "a";
            TestInstruction(line, 0x0001, new byte[] { 0x4a }, "lsr");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x4e, 0x34, 0x12 }, "lsr $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x56, 0x34 }, "lsr $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x5e, 0x34, 0x12 }, "lsr $1234,x");

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestOra()
        {
            /*
            "ora (${0:x2},x)",  // 01
            "ora ${0:x2}",      // 05
            "ora #${0:x2}",     // 09
            "ora ${0:x4}",      // 0d
            "ora (${0:x2}),y",  // 11
            "ora ${0:x2},x",    // 15
            "ora ${0:x4},y",    // 19
            "ora ${0:x4},x",    // 1d*/
            SourceLine line = new SourceLine();
            line.Instruction = "ora";
            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x01, 0x34 }, "ora ($34,x)");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x05, 0x34 }, "ora $34");

            line.Operand = "#$34";
            TestInstruction(line, 0x0002, new byte[] { 0x09, 0x34 }, "ora #$34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x0d, 0x34, 0x12 }, "ora $1234");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0x11, 0x34 }, "ora ($34),y");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x15, 0x34 }, "ora $34,x");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0x19, 0x34, 0x00 }, "ora $0034,y");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x1d, 0x34, 0x12 }, "ora $1234,x");
        }

        [Test]
        public void TestPha()
        {
            TestImplied("pha", 0x48);
        }

        [Test]
        public void TestPhp()
        {
            TestImplied("php", 0x08);
        }

        [Test]
        public void TestPla()
        {
            TestImplied("pla", 0x68);
        }

        [Test]
        public void TestPlp()
        {
            TestImplied("plp", 0x28);
        }

        [Test]
        public void TestRol()
        {
            /*
            "rol ${0:x2}",      // 26
            "rol",              // 2a
            "rol ${0:x4}",      // 2e
            "rol ${0:x2},x",    // 36
            "rol ${0:x4},x",    // 3e
             */
            SourceLine line = new SourceLine();
            line.Instruction = "rol";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x26, 0x34 }, "rol $34");

            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x2a }, "rol");

            line.Operand = "a";
            TestInstruction(line, 0x0001, new byte[] { 0x2a }, "rol");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x2e, 0x34, 0x12 }, "rol $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x36, 0x34 }, "rol $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x3e, 0x34, 0x12 }, "rol $1234,x");

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);
        }

        [Test]
        public void TestRor()
        {
            /*
            "ror ${0:x2}",      // 66
            "ror",              // 6a
            "ror ${0:x4}",      // 6e
            "ror ${0:x2},x",    // 76
            "ror ${0:x4},x",    // 7e*/
            SourceLine line = new SourceLine();
            line.Instruction = "ror";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x66, 0x34 }, "ror $34");

            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x6a }, "ror");

            line.Operand = "a";
            TestInstruction(line, 0x0001, new byte[] { 0x6a }, "ror");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x6e, 0x34, 0x12 }, "ror $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x76, 0x34 }, "ror $34,x");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x7e, 0x34, 0x12 }, "ror $1234,x");

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
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
        public void TestRti()
        {
            TestImplied("rti", 0x40);
        }

        [Test]
        public void TestRts()
        {
            TestImplied("rts", 0x60);
        }

        [Test]
        public void TestSec()
        {
            TestImplied("sec", 0x38);
        }

        [Test]
        public void TestSed()
        {
            TestImplied("sed", 0xf8);
        }

        [Test]
        public void TestSei()
        {
            TestImplied("sei", 0x78);
        }

        [Test]
        public void TestSta()
        {
            /*
            "sta (${0:x2},x)",  // 81
            "sta ${0:x2}",      // 85
            "sta ${0:x4}",      // 8d
            "sta (${0:x2}),y",  // 91
            "sta ${0:x2},x",    // 95
            "sta ${0:x4},y",    // 99
            "sta ${0:x4},x",    // 9d*/
            SourceLine line = new SourceLine();
            line.Instruction = "sta";
            line.Operand = "($34,x)";
            TestInstruction(line, 0x0002, new byte[] { 0x81, 0x34 }, "sta ($34,x)");

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x85, 0x34 }, "sta $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x8d, 0x34, 0x12 }, "sta $1234");

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0x91, 0x34 }, "sta ($34),y");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x95, 0x34 }, "sta $34,x");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0003, new byte[] { 0x99, 0x34, 0x00 }, "sta $0034,y");

            line.Operand = "$1234,x";
            TestInstruction(line, 0x0003, new byte[] { 0x9d, 0x34, 0x12 }, "sta $1234,x");

            line.Operand = "#$34";
            TestForFailure(line);
        }

        [Test]
        public void TestStx()
        {
            /*
            "stx ${0:x2}",      // 86
            "stx ${0:x4}",      // 8e
            "stx ${0:x2},y",    // 96*/
            SourceLine line = new SourceLine();
            line.Instruction = "stx";

            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x86, 0x34 }, "stx $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x8e, 0x34, 0x12 }, "stx $1234");

            line.Operand = "$34,y";
            TestInstruction(line, 0x0002, new byte[] { 0x96, 0x34 }, "stx $34,y");

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);
        }

        [Test]
        public void TestSty()
        {
            /*
            "sty ${0:x2}",      // 84
            "sty ${0:x4}",      // 8c
            "sty ${0:x2},x",    // 94*/
            SourceLine line = new SourceLine();
            line.Instruction = "sty";
            line.Operand = "$34";
            TestInstruction(line, 0x0002, new byte[] { 0x84, 0x34 }, "sty $34");

            line.Operand = "$1234";
            TestInstruction(line, 0x0003, new byte[] { 0x8c, 0x34, 0x12 }, "sty $1234");

            line.Operand = "$34,x";
            TestInstruction(line, 0x0002, new byte[] { 0x94, 0x34 }, "sty $34,x");

            line.Operand = "#$34";
            TestForFailure(line);

            line.Operand = "$1234,y";
            TestForFailure(line);

            line.Operand = "($34,x)";
            TestForFailure(line);

            line.Operand = "($34),y";
            TestForFailure(line);

            line.Operand = "$1234,x";
            TestForFailure(line);
        }

        [Test]
        public void TestTax()
        {
            TestImplied("tax", 0xaa);
        }

        [Test]
        public void TestTay()
        {
            TestImplied("tay", 0xa8);
        }

        [Test]
        public void TestTsx()
        {
            TestImplied("tsx", 0xba);
        }

        [Test]
        public void TestTxa()
        {
            TestImplied("txa", 0x8a);
        }

        [Test]
        public void TestTxs()
        {
            TestImplied("txs", 0x9a);
        }

        [Test]
        public void TestTya()
        {
            TestImplied("tya", 0x98);
        }

        [Test]
        public void TestSyntaxErrors()
        {
            SourceLine line = new SourceLine();
            line.Instruction = "lda";
            line.Operand = "# 34";
            TestForFailure<ExpressionException>(line);

            line.Operand = "($34),y";
            TestInstruction(line, 0x0002, new byte[] { 0xb1, 0x34 }, "lda ($34),y");

            line.Operand = "#sqrt(25)";
            TestInstruction(line, 0x0002, new byte[] { 0xa9, 0x05 }, "lda #$05");

            line.Operand = "#4*23";
            TestInstruction(line, 0x0002, new byte[] { 0xa9, 0x5c }, "lda #$5c");

            line.Instruction = "sta";
            line.Operand = "$1000<<8";
            TestForFailure<OverflowException>(line);

            line.Instruction = "tyx";
            line.Operand = string.Empty;

            TestForFailure(line);

            line.Instruction = "lda";
            TestForFailure(line);

            line.Operand = "$10000";
            TestForFailure<OverflowException>(line);

            line.Operand = "$1000|$100";
            TestInstruction(line, 0x0003, new byte[] { 0xad, 0x00, 0x11 }, "lda $1100");

            line.Operand = "3<34";
            TestInstruction(line, 0x0002, new byte[] { 0xa5, 0x01 }, "lda $01");

            line.Operand = "#256";
            TestForFailure(line);

            line.Operand = "($1234),y";
            TestForFailure(line);

            line.Operand = "($12,x)";
            TestInstruction(line, 0x0002, new byte[] { 0xa1, 0x12 }, "lda ($12,x)");

            line.Instruction = "jmp";
            TestForFailure(line);

            line.Operand = "($12)";
            TestInstruction(line, 0x0003, new byte[] { 0x6c, 0x12, 0x00 }, "jmp ($0012)");

            line.Operand = "(65535+1)";
            TestForFailure<OverflowException>(line);

            line.Operand = "(65535)";
            TestInstruction(line, 0x0003, new byte[] { 0x6c, 0xff, 0xff }, "jmp ($ffff)");

            line.Operand = "()";
            TestForFailure<ExpressionException>(line);
            
            line.Operand = "0xffd2"; // oops wrong architecture!
            TestForFailure<ExpressionException>(line);

            line.Operand = "pow(2,4)";
            TestInstruction(line, 0x0003, new byte[] { 0x4c, 0x10, 0x00 }, "jmp $0010");

            line.Operand = "2**4";
            TestInstruction(line, 0x0003, new byte[] { 0x4c, 0x10, 0x00 }, "jmp $0010");
        }

        [Test]
        public void TestEncodings()
        {
            PseudoAssembler stringAsm = new PseudoAssembler(this.Controller);
            SourceLine line = new SourceLine();
            string teststring = "\"hello, world\"";

            var ascbytes = Encoding.ASCII.GetBytes(teststring.Trim('"'));
            var petbytes = Encoding.ASCII.GetBytes(teststring.Trim('"').ToUpper());
            var cbmscreenbytes = petbytes.Select(b =>
            {
                if (b >= '@' && b <= 'Z')
                    b -= 0x40;
                return b;
            });
            var atascreenbytes = ascbytes.Select(b =>
            {
                if (b < 96)
                    return Convert.ToByte(b - 32);
                return b;
            });

            line.Instruction = ".encoding";
            line.Operand = "petscii";
            stringAsm.AssembleLine(line);

            line.Instruction = ".string";
            line.Operand = teststring;
            TestInstruction(line, stringAsm, petbytes.Count(), petbytes.Count(), petbytes);

            line.Instruction = ".byte";
            line.Operand = "'└'";
            TestInstruction(line, stringAsm, 0x0001, 1, new byte[] { 0xad });

            line.Instruction = ".encoding";
            line.Operand = "atascreen";
            stringAsm.AssembleLine(line);

            line.Instruction = ".string";
            line.Operand = teststring;
            TestInstruction(line, stringAsm, atascreenbytes.Count(), atascreenbytes.Count(), atascreenbytes.ToArray());

            line.Instruction = ".encoding";
            line.Operand = "cbmscreen";
            stringAsm.AssembleLine(line);

            line.Instruction = ".string";
            line.Operand = teststring.ToUpper();
            TestInstruction(line, stringAsm, cbmscreenbytes.Count(), cbmscreenbytes.Count(), cbmscreenbytes.ToArray());
        }
    }
}
