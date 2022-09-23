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
            _ = parse.source();
            Assert.IsTrue(Services.Log.HasErrors);

            parse = ParseSource(
@"myfunc .function
myblock .block
myvar := 3
    .endblock
    .return myblock.myvar *3
    .endfunction
result := myfunc()", true);
            var tree = parse.source();
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

        [TestMethod]
        public void Assign()
        {
            var parse = ParseSource(
@"myfunc .function val
    .return val * 2
    .endfunction
    .let myfuncVar = myfunc
    .let myval = myfuncVar(2)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);
            
            var myval = Services.Symbols.GlobalScope.Resolve("myval") as Variable;
            Assert.IsNotNull(myval);
            Assert.IsNotNull(myval.Value);
            Assert.IsTrue(myval.Value.IsDefined);
            Assert.IsTrue(myval.Value.IsNumeric);
            Assert.AreEqual(4, myval.Value.ToInt());

            parse = ParseSource(
@"  .let myfuncVar = myfunc
    myfunc .function val
    .return val * 2
    .endfunction
    
    .let myval = myfuncVar(2)", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void ArrowSimple()
        {
            var parse = ParseSource("(el) => 3", true);
            _ = parse.arrowFunc();
            Assert.IsFalse(Services.Log.HasErrors);

            parse = ParseSource("(el) => 3", true);
            _ = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
        }

        [TestMethod]
        public void ArrowBlock()
        {
            var arrowBlockSrc =
@"(el) => {
    .let doubleEl = el * 2
    .return doubleEl
}";
            var parse = ParseSource(arrowBlockSrc, true);
            _ = parse.arrowFunc();
            Assert.IsFalse(Services.Log.HasErrors);

            parse = ParseSource(arrowBlockSrc, true);
            _ = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);

            parse = ParseSource(
@"pots = map(arr, (num) => {
        .let somebs = 3
        .return num == 3 ? 3 * 3 : 2 ^^ num
})", true);
            _ = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            var abs = Services.Symbols.GlobalScope.Resolve("abs") as MathFunction;
            Assert.IsNotNull(abs);

            FunctionValue math = new FunctionValue(abs, Services);
            Value simple = new Value(3);
            Assert.IsFalse(math.GetType() == simple.GetType());
            Assert.IsFalse(math.GetType().Equals(simple.GetType()));
        }

        [TestMethod]
        public void Filter()
        {
            var parse = ParseSource(
@"  .let arr = [1,2,3,4,5,6]
    .let evens = filter(arr, (el) => el % 2 == 0)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var evens = Services.Symbols.GlobalScope.Resolve("evens") as Variable;
            Assert.IsNotNull(evens);
            Assert.IsNotNull(evens.Value);
            Assert.IsTrue(evens.Value.IsDefined);
            Assert.IsInstanceOfType(evens.Value, typeof(ArrayValue));

            var evensArray = evens.Value as ArrayValue;
            Assert.AreEqual(3, evensArray.Count);
            Assert.IsTrue(evensArray.ElementsNumeric);
            Assert.AreEqual(2, evensArray[0].ToInt());
            Assert.AreEqual(4, evensArray[1].ToInt());
            Assert.AreEqual(6, evensArray[2].ToInt());

            parse = ParseSource(
@"  .let hello=""HELLO""
    .let no_ls = filter(hello, (chr) => chr != 'L')", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var no_ls = Services.Symbols.GlobalScope.Resolve("no_ls") as Variable;
            Assert.IsNotNull(no_ls);
            Assert.IsNotNull(no_ls.Value);
            Assert.IsTrue(no_ls.Value.IsDefined);
            Assert.IsTrue(no_ls.Value.IsString);
            Assert.AreEqual("HEO", no_ls.Value.ToString(true));
        }

        [TestMethod]
        public void Concat()
        {
            var parse = ParseSource(
@"  .let odds = [1,3,5]
    .let evens = [2,4,6]
    .let all = concat(odds, evens)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var all = Services.Symbols.GlobalScope.Resolve("all") as Variable;
            Assert.IsNotNull(all);
            Assert.IsNotNull(all.Value);
            Assert.IsTrue(all.Value.IsDefined);
            Assert.IsInstanceOfType(all.Value, typeof(ArrayValue));

            var allArr = all.Value as ArrayValue;
            Assert.AreEqual(6, allArr.Count);
            Assert.IsTrue(allArr.ElementsNumeric);
            Assert.AreEqual(1, allArr[0].ToInt());
            Assert.AreEqual(2, allArr[3].ToInt());
        }

        [TestMethod]
        public void Map()
        {
            var parse = ParseSource(
@"  .let arr = [1,2,3]
    .let doubled = map(arr, (el) => el * 2)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var doubled = Services.Symbols.GlobalScope.Resolve("doubled") as Variable;
            Assert.IsNotNull(doubled);
            Assert.IsNotNull(doubled.Value);
            Assert.IsTrue(doubled.Value.IsDefined);
            Assert.IsInstanceOfType(doubled.Value, typeof(ArrayValue));

            var doubledArray = doubled.Value as ArrayValue;
            Assert.AreEqual(3, doubledArray.Count);
            Assert.IsTrue(doubledArray.ElementsNumeric);
            Assert.AreEqual(2, doubledArray[0].ToInt());
            Assert.AreEqual(4, doubledArray[1].ToInt());
            Assert.AreEqual(6, doubledArray[2].ToInt());

            parse = ParseSource(
@"  .let arr = [1,2,3,4]
    .let pots = map(arr, (num) => {
        .if num == 3
            .return 3 * 3
        .endif
        .return 2 ^^ num
})
", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var pots = Services.Symbols.GlobalScope.Resolve("pots") as Variable;
            Assert.IsNotNull(pots);
            Assert.IsNotNull(pots.Value);
            Assert.IsTrue(pots.Value.IsDefined);
            Assert.IsInstanceOfType(pots.Value, typeof(ArrayValue));

            var potsArray = pots.Value as ArrayValue;
            Assert.AreEqual(4, potsArray.Count);
            Assert.IsTrue(potsArray.ElementsNumeric);
            Assert.AreEqual(2, potsArray[0].ToInt());
            Assert.AreEqual(4, potsArray[1].ToInt());
            Assert.AreEqual(9, potsArray[2].ToInt());
            Assert.AreEqual(16, potsArray[3].ToInt());

            parse = ParseSource(@"
    .let arr = [-1,2,-3]
    .let pos = map(arr, abs)", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var pos = Services.Symbols.GlobalScope.Resolve("pos") as Variable;
            Assert.IsNotNull(pos);
            Assert.IsNotNull(pos.Value);
            Assert.IsTrue(pos.Value.IsDefined);
            Assert.IsInstanceOfType(pos.Value, typeof(ArrayValue));

            var posArray = pos.Value as ArrayValue;
            Assert.AreEqual(3, posArray.Count);
            Assert.IsTrue(posArray.ElementsNumeric);
            Assert.AreEqual(1, posArray[0].ToInt());
            Assert.AreEqual(2, posArray[1].ToInt());
            Assert.AreEqual(3, posArray[2].ToInt());

        }

        [TestMethod]
        public void Reduce()
        {
            var parse = ParseSource(
@"  .let arr = [1,2,3,4]
    .let sum = reduce(arr, (num1, num2) => num1 + num2)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var sum = Services.Symbols.GlobalScope.Resolve("sum") as Variable;
            Assert.IsNotNull(sum);
            Assert.IsNotNull(sum.Value);
            Assert.IsTrue(sum.Value.IsDefined);
            Assert.IsTrue(sum.Value.IsNumeric);
            Assert.AreEqual(1 + 2 + 3 + 4, sum.Value.ToInt());

            parse = ParseSource(
@"  .let arr = [2,3,4]
    .let powed = reduce(arr, pow)", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var powed = Services.Symbols.GlobalScope.Resolve("powed") as Variable;
            Assert.IsNotNull(powed);
            Assert.IsNotNull(powed.Value);
            Assert.IsTrue(powed.Value.IsDefined);
            Assert.IsTrue(powed.Value.IsNumeric);
            Assert.AreEqual(4096, powed.Value.ToInt());
        }


        [TestMethod]
        public void Sort()
        {
            var parse = ParseSource(
@"  .let arr = [8,1,7,4]
    .let sorted = sort(arr)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);

            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var sorted = Services.Symbols.GlobalScope.Resolve("sorted") as Variable;
            Assert.IsNotNull(sorted);
            Assert.IsNotNull(sorted.Value);
            Assert.IsTrue(sorted.Value.IsDefined);
            Assert.IsInstanceOfType(sorted.Value, typeof(ArrayValue));

            var sortedArray = sorted.Value as ArrayValue;
            Assert.AreEqual(4, sortedArray.Count);
            Assert.IsTrue(sortedArray.ElementsNumeric);
            Assert.AreEqual(1, sortedArray[0].ToInt());
            Assert.AreEqual(4, sortedArray[1].ToInt());
            Assert.AreEqual(7, sortedArray[2].ToInt());
            Assert.AreEqual(8, sortedArray[3].ToInt());

        }

        [TestMethod]
        public void ArrowVariable()
        {
            var parse = ParseSource(
@" .let fcn = (el) => el * 2
    .let times2 = fcn(2)", true);
            var tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            var times2 = Services.Symbols.GlobalScope.Resolve("times2") as Variable;
            Assert.IsNotNull(times2);
            Assert.IsNotNull(times2.Value);
            Assert.IsTrue(times2.Value.IsDefined);
            Assert.IsTrue(times2.Value.IsNumeric);
            Assert.AreEqual(4, times2.Value.ToInt());

            parse = ParseSource(
@" .let fcn = (el) => el * 2
    .let arr = [1,2,3]
    .let times2arr = map(arr, fcn)", true);
            tree = parse.source();
            Assert.IsFalse(Services.Log.HasErrors);
            _ = Visitor.Visit(tree);
            Assert.IsFalse(Services.Log.HasErrors);

            times2 = Services.Symbols.GlobalScope.Resolve("times2arr") as Variable;
            Assert.IsNotNull(times2);
            Assert.IsNotNull(times2.Value);
            Assert.IsTrue(times2.Value.IsDefined);
            Assert.IsInstanceOfType(times2.Value, typeof(ArrayValue));

            var times2Arr = times2.Value as ArrayValue;
            Assert.AreEqual(3, times2Arr.Count);
            Assert.IsTrue(times2Arr.ElementsNumeric);
            Assert.AreEqual(2, times2Arr[0].ToInt());
            Assert.AreEqual(4, times2Arr[1].ToInt());
            Assert.AreEqual(6, times2Arr[2].ToInt());

        }
    }
}
