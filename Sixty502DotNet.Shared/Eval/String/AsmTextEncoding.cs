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

using System.Text;

namespace Sixty502DotNet.Shared.Eval.String;

public class AsmTextEncoding : Encoding
{
    private readonly Dictionary<string, char> _charCodeUnits;
    private readonly Dictionary<string, char> _controlCodeCodeUnits;
    private readonly Dictionary<int, string> _codeUnitChars = new();
    
    public AsmTextEncoding(string name)
    : this(name, new Dictionary<string, char>(), new Dictionary<string, char>())
    {
        
    }

    private AsmTextEncoding
    (
        string name,
        Dictionary<string, char> charCodeUnits,
        Dictionary<string, char> controlCodeCodeUnits
    )
    {
        EncodingName = name;
        _controlCodeCodeUnits = controlCodeCodeUnits;
        _charCodeUnits = charCodeUnits;
        foreach (var kvp in _charCodeUnits)
            _codeUnitChars[kvp.Value] = kvp.Key;
    }

    public bool IsMapped(char c) => _charCodeUnits.ContainsKey(c.ToString());
    
    public override int GetByteCount(char[] chars, int index, int count)
    {
        var byteCount = 0;
        for (var c = index; c < count; c++)
        {
            if (chars[c] == '{')
            {
                var closeBraceIndex = c + 3;
                while (closeBraceIndex < count && chars[closeBraceIndex] != '}')
                {
                    closeBraceIndex++;
                }
                if (closeBraceIndex < count)
                {
                    var bracketed = chars.Skip(c).Take(closeBraceIndex + 1 - c).ToArray();
                    var controlCode = new string(bracketed);
                    if (_controlCodeCodeUnits.TryGetValue(controlCode, out _))
                    {
                        c = closeBraceIndex;
                        byteCount++;
                        continue;
                    }
                }
            }
            if (_charCodeUnits.ContainsKey(chars[c].ToString()))
            {
                byteCount++;
            }
            else
            {
                var thisCount = 1;
                var thisIndex = c;
                if (chars[c] >= 0xd800 && chars[c] <= 0xdbff && c < count - 1 && 
                    chars[c + 1] >= 0xdc00 && chars[c + 1] <= 0xdfff)
                {
                    c++;
                    thisCount++;
                }
                byteCount += UTF8.GetByteCount(chars, thisIndex, thisCount);
            }
        }
        return byteCount;
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        var byteCount = 0;
        for (int c = charIndex, b = byteIndex; c < charCount; c++)
        {
            if (chars[c] == '{')
            {
                var closeBraceIndex = c + 3;
                while (closeBraceIndex < charCount && chars[closeBraceIndex] != '}')
                {
                    closeBraceIndex++;
                }
                if (closeBraceIndex < charCount)
                {
                    var bracketed = chars.Skip(c).Take(closeBraceIndex + 1 - c).ToArray();
                    var controlCode = new string(bracketed);
                    if (_controlCodeCodeUnits.TryGetValue(controlCode, out var controlCodeUnit))
                    {
                        c = closeBraceIndex;
                        bytes[b++] = (byte)controlCodeUnit;
                        byteCount++;
                        continue;
                    }
                }
            }
            if (_charCodeUnits.TryGetValue(chars[c].ToString(), out var codeUnit))
            {
                bytes[b++] = (byte)codeUnit;
                byteCount++;
                continue;
            }
            var thisCount = 1;
            var thisIndex = c;
            if (chars[c] >= 0xd800 && chars[c] <= 0xdbff && c < charCount - 1 && 
                chars[c + 1] >= 0xdc00 && chars[c + 1] <= 0xdfff)
            {
                c++;
                thisCount++;
            }
            var bytesWritten = UTF8.GetBytes(chars, thisIndex, thisCount, bytes, byteIndex + b);
            byteCount += bytesWritten;
            b += bytesWritten;
        }
        return byteCount;
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        var charCount = 0;
        for (var b = index; b < count;)
        {
            if (_codeUnitChars.ContainsKey(bytes[b]) || bytes[b] <= 0x7f)
            {
                charCount++;
                b++;
            }
            else
            {
                var byteCount = bytes[b] switch
                {
                    >= 0xf0 => 4,
                    >= 0xe0 => 3,
                    >= 0xc0 => 2,
                    _ => 1
                };
                charCount += UTF8.GetCharCount(bytes, b, byteCount);
                b += byteCount;
            }
        }

        return charCount;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        var charCount = 0;
        for (var b = byteIndex; b < byteCount;)
        {
            if (_codeUnitChars.TryGetValue(bytes[b], out var character))
            {
                chars[charIndex++] = character[0];
                if (char.IsSurrogate(character[0]))
                {
                    chars[charIndex++] = character[1];
                }
                b++;
            }
            else
            {
                var charByteCount = bytes[b] switch
                {
                    >= 0xf0 => 4,
                    >= 0xe0 => 3,
                    >= 0xc0 => 2,
                    _ => 1
                };
                var charsWritten = UTF8.GetChars(bytes, b, charByteCount, chars, charIndex);
                charCount += charsWritten;
                charIndex += charsWritten;
                b += charByteCount;
            }
        }
        return charCount;
    }

