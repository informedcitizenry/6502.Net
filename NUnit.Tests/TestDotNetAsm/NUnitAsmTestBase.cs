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

        protected void TestInstruction(SourceLine line, int pc, byte[] expected, string disasm, bool positive = true)
        {
            LineAssembler.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(Controller.Log.HasErrors);
                Assert.AreEqual(pc, Controller.Output.GetPC());
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

        protected void TestInstruction(SourceLine line, int pc, int expectedsize, IEnumerable<byte> expected, bool positive = true)
        {
            int size = LineAssembler.GetInstructionSize(line);
            LineAssembler.AssembleLine(line);
            if (positive)
            {
                Assert.IsFalse(Controller.Log.HasErrors);
                Assert.AreEqual(pc, Controller.Output.GetPC());

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
            Controller.Output.Reset();
            Controller.Log.ClearErrors();
        }

        protected void TestForFailure(SourceLine line)
        {
            TestInstruction(line, 0, 0, null, false);
        }
    }
}
