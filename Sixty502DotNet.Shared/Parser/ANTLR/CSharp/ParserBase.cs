//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The base class for a 6502.Net parser. This class must be inherited.
/// </summary>
public abstract class ParserBase : Parser
{
    protected ParserBase(ITokenStream input)
        : base(input, TextWriter.Null, TextWriter.Null)
    {

    }

    protected ParserBase(ITokenStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput)
    {

    }

    protected bool StartsAtNewline()
    {
        try
        {
            return TokenStream.LT(-1).Type == SyntaxParser.NL;
        }
        catch (NullReferenceException)
        {
            return true;
        }
    }

    public static IList<IToken> ParseMacroArguments(IList<IToken> unparsedTokens, out int parsedCount)
    {
        IList<IList<IToken>> parsed = ParseMacroParams(unparsedTokens);
        parsedCount = parsed.Count;
        List<IToken> args = new();
        for (int p = 0; p < parsed.Count; p++)
        {
            IList<IToken> param = parsed[p];
            if (param[0].Type != SyntaxParser.Identifier)
            {
                throw new Error(param[0], "Identifier expected");
            }
            if (param.Count > 1)
            {
                break;
            }
            //parsedCount = p + 1;
            args.Add(param[0]);
        }
        return args;
    }

    /// <summary>
    /// Parse the token list into macro arguments. The parsing tracks nesting
    /// symbols (grouping symbols) for nested expressions and function calls so
    /// a comma will be properly be understood to be a part of the argument,
    /// rather than an argument separator.
    /// </summary>
    /// <param name="unparsedTokens">The token list.</param>
    /// <param name="startIndex">The start index to evaluate the list.</param>
    /// <returns>The parsed arguments, including default arguments.</returns>
    /// <exception cref="Error"></exception>
    public static IList<IList<IToken>> ParseDefaultMacroArguments(IList<IToken> unparsedTokens, int startIndex)
    {
        IList<IList<IToken>> parsed = ParseMacroParams(unparsedTokens);
        List<IList<IToken>> defaultArgs = new();
        for (int p = startIndex; p < parsed.Count; p++)
        {
            IList<IToken> param = parsed[p];
            if (param[0].Type != SyntaxParser.Identifier)
            {

                throw new Error(param[0], "Identifier expected");
            }
            if (param.Count < 2 || param[1].Type != SyntaxParser.Equal)
            {
                throw new Error(param[^1], "Optional parmaeter must define a default value");
            }
            defaultArgs.Add(param);
        }
        return defaultArgs;
    }

    /// <summary>
    /// Parse the token list into macro parameters. The parsing tracks nesting
    /// symbols (grouping symbols) for nested expressions and function calls so
    /// a comma will be properly be understood to be a part of the parameter,
    /// rather than a parameter separator.
    /// </summary>
    /// <param name="unparsedTokens">The token list.</param>
    /// <returns>The parsed parameters.</returns>
    /// <exception cref="Error"></exception>
    public static IList<IList<IToken>> ParseMacroParams(IList<IToken> unparsedTokens)
    {
        List<IList<IToken>> parameters = new();
        int lParens = 0, lSquares = 0, lCurlies = 0;
        List<IToken> currentParam = new();
        for (int i = 0; i < unparsedTokens.Count; i++)
        {
            IToken token = unparsedTokens[i];
            if (token.Type == SyntaxParser.Comma && lParens == 0 && lSquares == 0 && lCurlies == 0)
            {
                if (currentParam.Count == 0)
                {
                    throw new Error(token, "Unexpected token");
                }
                parameters.Add(currentParam);
                currentParam = new();
                continue;
            }
            if (token.Type == SyntaxParser.LeftParen) lParens++;
            if (token.Type == SyntaxParser.LeftSquare) lSquares++;
            if (token.Type == SyntaxParser.LeftCurly) lCurlies++;
            if (token.Type == SyntaxParser.RightParen && lParens > 0) lParens--;
            if (token.Type == SyntaxParser.RightSquare && lSquares > 0) lSquares--;
            if (token.Type == SyntaxParser.RightCurly && lCurlies > 0) lCurlies--;
            currentParam.Add(token);
        }
        if (currentParam.Count == 0)
        {
            throw new Error(unparsedTokens[^1], "Unexpected token");
        }
        parameters.Add(currentParam);
        return parameters;
    }

    private static SyntaxParser ParserFromSource(string source)
    {
        SyntaxLexer lexer = new(CharStreams.fromString(source));
        SyntaxParser parser = new(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.ErrorHandler = new BailErrorStrategy();
        return parser;
    }

    /// <summary>
    /// Parse a section string into its corresponding name, start and end
    /// address. This is useful for parsing any passed sections in the
    /// command line.
    /// </summary>
    /// <param name="section">The section source.</param>
    /// <returns>The section name, start, and end address.</returns>
    /// <exception cref="Exception"></exception>
    public static (string, int, int?) ParseDefineSection(string section)
    {
        SyntaxParser defineParser = ParserFromSource(section);
        SyntaxParser.DefineSectionContext context = defineParser.defineSection();
        if (context.exception != null)
        {
            throw new Exception($"Section argument '{section})' is not valid");
        }
        ValueBase start = Evaluator.EvalConstant(context.start);
        if (!start.IsDefined || !start.IsNumeric)
        {
            throw new Exception($"Start address in section argument '{section}' must be a constant numeric expression");
        }
        ValueBase? end = null;
        if (context.end != null)
        {
            end = Evaluator.EvalConstant(context.end);
            if (!end.IsDefined || !end.IsNumeric)
            {
                throw new Exception($"End address in section argument '{section}' must be a constant numeric expression");
            }
        }
        return (context.Identifier().GetText(), start.AsInt(), end?.AsInt());
    }

    /// <summary>
    /// Parse a constant defintion. 
    /// </summary>
    /// <param name="define">The define source.</param>
    /// <returns>A parse tree of the constant definition.</returns>
    public static SyntaxParser.DefineAssignContext ParseDefineAssign(string define)
    {
        SyntaxParser defineParser = ParserFromSource(define);
        return defineParser.defineAssign();
    }
}

