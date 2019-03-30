using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestSourceLine
    {
        [Test]
        public void TestCsv()
        {
            var line = new SourceLine();
            line.Operand = "147, \"He said, \",'\"',\"Hello, World!\",'\"', $0d, ','";
            var csv = line.Operand.CommaSeparate();

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
    }
}
