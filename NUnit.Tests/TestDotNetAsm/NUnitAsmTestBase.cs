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
    public abstract class NUnitAsmTestBase
    {
        protected IAssemblyController Controller { get; set; }

        protected ILineAssembler LineAssembler { get; set; }

        protected void TestInstruction(SourceLine line, ILineAssembler asm, int pc, int expectedsize, IEnumerable<byte> expected, bool positive)
        {
            var size = asm.GetInstructionSize(line);
            asm.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(Controller.Log.HasErrors);
                Assert.AreEqual(pc, Controller.Output.LogicalPC);

                if (expected != null)
                    Assert.IsTrue(Controller.Output.GetCompilation().SequenceEqual(expected));
                else
                    Assert.IsTrue(Controller.Output.GetCompilation().Count == 0);
                Assert.AreEqual(expectedsize, size);
            }
            else
            {
                Assert.IsTrue(Controller.Log.HasErrors);

            }
            ResetController();
        }

        protected void TestInstruction(SourceLine line, ILineAssembler asm, int pc, byte[] expected, string disasm, bool positive)
        {
            asm.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(Controller.Log.HasErrors);
                Assert.AreEqual(pc, Controller.Output.LogicalPC);
                Assert.IsTrue(Controller.Output.GetCompilation().SequenceEqual(expected));
                Assert.AreEqual(disasm, line.Disassembly);
                Assert.AreEqual(expected.Count(), LineAssembler.GetInstructionSize(line));
            }
            else
            {
                Assert.IsTrue(Controller.Log.HasErrors);
            }
            Controller.Output.Reset();
            Controller.Log.ClearErrors();
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

        void ResetController()
        {
            if (Controller.Output.Transforms.Count > 0)
                Controller.Output.Transforms.Pop();
            Controller.Output.Reset();
            Controller.Log.ClearErrors();
        }

        protected void TestForFailure(SourceLine line)
        {
            TestInstruction(line, 0, 0, null, false);
        }

        protected void TestForFailure<Texc>(SourceLine line) where Texc : System.Exception
        {
            try { Assert.Throws<Texc>(() => TestInstruction(line, 0, 0, null, false)); }
            catch { }
            finally { ResetController(); }
        }
    }
}
