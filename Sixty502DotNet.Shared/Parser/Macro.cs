//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class containing a macro definition, including its defined parameters,
/// if any.
/// </summary>
public sealed class Macro
{
    /// <summary>
    /// The substitution location within a macro definition.
    /// </summary>
    public struct SubstitutionPoint
    {
        public int argumentIndex;
        public string? argumentName;
        public int definitionIndex;
    }

    /// <summary>
    /// Construct a new instance of the <see cref="Macro"/> class.
    /// </summary>
    /// <param name="macroStat">The parsed macro definition block.</param>
    /// <param name="tokenStream">The token stream of the current parser.</param>
    public Macro(SyntaxParser.PreprocStatContext macroStat, CommonTokenStream tokenStream)
    {

        MacroDeclaration = macroStat;
        IsInvoked = false;
        ArgumentNames = new HashSet<string>();
        SubstitutionPoints = new();
        if (macroStat.exception != null)
        {
            Definition = new List<IToken>();
            Arguments = new List<IToken>();
            OptionalArguments = new List<IList<IToken>>();
            return;
        }
        if (macroStat.macroParam() != null)
        {
            int paramStart = macroStat.macroParam().Start.TokenIndex;
            int paramEnd = macroStat.macroParam().Stop.TokenIndex;
            IList<IToken> unparsed = tokenStream.GetTokens(paramStart, paramEnd);
            Arguments = ParserBase.ParseMacroArguments(unparsed, out int parsedArgs);
            for (int i = 0; i < Arguments.Count; i++)
            {
                if (!ArgumentNames.Add(Arguments[i].Text))
                {
                    throw new Error(Arguments[i], "Duplicate argument name");
                }
            }
            if (Arguments.Count < parsedArgs)
            {
                OptionalArguments = ParserBase.ParseDefaultMacroArguments(unparsed, Arguments.Count);
                for (int i = 0; i < OptionalArguments.Count; i++)
                {
                    if (!ArgumentNames.Add(OptionalArguments[i][0].Text))
                    {
                        throw new Error(OptionalArguments[i][0], "Duplicate argument name");
                    }
                }
            }
            else
            {
                OptionalArguments = new List<IList<IToken>>();
            }
        }
        else
        {
            Arguments = new List<IToken>();
            OptionalArguments = new List<IList<IToken>>();
        }
        int start = macroStat.begin.TokenIndex + 1;
        int end = macroStat.end.TokenIndex;
        Definition = tokenStream.GetTokens(start, end);
        if (macroStat.label() != null)
        {
            Definition.Add(macroStat.label().Start);
        }
        for (int i = 0; i < Definition.Count; i++)
        {
            if (Definition[i].Type == SyntaxParser.MacroSub)
            {
                int argumentIndex;
                string? argumentName = null;
                string subName = Definition[i].Text[1..];
                if (char.IsLetter(subName[0]))
                {
                    argumentIndex = ArgumentNames.ToList().IndexOf(subName);
                    if (argumentIndex < 0)
                    {
                        throw new Error(Definition[i], $"Invalid substitution '{subName}' specified");
                    }
                    argumentName = subName;
                }
                else if (subName[0] == '*')
                {
                    argumentIndex = 0;
                }
                else
                {
                    argumentIndex = int.Parse(subName) - 1;
                }
                SubstitutionPoints[i] = new SubstitutionPoint
                {
                    definitionIndex = i,
                    argumentIndex = argumentIndex,
                    argumentName = argumentName
                };
            }
        }
        Valid = true;
    }

    /// <summary>
    /// Get if the macro is valid and can be expanded.
    /// </summary>
    public bool Valid { get; }

    /// <summary>
    /// Get the context of the macro's declaration statement.
    /// </summary>
    public SyntaxParser.PreprocStatContext MacroDeclaration { get; }

    /// <summary>
    /// Get or set the flag whether the macro is invoked.
    /// </summary>
    public bool IsInvoked { get; set; }

    /// <summary>
    /// Get the macro's substition points in its definition.
    /// </summary>
    public Dictionary<int, SubstitutionPoint> SubstitutionPoints { get; }

    /// <summary>
    /// Get the list of tokens comprising the macro definition body.
    /// </summary>
    public IList<IToken> Definition { get; }

    /// <summary>
    /// Get the macro argument names.
    /// </summary>
    public ISet<string> ArgumentNames { get; }

    /// <summary>
    /// Get the list of tokens comprising the macro definition arguments.
    /// </summary>
    public IList<IToken> Arguments { get; }

    /// <summary>
    /// Get the list of tokens comprising the macro definition optional
    /// arguments.
    /// </summary>
    public IList<IList<IToken>> OptionalArguments { get; }
}

