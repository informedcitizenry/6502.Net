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
    public class NUnitTestOpcode
    {
        [Test]
        public void TestFormatBuilder()
        {
            var builder = new FormatBuilder(@"^#(.+)$()", "#{2}", "${0:x2}", string.Empty, 2, 2, 1, 2);

            var fmt = builder.GetFormat("#$34");
            Assert.IsNotNull(fmt);
            Assert.AreEqual("#${0:x2}", fmt.FormatString);
            Assert.AreEqual("$34", fmt.Expression1);
            Assert.IsTrue(string.IsNullOrEmpty(fmt.Expression2));

            builder = new FormatBuilder(@"^\(\s*(.+),\s*x\s*\)$()", "({2},x)", "${0:x2}", string.Empty, 2, 2, 1, 2);

            fmt = builder.GetFormat("( ZP_VAR , x)");
            Assert.IsNotNull(fmt);
            Assert.AreEqual("(${0:x2},x)", fmt.FormatString);
            Assert.AreEqual("ZP_VAR ", fmt.Expression1);
            Assert.IsTrue(string.IsNullOrEmpty(fmt.Expression2));

            builder = new FormatBuilder(@"^(.+)$()", "{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, System.Text.RegularExpressions.RegexOptions.IgnoreCase, true);

            fmt = builder.GetFormat("($3000)");
            Assert.IsNotNull(fmt);
            Assert.AreEqual("(${0:x2})", fmt.FormatString);
            Assert.AreEqual("($3000)", fmt.Expression1);
            Assert.IsTrue(string.IsNullOrEmpty(fmt.Expression2));

            fmt = builder.GetFormat("$3000");
            Assert.IsNotNull(fmt);
            Assert.AreEqual("${0:x2}", fmt.FormatString);
            Assert.AreEqual("$3000", fmt.Expression1);
            Assert.IsTrue(string.IsNullOrEmpty(fmt.Expression2));
        }
    }
}
