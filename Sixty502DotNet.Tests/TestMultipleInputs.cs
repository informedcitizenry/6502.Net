using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestMultipleInputs
    {
        [TestMethod]
        public void TwoSimpleFiles()
        {
            var source1 = $"{TestInclude.GetExamplesDir()}simple2.a65";
            var source2 = $"{TestInclude.GetExamplesDir()}simple.a65";
            var services = new AssemblyServices(Options.FromArgs(new string[] { source1, source2 }));
            var preprocessor = new Preprocessor(services);
            var parser = new Sixty502DotNetParser(new CommonTokenStream(preprocessor.Lexer));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(services.Log);
            parser.Symbols = services.Symbols;
            var tree = parser.source();
            var stringTree = tree.ToStringTree(parser);
            Assert.IsFalse(services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsTrue(tree.block().stat().Length > 1);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat()?.immStat()?.mnemonic().LDA());
            Assert.IsNotNull(tree.block().stat()[1].asmStat()?.cpuStat().zpAbsStat()?.mnemonic().JSR());
        }
    }
}
