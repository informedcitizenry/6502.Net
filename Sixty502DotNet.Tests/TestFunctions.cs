using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestFunctions : TestBase
    {
        [TestMethod]
        public void TestDeclaration()
        {
            var parse = ParseSource(
@"simple .function
        .return 1
        .endfunction", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            var simple = Services.Symbols.GlobalScope.Resolve("simple") as FunctionDefinitionBase;
            Assert.IsNotNull(simple);
            Assert.AreEqual(0, simple.Args.Count);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void TestCall()
        {
            var parse = ParseSource(
@"simple .function
        .return 1
        .endfunction
myvar := simple()", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            var simple = Services.Symbols.GlobalScope.Resolve("simple") as FunctionDefinitionBase;
            Assert.IsNotNull(simple);
            Assert.AreEqual(0, simple.Args.Count);

            var callExpr = tree.block().stat()[1].labelStat().assignExpr();
            var blockResult = Visitor.Visit(callExpr);
            Assert.IsNotNull(blockResult);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myvar = Services.Symbols.GlobalScope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsNotNull(myvar.Value);
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(1, myvar.Value.ToInt());
        }

        [TestMethod]
        public void TestScope()
        {
            var parse = ParseSource(
@"      .let localvar := 4
simple .function
        .let localvar := 2
        .return 1
        .endfunction
myvar := simple()", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var localvar = Services.Symbols.GlobalScope.Resolve("localvar") as Variable;
            Assert.IsNotNull(localvar);
            Assert.IsNotNull(localvar.Value);
            Assert.IsTrue(localvar.Value.IsDefined);
            Assert.IsTrue(localvar.Value.IsIntegral);
            Assert.AreEqual(2, localvar.Value.ToInt());

            parse = ParseSource(
@"myconst = 3
simple .function
myconst = 2
        .return myconst
        .endfunction
myresult := simple()", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myconst = Services.Symbols.GlobalScope.Resolve("myconst") as Constant;
            Assert.IsNotNull(myconst);
            Assert.IsNotNull(myconst.Value);
            Assert.IsTrue(myconst.Value.IsDefined);
            Assert.IsTrue(myconst.Value.IsIntegral);
            Assert.AreEqual(3, myconst.Value.ToInt());
            var myresult = Services.Symbols.GlobalScope.Resolve("myresult") as Variable;
            Assert.IsNotNull(myresult);
            Assert.IsNotNull(myresult.Value);
            Assert.IsTrue(myresult.Value.IsDefined);
            Assert.IsTrue(myresult.Value.IsIntegral);
            Assert.AreEqual(2, myresult.Value.ToInt());
        }

        [TestMethod]
        public void TestWithArgs()
        {
            var parse = ParseSource(
@"doubleit .function num
                .return num*2
            .endfunction
myresult := doubleit(2)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var num = Services.Symbols.GlobalScope.Resolve("num");
            Assert.IsNull(num);

            var myresult = Services.Symbols.GlobalScope.Resolve("myresult") as Variable;
            Assert.IsNotNull(myresult);
            Assert.IsNotNull(myresult.Value);
            Assert.IsTrue(myresult.Value.IsDefined);
            Assert.IsTrue(myresult.Value.IsIntegral);
            Assert.AreEqual(4, myresult.Value.ToInt());

            parse = ParseSource(
@"num = 3
doubleit .function num
                .return num*2
            .endfunction
myresult := doubleit(2)", true);

            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var numConst = Services.Symbols.GlobalScope.Resolve("num") as Constant;

            Assert.IsNotNull(numConst);
            Assert.IsNotNull(numConst.Value);
            Assert.IsTrue(numConst.Value.IsDefined);
            Assert.IsTrue(numConst.Value.IsIntegral);
            Assert.AreEqual(3, numConst.Value.ToInt());

            myresult = Services.Symbols.GlobalScope.Resolve("myresult") as Variable;
            Assert.IsNotNull(myresult);
            Assert.IsNotNull(myresult.Value);
            Assert.IsTrue(myresult.Value.IsDefined);
            Assert.IsTrue(myresult.Value.IsIntegral);
            Assert.AreEqual(4, myresult.Value.ToInt());
        }

        [TestMethod]
        public void Recursive()
        {
            var parse = ParseSource(
@"fibonacci    .function num
                .if num == 0
                    .return 0
                .elseif num == 1
                    .return 1
                .else
                    .return fibonacci(num - 1) + fibonacci(num - 2)
                .endif
            .endfunction
myresult := fibonacci(7)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var num = Services.Symbols.GlobalScope.Resolve("num");
            Assert.IsNull(num);

            var myresult = Services.Symbols.GlobalScope.Resolve("myresult") as Variable;
            Assert.IsNotNull(myresult);
            Assert.IsNotNull(myresult.Value);
            Assert.IsTrue(myresult.Value.IsDefined);
            Assert.IsTrue(myresult.Value.IsIntegral);
            Assert.AreEqual(13, myresult.Value.ToInt());
        }

        [TestMethod]
        public void LocalVariables()
        {
            var parse = ParseSource(
@"myfunction .function myparam
        .for i = 0, i < myparam, i++
            .echo i
        .next
        .return myparam*2
    .endfunction
    * = $c000
    .dword myfunction(2)", true);
            var tree = parse.source();
            var treeString = tree.ToStringTree(parse);
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.AreEqual(0xc004, Services.Output.ProgramCounter);
            var bytes = Services.Output.GetCompilation();
            Assert.AreEqual(0x04, bytes[0]);
            Assert.AreEqual(0x00, bytes[1]);
            Assert.AreEqual(0x00, bytes[2]);
            Assert.AreEqual(0x00, bytes[3]);
        }

        [TestMethod]
        public void ScopeBlock()
        {
            var parse = ParseSource(
@"myfunc .function
    .namespace mynamespace
myvar := 3
    .endnamespace
    .return mynamespace.myvar *3
    .endfunction
result := myfunc()", true);
            var tree = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource(
@"myfunc .function
myblock .block
myvar := 3
    .endblock
    .return myblock.myvar *3
    .endfunction
result := myfunc()", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void IllegalReturn()
        {
            var parse = ParseSource(" .return 42", true);
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Invoke()
        {
            var parse = ParseSource(
@" .let myvar = 3
myfunc .function
    myvar *= 2
    .endfunction
    .invoke myfunc()", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            var myvar = Services.Symbols.GlobalScope.Resolve("myvar") as Variable;
            Assert.IsNotNull(myvar);
            Assert.IsNotNull(myvar.Value);
            Assert.IsTrue(myvar.Value.IsDefined);
            Assert.IsTrue(myvar.Value.IsIntegral);
            Assert.AreEqual(6, myvar.Value.ToInt());
        }
    }
}
