using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestSwitch : TestBase
    {
        [TestMethod]
        public void Default()
        {
            var parse = ParseSource(
@" .let k = 0
    .switch k
        .case 1
            .let k = 1
            .break
        .case 2
            .let k = 2
            .break
        .default
            .let k = 3
            .break
    .endswitch", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var k = Services.Symbols.GlobalScope.Resolve("k") as Variable;
            Assert.IsNotNull(k);
            Assert.IsNotNull(k.Value);
            Assert.IsTrue(k.Value.IsIntegral);
            Assert.AreEqual(3, k.Value.ToInt());
        }

        [TestMethod]
        public void MultipleCases()
        {
            var parse = ParseSource(
@" .let k = 0
    .switch k
        .case 0
        .case 1
            .let k = 1
            .break
        .case 2
            .let k = 2
            .break
        .case 3
        .default
            .let k = 3
            .break
    .endswitch", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var k = Services.Symbols.GlobalScope.Resolve("k") as Variable;
            Assert.IsNotNull(k);
            Assert.IsNotNull(k.Value);
            Assert.IsTrue(k.Value.IsIntegral);
            Assert.AreEqual(1, k.Value.ToInt());
        }
    }
}
