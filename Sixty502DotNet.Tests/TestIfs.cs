using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestIfs : TestBase
    {
        [TestMethod]
        public void If()
        {
            var parse = ParseSource
(@" .let i = 1
   .if 1 < 2
     .let i = 2
   .endif", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(2, i.Value.ToInt());
        }

        [TestMethod]
        public void IfElse()
        {
            var parse = ParseSource(
@" .let i = 1
    .if 1 > 2
        .let i = 2
    .else
        .let i = 3
    .endif", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(3, i.Value.ToInt());
        }

        [TestMethod]
        public void IfElseIf()
        {
            var parse = ParseSource(
@" .let i = 1
    .if 1 > 2
        .let i = 2
    .elseif 1 == 2
        .let i = 3
    .endif", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(1, i.Value.ToInt());
        }

        [TestMethod]
        public void IfComplex()
        {
            var parse = ParseSource(
@" .let i = 1
    .if 1 > 2
        .let i = 2
    .elseif 1 == 2
        .let i = 3
    .elseif 1 > -1
        .let i = 7
    .else
        .let i = 0
    .endif", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(7, i.Value.ToInt());
        }

        [TestMethod]
        public void IfConst()
        {
            var parse = ParseSource(
@"  .let i = false
    .ifconst MATH_PI
        .let i = true
    .endif", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsPrimitiveType);
            Assert.IsTrue(i.Value.ToBool());
        }
    }
}