    public override int GetMaxByteCount(int charCount) => charCount * 2;

    public override int GetMaxCharCount(int byteCount) => byteCount;

    public bool Map(string chr, char value)
    {
        if (_codeUnitChars.ContainsKey(value))
        {
            return false;
        }
        _charCodeUnits[chr] = value;
        _codeUnitChars[value] = chr;
        return true;
    }

    public bool MapRange(string start, string end, char startValue)
    {
        var i = char.ConvertToUtf32(start, 0);
        var e = char.ConvertToUtf32(end, 0);
        if (i >= e) return false;
        for (; i < e; i++)
        {
            if (!Map(char.ConvertFromUtf32(i), startValue++))
            {
                return false;
            }
        }
        return true;
    }

    public bool Unmap(string chr)
    {
        if (!_charCodeUnits.Remove(chr, out var codePoint))
        {
            return false;
        }
        _codeUnitChars.Remove(codePoint);
        return true;
    }

    public bool UnmapRange(string start, string end)
    {
        var i = char.ConvertToUtf32(start, 0);
        var e = char.ConvertToUtf32(end, 0);
        if (i >= e) return false;
        for (; i < e; i++)
        {
            if (!Unmap(char.ConvertFromUtf32(i)))
            {
                return false;
            } 
        }
        return true;
    }

    public override string EncodingName { get; }

