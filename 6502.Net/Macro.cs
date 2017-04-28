//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Asm6502.Net
{
    /// <summary>
    /// Represents a macro definition. Macros are small snippets of code that can be re-used
    /// and even parameterized. Parameters are text-substituted.
    /// </summary>
    public class Macro
    {
        #region Exception

        /// <summary>
        /// Represents a macro-related error that occurs during runtime.
        /// </summary>
        public class MacroException : Exception
        {
            #region Exception.Members

            private string message_;

            #endregion

            #region Exception.Constructors

            /// <summary>
            /// Constructs a new macro exception.
            /// </summary>
            /// <param name="line">The SourceLine where the exception occurred.</param>
            /// <param name="message">The error message.</param>
            public MacroException(SourceLine line, string message)
            {
                Line = line;
                message_ = message;
            }

            #endregion

            #region Exception.Methods

            /// <summary>
            /// Gets the exception message.
            /// </summary>
            public override string Message
            {
                get
                {
                    return message_;
                }
            }

            #endregion

            #region Exception.Properties

            /// <summary>
            /// Gets the SourceLine for which the exception occurred
            /// </summary>
            public SourceLine Line { get; private set; }

            #endregion
        }

        #endregion

        #region Subclass

        /// <summary>
        /// Represents a macro parameter.
        /// </summary>
        public class Param
        {
            #region Param.Constructor

            /// <summary>
            /// Constructs an new instance of a parameter.
            /// </summary>
            public Param()
            {
                Name = DefaultValue = Passed = string.Empty;
                Number = 0;
                SourceLines = new List<string>();
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
            
            /// <summary>
            /// Gets or sets the source line lists (SourceLine.SourceInfo) 
            /// that reference the parameter in macro source.
            /// </summary>
            public List<string> SourceLines { get; set; }

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

        /// <summary>
        /// Expands the macro into source from the invocation.
        /// </summary>
        /// <param name="macrocall">The SourceLine that is invoking the macro. The macro
        /// name is in the instruction, while the list of parameters passed 
        /// are in the operand.</param>
        /// <returns>A list of source from the expanded macro, included substituted 
        /// parameters in source.</returns>
        public List<SourceLine> Expand(SourceLine macrocall)
        {
            List<SourceLine> processed = new List<SourceLine>();
            List<Param> parms = new List<Param>(Params);

            if (IsSegment == false)
            {
                // any parameters passed?
                // get passed parameters
                var passed = macrocall.CommaSeparateOperand();


                // no passed parameters is ok
                if (passed == null)
                    passed = new List<string>();

                //  if passed exceeds defined parameters raise an error
                if (passed.Count > parms.Count)
                    throw new MacroException(macrocall, "Too many arguments");
                else if (passed.Count < parms.Count) // else pad passed to match defined
                    passed.AddRange(Enumerable.Repeat(string.Empty, parms.Count - passed.Count));

                for (int i = 0; i < passed.Count; i++)
                {
                    if (string.IsNullOrEmpty(passed[i]))
                    {
                        if (string.IsNullOrEmpty(parms[i].DefaultValue))
                            throw new MacroException(macrocall,
                                string.Format("Macro '{0}' expects a value for parameter {1}; no default value defined",
                                macrocall.Instruction.TrimStart('.'),
                                parms[i].Number));

                        parms[i].Passed = parms[i].DefaultValue;
                    }
                    else
                    {
                        parms[i].Passed = passed[i];
                    }
                }
            }
            
            foreach (var src in Source)
            {
                SourceLine repl = src.Clone() as SourceLine;
                if (object.ReferenceEquals(src, Source.First()))
                {
                    // if there is a label for the macro invocation, 
                    // put the scope into that.
                    repl.Label = macrocall.Label;
                }

                if (IsSegment == false)
                {
                    var insertedparm = parms.FirstOrDefault(pa =>
                                            pa.SourceLines.Any(pas =>
                                             pas == src.SourceInfo()));
                    if (insertedparm != null)
                    {
                        var passed_pattern = string.Format(@"\\{0}|\\{1}", insertedparm.Number, insertedparm.Name);
                        repl.Operand = Regex.Replace(src.Operand, passed_pattern,
                            m => insertedparm.Passed, RegexOptions.IgnoreCase);
                        repl.SourceString = Regex.Replace(repl.SourceString, passed_pattern,
                            insertedparm.Passed, RegexOptions.IgnoreCase); ;
                    }
                }

                processed.Add(repl);
            }
            return processed;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Define a macro from source.
        /// </summary>
        /// <param name="source">The source that defines the macro.</param>
        /// <param name="comparer">A string comparer (to support macro name resolution).</param>
        /// <param name="openBlock">The keyword or symbol for the scoped block opening.</param>
        /// <param name="closeBlock">The keyword or symbol for the scoped block closure.</param>
        /// <param name="isSegment">Sets whether the macro is a segment. Segments do not 
        /// enclose their expanded source upon invocation into local blocks, nor do they
        /// take parameters.</param>
        /// <returns>Returns a macro definition.</returns>
        public static Macro Create(IEnumerable<SourceLine> source, 
                                   StringComparison comparer,
                                   string openBlock,
                                   string closeBlock,
                                   bool isSegment)
        {
            var def = source.First();
            var macro = new Macro();
            string name = def.Label;
            string class_ = isSegment ? "Segment" : "Macro";
            if (def.IsComment == false)
            {
                
                macro.IsSegment = isSegment;
                if (macro.IsSegment == false)
                {
                    macro.Source.Add(new SourceLine());
                    macro.Source.First().Filename = def.Filename;
                    macro.Source.First().LineNumber = def.LineNumber;
                    macro.Source.First().Instruction = openBlock;

                    if (string.IsNullOrEmpty(def.Operand) == false)
                    {
                        var parms = def.CommaSeparateOperand();
                        if (parms == null)
                        {
                            throw new MacroException(def, "Invalid parameter(s) (" + def.Operand + ")");
                        }
                        for (int i = 0; i < parms.Count; i++)
                        {
                            var p = parms[i];
                            Macro.Param parm = new Macro.Param();
                            parm.Number = i + 1;
                            if (p.Contains("="))
                            {
                                string[] ps = p.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                                string pname = ps.First().Trim();
                                if (ps.Count() != 2)
                                {
                                    throw new MacroException(def, "Default parameter assignment error");
                                }
                                if (Regex.IsMatch(pname, @"[a-zA-Z][a-zA-Z0-9_]") == false)
                                {
                                    throw new MacroException(def, "Parameter name '" + pname + "' invalid");
                                }
                                parm.Name = pname;
                                parm.DefaultValue = ps.Last().Trim();
                            }
                            else if (Regex.IsMatch(p, @"[a-zA-Z][a-zA-Z0-9_]") == false)
                            {
                                throw new MacroException(def, "Parameter name '" + p + "' invalid");
                            }
                            else
                            {
                                parm.Name = p;
                                parm.DefaultValue = string.Empty;
                            }
                            // check for duplicate param names
                            if (macro.Params.Any(prm => parm.Name.Equals(prm.Name, comparer)))
                            {
                                throw new MacroException(def, "Duplicate parameter name found: " + parm.Name);
                            }
                            macro.Params.Add(parm);
                        }
                    }
                }
            }

            foreach (var line in source.Where(l => !l.IsComment))
            {
                if (object.ReferenceEquals(source.First(), line) ||
                    object.ReferenceEquals(source.Last(), line)) continue;

                if (def.Label.Equals(line.Instruction, comparer))
                {
                    throw new MacroException(line, string.Format(Resources.ErrorStrings.RecursiveMacro, line.Label));
                }
                var param_ix = line.Operand.IndexOf('\\');
                if (param_ix >= 0 && isSegment == false)
                {
                    if (line.Operand.EndsWith("\\"))
                    {
                        throw new MacroException(line, Resources.ErrorStrings.MacroParamNotSpecified);
                    }
                    else
                    {
                        string param = String.Empty;
                        if (char.IsLetterOrDigit(line.Operand.ElementAt(param_ix + 1)) == false)
                        {
                            throw new MacroException(line, Resources.ErrorStrings.MacroParamIncorrect);
                        }
                        foreach (var c in line.Operand.Substring(param_ix + 1, line.Operand.Length - param_ix - 1))
                        {
                            if (Regex.IsMatch(c.ToString(), @"[A-Z0-9_.]", RegexOptions.IgnoreCase))
                                param += c;
                            else
                                break;
                        }
                        if (string.IsNullOrEmpty(param))
                        {
                            throw new MacroException(line, Resources.ErrorStrings.MacroParamNotSpecified);
                        }

                        int paramref;

                        // is the parameter in the operand a number or named
                        if (int.TryParse(param, out paramref))
                        {
                            // if it is a number and higher than the number of explicitly
                            // defined params, just add it as a param
                            int paramcount = macro.Params.Count;
                            if (paramref > paramcount)
                            {
                                while (paramref > paramcount)
                                    macro.Params.Add(new Macro.Param { Number = ++paramcount });
                            }
                            else if (paramref < 1)
                            {
                                throw new MacroException(line, string.Format(Resources.ErrorStrings.InvalidParamRef, param));
                            }
                            paramref--;
                            macro.Params[paramref].SourceLines.Add(line.SourceInfo());
                        }
                        else
                        {
                            if (macro.Params.Any(p => p.Name == param) == false)
                            {
                                throw new MacroException(line, string.Format(Resources.ErrorStrings.InvalidParamRef, param));
                            }
                            var macparm = macro.Params.First(p => p.Name == param);
                            macparm.SourceLines.Add(line.SourceInfo());
                        }
                    }
                }
                macro.Source.Add(line);
            }
            def = source.Last();

            if (def.IsComment)
                throw new MacroException(def, string.Format(Resources.ErrorStrings.MissingClosureMacro, class_));

            if (string.IsNullOrEmpty(def.Operand) == false)
            {
                if (isSegment && !name.Equals(def.Operand, comparer))
                    throw new MacroException(def, string.Format(Resources.ErrorStrings.ClosureDoesNotCloseMacro, def.Instruction, "segment"));
                else
                    throw new MacroException(def, string.Format(Resources.ErrorStrings.DirectiveTakesNoArguments, def.Instruction));
            }
            if (macro.IsSegment == false)
            {
                macro.Source.Add(new SourceLine());
                macro.Source.Last().Filename = def.Filename;
                macro.Source.Last().LineNumber = def.LineNumber;
                macro.Source.Last().Label = def.Label;
                macro.Source.Last().Instruction = closeBlock;
            }
            return macro;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of parameters associated with the defined macro.
        /// </summary>
        public List<Param> Params { get; private set; }

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
