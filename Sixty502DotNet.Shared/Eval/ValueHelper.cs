// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Lex;
using System.Globalization;
using System.Text;

namespace Sixty502DotNet.Shared.Eval;

public static class ValueHelper
{
    private const decimal MaxTByte = 1208925819614629174706175m;

    public static bool ExceedsTByte(this Int128 value)
    {
        return Int128.MaxMagnitude(value, (Int128)MaxTByte) > (Int128)MaxTByte;
    }
    
    public static long GetCbmFloat(double val, bool packed)
    {
        var longDouble = BitConverter.DoubleToInt64Bits(val);
                           
        const long expoMask = 0b0_11111111111_0000000000000000000000000000000000000000000000000000;
        const long mantMask = 0b0_00000000000_1111111111111111111111111111111111111111111111111111;
        const int adjustedBias = 894;
        var exponent = (longDouble & expoMask) >> 52;
        var mantissa = longDouble & mantMask;
        var cbmExponent = exponent - adjustedBias;
        var cbmMantissa = mantissa >> (52 - 31); 
        if (packed)
        {
            var sign = longDouble < 0 ? -1 : 0;
            return (cbmExponent << 32) | (uint)(sign << 31) | cbmMantissa;
        }
        var signByte = longDouble < 0 ? 0x80 : 0x00;
        return (cbmExponent << 40) | (cbmMantissa << 8) | (uint)signByte;
    }
    
    public static string? GetString(string s)
    {
        var isMultiline = s.Length > 5 && s[^3] == '"' && s[^2] == '"' && s[^1] == '"';
        var textStart = s[0] switch
        {
            '$' when isMultiline => 3,
            '"' when isMultiline => 2,
            'u' when s[1] == '8' => 2,
            '$' or 'U' or 'p' or 's' or 'u' => 1,
            _ => 0
        };
        var chars = StringInfo.GetTextElementEnumerator(s, 0);
        
        var sb = new StringBuilder();
        
        var delimiter = "\"";
        var escapeDoubleBraces = false;
        
        chars.MoveNext();
        if (s[0] == '$' || chars.GetTextElement() == "}")
        {
            escapeDoubleBraces = true;
            delimiter = s[^1].ToString();
        }
        while (textStart > 0)
        {
            chars.MoveNext();
            textStart--;
        }
        while (true)
        {
            chars.MoveNext();
            var current = chars.GetTextElement();
 
            if (escapeDoubleBraces && current is "{" or "}")
            {
                if (!chars.MoveNext() || !chars.GetTextElement().Equals(current))
                {
                    if (current.Equals(delimiter))
                    {
                        break;
                    }
                }
            }
            else if (current.Equals(delimiter))
            {
                break;
            }
            var chr = GetUnicode(chars);
            if (chr == null) return null;
            try
            {
                sb.Append(char.ConvertFromUtf32(chr.Value));
            }
            catch
            {
                return null;
            }
        }
        return sb.ToString();
    }

