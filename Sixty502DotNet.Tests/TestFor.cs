using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestFor : TestBase
    {
        [TestMethod]
        public void SimpleFor()
        {
            var parse = ParseSource(
@" .let i = 3 // weakly scoped variable will be reassigned in the for loop
    .for i = 0, i < 2, i++
        nop
    .next", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            var fordecl = tree.block().stat()[1];
            var fordeclscope = fordecl.scope;
            Assert.IsNotNull(fordeclscope);
            Assert.IsInstanceOfType(fordeclscope, typeof(AnonymousScope));

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(2, i.Value.ToInt());
        }

        [TestMethod]
        public void ForBreak()
        {
            var parse = ParseSource(
@" .let k = 0
    .for i = 0, i < 4, i++
        .if i == 2
            .break
        .endif
        k++
    .next", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var k = Services.Symbols.GlobalScope.Resolve("k") as Variable;
            Assert.IsNotNull(k);
            Assert.IsNotNull(k.Value);
            Assert.IsTrue(k.Value.IsIntegral);
            Assert.AreEqual(2, k.Value.ToInt());
        }

        [TestMethod]
        public void ForContinue()
        {
            var parse = ParseSource(
@" .let k = 0
    .for i = 0, i < 4, i++
        .if i == 2
            .continue
        .endif
        k++
    .next", true);
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
        public void MultipleIterations()
        {
            var parse = ParseSource(
@" .let k = 0
    .for i = 0, i < 4, i++, k++
        nop
    .next", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var k = Services.Symbols.GlobalScope.Resolve("k") as Variable;
            Assert.IsNotNull(k);
            Assert.IsNotNull(k.Value);
            Assert.IsTrue(k.Value.IsIntegral);
            Assert.AreEqual(4, k.Value.ToInt());
        }
    }
}
