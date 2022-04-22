using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGen6800 : TestBase
    {
        [TestMethod]
        public void Abs()
        {
            var parse = ParseSource("jsr $ffd2", "m6800");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xbd, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0xd2, bytes[2]);
        }

        [TestMethod]
        public void Zp()
        {
            var parse = ParseSource("cmpa $80", "m6800");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x91, bytes[0]);
            Assert.AreEqual(0x80, bytes[1]);
        }

        [TestMethod]
        public void ZpX()
        {
            var parse = ParseSource("jsr 42,x", "m6800");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xad, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void Relative()
        {
            var parse = ParseSource(
@" * = $8000
bhi $8003", "m6800");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x8000, Services.Output.ProgramStart);
            Assert.AreEqual(0x8002, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x22, bytes[0]);
            Assert.AreEqual(0x01, bytes[1]);
        }

        [TestMethod]
        public void Immediate()
        {
            var parse = ParseSource("suba #42", "m6800");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x80, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }
    }
}
