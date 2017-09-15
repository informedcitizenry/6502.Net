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
    public class NUnitTestCharEncoding
    {
        [Test]
        public void TestMethod()
        {
            EncodingTranslator translator = new EncodingTranslator();
            translator.DefineEncoding("petscii");
            translator.AddEncodingClass("petscii", "az", "A");
            translator.AddEncodingClass("petscii", "AZ", "a");
        }
    }
}