    public static AsmTextEncoding Petscii =>
        new("petscii",
            new Dictionary<string, char>
            {
                { "a", 'A' },
                { "b", 'B' },
                { "c", 'C' },
                { "d", 'D' },
                { "e", 'E' },
                { "f", 'F' },
                { "g", 'G' },
                { "h", 'H' },
                { "i", 'I' },
                { "j", 'J' },
                { "k", 'K' },
                { "l", 'L' },
                { "m", 'M' },
                { "n", 'N' },
                { "o", 'O' },
                { "p", 'P' },
                { "q", 'Q' },
                { "r", 'R' },
                { "s", 'S' },
                { "t", 'T' },
                { "u", 'U' },
                { "v", 'V' },
                { "w", 'W' },
                { "x", 'X' },
                { "y", 'Y' },
                { "z", 'Z' },
                { "A", '\xc1' },
                { "B", '\xc2' },
                { "C", '\xc3' },
                { "D", '\xc4' },
                { "E", '\xc5' },
                { "F", '\xc6' },
                { "G", '\xc7' },
                { "H", '\xc8' },
                { "I", '\xc9' },
                { "J", '\xca' },
                { "K", '\xcb' },
                { "L", '\xcc' },
                { "M", '\xcd' },
                { "N", '\xce' },
                { "O", '\xcf' },
                { "P", '\xd0' },
                { "Q", '\xd1' },
                { "R", '\xd2' },
                { "S", '\xd3' },
                { "T", '\xd4' },
                { "U", '\xd5' },
                { "V", '\xd6' },
                { "W", '\xd7' },
                { "X", '\xd8' },
                { "Y", '\xd9' },
                { "Z", '\xda' },
                { "£", '\\' },
                { "↑", '^' },
                { "←", '_' },
                { "▌", '\xa1' },
                { "▄", '\xa2' },
                { "▔", '\xa3' },
                { "▁", '\xa4' },
                { "▏", '\xa5' },
                { "▒", '\xa6' },
                { "▕", '\xa7' },
                { "◤", '\xa9' },
                { "├", '\xab' },
                { "└", '\xad' },
                { "┐", '\xae' },
                { "▂", '\xaf' },
                { "┌", '\xb0' },
                { "┴", '\xb1' },
                { "┬", '\xb2' },
                { "┤", '\xb3' },
                { "▎", '\xb4' },
                { "▍", '\xb5' },
                { "▃", '\xb9' },
                { "✓", '\xba' },
                { "┘", '\xbd' },
                { "━", '\xc0' },
                { "♠", '\xc1' },
                { "│", '\xc2' },
                { "╮", '\xc9' },
                { "╰", '\xca' },
                { "╯", '\xcb' },
                { "╲", '\xcd' },
                { "╱", '\xce' },
                { "●", '\xd1' },
                { "♥", '\xd3' },
                { "╭", '\xd5' },
                { "╳", '\xd6' },
                { "○", '\xd7' },
                { "♣", '\xd8' },
                { "♦", '\xda' },
                { "┼", '\xdb' },
                { "π", '\xde' },
                { "◥", '\xdf' }
            },
            new Dictionary<string, char>
            {
                { "{BELL}", '\x07'  },
                { "{BLACK}", '\x90'  },
                { "{BLK}", '\x90'  },
                { "{BLUE}", '\x1F'  },
                { "{BLU}", '\x1F'  },
                { "{BRN}", '\x95'  },
                { "{BROWN}", '\x95'  },
                { "{CBM-*}", '\xDF'  },
                { "{CBM-+}", '\xA6'  },
                { "{CBM--}", '\xDC'  },
                { "{CBM-0}", '\x30'  },
                { "{CBM-1}", '\x81'  },
                { "{CBM-2}", '\x95'  },
                { "{CBM-3}", '\x96'  },
                { "{CBM-4}", '\x97'  },
                { "{CBM-5}", '\x98'  },
                { "{CBM-6}", '\x99'  },
                { "{CBM-7}", '\x9A'  },
                { "{CBM-8}", '\x9B'  },
                { "{CBM-9}", '\x29'  },
                { "{CBM-@}", '\xA4'  },
                { "{CBM-^}", '\xDE'  },
                { "{CBM-A}", '\xB0'  },
                { "{CBM-B}", '\xBF'  },
                { "{CBM-C}", '\xBC'  },
                { "{CBM-D}", '\xAC'  },
                { "{CBM-E}", '\xB1'  },
                { "{CBM-F}", '\xBB'  },
                { "{CBM-G}", '\xA5'  },
                { "{CBM-H}", '\xB4'  },
                { "{CBM-I}", '\xA2'  },
                { "{CBM-J}", '\xB5'  },
                { "{CBM-K}", '\xA1'  },
                { "{CBM-L}", '\xB6'  },
                { "{CBM-M}", '\xA7'  },
                { "{CBM-N}", '\xAA'  },
                { "{CBM-O}", '\xB9'  },
                { "{CBM-POUND}", '\xA8'  },
                { "{CBM-P}", '\xAF'  },
                { "{CBM-Q}", '\xAB'  },
                { "{CBM-R}", '\xB2'  },
                { "{CBM-S}", '\xAE'  },
                { "{CBM-T}", '\xA3'  },
                { "{CBM-UP ARROW}", '\xDE'  },
                { "{CBM-U}", '\xB8'  },
                { "{CBM-V}", '\xBE'  },
                { "{CBM-W}", '\xB3'  },
                { "{CBM-X}", '\xBD'  },
                { "{CBM-Y}", '\xB7'  },
                { "{CBM-Z}", '\xAD'  },
                { "{CLEAR}", '\x93'  },
                { "{CLR}", '\x93'  },
                { "{CONTROL-0}", '\x92'  },
                { "{CONTROL-1}", '\x90'  },
                { "{CONTROL-2}", '\x05'  },
                { "{CONTROL-3}", '\x1C'  },
                { "{CONTROL-4}", '\x9F'  },
                { "{CONTROL-5}", '\x9C'  },
                { "{CONTROL-6}", '\x1E'  },
                { "{CONTROL-7}", '\x1F'  },
                { "{CONTROL-8}", '\x9E'  },
                { "{CONTROL-9}", '\x12'  },
                { "{CONTROL-:}", '\x1B'  },
                { "{CONTROL-;}", '\x1D'  },
                { "{CONTROL-=}", '\x1F'  },
                { "{CONTROL-@}", '\x00'  },
                { "{CONTROL-A}", '\x01'  },
                { "{CONTROL-B}", '\x02'  },
                { "{CONTROL-C}", '\x03'  },
                { "{CONTROL-D}", '\x04'  },
                { "{CONTROL-E}", '\x05'  },
                { "{CONTROL-F}", '\x06'  },
                { "{CONTROL-G}", '\x07'  },
                { "{CONTROL-H}", '\x08'  },
                { "{CONTROL-I}", '\x09'  },
                { "{CONTROL-J}", '\x0A'  },
                { "{CONTROL-K}", '\x0B'  },
                { "{CONTROL-LEFT ARROW}", '\x06'  },
                { "{CONTROL-L}", '\x0C'  },
                { "{CONTROL-M}", '\x0D'  },
                { "{CONTROL-N}", '\x0E'  },
                { "{CONTROL-O}", '\x0F'  },
                { "{CONTROL-POUND}", '\x1C'  },
                { "{CONTROL-P}", '\x10'  },
                { "{CONTROL-Q}", '\x11'  },
                { "{CONTROL-R}", '\x12'  },
                { "{CONTROL-S}", '\x13'  },
                { "{CONTROL-T}", '\x14'  },
                { "{CONTROL-UP ARROW}", '\x1E'  },
                { "{CONTROL-U}", '\x15'  },
                { "{CONTROL-V}", '\x16'  },
                { "{CONTROL-W}", '\x17'  },
                { "{CONTROL-X}", '\x18'  },
                { "{CONTROL-Y}", '\x19'  },
                { "{CONTROL-Z}", '\x1A'  },
                { "{CR}", '\x0D'  },
                { "{CYAN}", '\x9F'  },
                { "{CYN}", '\x9F'  },
                { "{DELETE}", '\x14'  },
                { "{DEL}", '\x14'  },
                { "{DISH}", '\x08'  },
                { "{DOWN}", '\x11'  },
                { "{ENSH}", '\x09'  },
                { "{ESC}", '\x1B'  },
                { "{F10}", '\x82'  },
                { "{F11}", '\x84'  },
                { "{F12}", '\x8F'  },
                { "{F1}", '\x85'  },
                { "{F2}", '\x89'  },
                { "{F3}", '\x86'  },
                { "{F4}", '\x8A'  },
                { "{F5}", '\x87'  },
                { "{F6}", '\x8B'  },
                { "{F7}", '\x88'  },
                { "{F8}", '\x8C'  },
                { "{F9}", '\x80'  },
                { "{GRAY1}", '\x97'  },
                { "{GRAY2}", '\x98'  },
                { "{GRAY3}", '\x9B'  },
                { "{GREEN}", '\x1E'  },
                { "{GREY1}", '\x97'  },
                { "{GREY2}", '\x98'  },
                { "{GREY3}", '\x9B'  },
                { "{GRN}", '\x1E'  },
                { "{GRY1}", '\x97'  },
                { "{GRY2}", '\x98'  },
                { "{GRY3}", '\x9B'  },
                { "{HELP}", '\x84'  },
                { "{HOME}", '\x13'  },
                { "{INSERT}", '\x94'  },
                { "{INST}", '\x94'  },
                { "{LBLU}", '\x9A'  },
                { "{LEFT ARROW}", '\x5F'  },
                { "{LEFT}", '\x9D'  },
                { "{LF}", '\x0A'  },
                { "{LGRN}", '\x99'  },
                { "{LOWER CASE}", '\x0E'  },
                { "{LRED}", '\x96'  },
                { "{LT BLUE}", '\x9A'  },
                { "{LT GREEN}", '\x99'  },
                { "{LT RED}", '\x96'  },
                { "{ORANGE}", '\x81'  },
                { "{ORNG}", '\x81'  },
                { "{PI}", '\xFF'  },
                { "{POUND}", '\x5C'  },
                { "{PURPLE}", '\x9C'  },
                { "{PUR}", '\x9C'  },
                { "{RED}", '\x1C'  },
                { "{RETURN}", '\x0D'  },
                { "{REVERSE OFF}", '\x92'  },
                { "{REVERSE ON}", '\x12'  },
                { "{RGHT}", '\x1D'  },
                { "{RIGHT}", '\x1D'  },
                { "{RUN}", '\x83'  },
                { "{RVOF}", '\x92'  },
                { "{RVON}", '\x12'  },
                { "{RVS OFF}", '\x92'  },
                { "{RVS ON}", '\x12'  },
                { "{SHIFT RETURN}", '\x8D'  },
                { "{SHIFT-*}", '\xC0'  },
                { "{SHIFT-+}", '\xDB'  },
                { "{SHIFT-,}", '\x3C'  },
                { "{SHIFT--}", '\xDD'  },
                { "{SHIFT-.}", '\x3E'  },
                { "{SHIFT-/}", '\x3F'  },
                { "{SHIFT-0}", '\x30'  },
                { "{SHIFT-1}", '\x21'  },
                { "{SHIFT-2}", '\x22'  },
                { "{SHIFT-3}", '\x23'  },
                { "{SHIFT-4}", '\x24'  },
                { "{SHIFT-5}", '\x25'  },
                { "{SHIFT-6}", '\x26'  },
                { "{SHIFT-7}", '\x27'  },
                { "{SHIFT-8}", '\x28'  },
                { "{SHIFT-9}", '\x29'  },
                { "{SHIFT-:}", '\x5B'  },
                { "{SHIFT-;}", '\x5D'  },
                { "{SHIFT-@}", '\xBA'  },
                { "{SHIFT-^}", '\xDE'  },
                { "{SHIFT-A}", '\xC1'  },
                { "{SHIFT-B}", '\xC2'  },
                { "{SHIFT-C}", '\xC3'  },
                { "{SHIFT-D}", '\xC4'  },
                { "{SHIFT-E}", '\xC5'  },
                { "{SHIFT-F}", '\xC6'  },
                { "{SHIFT-G}", '\xC7'  },
                { "{SHIFT-H}", '\xC8'  },
                { "{SHIFT-I}", '\xC9'  },
                { "{SHIFT-J}", '\xCA'  },
                { "{SHIFT-K}", '\xCB'  },
                { "{SHIFT-L}", '\xCC'  },
                { "{SHIFT-M}", '\xCD'  },
                { "{SHIFT-N}", '\xCE'  },
                { "{SHIFT-O}", '\xCF'  },
                { "{SHIFT-POUND}", '\xA9'  },
                { "{SHIFT-P}", '\xD0'  },
                { "{SHIFT-Q}", '\xD1'  },
                { "{SHIFT-R}", '\xD2'  },
                { "{SHIFT-SPACE}", '\xA0'  },
                { "{SHIFT-S}", '\xD3'  },
                { "{SHIFT-T}", '\xD4'  },
                { "{SHIFT-UP ARROW}", '\xDE'  },
                { "{SHIFT-U}", '\xD5'  },
                { "{SHIFT-V}", '\xD6'  },
                { "{SHIFT-W}", '\xD7'  },
                { "{SHIFT-X}", '\xD8'  },
                { "{SHIFT-Y}", '\xD9'  },
                { "{SHIFT-Z}", '\xDA'  },
                { "{SPACE}", '\x20'  },
                { "{SRET}", '\x8D'  },
                { "{STOP}", '\x03'  },
                { "{SWLC}", '\x0E'  },
                { "{SWUC}", '\x8E'  },
                { "{TAB}", '\x09'  },
                { "{UP ARROW}", '\x5E'  },
                { "{UP/LO LOCK OFF}", '\x09'  },
                { "{UP/LO LOCK ON}", '\x08'  },
                { "{UPPER CASE}", '\x8E'  },
                { "{UP}", '\x91'  },
                { "{WHITE}", '\x05'  },
                { "{WHT}", '\x05'  },
                { "{YELLOW}", '\x9E'  },
                { "{YEL}", '\x9E'  },
            });

