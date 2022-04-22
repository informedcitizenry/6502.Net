using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestTwoPass : TestBase
    {
        [TestMethod]
        public void ForwardLabel()
        {
            var parse = ParseSource(
@"    * = $c000
    lda message,x
message .cstring ""hello""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(Services.State.PassNeeded);
            Assert.AreEqual(0xc009, Services.Output.ProgramCounter);
            var message = Services.Symbols.Scope.Resolve("message") as Label;
            Assert.IsNotNull(message);
            Assert.IsTrue(message.Value.IsDefined);
            Assert.IsTrue(message.Value.IsIntegral);
            Assert.AreEqual(0xc003, message.Value.ToInt());
            Services.State.CurrentPass++;
            Services.State.PassNeeded = false;
            Services.Output.Reset();
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsFalse(Services.State.PassNeeded);
            Assert.AreEqual(0xc009, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(9, bytes.Count);

            // lda message,x
            Assert.AreEqual(0x03, bytes[1]); // <message
            Assert.AreEqual(0xc0, bytes[2]); // >message
        }
    }
}

