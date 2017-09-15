//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetAsm
{
    public class CharEncoding
    {
        public string Class { get; set; }
        public UInt32 Translation { get; set; }

        public CharEncoding()
            : this(string.Empty, 0xFF)
        {

        }

        public CharEncoding(string classname, UInt32 translation)
        {
            Class = classname;
            Translation = translation;
        }

        public override string ToString()
        {
            string trans = Convert.ToChar(Translation).ToString();
            if (Translation > 127)
                trans = string.Format("\\u{0:X}", Translation);
            return string.Format("{0}={1}", Class, trans);
        }
    }

    public class EncodingTranslator
    {
        private Dictionary<string, List<CharEncoding> > _encodings;

        public EncodingTranslator()
        {
            _encodings = new Dictionary<string, List<CharEncoding> >();
        }

        public void AddEncodingClass(string encodingName, string className, UInt32 translation)
        {
            _encodings[encodingName].Add(new CharEncoding(className, translation));
        }

        public void AddEncodingClass(string encodingName, string classRange, string firstTranslation)
        {
            var rangeChars = classRange.ToCharArray();
            var transChars = firstTranslation.ToCharArray();
            if (rangeChars.Length != 2 || transChars.Length != 1)
            {
                // todo: invalid
                return;
            }
            var firstChar = Convert.ToInt32(rangeChars.First());
            var lastChar = Convert.ToInt32(rangeChars.Last());
            var transChar = Convert.ToInt32(transChars.First());
            if (firstChar > lastChar)
            {
                // todo: invalid
                return;
            }
            int displace = transChar - firstChar;
            
            var encoding = _encodings[encodingName];

            while (firstChar <= lastChar)
            {
                var charClass = Convert.ToChar(firstChar);
                var transClass = Convert.ToUInt32(firstChar + displace);
                encoding.Add(new CharEncoding(charClass.ToString(), transClass));
                firstChar++;
            }
        }

        public IEnumerable<byte> TranslateString(string encodingName, string expression)
        {
            List<byte> bytes = new List<byte>();
            var encoding = _encodings[encodingName];
            foreach(var s in expression)
            {
                var transbyte = encoding.First(e => e.Class.Equals(s)).Translation;
                bytes.AddRange(BitConverter.GetBytes(transbyte));
            }
            return bytes;
        }

        public void DefineEncoding(string encoding)
        {
            _encodings.Add(encoding, new List<CharEncoding>());
        }
    }
}
