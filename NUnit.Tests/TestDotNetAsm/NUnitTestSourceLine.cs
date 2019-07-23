using DotNetAsm;
using NUnit.Framework;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestSourceLine
    {
        [Test]
        public void TestCsv()
        {
            var line = new SourceLine();
            line.Operand = "147, \"He said, \",'\"',\"Hello, World!\",'\"', $0d, ','";
            System.Collections.Generic.List<string> csv = line.Operand.CommaSeparate();

            Assert.IsTrue(csv.Count == 7);
            Assert.AreEqual(csv[0], "147");
            Assert.AreEqual(csv[1], "\"He said, \"");
            Assert.AreEqual(csv[2], "'\"'");
            Assert.AreEqual(csv[3], "\"Hello, World!\"");
            Assert.AreEqual(csv[4], "'\"'");
            Assert.AreEqual(csv[5], "$0d");
            Assert.AreEqual(csv[6], "','");
        }

        [Test]
        public void TestFlags()
        {
            var line = new SourceLine();

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

            // reset all flags
            line.DoNotAssemble = false;
            // and check!
            Assert.AreEqual(false, line.IsComment);
            Assert.AreEqual(false, line.DoNotAssemble);
        }

        [Test]
        public void TestParse()
        {
            var line = new SourceLine
            {
                SourceString = "label       lda #$01 ;comment"
            };

            foreach (var l in line.Parse(s => s.Equals("lda"))) { }
            Assert.AreEqual("label", line.Label);
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$01", line.Operand);

            line.Label = line.Instruction = line.Operand = string.Empty;
            line.SourceString = "       lda #$01 ;comment";
            foreach (var l in line.Parse(s => s.Equals("lda"))) { }
            Assert.IsEmpty(line.Label);
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$01", line.Operand);

            line.Label = line.Instruction = line.Operand = string.Empty;
            line.SourceString = "       inx;comment";
            foreach (var l in line.Parse(s => s.Equals("inx"))) { }
            Assert.IsEmpty(line.Label);
            Assert.AreEqual("inx", line.Instruction);
            Assert.IsEmpty(line.Operand);

            line.Label = line.Instruction = line.Operand = string.Empty;
            line.SourceString = "label: ;comment";
            foreach (var l in line.Parse(s => s.Equals("inx"))) { }
            Assert.AreEqual("label", line.Label);
            Assert.IsEmpty(line.Instruction);
            Assert.IsEmpty(line.Operand);

            line.Label = line.Instruction = line.Operand = string.Empty;
            line.SourceString = "label      lda #$00:inx";
            var yielded = new System.Collections.Generic.List<SourceLine>();
            foreach (var l in line.Parse(s => s.Equals("lda")))
                yielded.Add(l);
            Assert.AreEqual("label", line.Label);
            Assert.AreEqual("lda", line.Instruction);
            Assert.AreEqual("#$00", line.Operand);
            Assert.That(yielded.Count == 1);
            Assert.IsEmpty(yielded[0].Label);
            Assert.IsEmpty(yielded[0].Instruction);
            Assert.IsEmpty(yielded[0].Operand);
            Assert.AreEqual("\tinx", yielded[0].SourceString);
           
        }
    }
}
