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

        private Regex _regStrFunc;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a DotNetAsm.StringAssemblerBase class.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssemblyController to associate</param>
        public StringAssemblerBase(IAssemblyController controller) :
            base(controller)
        {
            Reserved.DefineType("Functions", new string[] { "str" });

            Reserved.DefineType("Directives", new string[]
                {
                    ".cstring", ".lsstring", ".nstring", ".pstring", ".string"
                });

            _regStrFunc = new Regex(@"str(\(.+\))",
                Controller.Options.RegexOption | RegexOptions.Compiled);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts an expression to a string.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine associated to the expression</param>
        /// <param name="arg">The string expression to convert</param>
        /// <returns></returns>
        private string StringFromExpression(SourceLine line, string arg)
        {
            if (_regStrFunc.IsMatch(arg))
            {
                var m = _regStrFunc.Match(arg);
                string strval = m.Groups[1].Value;
                var param = strval.TrimStart('(').TrimEnd(')');
                if (string.IsNullOrEmpty(strval))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                    return string.Empty;
                }
                else
                {
                    var val = Controller.Evaluator.Eval(param);
                    return val.ToString();
                }
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
            var csvs = line.CommaSeparateOperand();
            int size = 0;
            foreach(string s in csvs)
            {
                if (s.EnclosedInQuotes())
                {
                    size += s.Length - 2;
                }
                else
                {
                    if (s == "?")
                    {
                        size++;
                    }
                    else
                    {
                        string atoi = StringFromExpression(line, s);
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

        protected override bool IsReserved(string token)
        {
            return Reserved.IsOneOf("Directives", token);
        }

        /// <summary>
        /// Assemble strings to the output.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        protected void AssembleStrings(SourceLine line)
        {
            string format = line.Instruction;

            if (format.Equals(".pstring", Controller.Options.StringComparison))
            {
                // we need to get the instruction size for the whole length, including all args
                int length = GetExpressionSize(line) - 1;
                if (length > 255)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.PStringSizeTooLarge);
                    return;
                }
                else if (length < 0)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments);
                    return;
                }
                else
                {
                    Controller.Output.Add(length, 1);
                }
            }
            else if (format.Equals(".lsstring", Controller.Options.StringComparison))
            {
                Controller.Output.Transforms.Push(delegate(byte b)
                {
                    if (b > 0x7F)
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.MSBShouldNotBeSet, b.ToString());
                        return 0;
                    }
                    return b <<= 1;
                });
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
                    string atoi = StringFromExpression(line, arg);

                    if (string.IsNullOrEmpty(atoi))
                    {
                        var val = Controller.Evaluator.Eval(arg);
                        Controller.Output.Add(val, val.Size());
                    }
                    else
                    {
                        Controller.Output.Add(atoi);
                    }
                }
                else
                {
                    string noquotes = arg.Trim('"');
                    if (string.IsNullOrEmpty(noquotes))
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments);
                        return;
                    }
                    Controller.Output.Add(noquotes);
                }
            }
            var lastbyte = Controller.Output.GetCompilation().Last();

            if (format.Equals(".lsstring", Controller.Options.StringComparison))
            {
                Controller.Output.ChangeLast(lastbyte | 1, 1);
            }
            else if (format.Equals(".nstring", Controller.Options.StringComparison))
            {
                if (lastbyte > 0x7F)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.MSBShouldNotBeSet, lastbyte.ToString());
                    return;
                }
                Controller.Output.ChangeLast(lastbyte | 0x80, 1);
            }

            if (format.Equals(".cstring", Controller.Options.StringComparison))
                Controller.Output.Add(0, 1);
            else if (format.Equals(".lsstring", Controller.Options.StringComparison))
                Controller.Output.Transforms.Pop(); // clean up again :)
        }
        #endregion
    }
}
