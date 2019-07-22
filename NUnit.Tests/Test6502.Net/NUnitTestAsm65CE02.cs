using DotNetAsm;
using NUnit.Framework;
namespace NUnit.Tests.Test6502.Net
{
    [TestFixture]
    public class NUnitTestAsm65CE02 : NUnitTestAsm6502Base
    {
        protected override void TestRelativeBranch(string mnemonic, byte opcode, bool testOverflow = false)
        {
            SetCpu("65CE02");
            var line = new SourceLine
            {
                Instruction = mnemonic,
                Operand = "$0082"
            };
            TestInstruction(line, 0x0003, new byte[] { opcode, 0x7f, 0x00 }, mnemonic + " " + line.Operand);

            line.Operand = "-$007f";
            TestInstruction(line, 0x0003, new byte[] { opcode, 0x7e, 0xff }, mnemonic + " $ff81");

            line.Operand = "$8000";
            TestInstruction(line, 0x0003, new byte[] { opcode, 0xfd, 0x7f }, mnemonic + " " + line.Operand);

            line.Operand = "$c000";
            TestInstruction(line, 0x0003, new byte[] { opcode, 0xfd, 0xbf }, mnemonic + " " + line.Operand);

            line.Operand = "$10000";
            TestForFailure(line);

            opcode -= 3;
            base.TestRelativeBranch(mnemonic, opcode, false);
        }

        [Test]
        public void TestRockwellInstructions()
        {
            SetCpu("R65C02");
            var line = new SourceLine
            {
                Instruction = "rmb",
                Operand = "0,$25"
            };
            TestInstruction(line, 0x0002, new byte[] { 0x07, 0x25 }, "rmb 0,$25");

            line.Operand = "1,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x17, 0x25 }, "rmb 1,$25");

            line.Operand = "2,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x27, 0x25 }, "rmb 2,$25");

            line.Operand = "3,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x37, 0x25 }, "rmb 3,$25");

            line.Operand = "4,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x47, 0x25 }, "rmb 4,$25");

            line.Operand = "5,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x57, 0x25 }, "rmb 5,$25");

            line.Operand = "6,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x67, 0x25 }, "rmb 6,$25");

            line.Operand = "7,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x77, 0x25 }, "rmb 7,$25");

            line.Instruction = "smb";
            line.Operand = "0,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x87, 0x25 }, "smb 0,$25");

            line.Operand = "1,$25";
            TestInstruction(line, 0x0002, new byte[] { 0x97, 0x25 }, "smb 1,$25");

            line.Operand = "2,$25";
            TestInstruction(line, 0x0002, new byte[] { 0xa7, 0x25 }, "smb 2,$25");

            line.Operand = "3,$25";
            TestInstruction(line, 0x0002, new byte[] { 0xb7, 0x25 }, "smb 3,$25");

            line.Operand = "4,$25";
            TestInstruction(line, 0x0002, new byte[] { 0xc7, 0x25 }, "smb 4,$25");

            line.Operand = "5,$25";
            TestInstruction(line, 0x0002, new byte[] { 0xd7, 0x25 }, "smb 5,$25");

            line.Operand = "6,$25";
            TestInstruction(line, 0x0002, new byte[] { 0xe7, 0x25 }, "smb 6,$25");

            line.Operand = "7,$25";
            TestInstruction(line, 0x0002, new byte[] { 0xf7, 0x25 }, "smb 7,$25");

            line.Instruction = "bbr";
            line.Operand = "0,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x0f, 0x25, 0x02 }, "bbr 0,$25,$04");

            line.Operand = "1,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x1f, 0x25, 0x02 }, "bbr 1,$25,$04");

            line.Operand = "2,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x2f, 0x25, 0x02 }, "bbr 2,$25,$04");

            line.Operand = "3,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x3f, 0x25, 0x02 }, "bbr 3,$25,$04");

            line.Operand = "4,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x4f, 0x25, 0x02 }, "bbr 4,$25,$04");

            line.Operand = "5,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x5f, 0x25, 0x02 }, "bbr 5,$25,$04");

            line.Operand = "6,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x6f, 0x25, 0x02 }, "bbr 6,$25,$04");

            line.Operand = "7,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x7f, 0x25, 0x02 }, "bbr 7,$25,$04");

            line.Instruction = "bbs";
            line.Operand = "0,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x8f, 0x25, 0x02 }, "bbs 0,$25,$04");

            line.Operand = "1,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0x9f, 0x25, 0x02 }, "bbs 1,$25,$04");

            line.Operand = "2,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0xaf, 0x25, 0x02 }, "bbs 2,$25,$04");

            line.Operand = "3,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0xbf, 0x25, 0x02 }, "bbs 3,$25,$04");

            line.Operand = "4,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0xcf, 0x25, 0x02 }, "bbs 4,$25,$04");

            line.Operand = "5,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0xdf, 0x25, 0x02 }, "bbs 5,$25,$04");

            line.Operand = "6,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0xef, 0x25, 0x02 }, "bbs 6,$25,$04");

            line.Operand = "7,$25,$04";
            TestInstruction(line, 0x0003, new byte[] { 0xff, 0x25, 0x02 }, "bbs 7,$25,$04");
        }

