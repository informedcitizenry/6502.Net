using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.Test6502.Net
{
    [TestFixture]
    public class NUnitTestPseudo6502 : TestDotNetAsm.NUnitAsmTestBase
    {
        public NUnitTestPseudo6502()
        {
            Controller = new TestDotNetAsm.TestController();
            LineAssembler = new Asm6502.Net.Pseudo6502(Controller);
        }

        [Test]
        public void TestEncodings()
        {
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

            line.Instruction = ".petscii";
            line.Operand = teststring;
            TestInstruction(line, petbytes.Count(), petbytes.Count(), petbytes);

            line.Instruction = ".atascreen";
            TestInstruction(line, atascreenbytes.Count(), atascreenbytes.Count(), atascreenbytes);

            line.Instruction = ".cbmscreen";
            line.Operand = teststring.ToUpper();
            TestInstruction(line, cbmscreenbytes.Count(), cbmscreenbytes.Count(), cbmscreenbytes);

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
    }
}
