using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGen6809 : TestBase
    {
        [TestMethod]
        public void Implied()
        {
            var parse = ParseSource("nop", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x12, bytes[0]);

            parse = ParseSource("daa", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x19, bytes[0]);

        }

        [TestMethod]
        public void Immediate()
        {
            var parse = ParseSource("ldb #42", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xc6, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void ImmAbs()
        {
            var parse = ParseSource("addd #42", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xc3, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(42, bytes[2]);

            parse = ParseSource("ldx #$4020", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x8e, bytes[0]);
            Assert.AreEqual(0x40, bytes[1]);
            Assert.AreEqual(0x20, bytes[2]);
        }

        [TestMethod]
        public void Zp()
        {
            var parse = ParseSource("ldx 42", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x9e, bytes[0]);
            Assert.AreEqual(42, bytes[1]);

            parse = ParseSource("jmp 42", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x0e, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void ZeroIncrement()
        {
            var parse = ParseSource("leax ,x", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0x84, bytes[1]);

            parse = ParseSource("leax ,s", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0xe4, bytes[1]);

            parse = ParseSource("leax [,x]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0x94, bytes[1]);
        }

        [TestMethod]
        public void SingleIncrement()
        {
            var parse = ParseSource("leax ,x+", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);

            parse = ParseSource("lda ,y+", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa6, bytes[0]);
            Assert.AreEqual(0b1_01_00000, bytes[1]);

            parse = ParseSource("lda ,u+", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa6, bytes[0]);
            Assert.AreEqual(0b1_10_00000, bytes[1]);

            parse = ParseSource("lda ,s+", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa6, bytes[0]);
            Assert.AreEqual(0b1_11_00000, bytes[1]);

            parse = ParseSource("lda [,s+]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void SingleDecrement()
        {
            var parse = ParseSource("leax ,-x", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0x82, bytes[1]);

            parse = ParseSource("lda ,-y", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa6, bytes[0]);
            Assert.AreEqual(0b1_01_00010, bytes[1]);

            parse = ParseSource("lda ,-u", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa6, bytes[0]);
            Assert.AreEqual(0b1_10_00010, bytes[1]);

            parse = ParseSource("lda ,-s", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa6, bytes[0]);
            Assert.AreEqual(0b1_11_00010, bytes[1]);

            parse = ParseSource("lda [,-s]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void DoubleIncrement()
        {
            var parse = ParseSource("leax ,x++", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0x81, bytes[1]);

            parse = ParseSource("leax [,x++]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_1_0001, bytes[1]);
        }

        [TestMethod]
        public void DoubleDecrement()
        {
            var parse = ParseSource("leax ,--s", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_11_0_0011, bytes[1]);

            parse = ParseSource("leax [,--s]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_11_1_0011, bytes[1]);
        }

        [TestMethod]
        public void AccumulatorOffset()
        {
            var parse = ParseSource("leax a,x", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_0_0110, bytes[1]);

            parse = ParseSource("leax b,y", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_01_0_0101, bytes[1]);

            parse = ParseSource("jmp d,s", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x6e, bytes[0]);
            Assert.AreEqual(0b1_11_0_1011, bytes[1]);

            parse = ParseSource("leax [d,s]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_11_1_1011, bytes[1]);

            parse = ParseSource("                LDD        B,U", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void Offset5Bit()
        {
            var parse = ParseSource("leax +14,x", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b0000_0000 | 14, bytes[1]);

            parse = ParseSource("leax -15,s", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b011_10001, bytes[1]);
        }

        [TestMethod]
        public void Offset8Bit()
        {
            var parse = ParseSource("leax -17,x", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_0_1000, bytes[1]);
            Assert.AreEqual(-17 & 0xff, bytes[2]);

            parse = ParseSource("leax [15,x]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_1_1000, bytes[1]);
            Assert.AreEqual(15, bytes[2]);

            parse = ParseSource("leax -16,pc", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_0_1100, bytes[1]);
            Assert.AreEqual(0b1110_1101, bytes[2]);
        }

        [TestMethod]
        public void Offset16Bit()
        {
            var parse = ParseSource("leax -129,x", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_0_1001, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
            Assert.AreEqual(0x7f, bytes[3]);

            parse = ParseSource("leax 131,pc", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1_00_0_1101, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);
            Assert.AreEqual(127, bytes[3]);

            parse = ParseSource("jsr [$0300,pc]", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xad, bytes[0]);
            Assert.AreEqual(0b1001_1101, bytes[1]);
            Assert.AreEqual(0x02, bytes[2]);
            Assert.AreEqual(0xfc, bytes[3]);

            parse = ParseSource("leax -4,y++", "m6809");
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void ExtendedIndirect()
        {
            var parse = ParseSource("leax [128]", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x30, bytes[0]);
            Assert.AreEqual(0b1001_1111, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);
            Assert.AreEqual(128, bytes[3]);
        }

        [TestMethod]
        public void Pushpull()
        {
            var parse = ParseSource("pshs b", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x34, bytes[0]);

            parse = ParseSource("pshs a,b,cc,dp", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x34, bytes[0]);
            Assert.AreEqual(0b1111, bytes[1]);

            parse = ParseSource("pulu a,b,cc,dp,pc", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x37, bytes[0]);
            Assert.AreEqual(0b1000_1111, bytes[1]);

            parse = ParseSource("puls a,b,s", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource("puls a,d", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource("puls [a,b]", "m6809");
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Relative()
        {
            var parse = ParseSource(
@"* = $8000
bsr $8010", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x8002, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x8d, bytes[0]);
            Assert.AreEqual(0x0e, bytes[1]);

            parse = ParseSource(
@"* = $8000
lbsr $4000", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x8003, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x17, bytes[0]);
            Assert.AreEqual(0xbf, bytes[1]);
            Assert.AreEqual(0xfd, bytes[2]);

            parse = ParseSource(
@"lbsr 125", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x17, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(122, bytes[2]);
        }

        [TestMethod]
        public void TransferExchange()
        {
            var parse = ParseSource("tfr a,dp", "m6809");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x1f, bytes[0]);
            Assert.AreEqual(0b1000_1011, bytes[1]);

            parse = ParseSource("tfr a,d", "m6809");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
