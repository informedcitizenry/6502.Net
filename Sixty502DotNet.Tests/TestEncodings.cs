using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestEncodings : TestBase
    {
        [TestMethod]
        public void None()
        {
            var parse = ParseSource(" .string \"HELLO, WORLD\"", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual("HELLO, WORLD".Length, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual('H', Convert.ToChar(bytes[0]));
        }

        [TestMethod]
        public void Cbm()
        {
            var parse = ParseSource(
@"      .encoding ""cbmscreen""
        .string ""HELLO, WORLD""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual("HELLO, WORLD".Length, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(8, bytes[0]);

        }

        [TestMethod]
        public void Petscii()
        {
            var parse = ParseSource(
@"      .encoding ""petscii""
        .string ""hello, world""", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual("hello, world".Length, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual('H', Convert.ToChar(bytes[0]));
        }

        [TestMethod]
        public void Custom()
        {
            var parse = ParseSource(
@"          .encoding ""custom""
            .map "" "",""@""
            .char ' '", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(1, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual('@', Convert.ToChar(bytes[0]));
        }
    }
}
