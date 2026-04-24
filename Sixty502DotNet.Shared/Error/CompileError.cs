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

namespace Sixty502DotNet.Shared.Error;

public readonly struct CompileError : IEquatable<CompileError>
{
    public void ReportToConsole(bool suppressHighlighting)
    {
        if (Inclusions.Count > 0)
        {
            Inclusions.ForEach(incl =>
            {
                Console.Error.WriteLine(incl.IsMacro
                    ? $"In macro expanded at {incl.Name}({incl.Line}:{incl.Column + 1})"
                    : $"In file included from {incl.Name}({incl.Line}:{incl.Column + 1})");
            });
        }
        if (!IsFatal)
        {
            Console.Error.Write($"{Path}({Line}:{Column + 1}): ");
        }
        if (IsError)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(IsFatal ? "fatal " : string.Empty);
            Console.Error.Write("error: ");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Error.Write("warning: ");
        }
        Console.ResetColor();
        Console.Error.Write($"{Message}.");
        if (suppressHighlighting || string.IsNullOrEmpty(LineText))
        {
            Console.Error.WriteLine();
            return;
        }
        Console.Error.WriteLine($"\n {Line} | {LineText}");
        Console.Error.Write(' ');
        Console.Error.Write(new string(' ', Line.ToString().Length));
        Console.Error.Write(" | ");
        for (var i = 0; i < Column && i < LineText.Length; i++)
        {
            Console.Error.Write(LineText[i] == '\t' ? '\t' : ' ');
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Error.Write(Length > 0 ? new string('^', Length) : '^');
        Console.ResetColor();
        if (!string.IsNullOrEmpty(BlockBeginningLineText))
        {
            Console.Error.WriteLine("\nBlock declared here:");
            Console.Error.Write($" {BlockBeginningLine} | ");
            Console.Error.Write(BlockBeginningLineText);
            Console.Error.WriteLine();
            Console.Error.Write(new string(' ', BlockBeginningLine.ToString().Length + 1));
            Console.Error.Write(" | ");
            for (var i = 0; i < BlockBeginningColumn; i++)
            {
                Console.Error.Write(LineText[i] == '\t' ? '\t' : ' ');
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (BlockBeginningLength < 1)
            {
                Console.Error.Write('^');
            }
            else
            {
                Console.Error.Write(new string('^', BlockBeginningLength));
            }
            Console.ResetColor();
        }
        Console.WriteLine();
    }
    
    public string Report(bool suppressHighlighting)
    {
        var sb = new StringBuilder();
        if (Inclusions.Count > 0)
        {
            Inclusions.ForEach(incl =>
            {
                if (incl.IsMacro)
                    sb.AppendLine($"In macro expanded at {incl.Name}({incl.Line}:{incl.Column + 1})");
                else
                    sb.AppendLine($"In file included from {incl.Name}({incl.Line}:{incl.Column + 1})");
            });
        }
        if (!IsFatal)
        {
            sb.Append($"{Path}({Line}:{Column + 1}): ");
        } 
        sb.Append(IsFatal ? IsError ? "fatal " : string.Empty : string.Empty);
        sb.Append(IsError ? "error: " : "warning: ");

        sb.Append($"{Message}.");
        if (suppressHighlighting || string.IsNullOrEmpty(LineText))
        {
            return sb.ToString();
        }
        sb.AppendLine($"\n {Line} | {LineText}");
        sb.Append(' ');
        sb.Append(new string(' ', Line.ToString().Length));
        sb.Append(" | ");
        for (var i = 0; i < Column; i++)
        {
            sb.Append(LineText[i] == '\t' ? '\t' : ' ');
        }
        sb.Append(Length > 0 ? new string('^', Length) : '^');
        if (!string.IsNullOrEmpty(BlockBeginningLineText))
        {
            sb.AppendLine("\nBlock declared here:");
            sb.Append($" {BlockBeginningLine} | ");
            sb.Append(BlockBeginningLineText);
            sb.AppendLine();
            sb.Append(new string(' ', BlockBeginningLine.ToString().Length + 1));
            sb.Append(" | ");
            for (var i = 0; i < BlockBeginningColumn; i++)
            {
                sb.Append(LineText[i] == '\t' ? '\t' : ' ');
            }
            sb.Append(new string('^', BlockBeginningLength));
        }
        return sb.ToString();
    }
    
    public bool Equals(CompileError other) 
        => Path == other.Path && 
           Line == other.Line && 
           Column == other.Column && 
           IsError == other.IsError && 
           IsFatal == other.IsFatal && 
           Message == other.Message;

    public override bool Equals(object? obj) 
        => obj is CompileError other && Equals(other);

    public override int GetHashCode() 
        => HashCode.Combine(Path, Line, Column, IsError, IsFatal, Message);
    
    public static bool operator ==(CompileError left, CompileError right) => left.Equals(right);

    public static bool operator !=(CompileError left, CompileError right) => !(left == right);

    public string Path { get; init; }
    
    public int Line { get; init; }
    
    public int Column { get; init; }
    
    public int Length { get; init; }
    
    public bool IsError { get; init; }
    
    public bool IsFatal { get; init; }
    
    public string Message { get; init; }
    
    public string LineText { get; init; }
    
    public int BlockBeginningLine { get; init; }
    
    public int BlockBeginningColumn { get; init; }
    
    public int BlockBeginningLength { get; init; }
    
    public List<Inclusion> Inclusions { get; init; }
    
    public string BlockBeginningLineText { get; init; }
}