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
                        message += csv[i].Trim('"');
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


        /// <summary>
        /// Handle first-pass assembly, returning true if another pass is needed.
        /// </summary>
        /// <param name="line">The SourceLine.</param>
        /// <returns>True if another pass is needed, otherwise false</returns>
        public bool HandleFirstPass(SourceLine line)
        {
            switch (line.Instruction.ToLower())
            {
                case ".pseudopc":
                case ".relocate":
                    {
                        Relocate(line);
                        if (line.PC != Controller.Output.GetPC())
                        {
                            line.PC = (ushort)Controller.Output.GetPC();
                            return true;
                        }
                        break;
                    }
                case ".endrelocate":
                case ".realpc":
                    {
                        if (string.IsNullOrEmpty(line.Operand) == false)
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.DirectiveTakesNoArguments);

                        var logical_pc = Controller.Output.SynchPC();
                        if (logical_pc != line.PC)
                        {
                            line.PC = logical_pc;
                            return true;
                        }
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Assemble the line of source into output bytes of the
        /// target architecture.
        /// </summary>
        /// <param name="evaluator">An expression evaluator.</param>
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
                        Controller.Log.LogEntry(line, line.Operand.Trim('"'));
                    break;
                case ".warn":
                    if (!line.Operand.EnclosedInQuotes())
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression);
                    else
                        Controller.Log.LogEntry(line.Filename, line.LineNumber, line.Operand.Trim('"'), Controller.Options.WarningsAsErrors);
                    break;
                case ".relocate":
                case ".pseudopc":
                    Relocate(line);
                    break;
                case ".realpc":
                case ".endrelocate":
                    Controller.Output.SynchPC();
                    break;
                case ".cpu":
                    break;
                default:
                   break;
            }
        }

        /// <summary>
        /// Perform the relocate directive to change the logical program
        /// counter.
        /// </summary>
        /// <param name="line">The SourceLine.</param>
        private void Relocate(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments, line.Instruction);
                return;
            }
            var relocval = Controller.Evaluator.Eval(line.Operand);
            if (relocval < short.MinValue || relocval > ushort.MaxValue)
                Controller.Log.LogEntry(line, Resources.ErrorStrings.IllegalQuantity, relocval.ToString());
            else
                Controller.Output.SetLogicalPC(Convert.ToInt32(relocval) & ushort.MaxValue);
        }

        /// <summary>
        /// Gets the size of the instruction in the source line.
        /// </summary>
        /// <param name="line">The source line to query.</param>
        /// <returns>Returns the size in bytes of the instruction or directive.</returns>
        public int GetInstructionSize(SourceLine line)
        {
            return 0;
        }

        /// <summary>
        /// Indicates whether this line assembler will assemble the 
        /// given instruction or directive.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <returns>True, if the line assembler can assemble the source, 
        /// otherwise false.</returns>
        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsReserved(instruction);
        }

        #endregion
    }
}
