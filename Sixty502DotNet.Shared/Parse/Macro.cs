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

using Sixty502DotNet.Shared.Lex;
using System.Text;

namespace Sixty502DotNet.Shared.Parse;

public sealed class Macro(Token token, Source templateSource, StringComparer comparer)
{
    private readonly Dictionary<string, int> _namedParameterIndeces = new(comparer);

    private readonly Dictionary<int, string?> _parameterDefaultValues = new();
    
    private readonly List<KeyValuePair<int,int>> _parameterLocations = [];
    

    public bool AddParameter(string name, int index, string? defaultValue)
    {   
        _ = _parameterDefaultValues.TryAdd(index, defaultValue);
        return _namedParameterIndeces.TryAdd(name, index);
    }

    public void AddNumberedParamLocation(int parameterIndex, int column) 
        => _parameterLocations.Add(new KeyValuePair<int,int>(column, parameterIndex));

    public bool AddNamedParameterLocation(string name, int column)
    {
        if (!_namedParameterIndeces.TryGetValue(name, out var index))
        {
            return false;
        }
        AddNumberedParamLocation(index, column);
        return true;
    }

    public string Expand(IList<string> args, ReadOnlySpan<char> argList)
    {
        var sb = new StringBuilder();
        var column = Start;
        foreach (var paramLoc in _parameterLocations)
        {
            if (paramLoc.Key > 0)
            {
                sb.Append(templateSource.Text.Slice(column, paramLoc.Key - column));
            }
            int argIndex;
            var paramType = templateSource.Text[paramLoc.Key + 1];
            if (paramType.IsIdentHead())
            {
                var end = paramLoc.Key + 2;
                while (templateSource.Text[end].IsIdent()) end++;
                var namedParam = templateSource.Text
                    .Slice(paramLoc.Key + 1, end - paramLoc.Key - 1)
                    .ToString();
                argIndex = _namedParameterIndeces[namedParam];
                column = end;
            }
            else
            {
                argIndex = paramLoc.Value - 1;
                column = paramLoc.Key + paramLoc.Value.ToString().Length + 1;
            }
            string substitution;
            if (paramType == '*')
            {
                substitution = argList.ToString();
            }
            else if (argIndex < args.Count)
            {
                substitution = args[argIndex];
            }
            else if (argIndex < _parameterDefaultValues.Count)
            {
                substitution = _parameterDefaultValues[argIndex] ?? throw new ArgumentException();
            }
            else
            {
                throw new ArgumentException();
            }
            sb.Append(substitution);
        }
        if (column < End)
        {
            sb.Append(templateSource.Text.Slice(column, End - column));
        }
        return sb.ToString();
    }

    public Token Token { get; } = token;
    
    public int StartLine { get; set; }
    
    public int Start { get; set; }

    public int End { get; set; }

    public bool Expanded { get; set; }
}