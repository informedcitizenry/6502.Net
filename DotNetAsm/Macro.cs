//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// Represents a macro definition. Macros are small snippets of code that can be re-used
    /// and even parameterized. Parameters are text-substituted.
    /// </summary>
    public class Macro
    {
        #region Subclass

        /// <summary>
        /// Represents a macro parameter.
        /// </summary>
        internal class Param
        {
            #region Param.Constructor

            /// <summary>
            /// Constructs an new instance of a parameter.
            /// </summary>
            public Param()
            {
                Name = DefaultValue = Passed = string.Empty;
                Number = 0;
            }
            #endregion

            #region Param.Properties

            /// <summary>
            /// Gets or sets the parameter name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the parameter number reference in the macro source.
            /// </summary>
            public int Number { get; set; }

            /// <summary>
            /// Gets or sets the default value of the parameter if no parameter
            /// passed by the client.
            /// </summary>
            public string DefaultValue { get; set; }

            /// <summary>
            /// Gets or sets the value passed by the client.
            /// </summary>
            public string Passed { get; set; }

            #endregion
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new macro instance.
        /// </summary>
        public Macro()
        {
            Params = new List<Param>();
            Source = new List<SourceLine>();
        }

        #endregion

        #region Methods

        private string ReplaceParameter(string paramName, List<Param> parms)
        {
            Param paramPassed = null;
            var paramNumber = -1;
            var paramPassedName = string.Empty;
            if (int.TryParse(paramName, out var number))
                paramNumber = number;

            if (paramNumber > 0)
                paramPassed = parms.FirstOrDefault(p => p.Number == paramNumber);
            else
                paramPassed = parms.FirstOrDefault(p => p.Name.Equals(paramName, Assembler.Options.StringComparison));
            if (paramPassed == null)
                throw new Exception(string.Format(ErrorStrings.InvalidParamRef, paramName));
            return paramPassed.Passed;
        }

        /// <summary>
        /// Expands the macro into source from the invocation.
        /// </summary>
        /// <param name="macrocall">The <see cref="T:SourceLine"/> that is invoking the macro. The macro
        /// name is in the instruction, while the list of parameters passed 
        /// are in the operand.</param>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerable&lt;DotNetAsm.SourceLine&gt;"/> 
        /// from the expanded macro, included substituted parameters in source.</returns>
        /// <exception cref="T:System.Exception"></exception>
        public IEnumerable<SourceLine> Expand(SourceLine macrocall)
        {
            if (!string.IsNullOrEmpty(macrocall.Label))
            {
                yield return new SourceLine
                {
                    Label = macrocall.Label,
                    Scope = macrocall.Scope
                };
            }

            if (IsSegment == false)
            {
                var parms = new List<Param>(Params);
                // any parameters passed?
                // get passed parameters
                List<string> passed = macrocall.Operand.CommaSeparate();

                // no passed parameters is ok
                if (passed == null)
                    passed = new List<string>();

                var useDefaultsIx = parms.Count - passed.Count - 1;
                if (useDefaultsIx > -1)
                {
                    while (useDefaultsIx < parms.Count)
                    {
                        if (string.IsNullOrEmpty(parms[useDefaultsIx].DefaultValue))
                        {
                            throw new Exception(string.Format(string.Format(ErrorStrings.MacroParamNoDefault,
                                    macrocall.Instruction.TrimStartOnce('.'),
                                    parms[useDefaultsIx].Number)));
                        }
                        parms[useDefaultsIx].Passed = parms[useDefaultsIx++].DefaultValue;
                    }
                }
                else
                {
                    for (var i = 0; i < passed.Count; i++)
                    {
                        if (i >= parms.Count)
                        {
                            parms.Add(new Param
                            {
                                Number = i + 1,
                                Passed = passed[i]
                            });
                        }
                        else if (string.IsNullOrEmpty(passed[i]))
                        {
                            if (string.IsNullOrEmpty(parms[i].DefaultValue))
                            {
                                throw new Exception(string.Format(ErrorStrings.MacroParamNoDefault,
                                    macrocall.Instruction.TrimStartOnce('.'),
                                    parms[i].Number));
                            }

                            parms[i].Passed = parms[i].DefaultValue;
                        }
                        else
                        {
                            parms[i].Passed = passed[i];
                        }
                    }
                }
                foreach (SourceLine src in Source)
                {
                    var repl = src.Clone() as SourceLine;
                    repl.Scope = macrocall.Scope + repl.Scope;
                    if (IsSegment == false)
                    {
                        for (var i = 0; i < repl.SourceString.Length; i++)
                        {
                            var c = repl.SourceString[i];
                            if (c == '\\')
                            {
                                var j = i + 1;
                                while (j < repl.SourceString.Length && char.IsLetterOrDigit(repl.SourceString[j]))
                                    j++;
                                if (j == i + 1)
                                    throw new Exception(ErrorStrings.MacroParamNotSpecified);
                                var paramName = repl.SourceString.Substring(i + 1, j - i - 1);
                                var paramPassed = ReplaceParameter(paramName, parms);
                                var rgx = new Regex(@"\\" + paramName, Assembler.Options.RegexOption);
                                if (rgx.IsMatch(repl.SourceString))
                                {
                                    repl.SourceString = rgx.Replace(repl.SourceString, paramPassed);
                                    i += paramPassed.Length - 1;
                                    repl.Reset();
                                }
                            }
                            else if (c == '"' || c == '\'')
                            {
                                var quoted = repl.SourceString.GetNextQuotedString(i, true);
                                if (!string.IsNullOrEmpty(quoted))
                                {
                                    i += quoted.Length + 1;
                                    quoted = Regex.Unescape(quoted);
                                    for (var j = i + 1; j < quoted.Length; j++)
                                    {
                                        var qc = quoted[j];
                                        if (qc == '@' && quoted[j + 1] == '{')
                                        {
                                            j += 2;
                                            var k = j;
                                            while (k < quoted.Length && quoted[k] != '}')
                                                k++;
                                            if (quoted[k] == '}')
                                            {
                                                var paramName = quoted.Substring(j, k - j);
                                                if ((char.IsDigit(paramName[0]) && qc == '$') ||
                                                    (char.IsLetter(paramName[0]) && qc == '{'))
                                                {
                                                    throw new Exception(ErrorStrings.None);
                                                }

                                                var paramPassed = ReplaceParameter(paramName, parms);
                                                var rgx = new Regex(@"@\{" + paramName + @"\}", Assembler.Options.RegexOption);
                                                if (rgx.IsMatch(quoted))
                                                {
                                                    quoted = rgx.Replace(quoted, paramPassed);
                                                    j += paramPassed.Length - 1;
                                                    repl.Reset();
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            else if (c == ';')
                            {
                                break;
                            }
                        }
                    }
                    yield return repl;
                }
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Create the specified definition, closure, source, comparer, openBlock and closeBlock.
        /// </summary>
        /// <returns>The resulting macro.</returns>
        /// <param name="definition">Definition.</param>
        /// <param name="closure">Closure.</param>
        /// <param name="source">Source.</param>
        /// <param name="comparer">Comparer.</param>
        /// <param name="openBlock">Open block.</param>
        /// <param name="closeBlock">Close block.</param>
        /// <exception cref="T:System.Exception"></exception>
        public static Macro Create(SourceLine definition,
                                           SourceLine closure,
                                           IEnumerable<SourceLine> source,
                                           StringComparison comparer,
                                           string openBlock,
                                           string closeBlock)
        {
            var macro = new Macro();
            var name = definition.Label;
            var isSegment = false;
            if (definition.Instruction.Equals(".segment", comparer))
            {
                isSegment = true;
                name = definition.Operand;
            }
            macro.IsSegment = isSegment;
            if (macro.IsSegment == false)
            {
                macro.Source.Add(new SourceLine());
                macro.Source.First().Filename = definition.Filename;
                macro.Source.First().LineNumber = definition.LineNumber;
                macro.Source.First().Instruction = openBlock;

                if (string.IsNullOrEmpty(definition.Operand) == false)
                {
                    List<string> parms = definition.Operand.CommaSeparate();
                    if (parms == null)
                    {
                        Assembler.Log.LogEntry(definition, ErrorStrings.InvalidParameters, definition.Operand);
                        return macro;
                    }
                    for (var i = 0; i < parms.Count; i++)
                    {
                        var p = parms[i];
                        var parm = new Macro.Param
                        {
                            Number = i + 1
                        };
                        if (p.Contains("="))
                        {
                            var ps = p.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                            var pname = ps.First().Trim();
                            if (ps.Count() != 2)
                                throw new Exception("Default parameter assignment error");
                            if (Regex.IsMatch(pname, Patterns.SymbolUnicode) == false)
                                throw new Exception(string.Format(ErrorStrings.ParameterNameInvalid, pname));
                            parm.Name = pname;
                            parm.DefaultValue = ps.Last().Trim();
                        }
                        else if (Regex.IsMatch(p, Patterns.SymbolUnicode) == false)
                        {
                            throw new Exception(string.Format(ErrorStrings.ParameterNameInvalid, p));
                        }
                        else
                        {
                            parm.Name = p;
                            parm.DefaultValue = string.Empty;
                        }
                        // check for duplicate param names
                        if (macro.Params.Any(prm => parm.Name.Equals(prm.Name, comparer)))
                            throw new Exception("Duplicate parameter name found: " + parm.Name);
                        macro.Params.Add(parm);
                    }
                }
            }
            foreach (SourceLine line in source.Where(l => !l.IsComment))
            {
                if (object.ReferenceEquals(definition, line) ||
                    object.ReferenceEquals(closure, line))
                {
                    continue;
                }

                if ((isSegment && line.Instruction.Equals("." + name, comparer))
                    || line.Instruction.Equals("." + name, comparer))
                {
                    throw new Exception(string.Format(ErrorStrings.RecursiveMacro, line.Label));
                }

                macro.Source.Add(line);
            }
            if (string.IsNullOrEmpty(closure.Operand) == false)
            {
                if (isSegment && !name.Equals(closure.Operand, comparer))
                    throw new Exception(string.Format(ErrorStrings.ClosureDoesNotCloseMacro, definition.Instruction, "segment"));
                if (!isSegment)
                    throw new Exception(string.Format(ErrorStrings.DirectiveTakesNoArguments, definition.Instruction));
            }
            if (macro.IsSegment == false)
            {
                macro.Source.Add(new SourceLine
                {
                    Filename = closure.Filename,
                    LineNumber = closure.LineNumber,
                    Label = closure.Label,
                    Instruction = closeBlock
                });
            }
            return macro;
        }

        #region Static Methods

        /// <summary>
        /// Determines whether the given token is a valid macro name.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns><c>True</c> if the token is a valid macro name,
        /// otherwise<c>false</c>.</returns>
        public static bool IsValidMacroName(string token) =>
            Regex.IsMatch(token, "^\\.?" + Patterns.SymbolUnicodeNoLeadingUnderscore + "$");

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of parameters associated with the defined macro.
        /// </summary>
        internal List<Param> Params { get; private set; }

        /// <summary>
        /// Gets the macro source.
        /// </summary>
        public List<SourceLine> Source { get; private set; }

        /// <summary>
        /// Gets or sets the flag that determines whether the macro definition is a segment.
        /// </summary>
        public bool IsSegment { get; set; }

        #endregion
    }
}