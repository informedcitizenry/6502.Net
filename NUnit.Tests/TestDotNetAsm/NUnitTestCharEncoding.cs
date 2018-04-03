using DotNetAsm;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Tests.TestDotNetAsm
{
    public class NUnitTestCharEncoding
    {
        [Test]
        public void TestEncoding()
        {
            var translator = new AsmEncoding();
            translator.SelectEncoding("petscii");
            translator.Map("az", 'A');
            translator.Map("AZ", 'a');

            var expected = Encoding.ASCII.GetBytes("hELLO, World");
            var transbytes = translator.GetBytes("Hello, wORLD");

            Assert.AreEqual(expected, transbytes);

            translator.SelectEncoding("cbmscreen");
            translator.Map('@', '\0');
            translator.Map("az", Convert.ToChar(1));

            expected = expected.Select(delegate (byte b)
            {
                if (b == '@' || (b >= 'a' && b <= 'z'))
                    b -= 0x60;
                return b;
            }).ToArray();

            transbytes = translator.GetBytes("hELLO, World").ToArray();

            Assert.AreEqual(expected, transbytes);

            expected = Encoding.ASCII.GetBytes("Hello, World!");

            translator.SelectEncoding("none");
            transbytes = translator.GetBytes("Hello, World!");
            Assert.AreEqual(expected, transbytes);

            expected = Encoding.ASCII.GetBytes("\t\t\"'");
            expected = expected.Select(delegate (byte b)
            {
                if (b == '\t') return (byte)14;
                return b;
            }).ToArray();

            translator.SelectEncoding("escapeclub");
            translator.Map('\t', 14);
            transbytes = translator.GetBytes("\t\t\"'");
            Assert.AreEqual(expected, transbytes);

            expected = new byte[] { 0x9f };
            translator.Map('\u21d4', 0x9f);
            transbytes = translator.GetBytes("\u21d4");
            Assert.AreEqual(expected, transbytes);
        }

        [Test]
        public void TestEncodingGetChar()
        {
            var encoding = new AsmEncoding();

            encoding.SelectEncoding("test");
            string teststring = "τϵστ";
            var testbytes = encoding.GetBytes(teststring);
            var expectedbytes = Encoding.UTF8.GetBytes(teststring);
            Assert.AreEqual(expectedbytes, testbytes);

            var testchars = encoding.GetChars(testbytes);
            var expectedchars = Encoding.UTF8.GetChars(expectedbytes);
            Assert.AreEqual(expectedchars, testchars);

            var testcharcount = encoding.GetChars(testbytes, 0, testbytes.Length, testchars, 0);
            var expectedcharcount = Encoding.UTF8.GetChars(expectedbytes, 0, expectedbytes.Length, expectedchars, 0);
            Assert.AreEqual(expectedcharcount, testcharcount);

            encoding.Map('τ', 0xff);
            expectedbytes = new byte[] { 0xff, 207, 181, 207, 131, 0xff };
            testbytes = encoding.GetBytes(teststring);

            Assert.AreEqual(expectedbytes, testbytes);
            testchars = encoding.GetChars(testbytes);
            Assert.AreEqual(expectedchars, testchars);

            testcharcount = encoding.GetChars(testbytes, 0, testbytes.Length, testchars, 0);
            Assert.AreEqual(expectedcharcount, testcharcount);

            encoding.Unmap('τ');
            expectedbytes = Encoding.UTF8.GetBytes(teststring);
            testbytes = encoding.GetBytes(teststring);
            Assert.AreEqual(expectedbytes, testbytes);

            testchars = encoding.GetChars(testbytes);
            Assert.AreEqual(expectedchars, testchars);

            testcharcount = encoding.GetChars(testbytes, 0, testbytes.Length, testchars, 0);
            Assert.AreEqual(expectedcharcount, testcharcount);
        }
    }
}
