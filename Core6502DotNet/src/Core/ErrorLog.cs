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
using System.Text;
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
            : base(message)
        {
            Log = log;
        }

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
        const ConsoleColor ErrorColor = ConsoleColor.Red;
        const ConsoleColor WarnColor  = ConsoleColor.Yellow;

        #region Members

        readonly List<(string message, bool isError)> _errors;
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
            _errors = new List<(string message, bool isError)>();
        }

        #endregion

        #region Methods

        static void DumpEntries(IEnumerable<(string message, bool isError)> entries,
                         ConsoleColor textColor,
                         TextWriter writer)
        {
            ConsoleColor consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = textColor;
            entries.ToList().ForEach(entry => writer.WriteLine(entry.message));
            Console.ForegroundColor = consoleColor;
        }

        /// <summary>
        /// Clear all logged messages.
        /// </summary>
        public void ClearAll() => _errors.Clear();

        /// <summary>
        /// Clear all logged errors.
        /// </summary>
        public void ClearErrors() => _errors.RemoveAll(e => e.isError);

        /// <summary>
        /// Clears all logged warnings.
        /// </summary>
        public void ClearWarnings() => _errors.RemoveAll(e => !e.isError);

        /// <summary>
        /// Dumps all logged messages to console output.
        /// </summary>
        public void DumpAll() => DumpAll(Console.Error);

        /// <summary>
        /// Dumps all logged messages to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump log messages to.</param>
        public void DumpAll(TextWriter writer)
        {
            ConsoleColor consoleColor = Console.ForegroundColor;
            _errors.ForEach(e => {
                Console.ForegroundColor = e.isError ? ErrorColor : WarnColor;
                writer.WriteLine(e.message);
            });
            Console.ForegroundColor = consoleColor;
        }

        /// <summary>
        /// Dumps all logged errors to console output.
        /// </summary>
        public void DumpErrors() => DumpErrors(Console.Error);

        /// <summary>
        /// Dumps all logged errors to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump errors to.</param>
        public void DumpErrors(TextWriter writer) 
            => DumpEntries(_errors.Where(e => e.isError), ErrorColor, writer);

        /// <summary>
        /// Dumps all logged warnings to console output.
        /// </summary>
        public void DumpWarnings() => DumpWarnings(Console.Out);

        /// <summary>
        /// Dumps all logged warnings to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump warnings to.</param>
        public void DumpWarnings(TextWriter writer) 
            => DumpEntries(_errors.Where(e => !e.isError), WarnColor, writer);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The custom string message.</param>
        public void LogEntrySimple(string message)
            => _errors.Add((message, true));

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntrySimple(string message, bool isError)
            => _errors.Add((message, isError || _warningsAsErrors));

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="source">The source text of the source that raised the message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(string filename, int linenumber, int position, string message, string source, bool isError)
        {
            var errorBuilder = new StringBuilder();
            var type = isError ? "Error" : "Warning";
            if (string.IsNullOrEmpty(filename))
            {
                errorBuilder.Append(type);
            }
            else
            {
                filename = Path.GetFileName(filename);
                errorBuilder.Append($"{filename}({linenumber}");
                if (position > 0)
                    errorBuilder.Append($",{position}");
                errorBuilder.Append("): ");
                errorBuilder.Append($"{type.ToLower()}");
            }
            if (!string.IsNullOrEmpty(message))
            {
                errorBuilder.Append($": {message}");
            }
            if (!string.IsNullOrEmpty(source))
            {
                var highlightWs = Regex.Replace(source, @"[^\t]", " ");
                errorBuilder.AppendLine($"\n{source}")
                            .Append(highlightWs.Substring(0, position - 1))
                            .Append('^');
            }
            isError = isError || _warningsAsErrors;
            var errorMessage = errorBuilder.ToString();
            if (!_errors.Contains((errorMessage, isError)))
                _errors.Add((errorMessage, isError));
            if (_errors.Count > 1000)
                throw new ErrorLogFullException(this, "Too many errors.");
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custome string message.</param>
        /// <param name="source">The source text of the source that raised the message.</param>
        public void LogEntry(string filename, int linenumber, int position, string message, string source)
            => LogEntry(filename, linenumber, position, message, source, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custome string message.</param>
        public void LogEntry(string filename, int linenumber, int position, string message)
            => LogEntry(filename, linenumber, position, message, string.Empty, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        public void LogEntry(SourceLine line, int position, string message)
            => LogEntry(line.Filename, line.LineNumber, position, message, line.Source, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(Token token, string message, bool isError) =>
            LogEntry(token.Line.Filename, token.Line.LineNumber, token.Position, message, token.Line.Source, isError);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        public void LogEntry(Token token, string message) =>
            LogEntry(token.Line.Filename, token.Line.LineNumber, token.Position, message, token.Line.Source, true);

        #endregion

        #region Properties

        /// <summary>
        /// Gets if the log has errors.
        /// </summary>
        public bool HasErrors => _errors.Any(e => e.isError);

        /// <summary>
        /// Gets if the log has warnings.
        /// </summary>
        public bool HasWarnings => _errors.Any(e => !e.isError);

        /// <summary>
        /// Gets the error count in the log.
        /// </summary>
        public int ErrorCount => _errors.Count(e => e.isError);

        /// <summary>
        /// Gets the warning count in the log.
        /// </summary>
        public int WarningCount => _errors.Count(w => !w.isError);

        #endregion
    }
}