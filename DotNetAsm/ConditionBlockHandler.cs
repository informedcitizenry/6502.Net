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
    public class ConditionBlockHandler : BlockHandler<ConditionBlockHandler.Condition>
    {
        public class Condition
        {
            public Condition(string cond, bool result)
            {
                ConditionName = cond;
                Result = result;
            }
            public string ConditionName { get; set; }
            public bool Result { get; set; }
        }

        private Stack<bool> _resultStack;
      
        public ConditionBlockHandler(IAssemblyController controller, List<SourceLine> source) :
            base(controller, source)
        {
            Reserved.DefineType("Conditionals", new string[]
                {
                    ".if", ".ifdef", ".ifndef", 
                    ".elif", ".elifdef", ".elifndef",
                    ".else", ".endif"
                });
            _resultStack = new Stack<bool>();
        }

        protected override void DoProcess(SourceLine line, BlockHandler<Condition>.Block<Condition> block)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                return;
            }

            string instruction = Controller.Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();
            
            if (instruction.StartsWith(".el"))
            {
                string lastcond = block.BackLink == null ? string.Empty : block.BackLink.Key.ConditionName;

                if (string.IsNullOrEmpty(lastcond) || lastcond.Equals(".else"))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                    return;
                }
                
                if (block.Key.Result)
                {
                    if (instruction.Equals(".else"))
                    {
                        block.Key.Result = !(block.Key.Result);
                    }
                    return;
                }
            }
 
            bool result = Controller.Evaluator.EvalCondition(line.Operand);

            block.Key = new Condition(instruction, result);
        }

        protected override void DoProcessBlock(BlockHandler<Condition>.Block<Condition> block, 
                                               List<SourceLine> source)
        {
            if (block.Key.Result)
            {
                ProcessEntries(block, source);
            }
        }

        protected override void DoReset()
        {
            _resultStack.Clear();
        }

        protected override bool IsOpen(string token)
        {
            if (Controller.Options.CaseSensitive == false)
                token = token.ToLower();
            if (Reserved.IsOneOf("Conditionals", token) == false)
                return false;
            return token.Contains("if") || token.Equals(".else");
        }

        protected override bool IsClosure(string token)
        {
            return string.IsNullOrEmpty(token) == false &&
                   token.Equals(".endif");
        }
    }
}