        [Test]
        public void TestBranches()
        {
            TestRelativeBranch("bpl", 0x13);
            TestRelativeBranch("bmi", 0x33);
            TestRelativeBranch("bvc", 0x53);
            TestRelativeBranch("bvs", 0x73);
            TestRelativeBranch("bra", 0x83);
            TestRelativeBranch("bcc", 0x93);
            TestRelativeBranch("bcs", 0xb3);
            TestRelativeBranch("bne", 0xd3);
            TestRelativeBranch("beq", 0xf3);

            var line = new SourceLine
            {
                Instruction = "blt",
                Operand = "$0005"
            };
            TestInstruction(line, 0x0003, new byte[] { 0x93, 0x02, 0x00 }, "blt $0005");
            line.Instruction = "bge";
            TestInstruction(line, 0x0003, new byte[] { 0xb3, 0x02, 0x00 }, "bge $0005");
        }

        [Test]
        public void Test65CE02()
        {
            SetCpu("65CE02");

            var line = new SourceLine
            {
                Instruction = "cle"
            };
            TestInstruction(line, 0x0001, new byte[] { 0x02 }, "cle");

            line.Instruction = "see";
            TestInstruction(line, 0x0001, new byte[] { 0x03 }, "see");

            line.Instruction = "tsy";
            TestInstruction(line, 0x0001, new byte[] { 0x0b }, "tsy");

            line.Instruction = "ora";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0x12, 0xfb }, "ora ($fb),z");

