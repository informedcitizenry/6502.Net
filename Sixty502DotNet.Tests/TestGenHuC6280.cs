using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGenHuC6280 : TestBase
    {
        [TestMethod]
        public void TstZp()
        {
            var parse = ParseSource("tst #42,42", "HuC6280");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x83, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(42, bytes[2]);
        }

        [TestMethod]
        public void TstZpX()
        {
            var parse = ParseSource("tst #42,42,x", "HuC6280");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x93, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(42, bytes[2]);
        }

        [TestMethod]
        public void TstAbs()
        {
            var parse = ParseSource("tst #42,1023", "HuC6280");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa3, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
            Assert.AreEqual(0x03, bytes[3]);
        }

        [TestMethod]
        public void TestAbsX()
        {
            var parse = ParseSource("tst #42,1023,x", "HuC6280");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xb3, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
            Assert.AreEqual(0x03, bytes[3]);
        }

        [TestMethod]
        public void TII()
        {
            var parse = ParseSource("tii 1023,2047,4095", "HuC6280");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(7, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x73, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x03, bytes[2]);
            Assert.AreEqual(0xff, bytes[3]);
            Assert.AreEqual(0x07, bytes[4]);
            Assert.AreEqual(0xff, bytes[5]);
            Assert.AreEqual(0x0f, bytes[6]);
        }
    }
}
