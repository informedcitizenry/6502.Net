using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestFullExample
    {
        [TestMethod]
        public void TenPrint()
        {
            var testDir = TestInclude.GetExamplesDir();
            var charStream = CharStreams.fromPath($"{testDir}10print.a65");
            var services = new AssemblyServices(Options.FromArgs(new string[] { $"{testDir}10print.a65" }));
            var preprocessor = new Preprocessor(services, charStream);
            var stream = new CommonTokenStream(preprocessor.Lexer);
            var parser = new Sixty502DotNetParser(stream)
            {
                Symbols = new SymbolManager(services.Options.CaseSensitive)
            };
            parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(services.Log);
            var codeGenVisitor = new CodeGenVisitor(services, preprocessor.Lexer.InstructionSet);
            var parse = parser.source();
            Assert.IsFalse(services.Log.HasErrors);
            _ = codeGenVisitor.Visit(parse);
            Assert.IsFalse(services.Log.HasErrors);
            Assert.IsFalse(services.State.PassNeeded);
            Assert.AreEqual(0xc01e, services.Output.ProgramCounter);
        }
    }
}
