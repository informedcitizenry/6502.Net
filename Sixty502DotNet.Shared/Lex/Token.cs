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

namespace Sixty502DotNet.Shared.Lex;

public record Location(int Start, int End)
{
    public int Start { get; set; } = Start;

    public int End { get; set; } = End;
}

public record Inclusion(string Name, int Line, int Column, bool IsMacro = false)
{
    public string Name { get; } = Name;

    public int Line { get; } = Line;

    public int Column { get; } = Column;

    public bool IsMacro { get; } = IsMacro;
}

public readonly struct Token
{


    public Token(string sourcePath, TokenType type, string text)
    {
        Source = new Source(sourcePath, text);
        Type = type;
        Location = new Location(0, text.Length);
        Line = 1;
        Column = 0;
        AdjustedColumn = 0;
        LineTextStart = 0;
        Inclusions = [];
    }

    public Token(Token other, TokenType type, string text)
    {
        Type = type;
        Location = new Location(0, text.Length);
        Line = other.Line;
        Column = other.Column;
        AdjustedColumn = other.AdjustedColumn;
        LineTextStart = other.LineTextStart;
        Inclusions = other.Inclusions;
        Source = new Source(other.Source.Name, text);
    }
    
    public Token
    (
        Source source,
        Location location,
        TokenType tokenType,
        int line,
        int column,
        int adjustedColumn,
        int lineStart
    ) : this(source, location, tokenType, line, column, adjustedColumn, lineStart, [])
    {
        
    }
        
    public Token
    (
        Source source, 
        Location location, 
        TokenType tokenType,
        int line, 
        int column,
        int adjustedColumn,
        int lineStart,
        List<Inclusion> inclusions
    )
    {
        Type = tokenType;
        Line = line;
        Column = column;
        AdjustedColumn = adjustedColumn;
        Source = source;
        Location = new Location(location.Start, location.End);
        LineTextStart = lineStart;
        Inclusions = inclusions;
    }
    
    public Token CopyWithType(TokenType type)
        => new(Source, Location, type, Line, Column, AdjustedColumn, LineTextStart, Inclusions);

    public TokenType Type { get; } = TokenType.Eof;

    public Source Source { get; } = new();

    public Location Location { get; } = new(0, 0);

    public int Line { get; } = 1;

    public int Column { get; } = 0;

    public int AdjustedColumn { get; } = 0;

    public ReadOnlySpan<char> Text => Source.Text[Location.Start..Location.End];
    
    public int LineTextStart { get; } = 0;

    public ReadOnlySpan<char> GetLineText()
    {
        var end = Location.Start;
        while (end < Source.Text.Length && 
               !Source.Text[end].IsVerticalWhitespace())
        {
            end++;
        }
        if (Source.Text[end - 1] == '\\' && 
            (end == Source.Text.Length || Source.Text[end].IsVerticalWhitespace()))
        {
            end--;
        }
        if (end - Location.Start > 80)
        {
            end = Location.Start + 80;
        }
        return Source.Text[LineTextStart..end];
    }

    public string LocationInfo => $"{Source.Name}({Line}):";
    
    public List<Inclusion> Inclusions { get; }
    
    public override string ToString()
        => $"{Source.Name}:{Line}:{Column}:{Text} ({Type.Stringified()})";

}