    public static char? GetChar(string s)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(s, 0);
        enumerator.MoveNext();
        enumerator.MoveNext();
        var u = GetUnicode(enumerator);
        if (u == null || !enumerator.MoveNext() || !enumerator.GetTextElement().Equals("'")) 
            return null;
        try
        {
            return Convert.ToChar(u);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsTrue(this bool? b)
    {
        return b.HasValue && b.Value;
    }
    
    public static Value? ParseAltBinary(string s)
    {
        if (s.Length > 64) return null;
        long value = 0;
        for (var i = s.Length - 1; i >= 0; i--)
        {
            if (s[i] == '#')
            {
                value |= (uint)(1 << i);
            }
        }
        return value <= int.MaxValue 
            ? new Value(new Alias((int)value)) 
            : new Value(value);
    }
    
    public static Value? ParseInt(string s)
    {
        var radix = GetLiteralRadix(s, out var prefix);
        var enumerator = s[prefix..].GetEnumerator();
        var (sanitizedIntText, _) = GetSanitizedLiteral(enumerator, radix);
        try
        {
            var value = Convert.ToInt64(sanitizedIntText, radix);
            if (radix != 10 && value <= int.MaxValue)
            {
                return new Value(new Alias((int)value));
            }
            return new Value(value);
        }
        catch (FormatException)
        {
            return null;
        }
        catch
        {
            if (sanitizedIntText.Length > 1) sanitizedIntText = sanitizedIntText.TrimStart('0');
            try
            {
                if (radix == 8)
                {
                    var octal = sanitizedIntText
                        .Aggregate<char, Int128>(0, (current, t) 
                        => (current << 3) | (t - '0'));
                    return new Value(octal);
                }
                var style = radix switch
                {
                    2 => NumberStyles.BinaryNumber,
                    16 => NumberStyles.HexNumber,
                    _ => NumberStyles.Integer
                };
                return new Value(Int128.Parse(sanitizedIntText, style));
            }
            catch
            {
                var bigFloat = ParseBigFloat(sanitizedIntText, radix);
                return bigFloat != null ? new Value(bigFloat.Value) : null;
            }
        }
    }

    public static Value? ParseFloat(string s)
    {
        var radix = GetLiteralRadix(s, out var prefix);
        var enumerator = s[prefix..].GetEnumerator();
        var (sanitizedIntText, _) = GetSanitizedLiteral(enumerator, radix);
        if (sanitizedIntText[0] == '0')
        {
            var i = 0;
            for (; i < sanitizedIntText.Length && sanitizedIntText[i] != '0'; i++) { }
            sanitizedIntText = i < sanitizedIntText.Length ? sanitizedIntText[i..] : sanitizedIntText;
        }
        var hasExponent = false;
        var exponentBase = 10.0;
        var expoSign = 1.0;
        string sanitizedFracText = "0", expoText = "0";
        switch (enumerator.Current)
        {
            case '.':
            {
                (sanitizedFracText, var endReached) = GetSanitizedLiteral(enumerator, radix);
                if (sanitizedFracText.Length > 1)
                {
                    sanitizedFracText = sanitizedFracText.TrimEnd('0');
                }
                if (!endReached && enumerator.Current.IsExponent())
                {
                    hasExponent = true;
                    if (enumerator.Current is 'P' or 'p')
                    {
                        exponentBase = 2.0;
                    }
                    (expoText, _) = GetSanitizedLiteral(enumerator, 16);
                    if (expoText[0] == '0' && expoText.Length > 1)
                    {
                        return null;
                    }
                }
                break;
            }
            case var c when c.IsExponent():
            {
                hasExponent = true;
                if (enumerator.Current is 'P' or 'p')
                {
                    exponentBase = 2.0;
                }
                (expoText, _) = GetSanitizedLiteral(enumerator, 16);
                break;
            }
            default:
                return null;
        }
        try
        {
            var i = ParseBigFloat(sanitizedIntText, radix);
            if (i == null) return null;

            var f = ParseBigFloat(sanitizedFracText, radix);
            if (f == null) return null;

            if (hasExponent && expoText[0] is '+' or '-')
            {
                expoSign = expoText[0] == '-' ? -1.0 : 1.0;
                expoText = expoText[1..];
            }
            if (expoText.Length > 1 && expoText[0] == '0')
            {
                return null; // disallow exponent of non-base 10 (e.g, 10e03)
            }
            var e = ParseBigFloat(expoText,10) * expoSign;
            if (e == null) return null;
            
            var fractPart = f.Value / Math.Pow(radix * 1.0, sanitizedFracText.Length);
            
            var val = (i.Value + fractPart) * Math.Pow(exponentBase, e.Value);
            return new Value(val);
        }
        catch
        {
            return null;
        }
    }

    private static double? ParseBigFloat(string sanitizedText, int radix)
    {
        var digits = sanitizedText.Length;
        var maxDigits = radix switch
        {
            2 => 63,
            8 => 21,
            10 => 18,
            _ => 15
        };
        if (sanitizedText.Length <= maxDigits)
        {
            return 1.0 * Convert.ToInt64(sanitizedText, radix);
        }
        sanitizedText = sanitizedText[..maxDigits];
        var integerPart = 1.0 * Convert.ToInt64(sanitizedText[0].ToString(), radix);
        try
        {
            var fractPart = Convert.ToInt64(sanitizedText[1..], radix) * 1.0 / Math.Pow(radix * 1.0, sanitizedText.Length * 1.0 - 1);
            return (integerPart + fractPart) * Math.Pow(radix * 1.0, digits * 1.0 - 1); 
        }
        catch
        {
            return null;
        }
    }

    extension(int i)
    {
        public int Size() => ((long)i).Size();
    }
    
    extension(long i)
    {
        private byte[] ToBytes() => BitConverter.GetBytes(i).Take(i.Size()).ToArray();

        public byte[] ToBytes(ByteOrder byteOrder)
        {
            var final = i.ToBytes();
            if ((byteOrder == ByteOrder.LittleEndian && !BitConverter.IsLittleEndian) ||
                (byteOrder == ByteOrder.BigEndian && BitConverter.IsLittleEndian))
            {
                final = final.Reverse().ToArray();
            }
            return final;
        }
    }

    extension(double d)
    {
        public bool FloatLt(double other) 
            => other - d > Epsilon.Value;

        public bool FloatGt(double other) 
            => d - other > Epsilon.Value;

        public bool FloatEq(double other) 
            => Math.Abs(d - other) < Epsilon.Value;

        public bool FloatLe(double other) 
            => d.FloatLt(other) || d.FloatEq(other);

        public bool FloatGe(double other) 
            => d.FloatGt(other) || d.FloatEq(other);
    }

    extension(long value)
    {
        public int Size()
        {
            return value switch
            {
                >= sbyte.MinValue and <= byte.MaxValue => 1,
                >= short.MinValue and <= ushort.MaxValue => 2,
                >= Int24.MinValue and <= UInt24.MaxValue => 3,
                >= int.MinValue and <= uint.MaxValue => 4,
                _ => 8
            };
        }

        public long AsPositive()
        {
            return value switch
            {
                >= sbyte.MinValue and <= byte.MaxValue => value & 0xff,
                >= short.MinValue and <= ushort.MaxValue => value & 0xffff,
                >= Int24.MinValue and <= UInt24.MaxValue => value & 0xffffff,
                >= int.MinValue and <= uint.MaxValue => value & 0xffffffff,
                _ => value
            };
        }
    }
    

    private static int GetLiteralRadix(string s, out int firstDigitIndex)
    {
        firstDigitIndex = 0;
        
        switch (s[0])
        {
            case '0':
                if (s.Length == 1 || s[1] == '.') return 10;
                if (s.Length <= 2 || !(s[1].IsBasePrefix() || s[1] is 'B' or 'b'))
                {
                    return s.Length > 1 && s[1] is >= '0' and <= '7' ? 8 : 10;
                }
                firstDigitIndex = 2;
                return s[1] switch
                {
                    'B' or 'b' => 2,
                    'O' or 'o' => 8,
                    'X' or 'x' => 16,
                    _ => 10
                };
            case '$':
                firstDigitIndex = 1;
                return 16;
            case '%':
                firstDigitIndex = 1;
                return 2;
        }
        return 10;
    }

    extension(TokenType type)
    {
        public TextEncodingType ToTextEncodingType()
        {
            return type switch
            {
                TokenType.AtaScreenStringLiteral => TextEncodingType.AtaScreen,
                TokenType.CbmScreenStringLiteral => TextEncodingType.CbmScreen,
                TokenType.PetsciiStringLiteral => TextEncodingType.Petscii,
                TokenType.Utf8StringLiteral => TextEncodingType.Utf8,
                TokenType.Utf16StringLiteral => TextEncodingType.Utf16,
                TokenType.Utf32StringLiteral => TextEncodingType.Utf32,
                _ => TextEncodingType.Default
            };
        }
    }

    private static (string, bool) GetSanitizedLiteral(CharEnumerator enumerator, int radix)
    {
        var sb = new StringBuilder();
        enumerator.MoveNext();
        sb.Append(enumerator.Current);
        var endReached = false;
        while (true)
        {
            if (!enumerator.MoveNext())
            {
                endReached = true;
                break;
            }

            var c = enumerator.Current;
            if (c.IsBaseDigit(radix))
            {
                sb.Append(c);
            }
            else if (c != '_')
            {
                break;
            }
        }
        return (sb.ToString(), endReached);
    }

    private static int? GetUnicode(TextElementEnumerator enumerator)
    {
        var ch = enumerator.GetTextElement();
        if (!ch.Equals("\\") || !enumerator.MoveNext())
        {
            return char.ConvertToUtf32(ch, 0);
        }
        var next = enumerator.GetTextElement();
        if (next.Length > 1) return null;
        return next[0] switch
        {
            '0' => 0,
            '"' => '"',
            '\'' => '\'',
            '\\' => '\\',
            'a' => '\a',
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'u' => GetUnicodeEscaped(enumerator, 'u'),
            'U' => GetUnicodeEscaped(enumerator, 'U'),
            'v' => '\v',
            'x' => GetUnicodeEscaped(enumerator, 'x'),
            _ => null
        };
    }
    
    private static int? GetUnicodeEscaped(TextElementEnumerator enumerator, char escapeType)
    {
        if (!enumerator.MoveNext()) return null;
        try
        {
            var unicode = Convert.ToInt32(enumerator.GetTextElement(), 16);
            var expectedCodes = escapeType switch
            {
                'x' => 2,
                '2' or 
                'u' => 4,
                _ => 8
            };
            for (var i = 1; i < expectedCodes; i++)
            {
                if (!enumerator.MoveNext()) return null;
                var hex = enumerator.GetTextElement();
                if (hex.Length > 1 || !char.IsAsciiHexDigit(hex[0]))
                {
                    return null;
                }
                unicode <<= 4;
                unicode |= Convert.ToInt32(hex, 16);
            }
            if (unicode is >= Unicode.SurrogateMin and <= Unicode.SurrogateMax)
            {
                if (unicode < Unicode.LowSurrogate)
                {
                    if (escapeType != 'u' || 
                        !enumerator.MoveNext() || 
                        enumerator.GetTextElement() != "\\" ||
                        !enumerator.MoveNext() ||
                        enumerator.GetTextElement() != "u")
                    {
                        return null;
                    }
                    var low = GetUnicodeEscaped(enumerator, '2');
                    if (low == null) return null;
                    unicode = (unicode - Unicode.HighSurrogate) * 0x400 + (low.Value - Unicode.LowSurrogate) + 0x10000;
                }
                else if (escapeType != '2')
                {
                    return null;
                }
            }
            return unicode;
        }
        catch
        {
            return null;
        }
    }
    
    public static string Name(this TypeTag typeTag) => $"<type@{typeTag}>";
}