using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGen6502 : TestBase
    {
        [TestMethod]
        public void Implied()
        {
            var parse = ParseSource("brk", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0, bytes[0]);

            parse = ParseSource("nop", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xea, bytes[0]);
        }

        [TestMethod]
        public void ImpliedA()
        {
            var parse = ParseSource("asl", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa, bytes[0]);

            parse = ParseSource("asl a", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa, bytes[0]);

            parse = ParseSource("brk a", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Immediate()
        {
            var parse = ParseSource("lda #$80", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa9, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);

            parse = ParseSource("lda #$100", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource("brk #$80", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Zp()
        {
            var parse = ParseSource("lda $0080", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa5, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);
        }

        [TestMethod]
        public void ZpIx()
        {
            var parse = ParseSource("lda $0080,x", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb5, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);

            parse = ParseSource("ldx $0080,x", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Abs()
        {
            var parse = ParseSource("lda $100", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xad, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0x01, bytes[2]);
        }

        [TestMethod]
        public void AbsIx()
        {
            var parse = ParseSource("lda $0080,y", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb9, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);

            parse = ParseSource("ldx [16] $0080,y", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xbe, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);
        }

        [TestMethod]
        public void IndAbs()
        {
            var parse = ParseSource("jmp ($1000)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x6c, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0x10, bytes[2]);

            parse = ParseSource("jmp ($00ff)", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x6c, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);

            parse = ParseSource("jmp ($00ff)+1", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x4c, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0x01, bytes[2]);
        }

        [TestMethod]
        public void IndY()
        {
            var parse = ParseSource("lda (42),y", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb1, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void IndX()
        {
            var parse = ParseSource("lda (42,x)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa1, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void Relative()
        {
            var parse = ParseSource(
@"bne 3
    nop
 rts", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xd0, bytes[0]);
            Assert.AreEqual(0x01, bytes[1]);
            Assert.AreEqual(0xea, bytes[2]);
            Assert.AreEqual(0x60, bytes[3]);

            parse = ParseSource(
@"nop
    nop
bne 0", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xd0, bytes[2]);
            Assert.AreEqual(0xfc, bytes[3]);
        }

        [TestMethod]
        public void PseudoRelative()
        {
            var parse = ParseSource(
@"jne 3
    nop
rts", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xd0, bytes[0]);
            Assert.AreEqual(0x01, bytes[1]);

            parse = ParseSource(
@" jne $c000
 nop
 rts", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(7, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xf0, bytes[0]);
            Assert.AreEqual(0x03, bytes[1]);
            Assert.AreEqual(0x4c, bytes[2]);
            Assert.AreEqual(0x00, bytes[3]);
            Assert.AreEqual(0xc0, bytes[4]);
            Assert.AreEqual(0xea, bytes[5]);
            Assert.AreEqual(0x60, bytes[6]);
        }
    }
}