    public static AsmTextEncoding CbmScreen =>
        new("cbmscreen",
            new Dictionary<string, char>
            {
                { "@", '\0' },
                { "A", '\x01' },
                { "B", '\x02' },
                { "C", '\x03' },
                { "D", '\x04' },
                { "E", '\x05' },
                { "F", '\x06' },
                { "G", '\x07' },
                { "H", '\x08' },
                { "I", '\x09' },
                { "J", '\x0A' },
                { "K", '\x0B' },
                { "L", '\x0C' },
                { "M", '\x0D' },
                { "N", '\x0E' },
                { "O", '\x0F' },
                { "P", '\x10' },
                { "Q", '\x11' },
                { "R", '\x12' },
                { "S", '\x13' },
                { "T", '\x14' },
                { "U", '\x15' },
                { "V", '\x16' },
                { "W", '\x17' },
                { "X", '\x18' },
                { "Y", '\x19' },
                { "Z", '\x1A' },
                { "£", '\\' },
                { "π", '^' }, // π is $5e in unshifted
                { "↑", '^' }, // ↑ is $5e in shifted
                { "←", '_' },
                { "▌", '`' },
                { "▄", 'a' },
                { "▔", 'b' },
                { "▁", 'c' },
                { "▏", 'd' },
                { "▒", 'e' },
                { "▕", 'f' },
                { "◤", 'i' },
                { "├", 'k' },
                { "└", 'm' },
                { "┐", 'n' },
                { "▂", 'o' },
                { "┌", 'p' },
                { "┴", 'q' },
                { "┬", 'r' },
                { "┤", 's' },
                { "▎", 't' },
                { "▍", 'u' },
                { "▃", 'y' },
                { "✓", 'z' },
                { "┘", '}' },
                { "━", '@' },
                { "♠", 'A' },
                { "│", 'B' },
                { "╮", 'I' },
                { "╰", 'J' },
                { "╯", 'K' },
                { "╲", 'M' },
                { "╱", 'N' },
                { "●", 'Q' },
                { "♥", 'S' },
                { "╭", 'U' },
                { "╳", 'V' },
                { "○", 'W' },
                { "♣", 'X' },
                { "♦", 'Z' },
                { "┼", '[' },
                { "◥", '_' }
            },
            new Dictionary<string, char>
            {
                { "{CBM-*}", '\x5F' },
                { "{CBM-+}", '\x66' },
                { "{CBM--}", '\x5C' },
                { "{CBM-0}", '\x30' },
                { "{CBM-9}", '\x29' },
                { "{CBM-@}", '\x64' },
                { "{CBM-^}", '\x5E' },
                { "{CBM-A}", '\x70' },
                { "{CBM-B}", '\x7F' },
                { "{CBM-C}", '\x7C' },
                { "{CBM-D}", '\x6C' },
                { "{CBM-E}", '\x71' },
                { "{CBM-F}", '\x7B' },
                { "{CBM-G}", '\x65' },
                { "{CBM-H}", '\x74' },
                { "{CBM-I}", '\x62' },
                { "{CBM-J}", '\x75' },
                { "{CBM-K}", '\x61' },
                { "{CBM-L}", '\x76' },
                { "{CBM-M}", '\x67' },
                { "{CBM-N}", '\x6A' },
                { "{CBM-O}", '\x79' },
                { "{CBM-POUND}", '\x68' },
                { "{CBM-P}", '\x6F' },
                { "{CBM-Q}", '\x6B' },
                { "{CBM-R}", '\x72' },
                { "{CBM-S}", '\x6E' },
                { "{CBM-T}", '\x63' },
                { "{CBM-UP ARROW}", '\x5E' },
                { "{CBM-U}", '\x78' },
                { "{CBM-V}", '\x7E' },
                { "{CBM-W}", '\x73' },
                { "{CBM-X}", '\x7D' },
                { "{CBM-Y}", '\x77' },
                { "{CBM-Z}", '\x6D' },
                { "{LEFT ARROW}", '\x1F' },
                { "{PI}", '\x5E' },
                { "{POUND}", '\x1C' },
                { "{SHIFT-*}", '\x40' },
                { "{SHIFT-+}", '\x5B' },
                { "{SHIFT-,}", '\x3C' },
                { "{SHIFT--}", '\x5D' },
                { "{SHIFT-.}", '\x3E' },
                { "{SHIFT-/}", '\x3F' },
                { "{SHIFT-0}", '\x30' },
                { "{SHIFT-1}", '\x21' },
                { "{SHIFT-2}", '\x22' },
                { "{SHIFT-3}", '\x23' },
                { "{SHIFT-4}", '\x24' },
                { "{SHIFT-5}", '\x25' },
                { "{SHIFT-6}", '\x26' },
                { "{SHIFT-7}", '\x27' },
                { "{SHIFT-8}", '\x28' },
                { "{SHIFT-9}", '\x29' },
                { "{SHIFT-:}", '\x1B' },
                { "{SHIFT-;}", '\x1D' },
                { "{SHIFT-@}", '\x7A' },
                { "{SHIFT-^}", '\x5E' },
                { "{SHIFT-A}", '\x41' },
                { "{SHIFT-B}", '\x42' },
                { "{SHIFT-C}", '\x43' },
                { "{SHIFT-D}", '\x44' },
                { "{SHIFT-E}", '\x45' },
                { "{SHIFT-F}", '\x46' },
                { "{SHIFT-G}", '\x47' },
                { "{SHIFT-H}", '\x48' },
                { "{SHIFT-I}", '\x49' },
                { "{SHIFT-J}", '\x4A' },
                { "{SHIFT-K}", '\x4B' },
                { "{SHIFT-L}", '\x4C' },
                { "{SHIFT-M}", '\x4D' },
                { "{SHIFT-N}", '\x4E' },
                { "{SHIFT-O}", '\x4F' },
                { "{SHIFT-POUND}", '\x69' },
                { "{SHIFT-P}", '\x50' },
                { "{SHIFT-Q}", '\x51' },
                { "{SHIFT-R}", '\x52' },
                { "{SHIFT-SPACE}", '\x60' },
                { "{SHIFT-S}", '\x53' },
                { "{SHIFT-T}", '\x54' },
                { "{SHIFT-UP ARROW}", '\x5E' },
                { "{SHIFT-U}", '\x55' },
                { "{SHIFT-V}", '\x56' },
                { "{SHIFT-W}", '\x57' },
                { "{SHIFT-X}", '\x58' },
                { "{SHIFT-Y}", '\x59' },
                { "{SHIFT-Z}", '\x5A' },
                { "{SPACE}", '\x20' },
                { "{UP ARROW}", '\x1E' }
            });

