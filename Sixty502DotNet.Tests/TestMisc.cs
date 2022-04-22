using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestMisc : TestBase
    {
        [TestMethod]
        public void SimpleGoto()
        {
            var parse = ParseSource(
@"          .goto skip3
            nop
            nop
            nop
skip3       brk", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void GotoFromScope()
        {
            var parse = ParseSource(
@"      .block
        nop
        .goto outside
        nop
        .endblock
outside brk", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void End()
        {
            var parse = ParseSource(
@"  
        lda #$00
        .end
        jsr $ffd2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void Initmem()
        {
            var parse = ParseSource(
@"      .initmem $ea // init to nops
        .byte $ea
        .word ?, ?, ?
        jsr $ffd2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(10, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xea, bytes[0]);
            Assert.AreEqual(0xea, bytes[1]);
            Assert.AreEqual(0xea, bytes[2]);
            Assert.AreEqual(0xea, bytes[3]);
            Assert.AreEqual(0xea, bytes[4]);
            Assert.AreEqual(0xea, bytes[5]);
            Assert.AreEqual(0xea, bytes[6]);
            Assert.AreEqual(0x20, bytes[7]);
            Assert.AreEqual(0xd2, bytes[8]);
            Assert.AreEqual(0xff, bytes[9]);
        }
    }
}
