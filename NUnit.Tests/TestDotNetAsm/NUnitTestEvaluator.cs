using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestEvaluator
    {
        [Test]
        public void TestEvaluatorSimple()
        {
            var eval = new Evaluator();

            string expression = "3+4";
            var result = eval.Eval(expression);
            Assert.AreEqual(7, result);

            result = eval.Eval("3 + 4 * 2");
            Assert.AreEqual(3 + 4 * 2, result);

            result = eval.Eval("3 * 4 - 2");
            Assert.AreEqual(3 * 4 - 2, result);

            result = eval.Eval("3*(4-2)");
            Assert.AreEqual(3 * (4 - 2), result);

            result = eval.Eval("-3");
            Assert.AreEqual(-3, result);

            result = eval.Eval("+3");
            Assert.AreEqual(3, result);

            result = eval.Eval("3-(-4)");
            Assert.AreEqual(3-(-4), result);

            result = eval.Eval("(2 << 4) << 1");
            Assert.AreEqual((2 << 4) << 1, result);

            result = eval.Eval("(3*(3+4))/9");
            Assert.AreEqual((3 * (3 + 4)) / 9, result);

            result = eval.Eval("2**5");
            Assert.AreEqual((long)Math.Pow(2, 5), result);

            result = eval.Eval("432      +    16  /    78 -   3");
            Assert.AreEqual(432 + 16 / 78 - 3, result);

            result = eval.Eval("5 <= 6 && 7 < 8");
            Assert.AreEqual(1, result);

            Assert.Throws<ExpressionException>(() => eval.Eval("55 33"));

            Assert.Throws<ExpressionException>(() => eval.Eval("55 < <"));
        }

        [Test]
        public void TestEvaluatorFunction()
        {
            var eval = new Evaluator();
            var result = eval.Eval("abs(-3)");
            Assert.AreEqual(3, result);

            result = eval.Eval("pow(2,4)+pow(2,6)");
            Assert.AreEqual((long)(Math.Pow(2, 4) + Math.Pow(2, 6)), result);

            result = eval.Eval("pow(pow(2,1),pow(2,2))+pow(3,abs(-3))");
            var result2 = Math.Pow(Math.Pow(2, 1), Math.Pow(2, 2)) + Math.Pow(3, Math.Abs(-3));
            Assert.AreEqual((long)result2, result);
        }

        [Test]
        public void TestExpressionErrors()
        {
            var eval = new Evaluator();

            Assert.Throws<ExpressionException>(() => eval.Eval("56(34)"));
            Assert.Throws<ExpressionException>(() => eval.Eval("*56"));
            Assert.Throws<ExpressionException>(() => eval.Eval("56 (24)"));
            Assert.Throws<ExpressionException>(() => eval.Eval("56+"));
            Assert.Throws<ExpressionException>(() => eval.Eval("56*(24+)"));
            Assert.Throws<ExpressionException>(() => eval.Eval("(4+1"));
            Assert.Throws<ExpressionException>(() => eval.Eval("8+2)"));
        }

        [Test]
        public void TestAllFunctions()
        {
            IEvaluator evaluator = new Evaluator();

            var abs = evaluator.Eval("abs(-2234)");
            var acos = evaluator.Eval("acos(1.0)");
            var atan = evaluator.Eval("atan(0.0)");
            var cbrt = evaluator.Eval("cbrt(2048383)");
            var ceil = evaluator.Eval("ceil(1.1)");
            var cos = evaluator.Eval("cos(0.0)");
            var cosh = evaluator.Eval("cosh(0.0)");
            var deg = evaluator.Eval("deg(1.0)");
            var exp = evaluator.Eval("exp(15.0)");
            var floor = evaluator.Eval("floor(-4.8)");
            var frac = evaluator.Eval("frac(5.25)*100");
            var hypot = evaluator.Eval("hypot(4.0, 3.0)");
            var ln = evaluator.Eval("ln(2048.0)");
            var log10 = evaluator.Eval("log10(" + 0x7fffff.ToString() + ") + log10(3*4) + (12*3)");
            var pow = evaluator.Eval("pow(2,16)");
            var rad = evaluator.Eval("rad(79999.9)");
            var random = evaluator.Eval("random(-3,2047)");
            var round = evaluator.Eval("round(18.21)");
            var negsgn = evaluator.Eval("sgn(-8.0)");
            var possgn = evaluator.Eval("sgn(14.0)");
            var nosgn = evaluator.Eval("sgn(0)");
            var sin = evaluator.Eval("sin(1003.9) * 14");
            var sinh = evaluator.Eval("sinh(0.0)");
            var sqrt = evaluator.Eval("sqrt(65536) - 1");
            var tan = evaluator.Eval("tan(444.0)*5.0");
            var tanh = evaluator.Eval("tanh(0.0)");

            
            Assert.AreEqual((long)Math.Abs(-2234), abs);
            Assert.AreEqual((long)Math.Acos(1.0), acos);
            Assert.AreEqual((long)Math.Atan(0.0), atan);
            Assert.AreEqual((long)Math.Pow(2048383, 1.0 / 3.0), cbrt);
            Assert.AreEqual((long)Math.Ceiling(1.1), ceil);
            Assert.AreEqual((long)Math.Cos(0.0), cos);
            Assert.AreEqual((long)Math.Cosh(0.0), cosh);
            Assert.AreEqual((long)((1.0 * 180.0) / Math.PI), deg);
            Assert.AreEqual((long)((Math.Abs(5.25) - Math.Abs(Math.Round(5.25, 0))) * 100), frac);
            Assert.AreEqual((long)((79999.9 * Math.PI) / 180.0), rad);
            Assert.AreEqual((long)Math.Sqrt(Math.Pow(3.0, 2) + Math.Pow(4.0, 2)), hypot);

            // random should be sufficiently random at each test, but
            // really no easy way to test "randommness" so we test if it
            // is within the given range.
            Assert.IsTrue(random >= -3 && random <= 2047);

            Assert.AreEqual((long)Math.Exp(15.0), exp);
            Assert.AreEqual((long)Math.Floor(-4.8), floor);
            Assert.AreEqual((long)Math.Log(2048.0), ln);
            Assert.AreEqual((long)(Math.Log10(0x7fffff) + Math.Log10(3 * 4) + (12 * 3)), log10);
            Assert.AreEqual((long)Math.Pow(2, 16), pow);
            Assert.AreEqual((long)Math.Round(18.21), round);
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
            var eval = new Evaluator();

            var result = eval.Eval("~(53+24)-6*(5-1)");
            Assert.AreEqual(~(53 + 24) - 6 * (5 - 1), result);

            result = eval.Eval(">65490");
            Assert.AreEqual((65490 / 256), result);

            result = eval.Eval("<65490");
            Assert.AreEqual(65490 & 0xFF, result);

            result = eval.Eval(">65490*<32768");
            Assert.AreEqual((65490 / 256) * (32768 & 0xFF), result);

            int myvar = 548;
            var notZero = eval.Eval("~0");
            var not255 = eval.Eval("~255");
            var notmyvar = eval.Eval("~(548*2)%256");
            var lsb = eval.Eval("<$8040ffd2");
            var msb = eval.Eval(">$8040ffd2");
            var word = eval.Eval("&$8040ffd2");
            var bb = eval.Eval("^$8040ffd2");
            var mixed = eval.Eval("25*<548+>$2456*2");
            var notandnot = eval.Eval("~(35*2) + ~(22*6) + (12*2)");

            Assert.AreEqual(~(myvar * 2) % 256, notmyvar);
            Assert.AreEqual(~0, notZero);
            Assert.AreEqual(~255, not255);
            Assert.AreEqual(0xd2, lsb);
            Assert.AreEqual(0xff, msb);
            Assert.AreEqual(0xffd2, word);
            Assert.AreEqual(0x40, bb);
            Assert.AreEqual(25 * (myvar % 256) + 0x24 * 2, mixed);
            Assert.AreEqual(~(35 * 2) + ~(22 * 6) + (12 * 2), notandnot);
        }

        [Test]
        public void TestEvaluatorHexBins()
        {
            var eval = new Evaluator();

            var result = eval.Eval("$100 + %11110000");
            Assert.AreEqual(256 + 0xF0, result);

            result = eval.Eval("%##..#.#.");
            Assert.AreEqual(202, result);
        }

        [Test]
        public void TestEvaluatorBitwise()
        {
            IEvaluator evaluator = new Evaluator();

            int myvar = 224;

            var and = evaluator.Eval("224&$e0");
            var or = evaluator.Eval("224|$0f");
            var xor = evaluator.Eval("224^$ef");
            var rol = evaluator.Eval("224<<2");
            var ror = evaluator.Eval("224>>2");

            Assert.AreEqual(myvar & 0xe0, and);
            Assert.AreEqual(myvar | 0x0f, or);
            Assert.AreEqual(myvar ^ 0xef, xor);
            Assert.AreEqual(myvar << 2, rol);
            Assert.AreEqual(myvar >> 2, ror);
        }

        [Test]
        public void TestEvaluatorConditionals()
        {
            double d1 = 12.0;
            double d2 = 7.0;
            double d3 = 7.0;
            Assert.IsTrue(Math.Abs(d1 - d2) > double.Epsilon);
            Assert.IsTrue(Math.Abs(d2 - d3) <= double.Epsilon);
            IEvaluator eval = new Evaluator();
            var simple = eval.EvalCondition("1 < 3");
            var compound = eval.EvalCondition("5+2 > 6 && 4+3 != 12");
            var complex = eval.EvalCondition("((1<3)||!(4>6)&&(13*2!=4||8>0))");
            var tricky = eval.EvalCondition("$8000 > $6900 - (16*3 + 32*2)");
            Assert.IsTrue(simple);
            Assert.IsTrue(compound);
            Assert.IsTrue(complex);
            Assert.IsTrue(tricky);
            //Assert.AreEqual((1 < 3), simple);
            //Assert.AreEqual((5 + 2 > 6 && 4 + 3 != 12), compound);
            //Assert.AreEqual(((1 < 3) || !(4 > 6) && (13 * 2 != 4 || 8 > 0)), complex);
            //Assert.AreEqual(0x8000 > 0x6900 - (16 * 3 + 32 * 2), tricky);
        }

        [Test]
        public void TestParsing()
        {
            var eval = new Evaluator();

            var expression = "-6";
            var elements = eval.ParseElements(expression).ToList();
            Assert.IsTrue(elements.Count == 2);
            Assert.AreEqual("-", elements[0].word);
            Assert.IsTrue(elements[0].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[0].subtype == ExpressionElement.Subtype.Unary);
            Assert.AreEqual("6", elements[1].word);
            Assert.IsTrue(elements[1].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[1].subtype == ExpressionElement.Subtype.None);

            expression = "5 +2";
            elements = eval.ParseElements(expression).ToList();
            Assert.IsTrue(elements.Count == 3);
            Assert.AreEqual("5", elements[0].word);
            Assert.IsTrue(elements[0].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[0].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual("+", elements[1].word);
            Assert.IsTrue(elements[1].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[1].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("2", elements[2].word);
            Assert.IsTrue(elements[2].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[2].subtype == ExpressionElement.Subtype.None);

            expression = "5<<2";
            elements = eval.ParseElements(expression).ToList();
            Assert.IsTrue(elements.Count == 3);
            Assert.AreEqual("5", elements[0].word);
            Assert.IsTrue(elements[0].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[0].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual("<<", elements[1].word);
            Assert.IsTrue(elements[1].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[1].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("2", elements[2].word);
            Assert.IsTrue(elements[2].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[2].subtype == ExpressionElement.Subtype.None);


            expression = "-(6.34-2) * 5+ 3";
            elements = eval.ParseElements(expression).ToList();
            Assert.IsTrue(elements.Count == 10);
            Assert.AreEqual("-", elements[0].word);
            Assert.IsTrue(elements[0].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[0].subtype == ExpressionElement.Subtype.Unary);
            Assert.AreEqual("(", elements[1].word);
            Assert.IsTrue(elements[1].type == ExpressionElement.Type.Group);
            Assert.IsTrue(elements[1].subtype == ExpressionElement.Subtype.Open);
            Assert.AreEqual("6.34", elements[2].word);
            Assert.IsTrue(elements[2].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[2].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual("-", elements[3].word);
            Assert.IsTrue(elements[3].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[3].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("2", elements[4].word);
            Assert.IsTrue(elements[4].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[4].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual(")", elements[5].word);
            Assert.IsTrue(elements[5].type == ExpressionElement.Type.Group);
            Assert.IsTrue(elements[5].subtype == ExpressionElement.Subtype.Close);
            Assert.AreEqual("*", elements[6].word);
            Assert.IsTrue(elements[6].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[6].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("5", elements[7].word);
            Assert.IsTrue(elements[7].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[7].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual("+", elements[8].word);
            Assert.IsTrue(elements[8].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[8].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("3", elements[9].word);
            Assert.IsTrue(elements[9].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[9].subtype == ExpressionElement.Subtype.None);

            expression = "523 < <+32";
            elements = eval.ParseElements(expression).ToList();
            Assert.IsTrue(elements.Count == 5);
            Assert.AreEqual("523", elements[0].word);
            Assert.IsTrue(elements[0].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[0].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual("<", elements[1].word);
            Assert.IsTrue(elements[1].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[1].subtype == ExpressionElement.Subtype.Binary);
            Assert.IsTrue(elements[1].Equals(elements[2]));
            Assert.AreEqual("+", elements[3].word);
            Assert.IsTrue(elements[3].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[3].subtype == ExpressionElement.Subtype.Unary);
            Assert.AreEqual("32", elements[4].word);
            Assert.IsTrue(elements[4].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[4].subtype == ExpressionElement.Subtype.None);

            expression = "pow(2,4) + pow( 3 , 8)";
            elements = eval.ParseElements(expression).ToList();
            Assert.IsTrue(elements.Count == 13);
            Assert.AreEqual("pow", elements[0].word);
            Assert.IsTrue(elements[0].type == ExpressionElement.Type.Function);
            Assert.IsTrue(elements[0].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual("(", elements[1].word);
            Assert.IsTrue(elements[1].type == ExpressionElement.Type.Group);
            Assert.IsTrue(elements[1].subtype == ExpressionElement.Subtype.Open);
            Assert.AreEqual("2", elements[2].word);
            Assert.IsTrue(elements[2].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[2].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual(",", elements[3].word);
            Assert.IsTrue(elements[3].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[3].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("4", elements[4].word);
            Assert.IsTrue(elements[4].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[4].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual(")", elements[5].word);
            Assert.IsTrue(elements[5].type == ExpressionElement.Type.Group);
            Assert.IsTrue(elements[5].subtype == ExpressionElement.Subtype.Close);
            Assert.AreEqual("+", elements[6].word);
            Assert.IsTrue(elements[6].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[6].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual(elements[0], elements[7]);
            Assert.AreEqual("(", elements[8].word);
            Assert.IsTrue(elements[8].type == ExpressionElement.Type.Group);
            Assert.IsTrue(elements[8].subtype == ExpressionElement.Subtype.Open);
            Assert.AreEqual("3", elements[9].word);
            Assert.IsTrue(elements[9].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[9].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual(",", elements[10].word);
            Assert.IsTrue(elements[10].type == ExpressionElement.Type.Operator);
            Assert.IsTrue(elements[10].subtype == ExpressionElement.Subtype.Binary);
            Assert.AreEqual("8", elements[11].word);
            Assert.IsTrue(elements[11].type == ExpressionElement.Type.Operand);
            Assert.IsTrue(elements[11].subtype == ExpressionElement.Subtype.None);
            Assert.AreEqual(")", elements[12].word);
            Assert.IsTrue(elements[12].type == ExpressionElement.Type.Group);
            Assert.IsTrue(elements[12].subtype == ExpressionElement.Subtype.Close);
        }
    }
}
