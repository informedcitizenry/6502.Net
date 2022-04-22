using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestCpuDirective : TestBase
    {
        [TestMethod]
        public void M6502()
        {
            var parse = ParseSource(
@".cpu ""6502""
    brk", false);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            Assert.AreEqual(0, Services.Output.GetCompilation()[0]);
        }

        [TestMethod]
        public void M6502i()
        {
            var parse = ParseSource(
@".cpu ""6502i""
    isb $ffff,x", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat());
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat().ixStat()?.mnemonic()?.ISB());

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xff, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0xff, bytes[2]);
        }

        [TestMethod]
        public void M6809()
        {
            var parse = ParseSource(
@" .cpu ""m6809""
 leax ,x++", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat());
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat().autoIncIndexedStat());
        }

        [TestMethod]
        public void Z80()
        {
            var parse = ParseSource(
@" .cpu ""z80""
    ld a,(hl)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat());
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat().z80RegReg());
        }

        [TestMethod]
        public void Illegal()
        {
            var parse = ParseSource(" .cpu 6502\n nop", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource(" .cpu \"x86\"\n mov eax,ebx", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
