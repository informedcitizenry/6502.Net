using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture]
    public class NUnitTestEvaluation
    {
        [Test]
        public void TestEvaluatorSimple()
        {
            Evaluator eval = new Evaluator();

            eval.DefineSymbolLookup(@"(?<![a-zA-Z0-9_)])'(.)'(?![a-zA-Z0-9_(])", s =>
                Convert.ToInt32(s.Trim('\'').First()).ToString());
           
            string expression = "3+4";
            long dude = eval.Eval(expression);
            Assert.AreEqual(7, dude);

            dude = eval.Eval("3 + 4 * 2");
            Assert.AreEqual(3 + 4 * 2, dude);

            dude = eval.Eval("3 * 4 - 2");
            Assert.AreEqual(3 * 4 - 2, dude);

            dude = eval.Eval("3*(4-2)");
            Assert.AreEqual(3 * (4 - 2), dude);

            dude = eval.Eval("-3");
            Assert.AreEqual(-3, dude);

            dude = eval.Eval("+3");
            Assert.AreEqual(3, dude);

            dude = eval.Eval("3-(-4)");
            Assert.AreEqual(3-(-4), dude);

            dude = eval.Eval("(2 << 4) << 1");
            Assert.AreEqual((2 << 4) << 1, dude);

            dude = eval.Eval("(3*(3+4))/9");
            Assert.AreEqual((3 * (3 + 4)) / 9, dude);

            dude = eval.Eval("2**5");
            Assert.AreEqual((long)Math.Pow(2, 5), dude);

            dude = eval.Eval("432      +    16  /    78 -   ' '");
            Assert.AreEqual(432 + 16 / 78 - 32, dude);

            dude = eval.Eval("5 <= 6 && 7 < 8");
            Assert.AreEqual(1, dude);

            Assert.Throws<ExpressionException>(() => eval.Eval("55 33"));

            Assert.Throws<ExpressionException>(() => eval.Eval("55 < <"));
        }

        [Test]
        public void TestEvaluatorFunction()
        {
            Evaluator eval = new Evaluator();

            // define constant PI
            eval.DefineSymbolLookup(@"(?>_?[a-zA-Z][a-zA-Z0-9_.]*)(?!\()",
                delegate(string symbol)
                {
                    if (symbol.Equals("PI"))
                        return "3.14";
                    return symbol;
                });

            long dude = eval.Eval("abs(-3)");
            Assert.AreEqual(3, dude);

            dude = eval.Eval("pow(2,4)+pow(2,6)");
            Assert.AreEqual((long)(Math.Pow(2, 4) + Math.Pow(2, 6)), dude);

            dude = eval.Eval("PI*pow(1,2)");
            Assert.AreEqual((long)(Math.PI * Math.Pow(1, 2)), dude);

            dude = eval.Eval("pow(pow(2,1),pow(2,2))+pow(3,abs(-3))");
            var result = Math.Pow(Math.Pow(2, 1), Math.Pow(2, 2)) + Math.Pow(3, Math.Abs(-3));
            Assert.AreEqual((long)result, dude);

            dude = eval.Eval("pow(pow(2,1),pow(2,2))+pow(3,abs(-3))");
            Assert.AreEqual((long)result, dude);
        }

        [Test]
        public void TestAllFunctions()
        {
            IEvaluator evaluator = new Evaluator();

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

            Assert.AreEqual((long)Math.Exp(15.0), exp);
            Assert.AreEqual((long)Math.Floor(-4.8), floor);
            Assert.AreEqual((long)Math.Log(2048.0), ln);
            Assert.AreEqual((long)(Math.Log10(0x7fffff) + Math.Log10(3 * 4) + (12 * 3)), log10);
            Assert.AreEqual((long)Math.Pow(2, 16), pow);
            Assert.AreEqual((long)Math.Round(18.21, 0), round);
            Assert.AreEqual((long)Math.Sign(-8.0), negsgn);
            Assert.AreEqual((long)Math.Sign(14.0), possgn);
            Assert.AreEqual((long)Math.Sign(0), nosgn);
            Assert.AreEqual((long)(Math.Sin(1003.9) * 14), sin);
            Assert.AreEqual((long)Math.Sinh(0.0), sinh);
            Assert.AreEqual((long)(Math.Sqrt(65536) - 1), sqrt);
            Assert.AreEqual((long)(Math.Tan(444.0) * 5.0), tan);
            Assert.AreEqual((long)Math.Tanh(0.0), tanh);
        }

        [Test]
        public void TestEvaluatorUnaries()
        {
            Evaluator eval = new Evaluator(@"\$([a-fA-F0-9]+)");

            long dude = eval.Eval("~(53+24)-6*(5-1)");
            Assert.AreEqual(~(53 + 24) - 6 * (5 - 1), dude);

            dude = eval.Eval(">65490");
            Assert.AreEqual((65490 / 256), dude);

            dude = eval.Eval("<65490");
            Assert.AreEqual(65490 & 0xFF, dude);

            dude = eval.Eval(">65490*<32768");
            Assert.AreEqual((65490 / 256) * (32768 & 0xFF), dude);

            int myvar = 548;
            eval.DefineSymbolLookup("myvar", (s) => myvar.ToString());

            long notZero = eval.Eval("~0");
            long not255 = eval.Eval("~255");
            long notmyvar = eval.Eval("~(myvar*2)%256");
            long lsb = eval.Eval("<$8040ffd2");
            long msb = eval.Eval(">$8040ffd2");
            long bb = eval.Eval("^$8040ffd2");
            long mixed = eval.Eval("25*<myvar+>$2456*2");
            long notandnot = eval.Eval("~(35*2) + ~(22*6) + (12*2)");

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
        public void TestEvaluatorHexBins()
        {
            Evaluator eval = new Evaluator(@"\$([a-fA-F0-9]+)");

            long dude = eval.Eval("$100 + %11110000");
            Assert.AreEqual(256 + 0xF0, dude);

            dude = eval.Eval("%##..#.#.");
            Assert.AreEqual(202, dude);
        }

        [Test]
        public void TestEvaluatorBitwise()
        {
            IEvaluator evaluator = new Evaluator(@"\$([a-fA-F0-9]+)");

            int myvar = 224;
            evaluator.DefineSymbolLookup("myvar", (s) => myvar.ToString());

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
        public void TestEvaluatorConditionals()
        {
            IEvaluator eval = new Evaluator(@"\$([a-fA-F0-9]+)");
            bool simple = eval.EvalCondition("1 < 3");
            bool compound = eval.EvalCondition("5+2 > 6 && 4+3 != 12");
            bool complex = eval.EvalCondition("((1<3)||!(4>6)&&(13*2!=4||8>0))");
            bool tricky = eval.EvalCondition("$8000 > $6900 - (16*3 + 32*2)");
            Assert.AreEqual((1 < 3), simple);
            Assert.AreEqual((5 + 2 > 6 && 4 + 3 != 12), compound);
            Assert.AreEqual(((1 < 3) || !(4 > 6) && (13 * 2 != 4 || 8 > 0)), complex);
            Assert.AreEqual(0x8000 > 0x6900 - (16 * 3 + 32 * 2), tricky);
        }

        [Test]
        public void TestDefineSymbols()
        {
            Evaluator eval = new Evaluator();
            eval.DefineSymbolLookup(@"^\++$|^-+$|\(\++\)|\(-+\)", (str) => str.TrimStart('(').TrimEnd(')').Length.ToString());
            eval.DefineSymbolLookup(@"testvar", (str) => "42");
            eval.DefineSymbolLookup(@"(?<![a-zA-Z0-9_.)])\*(?![a-zA-Z0-9_.(])", (str) => "49152");
            eval.DefineSymbolLookup(@"myfunction\(.+\)", m => "34");

            long dude = eval.Eval("5**");
            Assert.AreEqual(5 * 49152, dude);

            dude = eval.Eval("(--)+3");
            Assert.AreEqual(2 + 3, dude);

            dude = eval.Eval("5**");
            Assert.AreEqual(5 * 49152, dude);

            dude = eval.Eval("abs(-34) + myfunction(88.5)");
            Assert.AreEqual((long)(Math.Abs(-34) + 34), dude);

            string tricky = "testvar * *";
            tricky = System.Text.RegularExpressions.Regex.Replace(tricky, @"\s?\*\s?", "*");

            long result1 = eval.Eval("testvar");
            long result2 = eval.Eval("testvar*12");
            long result3 = eval.Eval("testvar**");
            long result4 = eval.Eval(tricky);
            long result5 = eval.Eval("*+testvar");
            long result6 = eval.Eval("testvar**3");
            long result7 = eval.Eval("<testvar+ */256+ >testvar");

            tricky = System.Text.RegularExpressions.Regex.Replace("sin(1009.3) * 42* *", @"\s?\*\s?", "*");
            long result8 = eval.Eval(tricky);

            tricky = System.Text.RegularExpressions.Regex.Replace("* * 3", @"\s?\*\s?", "*");

            long result9 = eval.Eval("**3");
            long result10 = eval.Eval(tricky);
            long result11 = eval.Eval("pow(*,2)");

            Assert.AreEqual(42, result1);
            Assert.AreEqual(42 * 12, result2);
            Assert.AreEqual(42 * 49152, result3);
            Assert.AreEqual(49152 * 42, result4);
            Assert.AreEqual(49152 + 42, result5);
            Assert.AreEqual((long)Math.Pow(42, 3), result6);
            Assert.AreEqual(42 + 49152 / 256 + 0, result7);
            Assert.AreEqual((long)(Math.Sin(1009.3) * 42 * 49152), result8);
            Assert.AreEqual(49152 * 3, result9);
            Assert.AreEqual(49152 * 3, result10);
            Assert.AreEqual(Math.Pow(49152, 2), result11);

            eval.DefineSymbolLookup(@"(?>_?[a-zA-Z][a-zA-Z0-9_]*)(?!\()",
                (str) =>
                {
                    if (str.Equals("var1"))
                        return "1";
                    if (str.Equals("var2"))
                        return "2";
                    Assert.IsTrue(str.Equals("pow") == false);
                    return string.Empty;
                });

            string expression = "var1+var1*var2+pow(2,4)";
            result1 = eval.Eval(expression);
            Assert.AreEqual(1 + 1 * 2 + Math.Pow(2, 4), result1);
        }
    }
}
