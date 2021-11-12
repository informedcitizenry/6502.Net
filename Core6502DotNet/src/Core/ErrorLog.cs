//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// An error for error log size being exceeded.
    /// </summary>
    public class ErrorLogFullException : Exception
    {
        /// <summary>
        /// Create a new instance of the error.
        /// </summary>
        /// <param name="log">The <see cref="ErrorLog"/> raising the exception.</param>
        /// <param name="message">The exception message.</param>
        public ErrorLogFullException(ErrorLog log, string message)
            : base(message) => Log = log;

        /// <summary>
        /// Gets the error log associated with the exception.
        /// </summary>
        public ErrorLog Log { get; }
    }

    /// <summary>
    /// A simple, flexible error log.
    /// </summary>
    public class ErrorLog
    {
        #region Constants

        const ConsoleColor ErrorColor = ConsoleColor.Red;
        const ConsoleColor WarnColor = ConsoleColor.Yellow;
        const int MaxErrors = 1000;

        #endregion

        #region Structs

        struct Error : IEquatable<Error>
        {
            public string fileName;
            public int lineNumber;
            public int position;
            public string message;
            public string token;
            public string source;
            public bool isError;

            public Error(string m, bool e)
            {
                message = m;
                isError = e;
                fileName = source = token = string.Empty;
                lineNumber = position = 0;
            }

            public bool Equals(Error other)
                => fileName.Equals(other.fileName) &&
                   lineNumber == other.lineNumber &&
                   message.Equals(other.message);

            public override bool Equals(object obj)
                => obj is Error other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17 * 23 + fileName.GetHashCode();
                    hash = hash * 23 + lineNumber.GetHashCode();
                    return hash * 23 + message.GetHashCode();
                }
            }

            public static bool operator ==(Error e1, Error e2)
                => e1.fileName.Equals(e2.fileName) &&
                   e1.lineNumber == e2.lineNumber &&
                   e1.message.Equals(e2.message);

            public static bool operator !=(Error e1, Error e2)
                => !e1.fileName.Equals(e2.fileName) ||
                   e1.lineNumber != e2.lineNumber ||
                   !e1.message.Equals(e2.message);
        }

        #endregion

        #region Members

        readonly HashSet<Error> _entries;
        readonly bool _warningsAsErrors;

        #endregion

        #region Constructor
        /// <summary>
        /// Constructs an instance of the ErrorLog class.
        /// </summary>
        /// <param name="warningsAsErrors">Treat warnings as errors.</param>
        public ErrorLog(bool warningsAsErrors)
        {
            _warningsAsErrors = warningsAsErrors;
            _entries = new HashSet<Error>();
        }

        #endregion

        #region Methods

        static void DumpEntries(IEnumerable<Error> entries, TextWriter writer)
        {
            ConsoleColor consoleColor = Console.ForegroundColor;
            foreach (var entry in entries)
            {
                var highlightColor = entry.isError ? ErrorColor : WarnColor;
                string type = entry.isError ? "Error" : "Warning";

                Console.ForegroundColor = consoleColor;
                if (!string.IsNullOrEmpty(entry.fileName))
                {
                    writer.Write($"{entry.fileName}({entry.lineNumber}");
                    if (entry.position > 0)
                        writer.Write($",{entry.position}");
                    writer.Write("): ");
                    type = type.ToLower();
                }

                Console.ForegroundColor = highlightColor;
                writer.Write($"{type}");

                Console.ForegroundColor = consoleColor;
                if (!string.IsNullOrEmpty(entry.message))
                {
                    writer.WriteLine($": {entry.message}");
                    if (!string.IsNullOrEmpty(entry.source))
                    {
                        var beforeHighlight = entry.source.Substring(0, entry.position - 1);
                        var highlight = Regex.Replace(entry.token, @"\t", " ");
                        var afterHighlight = string.Empty;
                        if (highlight.Length < entry.source.Length - entry.position + 1)
                            afterHighlight = entry.source[(entry.position - 1 + highlight.Length)..];
                        var highlightOffs = Regex.Replace(entry.source, @"[^\t]", " ");

                        writer.Write(beforeHighlight);

                        Console.ForegroundColor = highlightColor;
                        writer.Write(highlight);

                        Console.ForegroundColor = consoleColor;
                        writer.WriteLine(afterHighlight);

                        Console.ForegroundColor = highlightColor;
                        writer.WriteLine($"{highlightOffs.Substring(0, entry.position - 1)}^~~");

                        Console.ForegroundColor = consoleColor;
                    }
                }
            }
        }

        /// <summary>
        /// Clear all logged messages.
        /// </summary>
        public void ClearAll() => _entries.Clear();

        /// <summary>
        /// Clear all logged errors.
        /// </summary>
        public void ClearErrors() => _entries.RemoveWhere(e => e.isError);

        /// <summary>
        /// Clears all logged warnings.
        /// </summary>
        public void ClearWarnings() => _entries.RemoveWhere(e => !e.isError);

        /// <summary>
        /// Dumps all logged messages to console output.
        /// </summary>
        public void DumpAll() => DumpAll(Console.Error);

        /// <summary>
        /// Dumps all logged messages to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump log messages to.</param>
        public void DumpAll(TextWriter writer) => DumpEntries(_entries, writer);

        /// <summary>
        /// Dumps all logged errors to console output.
        /// </summary>
        public void DumpErrors() => DumpErrors(Console.Error);

        /// <summary>
        /// Dumps all logged errors to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump errors to.</param>
        public void DumpErrors(TextWriter writer)
            => DumpEntries(_entries.Where(e => e.isError), writer);

        /// <summary>
        /// Dumps all logged warnings to console output.
        /// </summary>
        public void DumpWarnings() => DumpWarnings(Console.Out);

        /// <summary>
        /// Dumps all logged warnings to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump warnings to.</param>
        public void DumpWarnings(TextWriter writer)
            => DumpEntries(_entries.Where(e => !e.isError), writer);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The custom string message.</param>
        public void LogEntrySimple(string message)
            => _entries.Add(new Error(message, true));

        public T LogEntrySimple<T>(string message)
        {
            _entries.Add(new Error(message, true));
            return default;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntrySimple(string message, bool isError)
            => _entries.Add(new Error(message, isError));

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="token">The token or object of the error.</param>
        /// <param name="source">The source text of the source that raised the message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(string filename, int linenumber, int position, string message, string token, string source, bool isError)
        {
            var error = new Error
            {
                fileName = filename,
                lineNumber = linenumber,
                position = position,
                isError = isError || _warningsAsErrors,
                message = message,
                token = token,
                source = source
            };
            _entries.Add(error);
            if (_entries.Count > MaxErrors)
                throw new ErrorLogFullException(this, "Too many errors.");
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custome string message.</param>
        /// <param name="token">The token or object of the error.</param>
        /// <param name="source">The source text of the source that raised the message.</param>
        public void LogEntry(string filename, int linenumber, int position, string message, string token, string source)
            => LogEntry(filename, linenumber, position, message, token, source, true);

        public T LogEntry<T>(string filename, int linenumber, int position, string message, string token, string source)
        {
            LogEntry(filename, linenumber, position, message, token, source, true);
            return default;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="token">The token or object of the error.</param>
        public void LogEntry(SourceLine line, int position, string message, string token)
            => LogEntry(line.Filename, line.LineNumber, position, message, token, line.Source, true);

        public T LogEntry<T>(SourceLine line, int position, string message, string token)
        {
            LogEntry(line.Filename, line.LineNumber, position, message, token, line.Source, true);
            return default;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        public void LogEntry(SourceLine line, int position, string message)
            => LogEntry(line.Filename,
                        line.LineNumber,
                        position,
                        message,
                        !string.IsNullOrEmpty(line.Source) ? line.Source[(position - 1)..] : string.Empty,
                        line.Source,
                        true);

        public T LogEntry<T>(SourceLine line, int position, string message)
        {
            LogEntry(line, position, message);
            return default;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="highlightToken">Indicate whether to highlight the token or the expression
        /// the token belongs to.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(Token token, string message, bool highlightToken, bool isError)
        {
            var lineSources = token.Line.FullSource.Split('\n');
            if (token.FirstInExpression != null && !highlightToken)
                token = token.FirstInExpression;
            var highlight = highlightToken ? token.Name.ToString() : Token.GetExpression(token, true, true);
            LogEntry(token.Line.Filename, token.Line.LineNumber + token.LineSourceIndex, token.Position, message, highlight, lineSources[token.LineSourceIndex], isError);
        }

        public T LogEntry<T>(Token token, string message, bool highlightToken, bool isError)
        {
            LogEntry(token, message, highlightToken, isError);
            return default;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(Token token, string message, bool isError)
            => LogEntry(token, message, true, isError);
        

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        public void LogEntry(Token token, string message) =>
            LogEntry(token, message, true, true);

        public T LogEntry<T>(Token token, string message)
        {
            LogEntry(token, message, true, true);
            return default;
        }



        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="token">The token or object of the error.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(SourceLine line, string message, string token, bool isError) =>
            LogEntry(line.Filename, line.LineNumber, line.Instruction.Position, message, token, line.Source, isError);

        #endregion

        #region Properties

        /// <summary>
        /// Gets if the log has errors.
        /// </summary>
        public bool HasErrors => _entries.Any(e => e.isError);

        /// <summary>
        /// Gets if the log has warnings.
        /// </summary>
        public bool HasWarnings => _entries.Any(e => !e.isError);

        /// <summary>
        /// Gets the error count in the log.
        /// </summary>
        public int ErrorCount => _entries.Count(e => e.isError);

        /// <summary>
        /// Gets the warning count in the log.
        /// </summary>
        public int WarningCount => _entries.Count(e => !e.isError);

        #endregion
    }
}