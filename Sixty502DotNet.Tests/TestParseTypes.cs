using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sixty502DotNet;

namespace Sixty502DotNet.Tests
{
    [TestClass]
    public class TestParseTypes : TestBase
    {
        [TestMethod]
        public void PrimaryInteger()
        {
            var result = ParseExpression("3");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(3, result.ToInt());

            result = ParseExpression("1_022_727");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(1_022_727, result.ToInt());
        }

        [TestMethod]
        public void PrimaryDouble()
        {
            var result = ParseExpression("3.1415");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsNumeric);
            Assert.IsFalse(result.IsIntegral);
            Assert.AreEqual(3.1415, result.ToDouble());
        }

        [TestMethod]
        public void PrimaryChar()
        {
            var result = ParseExpression("'A'");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsFalse(result.IsNumeric);
            Assert.IsFalse(result.IsString);
            Assert.AreEqual(TypeCode.Char, result.DotNetType);
            Assert.AreEqual('A', result.ToChar());

            result = ParseExpression("'\u0041'");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsFalse(result.IsNumeric);
            Assert.IsFalse(result.IsString);
            Assert.AreEqual(TypeCode.Char, result.DotNetType);
            Assert.AreEqual('A', result.ToChar());
        }

        [TestMethod]
        public void PrimaryBool()
        {
            var result = ParseExpression("true");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsFalse(result.IsNumeric);
            Assert.IsFalse(result.IsString);
            Assert.AreEqual(TypeCode.Boolean, result.DotNetType);
            Assert.IsTrue(result.ToBool());

            result = ParseExpression("false");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsFalse(result.IsNumeric);
            Assert.IsFalse(result.IsString);
            Assert.AreEqual(TypeCode.Boolean, result.DotNetType);
            Assert.IsFalse(result.ToBool());
        }

        [TestMethod]
        public void PrimaryString()
        {
            var result = ParseExpression("\"he said, \\\"hello world\\\" to me\"");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("he said, \"hello world\" to me", result.ToString(true));

            result = ParseExpression("\"hi \\uD83D\\uDE00\"");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("hi 😀", result.ToString(true));

            result = ParseExpression("\"hi \\U0001F600\"");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("hi 😀", result.ToString(true));
        }

        [TestMethod]
        public void PrimaryNonBase10()
        {
            var result = ParseExpression("$ffd2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0xffd2, result.ToInt());

            result = ParseExpression("0xFFD2");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0xffd2, result.ToInt());

            result = ParseExpression("0101");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(65, result.ToInt());

            result = ParseExpression("0o101");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(65, result.ToInt());

            result = ParseExpression("%0101");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0b0101, result.ToInt());

