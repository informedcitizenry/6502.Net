//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;
using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// An exception specific to processing 6502.Net source code.
/// </summary>
public class Error : Exception, IEquatable<Error>
{
    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> class.
    /// </summary>
    /// <param name="offendingSymbol">The <see cref="IToken"/> that is the
    /// source of the error.</param>
    /// <param name="message">The error message.</param>
    /// <param name="isError"></param>
    protected Error(IToken? offendingSymbol, string message, bool isError)
        : base(message)
    {
        SourceName = offendingSymbol?.TokenSource.SourceName ?? string.Empty;
        Line = offendingSymbol?.Line ?? 0;
        Column = offendingSymbol?.Column ?? 0;
        Token = offendingSymbol as Token;
        IsControlFlow = false;
        IsError = isError;
    }

    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> class.
    /// </summary>
    /// <param name="parserRuleContext">The <see cref="ParserRuleContext"/>
    /// that is the source of the error.</param>
    /// <param name="message">The error message.</param>
    /// <param name="isError">The flag indicating this is an error.</param>
    protected Error(ParserRuleContext parserRuleContext, string message, bool isError)
        : this(parserRuleContext.Start, message, isError)
    {
        Context = parserRuleContext;
    }

    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> class.
    /// </summary>
    /// <param name="parserRuleContext">The <see cref="ParserRuleContext"/>
    /// that is the source of the error.</param>
    /// <param name="message">The error message.</param>
    public Error(ParserRuleContext parserRuleContext, string message)
        : this(parserRuleContext, message, true)
    {

    }

    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> class.
    /// </summary>
    /// <param name="offendingSymbol">The <see cref="IToken"/> that is the
    /// source of the error.</param>
    /// <param name="message">The error message.</param>
    public Error(IToken? offendingSymbol, string message)
        : this(offendingSymbol, message, true)
    {
    }

    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public Error(string message)
        : this((IToken?)null, message)
    {

    }

    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> from a warning.
    /// </summary>
    /// <param name="warning">The warning to copy.</param>
    public Error(Warning warning)
        : this(warning.Token, warning.Message)
    {

    }

    public override bool Equals(object? obj)
    {
        if (obj is Error other)
        {
            return Equals(other);
        }
        return false;
    }

    /// <summary>
    /// Get if this <see cref="Error"/> is equal in value to another.
    /// </summary>
    /// <param name="other">The other error.</param>
    /// <returns><c>true</c> if the this error equals the other in value,
    /// <c>false</c> otherwise.</returns>
    public bool Equals(Error? other)
    {
        if (other == null)
        {
            return false;
        }
        if (other.Message.Equals(Message))
        {
            if (Token != null)
            {
                return Token.Equals(other.Token);
            }
            return true;
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (Token != null)
        {
            return HashCode.Combine(Token, Message, IsError);
        }
        return HashCode.Combine(Message, IsError);
    }

    /// <summary>
    /// Get a JSON encoded string representation of the error.
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        StringBuilder json = new("{\"");
        json.Append(IsError ? "error" : "warning");
        json.Append("\":{");
        if (Token != null)
        {
            if (Token.MacroName != null)
            {
                json.Append($"\"macroName\":\"{Token.MacroName}\",\"macroLine\":{Token.MacroLine},");
            }
            json.Append($"\"source\":\"{Token.InputStream.SourceName}\",\"line\":{Token.Line},\"column\":{Token.Column},");

            json.Append($"\"highlight\":\"{Highlight}\",");
        }
        json.Append($"\"message\":\"{Message}\"}}}}");
        return json.ToString();
    }

    public override string ToString()
    {
        IToken? startToken = Context?.Start ?? Token;
        if (startToken == null)
        {
            return Message;
        }
        string type = IsError ? "error" : "warning";
        return $"{startToken.SourceInfo()} {type}: {Message}";
    }

    /// <summary>
    /// Get the highlighted part of the error based on the token.
    /// </summary>
    public string[]? Highlight
    {
        get
        {
            if (Context != null || Token != null)
            {
                IToken startToken = Context?.Start ?? Token!;
                string input = startToken.InputStream.ToString()?.Replace("\r\n", "\n").Replace('\t', ' ') ?? "";
                string[] sourceLines = input.Split(new char[] { '\n', '\r' });
                string errorLine = sourceLines[startToken.Line - 1];
                int highlightLen;
                if (Context != null)
                {
                    highlightLen = Context.GetAllTextLength();
                }
                else
                {
                    highlightLen = Token!.Text.Length;
                }
                if (highlightLen > errorLine.Length)
                {
                    highlightLen = errorLine.Length;
                }
                string highlight;
                if (startToken.Column == 0)
                {
                    highlight = new string('~', highlightLen);
                }
                else
                {
                    highlight = $"{" ".PadLeft(startToken.Column)}{new string('~', highlightLen)}";
                }
                return new string[] { errorLine, highlight };
            }
            return null;
        }
    }

    /// <summary>
    /// Get the <see cref="ParserRuleContext"/> that caused the error.
    /// </summary>
    public ParserRuleContext? Context { get; }

    /// <summary>
    /// Get the <see cref="Token"/> that cased the error.
    /// </summary>
    public Token? Token { get; }

    /// <summary>
    /// Get the error source file name.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Get the error line number.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Get the column number in the line.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Get whether the error is actually an error or a warning.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// Get whether the error is actually a control-flow type exception, used
    /// to unwind the call stack to an earlier point to implement directives
    /// such as <c>.break</c>, <c>.continue</c>, and <c>.goto</c>.
    /// </summary>
    public bool IsControlFlow { get; protected init; }
}

/// <summary>
/// A warning-type exception specific to processing 6502.Net source code.
/// </summary>
public class Warning : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="Warning"/> class.
    /// </summary>
    /// <param name="ruleContext">The <see cref="ParserRuleContext"/> that
    /// is the source of the warning.</param>
    /// <param name="message">The warning message.</param>
    public Warning(ParserRuleContext ruleContext, string message)
        : base(ruleContext, message, false)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="Warning"/> class.
    /// </summary>
    /// <param name="message">The warning message.</param>
    public Warning(string message)
        : base((IToken?)null, message, false)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="Warning"/> class.
    /// </summary>
    /// <param name="offendingSymbol">The <see cref="IToken"/> that is the
    /// source of the warning.</param>
    /// <param name="message">The warning message.</param>
    public Warning(IToken? offendingSymbol, string message)
        : base(offendingSymbol, message, false)
    {

    }
}
