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
using System.Threading.Tasks;

namespace DotNetAsm
{
    /// <summary>
    /// A line assembler that processes conditional blocks for a DotNetAsm.AssemblyController.
    /// </summary>
    public class ConditionAssembler : AssemblerBase, ILineAssembler
    {
        #region Members

        private int _condLevel;
        private Stack<string> _condStack;
        private Stack<bool> _resultStack;
        private bool _doNotAsm;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a DotNetAsm.ConditionAssembler to process conditional directives in
        /// assembly source.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssemblyController to associate</param>
        public ConditionAssembler(IAssemblyController controller)
            : base(controller)
        {
            Reserved.DefineType("Conditionals", new string[]
                {
                    ".if", ".ifdef", ".ifndef", ".elif", ".elifdef", ".elifndef", ".else", ".endif"
                });

            _condLevel = 0;
            _doNotAsm = false;
            _condStack = new Stack<string>();
            _resultStack = new Stack<bool>();
        }

        #endregion

        public void AssembleLine(SourceLine line)
        {
            string instruction = Controller.Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();

            if (Reserved.IsReserved(instruction) == false)
            {
                line.DoNotAssemble = _doNotAsm;
                return;
            }

            line.DoNotAssemble = true;

            string lastcond = _condStack.Count > 0 ? _condStack.Peek() : string.Empty;

            if (instruction.StartsWith(".if"))
            {
                _resultStack.Push(!_doNotAsm);
                _condStack.Push(instruction);
            }
            else if (instruction.Equals(".else"))
            {
                _condStack.Pop();
                _condStack.Push(instruction);
            }
            else if (instruction.Equals(".endif"))
            {
                // .endif
                if (_condStack.Count == 0)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.ClosureDoesNotCloseBlock, instruction);
                    return;
                }
                _condStack.Pop();
            }
            if (_condStack.Count > _condLevel && _doNotAsm)
                return;

            if (instruction.Equals(".endif"))
            {
                _resultStack.Pop();
                _doNotAsm = false;
            }
            else
            {
                if (instruction.StartsWith(".if"))
                {
                    _condLevel = _condStack.Count;

                    if (instruction.Equals(".if"))
                        _doNotAsm = !Controller.Evaluator.EvalCondition(line.Operand);
                    else if (instruction.Equals(".ifdef"))
                        _doNotAsm = !Controller.Labels.ContainsKey(line.Operand);
                    else
                        _doNotAsm = Controller.Labels.ContainsKey(line.Operand);
                }
                else
                {
                    if (string.IsNullOrEmpty(lastcond) || (!instruction.Equals(".endif") && lastcond.Equals(".else")))
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.None);
                        return;
                    }

                    _doNotAsm = _resultStack.Peek();    // this can seem confusing but in this case
                                                        // not assembling correlates directly to the last result
                         if (_doNotAsm)
                        return;
                    else if (instruction.Equals(".else"))
                        _doNotAsm = !_doNotAsm;
                    else if (instruction.Equals(".elifdef"))
                        _doNotAsm = !Controller.Labels.ContainsKey(line.Operand);
                    else if (instruction.Equals(".elifndef"))
                         _doNotAsm = Controller.Labels.ContainsKey(line.Operand);
                    else if (instruction.Equals(".elif"))
                         _doNotAsm = !Controller.Evaluator.EvalCondition(line.Operand);
                }
                
                _resultStack.Pop(); // in the if ... elif ... else... block change the result
                _resultStack.Push(!_doNotAsm);
            }
        }

        public int GetInstructionSize(SourceLine line)
        {
            return 0;
        }

        public bool AssemblesInstruction(string instruction)
        {
            return IsReserved(instruction);
        }

        protected override bool IsReserved(string token)
        {
            return Reserved.IsReserved(token);
        }
    }
}