            result = ParseExpression("0b01_01");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0b01_01, result.ToInt());

            result = ParseExpression("%.#.#");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(0b0101, result.ToInt());
        }

        [TestMethod]
        public void Array()
        {
            var array = new ArrayValue();
            array.Add(new Value(1));
            array.Add(new Value(2));
            array.Add(new Value(3));
            Assert.AreEqual(TypeCode.Object, array.DotNetType);
            Assert.IsTrue(array.ElementsSameType);
            Assert.IsTrue(array.ElementsIntegral);
            Assert.AreEqual(TypeCode.Int32, array.ElementType);

            array.Add(new Value(3.14));
            Assert.IsTrue(array.ElementsSameType);
            Assert.AreEqual(TypeCode.Double, array.ElementType);

            array.Add(new Value("\"hello world\""));
            Assert.IsFalse(array.ElementsSameType);

            var result = ParseExpression("[1, 2, 3, 4 + 5]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsFalse(result.IsPrimitiveType);
            Assert.IsInstanceOfType(result, typeof(ArrayValue));

            array = result as ArrayValue;
            Assert.AreEqual(4, array.ElementCount);
            Assert.AreEqual(1, array[0].ToInt());
            Assert.AreEqual(2, array[1].ToInt());
            Assert.AreEqual(3, array[2].ToInt());
            Assert.AreEqual(4 + 5, array[3].ToInt());

            result = ParseExpression("[1, \"1\"]");
            Assert.IsTrue(Services.Log.HasErrors);
            Assert.IsFalse(result.IsDefined);
        }

        [TestMethod]
        public void ArrayOfArrays()
        {
            var array = new ArrayValue();
            var arr1 = new ArrayValue
            {
                new Value(1),
                new Value(2),
                new Value(3)
            };

            Assert.IsTrue(arr1.ElementsSameType);
            Assert.AreEqual(TypeCode.Int32, arr1.ElementType);

            var arr2 = new ArrayValue
            {
                new Value(3),
                new Value(4),
                new Value(5)
            };

            Assert.IsTrue(arr2.ElementsSameType);
            Assert.AreEqual(TypeCode.Int32, arr2.ElementType);

            array.Add(arr1);
            Assert.AreEqual(TypeCode.Object, array.DotNetType);
            Assert.AreEqual(TypeCode.Object, array.ElementType);

            array.Add(arr2);
            Assert.AreEqual(TypeCode.Object, array.DotNetType);
            Assert.AreEqual(TypeCode.Object, array.ElementType);
            Assert.IsTrue(array.ElementsSameType);

            array.Clear();

            arr2.Clear();
            arr2.Add(new Value("\"hello\""));
            arr2.Add(new Value("\"world\""));
            Assert.IsTrue(arr2.ElementsSameType);
            Assert.AreEqual(TypeCode.String, arr2.ElementType);

            array.Add(arr1);
            Assert.IsTrue(array.ElementsSameType);

            array.Add(arr2);
            Assert.IsFalse(array.ElementsSameType);

            var result = ParseExpression(
@"[
 [ 1,2,3,4],
 [ 5,6,7,8],
 [ 9,10,11,12]
]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsFalse(result.IsPrimitiveType);
            Assert.IsInstanceOfType(result, typeof(ArrayValue));

            array = result as ArrayValue;
            Assert.AreEqual(3, array.ElementCount);

            arr1 = array[0] as ArrayValue;
            Assert.IsNotNull(arr1);
            Assert.IsTrue(arr1.IsDefined);
            Assert.IsFalse(arr1.IsPrimitiveType);

            Assert.AreEqual(4, arr1.ElementCount);
            Assert.AreEqual(1, arr1[0].ToInt());
            Assert.AreEqual(2, arr1[1].ToInt());
            Assert.AreEqual(3, arr1[2].ToInt());
            Assert.AreEqual(4, arr1[3].ToInt());

            _ = ParseExpression(
@"[
 [ 1,2,3,4],
 [ ""hello"",""world""]
]");
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Dictionary()
        {
            var result = ParseExpression("{\"key1\":0, \"key2\":1}");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsFalse(result.IsPrimitiveType);
            Assert.IsInstanceOfType(result, typeof(DictionaryValue));

            var dict = result as DictionaryValue;
            Assert.AreEqual(2, dict.ElementCount);
            Assert.AreEqual(TypeCode.String, dict.KeyType);
            Assert.AreEqual(TypeCode.Int32, dict.ElementType);
            Assert.IsTrue(dict.TryGetElement(new Value("\"key1\""), out var key1val));
            Assert.AreEqual(0, key1val.ToInt());
            Assert.IsTrue(dict.TryGetElement(new Value("\"key2\""), out var key2val));
            Assert.AreEqual(1, key2val.ToInt());

            _ = ParseExpression("{\"key1\":0, 1:1}");
            Assert.IsTrue(Services.Log.HasErrors);

            _ = ParseExpression("{\"key1\":0, \"key1\":1}");
            Assert.IsTrue(Services.Log.HasErrors);

            _ = ParseExpression("{\"key1\":0, [1,2]:3}");
            Assert.IsTrue(Services.Log.HasErrors);

        }

        [TestMethod]
        public void ArrayIndex()
        {
            var result = ParseExpression("[1,2,3,4,5][0]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(1, result.ToInt());

            result = ParseExpression("[1,2,3,4][-1]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(4, result.ToInt());

            _ = ParseExpression("[1,2,3,4,5][6]");
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void ArrayRange()
        {
            var result = ParseExpression("[1,2,3,4,5][1..3]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsInstanceOfType(result, typeof(ArrayValue));

            var arra = result as ArrayValue;
            Assert.AreEqual(3, arra.Count);
            Assert.AreEqual(2, arra[0].ToInt());
            Assert.AreEqual(3, arra[1].ToInt());
            Assert.AreEqual(4, arra[2].ToInt());

            result = ParseExpression("[1,2,3,4,5][-3..-1]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsInstanceOfType(result, typeof(ArrayValue));

            arra = result as ArrayValue;
            Assert.AreEqual(3, arra.Count);
            Assert.AreEqual(3, arra[0].ToInt());
            Assert.AreEqual(4, arra[1].ToInt());
            Assert.AreEqual(5, arra[2].ToInt());
        }

        [TestMethod]
        public void StringIndex()
        {
            var result = ParseExpression("\"Hello, World\"[5]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsPrimitiveType);
            Assert.AreEqual(TypeCode.Char, result.DotNetType);
            Assert.AreEqual(',', result.ToChar());

            _ = ParseExpression("\"Hello, World\"[12]");
            Assert.IsTrue(Services.Log.HasErrors);
        }

        [TestMethod]
        public void Substring()
        {
            var result = ParseExpression("\"Hello, World\"[..4]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("Hello", result.ToString(true));

            result = ParseExpression("\"Hello, World\"[-5..-1]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsString);
            Assert.AreEqual("World", result.ToString(true));
        }

        [TestMethod]
        public void DictionaryElement()
        {
            var result = ParseExpression("{\"key1\":0, \"key2\":1}[\"key2\"]");
            Assert.IsFalse(Services.Log.HasErrors);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDefined);
            Assert.IsTrue(result.IsIntegral);
            Assert.AreEqual(1, result.ToInt());

            _ = ParseExpression("{\"key1\":0, \"key2\":1}[\"key3\"]");
            Assert.IsTrue(Services.Log.HasErrors);
        }
    }
}
