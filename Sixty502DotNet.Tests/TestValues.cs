using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestValues
    {
        [TestMethod]
        public void Numeric()
        {
            var val = new Value(2);
            Assert.IsTrue(val.IsDefined);
            Assert.IsTrue(val.IsNumeric);
            Assert.AreEqual(2, val.ToInt());

            Assert.AreEqual(2, val.ToDouble());

            var val2 = new Value(2.0);
            Assert.IsTrue(val2.IsDefined);
            Assert.IsTrue(val2.IsNumeric);
            Assert.AreEqual(2, val2.ToDouble());
            Assert.AreEqual(2, val2.ToInt());

            Assert.AreEqual(val, val2);
        }

        [TestMethod]
        public void Char()
        {
            var chr = new Value("'A'");
            Assert.IsTrue(chr.IsDefined);
            Assert.IsTrue(chr.IsPrimitiveType);
            Assert.IsFalse(chr.IsString);
            Assert.AreEqual(TypeCode.Char, chr.DotNetType);
            Assert.AreEqual('A', chr.ToChar());

        }

        [TestMethod]
        public void String()
        {
            var str = new Value("\"A\"");
            Assert.IsTrue(str.IsDefined);
            Assert.IsTrue(str.IsPrimitiveType);
            Assert.IsTrue(str.IsString);
            Assert.AreEqual(1, str.ElementCount);
            Assert.AreEqual("A", str.ToString(true));
            Assert.AreEqual("\"A\"", str.ToString());

            var str2 = new Value("\"A\"");
            Assert.AreEqual("\"A\"", str.ToString());
            Assert.AreEqual(str, str2);
        }

        [TestMethod]
        public void ArrayValue()
        {
            var vals = new List<Value>
            { new Value(1), new Value(2), new Value(3)};
            var arrayVal = new ArrayValue(vals);
            Assert.IsTrue(arrayVal.IsDefined);
            Assert.AreEqual(TypeCode.Object, arrayVal.DotNetType);
            Assert.AreEqual(TypeCode.Int32, arrayVal.ElementType);
            Assert.AreEqual(3, arrayVal.ElementCount);
            Assert.IsTrue(arrayVal.ElementsIntegral);
            Assert.IsTrue(arrayVal.ElementsNumeric);
            Assert.IsTrue(arrayVal.ElementsSameType);
            Assert.IsTrue(arrayVal.ElementsDistinct);
            Assert.AreEqual(1, arrayVal[0].ToInt());

            arrayVal.Add(new Value(Math.Pow(2.0, 2.0)));
            Assert.IsTrue(arrayVal[3].IsDefined);
            Assert.AreEqual(4, arrayVal[3].ToDouble());
            Assert.IsFalse(arrayVal[3].IsIntegral);
            Assert.IsFalse(arrayVal.ElementsIntegral);
            Assert.IsTrue(arrayVal.ElementsNumeric);
            Assert.IsTrue(arrayVal.ElementsSameType);
            Assert.AreEqual(TypeCode.Object, arrayVal.DotNetType);
            Assert.AreEqual(TypeCode.Double, arrayVal.ElementType);

            arrayVal.Add(new Value(false));
            Assert.IsFalse(arrayVal.ElementsIntegral);
            Assert.IsFalse(arrayVal.ElementsNumeric);
            Assert.IsFalse(arrayVal.ElementsSameType);
            Assert.AreEqual(TypeCode.Object, arrayVal.DotNetType);
            Assert.AreEqual(TypeCode.Object, arrayVal.ElementType);

            arrayVal = new ArrayValue(vals);
            Assert.AreEqual(TypeCode.Object, arrayVal.DotNetType);
            Assert.AreEqual(TypeCode.Int32, arrayVal.ElementType);
            Assert.AreEqual(3, arrayVal.ElementCount);
            Assert.IsTrue(arrayVal.ElementsIntegral);
            Assert.IsTrue(arrayVal.ElementsNumeric);
            Assert.IsTrue(arrayVal.ElementsSameType);
            Assert.IsTrue(arrayVal.ElementsDistinct);
            Assert.AreEqual(1, arrayVal[0].ToInt());

            var arrayVal2 = new ArrayValue
            {
                new Value(4),
                new Value(5.0),
                new Value(6)
            };

            Assert.IsFalse(arrayVal2.ElementsIntegral);
            Assert.IsTrue(arrayVal2.ElementsNumeric);
            Assert.IsTrue(arrayVal2.ElementsSameType);
            Assert.AreEqual(TypeCode.Object, arrayVal2.DotNetType);
            Assert.AreEqual(TypeCode.Double, arrayVal2.ElementType);

            var arrayVal3 = new ArrayValue
            {
                arrayVal
            };
            Assert.AreEqual(TypeCode.Object, arrayVal3.DotNetType);
            Assert.AreEqual(TypeCode.Object, arrayVal3.ElementType);
            Assert.IsTrue(arrayVal3.ElementsIntegral);
            Assert.IsTrue(arrayVal3.ElementsNumeric);
            Assert.IsTrue(arrayVal3.ElementsSameType);

            arrayVal3.Add(arrayVal2);
            Assert.AreEqual(TypeCode.Object, arrayVal3.DotNetType);
            Assert.AreEqual(TypeCode.Object, arrayVal3.ElementType);
            Assert.IsFalse(arrayVal3.ElementsIntegral);
            Assert.IsTrue(arrayVal3.ElementsNumeric);
            Assert.IsTrue(arrayVal3.ElementsSameType);

            var arrayVal4 = new ArrayValue
            {
                new Value(true)
            };
            Assert.AreEqual(TypeCode.Object, arrayVal4.DotNetType);
            Assert.AreEqual(TypeCode.Boolean, arrayVal4.ElementType);
            Assert.IsFalse(arrayVal4.ElementsIntegral);
            Assert.IsFalse(arrayVal4.ElementsNumeric);
            Assert.IsTrue(arrayVal.ElementsSameType);

            arrayVal3.Add(arrayVal4);
            Assert.AreEqual(TypeCode.Object, arrayVal3.DotNetType);
            Assert.AreEqual(TypeCode.Object, arrayVal3.ElementType);
            Assert.IsFalse(arrayVal3.ElementsIntegral);
            Assert.IsFalse(arrayVal3.ElementsNumeric);
            Assert.IsFalse(arrayVal3.ElementsSameType);

            var stringArray = new ArrayValue
            {
                new Value("\"1\""), new Value("\"2\""), new Value("\"3\"")
            };
            Assert.AreEqual(TypeCode.Object, stringArray.DotNetType);
            Assert.AreEqual(TypeCode.String, stringArray.ElementType);
            Assert.IsFalse(stringArray.ElementsIntegral);
            Assert.IsFalse(stringArray.ElementsNumeric);
            Assert.IsTrue(stringArray.ElementsSameType);
            Assert.IsTrue(stringArray.ElementsDistinct);

            var stringArray2 = new ArrayValue
            {
                new Value("\"4\""), new Value("\"4\"")
            };
            Assert.AreEqual(TypeCode.Object, stringArray2.DotNetType);
            Assert.AreEqual(TypeCode.String, stringArray2.ElementType);
            Assert.IsFalse(stringArray2.ElementsIntegral);
            Assert.IsFalse(stringArray2.ElementsNumeric);
            Assert.IsTrue(stringArray2.ElementsSameType);
            Assert.IsFalse(stringArray2.ElementsDistinct);

            var stringArrayArray = new ArrayValue
            {
                stringArray, stringArray2
            };
            Assert.AreEqual(TypeCode.Object, stringArrayArray.DotNetType);
            Assert.AreEqual(TypeCode.Object, stringArrayArray.ElementType);
            Assert.IsFalse(stringArrayArray.ElementsIntegral);
            Assert.IsFalse(stringArrayArray.ElementsNumeric);
            Assert.IsTrue(stringArrayArray.ElementsSameType);

            stringArrayArray.Add(arrayVal);
            Assert.IsFalse(stringArrayArray.ElementsSameType);
        }

        [TestMethod]
        public void DictionaryValue()
        {
            var keys = new ArrayValue
            {
                new Value("\"key1\""), new Value("\"key2\"")
            };
            var vals = new ArrayValue
            {
                new Value(1), new Value(2.3)
            };
            Assert.IsTrue(keys.All(k => Sixty502DotNet.DictionaryValue.CanBeKey(k)));
            Assert.IsTrue(keys.ElementsSameType);
            Assert.IsTrue(keys.ElementsDistinct);

            Assert.IsTrue(vals.ElementsSameType);

            var dictionary = new DictionaryValue(keys, vals);
            Assert.IsTrue(dictionary.IsValid);
            Assert.AreEqual(TypeCode.String, dictionary.KeyType);
            Assert.AreEqual(TypeCode.Double, dictionary.ElementType);

            dictionary.Add(new Value(1), new Value("\"3\""));
            Assert.IsFalse(dictionary.IsValid);
        }
    }
}
