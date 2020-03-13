//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

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
            public SourceLine Line { get; set; }

            public List<(int paramIndex, string reference, Token token)> ParamPlaces { get; set; }

            public override string ToString() => Line.ParsedSource;
        }
        #endregion

        #region Members


        readonly List<MacroSource> _sources;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new macro instance.
        /// </summary>
        /// <param name="parms">The parameters token.</param>
        /// <param name="source">The original source string for the macro definition.</param>
        public Macro(Token parms, string source)
            : base(parms, source) => _sources = new List<MacroSource>();

        #endregion

        #region Methods

        /// <summary>
        /// Expands the macro into source from the invocation.
        /// </summary>
        /// <param name="passedParams">The parameters passed from the invocation.</param>
        /// <returns>A string representation of the expanded macro, including all
        /// substituted parameters.</returns>
        public IEnumerable<SourceLine> Expand(Token passedParams)
        {
            var paramList = new List<string>();
            // capture passed parameters to a simple string list
            if (passedParams != null && !string.IsNullOrEmpty(passedParams.ToString()))
            {
                var index = 0;
                foreach (Token p in passedParams.Children)
                {
                    var parmName = p.ToString(useUnparsed: true);
                    if (passedParams.Children.Count > 1)
                        parmName = parmName.TrimStartOnce(',');
                    if (string.IsNullOrEmpty(parmName))
                    {
                        // if empty then simply take the default value if it iexists.
                        if (index >= Params.Count || string.IsNullOrEmpty(Params[index].DefaultValue))
                            throw new ExpressionException(p.Position, "No default value was passed for unnamed parameter in parameter list.");
                        paramList.Add(Params[index].DefaultValue);
                    }
                    else
                    {
                        if (parmName.EnclosedInDoubleQuotes())
                            parmName = parmName.TrimOnce('"');
                        paramList.Add(parmName);
                    }
                    index++;
                }
            }
            var expanded = new List<SourceLine> { GetBlockDirectiveLine(string.Empty, 1, ".block") };
            foreach (MacroSource source in _sources)
            {
                if (source.ParamPlaces.Count > 0)
                {
                    string expandedSource = string.Empty;

                    foreach ((int paramIndex, string reference, Token token) parmRef in source.ParamPlaces)
                    {
                        string replacement;
                        string substitution;
                        if (parmRef.paramIndex >= paramList.Count)
                        {
                            // expected parameter exceeded passed parameters. Is there a default value?
                            if (parmRef.paramIndex > Params.Count || string.IsNullOrEmpty(Params[parmRef.paramIndex].DefaultValue))
                                throw new ExpressionException(source.Line.Operand.Position, "Macro expected parameter but was not supplied.");
                            substitution = Params[parmRef.paramIndex].DefaultValue;
                        }
                        else
                        {
                            substitution = paramList[parmRef.paramIndex];
                        }
                        replacement = parmRef.token.UnparsedName.Replace(parmRef.reference, substitution);//Name.Replace(parmRef.reference, substitution);
                        expandedSource = source.Line.UnparsedSource.Replace(parmRef.token.Name, replacement);
                    }
                    var expandedLine = LexerParser.Parse(source.Line.Filename, expandedSource).First();
                    expandedLine.LineNumber = source.Line.LineNumber;
                    expanded.Add(expandedLine);
                    //expanded.AddRange(LexerParser.Parse(source.Line.Filename, clone.UnparsedSource));
                }
                else
                {
                    expanded.Add(source.Line);
                }
            }
            expanded.Add(GetBlockDirectiveLine(string.Empty, 1, ".endblock"));

            SourceLine line = expanded[^1];

            return expanded;
        }

        /// <summary>
        /// Returns a new source line representing a block or endblock directive.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <param name="lineNumber">The source's line number.</param>
        /// <param name="directive">The directive name.</param>
        /// <returns></returns>
        public static SourceLine GetBlockDirectiveLine(string fileName, int lineNumber, string directive)
            => GetBlockDirectiveLine(fileName, lineNumber, string.Empty, directive);

        /// <summary>
        /// Returns a new source line representing a block or endblock directive.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <param name="lineNumber">The source's line number.</param>
        /// <param name="label">The label to attach the block line.</param>
        /// <param name="directive">The directive name.</param>
        /// <returns></returns>
        public static SourceLine GetBlockDirectiveLine(string fileName, int lineNumber, string label, string directive)
        {
            var blockLine = new SourceLine(fileName, lineNumber)
            {
                Instruction = new Token
                {
                    Name = directive,
                    Type = TokenType.Instruction
                }
            };
            if (!string.IsNullOrEmpty(label))
            {
                blockLine.Label = new Token
                {
                    Name = label,
                    Type = TokenType.Operand
                };
                blockLine.ParsedSource = $"{label} {directive}";
            }
            else
            {
                blockLine.ParsedSource = directive;
            }
            return blockLine;
        }

        /// <summary>
        /// Add a line of source to the macro definition.
        /// </summary>
        /// <param name="line">The source line to add to the macro's definition.</param>
        public void AddSource(SourceLine line)
        {
            var macroSource = new MacroSource
            {
                Line = line,
                ParamPlaces = new List<(int paramIndex, string reference, Token token)>()
            };

            if (line.OperandHasToken)
            {
                var tokenEnumerator = TokenEnumerator.GetEnumerator(line.Operand);
                var source = line.ParsedSource;
                foreach (Token op in tokenEnumerator.Where(o => o.Type == TokenType.Operand))
                {
                    if (op.Name[0] == '\\' || op.Name.EnclosedInDoubleQuotes())
                    {
                        int refPos = 0, originalPos = 0;
                        int length;
                        var inQuotes = op.Name.EnclosedInDoubleQuotes();
                        var refMark = inQuotes ? "@{" : "\\";
                        while (originalPos < op.Name.Length - 1 && (refPos = op.Name.Substring(originalPos).IndexOf(refMark)) > -1)
                        {
                            var afterMark = op.Name.Substring(originalPos + refPos + refMark.Length);
                            if (inQuotes)
                            {
                                length = afterMark.IndexOf("}");
                            }
                            else
                            {
                                if (char.IsLetter(afterMark[0]))
                                    length = afterMark.ToList().FindIndex(c => !char.IsLetterOrDigit(c));
                                else if (char.IsDigit(afterMark[0]))
                                    length = afterMark.ToList().FindIndex(c => !char.IsDigit(c));
                                else
                                    length = 0;
                                if (length < 0)
                                    length = afterMark.Length;
                            }
                            if (length > 0)
                            {
                                string reference;
                                if (inQuotes)
                                    reference = afterMark.Substring(0, length);
                                else
                                    reference = afterMark.Substring(0, length);
                                if (!int.TryParse(reference, out var paramIx))
                                    paramIx = Params.FindIndex(p => p.Name.Equals(reference));
                                else
                                    paramIx--;

                                reference = refMark + reference;
                                if (inQuotes)
                                    reference += "}";
                                if (paramIx < 0)
                                    throw new ExpressionException(originalPos, $"Reference parameter \"{reference}\" not defined.");
                                macroSource.ParamPlaces.Add((paramIx, reference, op));
                                originalPos += refPos + length;
                            }

                        }
                    }
                }
            }
            _sources.Add(macroSource);
        }
        #endregion
    }
}
