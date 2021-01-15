//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents a macro definition. Macros are small snippets of code that can be re-used
    /// and even parameterized. Parameters are text-substituted.
    /// </summary>
    public sealed class Macro : ParameterizedSourceBlock
    {
        #region Subclasses

        class MacroSource
        {
            public MacroSource(SourceLine line)
            {
                Line = line;
                ParamPlaces = new List<(int, string)>();
            }

            public SourceLine Line { get; }

            public List<(int paramIndex, string reference)> ParamPlaces { get; }

            public override string ToString() => Line.Source;
        }

        #endregion

        #region Members

        readonly List<MacroSource> _sources;
        readonly Preprocessor _processor;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new macro definition.
        /// </summary>
        /// <param name="parms">The list of macro definition's accepted parameters.</param>
        /// <param name="caseSensitive">Indicates whether the evaluation of passed parameters to the
        /// macro's own defined parameters is case-sensitive.</param>
        /// <param name="processor">The <see cref="Preprocessor"/> object.</param>
        public Macro(List<Token> parms, bool caseSensitive, Preprocessor processor)
            : base(parms, caseSensitive)
        {
            _sources = new List<MacroSource>();
            _processor = processor;
        }

        #endregion

        #region Methods

        List<List<Token>> GetParamListFromParameters(RandomAccessIterator<Token> passedParams)
        {
            var paramList = new List<List<Token>>();

            var index = 0;
            Token token;
            while ((token = passedParams.GetNext()) != null)
            {
                if (token.IsSeparator())
                {
                    if (index >= Params.Count || Params[index].DefaultValue.Count == 0)
                        throw new ExpressionException(token, "No default value.");
                    paramList.Add(Params[index].DefaultValue);
                }
                else
                {
                    var param = new List<Token>();
                    while (token != null && !token.IsSeparator())
                    {
                        param.Add(token);
                        token = passedParams.GetNext();
                    }
                    paramList.Add(param);
                }
                index++;
            }
            return paramList;
        }

        /// <summary>
        /// Add a line of source to the macro definition.
        /// </summary>
        /// <param name="line">The source line to add to the macro's definition.</param>
        public void AddSource(SourceLine line)
        {
            var comp = CaseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;
            var strcomp = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var macroSource = new MacroSource(line);
            for (var i = 0; i < line.Operands.Count; i++)
            {
                var op = line.Operands[i];
                if (op.Name[0] == '\\')
                {
                    if (i == line.Operands.Count - 1 || line.Operands[i + 1].Type != TokenType.Operand)
                        throw new ExpressionException(op, "Reference parameter not specified.");
                    op = line.Operands[++i];
                    if (!int.TryParse(op.Name.ToString(), out var paramRef))
                    {
                        paramRef = Params.FindIndex(p => p.Name.Equals(op.Name, comp));
                        if (paramRef < 0)
                            throw new ExpressionException(op, "Reference parameter not valid.");
                    }
                    else
                    {
                        paramRef--;
                    }
                    macroSource.ParamPlaces.Add((paramRef, "\\" + op.Name.ToString()));
                }
                else if (op.IsDoubleQuote())
                {
                    var firstIx = op.Name.ToString().IndexOf("@{");
                    var lastIx = op.Name.ToString().IndexOf("}");
                    while (firstIx > -1 && firstIx < lastIx)
                    {
                        var strRef = op.Name.Substring(firstIx + 2, lastIx - firstIx - 2);

                        if (!int.TryParse(strRef, out var paramRef))
                        {
                            paramRef = Params.FindIndex(p => p.Name.Equals(strRef, strcomp));
                            if (paramRef < 0)
                                throw new ExpressionException(op, "Reference parameter not valid.");
                            if (Params[paramRef].DefaultValue.Count > 0 && !Params[paramRef].DefaultValue[0].IsDoubleQuote())
                                throw new ExpressionException(Params[paramRef].DefaultValue[0],
                                    "Default value for macro parameter must be a string.");
                        }
                        else
                        {
                            paramRef--;
                        }
                        macroSource.ParamPlaces.Add((paramRef, op.Name.Substring(firstIx, lastIx - firstIx + 1)));
                        firstIx = op.Name.ToString().IndexOf("@{", lastIx + 1);
                        lastIx = op.Name.ToString().IndexOf("}", lastIx + 1);
                    }
                }
            }
            _sources.Add(macroSource);
        }

        /// <summary>
        /// Expands the macro into source from the invocation.
        /// </summary>
        /// <param name="passedParams">The parameters passed from the invocation.</param>
        /// <returns>A parsed collection of <see cref="SourceLine"/>s representing the expanded macro, including all
        /// substituted parameters.</returns>
        public IEnumerable<SourceLine> Expand(List<Token> passedParams)
        {
            var paramList = GetParamListFromParameters(passedParams.GetIterator());
            var expanded = new List<SourceLine>();

            foreach (var source in _sources)
            {
                if (source.ParamPlaces.Count > 0)
                {
                    string expandedSource = source.Line.FullSource;
                    foreach (var (paramIndex, reference) in source.ParamPlaces)
                    {
                        string substitution;
                        if (paramIndex >= paramList.Count)
                        {
                            if (paramIndex > Params.Count || Params[paramIndex].DefaultValue.Count == 0)
                                throw new ExpressionException(source.Line.Instruction, "Macro expected parameter but was not supplied.");
                            substitution = Token.Join(Params[paramIndex].DefaultValue).Trim();
                        }
                        else
                        {
                            substitution = Token.Join(paramList[paramIndex]).Trim();
                        }
                        if (substitution.Contains('$'))
                            substitution = substitution.Replace("$", "$$");
                        string pattern;
                        if (reference[0] == '@')
                        {
                            if (!substitution.EnclosedInDoubleQuotes())
                                throw new ExpressionException(source.Line.Instruction, "Macro parameter was not a string.");
                            pattern = @"@\{" + reference[2..^1] + @"\}";
                            substitution = substitution.TrimOnce('"');
                        }
                        else
                        {
                            pattern = @"\" + reference;
                        }
                        Regex regex = new Regex(pattern, CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        expandedSource = regex.Replace(expandedSource, substitution, 1);
                    }
                    var expandedList = _processor.Process(source.Line.Filename, source.Line.LineNumber, expandedSource);
                    expanded.AddRange(expandedList);
                }
                else
                {
                    expanded.Add(source.Line);
                }
            }
            return expanded;
        }
        #endregion
    }
}