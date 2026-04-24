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

using Sixty502DotNet.Shared.Arch;

namespace Sixty502DotNet.Shared.Lex;

public record struct LexerState
{
    public LexerState()
    {
        
    }

    public LexerState(LexerState other)
    {
        Line = other.Line;
        LineStart = other.LineStart;
        AdjustedLineStart = other.AdjustedLineStart;
        Cursor = new Location(other.Cursor.Start, other.Cursor.End);
        ColumnCursor = new Location(other.ColumnCursor.Start, other.ColumnCursor.End);
        PreviousChar = other.PreviousChar;
        CurrentChar = other.CurrentChar;
        LastType = other.LastType;
        Cpu = other.Cpu;
        OpenBraces = other.OpenBraces;
        SavedGroups = new Stack<int>(other.SavedGroups);
        Groups = other.Groups;
        LexCommand = other.LexCommand;
        LexPercentAsOperator = other.LexPercentAsOperator;
        InterpolatedStringIsMultiline = other.InterpolatedStringIsMultiline;
        InterpolationMode = other.InterpolationMode;
        SavedInterpolatedStringsModes = new Stack<bool>(other.SavedInterpolatedStringsModes);
    }

    public override string ToString()
    {
        var previous = PreviousChar.IsVerticalWhitespace() ? "\\n" : PreviousChar.ToString();
        var current = CurrentChar.IsVerticalWhitespace() ? "\\n" : CurrentChar.ToString();
        return $"Line: {Line}, Column: {ColumnCursor}, Cursor: {Cursor},  PreviousChar: {previous}, CurrentChar: {current}";
    }
    
    public char PreviousChar { get; set; } = '\0';

    public char CurrentChar { get; set; } = '\0';

    public int Line { get; set; } = 1;

    public int LineStart { get; set; } = 0;

    public int AdjustedLineStart { get; set; } = 0;

    public TokenType LastType { get; set; } = TokenType.Eof;
    
    public Stack<int> SavedGroups { get; } = new();
    
    public Location Cursor { get; } = new(0, 0);
    
    public Location ColumnCursor { get; } = new(0, 0);

    public int Groups { get; set; } = 0;
    
    public int OpenBraces { get; set; } = 0;

    public bool LexCommand { get; set; } = true;

    public bool LexPercentAsOperator { get; set; } = false;

    public Cpu Cpu { get; set; } = Cpu.M6502;

    public Stack<bool> SavedInterpolatedStringsModes { get; } = new();
    
    public bool InterpolatedStringIsMultiline { get; set; } = false;

    public bool InterpolationMode { get; set; } = false;
};