using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGeni8080 : TestBase
    {
        [TestMethod]
        public void Mov()
        {
            var parse = ParseSource("mov a,b", "i8080");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x78, bytes[0]);
        }

        [TestMethod]
        public void Mvi()
        {
            var parse = ParseSource("mvi m,42", "i8080");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x36, bytes[0]);
            Assert.AreEqual(42, bytes[1]);

            parse = ParseSource("mvi m,$ffd2", "i8080");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

        }

        [TestMethod]
        public void Lxi()
        {
            var parse = ParseSource("lxi h,42", "i8080");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x21, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);

            parse = ParseSource("lxi m,42", "i8080");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Pop()
        {
            var parse = ParseSource("pop psw", "i8080");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xf1, bytes[0]);
        }
    }
}
