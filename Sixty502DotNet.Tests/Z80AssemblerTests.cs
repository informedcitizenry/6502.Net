using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class Z80AssemblerTests
    {
        [TestMethod]
        public void Test()
        {
            var sut = new Z80Assembler();
            var input = ";\r\n;\r\nCALL    MAIN \r\n            HALT     \r\nMAIN:                \r\n            LD      a,1 \r\n            LD      b,2 \r\n            ADD     a,b \r\n            RET      \r\n";
            var bytes = sut.Assemble(input);
        }
    }
}