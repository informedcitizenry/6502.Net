//-----------------------------------------------------------------------------
// Copyright (c) 2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// Manages custom text codes from UTF-8 source
    /// to target architectures with different character encoding
    /// schemes.
    /// </summary>
    public sealed class AsmEncoding : Encoding
    {
        #region Members

        Dictionary<StringView, Dictionary<int, int>> _maps;
        Dictionary<int, int> _currentMap;
        bool _caseSensitive;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the <see cref="AsmEncoding"/> class, used to encode 
        /// strings from UTF-8 source to architecture-specific encodings.
        /// </summary>
        /// <param name="caseSensitive">Indicates whether encoding names 
        /// should be treated as case-sensitive. Note: This has no effect on how character
        /// mappings are translated</param>
        public AsmEncoding(bool caseSensitive)
        {
            _caseSensitive = caseSensitive;
            StringViewComparer comparer = caseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;
            _maps = new Dictionary<StringView, Dictionary<int, int>>(comparer);
            SelectEncoding("\"none\"");
        }

        /// <summary>
        /// Constructs an instance of the <see cref="AsmEncoding"/> class, used to encode 
        /// strings from UTF-8 source to architecture-specific encodings.
        /// </summary>
        public AsmEncoding() :
            this(false)
        {
        }

        #endregion

        #region Methods

        byte[] GetCharBytes(string s)
        {
            var utf32 = char.ConvertToUtf32(s, 0);
            if (_currentMap.TryGetValue(utf32, out var code))
            {
                var codebytes = BitConverter.GetBytes(code);
                return codebytes.Take(code.Size()).ToArray();
            }
            return UTF8.GetBytes(s);
        }

        /// <summary>
        /// Get the encoded binary value of the given character as an 
        /// <see cref="int" /> value.
        /// </summary>
        /// <param name="s">The string element to encode.</param>
        /// <returns>An unsigned 32-bit integer representing the 
        /// encoding of the character.</returns>
        /// <exception cref="ArgumentException">System.ArgumentException</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public int GetEncodedValue(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s), "String cannot be null.");

            var bytes = GetCharBytes(s);
            if (bytes.Length < 4)
                Array.Resize(ref bytes, 4);
            else if (bytes.Length > 4)
                throw new OverflowException($"Size of string literal \"{s}\" exceeds integer value");
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Get the mapped representation of the Unicode codepoint in the selected encoding.
        /// </summary>
        /// <param name="code">The Unicode codepoint.</param>
        /// <returns>The mapped value of the selected encoding.</returns>
        public int GetCodePoint(int code)
        {
            if (_currentMap.Values.Contains(code))
                return _currentMap.First(k => k.Value == code).Key;
            return code;
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="mapping">The text element, or range of text elements to map.</param>
        /// <param name="code">The code of the mapping.</param>
        /// <exception cref="ArgumentException">System.ArgumentException</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Map(string mapping, int code)
        {
            // the default encoding cannot be changed
            if (_currentMap == _maps.First().Value)
                return;

            if (string.IsNullOrEmpty(mapping))
                throw new ArgumentNullException(nameof(mapping), "Mapping argument missing.");

            var stringInfo = StringInfo.ParseCombiningCharacters(mapping);
            if (stringInfo.Length > 1)
            {
                if (stringInfo.Length > 2)
                    throw new ArgumentException("Invalid mapping argument.");

                var first = char.ConvertToUtf32(mapping, stringInfo.First());
                var last = char.ConvertToUtf32(mapping, stringInfo.Last());

                if (first > last)
                    throw new ArgumentException("Invalid mapping range.");

                while (first <= last)
                    _currentMap[first++] = code++;
            }
            else
            {
                if (mapping.Length > 1)
                    throw new ArgumentException("Invalid mapping argument.");
                _currentMap[char.ConvertToUtf32(mapping, 0)] = code;
            }
        }

        /// <summary>
        /// Adds a character mapping to translate from source to object
        /// </summary>
        /// <param name="mapping">The character to map</param>
        /// <param name="code">The code of the mapping.</param>
        /// <exception cref="ArgumentException">System.ArgumentException</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Map(char mapping, int code) => Map(mapping.ToString(), code);

        /// <summary>
        /// Remove a mapping for the current encoding.
        /// </summary>
        /// <param name="mapping">The text element, or range of text elements to unmap.</param>
        /// <exception cref="T:System.ArgumentException">System.ArgumentException</exception>
        /// <exception cref="T:System.ArgumentNullException"></exception>
        public void Unmap(string mapping)
        {
            // the default encoding cannot be changed
            if (_currentMap == _maps.First().Value)
                return;

            if (string.IsNullOrEmpty(mapping))
                throw new ArgumentNullException(nameof(mapping), "Argument cannot be null.");

            var stringInfo = StringInfo.ParseCombiningCharacters(mapping);
            if (stringInfo.Length > 1)
            {
                if (stringInfo.Length > 2)
                    throw new ArgumentException(mapping);

                var first = char.ConvertToUtf32(mapping, stringInfo.First());
                var last = char.ConvertToUtf32(mapping, stringInfo.Last());

                if (first > last)
                    throw new ArgumentException(mapping);

                while (first <= last)
                    _currentMap.Remove(first++);
            }
            else
            {
                _currentMap.Remove(char.ConvertToUtf32(mapping, 0));
            }
        }

        /// <summary>
        /// Select the current encoding to the default UTF-8 encoding
        /// </summary>
        public void SelectDefaultEncoding() => _currentMap = _maps["\"none\""];

        /// <summary>
        /// Select the current named encoding
        /// </summary>
        /// <param name="encodingName">The encoding name</param>
        public void SelectEncoding(StringView encodingName)
        {
            if (!_maps.ContainsKey(encodingName))
                _maps.Add(encodingName, new Dictionary<int, int>());
            _currentMap = _maps[encodingName];
        }

        #region Encoding Methods

        public override int GetByteCount(string s)
        {
            var numbytes = 0;
            TextElementEnumerator textEnumerator = StringInfo.GetTextElementEnumerator(s);
            while (textEnumerator.MoveNext())
            {
                var elem = textEnumerator.GetTextElement();
                var cu = char.ConvertToUtf32(elem, 0);
                if (_currentMap.ContainsKey(cu))
                    numbytes += _currentMap[cu].Size();
                else
                    numbytes += UTF8.GetByteCount(elem);
            }
            return numbytes;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            var s = new string(chars.Skip(index).Take(count).ToArray());
            return GetByteCount(s);
        }

        public override byte[] GetBytes(string s)
        {
            var bytes = new List<byte>();
            TextElementEnumerator textEnumerator = StringInfo.GetTextElementEnumerator(s);

            while (textEnumerator.MoveNext())
            {
                var elem = textEnumerator.GetTextElement();
                bytes.AddRange(GetCharBytes(elem));
            }
             return bytes.ToArray();
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            var s = new string(chars.Skip(charIndex).Take(charCount).ToArray());
            var charBytes = GetBytes(s);
            var j = byteIndex;

            foreach (var b in charBytes)
                bytes[j++] = b;

            return j - byteIndex;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            var chars = new char[GetMaxCharCount(count)];
            return GetChars(bytes, index, count, chars, 0);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            var i = charIndex;
            while (byteIndex < byteCount)
            {
                var displ = 0;
                var encoding = 0;
                if (byteIndex + 3 < byteCount)
                {
                    encoding = bytes[byteIndex] |
                               (bytes[byteIndex + 1] * 0x100) |
                               (bytes[byteIndex + 2] * 0x10000) |
                               (bytes[byteIndex + 3] * 0x1000000);
                    if (_currentMap.ContainsValue(encoding))
                    {
                        displ = 4;
                        goto SetChar;
                    }
                }
                if (byteIndex + 2 < byteCount)
                {
                    encoding = bytes[byteIndex] |
                               (bytes[byteIndex + 1] * 0x100) |
                               (bytes[byteIndex + 2] * 0x10000);
                    if (_currentMap.ContainsValue(encoding))
                    {
                        displ = 3;
                        goto SetChar;
                    }
                }
                if (byteIndex + 1 < byteCount)
                {
                    encoding = bytes[byteIndex] |
                               (bytes[byteIndex + 1] * 0x100);
                    if (_currentMap.ContainsValue(encoding))
                    {
                        displ = 2;
                        goto SetChar;
                    }
                }
                encoding = bytes[byteIndex];
                if (_currentMap.ContainsValue(encoding))
                {
                    displ = 1;
                    goto SetChar;
                }

                var count = 1;
                var utfChars = UTF8.GetChars(bytes.Skip(byteIndex).ToArray(), 0, byteCount - byteIndex);

                if (char.IsSurrogate(utfChars.First()))
                    count++;

                utfChars = utfChars.Take(count).ToArray();
                foreach (var utfChar in utfChars)
                    chars[i++] = utfChar;

                byteIndex += UTF8.GetByteCount(utfChars);
                continue;

            SetChar:
                var key = _currentMap.First(e => e.Value.Equals(encoding)).Key;
                var utfchars = char.ConvertFromUtf32(key);
                foreach (var utfc in utfchars)
                    chars[i++] = utfc;
                byteIndex += displ;
            }
            return i - charIndex;
        }

        public override int GetMaxByteCount(int charCount) => charCount * sizeof(int);

        public override int GetMaxCharCount(int byteCount)
        {
            // An encoding could be mapped to a surrogate pair, so we must double max char count
            return byteCount * sizeof(char);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the case sensitivity of the encoding definitions.
        /// </summary>
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set
            {
                _caseSensitive = value;
                StringViewComparer comparer = _caseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;
                _maps = new Dictionary<StringView, Dictionary<int, int>>(_maps, comparer);
            }
        }

        #endregion
    }
}