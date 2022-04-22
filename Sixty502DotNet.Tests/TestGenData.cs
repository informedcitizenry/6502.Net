using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;
using System;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestGenData : TestBase
    {
        [TestMethod]
        public void Align()
        {
            var parse = ParseSource(".align 256, $ff", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            Services.Output.SetPC(253);
            
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(253, Services.Output.ProgramStart);
            Assert.AreEqual(256, Services.Output.ProgramEnd);
            Assert.AreEqual(256, Services.Output.LogicalPC);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xFF, bytes[0]);
            Assert.AreEqual(0xFF, bytes[1]);
            Assert.AreEqual(0xFF, bytes[2]);
        }

        [TestMethod]
        public void Bytes()
        {
            var parse = ParseSource(".byte $01,$02,$03", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramEnd);
            Assert.AreEqual(3, Services.Output.LogicalPC);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(1, bytes[0]);
            Assert.AreEqual(2, bytes[1]);
            Assert.AreEqual(3, bytes[2]);
        }

        [TestMethod]
        public void Bankbytes()
        {
            var parse = ParseSource(" .bankbytes 0x010000, 0x020000, 0x030000", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(1, bytes[0]);
            Assert.AreEqual(2, bytes[1]);
            Assert.AreEqual(3, bytes[2]);
        }

        [TestMethod]
        public void Hibytes()
        {
            var parse = ParseSource(" .hibytes 0x4201, 0x8855, 0x321", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x42, bytes[0]);
            Assert.AreEqual(0x88, bytes[1]);
            Assert.AreEqual(0x03, bytes[2]);
        }

        [TestMethod]
        public void Hiwords()
        {
            var parse = ParseSource(" .hiwords 0xffff4201, 0x88550000, 0x0000321", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(6, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xff, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0x55, bytes[2]);
            Assert.AreEqual(0x88, bytes[3]);
            Assert.AreEqual(0x00, bytes[4]);
            Assert.AreEqual(0x00, bytes[5]);
        }

        [TestMethod]
        public void Lobytes()
        {
            var parse = ParseSource(" .lobytes 0x8042, 0x100033, 0x20", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(3, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x42, bytes[0]);
            Assert.AreEqual(0x33, bytes[1]);
            Assert.AreEqual(0x20, bytes[2]);
        }

        [TestMethod]
        public void Lowords()
        {
            var parse = ParseSource(" .lowords 0xffffd2ff, 0x88550000, 0x0000321", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(6, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xff, bytes[0]);
            Assert.AreEqual(0xd2, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);
            Assert.AreEqual(0x00, bytes[3]);
            Assert.AreEqual(0x21, bytes[4]);
            Assert.AreEqual(0x03, bytes[5]);
        }


        [TestMethod]
        public void Bstring()
        {
            var parse = ParseSource(".bstring \"11001001\"", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(1, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0b11001001, bytes[0]);
        }

        [TestMethod]
        public void Cbmfloats()
        {
            var parse = ParseSource(".cbmflt 3.141592653", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(6, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x82, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0x49, bytes[2]);
            Assert.AreEqual(0x0f, bytes[3]);
            Assert.AreEqual(0xda, bytes[4]);
            Assert.AreEqual(0xa1, bytes[5]);

            parse = ParseSource(".cbmfltp 3.141592653", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(5, Services.Output.ProgramEnd);
            bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x82, bytes[0]);
            Assert.AreEqual(0x49, bytes[1]);
            Assert.AreEqual(0x0f, bytes[2]);
            Assert.AreEqual(0xda, bytes[3]);
            Assert.AreEqual(0xa1, bytes[4]);
        }

        [TestMethod]
        public void Chars()
        {
            var parse = ParseSource(".char -1, -2, -3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(3, Services.Output.ProgramEnd);
            Assert.AreEqual(3, Services.Output.LogicalPC);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xFF, bytes[0]);
            Assert.AreEqual(0xFE, bytes[1]);
            Assert.AreEqual(0xFD, bytes[2]);
        }

        [TestMethod]
        public void Cstrings()
        {
            var parse = ParseSource(
@".cstring ""hello"",""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual("helloworld".Length + 1, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(Convert.ToByte('h'), bytes[0]);
            Assert.AreEqual(Convert.ToByte('e'), bytes[1]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[2]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[3]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[4]);
            Assert.AreEqual(Convert.ToByte('w'), bytes[5]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[6]);
            Assert.AreEqual(Convert.ToByte('r'), bytes[7]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[8]);
            Assert.AreEqual(Convert.ToByte('d'), bytes[9]);
            Assert.AreEqual(Convert.ToByte('\0'), bytes[10]);

        }

        [TestMethod]
        public void Dints()
        {
            var parse = ParseSource(".dint -1, -2, -3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(12, Services.Output.ProgramEnd);
            Assert.AreEqual(12, Services.Output.LogicalPC);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xFF, bytes[0]);
            Assert.AreEqual(0xFF, bytes[1]);
            Assert.AreEqual(0xFF, bytes[2]);
            Assert.AreEqual(0xFF, bytes[3]);
            Assert.AreEqual(0xFE, bytes[4]);
            Assert.AreEqual(0xFF, bytes[5]);
            Assert.AreEqual(0xFF, bytes[6]);
            Assert.AreEqual(0xFF, bytes[7]);
            Assert.AreEqual(0xFD, bytes[8]);
            Assert.AreEqual(0xFF, bytes[9]);
            Assert.AreEqual(0xFF, bytes[10]);
            Assert.AreEqual(0xFF, bytes[11]);
        }

        [TestMethod]
        public void Dwords()
        {
            var parse = ParseSource(".dword $01,$02,$03", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(12, Services.Output.ProgramEnd);
            Assert.AreEqual(12, Services.Output.LogicalPC);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(1, bytes[0]);
            Assert.AreEqual(0, bytes[1]);
            Assert.AreEqual(0, bytes[2]);
            Assert.AreEqual(0, bytes[3]);
            Assert.AreEqual(2, bytes[4]);
            Assert.AreEqual(0, bytes[5]);
            Assert.AreEqual(0, bytes[6]);
            Assert.AreEqual(0, bytes[7]);
            Assert.AreEqual(3, bytes[8]);
            Assert.AreEqual(0, bytes[9]);
            Assert.AreEqual(0, bytes[10]);
            Assert.AreEqual(0, bytes[11]);
        }

        [TestMethod]
        public void Fill()
        {
            var parse = ParseSource(".fill 6,$ffd2", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(6, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xd2, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
            Assert.AreEqual(0xd2, bytes[2]);
            Assert.AreEqual(0xff, bytes[3]);
            Assert.AreEqual(0xd2, bytes[4]);
            Assert.AreEqual(0xff, bytes[5]);
        }

        [TestMethod]
        public void Hstrings()
        {
            var parse = ParseSource(".hstring \"a94120d2ff60\"", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(6, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xa9, bytes[0]);
            Assert.AreEqual(0x41, bytes[1]);
            Assert.AreEqual(0x20, bytes[2]);
            Assert.AreEqual(0xd2, bytes[3]);
            Assert.AreEqual(0xff, bytes[4]);
            Assert.AreEqual(0x60, bytes[5]);
        }

        [TestMethod]
        public void Lint()
        {
            var parse = ParseSource(".lint -1, -2, -3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(9, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xFF, bytes[0]);
            Assert.AreEqual(0xFF, bytes[1]);
            Assert.AreEqual(0xFF, bytes[2]);
            Assert.AreEqual(0xFE, bytes[3]);
            Assert.AreEqual(0xFF, bytes[4]);
            Assert.AreEqual(0xFF, bytes[5]);
            Assert.AreEqual(0xFD, bytes[6]);
            Assert.AreEqual(0xFF, bytes[7]);
            Assert.AreEqual(0xFF, bytes[8]);
        }

        [TestMethod]
        public void Long()
        {
            var parse = ParseSource(".long 1, 2, 3", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(9, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(1, bytes[0]);
            Assert.AreEqual(0, bytes[1]);
            Assert.AreEqual(0, bytes[2]);
            Assert.AreEqual(2, bytes[3]);
            Assert.AreEqual(0, bytes[4]);
            Assert.AreEqual(0, bytes[5]);
            Assert.AreEqual(3, bytes[6]);
            Assert.AreEqual(0, bytes[7]);
            Assert.AreEqual(0, bytes[8]);
        }

        [TestMethod]
        public void Lstrings()
        {
            var parse = ParseSource(
@".lstring ""hello"", ""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual("helloworld".Length, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(Convert.ToByte('h' << 1), bytes[0]);
            Assert.AreEqual(Convert.ToByte('e' << 1), bytes[1]);
            Assert.AreEqual(Convert.ToByte('l' << 1), bytes[2]);
            Assert.AreEqual(Convert.ToByte('l' << 1), bytes[3]);
            Assert.AreEqual(Convert.ToByte('o' << 1), bytes[4]);
            Assert.AreEqual(Convert.ToByte('w' << 1), bytes[5]);
            Assert.AreEqual(Convert.ToByte('o' << 1), bytes[6]);
            Assert.AreEqual(Convert.ToByte('r' << 1), bytes[7]);
            Assert.AreEqual(Convert.ToByte('l' << 1), bytes[8]);
            Assert.AreEqual(Convert.ToByte(('d' << 1) | 1), bytes[9]);

        }

        [TestMethod]
        public void Nstrings()
        {
            var parse = ParseSource(
@".nstring ""hello"",""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual("helloworld".Length, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(Convert.ToByte('h'), bytes[0]);
            Assert.AreEqual(Convert.ToByte('e'), bytes[1]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[2]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[3]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[4]);
            Assert.AreEqual(Convert.ToByte('w'), bytes[5]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[6]);
            Assert.AreEqual(Convert.ToByte('r'), bytes[7]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[8]);
            Assert.AreEqual(Convert.ToByte('d' | 0x80), bytes[9]);
        }

        [TestMethod]
        public void PeekAndPoke()
        {
            var parse = ParseSource(
@"*=$c000
 .byte 1,2,3
byte1 := peek($c001)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramStart);
            Assert.AreEqual(0xc003, Services.Output.ProgramEnd);
            var byte1 = Services.Symbols.GlobalScope.Resolve("byte1") as Variable;
            Assert.IsNotNull(byte1);
            Assert.IsNotNull(byte1.Value);
            Assert.IsTrue(byte1.Value.IsDefined);
            Assert.IsTrue(byte1.Value.IsIntegral);
            Assert.AreEqual(2, byte1.Value.ToInt());

            parse = ParseSource(
@"*=$c000
 .byte 1,2,3
 .invoke poke($c001,42)
byte1 := peek($c001)", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc000, Services.Output.ProgramStart);
            Assert.AreEqual(0xc003, Services.Output.ProgramEnd);
            byte1 = Services.Symbols.GlobalScope.Resolve("byte1") as Variable;
            Assert.IsNotNull(byte1);
            Assert.IsNotNull(byte1.Value);
            Assert.IsTrue(byte1.Value.IsDefined);
            Assert.IsTrue(byte1.Value.IsIntegral);
            Assert.AreEqual(42, byte1.Value.ToInt());
        }

        [TestMethod]
        public void Pstrings()
        {
            var parse = ParseSource(
@".pstring ""hello"",""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual("helloworld".Length + 1, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(10, bytes[0]);
            Assert.AreEqual(Convert.ToByte('h'), bytes[1]);
            Assert.AreEqual(Convert.ToByte('e'), bytes[2]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[3]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[4]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[5]);
            Assert.AreEqual(Convert.ToByte('w'), bytes[6]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[7]);
            Assert.AreEqual(Convert.ToByte('r'), bytes[8]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[9]);
            Assert.AreEqual(Convert.ToByte('d'), bytes[10]);
        }

        [TestMethod]
        public void Rta()
        {
            var parse = ParseSource(".rta $1000,$1002", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(4, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xFF, bytes[0]);
            Assert.AreEqual(0x0F, bytes[1]);
            Assert.AreEqual(0x01, bytes[2]);
            Assert.AreEqual(0x10, bytes[3]);
        }

        [TestMethod]
        public void Strings()
        {
            var parse = ParseSource(
@".string ""hello"",""world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual("helloworld".Length, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(Convert.ToByte('h'), bytes[0]);
            Assert.AreEqual(Convert.ToByte('e'), bytes[1]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[2]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[3]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[4]);
            Assert.AreEqual(Convert.ToByte('w'), bytes[5]);
            Assert.AreEqual(Convert.ToByte('o'), bytes[6]);
            Assert.AreEqual(Convert.ToByte('r'), bytes[7]);
            Assert.AreEqual(Convert.ToByte('l'), bytes[8]);
            Assert.AreEqual(Convert.ToByte('d'), bytes[9]);
        }

        [TestMethod]
        public void Words()
        {
            var parse = ParseSource(".word $0200, $0300, $0400", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(6, Services.Output.ProgramEnd);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0, bytes[0]);
            Assert.AreEqual(2, bytes[1]);
            Assert.AreEqual(0, bytes[2]);
            Assert.AreEqual(3, bytes[3]);
            Assert.AreEqual(0, bytes[4]);
            Assert.AreEqual(4, bytes[5]);
        }

        [TestMethod]
        public void Uninitialized()
        {
            var parse = ParseSource(".byte 1, ?, ?", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(1, Services.Output.ProgramEnd);
            Assert.AreEqual(3, Services.Output.ProgramCounter);

            parse = ParseSource(".word ?, ?", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0, Services.Output.ProgramStart);
            Assert.AreEqual(0, Services.Output.ProgramEnd);
            Assert.AreEqual(4, Services.Output.ProgramCounter);

            parse = ParseSource(".dword ?, $deadface", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(4, Services.Output.ProgramStart);
            Assert.AreEqual(8, Services.Output.ProgramEnd);
            Assert.AreEqual(8, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0xde, bytes[3]);
            Assert.AreEqual(0xad, bytes[2]);
            Assert.AreEqual(0xfa, bytes[1]);
            Assert.AreEqual(0xce, bytes[0]);
        }
    }
}
