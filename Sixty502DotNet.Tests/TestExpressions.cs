using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;
namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestExpressions : TestBase
    {
        [TestMethod]
        public void Exponential()
        {
            var result = ParseExpression("1E+3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(TypeCode.Double, result.DotNetType);
            Assert.AreEqual(1e+3, result.ToDouble());

            result = ParseExpression("6.0221409e+23");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(TypeCode.Double, result.DotNetType);
            Assert.AreEqual(6.0221409e+23, result.ToDouble());

            result = ParseExpression("$1.ffa4000p+15");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(65490.0, result.ToDouble());
        }

        [TestMethod]
        public void LargeNumbers()
        {
            var result = ParseExpression("$FFFF_FFFF");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0xFFFF_FFFF, result.ToLong());

            result = ParseExpression("0x1_0000_0000");
            //Assert.IsTrue(Services.Log.HasErrors);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.IsFalse(result.IsIntegral);
        }

        [TestMethod]
        public void MultilineString()
        {
            var result = ParseExpression(
@"""""""he said,
""hello world""
to me
""""""");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("he said,\n\"hello world\"\nto me\n", result.ToString(true));

        }

        [TestMethod]
        public void ExpressionType()
        {
            var result = ParseExpression("3*6-4");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);

            result = ParseExpression("5*6.0");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(TypeCode.Double, result.DotNetType);

            result = ParseExpression("(3*(3+4))/9.0");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(TypeCode.Double, result.DotNetType);
            Assert.AreEqual(2.33333333333333333333, result.ToDouble());

            result = ParseExpression("\"h\"+\"e\"");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("he", result.ToString(true));

            result = ParseExpression("'l'+\"l\"");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("ll", result.ToString(true));

            result = ParseExpression("'o'+'!'");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual('o' + '!', result.ToInt());

            result = ParseExpression("5<6");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.AreEqual(TypeCode.Boolean, result.DotNetType);
            Assert.IsTrue(result.ToBool());

            result = ParseExpression("5<=>6");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(Math.Sign(5 - 6), result.ToInt());

            result = ParseExpression("5 > 6 ? 42 : \"false\"");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("false", result.ToString(true));
        }

        [TestMethod]
        public void Unary()
        {
            var result = ParseExpression("-2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(-2, result.ToInt());

            result = ParseExpression("~1");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(~1, result.ToInt());

            result = ParseExpression("!(5 > 2)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.AreEqual(TypeCode.Boolean, result.DotNetType);
            Assert.IsFalse(result.ToBool());

            result = ParseExpression("<$ffd2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0xd2, result.ToInt());
        }

        [TestMethod]
        public void Precedence()
        {
            var result = ParseExpression("5+6*3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(5 + 6 * 3, result.ToInt());

            result = ParseExpression("(5+6)*3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual((5 + 6) * 3, result.ToInt());

            result = ParseExpression("5+6<<3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(5 + 6 << 3, result.ToInt());

            result = ParseExpression("5<<6+3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(5 << 6 + 3, result.ToInt());

            result = ParseExpression("5&6<<3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(5 & 6 << 3, result.ToInt());

            result = ParseExpression("((5-2)*6)/(8-3)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(((5 - 2) * 6) / (8 - 3), result.ToInt());

            result = ParseExpression("-5+2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(-3, result.ToInt());

            result = ParseExpression(">0xFFD2+2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0xFF, result.ToInt());

            result = ParseExpression("(>0xFFD2)+2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0x101, result.ToInt());
        }

        [TestMethod]
        public void PrimaryExpressions()
        {
            var parse = ParseSource("$ffd2", true, false);
            var tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            var result = Evaluator.GetPrimaryExpression(tree);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0xffd2, result.ToInt());

            parse = ParseSource("-$ffd2", true, false);
            tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            result = Evaluator.GetPrimaryExpression(tree);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(-0xffd2, result.ToInt());

            parse = ParseSource("\"hello\"+' '", true, false);
            tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            result = Evaluator.GetPrimaryExpression(tree);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("hello ", result.ToString(true));

            parse = ParseSource("5/2+(13-2)", true, false);
            tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            result = Evaluator.GetPrimaryExpression(tree);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(5 / 2 + (13 - 2), result.ToInt());

            parse = ParseSource("%1011010", true, false);
            tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            result = Evaluator.GetPrimaryExpression(tree);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0b1011010, result.ToInt());

            parse = ParseSource("MATH_PI", true, false);
            tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            result = Evaluator.GetPrimaryExpression(tree);
            Assert.IsFalse(result.IsDefined);

            parse = ParseSource("TRUE", true, false);
            tree = parse.expr();
            Assert.IsFalse(Services.Log.HasErrors);
            result = Services.ExpressionVisitor.Visit(tree);//Evaluator.GetPrimaryExpression(tree);
            Assert.IsTrue(result.IsDefined);
            Assert.AreEqual(TypeCode.Boolean, result.DotNetType);
            Assert.IsTrue(result.ToBool());
        }

        [TestMethod]
        public void BuiltinFunctions()
        {
            var powFcn = Services.Symbols.Scope.Resolve("pow");
            Assert.IsNotNull(powFcn);
            Assert.IsInstanceOfType(powFcn, typeof(MathFunction));

            var result = ParseExpression("pow(2,4)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(Math.Pow(2, 4), result.ToDouble());

            result = ParseExpression("word(-32768)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(0x8000, result.ToDouble());

            var formatFcn = Services.Symbols.Scope.Resolve("format");
            Assert.IsNotNull(formatFcn);
            Assert.IsInstanceOfType(formatFcn, typeof(FormatFunction));

            result = ParseExpression("format(\"Start: ${0:X4}\\nEnd: ${1:X4}\", $c000, $cfff)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("Start: $C000\nEnd: $CFFF", result.ToString(true));

            var sqrtFcn = Services.Symbols.Scope.Resolve("sqrt");
            Assert.IsNotNull(sqrtFcn);
            Assert.IsInstanceOfType(sqrtFcn, typeof(MathFunction));

            result = ParseExpression("sqrt(pow(2, pow(2,4)))");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(Math.Sqrt(Math.Pow(2, Math.Pow(2, 4))), result.ToDouble());

            var intFcn = Services.Symbols.Scope.Resolve("int");
            Assert.IsNotNull(intFcn);
            Assert.IsInstanceOfType(intFcn, typeof(IntFunction));

            result = ParseExpression("int(3.1419)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(3, result.ToInt());

            var floatFcn = Services.Symbols.Scope.Resolve("float");
            Assert.IsNotNull(floatFcn);
            Assert.IsInstanceOfType(floatFcn, typeof(FloatFunction));

            result = ParseExpression("float(42)");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.AreEqual(TypeCode.Double, result.DotNetType);
            Assert.AreEqual(42, result.ToDouble());
        }

        [TestMethod]
        public void PrePostFix()
        {
            var myvar = new Variable("myvar", new Value(1));
            Services.Symbols.Scope.Define("myvar", myvar);
            Services.Symbols.DeclareVariable(myvar);

            var result = ParseExpression("myvar++");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(1, result.ToInt());
            Assert.AreEqual(2, myvar.Value.ToInt());

            result = ParseExpression("++myvar");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(3, result.ToInt());
            Assert.AreEqual(3, myvar.Value.ToInt());
        }

        [TestMethod]
        public void CompoundAssignment()
        {
            var myvar = Services.Symbols.Scope.Resolve("myvar") as Variable;
            if (myvar == null)
            {
                myvar = new Variable("myvar", new Value(1));
                Services.Symbols.Scope.Define("myvar", myvar);
                Services.Symbols.DeclareVariable(myvar);
            }
            else
            {
                myvar.Value.SetAs(new Value(1));
            }
            var result = ParseExpression("myvar += 1");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(2, result.ToInt());

            result = ParseExpression("myvar *= 2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(4, result.ToInt());

            result = ParseExpression("myvar <<= 1");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(4 << 1, result.ToInt());
        }
    }
}
