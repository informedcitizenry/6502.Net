//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// A simple, flexible error log.
    /// </summary>
    public class ErrorLog : BaseErrorListener
    {
        private readonly HashSet<Error> _errors;
        private readonly bool _warningsAsErrors;
        private bool _hasErrors;
        private bool _hasWarnings;
        private bool _hasCriticalErrors;

        /// <summary>
        /// Construct a new instance of the <see cref="ErrorLog"/> class.
        /// </summary>
        public ErrorLog()
            : this(false)
        {

        }

        /// <summary>
        /// Construct a new instance of the <see cref="ErrorLog"/> class.
        /// </summary>
        /// <param name="warningsAsErrors">Treat warnings as error.</param>
        public ErrorLog(bool warningsAsErrors)
            => (_errors, _warningsAsErrors, _hasErrors) = (new HashSet<Error>(), warningsAsErrors, false);

        /// <summary>
        /// Log a parsing syntax error.
        /// </summary>
        /// <param name="output">The error writer output.</param>
        /// <param name="recognizer">The recognizer reporting the parsing error.</param>
        /// <param name="offendingSymbol">The source of the syntax error.</param>
        /// <param name="line">The line in source.</param>
        /// <param name="charPositionInLine">The character position in the line of soure.</param>
        /// <param name="msg">The parsing error messsage.</param>
        /// <param name="e">The recognition exception, if any.</param>
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            if (recognizer is Parser parser)
            {
                var ruleContext = parser.RuleContext;
                if (e is not CustomParseError)
                {
                    if (e is RecognitionException)
                    {
                        msg = $"Unexpected token \"{offendingSymbol.Text}\".";
                    }
                    else
                    {
                        msg = Sixty502DotNet.Errors.UnexpectedExpression;
                    }
                }
                LogEntry(ruleContext, offendingSymbol, msg, true);
            }
            base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The token that is the source of the error or warning.</param>
        /// <param name="message">The message.</param>
        /// <param name="isError">The message is an error.</param>
        public void LogEntry(Token token, string message, bool isError)
            => LogEntry(null, token, message, isError);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="token">The token that is the source of the error or warning.</param>
        /// <param name="message">The message.</param>
        public void LogEntry(Token token, string message)
            => LogEntry(null, token, message, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="context">The parse tree context where the error or warning ocurred.</param>
        /// <param name="message">The message.</param>
        public void LogEntry(ParserRuleContext context, string message)
            => LogEntry(context, (Token)context.Start, message, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="context">The parse tree context where the error or warning ocurred.</param>
        /// <param name="message">The message.</param>
        /// <param name="isError">The message is an error.</param>
        public void LogEntry(ParserRuleContext context, string message, bool isError)
            => LogEntry(context, (Token)context.Start, message, isError);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="context">The parse tree context where the error or warning ocurred.</param>
        /// <param name="offendingSymbol">The token that is the source of the error or warning.</param>
        /// <param name="message">The message.</param>
        public void LogEntry(ParserRuleContext? context, IToken offendingSymbol, string message)
            => LogEntry(context, offendingSymbol, message, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="context">The parse tree context where the error or warning ocurred.</param>
        /// <param name="offendingSymbol">The token that is the source of the error or warning.</param>
        /// <param name="message">The message.</param>
        /// <param name="isError">The message is an error.</param>
        public void LogEntry(ParserRuleContext? context, IToken offendingSymbol, string message, bool isError)
        {
            var line = offendingSymbol.Line;
            var charPositionInLine = offendingSymbol.Column;
            var token = offendingSymbol as Token;
            isError |= _warningsAsErrors;
            _hasErrors |= isError;
            _hasWarnings |= !isError;
            _errors.Add(new Error(context, message, token, token!.Filename, line, charPositionInLine, isError));
        }

        public void LogCriticalError(string message)
        {
            var consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("One or more critical errors encountered: ");
            Console.ForegroundColor = consoleColor;
            LogEntrySimple(message);
            _hasCriticalErrors = true;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message"></param>
        public void LogEntrySimple(string message)
            => LogEntrySimple(message, true);

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isError"></param>
        public void LogEntrySimple(string message, bool isError)
        {
            _errors.Add(new Error(message, isError));
            _hasErrors |= isError;
            _hasWarnings |= !isError;
        }

        /// <summary>
        /// Dump messages.
        /// </summary>
        /// <param name="includeWarnings"></param>
        /// <param name="noHighlighting"></param>
        public void Dump(bool includeWarnings, bool noHighlighting) => Dump(includeWarnings, noHighlighting, Console.Error);

        /// <summary>
        /// Dump messages.
        /// </summary>
        /// <param name="includeWarnings"></param>
        /// <param name="noHighlighting"></param>
        /// <param name="writer">The <see cref="TextWriter"/> to output
        /// the log.</param>
        public void Dump(bool includeWarnings, bool noHighlighting, TextWriter writer)
        {
            foreach (var e in _errors)
            {
                if ((includeWarnings && !_hasCriticalErrors) || e.IsError)
                {
                    e.Print(noHighlighting, writer);
                }
            }
            if (!_hasCriticalErrors)
            {
                PrintLogCount(writer);
            }
        }

        /// <summary>
        /// Print the count of log messages.
        /// </summary>
        public void PrintLogCount() => PrintLogCount(Console.Out);

        /// <summary>
        /// Print the count of log messages.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to output the
        /// log count.</param>
        public void PrintLogCount(TextWriter writer)
        {
            writer.WriteLine($"Number of errors: {ErrorCount}");
            writer.WriteLine($"Number of warnings: {WarningCount}");
        }

        /// <summary>
        /// Dump errors.
        /// </summary>
        /// <param name="noHighlighting">Suppress highlighting the
        /// sources of errors in the messages.</param>
        public void DumpErrors(bool noHighlighting)
            => DumpErrors(noHighlighting, Console.Error);

        /// <summary>
        /// Dump errors.
        /// </summary>
        /// <param name="noHighlighting">Suppress highlighting the
        /// sources of errors in the messages.</param>
        /// <param name="writer">The output to dump the error messages.</param>
        public void DumpErrors(bool noHighlighting, TextWriter writer)
        {
            foreach (var e in _errors.Where(e => e.IsError))
            {
                e.Print(noHighlighting, writer);
            }
        }

        /// <summary>
        /// Clear the log.
        /// </summary>
        public void Clear() => _errors.Clear();

        /// <summary>
        /// Get the list of errors.
        /// </summary>
        public ReadOnlyCollection<Error> Errors => _errors.ToList().AsReadOnly();

        /// <summary>
        /// Get if errors have been logged.
        /// </summary>
        public bool HasErrors => _hasErrors;

        /// <summary>
        /// Get if warnings have been logged.
        /// </summary>
        public bool HasWarnings => _hasWarnings;

        /// <summary>
        /// Get if any messages have been logged.
        /// </summary>
        public bool HasMessages => _hasErrors || _hasWarnings;

        /// <summary>
        /// Get the error count.
        /// </summary>
        public int ErrorCount => _errors.Count(e => e.IsError);

        /// <summary>
        /// Get the warning count.
        /// </summary>
        public int WarningCount => _errors.Count(e => !e.IsError);
    }
}
