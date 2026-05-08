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

using Sixty502DotNet.Shared.Eval.String;
using System.Globalization;

namespace Sixty502DotNet.Shared.Eval;

public sealed class ValueFormatter(TextEncodingCollection encoding) : ICustomFormatter, IFormatProvider
{
    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        if (arg is not Value value)
        {
            return HandleNonValue(format, arg);
        }
        if (string.IsNullOrEmpty(format))
        {
            return value.AsString(encoding.CurrentTextEncoding);
        }
        switch (format[0])
        {   
            case 'x':
            case 'X':
                if (format.Length <= 1) return string.Format(format, value.AsInt(encoding));
                return format[1] switch
                {
                    '2' => (value.AsInt(encoding) & 0xff).ToString(format),
                    '4' => (value.AsInt(encoding) & 0xffff).ToString(format),
                    '6' => (value.AsInt(encoding) & 0xffffff).ToString(format),
                    _ => value.AsInt(encoding).ToString(format)
                };
            case 'd':
            case 'D':
            case 'e':
            case 'E':
            case 'f':
            case 'F':
            case 'g':
            case 'G':
            case 'n':
            case 'N':
            case 'p':
            case 'P':
            case 'r':
            case 'R':
                return value.AsDouble(encoding).ToString(format);
            default:
                return format;
            
        }
    }

    public object? GetFormat(Type? formatType) 
        => typeof(ICustomFormatter) == formatType ? this : null;

    private static string HandleNonValue(string? format, object? arg)
    {
        if (arg is IFormattable fmt)
        {
            return fmt.ToString(format, CultureInfo.CurrentCulture);
        }
        return arg?.ToString() ?? string.Empty;
    }
}