using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGen65CE02 : TestBase
    {
        [TestMethod]
        public void Relative()
        {
            var parse = ParseSource("bcs 1024", "65CE02");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb3, bytes[0]);
            Assert.AreEqual(0xfd, bytes[1]);
            Assert.AreEqual(0x03, bytes[2]);

            parse = ParseSource("bge 1024", "65CE02");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb3, bytes[0]);
            Assert.AreEqual(0xfd, bytes[1]);
            Assert.AreEqual(0x03, bytes[2]);
        }

        [TestMethod]
        public void IndirectSp()
        {
            var parse = ParseSource("lda (42,sp),y", "65CE02");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xe2, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void IndirectZ()
        {

            var parse = ParseSource("lda (42),z", "65CE02");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb2, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }
    }
}