    public static AsmTextEncoding AtaScreen =>
        new("atascreen",
            new Dictionary<string, char>
            {
                { " ", '\0' },
                { "!", '\x01' },
                { "\"", '\x02' },
                { "#", '\x03' },
                { "$", '\x04' },
                { "%", '\x06' },
                { "&", '\x07' },
                { "'", '\x08' },
                { "(", '\x09' },
                { ")", '\x0A' },
                { "*", '\x0B' },
                { "+", '\x0C' },
                { ",", '\x0D' },
                { "-", '\x0E' },
                { ".", '\x0F' },
                { "/", '\x10' },
                { "0", '\x11' },
                { "1", '\x12' },
                { "2", '\x13' },
                { "3", '\x14' },
                { "4", '\x15' },
                { "5", '\x16' },
                { "6", '\x17' },
                { "7", '\x18' },
                { "8", '\x19' },
                { "9", '\x1A' },
                { ":", '\x1B' },
                { ";", '\x1C' },
                { "<", '\x1D' },
                { "=", '\x1E' },
                { ">", '\x1F' },
                { "?", '\x20' },
                { "@", '\x21' },
                { "A", '\x22' },
                { "B", '\x23' },
                { "C", '\x24' },
                { "D", '\x25' },
                { "E", '\x26' },
                { "F", '\x27' },
                { "G", '\x28' },
                { "H", '\x29' },
                { "I", '\x2A' },
                { "J", '\x2B' },
                { "K", '\x2C' },
                { "L", '\x2D' },
                { "M", '\x2E' },
                { "N", '\x2F' },
                { "O", '\x30' },
                { "P", '\x31' },
                { "Q", '\x32' },
                { "R", '\x33' },
                { "S", '\x34' },
                { "T", '\x35' },
                { "U", '\x36' },
                { "V", '\x37' },
                { "W", '\x38' },
                { "X", '\x39' },
                { "Y", '\x3A' },
                { "Z", '\x3B' },
            },
            new Dictionary<string, char>());
}
