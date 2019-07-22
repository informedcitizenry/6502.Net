using DotNetAsm;
using NUnit.Framework;

namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture()]
    public class NUnitTestCmdLine
    {
        [Test()]
        public void InputTest()
        {
            string[] args = { "test.asm" };
            AsmCommandLineOptions options = new AsmCommandLineOptions();
            options.ParseArgs(args);

            Assert.AreEqual(1, options.ArgsPassed);
            Assert.IsTrue(options.InputFiles.Count == 1);
            var inputFile = options.InputFiles[0];
            Assert.AreEqual("test.asm", inputFile);

            args = new string[]
            {
                "test.asm",
                "library.asm"
            };
            options = new AsmCommandLineOptions();
            options.ParseArgs(args);

            Assert.AreEqual(2, options.ArgsPassed);
            Assert.AreEqual(2, options.InputFiles.Count);
            Assert.AreEqual("test.asm", options.InputFiles[0]);
            Assert.AreEqual("library.asm", options.InputFiles[1]);
        }

        [Test]
        public void FlagOptionTest()
        {
            string[] args =
            {
                "-b",
                "--quiet"
            };
            AsmCommandLineOptions options = new AsmCommandLineOptions();
            options.ParseArgs(args);

            Assert.AreEqual(2, options.ArgsPassed);
            Assert.IsTrue(options.BigEndian);
            Assert.IsTrue(options.Quiet);
        }

        [Test]
        public void OneArgumentTest()
        {
            string[] args =
            {
                "test.a65",
                "-o",
                "test.bin"
            };
            AsmCommandLineOptions options = new AsmCommandLineOptions();
            options.ParseArgs(args);

            Assert.AreEqual(3, options.ArgsPassed);
            Assert.AreEqual(1, options.InputFiles.Count);
            Assert.AreEqual("test.a65", options.InputFiles[0]);
            Assert.AreEqual("test.bin", options.OutputFile);
        }

        [Test]
        public void MultipleArgumentTest()
        {
            string[] args =
            {
                "test1.a65",
                "test2.a65",
                "--define",
                "chrout=$ffd2",
                "chrin=$ffcf"
            };
            AsmCommandLineOptions options = new AsmCommandLineOptions();
            options.ParseArgs(args);

            Assert.AreEqual(5, options.ArgsPassed);
            Assert.AreEqual(2, options.InputFiles.Count);
            Assert.AreEqual("test1.a65", options.InputFiles[0]);
            Assert.AreEqual("test2.a65", options.InputFiles[1]);
            Assert.AreEqual(2, options.LabelDefines.Count);
            Assert.AreEqual("chrout=$ffd2", options.LabelDefines[0]);
            Assert.AreEqual("chrin=$ffcf", options.LabelDefines[1]);
        }

        [Test]
        public void MultipleOptionTest()
        {
            string[] args =
            {
                "library.a65",
                "game.a65",
                "-o",
                "game.prg",
                "-D",
                "DEBUG",
                "=",
                "true",
                "VERSION=1.0",
                "--cpu",
                "6502i",
                "--list=game_list.a65",
                "-a",
                "--werror",
                "-l",
                "symbols.a65"
            };
            AsmCommandLineOptions options = new AsmCommandLineOptions();
            options.ParseArgs(args);

            Assert.AreEqual(14, options.ArgsPassed);
            Assert.AreEqual(2, options.InputFiles.Count);
            Assert.AreEqual("library.a65", options.InputFiles[0]);
            Assert.AreEqual("game.a65", options.InputFiles[1]);
            Assert.AreEqual("game.prg", options.OutputFile);
            Assert.AreEqual(2, options.LabelDefines.Count);
            Assert.AreEqual("DEBUG=true", options.LabelDefines[0]);
            Assert.AreEqual("VERSION=1.0", options.LabelDefines[1]);
            Assert.AreEqual("6502i", options.CPU);
            Assert.AreEqual("game_list.a65", options.ListingFile);
            Assert.IsTrue(options.NoAssembly);
            Assert.IsTrue(options.WarningsAsErrors);
            Assert.AreEqual("symbols.a65", options.LabelFile);
        }
    }
}
