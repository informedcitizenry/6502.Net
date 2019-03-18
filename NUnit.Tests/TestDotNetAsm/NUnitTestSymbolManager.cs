using DotNetAsm;
using NUnit.Framework;
using System;
using System.Linq;
namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture()]
    public class NUnitTestSymbolManager
    {
        IAssemblyController _controller;

        public NUnitTestSymbolManager()
        {
            _controller = new TestController();
        }

        [Test]
        public void TestTranslateSymbols()
        {
            _controller.Symbols.Labels.SetLabel("BADGUY", -4096, false, true);
            _controller.Symbols.Labels.SetLabel("GAMERAM", 0x4000, false, true);
            _controller.Symbols.Labels.SetLabel("SCREEN_RAM", 0x6400, false, true);
            _controller.Symbols.Labels.SetLabel("CBM.CINV", 0x316, false, false);

            var expression = "BADGUY * 2";
            Assert.AreEqual(-8192, _controller.Evaluator.Eval(expression));

            expression = "+";
            Assert.AreEqual(-1, _controller.Evaluator.Eval(expression));

            expression = "-1";
            Assert.AreEqual(-1, _controller.Evaluator.Eval(expression));

            expression = "'E' | $80";
            Assert.AreEqual(197, _controller.Evaluator.Eval(expression));

            expression = "$A000-GAMERAM * +";
            Assert.AreEqual(0xA000 - 0x4000 * -1, _controller.Evaluator.Eval(expression));

            expression = "SCREEN_RAM+(40*10+30)";
            Assert.AreEqual(0x6400 + (40 * 10 + 30), _controller.Evaluator.Eval(expression));

            expression = "CBM.CINV+1";
            Assert.AreEqual(0x316 + 1, _controller.Evaluator.Eval(expression));

            expression = "1 + POW ( 2, 4)";
            Assert.AreEqual(1 + Math.Pow(2, 4), _controller.Evaluator.Eval(expression));

            expression = "'E' + 2";
            Assert.AreEqual(71, _controller.Evaluator.Eval(expression));
            
            expression = "(+) * (++)";
            Assert.AreEqual(1, _controller.Evaluator.Eval(expression));

            expression = "+ * ++";
            Assert.AreEqual(1, _controller.Evaluator.Eval(expression));

            expression = "* + 3";
            _controller.Output.SetPC(0x1000);
            Assert.AreEqual(0x1003, _controller.Evaluator.Eval(expression));

            expression = "* * 3";
            Assert.AreEqual(0x3000, _controller.Evaluator.Eval(expression));

            expression = "MATH_PI ** 2";
            Assert.AreEqual((long)Math.Pow(Math.PI, 2), _controller.Evaluator.Eval(expression));
        }
    }
}
