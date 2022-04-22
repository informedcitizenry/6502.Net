//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.IO;
namespace Sixty502DotNet
{
    public class Error : Exception, IEquatable<Error>
    {
        public Error(string message)
            : this(null, message, null, null, -1, -1, true)
        {
        }

        public Error(string message, bool isError)
            : this(null, message, null, null, -1, -1, isError)
        {
        }

        public Error(ParserRuleContext context, string message)
            : this(context, message, (Token)context.Start, ((Token)context.Start)?.Filename, context.Start!.Line, context.Start.Column, true)
        {
        }

        public Error(ParserRuleContext context, string message, bool isError)
            : this(context, message, (Token)context.Start, ((Token)context.Start).Filename, context.Start.Line, context.Start.Column, isError)
        {
        }

        public Error(ParserRuleContext context, string message, Token token)
            : this(context, message, token, token.TokenSource.SourceName, token.Line, token.Column, true)
        {
        }

        public Error(ParserRuleContext context, string message, Token token, bool isError)
            : this(context, message, token, token.TokenSource.SourceName, token.Line, token.Column, isError)
        {
        }

        public Error(ParserRuleContext? context, string message, Token? token, string? source, int lineNumber, int position)
            : this(context, message, token, source, lineNumber, position, true)
        {
        }

        public Error(ParserRuleContext? context, string message, Token? token, string? source, int lineNumber, int position, bool isError)
            : base(message)
        {
            Context = context;
            Token = token;
            SourceName = source;
            LineNumber = lineNumber;
            Position = position + 1;
            IsError = isError;
        }

        public void Print(bool noHighlighting, TextWriter writer)
        {
            if (!string.IsNullOrEmpty(Token?.IncludedFrom?.Item1))
            {
                writer.WriteLine($"Included from {Token.IncludedFrom.Item1}:{Token.IncludedFrom.Item2}:");
            }
            if (!string.IsNullOrEmpty(Token?.MacroInvoke?.Item1))
            {
                writer.WriteLine($"Expanded at {Token.MacroInvoke.Item1}:{Token.MacroInvoke.Item2}:");
            }
            if (!string.IsNullOrEmpty(SourceName) && LineNumber > 0)
            {
                writer.Write($"{SourceName}({LineNumber}:{Position}): ");
            }
            var consoleColor = Console.ForegroundColor;
            (string type, ConsoleColor color) = IsError ? ("error", ConsoleColor.Red) : ("warning", ConsoleColor.Yellow);
            Console.ForegroundColor = color;
            writer.Write($"{type}: ");
            Console.ForegroundColor = consoleColor;
            writer.WriteLine(Message);
            if (Token != null && !noHighlighting)
            {
                var input = Token.InputStream.ToString()?.Replace("\r\n", "\n").Replace('\t', ' ') ?? "";
                var sourceLines = input.Split(new char[] { '\n', '\r' });
                var errorLine = sourceLines[LineNumber - 1];
                writer.Write(errorLine.AsSpan(0, Token.Column));
                writer.Write(Token.Text);
                var afterTokenColumn = Token.Column + Token.Text.Length;
                writer.WriteLine(errorLine[afterTokenColumn..]);
                for (var i = 0; i < Token.Column; ++i)
                    writer.Write(' ');
                Console.ForegroundColor = color;
                writer.WriteLine("^~~");
                Console.ForegroundColor = consoleColor;
            }
        }

        public bool Equals(Error? other)
            => SourceName?.Equals(other?.SourceName) == true &&
                LineNumber == other?.LineNumber &&
                Position == other.Position &&
                Message?.Equals(other.Message) == true;

        public override bool Equals(object? obj)
        {
            if (obj is Error other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(SourceName, LineNumber, Position, Message);

        public ParserRuleContext? Context { get; init; }

        public Token? Token { get; init; }

        public string? SourceName { get; init; }

        public int Position { get; init; }

        public int LineNumber { get; init; }

        public bool IsError { get; init; }
    }
}
