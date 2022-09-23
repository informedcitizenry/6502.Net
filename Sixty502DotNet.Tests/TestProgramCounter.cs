using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;
namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestProgramCounter : TestBase
    {
        [TestMethod]
        public void Implied()
        {
            var parse = ParseSource("*", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Assign()
        {
            var parse = ParseSource("*=$1000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x1000, Services.Output.ProgramCounter);

            parse = ParseSource(" * = $2000\n", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x2000, Services.Output.ProgramCounter);

            parse = ParseSource("* .equ$3000", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x3000, Services.Output.ProgramCounter);

            parse = ParseSource("* .equ $4000", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x4000, Services.Output.ProgramCounter);

            parse = ParseSource("* := $5000", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x5000, Services.Output.ProgramCounter);

            parse = ParseSource(" .let * = $6000", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0x6000, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void Relocate()
        {
            var parse = ParseSource(
@" * = $0801
    .relocate $c000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.LogicalPC);
            Assert.AreEqual(0x0801, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void Reference()
        {
            var parse = ParseSource(
@" * = $c000
myconst = *", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramCounter);
            var myconst = Services.Symbols.GlobalScope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsNotNull(myconst.Value);
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(0xc000, myconst.Value.ToInt());
        }
    }
}
