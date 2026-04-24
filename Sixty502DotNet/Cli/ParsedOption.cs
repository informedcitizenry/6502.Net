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

namespace Sixty502DotNet.CLI;

public class ParsedOption
{
    private readonly object? _value;

    public ParsedOption(Option option, int value)
    {
        _value = value;
        Option = option;
    }

    public ParsedOption(Option option, bool value)
    {
        _value = value;
        Option = option;
    }
    
    
    public ParsedOption(Option option, string value)
    {
        _value = value;
        Option = option;
    }

    public ParsedOption(Option option, List<string> value)
    {
        _value = value;
        Option = option;
    }
    
    public Option Option { get; }

    public string GetStringValue() 
        => Option.Type == OptionType.String ? (string?)_value ?? string.Empty : string.Empty;

    public int GetIntValue() 
        => Option.Type == OptionType.Integer ? (int?)_value ?? -1 : -1;

    public bool GetBoolValue() 
        => Option.Type == OptionType.Boolean && ((bool?)_value ?? false);

    public List<string> GetListValue() 
        => Option.Type == OptionType.List ? (List<string>?)_value ?? [] : [];

    public override string ToString()
    {
        return Option.Type switch
        {
            OptionType.Boolean => GetBoolValue().ToString().ToLower(),
            OptionType.Integer => GetIntValue().ToString(),
            OptionType.String => GetStringValue(),
            OptionType.List => string.Join(':', GetListValue()),
            _ => string.Empty
        };
    }
}