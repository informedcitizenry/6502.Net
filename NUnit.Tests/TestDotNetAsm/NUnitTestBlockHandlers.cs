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
    public class NUnitTestBlockHandlers
    {
        private TestController _testController;

        public NUnitTestBlockHandlers()
        {
            _testController = new TestController();
        }

        [Test]
        public void TestMethod()
        {
            List<SourceLine> source = new List<SourceLine>();
            List<SourceLine> processed = new List<SourceLine>();
            
            ConditionBlockHandler condHandler = new ConditionBlockHandler(_testController, processed);

            source.Add(new SourceLine { Instruction = ".if", Operand = "3 < 4" });
            source.Add(new SourceLine { Instruction = "lda", Operand = "#$03" });
            source.Add(new SourceLine { Instruction = ".else" });
        }
    }
}
