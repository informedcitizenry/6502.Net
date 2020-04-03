//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// A simple, flexible error log.
    /// </summary>
    public class ErrorLog
    {
        readonly
        #region Members

        List<(string message, bool isError)> _errors;

        #endregion

        #region Constructor
        /// <summary>
        /// Constructs an instance of the ErrorLog class.
        /// </summary>
        public ErrorLog() => _errors = new List<(string message, bool isError)>();

        #endregion

        #region Methods

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
        public void DumpAll(TextWriter writer) =>
            _errors.ForEach(e => writer.WriteLine(e.message));

        /// <summary>
        /// Dumps all logged errors to console output.
        /// </summary>
        public void DumpErrors() => DumpErrors(Console.Error);

        /// <summary>
        /// Dumps all logged errors to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump errors to.</param>
        public void DumpErrors(TextWriter writer)
        {
            _errors.Where(e => e.isError).ToList()
                   .ForEach(error => writer.WriteLine(error.message));
        }

        /// <summary>
        /// Dumps all logged warnings to console output.
        /// </summary>
        public void DumpWarnings() => DumpWarnings(Console.Out);

        /// <summary>
        /// Dumps all logged warnings to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to dump warnings to.</param>
        public void DumpWarnings(TextWriter writer)
        {
            _errors.Where(e => !e.isError).ToList()
                   .ForEach(warning => writer.WriteLine(warning.message));
        }

        public void LogEntry(string filename, int linenumber, string message, params object[] source)
            => LogEntry(filename, linenumber, message, true, source);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        /// <param name="source">The message source.</param>
        public void LogEntry(string filename, int linenumber, int position, string message, bool isError, params object[] source)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrEmpty(filename))
            {
                if (isError)
                    sb.Append("Error");
                else
                    sb.Append("Warning");
            }
            else
            {
                filename = Path.GetFileName(filename);
                sb.Append($"{filename}({linenumber}");
                if (position > 0)
                    sb.Append($",{position}");
                sb.Append("): ");
                if (isError)
                    sb.Append("error");
                else
                    sb.Append("warning");
            }
            if (!string.IsNullOrEmpty(message))
            {
                sb.Append(": ");
                if (source == null || !message.Contains("{0}"))
                    sb.Append(Regex.Replace(message, @"\s?\{\d+\}\s?", string.Empty));
                else
                    sb.AppendFormat(message, source);
            }
            isError = isError || Assembler.Options.WarningsAsErrors;
            _errors.Add((sb.ToString(), isError));
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
        /// <param name="message">The custom string message.</param>
        /// <param name="source">The message source.</param>
        /// <param name="isError">Indicate if the mesage is an error.</param>
        public void LogEntry(string filename, int linenumber, string message, bool isError, params object[] source)
            => LogEntry(filename, linenumber, 0, message, isError, source);


        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="message">The custom string message.</param>
        public void LogEntry(string filename, int linenumber, string message) => LogEntry(filename, linenumber, message, true, null);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        public void LogEntry(string filename, int linenumber) => LogEntry(filename, linenumber, string.Empty, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="filename">The source file.</param>
        /// <param name="linenumber">The source line number.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        public void LogEntry(string filename, int linenumber, bool isError) => LogEntry(filename, linenumber, string.Empty, isError);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="token">The <see cref="Token"/> associated with the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the message is an error.</param>
        /// <param name="source">The message source.</param>
        public void LogEntry(SourceLine line, Token token, string message, bool isError, params object[] source)
            => LogEntry(line.Filename, line.LineNumber, token.Position, message, isError, source);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="token">The <see cref="Token"/> associated with the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="source">The message source.</param>
        public void LogEntry(SourceLine line, Token token, string message, params object[] source)
            => LogEntry(line.Filename, line.LineNumber, token.Position, message, true, source);


        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="position">The position in the source that raised the message.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="source">The message source.</param>
        public void LogEntry(SourceLine line, int position, string message, params object[] source)
            => LogEntry(line.Filename, line.LineNumber, position, message, true, source);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="token">The <see cref="Token"/> associated with the message.</param>
        /// <param name="message">The custom string message.</param>
        public void LogEntry(SourceLine line, Token token, string message)
            => LogEntry(line.Filename, line.LineNumber, token.Position, message, true, token.Name);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="token">The <see cref="Token"/> associated with the message.</param>
        public void LogEntry(SourceLine line, Token token)
            => LogEntry(line.Filename, line.LineNumber, token.Position, string.Empty, true, token.Name);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The The <see cref="SourceLine"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="source">The message source.</param>
        public void LogEntry(SourceLine line, string message, params object[] source)
            => LogEntry(line.Filename, line.LineNumber, message, true, source);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The The <see cref="SourceLine"/>.</param>
        /// <param name="message">The custom string message.</param>
        /// <param name="isError">Indicate if the mesage is an error.</param>
        public void LogEntry(SourceLine line, string message, bool isError) =>
            LogEntry(line.Filename, line.LineNumber, message, isError, null);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="isError">Indicate if the mesage is an error.</param>
        public void LogEntry(SourceLine line, bool isError) => LogEntry(line.Filename, line.LineNumber, isError);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the log entries
        /// </summary>
        public ReadOnlyCollection<string> Entries => _errors.Select(e => e.message).ToList().AsReadOnly();

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