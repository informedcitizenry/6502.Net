using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGen65816 : TestBase
    {
        [TestMethod]
        public void Sep()
        {
            var parse = ParseSource(" sep #$30", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Dir()
        {
            var parse = ParseSource("lda [$80],y", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb7, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);
        }

        [TestMethod]
        public void Mvn()
        {
            var parse = ParseSource("mvn $30,$50", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x54, bytes[0]);
            Assert.AreEqual(0x30, bytes[1]);
            Assert.AreEqual(0x50, bytes[2]);
        }

        [TestMethod]
        public void LongJump()
        {
            var parse = ParseSource("jml $10ffff", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x5c, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
            Assert.AreEqual(0x10, bytes[3]);

            parse = ParseSource("jsr $10ffff", "65816");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x22, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
            Assert.AreEqual(0x10, bytes[3]);
        }

        [TestMethod]
        public void IndSpY()
        {
            var parse = ParseSource("lda ($80,s),y", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb3, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);
        }

        [TestMethod]
        public void Brl()
        {
            var parse = ParseSource("brl 4096", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x82, bytes[0]);
            Assert.AreEqual(0xfd, bytes[1]);
            Assert.AreEqual(0x0f, bytes[2]);

        }

        [TestMethod]
        public void M16()
        {
            var parse = ParseSource(
@".m16
    lda #0", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(Services.Log.HasWarnings);
            Assert.AreEqual(2, Services.Output.ProgramCounter);

            parse = ParseSource(
@" .m16
    lda #0", "65816");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.Log.HasWarnings);
            Assert.AreEqual(3, Services.Output.ProgramCounter);

            parse = ParseSource(
@" lda #0", "65816");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void Dp()
        {
            var parse = ParseSource(
@"      .dp $01
        lda $01fd", "65816");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa5, bytes[0]);
            Assert.AreEqual(0xfd, bytes[1]);
        }
    }
}
