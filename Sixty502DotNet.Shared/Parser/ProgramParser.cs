//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The parsing options to pass to the parse method of the <see cref="ProgramParser"/>.
/// These options direct the parser how to understand source strings and how to
/// recognize keywords.
/// </summary>
public struct ParseOptions
{
    /// <summary>
    /// Gets or sets whether parsing is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the stream factory used to a create <see cref="ICharStream"/>
    /// for the source.
    /// </summary>
    public ICharStreamFactory StreamFactory { get; set; }

    /// <summary>
    /// Gets or sets the instruction set used by during the lexing process to
    /// recognize mnemonics and other keywords.
    /// </summary>
    public IDictionary<string, int> InstructionSet { get; set; }
}

/// <summary>
/// The result of parsing source(s), including syntax errors, uninvoked macros and
/// the parsed <see cref="SyntaxParser.ProgramContext"/> if successful.
/// </summary>
public readonly struct ParseResult
{
    /// <summary>
    /// Gets the parsed program.
    /// </summary>
    public SyntaxParser.ProgramContext Program { get; init; }

    /// <summary>
    /// Gets the list of errors that occurred during parsing.
    /// </summary>
    public IList<Error> Errors { get; init; }

    /// <summary>
    /// Gets the list of uninvoked macros during preprocessing.
    /// </summary>
    public IList<KeyValuePair<string, Macro>> UninvokeMacros { get; init; }
}

/// <summary>
/// A helper class to parse multiple sources according to certain options.
/// </summary>
public static class ProgramParser
{
    /// <summary>
    /// Parse source(s) with given <see cref="ParseOptions"/> options.
    /// </summary>
    /// <param name="sources">The sources to parse.</param>
    /// <param name="options">The parse options to direct the parser.</param>
    /// <returns>A <see cref="ParseResult"/>.</returns>
    public static ParseResult Parse(IList<string> sources, ParseOptions options)
    {
        List<IToken> preprocessedTokens = new();
        Preprocessor preprocessor = new(options.StreamFactory, options.InstructionSet, options.CaseSensitive);

        ErrorListener syntaxErrors = new();

        for (int i = 0; i < sources.Count; i++)
        {
            bool appendNewLine = i < sources.Count - 1;
            preprocessedTokens.AddRange(preprocessor.Preprocess(sources[i], appendNewLine, syntaxErrors));
        }
        CommonTokenStream tokenStream = new(new ListTokenSource(preprocessedTokens));
        SyntaxParser parser = new(tokenStream);
        parser.RemoveErrorListeners();
        if (syntaxErrors.Errors.Count == 0)
        {
            parser.AddErrorListener(syntaxErrors);
        }
        parser.Interpreter.PredictionMode = PredictionMode.SLL;
        ParseResult parseResult = new()
        {
            Program = parser.program(),
            Errors = syntaxErrors.Errors,
            UninvokeMacros = preprocessor.UninvokedMacros
        };
        return parseResult;
    }
}
