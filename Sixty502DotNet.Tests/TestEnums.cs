using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestEnums : TestBase
    {
        [TestMethod]
        public void Auto()
        {
            var parse = ParseSource(
@"directions .enum
    left
    right
    up
    down
    .endenum
player1dir = directions.up
player2dir = directions.right", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var directions = Services.Symbols.GlobalScope.Resolve("directions");
            Assert.IsNotNull(directions);
            Assert.IsInstanceOfType(directions, typeof(Enum));

            var player1Dir = Services.Symbols.GlobalScope.Resolve("player1dir") as Constant;
            Assert.IsNotNull(player1Dir);
            Assert.IsNotNull(player1Dir.Value);
            Assert.IsTrue(player1Dir.Value.IsDefined);
            Assert.IsTrue(player1Dir.Value.IsIntegral);
            Assert.AreEqual(2, player1Dir.Value.ToInt());

            var player2Dir = Services.Symbols.GlobalScope.Resolve("player2dir") as Constant;
            Assert.IsNotNull(player2Dir);
            Assert.IsNotNull(player2Dir.Value);
            Assert.IsTrue(player2Dir.Value.IsDefined);
            Assert.IsTrue(player2Dir.Value.IsIntegral);
            Assert.AreEqual(1, player2Dir.Value.ToInt());
        }

        [TestMethod]
        public void FirstDefined()
        {
            var parse = ParseSource(
@"directions .enum
    left = 1
    right
    up
    down
    .endenum
player1dir = directions.up
player2dir = directions.right", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var directions = Services.Symbols.GlobalScope.Resolve("directions");
            Assert.IsNotNull(directions);
            Assert.IsInstanceOfType(directions, typeof(Enum));

            var player1Dir = Services.Symbols.GlobalScope.Resolve("player1dir") as Constant;
            Assert.IsNotNull(player1Dir);
            Assert.IsNotNull(player1Dir.Value);
            Assert.IsTrue(player1Dir.Value.IsDefined);
            Assert.IsTrue(player1Dir.Value.IsIntegral);
            Assert.AreEqual(3, player1Dir.Value.ToInt());

            var player2Dir = Services.Symbols.GlobalScope.Resolve("player2dir") as Constant;
            Assert.IsNotNull(player2Dir);
            Assert.IsNotNull(player2Dir.Value);
            Assert.IsTrue(player2Dir.Value.IsDefined);
            Assert.IsTrue(player2Dir.Value.IsIntegral);
            Assert.AreEqual(2, player2Dir.Value.ToInt());
        }

        [TestMethod]
        public void MiddleDefined()
        {
            var parse = ParseSource(
@"directions .enum
    left
    right = 0x200
    up
    down
    .endenum
player1dir = directions.up
player2dir = directions.right", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var directions = Services.Symbols.GlobalScope.Resolve("directions");
            Assert.IsNotNull(directions);
            Assert.IsInstanceOfType(directions, typeof(Enum));

            var player1Dir = Services.Symbols.GlobalScope.Resolve("player1dir") as Constant;
            Assert.IsNotNull(player1Dir);
            Assert.IsNotNull(player1Dir.Value);
            Assert.IsTrue(player1Dir.Value.IsDefined);
            Assert.IsTrue(player1Dir.Value.IsIntegral);
            Assert.AreEqual(0x201, player1Dir.Value.ToInt());

            var player2Dir = Services.Symbols.GlobalScope.Resolve("player2dir") as Constant;
            Assert.IsNotNull(player2Dir);
            Assert.IsNotNull(player2Dir.Value);
            Assert.IsTrue(player2Dir.Value.IsDefined);
            Assert.IsTrue(player2Dir.Value.IsIntegral);
            Assert.AreEqual(0x200, player2Dir.Value.ToInt());
        }

        [TestMethod]
        public void EachDefined()
        {
            var parse = ParseSource(
@"directions .enum
    left    = %0001
    right   = %0010
    up      = %0100
    down    = %1000
    .endenum
player1dir = directions.up
player2dir = directions.right", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var directions = Services.Symbols.GlobalScope.Resolve("directions");
            Assert.IsNotNull(directions);
            Assert.IsInstanceOfType(directions, typeof(Enum));

            var player1Dir = Services.Symbols.GlobalScope.Resolve("player1dir") as Constant;
            Assert.IsNotNull(player1Dir);
            Assert.IsNotNull(player1Dir.Value);
            Assert.IsTrue(player1Dir.Value.IsDefined);
            Assert.IsTrue(player1Dir.Value.IsIntegral);
            Assert.AreEqual(0b0100, player1Dir.Value.ToInt());

            var player2Dir = Services.Symbols.GlobalScope.Resolve("player2dir") as Constant;
            Assert.IsNotNull(player2Dir);
            Assert.IsNotNull(player2Dir.Value);
            Assert.IsTrue(player2Dir.Value.IsDefined);
            Assert.IsTrue(player2Dir.Value.IsIntegral);
            Assert.AreEqual(0b0010, player2Dir.Value.ToInt());
        }

        [TestMethod]
        public void IllegalDefineType()
        {
            var parse = ParseSource(
@"directions .enum
    left = ""left""
    right = ""right""
        .endenum
player1dir = directions.up
player2dir = directions.right", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void IllegalDefineValue()
        {
            var parse = ParseSource(
@"directions .enum
    left = 1
    right = 0
        .endenum
player1dir = directions.up
player2dir = directions.right", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
