using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestForwardDeclarations : TestBase
    {
        [TestMethod]
        public void FunctionArg()
        {
            var parse = ParseSource(
@"          .string format(""{0}"", FORWARDREF)
FORWARDREF = $ffd2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(Services.State.PassNeeded);

        }
    }
}
