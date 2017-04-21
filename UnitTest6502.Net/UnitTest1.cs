using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Asm6502.Net;

namespace UnitTest6502.Net
{
    [TestClass]
    public class GeneralConceptsTest
    {
        [TestMethod]
        public void TestcaseSensitivity()
        {
            StringComparison ignore = StringComparison.CurrentCultureIgnoreCase;
            StringComparer ignoreC = StringComparer.CurrentCultureIgnoreCase;
            StringComparison casesensitive = StringComparison.CurrentCulture;
            StringComparer casesensitiveC = StringComparer.CurrentCulture;
            System.Collections.Generic.HashSet<string> ignoreHash = new System.Collections.Generic.HashSet<string>(ignoreC);
            System.Collections.Generic.HashSet<string> csHash = new System.Collections.Generic.HashSet<string>(casesensitiveC);

            string lstring = "bob";
            string Mstring = "Bob";

            ignoreHash.Add(lstring);
            csHash.Add(Mstring);

            Assert.IsTrue(ignoreHash.Contains(lstring.ToUpper())); // BOB
            Assert.IsTrue(csHash.Contains(Mstring));
            Assert.IsFalse(csHash.Contains(Mstring.ToUpper())); // BOB

            Assert.IsTrue(lstring.Equals(Mstring, ignore));
            Assert.IsFalse(Mstring.Equals(lstring, casesensitive));
        }
    }

    [TestClass]
    public class ExpressionEvaluatorTest
    {
        [TestMethod]
        public void TestBasicMath()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();

            long r1 = evaluator.Eval("1+2*5");
            long r2 = evaluator.Eval("6*5+3");
            long r3 = evaluator.Eval("((6+1)-2)/2");

            Assert.AreEqual(1 + 2 * 5, r1);
            Assert.AreEqual(6 * 5 + 3, r2);
            Assert.AreEqual(((6 + 1) - 2) / 2, r3);
        }

        [TestMethod]
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

        [TestMethod]
        [ExpectedException(typeof(ExpressionEvaluator.ExpressionException),
            "Evaluation failed because expression contained function names in the wrong case.")]
        public void TestCaseSensitivity()
        {
            string expression = "Cbrt(27)+cbrt(8)+CBRT(64)";
            var expected = Math.Pow(27, 1.0 / 3.0) + Math.Pow(8, 1.0 / 3.0) + Math.Pow(64, 1.0 / 3.0);
            ExpressionEvaluator caseInsensitiveEval = new ExpressionEvaluator(true);
            ExpressionEvaluator caseSensitiveEval = new ExpressionEvaluator(false);
            long ciResult = caseInsensitiveEval.Eval(expression);
            long csResult = caseSensitiveEval.Eval(expression);
            Assert.AreEqual(expected, ciResult);
        }

        [TestMethod]
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
            long log10 = evaluator.Eval("log10(" + 0x7fffff.ToString() + ")");
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
            Assert.AreEqual(Convert.ToInt64(Math.Pow(2048383,1.0/3.0)), cbrt);
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
            Assert.AreEqual(Convert.ToInt64(Math.Floor(Math.Log10(0x7fffff))), log10);
            Assert.AreEqual(Convert.ToInt64(Math.Pow(2,16)), pow);
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

        [TestMethod]
        public void TestSymbolLookup()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            evaluator.SymbolLookups.Add(@"testvar", (str, obj) => "42");
            evaluator.SymbolLookups.Add(@"\*", (str, obj) => "49152");
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

        [TestMethod]
        public void TestUnaryExpressions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            int myvar = 548;
            evaluator.SymbolLookups.Add("myvar", (s, o) => myvar.ToString());
            long notZero = evaluator.Eval("~0");
            long not255 = evaluator.Eval("~255");
            long notmyvar = evaluator.Eval("~(myvar*2)%256");
            long lsb = evaluator.Eval("<$ffd2");
           
            long msb = evaluator.Eval(">$ffd2");
            long bb = evaluator.Eval("^$ffd2");

