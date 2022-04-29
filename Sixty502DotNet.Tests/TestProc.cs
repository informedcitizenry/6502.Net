using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestProc : TestBase
    {
        [TestMethod]
        public void Used()
        {
            var parse = ParseSource(
@"myproc    .proc
            nop
            nop
            .endproc
        jmp myproc", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myprocLabel = Services.Symbols.Scope.Resolve("myproc") as Label;
            Assert.IsNotNull(myprocLabel);
            Assert.IsTrue(myprocLabel.IsBlockScope && myprocLabel.IsProcScope);
            Assert.IsTrue(myprocLabel.IsReferenced);
            Assert.IsTrue(Services.State.PassNeeded);
            Services.State.CurrentPass++;
            Services.Output.Reset();
            Services.State.PassNeeded = false;
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.State.PassNeeded);
            Assert.AreEqual(5, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void Unused()
        {
            var parse = ParseSource(
@"myproc    .proc
            nop
            nop
            .endproc
            rts", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myprocLabel = Services.Symbols.Scope.Resolve("myproc") as Label;
            Assert.IsNotNull(myprocLabel);
            Assert.IsTrue(myprocLabel.IsBlockScope && myprocLabel.IsProcScope);
            Assert.IsFalse(myprocLabel.IsReferenced);
            Assert.IsTrue(Services.State.PassNeeded);
            Services.State.CurrentPass++;
            Services.Output.Reset();
            Services.State.PassNeeded = false;
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.State.PassNeeded);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void ScopedSymbolUsed()
        {
            var parse = ParseSource(
@"myproc    .proc
            nop
mysubproc   nop
            .endproc
            jmp myproc.mysubproc", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var myprocLabel = Services.Symbols.Scope.Resolve("myproc") as Label;
            Assert.IsNotNull(myprocLabel);
            Assert.IsTrue(myprocLabel.IsBlockScope && myprocLabel.IsProcScope);
            Assert.IsTrue(myprocLabel.IsReferenced);

            var mysubprocLabel = myprocLabel.Resolve("mysubproc") as Label;
            Assert.IsNotNull(mysubprocLabel);
            Assert.IsTrue(mysubprocLabel.IsReferenced);

            Assert.IsTrue(Services.State.PassNeeded);
            Services.State.CurrentPass++;
            Services.Output.Reset();
            Services.State.PassNeeded = false;

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.State.PassNeeded);
            Assert.AreEqual(5, Services.Output.ProgramCounter);
        }
    }
}
