using DotNetAsm;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Tests.TestDotNetAsm
{
    [TestFixture]
    public abstract class NUnitAsmTestBase
    {
        protected NUnitAsmTestBase()
        {
            Assembler.Initialize();

            Assembler.Evaluator.DefineParser((arg) =>
                Assembler.Symbols.TranslateExpressionSymbols(new SourceLine(), arg, string.Empty, false));
        }

        protected ILineAssembler LineAssembler { get; set; }

        protected void TestInstruction(SourceLine line, ILineAssembler asm, int pc, int expectedsize, IEnumerable<byte> expected, bool positive)
        {
            var size = asm.GetInstructionSize(line);
            asm.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(Assembler.Log.HasErrors);
                Assert.AreEqual(pc, Assembler.Output.LogicalPC);

                if (expected != null)
                    Assert.IsTrue(Assembler.Output.GetCompilation().SequenceEqual(expected));
                else
                    Assert.IsTrue(Assembler.Output.GetCompilation().Count == 0);
                Assert.AreEqual(expectedsize, size);
            }
            else
            {
                Assert.IsTrue(Assembler.Log.HasErrors);

            }
            ResetAssembler();
        }

        protected void TestInstruction(SourceLine line, ILineAssembler asm, int pc, byte[] expected, string disasm, bool positive)
        {
            asm.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(Assembler.Log.HasErrors);
                Assert.AreEqual(pc, Assembler.Output.LogicalPC);
                Assert.IsTrue(Assembler.Output.GetCompilation().SequenceEqual(expected));
                Assert.AreEqual(disasm, line.Disassembly);
                Assert.AreEqual(expected.Count(), line.Assembly.Count);
            }
            else
            {
                Assert.IsTrue(Assembler.Log.HasErrors);
            }
            Assembler.Output.Reset();
            Assembler.Log.ClearErrors();
        }

        protected void TestInstruction(SourceLine line, int pc, byte[] expected, string disasm, bool positive)
        {
            TestInstruction(line, LineAssembler, pc, expected, disasm, positive);
        }

        protected void TestInstruction(SourceLine line, int pc, int expectedsize, IEnumerable<byte> expected, bool positive)
        {
            TestInstruction(line, LineAssembler, pc, expectedsize, expected, positive);
        }

        protected void TestInstruction(SourceLine line, ILineAssembler assembler, int pc, byte[] expected, string disasm)
        {
            TestInstruction(line, assembler, pc, expected, disasm, true);
        }

        protected void TestInstruction(SourceLine line, ILineAssembler assembler, int pc, int expectedsize, byte[] expected)
        {
            TestInstruction(line, assembler, pc, expectedsize, expected, true);
        }

        protected void TestInstruction(SourceLine line, int pc, byte[] expected, string disasm)
        {
            TestInstruction(line, LineAssembler, pc, expected, disasm, true);
        }

        protected void TestInstruction(SourceLine line, int pc, int expectedsize, IEnumerable<byte> expected)
        {
            TestInstruction(line, LineAssembler, pc, expectedsize, expected, true);
        }

        void ResetAssembler()
        {
            if (Assembler.Output.Transforms.Count > 0)
                Assembler.Output.Transforms.Pop();
            Assembler.Output.Reset();
            Assembler.Log.ClearErrors();
        }

        protected void TestForFailure(SourceLine line)
        {
            TestInstruction(line, 0, 0, null, false);
        }

        public void TestForFailure<Texc>(SourceLine line) where Texc : System.Exception
        {
            Assert.Throws<Texc>(() => TestForFailure(line));
            ResetAssembler();
        }
    }
}
