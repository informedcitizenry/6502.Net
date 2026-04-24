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

/*
 * challenge of representing/working with strings in our system:
 *
 * - strings can be just plain old ASCII/Unicode type literals, like "Hello World"
 * - strings can also be explicitly marked as a non-Unicode encoded type, e.g. p"Hello World"
 *   which is a PETSCII string
 * - furthermore, programmers can explicitly change how strings are encoded into the final binary
 *   using the `.encoding` and `.map` commands in their assembly source code. So how do we represent
 *   a non-Unicode encoded string then? For example:
 *
 *     .encoding "foo"
 *     .map "H", $08
 *
 *     .string p"H"
 *
 *  How does the string literal get assembled? As $48 (PETSCII), or $08 ("foo" encoding)? Finally,
 *  what do we do about expressions that concatenate strings? How do we represent/asssemble them?
 *
 *  For example:
 *
 *     fizz = p"FIZZ"
 *     buzz = "BUZZ"
 *
 *     .string fizz + buzz ; is the final concatenated string encoded as PETSCII or as Unicode?
 *
 *  It seems like the most straightforward approach is implementing the string as an array of
 *  strings tagged with their encodings. And the only strings whose encodings are affected by
 *  the `.encoding` and `map` commands are those where an explicit encoding marker was not
 *  used when they were declared/created. In the example above, the fizz part of the
 *  concatenated string will be encoded as PETSCII while the buzz will be encoded either as
 *  ASCII/UTF8 or whatever the programmer has globally specified (e.g., .encoding "foo").
 *
 *  In order to achieve this, we need to think of strings not just as a C# string, but as a
 *  text encoding and a C# string. So we do that by tagging an encoding type to the string,
 *  a data structure we are calling a EncodingTaggedString.
 *
 *   A combination of one or more EncodingTaggedString objects then is a complete string value for
 *   purposes of final code assembly, with the strings' encoding types governing how final
 *   bytes are emitted to machine language.
 */

public enum TextEncodingType
{
    Default,
    Utf8,
    Utf16,
    Utf32,
    AtaScreen,
    CbmScreen,
    Petscii
}

public readonly struct EncodingTaggedString(TextEncodingType encodingType, string text) 
    : IEquatable<EncodingTaggedString>, IComparable<EncodingTaggedString>
{
    public TextEncodingType TextEncoding { get; } = encodingType;

    public string Text { get; } = text;

    public bool Equals(EncodingTaggedString other) 
        => TextEncoding == other.TextEncoding && Text.Equals(other.Text);

    public int CompareTo(EncodingTaggedString other) 
        => Comparer<string>.Default.Compare(Text, other.Text);

    public override bool Equals(object? obj) => obj is EncodingTaggedString other && Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)TextEncoding, Text);

    public static bool operator ==(EncodingTaggedString left, EncodingTaggedString right) 
        => left.Equals(right);

    public static bool operator !=(EncodingTaggedString left, EncodingTaggedString right) 
        => !(left == right);
}

public sealed class AsmString : IEquatable<AsmString>, IComparable<AsmString>
{
    public AsmString() => Strings = [];

    public AsmString(TextEncodingType type, string text)
        => Strings = [new EncodingTaggedString(type, text)];
    
    public List<EncodingTaggedString> Strings { get; }

    public int IndexOf(char c)
    {
        for (var i = 0; i < Strings.Count; i++)
        {
            var indexOf = Strings[i].Text.IndexOf(c);
            if (indexOf >= 0) return indexOf;
        }
        return -1;
    }
    
    public AsmString Substring(int start, int length)
    {
        var substr = new AsmString();
        for (var i = 0; i < Strings.Count && length > 0; i++)
        {
            if (start > Strings[i].Text.Length) continue;
            if (length < Strings[i].Text.Length)
            {
                substr.Strings.Add(new EncodingTaggedString(Strings[i].TextEncoding, Strings[i].Text.Substring(start, length)));
                return substr;
            }
            substr.Strings.Add(new EncodingTaggedString(Strings[i].TextEncoding, Strings[i].Text[start..]));
            start = 0;
            length -= Strings[i].Text.Length;
        }
        return substr;
    }

    public IEnumerable<char> ToArray(Encoding defaultEncoding)
    {
        var charArray = new List<char>();
        foreach (var segment in Strings)
        {
            var encoding = segment.TextEncoding switch
            {
                TextEncodingType.AtaScreen => AsmTextEncoding.AtaScreen,
                TextEncodingType.CbmScreen => AsmTextEncoding.CbmScreen,
                TextEncodingType.Petscii => AsmTextEncoding.Petscii, 
                TextEncodingType.Utf8 => Encoding.UTF8,
                TextEncodingType.Utf16 => Encoding.Unicode,
                TextEncodingType.Utf32 => Encoding.UTF32,
                _ => defaultEncoding
            };
            var charBytes = encoding.GetBytes(segment.Text);
            charArray.AddRange(encoding.GetChars(charBytes));
        }
        return charArray;
    }
    
    public string ToString(Encoding defaultEncoding)
    {
        var sb = new StringBuilder();
        foreach (var segment in Strings)
        {
            var encoding = segment.TextEncoding switch
            {
                TextEncodingType.AtaScreen => AsmTextEncoding.AtaScreen,
                TextEncodingType.CbmScreen => AsmTextEncoding.CbmScreen,
                TextEncodingType.Petscii => AsmTextEncoding.Petscii, 
                TextEncodingType.Utf8 => Encoding.UTF8,
                TextEncodingType.Utf16 => Encoding.Unicode,
                TextEncodingType.Utf32 => Encoding.UTF32,
                _ => defaultEncoding
            };
            var segBytes =  encoding.GetBytes(segment.Text);
            var segText = encoding.GetString(segBytes);
            sb.Append(segText);
        }
        return sb.ToString();
    }

    public long? ToInt(Encoding defaultEncoding)
    {
        long num = 0;
        foreach (var segment in Strings)
        {
            var encoding = segment.TextEncoding switch
            {
                TextEncodingType.AtaScreen => AsmTextEncoding.AtaScreen,
                TextEncodingType.CbmScreen => AsmTextEncoding.CbmScreen,
                TextEncodingType.Petscii => AsmTextEncoding.Petscii,
                TextEncodingType.Utf8 => Encoding.UTF8,
                TextEncodingType.Utf16 => Encoding.Unicode,
                TextEncodingType.Utf32 => Encoding.UTF32,
                _ => defaultEncoding
            };
            var bytes = encoding.GetBytes(segment.Text);
            switch (bytes.Length)
            {
                case > 8:
                    return null;
                case < 8:
                    bytes = bytes.Concat(new byte[8 - bytes.Length]).ToArray();
                    break;
            }
            try
            {
                num += BitConverter.ToInt64(bytes, 0);
            }
            catch
            {
                return null;
            }
        }
        return num;
    }
    
    public override string ToString() => ToString(Encoding.UTF8);
    
    public int Length => Strings.Sum(segment => segment.Text.Length);

    public bool Equals(AsmString? other)
    {
        if (other is null || other.Strings.Count != Strings.Count) return false;
        if (ReferenceEquals(this, other)) return true;
        return Strings.SequenceEqual(other.Strings);
    }

    public int CompareTo(AsmString? other) 
        => Comparer<string>.Default.Compare(ToString(), other?.ToString());

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((AsmString)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        for (var i = 0; i < Strings.Count; i++)
        {
            hashCode.Add(Strings[i].Text.GetHashCode());
            hashCode.Add(Strings[i].TextEncoding.GetHashCode());
        }
        return hashCode.ToHashCode();
    }
}