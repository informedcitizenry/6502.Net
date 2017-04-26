using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest6502.Net
{
    [TestFixture]
    public class GeneralTest
    {
        [Test]
        public void TestcaseSensitivity()
        {
            StringComparison ignore = StringComparison.CurrentCultureIgnoreCase;
            StringComparer ignoreC = StringComparer.CurrentCultureIgnoreCase;
            StringComparison casesensitive = StringComparison.CurrentCulture;
            StringComparer casesensitiveC = StringComparer.CurrentCulture;
            System.Collections.Generic.HashSet<string> ignoreHash = new System.Collections.Generic.HashSet<string>(ignoreC);
            System.Collections.Generic.HashSet<string> csHash = new System.Collections.Generic.HashSet<string>(casesensitiveC);

            string lstring = "bob";
            string Mstring = "Bob";

            ignoreHash.Add(lstring);
            csHash.Add(Mstring);
           
            Assert.IsTrue(ignoreHash.Contains(lstring.ToUpper())); // BOB
            Assert.IsTrue(csHash.Contains(Mstring));
            Assert.IsFalse(csHash.Contains(Mstring.ToUpper())); // BOB

            Assert.IsTrue(lstring.Equals(Mstring, ignore));
            Assert.IsFalse(Mstring.Equals(lstring, casesensitive));
        }
    }
}