            Assert.AreEqual(~(myvar*2)%256, notmyvar);
            Assert.AreEqual(~0, notZero);
            Assert.AreEqual(~255, not255);
            Assert.AreEqual(0xd2, lsb);
            Assert.AreEqual(0xff, msb);
            Assert.AreEqual(0x00, bb);
        }

        [TestMethod]
        public void TestBitwiseExpressions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();

            int myvar = 224;
            evaluator.SymbolLookups.Add("myvar", (s, o) => myvar.ToString());

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

        [TestMethod]
        public void TestConditionalExpressions()
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            bool simple = evaluator.EvalCondition("1 < 3");
            bool compound = evaluator.EvalCondition("5+2 > 6 && 4+3 != 12");
            bool complex = evaluator.EvalCondition("((1<3)||(4>6)&&(13*2!=4||8>0))");
            Assert.AreEqual((1 < 3), simple);
            Assert.AreEqual((5 + 2 > 6 && 4 + 3 != 12), compound);
            Assert.AreEqual(((1 < 3) || (4 > 6) && (13 * 2 != 4 || 8 > 0)), complex);
        }
    }

    [TestClass]
    public class SourceLineTest
    {
        [TestMethod]
        public void TestParse()
        {
            SourceLine line = new SourceLine("myfile", 1, ".text \"He said, \",'\"',\"Hello, World!\"");
            line.Parse(s => s.Equals(".text"), s => false);
            Assert.AreEqual(".text", line.Instruction);
            Assert.AreEqual("\"He said, \",'\"',\"Hello, World!\"", line.Operand);
        }

        [TestMethod]
        public void TestParseComment()
        {
            SourceLine line = new SourceLine("myfile", 1, "mylabel .byte 1,2,3;,4,5");
            line.Parse(s => s.Equals(".byte"), s => s.Equals("mylabel"));
            Assert.AreEqual(line.Label, "mylabel");
            Assert.AreEqual(line.Instruction, ".byte");
            Assert.AreEqual(line.Operand, "1,2,3");

            // reset
            line.SourceString = "mylabel .byte 1,2,';',3";
            line.Label = line.Instruction = line.Operand = string.Empty;

            line.Parse(instr => instr.Equals(".byte"), lbl => lbl.Equals("mylabel"));
            Assert.AreEqual(line.Operand, "1,2,';',3");
        }

        [TestMethod]
        public void TestCsv()
        {
            SourceLine line = new SourceLine();
            line.Operand = "147, \"He said, \",'\"',\"Hello, World!\",'\"', $0d, ','";
            var csv = line.CommaSeparateOperand();

            Assert.IsTrue(csv.Count == 7);
            Assert.AreEqual(csv[0], "147");
            Assert.AreEqual(csv[1], "\"He said, \"");
            Assert.AreEqual(csv[2], "'\"'");
            Assert.AreEqual(csv[3], "\"Hello, World!\"");
            Assert.AreEqual(csv[4], "'\"'");
            Assert.AreEqual(csv[5], "$0d");
            Assert.AreEqual(csv[6], "','");
        }

        [TestMethod]
        public void TestFlags()
        {
            SourceLine line = new SourceLine();
            
            line.IsComment = true;
            // setting comment flag sets DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            // resetting DoNotAssemble has no effect if comment flag is true
            line.DoNotAssemble = false;
            Assert.AreEqual(true, line.DoNotAssemble);

            line.IsComment = false;
            // resetting comment flag should not reset DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            // reset DoNotAssemble
            line.DoNotAssemble = false;
            // and check!
            Assert.AreEqual(false, line.DoNotAssemble);

            line.IsDefinition = true;
            // setting definition flag sets DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            line.IsDefinition = false;
            // resetting definition flag should not reset DoNotAssemble flag
            Assert.AreEqual(true, line.DoNotAssemble);

            // reset all flags
            line.DoNotAssemble = false;
            // and check!
            Assert.AreEqual(false, line.IsComment);
            Assert.AreEqual(false, line.IsDefinition);
            Assert.AreEqual(false, line.DoNotAssemble);

            line.IsDefinition = true;
            line.IsComment = true;
            line.IsDefinition = false;
            // cannot reset definition flag if comment flag is set
            Assert.AreEqual(true, line.IsDefinition);
        }

        [TestMethod]
        public void TestScope()
        {
            SourceLine line = new SourceLine();
            line.Scope = "MYOUTERBLOCK.MYINNERBLOCK.MYNORMAL@";

            string scope = line.GetScope(false);
            Assert.AreEqual(scope, line.Scope);
            Assert.IsTrue(scope.Contains("MYNORMAL"));

            string modscope = line.GetScope(true);
            Assert.AreNotEqual(scope, modscope);
            Assert.IsFalse(modscope.Contains("MYNORMAL"));
        }
    }

    [TestClass]
    public class CompilationTest
    {
        [TestMethod]
        public void TestAddUninitialized()
        {
            Compilation output = new Compilation();
            output.AddUninitialized(4);
            Assert.AreEqual(4, output.GetPC());

            output.SetPC(0xC000);
            output.AddUninitialized(16);
            Assert.AreEqual(0xC010, output.GetPC());
        }

        [TestMethod]
        public void TestProgramStart()
        {
            Compilation output = new Compilation();

            output.SetPC(0x02);
            output.AddUninitialized(0x32);
            Assert.AreEqual(0x02 + 0x32, output.GetPC());

            // program start will not be set until we add
            // initialized data
            output.SetPC(0x0801);
            output.Add(0xffd220, 3);
            Assert.AreEqual(0x0801, output.ProgramStart);
            Assert.AreEqual(0x0804, output.GetPC());
        }

        [TestMethod]
        public void TestLittleEndian()
        {
            Compilation output = new Compilation(true);

            output.Add(0xffd2, 2);
            Assert.AreEqual(0x0002, output.GetPC());
            
            var bytes = output.GetCompilation();
            Assert.AreEqual(0xd2, bytes[0]);
            Assert.AreEqual(0xff, bytes[1]);
        }

        [TestMethod]
        public void TestAlignFillRepeat()
        {
            Compilation output = new Compilation();
            output.SetPC(0x4015);

            int foragoodtime = 0xffd220; // for a good time jsr $ffd2
            int align = output.Align(0x10, foragoodtime); // fill 11 bytes with 0xffd220...
            Assert.AreEqual(0x4020 - 0x4015, align);

            var bytes1 = output.GetCompilation();
            Assert.AreEqual(bytes1.Count, align);

            var expected1 = new byte[] { 0x20, 0xd2, 0xff, 
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2 };

            Assert.IsTrue(expected1.SequenceEqual(bytes1));

            output.Reset();
            output.Fill(7, foragoodtime, false);

            Assert.AreEqual(0x0007, output.GetPC());

            var bytes2 = output.GetCompilation();
            var expected2 = new byte[] { 0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff, 0x20 };

            Assert.IsTrue(expected2.SequenceEqual(bytes2));

            output.Reset();

            output.Fill(7, foragoodtime, true);

            Assert.AreEqual(0x0015, output.GetPC());

            var bytes3 = output.GetCompilation();
            var expected3 = new byte[] { 0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff,
                                         0x20, 0xd2, 0xff };

            Assert.IsTrue(expected3.SequenceEqual(bytes3));            
        }

        [TestMethod]
        public void TestLogicalRelocation()
        {
            Compilation output = new Compilation();

            output.SetPC(0xc000);
            Assert.AreEqual(0xc000, output.GetPC());
            Assert.AreEqual(0xc000, output.ProgramCounter);

            output.SetLogicalPC(0xf000);
            Assert.AreNotEqual(output.ProgramCounter, output.GetPC());

            output.Add(0xfeedface, 4);
            Assert.AreEqual(0xc000, output.ProgramStart);
            Assert.AreEqual(0xc004, output.ProgramCounter);
            Assert.AreEqual(0xf004, output.GetPC());

            output.SynchPC();
            Assert.AreEqual(output.ProgramCounter, output.GetPC());
        }
    }
}
