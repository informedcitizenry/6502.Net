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
    /// Manages custom text translations from standard ASCII/UTF-8
    /// to target architectures with different character encoding
    /// schemes.
    /// </summary>
    public class AsmEncoding : Encoding
    {
        #region Members
        
        private Dictionary<string, Dictionary<char, UInt32>> _maps;

        private Dictionary<char, UInt32> _currentMap;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the DotNetAsm.AsmEncoding class, used to encode 
        /// strings from ASCII/UTF-8 source to architecture-specific character set.
        /// </summary>
        /// <param name="caseSensitive">Indicates whether encoding names 
        /// should be treated as case-sensitive. Note: This has no effect on how character
        /// mappings are translated</param>
        public AsmEncoding(bool caseSensitive)
        {
            StringComparer comparer = caseSensitive ? StringComparer.CurrentCulture : StringComparer.CurrentCultureIgnoreCase;
            _maps = new Dictionary<string, Dictionary<char, uint>>(comparer);
            SelectEncoding("none");
        }

        /// <summary>
        /// Constructs an instance of the DotNetAsm.AsmEncoding class, used to encode 
        /// strings from ASCII/UTF-8 source to architecture-specific character set.
        /// </summary>
        public AsmEncoding() :
            this(false)
        {
            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="mapping">The char to map</param>
        /// <param name="translation">The corresponding translation</param>
        public void Map(char mapping, char translation)
        {
            // the default encoding cannot be changed
            if (_currentMap != _maps.First().Value)
                _currentMap.Add(mapping, translation);
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="range">The range of characters to map</param>
        /// <param name="firstTranslation">The first char translation</param>
        /// <exception cref="System.ArgumentException">System.ArgumentException</exception>
        public void Map(string range, char firstTranslation)
        {
            var rangeChars = range.ToCharArray();
            if (rangeChars.Length != 2)
                throw new ArgumentException(range);

            Map(System.Convert.ToInt32(rangeChars.First()),
                       System.Convert.ToInt32(rangeChars.Last()),
                       firstTranslation);
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="firstRange">The first character in the mapping range</param>
        /// <param name="lastRange">The last character in the mapping range</param>
        /// <param name="firstTranslation">The first char translation</param>
        /// <exception cref="System.ArgumentException">System.ArgumentException</exception>
        public void Map(int firstRange, int lastRange, char firstTranslation)
        {
            if (firstRange > lastRange)
                throw new ArgumentException(firstRange.ToString());

            // the default encoding cannot be changed
            if (_currentMap == _maps.First().Value)
                return;

            int displace = firstTranslation - firstRange;

            while (firstRange <= lastRange)
            {
                var charMap = System.Convert.ToChar(firstRange);
                var translation = System.Convert.ToUInt32(firstRange + displace);
                _currentMap.Add(charMap, translation);
                firstRange++;
            }
        }

        /// <summary>
        /// Get the char translation from its corresponding mapping
        /// </summary>
        /// <param name="mapping">The mapped char</param>
        /// <returns>The translated char</returns>
        public uint GetTranslation(char mapping)
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
        /// <exception cref="System.ArgumentException">System.ArgumentException</exception>
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
        /// <exception cref="System.ArgumentException">System.ArgumentException</exception>
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
                _maps.Add(encodingName, new Dictionary<char, UInt32>());
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
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return Encoding.UTF8.GetByteCount(chars);
        }

        /// <summary>
        /// Encodes all the characters in the specified string into a sequence of bytes.
        /// </summary>
        /// <param name="s">The string containing the characters to encode</param>
        /// <returns>A byte array containing the results of encoding the 
        /// specified set of characters.</returns>
        public override byte[] GetBytes(string s)
        {
            byte[] bytes = new byte[s.Length];
            GetBytes(s.ToCharArray(), 0, s.Length, bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Encodes a set of characters from the specified string 
        /// into the specified byte array.
        /// </summary>
        /// <param name="s">The string containing the characters to encode</param>
        /// <param name="charIndex">The index of the first character to encode</param>
        /// <param name="charCount">The number of characters to encode</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes</param>
        /// <param name="byteIndex">The index at which to start writing the resulting 
        /// sequence of bytes</param>
        /// <returns>The actual number of bytes written into bytes.</returns>
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
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
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int j = byteIndex;
            int bytecount = GetByteCount(chars);
            if (bytes.Length < bytecount)
                Array.Resize<byte>(ref bytes, bytecount);

            for (int i = charIndex; i < charCount; i++)
            {
                char transChar = chars[i];
                if (_currentMap.ContainsKey(chars[i]))
                     transChar = (char)_currentMap[chars[i]];
                byte[] transBytes = Encoding.UTF8.GetBytes(new char[] { transChar });
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
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return Encoding.UTF8.GetCharCount(bytes, index, count);
        }

        /// <summary>
        /// decodes a sequence of bytes from the specified byte array 
        /// into the specified character array.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode</param>
        /// <param name="byteIndex">The index of the first byte to decode</param>
        /// <param name="byteCount">The number of bytes to decode</param>
        /// <param name="chars">The character array to contain the resulting set of characters</param>
        /// <param name="charIndex">The index at which to start writing the resulting set of characters</param>
        /// <returns>The actual number of characters written into chars.</returns>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            char[] utfChars = Encoding.UTF8.GetChars(bytes, byteIndex, byteCount);

            for(int i = 0, j = charIndex; i < utfChars.Length; i++, j++)
            {
                var utfChar = utfChars[i];
                chars[j] = _currentMap.FirstOrDefault(m => m.Value.Equals(utfChar)).Key;
                if (chars[j].Equals('\0'))
                    chars[j] = utfChar;
            }
            return utfChars.Length;
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
            return Encoding.UTF8.GetMaxByteCount(charCount);
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
            return Encoding.UTF8.GetMaxCharCount(byteCount);
        }

        #endregion

        #endregion
    }
}
