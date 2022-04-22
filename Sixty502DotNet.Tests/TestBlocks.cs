using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestBlocks : TestBase
    {
        [TestMethod]
        public void Block()
        {

            string source =
@"myblock .block
  myconstant = 1
        .endblock";
            var parser = ParseSource(source, Services.Symbols);
            _ = parser.source();
            Assert.IsFalse(Services.Log.HasErrors);
            ReferenceEquals(Services.Symbols.GlobalScope, Services.Symbols.Scope);
            var myblock = Services.Symbols.Scope.Resolve("myblock") as Label;
            Assert.IsNotNull(myblock);
            var myconstant = myblock.ResolveMember("myconstant") as Constant;
            Assert.IsNotNull(myconstant);
        }

        [TestMethod]
        public void BlockResolve()
        {

            var source =
@"myblock .block
  myconstant = 1
        .endblock
someconst = myblock.myconstant + 2";
            var parser = ParseSource(source, true);
            var tree = parser.source();
            Assert.IsFalse(Services.Log.HasErrors); // no parse errors
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors); // no visit errors
            var someconst = Services.Symbols.Scope.Resolve("someconst") as Constant;
            Assert.IsNotNull(someconst);
            Assert.AreSame(Services.Symbols.GlobalScope, someconst.Scope);
            Assert.IsNotNull(someconst.Value);
            Assert.IsTrue(someconst.Value.IsDefined);
            Assert.IsTrue(someconst.Value.IsIntegral);
            Assert.AreEqual(3, someconst.Value.ToInt());
        }

        [TestMethod]
        public void BlockAddress()
        {
            var parse = ParseSource(
@"* = $c000
nop
nop
myblock .block
nop
nop
 .endblock
someconst = myblock", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc004, Services.Output.ProgramCounter);
            var someconst = Services.Symbols.Scope.Resolve("someconst") as Constant;
            Assert.IsNotNull(someconst);
            Assert.IsNotNull(someconst.Value);
            Assert.IsTrue(someconst.Value.IsDefined);
            Assert.IsTrue(someconst.Value.IsIntegral);
            Assert.AreEqual(0xc002, someconst.Value.ToInt());
        }

        [TestMethod]
        public void Illegal()
        {
            var parse = ParseSource(
@".block
myconst = 1
.endblock
.myconst", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);

            // attempts to resolve labels in anonymous blocks are seen as directives in this context
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.directiveStat()?.asmDirective()?.BadMacro());

            parse = ParseSource(
@".block
myconst = 1
.endblock
 lda .myconst", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void LabelAtEndblock()
        {
            var parse = ParseSource(
@"myblock .block
    nop // comment
blocklabel .endblock", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var myblock = Services.Symbols.GlobalScope.Resolve("myblock") as Label;
            Assert.IsNotNull(myblock);
            Assert.IsTrue(myblock.IsBlockScope);
            var blocklabel = myblock.Resolve("blocklabel") as Label;
            Assert.IsNotNull(blocklabel);
            Assert.IsNotNull(blocklabel.Value);
            Assert.IsTrue(blocklabel.Value.IsDefined);
            Assert.IsTrue(blocklabel.Value.IsIntegral);
            Assert.AreEqual(1, blocklabel.Value.ToInt());
        }
    }
}
