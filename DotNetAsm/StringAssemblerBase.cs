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
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// A base class to assemble string pseudo operations.
    /// </summary>
    public class StringAssemblerBase : AssemblerBase
    {
        #region Members

        Regex _regStrFunc, _regEncName;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a DotNetAsm.StringAssemblerBase class.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssemblyController to associate</param>
        public StringAssemblerBase(IAssemblyController controller) :
            base(controller)
        {
            Reserved.DefineType("Functions", "str");

            Reserved.DefineType("Directives", 
                    ".cstring", ".lsstring", ".nstring", ".pstring", ".string"
                );

            Reserved.DefineType("Encoding", ".encoding", ".map", ".unmap");

            _regStrFunc = new Regex(@"str(\(.+\))",
                Controller.Options.RegexOption | RegexOptions.Compiled);

            _regEncName = new Regex(@"^_?[a-z][a-z0-9_]*$",
                Controller.Options.RegexOption | RegexOptions.Compiled);

        }

        #endregion

        #region Methods

        /// <summary>
        /// Update the controller's encoding
        /// </summary>
        /// <param name="line">The SourceLine containing the encoding update</param>
        void UpdateEncoding(SourceLine line)
        {
            line.DoNotAssemble = true;
            string instruction = line.Instruction.ToLower();
            string encoding = Controller.Options.CaseSensitive ? line.Operand : line.Operand.ToLower();
            if (instruction.Equals(".encoding"))
            {
                if (!_regEncName.IsMatch(line.Operand))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.EncodingNameNotValid, line.Operand);
                    return;
                }
                Controller.Encoding.SelectEncoding(encoding);
            }
            else
            {
                var parms = line.CommaSeparateOperand();
                if (parms.Count == 0)
                    throw new ArgumentException(line.Operand);
                try
                {
                    string firstparm = parms.First();
                    string mapstring = string.Empty;
                    if (firstparm.EnclosedInQuotes() && firstparm.Length > 3)
                    {
                        if (firstparm.Length > 4)
                            throw new ArgumentException(firstparm);
                        mapstring = firstparm.Trim('"');
                    }
                    if (instruction.Equals(".map"))
                    {
                        if (parms.Count < 2 || parms.Count > 3)
                            throw new ArgumentException(line.Operand);
                        char translation = EvalEncodingParam(parms.Last());
                   
                        if (parms.Count == 2)
                        {
                            if (string.IsNullOrEmpty(mapstring) == false)
                            {
                                Controller.Encoding.Map(mapstring, translation);
                            }
                            else
                            {
                                char mapchar = EvalEncodingParam(firstparm);
                                Controller.Encoding.Map(mapchar, translation);
                            }
                        }
                        else
                        {
                            char firstRange = EvalEncodingParam(firstparm);
                            char lastRange = EvalEncodingParam(parms[1]);
                            Controller.Encoding.Map(firstRange, lastRange, translation);
                        }
                    }
                    else
                    {
                        if (parms.Count > 2)
                            throw new ArgumentException(line.Operand);
                        
                        if (parms.Count == 1)
                        {
                            if (string.IsNullOrEmpty(mapstring))
                            {
                                char unmap = EvalEncodingParam(firstparm);
                                Controller.Encoding.Unmap(unmap);
                            }
                            else
                            {
                                Controller.Encoding.Unmap(mapstring);
                            }
                        }
                        else
                        {
                            char firstunmap = EvalEncodingParam(firstparm);
                            char lastunmap = EvalEncodingParam(parms[1]);
                            Controller.Encoding.Unmap(firstunmap, lastunmap);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.BadExpression, line.Operand);
                }
            }
        }

        /// <summary>
        /// Evaluate parameter the string as either a char literal or expression
        /// </summary>
        /// <param name="p">The string parameter</param>
        /// <returns>A char representation of the parameter</returns>
        char EvalEncodingParam(string p)
        {
            // if char literal return the char itself
            if (p.EnclosedInQuotes())
            {
                if (p.Length != 3)
                    throw new ArgumentException(p);
                return p.Trim('"').First();
            }
            
            // else return the evaluated expression
            return (char)Controller.Evaluator.Eval(p);
        }

        /// <summary>
        /// Converts the numerical constant of a mathematical expression or to a string.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine associated to the expression</param>
        /// <param name="arg">The string expression to convert</param>
        /// <returns></returns>
        string ExpressionToString(SourceLine line, string arg)
        {
            if (_regStrFunc.IsMatch(arg))
            {
                var m = _regStrFunc.Match(arg);
                string strval = m.Groups[1].Value;
                var param = strval.TrimStart('(').TrimEnd(')');
                if (string.IsNullOrEmpty(strval) || 
                    strval.FirstParenEnclosure() != m.Groups[1].Value)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                    return string.Empty;
                }
                var val = Controller.Evaluator.Eval(param, int.MinValue, uint.MaxValue);
                return val.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Get the size of a string expression.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine associated to the expression</param>
        /// <returns>The size in bytes of the string expression</returns>
        protected int GetExpressionSize(SourceLine line)
        {
            if (Reserved.IsOneOf("Encoding", line.Instruction))
                return 0;

            var csvs = line.CommaSeparateOperand();
            int size = 0;
            foreach (string s in csvs)
            {
                if (s.EnclosedInQuotes())
                {
                    //var encodedString = Encoder.TranslateString(CurrentEncoding, s.Trim('"'));
                    size += Controller.Encoding.GetByteCount(s.Trim('"'));//encodedString.Count();
                }
                else
                {
                    if (s == "?")
                    {
                        size++;
                    }
                    else
                    {
                        string atoi = ExpressionToString(line, s);
                        if (string.IsNullOrEmpty(atoi))
                        {
                            var v = Controller.Evaluator.Eval(s);
                            size += v.Size();
                        }
                        else
                        {
                            size += atoi.Length;
                        }
                    }
                }
            }
            if (line.Instruction.Equals(".cstring", Controller.Options.StringComparison) ||
                line.Instruction.Equals(".pstring", Controller.Options.StringComparison))
                size++;
            return size;
        }

        public override bool IsReserved(string token)
        {
            return Reserved.IsOneOf("Directives", token) || 
                   Reserved.IsOneOf("Encoding", token);
        }

        /// <summary>
        /// Assemble strings to the output.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        protected void AssembleStrings(SourceLine line)
        {
            if (Reserved.IsOneOf("Encoding", line.Instruction))
            {
                UpdateEncoding(line);
                return;
            }
            string format = line.Instruction.ToLower();

            if (format.Equals(".pstring"))
            {
                try
                {
                    // we need to get the instruction size for the whole length, including all args
                    line.Assembly = Controller.Output.Add(Convert.ToByte(GetExpressionSize(line) - 1), 1);
                }
                catch (OverflowException)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.PStringSizeTooLarge);
                    return;
                }
            }
            else if (format.Equals(".lsstring"))
            {
                Controller.Output.Transforms.Push(b => Convert.ToByte(b << 1));
            }

            string operand = line.Operand;

            var args = line.CommaSeparateOperand();

            foreach (var arg in args)
            {
                if (arg.EnclosedInQuotes() == false)
                {
                    if (arg == "?")
                    {
                        Controller.Output.AddUninitialized(1);
                        continue;
                    }
                    string atoi = ExpressionToString(line, arg);

                    if (string.IsNullOrEmpty(atoi))
                    {
                        var val = Controller.Evaluator.Eval(arg);
                        line.Assembly.AddRange(Controller.Output.Add(val, val.Size()));
                    }
                    else
                    {
                        line.Assembly.AddRange(Controller.Output.Add(atoi));
                    }
                }
                else
                {
                    string noquotes = arg.Trim('"');
                    if (string.IsNullOrEmpty(noquotes))
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                        return;
                    }
                    line.Assembly.AddRange(Controller.Output.Add(noquotes, Controller.Encoding));
                }
            }
            var lastbyte = Controller.Output.GetCompilation().Last();

            if (format.Equals(".lsstring"))
            {
                line.Assembly[line.Assembly.Count - 1] = (byte)(lastbyte | 1);
                Controller.Output.ChangeLast(lastbyte | 1, 1);
            }
            else if (format.Equals(".nstring"))
            {
                line.Assembly[line.Assembly.Count - 1] = Convert.ToByte((lastbyte + 128));
                Controller.Output.ChangeLast(Convert.ToByte(lastbyte + 128), 1);
            }

            if (format.Equals(".cstring"))
            {
                line.Assembly.Add(0);
                Controller.Output.Add(0, 1);
            }
            else if (format.Equals(".lsstring"))
            {
                Controller.Output.Transforms.Pop(); // clean up again :)
            }
        }
        #endregion
    }
}