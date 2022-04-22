using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;
namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestSymbolResolutions : TestBase
    {
        [TestMethod]
        public void Constant()
        {
            var parse = ParseSource("constant = 1", Services.Symbols);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var constant = Services.Symbols.Scope.Resolve("constant") as Constant;
            Assert.IsNotNull(constant);
            Assert.IsNotNull(constant.Value);
            Assert.IsTrue(constant.Value.IsDefined);
            Assert.IsTrue(constant.Value.IsIntegral);
            Assert.AreEqual(1, constant.Value.ToInt());
        }

        [TestMethod]
        public void LabelImplied()
        {
            var parse = ParseSource("label", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void LocalLabel()
        {
            var parse = ParseSource(
@"label1
_local = 1.5
label2
_local = 2.5
const1 = label1_local
const2 = label2_local", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var label1 = Services.Symbols.GlobalScope.Resolve("label1") as Label;
            Assert.IsNotNull(label1);


            var label1_local = label1.ResolveMember("_local") as Constant;
            Assert.IsNotNull(label1_local);
            Assert.AreSame(label1, label1_local.Scope);

            Assert.IsNotNull(label1_local.Value);
            Assert.IsTrue(label1_local.Value.IsDefined);
            Assert.IsTrue(label1_local.Value.IsNumeric);
            Assert.AreEqual(1.5, label1_local.Value.ToDouble());

            var label2 = Services.Symbols.GlobalScope.Resolve("label2") as Label;
            Assert.IsNotNull(label2);

            var label2_local = label2.ResolveMember("_local") as Constant;
            Assert.IsNotNull(label2_local);
            Assert.AreSame(label2, label2_local.Scope);
            Assert.IsTrue(label2_local.Value.IsDefined);
            Assert.IsTrue(label2_local.Value.IsNumeric);
            Assert.AreEqual(2.5, label2_local.Value.ToDouble());


            var const1 = Services.Symbols.GlobalScope.Resolve("const1") as Constant;
            Assert.IsNotNull(const1);
            Assert.IsNotNull(const1.Value);
            Assert.IsTrue(const1.Value.IsDefined);
            Assert.IsTrue(const1.Value.IsNumeric);
            Assert.AreEqual(1.5, const1.Value.ToDouble());

            var const2 = Services.Symbols.GlobalScope.Resolve("const2") as Constant;
            Assert.IsNotNull(const2);
            Assert.IsNotNull(const2.Value);
            Assert.IsTrue(const2.Value.IsDefined);
            Assert.IsTrue(const2.Value.IsNumeric);
            Assert.AreEqual(2.5, const2.Value.ToDouble());
        }

        [TestMethod]
        public void Block()
        {
            var parse = ParseSource(
@"myblock .block
_local=1
myconstant = myblock_local
    .endblock
myconstant = myblock.myconstant", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var global_myconstant = Services.Symbols.GlobalScope.Resolve("myconstant") as Constant;
            Assert.IsNotNull(global_myconstant);
            Assert.IsNotNull(global_myconstant.Value);
            Assert.IsTrue(global_myconstant.Value.IsDefined);
            Assert.IsTrue(global_myconstant.Value.IsIntegral);
            Assert.AreEqual(1, global_myconstant.Value.ToInt());

            var myblock = Services.Symbols.GlobalScope.Resolve("myblock") as Label;
            Assert.IsNotNull(myblock);
            Assert.IsTrue(myblock.IsBlockScope);
            Assert.IsNotNull(myblock.Value);
            Assert.IsTrue(myblock.Value.IsDefined);
            Assert.IsTrue(myblock.Value.IsIntegral);
            Assert.AreEqual(0, myblock.Value.ToInt());


            parse = ParseSource(
@"outerblock .block
innerblock .block
myconstant = 2
.endblock
myconstant = innerblock.myconstant
.endblock
myconstant = outerblock.myconstant
myconstant2 = outerblock.innerblock.myconstant", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            global_myconstant = Services.Symbols.GlobalScope.Resolve("myconstant") as Constant;
            Assert.IsNotNull(global_myconstant);
            Assert.IsNotNull(global_myconstant.Value);
            Assert.IsTrue(global_myconstant.Value.IsDefined);
            Assert.IsTrue(global_myconstant.Value.IsIntegral);
            Assert.AreEqual(2, global_myconstant.Value.ToInt());

            var global_myconstant2 = Services.Symbols.GlobalScope.Resolve("myconstant2") as Constant;
            Assert.IsNotNull(global_myconstant2);
            Assert.IsNotNull(global_myconstant2.Value);
            Assert.IsTrue(global_myconstant2.Value.IsDefined);
            Assert.IsTrue(global_myconstant2.Value.IsIntegral);
            Assert.AreEqual(2, global_myconstant2.Value.ToInt());
        }

        [TestMethod]
        public void AnonymousBlock()
        {
            var parse = ParseSource(
@" .block
myconstant = 1
    .endblock
    .block
myconstant = 3
    .endblock", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var anon_myconstant1 = Services.Symbols.GlobalScope.Resolve("myconstant");
            Assert.IsNull(anon_myconstant1);

            Assert.AreEqual(2, tree.block().stat().Length);
            var anonblock1 = tree.block().stat()[0].blockStat()?.block();
            Assert.IsNotNull(anonblock1);

            Assert.IsInstanceOfType(anonblock1.scope, typeof(AnonymousScope));
            anon_myconstant1 = anonblock1.scope.Resolve("myconstant") as Constant;
            Assert.IsNotNull(anon_myconstant1);
            Assert.IsNotNull(((Constant)anon_myconstant1).Value);
            Assert.IsTrue(((Constant)anon_myconstant1).Value.IsDefined);
            Assert.IsTrue(((Constant)anon_myconstant1).Value.IsIntegral);
            Assert.AreEqual(1, ((Constant)anon_myconstant1).Value.ToInt());

            var anonblock2 = tree.block().stat()[1].blockStat()?.block();
            Assert.IsNotNull(anonblock2);

            Assert.IsInstanceOfType(anonblock2.scope, typeof(AnonymousScope));
            var anon_myconstant2 = anonblock2.scope.Resolve("myconstant") as Constant;
            Assert.IsNotNull(anon_myconstant2);
            Assert.IsNotNull(anon_myconstant2.Value);
            Assert.IsTrue(anon_myconstant2.Value.IsDefined);
            Assert.IsTrue(anon_myconstant2.Value.IsIntegral);
            Assert.AreEqual(3, anon_myconstant2.Value.ToInt());
        }

        [TestMethod]
        public void Namespace()
        {
            var parse = ParseSource(
@" .namespace mynamespace
myconstant = 1
    .endnamespace
myconstant = mynamespace.myconstant
    .namespace mynamespace
myconstant2 = 2
    .endnamespace
myconstant2 = mynamespace.myconstant2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            var block = tree.block();
            var blockScope = block.scope;
            Assert.IsNotNull(blockScope);
            Assert.AreSame(Services.Symbols.GlobalScope, blockScope);

            Assert.AreEqual(4, block.stat().Length);

            // .namespace mynamespace
            var ns1Stat = block.stat()[0];
            var ns1StatScope = ns1Stat.scope;
            Assert.AreSame(Services.Symbols.GlobalScope, ns1StatScope);

            var ns1block = ns1Stat.blockStat()?.block();
            Assert.IsNotNull(ns1block);

            var ns1blockScope = ns1block.scope;
            Assert.AreEqual("mynamespace", ns1blockScope.Name);
            Assert.AreSame(ns1StatScope, ns1blockScope.EnclosingScope);

            var globalStat1 = block.stat()[1];
            var globalStat1Scope = globalStat1.scope;
            Assert.AreSame(Services.Symbols.GlobalScope, globalStat1Scope);

            // .namespace mynamespace
            var ns2Stat = block.stat()[2];
            var ns2StatScope = ns2Stat.scope;
            Assert.AreSame(Services.Symbols.GlobalScope, ns2StatScope);

            var ns2block = ns2Stat.blockStat()?.block();
            Assert.IsNotNull(ns2block);

            var ns2blockScope = ns2block.scope;
            Assert.AreSame(ns1blockScope, ns2blockScope);

            var ns2BlockStats = ns2block.stat();
            Assert.IsNotNull(ns2BlockStats);
            Assert.AreEqual(1, ns2BlockStats.Length);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var mynamespace = Services.Symbols.GlobalScope.Resolve("mynamespace") as Namespace;
            Assert.IsNotNull(mynamespace);
            Assert.AreEqual(2, mynamespace.Members.Count);

            var mynamespace_myconst = mynamespace.ResolveMember("myconstant") as Constant;
            Assert.IsNotNull(mynamespace_myconst);
            Assert.IsNotNull(mynamespace_myconst.Value);
            Assert.IsTrue(mynamespace_myconst.Value.IsDefined);
            Assert.IsTrue(mynamespace_myconst.Value.IsIntegral);
            Assert.AreEqual(1, mynamespace_myconst.Value.ToInt());

            var global_myconst = Services.Symbols.GlobalScope.Resolve("myconstant") as Constant;
            Assert.IsNotNull(global_myconst);
            Assert.IsNotNull(global_myconst.Value);
            Assert.IsTrue(global_myconst.Value.IsDefined);
            Assert.IsTrue(global_myconst.Value.IsIntegral);
            Assert.AreEqual(1, global_myconst.Value.ToInt());
            Assert.AreNotSame(mynamespace_myconst, global_myconst);

            var mynamespace_myconst2 = mynamespace.ResolveMember("myconstant2") as Constant;
            Assert.IsNotNull(mynamespace_myconst2);
            Assert.IsNotNull(mynamespace_myconst2.Value);
            Assert.IsTrue(mynamespace_myconst2.Value.IsDefined);
            Assert.IsTrue(mynamespace_myconst2.Value.IsIntegral);
            Assert.AreEqual(2, mynamespace_myconst2.Value.ToInt());

            var global_myconst2 = Services.Symbols.GlobalScope.Resolve("myconstant2") as Constant;
            Assert.IsNotNull(global_myconst2);
            Assert.IsNotNull(global_myconst2.Value);
            Assert.IsTrue(global_myconst2.Value.IsDefined);
            Assert.IsTrue(global_myconst.Value.IsIntegral);
            Assert.AreEqual(2, global_myconst2.Value.ToInt());
            Assert.AreNotSame(mynamespace_myconst2, global_myconst2);

        }

        [TestMethod]
        public void TestDotNamespace()
        {
            var parse = ParseSource(
@"  .namespace my.inner.namespace
myconstant = 3
    .endnamespace
myglobal = my.inner.namespace.myconstant", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myglobal = Services.Symbols.GlobalScope.Resolve("myglobal") as Constant;
            Assert.IsNotNull(myglobal);
            Assert.IsNotNull(myglobal.Value);
            Assert.IsTrue(myglobal.Value.IsDefined);
            Assert.IsTrue(myglobal.Value.IsIntegral);
            Assert.AreEqual(3, myglobal.Value.ToInt());
        }

        [TestMethod]
        public void Import()
        {
            var parse = ParseSource(
@"  .namespace myns
myconstant = 3
    .endnamespace
    .import myns
myglobal = myconstant", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myglobal = Services.Symbols.GlobalScope.Resolve("myglobal") as Constant;
            Assert.IsNotNull(myglobal);
            Assert.IsTrue(myglobal.Value.IsDefined);
            Assert.IsTrue(myglobal.Value.IsIntegral);
            Assert.AreEqual(3, myglobal.Value.ToInt());
        }

        [TestMethod]
        public void IllegalAssignments()
        {
            var parse = ParseSource(
@"  .let myvar = 1
myconst = myvar // cannot assign constant to var", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            // only process the first statement, it will not error
            _ = Visitor.Visit(tree.block().stat()[0]);
            Assert.IsFalse(Services.Log.HasErrors);
            var myvar = Services.Symbols.GlobalScope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsNotNull(myvar.Value);
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(1, myvar.Value.ToInt());

            // now redo but process both statements, it errors not because
            // myvar is not assigned but because it is a variable
            parse = ParseSource(
@"  .let myvar = 1
myconst = myvar // cannot assign label to var", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource(@"
directions .enum
up
down
left
right
    .endenum
myconst = directions
", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource(@"INT16_MAX = 23", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
