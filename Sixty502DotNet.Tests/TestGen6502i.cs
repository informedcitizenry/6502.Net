using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGen6502i : TestBase
    {
        [TestMethod]
        public void Isb()
        {
            var parse = ParseSource("isb $ffff,x", "6502i");
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xff, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
        }
    }
}
