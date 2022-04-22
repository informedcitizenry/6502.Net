using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestAnonymousLabels : TestBase
    {
        [TestMethod]
        public void Implied()
        {
            var parse = ParseSource("\n-\n", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var backref = Services.Symbols.Scope.ResolveAnonymousLabel("-", 2);
            Assert.IsNotNull(backref);
            Assert.IsTrue(backref.Value.IsDefined);
            Assert.IsTrue(backref.Value.IsIntegral);
            Assert.AreEqual(0xC000, backref.Value.ToInt());

            parse = ParseSource("myconst = +\n+\n", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myconst = Services.Symbols.Scope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsNotNull(myconst.Value);
            Assert.IsFalse(myconst.Value.IsDefined);
            var forwardref = Services.Symbols.Scope.ResolveAnonymousLabel("+", 0);
            Assert.IsNotNull(forwardref);
            Assert.IsTrue(forwardref.Value.IsDefined);
            Assert.IsTrue(forwardref.Value.IsIntegral);
            Assert.AreEqual(0xC000, forwardref.Value.ToInt());
        }

        [TestMethod]
        public void SingleBack()
        {
            var parse = ParseSource(
@"myconst=1
- nop
other = -", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var other = Services.Symbols.GlobalScope.Resolve("other") as Constant;
            Assert.IsNotNull(other);
            Assert.IsNotNull(other.Value);
            Assert.IsTrue(other.Value.IsDefined);
            Assert.IsTrue(other.Value.IsIntegral);
            Assert.AreEqual(0xC000, other.Value.ToInt());
        }

        [TestMethod]
        public void SingleForward()
        {
            var parse = ParseSource(
@"other = +
+ nop", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var other = Services.Symbols.GlobalScope.Resolve("other") as Constant;
            Assert.IsNotNull(other);
            Assert.IsNotNull(other.Value);
            Assert.IsFalse(other.Value.IsDefined);

            var anon = Services.Symbols.GlobalScope.ResolveAnonymousLabel("+", 0);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC000, anon.Value.ToInt());
        }

        [TestMethod]
        public void TwoBack()
        {
            var parse = ParseSource(
@"- nop
- nop
other = --", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var other = Services.Symbols.GlobalScope.Resolve("other") as Constant;
            Assert.IsNotNull(other);
            Assert.IsNotNull(other.Value);
            Assert.IsTrue(other.Value.IsDefined);
            Assert.IsTrue(other.Value.IsIntegral);
            Assert.AreEqual(0xC000, other.Value.ToInt());

            var anon = Services.Symbols.GlobalScope.ResolveAnonymousLabel("--", other.DefinedAt.Start.StartIndex);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC000, anon.Value.ToInt());
        }

        [TestMethod]
        public void TwoForward()
        {
            var parse = ParseSource(
@"other = ++
+ nop
+ nop", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var other = Services.Symbols.GlobalScope.Resolve("other") as Constant;
            Assert.IsNotNull(other);
            Assert.IsNotNull(other.Value);
            Assert.IsFalse(other.Value.IsDefined);

            var anon = Services.Symbols.GlobalScope.ResolveAnonymousLabel("+", 0);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC000, anon.Value.ToInt());

            anon = Services.Symbols.GlobalScope.ResolveAnonymousLabel("++", 0);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC001, anon.Value.ToInt());
        }

        [TestMethod]
        public void ScopedBack()
        {
            var parse = ParseSource(
@"- nop
someblock .block
some = -
- nop
other = --
 .endblock
other = -", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetLogicalPC(0xC000);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var someBlock = Services.Symbols.GlobalScope.Resolve("someblock") as Label;
            Assert.IsNotNull(someBlock);
            var other = someBlock.ResolveMember("other") as Constant;
            Assert.IsNotNull(other);
            Assert.IsNotNull(other.Value);
            Assert.IsTrue(other.Value.IsDefined);
            Assert.IsTrue(other.Value.IsIntegral);
            Assert.AreEqual(0xC000, other.Value.ToInt());

            var anon = someBlock.ResolveAnonymousLabel("-", other.DefinedAt.Start.StartIndex);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC001, anon.Value.ToInt());

            anon = someBlock.ResolveAnonymousLabel("--", other.DefinedAt.Start.StartIndex);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC000, anon.Value.ToInt());

            other = Services.Symbols.GlobalScope.Resolve("other") as Constant;
            Assert.IsNotNull(other);
            Assert.IsNotNull(other.Value);
            Assert.IsTrue(other.Value.IsDefined);
            Assert.IsTrue(other.Value.IsIntegral);
            Assert.AreEqual(0xC000, other.Value.ToInt());

            anon = Services.Symbols.GlobalScope.ResolveAnonymousLabel("-", other.DefinedAt.Start.StartIndex);
            Assert.IsNotNull(anon);
            Assert.IsNotNull(anon.Value);
            Assert.IsTrue(anon.Value.IsDefined);
            Assert.IsTrue(anon.Value.IsIntegral);
            Assert.AreEqual(0xC000, anon.Value.ToInt());
        }

        [TestMethod]
        public void MixedForwardAndBackward()
        {
            var parse = ParseSource(
@"        * = $c000
        ldx #0
-    lda ++,x
        beq +
        jsr $ffd2
        inx
        bne -
+    rts
+ .cstring ""hello""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            ((CodeGenVisitor)Visitor).Reset();
            Services.StatementListings.Clear();
            Services.LabelListing.Clear();
            Services.State.CurrentPass++;
            Services.State.PassNeeded = false;
            Services.Output.Reset();
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc014, Services.Output.ProgramCounter);
            var f1 = Services.Symbols.GlobalScope.ResolveAnonymousLabel("+", 0);
            var f2 = Services.Symbols.GlobalScope.ResolveAnonymousLabel("++", 0);
            Assert.IsNotNull(f1);
            Assert.IsNotNull(f2);
            Assert.IsTrue(f1.Value.IsDefined);
            Assert.IsTrue(f1.Value.IsIntegral);
            Assert.AreEqual(0xc00d, f1.Value.ToInt());

            Assert.IsTrue(f2.Value.IsIntegral);
            Assert.AreEqual(0xc00e, f2.Value.ToInt());

            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(20, bytes.Count);
        }

        [TestMethod]
        public void Illegal()
        {
            var parse = ParseSource("+ = 3", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource("+ .equ 3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
