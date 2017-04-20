//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
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

namespace Asm6502.Net
{
    /// <summary>
    /// Handles general assembly directives for the 6502.Net assembler.
    /// </summary>
    public class Directives6502 : AssemblerBase, ILineAssembler
    {
        #region Constructors

        /// <summary>
        /// Constructs a new directives assembler instance.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        public Directives6502(IAssemblyController controller) :
            base(controller)
        {
            Reserved.Types.Add("Directives", new HashSet<string>(new string[]
                {
                    ".eor", ".error", ".cerror", 
                    ".cwarn", ".relocate", ".pseudopc", ".realpc", ".endrelocate", ".warn", 
                }));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Throw a conditional error or warning.
        /// </summary>
        /// <param name="line">The SourceLine with the operand condition.</param>
        private void ThrowConditional(SourceLine line)
        {
            var csv = line.CommaSeparateOperand();
            if (csv.Count < 2)
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments);
                return;
            }
            if (Controller.Evaluator.EvalCondition(csv.First()))
            {
                string message = string.Empty;
                for (int i = 1; i < csv.Count; i++)
                {
                    if (csv[i].EnclosedInQuotes())
                    {
                        message += csv[i];
                    }
                    else
                    {
                        var value = Controller.Evaluator.Eval(csv[i]);
                        message += value.ToString();
                    }
                }
                if (line.Instruction.Equals(".cerror", Controller.Options.StringComparison))
                    Controller.Log.LogEntry(line, message);
                else
                    Controller.Log.LogEntry(line.Filename, line.LineNumber, message, Controller.Options.WarningsAsErrors);
            }
        }

        /// Sets the byte value to XOR all values when outputted to assembly.
        /// Used by the .eor directive.
        /// </summary>
        /// <param name="line">The SourceLine.</param>
        private void SetEor(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression);
                return;
            }
            Int64 eor = Controller.Evaluator.Eval(line.Operand);

            if (eor < 0) eor += 256;
            if (eor > 255 || eor < 0)
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.IllegalQuantity);
                return;
            }

            byte eor_b = Convert.ToByte(eor);
            Controller.Output.Transforms.Push(delegate(byte b)
            {
                b ^= eor_b;
                return b;
            });
        }

        public void AssembleLine(SourceLine line)
        {
            switch (line.Instruction.ToLower())
            {
                case ".cwarn":
                case ".cerror":
                    ThrowConditional(line);
                    break;
                case ".eor":
                    SetEor(line);
                    break;
                case ".error":
                    if (!line.Operand.EnclosedInQuotes())
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression);
                    else
                        Controller.Log.LogEntry(line, line.Operand);
                    break;
                case ".warn":
                    if (!line.Operand.EnclosedInQuotes())
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression);
                    else
                        Controller.Log.LogEntry(line.Filename, line.LineNumber, line.Operand, Controller.Options.WarningsAsErrors);
                    break;
                case ".relocate":
                case ".pseudopc":
                case ".realpc":
                case ".endrelocate":
                case ".cpu":
                    break;
                default:
                    Controller.Log.LogEntry(line,
                        string.Format("Directive '{0}' is  not supported at this time.", line.Instruction),
                        Controller.Options.WarningsAsErrors);
                    break;
            }
        }

        public int GetInstructionSize(SourceLine line)
        {
            return 0;
        }

        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsReserved(instruction);
        }

        #endregion
    }
}
