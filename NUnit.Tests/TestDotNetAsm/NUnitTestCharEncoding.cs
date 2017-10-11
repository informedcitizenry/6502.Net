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
        public void TestEncoding()
        {
            AsmEncoding translator = new AsmEncoding();
            translator.SelectEncoding("petscii");
            translator.Map("az", 'A');
            translator.Map("AZ", 'a');

            var expected = ASCIIEncoding.ASCII.GetBytes("hELLO, World");
            var transbytes = translator.GetBytes("Hello, wORLD");

            Assert.AreEqual(expected, transbytes);

            translator.SelectEncoding("cbmscreen");
            translator.Map('@', '\0');
            translator.Map("az", Convert.ToChar(1));

            expected = expected.Select(delegate(byte b)
            {
                if (b == '@' || (b >= 'a' && b <= 'z'))
                    b -= 0x60;
                return b;
            }).ToArray();

            transbytes = translator.GetBytes("hELLO, World").ToArray();

            Assert.AreEqual(expected, transbytes);

            expected = ASCIIEncoding.ASCII.GetBytes("Hello, World!");

            translator.SelectEncoding("none");
            transbytes = translator.GetBytes("Hello, World!");
            Assert.AreEqual(expected, transbytes);
        }
    }
}
