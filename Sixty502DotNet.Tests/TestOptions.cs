using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestOptions
    {
        [TestMethod]
        public void Simple()
        {
            var oneInput = new string[] { "test.a65" };
            var options = Options.FromArgs(oneInput);
            Assert.AreEqual(1, options.InputFiles.Count);
            Assert.AreEqual("test.a65", options.InputFiles[0]);
            Assert.AreEqual("a.out", options.OutputFile);
        }

        [TestMethod]
        public void InputFileOutputFile()
        {
            var inputOutput = new string[] { "test.a65", "-o", "test.prg" };
            var options = Options.FromArgs(inputOutput);
            Assert.AreEqual(1, options.InputFiles.Count);
            Assert.AreEqual("test.a65", options.InputFiles[0]);
            Assert.AreEqual("test.prg", options.OutputFile);
        }
    }
}
