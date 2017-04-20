using System;
using System.Collections.Generic;
using Asm6502.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest6502.Net
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void TestHtmlOutput()
        {
            List<SourceLine> lines = new List<SourceLine>();
            SourceLine line2 = new SourceLine();
                                //1234567890123456789012345
            line2.SourceString = "mylabel lda message + 3,x";
            line2.Parse(r => r.Equals("lda"), s => s.Equals("mylabel"));
            line2.PC = 0xc002;
            line2.Assembly.AddRange(new byte[] { 0xbd, 0x1f, 0xc0 });
            line2.Disassembly = "lda $c01f,x";

            SourceLine line0 = new SourceLine();
            line0.SourceString = "      ldx #0";
            line0.Parse(r => r.Equals("ldx"), s => false);
            line0.PC = 0xc000;
            line0.Assembly.AddRange(new byte[] { 0xa2, 0x00 });
            line0.Disassembly = "ldx #$00";

            SourceLine line3 = new SourceLine();
            line3.SourceString = "message .string \"hello world\"";
            line3.Parse(r => r.Equals(".string"), s=> s.Equals("message"));
            line3.PC = 0xc005;
            line3.Assembly.AddRange(new byte[] { 0x48, 0x45, 0x4c, 0x4c, 0x4f, 0x20, 0x57, 0x4f, 0x52, 0x4c, 0x44 });
            line3.Disassembly = string.Empty;

            lines.Add(line0);
            lines.Add(line2);
            lines.Add(line3);

            Assert.AreEqual("mylabel", line2.Label);
            Assert.AreEqual("lda", line2.Instruction);
            Assert.AreEqual("message + 3,x", line2.Operand);


            HtmlOutput html = new HtmlOutput();
            string output = html.GetHtml(lines);
            Assert.IsTrue(output.Contains("<"));

        }
    }
}
