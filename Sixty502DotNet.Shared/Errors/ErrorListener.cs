//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System.Text.RegularExpressions;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A custom implementation of the ANTLR error listener to fine-tune parsing
/// errors.
/// </summary>
public sealed partial class ErrorListener : BaseErrorListener
{
    /// <summary>
    /// Create a new instance of the <see cref="ErrorListener"/> class.
    /// </summary>
    public ErrorListener() => Errors = new();

    private static string GetOffendingSymbolText(IToken offendingSymbol)
    {
        return offendingSymbol.Text.Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private static IToken? GetNestedBlockDirective(RuleContext context)
    {
        if (context is SyntaxParser.PreprocStatContext preprocStat && preprocStat.d.Type == SyntaxParser.Macro)
        {
            return preprocStat.d;
        }
        return context switch
        {
            SyntaxParser.StatBlockContext statBlock         => statBlock.b,
            SyntaxParser.StatFuncDeclContext statFunc       => statFunc.Function().Symbol,
            SyntaxParser.StatEnumDeclContext statEnum       => statEnum.Enum().Symbol,
            SyntaxParser.InstructionContext instrContext    => instrContext.Start,
            SyntaxParser.IfBlockContext ifBlock             => ifBlock.Start,
            _                                               => null
        };
    }

    private static readonly Dictionary<int, int> s_blockTypes = new()
    {
        { SyntaxParser.Do, SyntaxParser.Whiletrue },
        { SyntaxParser.For, SyntaxParser.Next },
        { SyntaxParser.Foreach, SyntaxParser.Next }
    };

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException? e)
    {
        if (offendingSymbol.Type == SyntaxParser.Unrecogized)
        {
            msg = $"Unrecognized character '{GetOffendingSymbolText(offendingSymbol)}'";
            goto logError;
        }
        Parser? parser = recognizer as Parser;
        if (parser == null && (e is NoViableAltException || e is LexerNoViableAltException))
        {
            msg = $"Unexpected: '{GetOffendingSymbolText(offendingSymbol)}'";
            goto logError;
        }
        if (parser != null && e is not CustomParseError)
        {
            var vocab = parser.Vocabulary;
            if (offendingSymbol.Type == TokenConstants.EOF && parser.Context != null)
            {
                IToken? directive = GetNestedBlockDirective(parser.Context);
                if (directive != null)
                {
                    string expectedName = $".end{directive.Text[1..].ToLower()}";
                    if (s_blockTypes.TryGetValue(directive.Type, out int expectedType))
                    {
                        expectedName = $".{vocab.GetDisplayName(expectedType).ToLower()}";
                    }
                    msg = $"Directive '{directive.Text.ToLower()}' is incomplete (End of file reached before '{expectedName}' was found)";
                    offendingSymbol = directive;
                    goto logError;
                }
            }
            if (e?.GetExpectedTokens()?.Count > 0 && e.Context is not SyntaxParser.EosContext)
            {
                if (offendingSymbol.Type == SyntaxParser.UnclosedLiteral)
                {
                    msg = $"unclosed string literal";
                    goto logError;
                }
                if (e.Context is SyntaxParser.EosContext)
                {
                    msg = $"end of statement expected but found '{GetOffendingSymbolText(offendingSymbol)}'";
                    goto logError;
                }
                var expected = e.GetExpectedTokens().ToList();
                if (expected.Count < 5)
                {
                    if (expected.Count > 1)
                    {
                        msg = $"Invalid context for '{GetOffendingSymbolText(offendingSymbol)}'";
                        goto logError;
                    }
                    string expectedName = "statement";
                    if (expected[0] > -1)
                    {
                        string litName = vocab.GetDisplayName(expected[0]);
                        if (!string.IsNullOrEmpty(litName))
                        {
                            expectedName = litName;
                        }
                    }
                    string messageFormat = "{0} expected but found '{1}'";
                    msg = string.Format(messageFormat, expectedName, GetOffendingSymbolText(offendingSymbol));
                    goto logError;
                }
            }
            if (offendingSymbol.Type == SyntaxParser.NL || offendingSymbol.Type == SyntaxParser.Eof)
            {
                msg = $"Unexpected end of statement";
            }
            else
            {
                msg = $"Unexpected: '{offendingSymbol.Text}'";
            }
        }
    logError:
        msg = EndOfFileRegex().Replace(msg, "end of file");
        Errors.Add(new Error(offendingSymbol, msg));
        base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }

    /// <summary>
    /// Gets the list of <see cref="Error"/> objects the parser encountered
    /// during parsing.
    /// </summary>
    public HashSet<Error> Errors { get; init; }

    [GeneratedRegex("'?EOF'?")]
    private static partial Regex EndOfFileRegex();
}

