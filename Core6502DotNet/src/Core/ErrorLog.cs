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

namespace Core6502DotNet
{
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

        void DumpEntries(IEnumerable<(string message, bool isError)> entries,
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
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        /// <param name="source">The message source.</param>
        public void LogEntry(string filename, int linenumber, int position, string message, bool isError)
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
                errorBuilder.Append($": {message}");
            isError = isError || _warningsAsErrors;
            _errors.Add((errorBuilder.ToString(), isError));
            if (_errors.Count > 1000)
            {
                DumpAll();
                throw new Exception("Too many errors.");
            }
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custome string message.</param>
        public void LogEntry(string filename, int linenumber, int position, string message)
            => LogEntry(filename, linenumber, position, message, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        public void LogEntry(SourceLine line, int position, string message)
            => LogEntry(line.Filename, line.LineNumber, position, message, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(Token token, string message, bool isError) =>
            LogEntry(token.Line.Filename, token.Line.LineNumber, token.Position, message, isError);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The <see cref="Token"/>.</param>
        public void LogEntry(Token token, string message) =>
            LogEntry(token.Line.Filename, token.Line.LineNumber, token.Position, message, true);

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