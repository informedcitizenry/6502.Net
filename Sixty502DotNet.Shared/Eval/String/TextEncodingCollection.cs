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
using Sixty502DotNet.Shared.Error;

namespace Sixty502DotNet.Shared.Eval.String;

public class TextEncodingCollection
{
    private readonly Dictionary<string, AsmTextEncoding> _encodings;
    private AsmTextEncoding _currentTextEncoding;

    public TextEncodingCollection(StringComparer comparer)
    {
        _currentTextEncoding = new AsmTextEncoding("none");
        _encodings = new  Dictionary<string, AsmTextEncoding>(comparer)
        {
            { "none", _currentTextEncoding },
            { "atascreen", AsmTextEncoding.AtaScreen },
            { "cbmscreen", AsmTextEncoding.CbmScreen },
            { "petscii", AsmTextEncoding.Petscii },
        };
        
    }

    public TextEncodingType EncodingType => _currentTextEncoding.EncodingName switch
    {
        "atascreen" => TextEncodingType.AtaScreen,
        "cbmscreen" => TextEncodingType.CbmScreen,
        "petscii" => TextEncodingType.Petscii,
        _ => TextEncodingType.Default
    };
    
    public int GetEncodedValue(string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentNullException(nameof(s), "String cannot be null.");

        var bytes = _currentTextEncoding.GetBytes(s);
        switch (bytes.Length)
        {
            case < 4:
                Array.Resize(ref bytes, 4);
                break;
            case > 4:
                throw new ArgumentException($"Size of string literal \"{s}\" exceeds integer value");
        }
        return BitConverter.ToInt32(bytes, 0);
    }

    public byte[] GetEncodedBytes(AsmString s)
    {
        var bytes = new List<byte>();
        for (var i = 0; i < s.Strings.Count; i++)
        {
            var segment = s.Strings[i];
            var encoding = segment.TextEncoding switch
            {
                TextEncodingType.AtaScreen => AsmTextEncoding.AtaScreen,
                TextEncodingType.CbmScreen => AsmTextEncoding.CbmScreen,
                TextEncodingType.Petscii => AsmTextEncoding.Petscii,
                TextEncodingType.Utf8 => Encoding.UTF8,
                TextEncodingType.Utf16 => Encoding.Unicode,
                TextEncodingType.Utf32 => Encoding.UTF32,
                _ => _currentTextEncoding
            };
            bytes.AddRange(encoding.GetBytes(segment.Text));
        }
        return bytes.ToArray();
    }

    public long GetEncodedValue(AsmString s)
    {
        var bytes = GetEncodedBytes(s);
        switch (bytes.Length)
        {
            case < 8:
                Array.Resize(ref bytes, 8);
                break;
            case > 8:
                throw new AsmEncodingError($"Size of string literal \"{s}\" exceeds integer value");
        }
        return BitConverter.ToInt64(bytes, 0);
    }

    public int GetEncodedValue(char c) 
        => GetEncodedValue(new string(c, 1));

    public void Map(string mapping, char code)
    {
        // the default encoding cannot be changed
        if (_currentTextEncoding.EncodingName.Equals("none"))
        {
            return;
        }
        _currentTextEncoding.Map(mapping, code);
    }

    public void Map(string start, string end, char startCode)
    {
        // the default encoding cannot be changed
        if (_currentTextEncoding.EncodingName.Equals("none"))
        {
            return;
        }
        _currentTextEncoding.MapRange(start, end, startCode);
    }
    
    public void Unmap(string mapping)
    {
        // the default encoding cannot be changed
        if (_currentTextEncoding.EncodingName.Equals("none"))
        {
            return;
        }
        _currentTextEncoding.Unmap(mapping);
    }

    public void Unmap(string start, string end)
    {
        // the default encoding cannot be changed
        if (_currentTextEncoding.EncodingName.Equals("none"))
        {
            return;
        }
        _currentTextEncoding.UnmapRange(start, end);
    }
    
    public void SelectDefaultEncoding()
    {
        _currentTextEncoding = _encodings["none"];
    } 
    
    public Encoding CurrentTextEncoding => _currentTextEncoding;
    
    public void SelectEncoding(string encodingName)
    {
        if (!_encodings.TryGetValue(encodingName, out var encoding))
        {
            _currentTextEncoding = new AsmTextEncoding(encodingName);
            _encodings[encodingName] = _currentTextEncoding;
            return;
        }
        _currentTextEncoding = encoding;
    }
}