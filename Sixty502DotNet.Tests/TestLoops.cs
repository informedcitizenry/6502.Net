using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestLoops : TestBase
    {
        [TestMethod]
        public void Do()
        {
            var parse = ParseSource(
@" .let i = 0
    .do
        i++
    .whiletrue i < 5", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(5, i.Value.ToInt());
        }

        [TestMethod]
        public void Foreach()
        {
            var parse = ParseSource(
@" .let i = 0
arr = [1,2,3,4,5]
    .foreach elem, arr
        i += elem
    .next", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(1 + 2 + 3 + 4 + 5, i.Value.ToInt());
        }

        [TestMethod]
        public void ForeachDictionary()
        {
            var parse = ParseSource(
@"
 .let keys = [ ""undefined"",""undefined"",""undefined"" ]
 .let values = [ ""undefined"",""undefined"",""undefined"" ]
dict = { ""key1"": ""value1"", ""key2"": ""value2"", ""key3"": ""value3"" }
    index := 0
    .foreach kvp, dict
        keys[index] = kvp.key
        values[index] = kvp.value
        index++
    .next
", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var keys = Services.Symbols.GlobalScope.Resolve("keys") as Variable;
            var values = Services.Symbols.GlobalScope.Resolve("values") as Variable;
            var index = Services.Symbols.GlobalScope.Resolve("index") as Variable;
            Assert.IsNotNull(keys);
            Assert.IsNotNull(values);
            Assert.IsNotNull(index);

            Assert.IsInstanceOfType(keys.Value, typeof(ArrayValue));
            var keysArr = keys.Value as ArrayValue;
            Assert.IsTrue(keysArr.ElementsDefined);
            Assert.AreEqual(3, keysArr.ElementCount);
            Assert.AreEqual("key1", keysArr[0].ToString(true));
            Assert.AreEqual("key2", keysArr[1].ToString(true));
            Assert.AreEqual("key3", keysArr[2].ToString(true));

            Assert.IsInstanceOfType(values.Value, typeof(ArrayValue));
            var valuesArr = values.Value as ArrayValue;
            Assert.IsTrue(valuesArr.ElementsDefined);
            Assert.AreEqual(3, valuesArr.ElementCount);
            Assert.AreEqual("value1", valuesArr[0].ToString(true));
            Assert.AreEqual("value2", valuesArr[1].ToString(true));
            Assert.AreEqual("value3", valuesArr[2].ToString(true));

            Assert.IsTrue(index.Value.IsDefined);
            Assert.IsTrue(index.Value.IsIntegral);
            Assert.AreEqual(3, index.Value.ToInt());
        }

        [TestMethod]
        public void Repeat()
        {
            var parse = ParseSource(
@" .let i = 0
    .repeat 5
        i++
    .endrepeat", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(5, i.Value.ToInt());
        }

        [TestMethod]
        public void While()
        {
            var parse = ParseSource(
@" .let i = 0
    .while i < 5
        i++
    .endwhile", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(5, i.Value.ToInt());

            parse = ParseSource(
@"alabel='A'
 i := 0
  .while i < 4
        i++
        .echo alabel
        .if i == 2
            .echo ""TRUE!""
            .break
        .endif
        .echo i
  .endwhile", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            i = Services.Symbols.GlobalScope.Resolve("i") as Variable;
            Assert.IsNotNull(i);
            Assert.IsNotNull(i.Value);
            Assert.IsTrue(i.Value.IsDefined);
            Assert.IsTrue(i.Value.IsIntegral);
            Assert.AreEqual(2, i.Value.ToInt());
        }

        [TestMethod]
        public void Illegal()
        {
            var parse = ParseSource(" .break", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
