using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestInclude : TestBase
    {
        public static string GetExamplesDir()
        {
            var info = new DirectoryInfo(Directory.GetCurrentDirectory());
            var sources = info.Parent?.Parent?.Parent;
            if (sources != null)
            {
                foreach (var dir in sources.GetDirectories())
                {
                    if (dir.Name.Equals("TestFiles"))
                    {
                        return dir.FullName + Path.DirectorySeparatorChar;
                    }
                }
            }
            return string.Empty;
        }

        [TestMethod]
        public void Simple()
        {
            string testFilesDir = GetExamplesDir();
            var parse = ParseSource($" .include \"{testFilesDir}simple.a65\"\n", true);
            var tree = parse.source();
            var treestring = tree.ToStringTree(parse);
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(tree.block());
            Assert.IsNotNull(tree.block().stat());
            Assert.IsTrue(tree.block().stat().Length > 0);
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat()?.zpAbsStat());
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat().zpAbsStat().mnemonic()?.JSR());
            Assert.IsNotNull(tree.block().stat()[0].asmStat()?.cpuStat().zpAbsStat().expr());
        }

        [TestMethod]
        public void Lib()
        {
            string testFilesDir = GetExamplesDir();

            var parse = ParseSource(
@"  .proff // turn off listing
    .include " + $"\"{testFilesDir}lib.a65\"" + @" // include lib
    .pron // turn on listing
    .basicstub // basic stub", true);
            var tree = parse.source();
            var treestring = tree.ToStringTree(parse);
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(2061, Services.Output.ProgramCounter);
        }

        [TestMethod]
        public void BinaryFull()
        {
            string testFilesDir = GetExamplesDir();
            var parse = ParseSource(
@"      * = $c000
hello   .binary " + $"\"{testFilesDir}hello.prg\"" + @"
        jmp hello+2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc021, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x00, bytes[0]);
            Assert.AreEqual(0xc0, bytes[1]);
        }

        [TestMethod]
        public void BinaryOffset()
        {
            string testFilesDir = GetExamplesDir();
            var parse = ParseSource(@"
            * = $c000
hello       .binary " + $"\"{testFilesDir}hello.prg\", 2" + @"
            jmp hello", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc01f, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa2, bytes[0]); // ldx #0
            Assert.AreEqual(0x00, bytes[1]); // 
        }
    }
}
