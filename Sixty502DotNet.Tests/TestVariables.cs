using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestVariables : TestBase
    {
        [TestMethod]
        public void AssignNonLet()
        {
            var parse = ParseSource("myvar := 3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            var myvarsym = Services.Symbols.GlobalScope.Resolve("myvar");
            Assert.IsNull(myvarsym);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var myvar = Services.Symbols.GlobalScope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsNotNull(myvar.Value);
            Assert.IsInstanceOfType(myvar, typeof(Variable));
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(3, myvar.Value.ToInt());
        }

        [TestMethod]
        public void AssignLet()
        {
            var parse = ParseSource(" .let myvar = 3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            var myvarsym = Services.Symbols.GlobalScope.Resolve("myvar");
            Assert.IsNull(myvarsym);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var myvar = Services.Symbols.GlobalScope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsNotNull(myvar.Value);
            Assert.IsInstanceOfType(myvar, typeof(Variable));
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(3, myvar.Value.ToInt());
        }

        [TestMethod]
        public void CheapLocal()
        {
            var parse = ParseSource(
@"myconst = 1
_myvar := 3
otherconst = 2
_myvar := 6", true);
            _ = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Illegal()
        {
            var parse = ParseSource(
@"myconst = 3
 .let myconst = 2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

            var myconst = Services.Symbols.GlobalScope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsNotNull(myconst.Value);
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(3, myconst.Value.ToInt());
        }
    }
}
