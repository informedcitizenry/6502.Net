using DotNetAsm;
using NUnit.Framework;
using System;
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

        [Test()]
        public void TestTranslateSymbol()
        {
            var expression = "+1";
            var translated = _controller.Symbols.TranslateExpressionSymbols(new SourceLine(), expression, string.Empty, true);
            Assert.AreEqual(expression, translated);

            expression = "+$02";
            translated = _controller.Symbols.TranslateExpressionSymbols(new SourceLine(), expression, string.Empty, true);
            Assert.AreEqual(expression, translated);
        }
    }
}
