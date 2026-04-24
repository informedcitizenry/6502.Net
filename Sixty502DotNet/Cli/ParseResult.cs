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

public class ParseResult
(
    string help, 
    string commandName,
    Dictionary<string, ParsedOption> options, 
    List<string> errors
)
{
    public IList<string> GetListOption(params string[] options)
    {
        var list = new List<string>();
        foreach (var option in options)
        {
            if (!Options.TryGetValue(option, out var parsedOption)) continue;
            list.AddRange(parsedOption.GetListValue());
        }
        return list;
    }

    public int GetIntOption(string option, int defaultValue = 0) 
        => Options.TryGetValue(option, out var parsedOption) ? parsedOption.GetIntValue() : defaultValue;

    public string? GetStringOption(params string[] options)
    {
        foreach (var option in options)
        {
            if (!Options.TryGetValue(option, out var parsedOption)) continue;
            var strOption =  parsedOption.GetStringValue();
            return !string.IsNullOrEmpty(strOption) ? strOption : null;
        }
        return null;
    }
    
    public bool GetBoolOption(params string[] options)
    {
        foreach (var option in options)
        {
            if (Options.TryGetValue(option, out var parsedOption))
            {
                return parsedOption.GetBoolValue();
            }
        }
        return false;
    }

    public string CommandName { get; } = commandName;

    public List<string> Errors { get; } = errors;
    
    public Dictionary<string, ParsedOption> Options { get; } = options;

    public string Help { get; } = help;
}