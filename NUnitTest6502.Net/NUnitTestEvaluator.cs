using Asm6502.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUnitTest6502.Net
{

    [TestFixture]
    public class EvaluatorTest
    {
        [Test]
        public void TestMaths()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();

            long compound = evaluator.Eval("(2*(3+4))/9");
            long negative = evaluator.Eval("-1");
            long pow = evaluator.Eval("13**2");

            Assert.AreEqual((2 * (3 + 4)) / 9, compound);
            Assert.AreEqual(-1, negative);
            Assert.AreEqual(Convert.ToInt64(Math.Pow(13, 2)), pow);
        }

        [Test]
        public void TestBinHex()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            long hex = evaluator.Eval("$ffd2");
            long bin = evaluator.Eval("%01111111");
            Assert.AreEqual(0xffd2, hex);
            Assert.AreEqual(Convert.ToInt64("01111111", 2), bin);

            evaluator.AllowAlternateBinString = true;
            bin = evaluator.Eval("%.#######");
            Assert.AreEqual(Convert.ToInt64("01111111", 2), bin);
        }

        [Test]
        public void TestCaseSensitivity()
        {
            string expression = "Cbrt(27)+cbrt(8)+CBRT(64)";
            var expected = Math.Pow(27, 1.0 / 3.0) + Math.Pow(8, 1.0 / 3.0) + Math.Pow(64, 1.0 / 3.0);
            ExpressionEvaluator caseInsensitiveEval = new ExpressionEvaluator(true);
            ExpressionEvaluator caseSensitiveEval = new ExpressionEvaluator(false);
            long ciResult = caseInsensitiveEval.Eval(expression);

            Assert.AreEqual(expected, ciResult);
            Assert.Throws<ExpressionEvaluator.ExpressionException>(() => caseSensitiveEval.Eval(expression));
        }

        [Test]
        public void TestFunctions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();

            long abs = evaluator.Eval("abs(-2234)");
            long acos = evaluator.Eval("acos(1.0)");
            long atan = evaluator.Eval("atan(0.0)");
            long cbrt = evaluator.Eval("cbrt(2048383)");
            long ceil = evaluator.Eval("ceil(1.1)");
            long cos = evaluator.Eval("cos(0.0)");
            long cosh = evaluator.Eval("cosh(0.0)");
            long deg = evaluator.Eval("deg(1.0)");
            long exp = evaluator.Eval("exp(15.0)");
            long floor = evaluator.Eval("floor(-4.8)");
            long frac = evaluator.Eval("frac(5.25)*100");
            long hypot = evaluator.Eval("hypot(4.0, 3.0)");
            long ln = evaluator.Eval("ln(2048.0)");
            long log10 = evaluator.Eval("log10(" + 0x7fffff.ToString() + ") + log10(3*4) + (12*3)");
            long pow = evaluator.Eval("pow(2,16)");
            long rad = evaluator.Eval("rad(79999.9)");
            long random = evaluator.Eval("random(-3,2047)");
            long round = evaluator.Eval("round(18.21, 0)");
            long negsgn = evaluator.Eval("sgn(-8.0)");
            long possgn = evaluator.Eval("sgn(14.0)");
            long nosgn = evaluator.Eval("sgn(0)");
            long sin = evaluator.Eval("sin(1003.9) * 14");
            long sinh = evaluator.Eval("sinh(0.0)");
            long sqrt = evaluator.Eval("sqrt(65536) - 1");
            long tan = evaluator.Eval("tan(444.0)*5.0");
            long tanh = evaluator.Eval("tanh(0.0)");

            Assert.AreEqual(Convert.ToInt64(Math.Abs(-2234)), abs);
            Assert.AreEqual(Convert.ToInt64(Math.Acos(1.0)), acos);
            Assert.AreEqual(Convert.ToInt64(Math.Atan(0.0)), atan);
            Assert.AreEqual(Convert.ToInt64(Math.Pow(2048383, 1.0 / 3.0)), cbrt);
            Assert.AreEqual(Convert.ToInt64(Math.Ceiling(1.1)), ceil);
            Assert.AreEqual(Convert.ToInt64(Math.Cos(0.0)), cos);
            Assert.AreEqual(Convert.ToInt64(Math.Cosh(0.0)), cosh);
            Assert.AreEqual(Convert.ToInt64((1.0 * 180.0) / Math.PI), deg);
            Assert.AreEqual(Convert.ToInt64((Math.Abs(5.25) - Math.Abs(Math.Round(5.25, 0))) * 100), frac);
            Assert.AreEqual(Convert.ToInt64((79999.9 * Math.PI) / 180.0), rad);
            Assert.AreEqual(Convert.ToInt64(Math.Sqrt(Math.Pow(3.0, 2) + Math.Pow(4.0, 2))), hypot);

            // random should be sufficiently random at each test, but
            // really no easy way to test "randommness" so we test if it
            // is within the given range.
            Assert.IsTrue(random >= -3 && random <= 2047);

            Assert.AreEqual(Convert.ToInt64(Math.Exp(15.0)), exp);
            Assert.AreEqual(Convert.ToInt64(Math.Floor(-4.8)), floor);
            Assert.AreEqual(Convert.ToInt64(Math.Floor(Math.Log(2048.0))), ln);
            Assert.AreEqual(Convert.ToInt64(Math.Floor(Math.Log10(0x7fffff) + Math.Log10(3 * 4) + (12 * 3))), log10);
            Assert.AreEqual(Convert.ToInt64(Math.Pow(2, 16)), pow);
            Assert.AreEqual(Convert.ToInt64(Math.Round(18.21, 0)), round);
            Assert.AreEqual(Convert.ToInt64(Math.Sign(-8.0)), negsgn);
            Assert.AreEqual(Convert.ToInt64(Math.Sign(14.0)), possgn);
            Assert.AreEqual(Convert.ToInt64(Math.Sign(0)), nosgn);
            Assert.AreEqual(Convert.ToInt64(Math.Sin(1003.9) * 14), sin);
            Assert.AreEqual(Convert.ToInt64(Math.Sinh(0.0)), sinh);
            Assert.AreEqual(Convert.ToInt64(Math.Sqrt(65536) - 1), sqrt);
            Assert.AreEqual(Convert.ToInt64(Math.Tan(444.0) * 5.0), tan);
            Assert.AreEqual(Convert.ToInt64(Math.Tanh(0.0)), tanh);
        }

        [Test]
        public void TestSymbolLookup()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            evaluator.SymbolLookups.Add(@"testvar", (str, ix, obj) => "42");
            evaluator.SymbolLookups.Add(@"(?<=[^a-zA-Z0-9_\.\)]|^)\*(?=[^a-zA-Z0-9_\.\(]|$)", (str, ix, obj) => "49152");
            long result1 = evaluator.Eval("testvar");
            long result2 = evaluator.Eval("testvar*12");
            long result3 = evaluator.Eval("testvar**");
            long result4 = evaluator.Eval("testvar * *");
            long result5 = evaluator.Eval("*+testvar");
            long result6 = evaluator.Eval("testvar**3");
            long result7 = evaluator.Eval("<testvar+ */256+ >testvar");
            long result8 = evaluator.Eval("sin(1009.3) * 42* *");
            long result9 = evaluator.Eval("**3");
            long result10 = evaluator.Eval("* * 3");
            long result11 = evaluator.Eval("pow(*,2)");

            Assert.AreEqual(42, result1);
            Assert.AreEqual(42 * 12, result2);
            Assert.AreEqual(42 * 49152, result3);
            Assert.AreEqual(49152 * 42, result4);
            Assert.AreEqual(49152 + 42, result5);
            Assert.AreEqual(Convert.ToInt64(Math.Pow(42, 3)), result6);
            Assert.AreEqual(42 + 49152 / 256 + 0, result7);
            Assert.AreEqual(Convert.ToInt64(Math.Floor(Math.Sin(1009.3) * 42 * 49152)), result8);
            Assert.AreEqual(49152 * 3, result9);
            Assert.AreEqual(49152 * 3, result10);
            Assert.AreEqual(Math.Pow(49152, 2), result11);
        }

        [Test()]
        public void TestUnaryExpressions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            int myvar = 548;
            evaluator.SymbolLookups.Add("myvar", (s, i, o) => myvar.ToString());
            long notZero = evaluator.Eval("~0");
            long not255 = evaluator.Eval("~255");
            long notmyvar = evaluator.Eval("~(myvar*2)%256");
            long lsb = evaluator.Eval("<$8040ffd2");
            long msb = evaluator.Eval(">$8040ffd2");
            long bb = evaluator.Eval("^$8040ffd2");
            long mixed = evaluator.Eval("25*<myvar+>$2456*2");
            long notandnot = evaluator.Eval("~(35*2) + ~(22*6) + (12*2)");

            Assert.AreEqual(~(myvar * 2) % 256, notmyvar);
            Assert.AreEqual(~0, notZero);
            Assert.AreEqual(~255, not255);
            Assert.AreEqual(0xd2, lsb);
            Assert.AreEqual(0xff, msb);
            Assert.AreEqual(0x40, bb);
            Assert.AreEqual(25 * (myvar % 256) + 0x24 * 2, mixed);
            Assert.AreEqual(~(35 * 2) + ~(22 * 6) + (12 * 2), notandnot);
        }

        [Test]
        public void TestBitwiseExpressions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();

            int myvar = 224;
            evaluator.SymbolLookups.Add("myvar", (s, i, o) => myvar.ToString());

            long and = evaluator.Eval("myvar&$e0");
            long or = evaluator.Eval("myvar|$0f");
            long xor = evaluator.Eval("myvar^$ef");
            long rol = evaluator.Eval("myvar<<2");
            long ror = evaluator.Eval("myvar>>2");

            Assert.AreEqual(myvar & 0xe0, and);
            Assert.AreEqual(myvar | 0x0f, or);
            Assert.AreEqual(myvar ^ 0xef, xor);
            Assert.AreEqual(myvar << 2, rol);
            Assert.AreEqual(myvar >> 2, ror);
        }

        [Test]
        public void TestConditionalExpressions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            bool simple = evaluator.EvalCondition("1 < 3");
            bool compound = evaluator.EvalCondition("5+2 > 6 && 4+3 != 12");
            bool complex = evaluator.EvalCondition("((1<3)||!(4>6)&&(13*2!=4||8>0))");
            bool tricky = evaluator.EvalCondition("$8000 > $6900 - (16*3 + 32*2)");
            Assert.AreEqual((1 < 3), simple);
            Assert.AreEqual((5 + 2 > 6 && 4 + 3 != 12), compound);
            Assert.AreEqual(((1 < 3) || !(4 > 6) && (13 * 2 != 4 || 8 > 0)), complex);
        }
    }
}
