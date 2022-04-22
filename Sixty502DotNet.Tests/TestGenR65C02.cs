using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGenR65C02 : TestBase
    {

        [TestMethod]
        public void RMB()
        {
            var parse = ParseSource("rmb 0,42", "R65C02");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x07, bytes[0]);
            Assert.AreEqual(42, bytes[1]);

            parse = ParseSource("rmb 7,42", "R65C02");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x77, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
        }

        [TestMethod]
        public void BBR()
        {
            var parse = ParseSource("bbr 0,42,9", "R65C02");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x0f, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(6, bytes[2]);

            parse = ParseSource(
@"* = $c000
    bbr 7,42,$c009", "R65C02");
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc003, Services.Output.ProgramCounter);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x7f, bytes[0]);
            Assert.AreEqual(42, bytes[1]);
            Assert.AreEqual(6, bytes[2]);
        }
    }
}
