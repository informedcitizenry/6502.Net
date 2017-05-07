﻿using Asm6502.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnitTest6502.Net
{
    [TestFixture]
    public class GeneralTest
    {
        [Test]
        public void TestRegex()
        {
            string expression = "MYVAR1 + 2 + MYVAR2";
            string pattern = @"[a-zA-Z][a-zA-Z0-9_]*";

            expression = Regex.Replace(expression, pattern, m =>
                {
                    if (m.Value.Equals("MYVAR1"))
                        return "1";
                    return "2";
                });

            Assert.AreEqual("1 + 2 + 2", expression);
        }

        [Test]
        public void TestStringBuilderExtensions()
        {
            StringBuilder sb = new StringBuilder("     hello     ");
            Assert.AreEqual("     hello     ", sb.ToString());

            sb.TrimStart();
            Assert.AreEqual("hello     ", sb.ToString());

            sb.TrimEnd();
            Assert.AreEqual("hello", sb.ToString());

            sb = new StringBuilder("     hello     ");
            sb.Trim();
            Assert.AreEqual("hello", sb.ToString());

            sb.Length = 10;
            sb.Replace('\0', ' ');
            Assert.AreEqual("hello     ", sb.ToString());
        }


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