            line.Instruction = "inz";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x1b }, "inz");

            line.Instruction = "jsr";
            line.Operand = "($ffd2)";
            TestInstruction(line, 0x0003, new byte[] { 0x22, 0xd2, 0xff }, "jsr ($ffd2)");

            line.Operand = "($ffd2,x)";
            TestInstruction(line, 0x0003, new byte[] { 0x23, 0xd2, 0xff }, "jsr ($ffd2,x)");

            line.Instruction = "tys";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x2b }, "tys");

            line.Instruction = "and";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0x32, 0xfb }, "and ($fb),z");

            line.Instruction = "dez";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x3b }, "dez");

            line.Instruction = "neg";
            TestInstruction(line, 0x0001, new byte[] { 0x42 }, "neg");

            line.Instruction = "asr";
            TestInstruction(line, 0x0001, new byte[] { 0x43 }, "asr");

            line.Operand = "$fb";
            TestInstruction(line, 0x0002, new byte[] { 0x44, 0xfb }, "asr $fb");

            line.Instruction = "taz";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x4b }, "taz");

            line.Instruction = "eor";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0x52, 0xfb }, "eor ($fb),z");

            line.Instruction = "asr";
            line.Operand = "$fb,x";
            TestInstruction(line, 0x0002, new byte[] { 0x54, 0xfb }, "asr $fb,x");

            line.Instruction = "tab";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x5b }, "tab");

            line.Instruction = "map";
            TestInstruction(line, 0x0001, new byte[] { 0x5c }, "map");

            line.Instruction = "rtn";
            TestInstruction(line, 0x0001, new byte[] { 0x62 }, "rtn");

            line.Instruction = "bsr";
            line.Operand = "$ffd2";
            TestInstruction(line, 0x0003, new byte[] { 0x63, 0xd2, 0xff }, "bsr $ffd2");

            line.Instruction = "tza";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x6b }, "tza");

            line.Instruction = "adc";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0x72, 0xfb }, "adc ($fb),z");

            line.Instruction = "tba";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0x7b }, "tba");

            line.Instruction = "sta";
            line.Operand = "($fb,sp),y";
            TestInstruction(line, 0x0002, new byte[] { 0x82, 0xfb }, "sta ($fb,sp),y");

            line.Instruction = "sty";
            line.Operand = "$01fb,x";
            TestInstruction(line, 0x0003, new byte[] { 0x8b, 0xfb, 0x01 }, "sty $01fb,x");

            line.Instruction = "sta";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0x92, 0xfb }, "sta ($fb),z");

            line.Instruction = "stx";
            line.Operand = "$01fb,y";
            TestInstruction(line, 0x0003, new byte[] { 0x9b, 0xfb, 0x01 }, "stx $01fb,y");

            line.Instruction = "ldz";
            line.Operand = "#$fb";
            TestInstruction(line, 0x0002, new byte[] { 0xa3, 0xfb }, "ldz #$fb");

            line.Operand = "$fb";
            TestInstruction(line, 0x0003, new byte[] { 0xab, 0xfb, 0x00 }, "ldz $00fb");

            line.Instruction = "lda";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0xb2, 0xfb }, "lda ($fb),z");

            line.Instruction = "ldz";
            line.Operand = "$fb,x";
            TestInstruction(line, 0x0003, new byte[] { 0xbb, 0xfb, 0x00 }, "ldz $00fb,x");

            line.Instruction = "cpz";
            line.Operand = "#$fb";
            TestInstruction(line, 0x0002, new byte[] { 0xc2, 0xfb }, "cpz #$fb");

            line.Instruction = "dew";
            line.Operand = "$fb";
            TestInstruction(line, 0x0002, new byte[] { 0xc3, 0xfb }, "dew $fb");

            line.Instruction = "asw";
            TestInstruction(line, 0x0003, new byte[] { 0xcb, 0xfb, 0x00 }, "asw $00fb");

            line.Instruction = "cmp";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0xd2, 0xfb }, "cmp ($fb),z");

            line.Instruction = "cpz";
            line.Operand = "$fb";
            TestInstruction(line, 0x0002, new byte[] { 0xd4, 0xfb }, "cpz $fb");

            line.Instruction = "phz";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0xdb }, "phz");

            line.Instruction = "cpz";
            line.Operand = "$01fb";
            TestInstruction(line, 0x0003, new byte[] { 0xdc, 0xfb, 0x01 }, "cpz $01fb");

            line.Instruction = "lda";
            line.Operand = "($fb,sp),y";
            TestInstruction(line, 0x0002, new byte[] { 0xe2, 0xfb }, "lda ($fb,sp),y");

            line.Instruction = "inw";
            line.Operand = "$fb";
            TestInstruction(line, 0x0002, new byte[] { 0xe3, 0xfb }, "inw $fb");

            line.Instruction = "row";
            TestInstruction(line, 0x0003, new byte[] { 0xeb, 0xfb, 0x00 }, "row $00fb");

            line.Instruction = "sbc";
            line.Operand = "($fb),z";
            TestInstruction(line, 0x0002, new byte[] { 0xf2, 0xfb }, "sbc ($fb),z");

            line.Instruction = "phw";
            line.Operand = "#$fb";
            TestInstruction(line, 0x0003, new byte[] { 0xf4, 0xfb, 0x00 }, "phw #$00fb");

            line.Instruction = "plz";
            line.Operand = string.Empty;
            TestInstruction(line, 0x0001, new byte[] { 0xfb }, "plz");

            line.Instruction = "phw";
            line.Operand = "$fb";
            TestInstruction(line, 0x0003, new byte[] { 0xfc, 0xfb, 0x00 }, "phw $00fb");
        }
    }
}
