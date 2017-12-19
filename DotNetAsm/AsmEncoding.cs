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

namespace DotNetAsm
{
    /// <summary>
    /// Manages custom text codes from UTF-8 source
    /// to target architectures with different character encoding
    /// schemes.
    /// </summary>
    public sealed class AsmEncoding : Encoding
    {
        #region Members
        
        Dictionary<string, Dictionary<char, int>> _maps;

        Dictionary<char, int> _currentMap;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the DotNetAsm.AsmEncoding class, used to encode 
        /// strings from UTF-8 source to architecture-specific encodings.
        /// </summary>
        /// <param name="caseSensitive">Indicates whether encoding names 
        /// should be treated as case-sensitive. Note: This has no effect on how character
        /// mappings are translated</param>
        public AsmEncoding(bool caseSensitive)
        {
            StringComparer comparer = caseSensitive ? StringComparer.CurrentCulture : StringComparer.CurrentCultureIgnoreCase;
            _maps = new Dictionary<string, Dictionary<char, int>>(comparer);
            SelectEncoding("none");
        }

        /// <summary>
        /// Constructs an instance of the DotNetAsm.AsmEncoding class, used to encode 
        /// strings from UTF-8 source to architecture-specific encodings.
        /// </summary>
        public AsmEncoding() :
            this(false)
        {
            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the actual encoded bytes for a given character.
        /// </summary>
        /// <param name="chr">The character to encode.</param>
        /// <returns>An array of encoded bytes for the character.</returns>
        byte[] GetCharBytes(char chr)
        {
            if (_currentMap.ContainsKey(chr))
            {
                long code = _currentMap[chr];
                var codebytes = BitConverter.GetBytes(code);
                return codebytes.Take(code.Size()).ToArray();
            }
            return Encoding.UTF8.GetBytes(new char[] { chr });
        }

        /// <summary>
        /// Get the encoded binary value of the given character.
        /// </summary>
        /// <param name="chr">The character to encode.</param>
        /// <returns>An unsigned 32-bit integer representing the 
        /// encoding of the character.</returns>
        public int GetEncodedValue(char chr)
        {
            if (_currentMap.ContainsKey(chr))
                return _currentMap[chr];
            byte[] charbytes = GetCharBytes(chr);
            byte[] paddedcodebytes = new byte[4];
            Array.Copy(GetCharBytes(chr), paddedcodebytes, charbytes.Length);
            return BitConverter.ToInt32(paddedcodebytes, 0);
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="mapping">The char to map</param>
        /// <param name="code">The corresponding code</param>
        public void Map(char mapping, int code)
        {
            // the default encoding cannot be changed
            if (_currentMap != _maps.First().Value)
                _currentMap.Add(mapping, code);
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="range">The range of characters to map</param>
        /// <param name="firstcode">The first char code</param>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public void Map(string range, int firstcode)
        {
            var rangeChars = range.ToCharArray();
            if (rangeChars.Length != 2)
                throw new ArgumentException(range);

            Map(System.Convert.ToInt32(rangeChars.First()),
                System.Convert.ToInt32(rangeChars.Last()),
                firstcode);
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="firstRange">The first character in the mapping range</param>
        /// <param name="lastRange">The last character in the mapping range</param>
        /// <param name="firstcode">The first char code</param>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public void Map(int firstRange, int lastRange, int firstcode)
        {
            if (firstRange > lastRange)
                throw new ArgumentException(firstRange.ToString());

            // the default encoding cannot be changed
            if (_currentMap == _maps.First().Value)
                return;

            int displace = firstcode - firstRange;

            while (firstRange <= lastRange)
            {
                var charMap = System.Convert.ToChar(firstRange);
                var code = System.Convert.ToInt32(firstRange + displace);
                _currentMap.Add(charMap, code);
                firstRange++;
            }
        }

        /// <summary>
        /// Get the encoding of the mapped character.
        /// </summary>
        /// <param name="mapping">The mapped character</param>
        /// <returns>The mapped character encoding</returns>
        public int GetCode(char mapping)
        {
            if (_currentMap.ContainsKey(mapping))
                return _currentMap[mapping];
            return mapping;
        }

        /// <summary>
        /// Remove a mapping for the current encoding.
        /// </summary>
        /// <param name="mapping">The character to unmap</param>
        public void Unmap(char mapping)
        {
            if (_currentMap.ContainsKey(mapping))
                _currentMap.Remove(mapping);
        }

        /// <summary>
        /// Remove a mapping for the current encoding.
        /// </summary>
        /// <param name="range">The range of of chars as a string to unmap</param>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public void Unmap(string range)
        {
            if (range.Length != 2)
                throw new ArgumentException(range);

            Unmap(range.First(), range.Last());
        }

        /// <summary>
        /// Remove a mapping for the current encoding.
        /// </summary>
        /// <param name="firstRange">The first char in the range to unamp</param>
        /// <param name="lastRange">The last char in the range to unmap</param>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        public void Unmap(char firstRange, char lastRange)
        {
            if (firstRange >= lastRange)
                throw new ArgumentException(lastRange.ToString());

            while (firstRange != lastRange)
                Unmap(firstRange++);
        }

        /// <summary>
        /// Select the current encoding to the default UTF-8 encoding
        /// </summary>
        public void SelectDefaultEncoding()
        {
            _currentMap = _maps["none"];
        }

        /// <summary>
        /// Select the current named encoding
        /// </summary>
        /// <param name="encodingName">The encoding name</param>
        public void SelectEncoding(string encodingName)
        {
            if (!_maps.ContainsKey(encodingName))
                _maps.Add(encodingName, new Dictionary<char, int>());
            _currentMap = _maps[encodingName];
        }

        #region Encoding Methods

        /// <summary>
        /// Calculates the number of bytes produced by encoding all the characters
        /// in the specified character array.
        /// </summary>
        /// <param name="chars">The character array containing the characters to encode</param>
        /// <param name="index">The index of the first character to encode</param>
        /// <param name="count">The number of characters to encode</param>
        /// <returns>The number of bytes produced by encoding the specified characters.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">System.IndexOutOfRangeException</exception>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            int numbytes = 0;
            for (int i = 0; i < count; i++)
            {
                byte[] bytechars = GetCharBytes(chars[i + index]);
                numbytes += bytechars.Length; 
            }
            return numbytes;
        }

        /// <summary>
        /// Encodes a set of characters from the specified character 
        /// array into the specified byte array.
        /// </summary>
        /// <param name="chars">The character array containing the set of characters to encode</param>
        /// <param name="charIndex">The index of the first character to encode</param>
        /// <param name="charCount">The number of characters to encode</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes</param>
        /// <param name="byteIndex">The index at which to start writing the resulting 
        /// sequence of bytes</param>
        /// <returns>The actual number of bytes written into bytes.</returns>
        /// <exception cref="T:System.ArgumentNullException">System.ArgumentNullException</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">System.ArgumentOutOfRangeException</exception>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        /// <exception cref="T:System.IndexOutOfRangeException">System.IndexOutOfRangeException</exception>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int j = byteIndex;
            for (int i = 0; i < charCount; i++)
            {
                byte[] transBytes = GetCharBytes(chars[i + charIndex]);
                foreach (byte b in transBytes)
                    bytes[j++] = b;
            }
            return j - byteIndex;
        }

        /// <summary>
        /// Calculates the number of characters produced by decoding a sequence of bytes 
        /// from the specified byte array.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode</param>
        /// <param name="index">The index of the first byte to decode</param>
        /// <param name="count">The number of bytes to decode</param>
        /// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
        /// <exception cref="T:System.ArgumentNullException">System.ArgumentNullException</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">System.ArgumentOutOfRangeException</exception>
        /// <exception cref="T:System.Text.TextDecoderFallbackException"></exception>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            char[] chars = new char[GetMaxCharCount(count)];
            return GetChars(bytes, index, count, chars, 0);
        }

        /// <summary>
        /// Decodes a sequence of bytes from the specified byte array 
        /// into the specified character array. 
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode</param>
        /// <param name="byteIndex">The index of the first byte to decode</param>
        /// <param name="byteCount">The number of bytes to decode</param>
        /// <param name="chars">The character array to contain the resulting set of characters</param>
        /// <param name="charIndex">The index at which to start writing the resulting set of characters</param>
        /// <returns>The actual number of characters written into chars.</returns>
        /// <exception cref="T:System.ArgumentNullException">System.ArgumentNullException</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">System.ArgumentOutOfRangeException</exception>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        /// <exception cref="T:System.IndexOutOfRangeException">System.IndexOutOfRangeException</exception>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int j = charIndex;
            int i = 0;
            int bsize = bytes.Length;
            while (i < byteCount)
            {
                int displ = 0;
                int encoding = 0;
                if (i + 3 + byteIndex < bsize)
                {
                    encoding = bytes[i + byteIndex] | 
                               (bytes[i + 1 + byteIndex] * 0x100) | 
                               (bytes[i + 2 + byteIndex] * 0x10000) |
                               (bytes[i + 3 + byteIndex] * 0x1000000);
                    if (_currentMap.ContainsValue(encoding))
                    {
                        displ = 4;
                        goto SetChar;
                    }
                }
                if (i + 2 + byteIndex < bsize)
                {
                    encoding = bytes[i + byteIndex] |
                               (bytes[i + 1 + byteIndex] * 0x100) |
                               (bytes[i + 2 + byteIndex] * 0x10000);
                    if (_currentMap.ContainsValue(encoding))
                    {
                        displ = 3;
                        goto SetChar;
                    }
                }
                if (i + 1 + byteIndex < bsize)
                {
                    encoding = bytes[i + byteIndex] |
                               (bytes[i + 1 + byteIndex] * 0x100);
                    if (_currentMap.ContainsValue(encoding))
                    {
                        displ = 2;
                        goto SetChar;
                    }
                }
                encoding = bytes[i + byteIndex];
                if (_currentMap.ContainsValue(encoding))
                {
                    displ = 1;
                    goto SetChar;
                }

                var utfChar = UTF8.GetChars(bytes.Skip(i).ToArray(), 0, byteCount - i).First();
                chars[j++] = utfChar;
                i += UTF8.GetByteCount(new char[] { utfChar }, 0, 1);
                continue;

            SetChar:
                chars[j++] = _currentMap.First(e => e.Value.Equals(encoding)).Key;
                i += displ;
            }
            return j - charIndex;
        }

        /// <summary>
        /// Calculates the maximum number of bytes 
        /// produced by encoding the specified number of characters.
        /// </summary>
        /// <param name="charCount">The number of characters to encode</param>
        /// <returns>The maximum number of bytes produced by 
        /// encoding the specified number of characters.</returns>
        public override int GetMaxByteCount(int charCount)
        {
            return charCount * sizeof(uint);
        }

        /// <summary>
        /// Calculates the maximum number of characters produced 
        /// by decoding the specified number of bytes.
        /// </summary>
        /// <param name="byteCount">The number of bytes to decod</param>
        /// <returns>The maximum number of characters produced by 
        /// decoding the specified number of bytes.</returns>
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        #endregion

        #endregion
    }
}
