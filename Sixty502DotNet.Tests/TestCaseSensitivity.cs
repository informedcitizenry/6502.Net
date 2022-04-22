using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestCaseSensitivity : TestBase
    {
        [TestMethod]
        public void Mnemonics()
        {
            var parse = ParseSource("LDA #0", true, false);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat()?.immStat()?.mnemonic()?.LDA());

            parse = ParseSource("LDA #0", true, true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Directives()
        {
            var parse = ParseSource(" .ORG $C000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.directiveStat());
            Assert.IsNotNull(tree.block().stat()[0].asmStat().directiveStat().asmDirective()?.directive);
            Assert.AreEqual(Sixty502DotNetParser.Org, tree.block().stat()[0].asmStat().directiveStat().asmDirective().directive.Type);

            parse = ParseSource(" .ORG $C000", true, true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 0);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.directiveStat());
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.directiveStat().asmDirective()?.directive);
            Assert.AreEqual(Sixty502DotNetParser.BadMacro, tree.block().stat()[0].asmStat().directiveStat().asmDirective().directive.Type);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void SymbolsAndScopes()
        {
            var parse = ParseSource(
@"MyConst = 1
myvar := myconst", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myconst = Services.Symbols.Scope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(1, myconst.Value.ToInt());
            var myvar = Services.Symbols.Scope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(1, myvar.Value.ToInt());

            parse = ParseSource(
@"MyConst = 1
myvar := MyConst", true, true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            myconst = Services.Symbols.Scope.Resolve("myconst") as Constant;
            Assert.IsNull(myconst);
            myconst = Services.Symbols.Scope.Resolve("MyConst") as Constant;
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(1, myconst.Value.ToInt());
            myvar = Services.Symbols.Scope.Resolve("MYVAR") as Variable;
            Assert.IsNull(myvar);
            myvar = Services.Symbols.Scope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(1, myvar.Value.ToInt());

            parse = ParseSource(
@"MyConst = 1
myvar := myconst", true, true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
            myconst = Services.Symbols.Scope.Resolve("myconst") as Constant;
            Assert.IsNull(myconst);
            myconst = Services.Symbols.Scope.Resolve("MyConst") as Constant;
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(1, myconst.Value.ToInt());
            myvar = Services.Symbols.Scope.Resolve("myvar") as Variable;
            Assert.IsNull(myvar);
        }
    }
}
