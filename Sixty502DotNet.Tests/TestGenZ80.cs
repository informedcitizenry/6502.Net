using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGenZ80 : TestBase
    {
        [TestMethod]
        public void Implied()
        {
            var parse = ParseSource("NOP", "z80");
            var tree = parse.source();
            Assert.IsInstanceOfType(parse.TokenStream.TokenSource, typeof(LexerBase));
            var lexer = parse.TokenStream.TokenSource as LexerBase;
            Assert.IsInstanceOfType(lexer.InstructionSet, typeof(Z80));
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat());
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat().implStat());
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat().implStat().mnemonic().NOP());
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            Assert.AreEqual(0, Services.Output.GetCompilation()[0]);

            parse = ParseSource("scf", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            Assert.AreEqual(0x37, Services.Output.GetCompilation()[0]);

            parse = ParseSource("reti", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xed, bytes[0]);
            Assert.AreEqual(0x4d, bytes[1]);

            parse = ParseSource("brk", "z80");
            tree = parse.source();
            lexer = parse.TokenStream.TokenSource as LexerBase;
            Assert.IsNotNull(lexer);
            Assert.IsInstanceOfType(lexer.InstructionSet, typeof(Z80));
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var brk = Services.Symbols.Scope.Resolve("brk") as Label;
            Assert.IsNotNull(brk);
            Assert.IsTrue(brk.Value.IsDefined);
            Assert.IsTrue(brk.Value.IsIntegral);
            Assert.AreEqual(0, brk.Value.ToInt());
        }

        [TestMethod]
        public void Immediate()
        {
            var parse = ParseSource("and 42", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat()?.mnemonic().AND());
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xe6, bytes[0]);
            Assert.AreEqual(42, bytes[1]);

            parse = ParseSource("ld b,42", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x06, bytes[0]);
            Assert.AreEqual(42, bytes[1]);

            parse = ParseSource("ld b,-1", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x06, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);

            parse = ParseSource("ld bc,$00ff", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x01, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);
        }


        [TestMethod]
        public void Reg()
        {
            var parse = ParseSource("push af", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xf5, bytes[0]);

            parse = ParseSource("and b", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }


        [TestMethod]
        public void RegExpr()
        {
            var parse = ParseSource("ld l,42", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);

        }

        [TestMethod]
        public void RegReg()
        {
            var parse = ParseSource("ex af,af'", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            Assert.AreEqual(8, Services.Output.GetCompilation()[0]);

            parse = ParseSource("adc a,b", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Indirect()
        {
            var parse = ParseSource("in a,(42)", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xdb, bytes[0]);
            Assert.AreEqual(42, bytes[1]);

            parse = ParseSource("out (42),a", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xd3, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void IndirectExtended()
        {
            var parse = ParseSource("ld hl,($00ff)", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x2a, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);

            parse = ParseSource(
@"myconst  .equ $e200
        ld a,(myconst)", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x3a, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0xe2, bytes[2]);

            parse = ParseSource("ld a,($ff)", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x3a, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);

            parse = ParseSource("ld ($ff),a", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x32, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);

            parse = ParseSource("ld ($1000),hl", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x22, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0x10, bytes[2]);
        }

        [TestMethod]
        public void IndReg()
        {
            var parse = ParseSource("inc (hl)", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void IndRegReg()
        {
            var parse = ParseSource("ld (hl),a", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Relative()
        {
            var parse = ParseSource(
@"* = $c000
start nop
djnz start", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc003, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x00, bytes[0]);
            Assert.AreEqual(0x10, bytes[1]);
            Assert.AreEqual(0xfe, bytes[2]);

            parse = ParseSource("jr z,3", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x28, bytes[0]);
            Assert.AreEqual(0x02, bytes[1]);

            parse = ParseSource("jr nc,132", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Indexed()
        {
            var parse = ParseSource("ld (ix+$40),a", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xdd, bytes[0]);
            Assert.AreEqual(0x77, bytes[1]);
            Assert.AreEqual(0x40, bytes[2]);

            parse = ParseSource("ld a,(ix+$40)", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xdd, bytes[0]);
            Assert.AreEqual(0x7e, bytes[1]);
            Assert.AreEqual(0x40, bytes[2]);

            parse = ParseSource("ld b,(ix-1)", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xdd, bytes[0]);
            Assert.AreEqual(0x46, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);

            parse = ParseSource("ld (ix+42),3", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xdd, bytes[0]);
            Assert.AreEqual(0x36, bytes[1]);
            Assert.AreEqual(42, bytes[2]);
            Assert.AreEqual(3, bytes[3]);

            parse = ParseSource("ld b,(ix-255)", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Bit()
        {
            var parse = ParseSource("bit 2,b", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xcb, bytes[0]);
            Assert.AreEqual(0x50, bytes[1]);

            parse = ParseSource("set 7,(ix+$40)", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xdd, bytes[0]);
            Assert.AreEqual(0xcb, bytes[1]);
            Assert.AreEqual(0x40, bytes[2]);
            Assert.AreEqual(0xfe, bytes[3]);

            parse = ParseSource("res 4,(iy-3),h", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xfd, bytes[0]);
            Assert.AreEqual(0xcb, bytes[1]);
            Assert.AreEqual(0xfd, bytes[2]);
            Assert.AreEqual(0xa4, bytes[3]);

            parse = ParseSource("bit 8,b", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

        }

        [TestMethod]
        public void Rst()
        {
            var parse = ParseSource("rst $20", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            Assert.AreEqual(0xe7, Services.Output.GetCompilation()[0]);

            parse = ParseSource("rst 20", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Im()
        {
            var parse = ParseSource("im 0", "z80");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xed, bytes[0]);
            Assert.AreEqual(0x46, bytes[1]);

            parse = ParseSource("im 3", "z80");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
