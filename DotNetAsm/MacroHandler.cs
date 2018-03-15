//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
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

namespace DotNetAsm
{
    public sealed class MacroHandler : AssemblerBase, IBlockHandler
    {
        #region Members

        Dictionary<string, Macro> _macros;

        List<SourceLine> _expandedSource;

        Stack<List<SourceLine>> _macroDefinitions;

        Func<string, bool> _instructionFcn;

        Stack<SourceLine> _definitions;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.MacroHandler"/> class.
        /// </summary>
        /// <param name="controller">A <see cref="T:DotNetAsm.IAssemblyController"/>.</param>
        /// <param name="instructionFcn">The lookup function to validate whether the name is an instruction or directive.</param>
        public MacroHandler(IAssemblyController controller, Func<string, bool> instructionFcn)
            : base(controller)
        {
            Reserved.DefineType("Directives", ".macro", ".endmacro", ".segment", ".endsegment");
            _macros = new Dictionary<string, Macro>(controller.Options.StringComparar);
            _expandedSource = new List<SourceLine>();
            _macroDefinitions = new Stack<List<SourceLine>>();
            _instructionFcn = instructionFcn;
            _definitions = new Stack<SourceLine>();
        }

        public bool Processes(string token)
        {
            return Reserved.IsReserved(token) || _macros.ContainsKey(token);
        }

        public void Process(SourceLine line)
        {
            string instruction = line.Instruction.ToLower();
            if (!Processes(line.Instruction))
            {
                if (instruction.Equals(_macros.Last().Key))
                    Controller.Log.LogEntry(line, ErrorStrings.RecursiveMacro, line.Instruction);
                _macroDefinitions.Peek().Add(line);
                return;
            }

            if (instruction.Equals(".macro") || instruction.Equals(".segment"))
            {
                _macroDefinitions.Push(new List<SourceLine>());
                string name;
                _definitions.Push(line);
                if (instruction.Equals(".segment"))
                {
                    if (!string.IsNullOrEmpty(line.Label))
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.None);
                        return;
                    }
                    name = line.Operand;
                }
                else
                {
                    name = line.Label;
                }
                if (!Macro.IsValidMacroName(name) || _instructionFcn(name))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.LabelNotValid, line.Label);
                    return;
                }
                if (_macros.ContainsKey("." + name))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.MacroRedefinition, line.Label);
                    return;
                }
                _macros.Add("." + name, null);
            }
            else if (instruction.Equals(".endmacro") || instruction.Equals(".endsegment"))
            {
                string def = instruction.Replace("end",string.Empty);
                string name = "." + _definitions.Peek().Label;
                if (!_definitions.Peek().Instruction.Equals(def, Controller.Options.StringComparison))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.ClosureDoesNotCloseMacro, line.Instruction);
                    return;
                }
                if (def.Equals(".segment"))
                {
                    name = _definitions.Peek().Operand;
                    if (!name.Equals(line.Operand, Controller.Options.StringComparison))
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.None);
                        return;
                    }
                    name = "." + name;
                }
                _macros[name] = Macro.Create(_definitions.Pop(),
                                             line,
                                             _macroDefinitions.Pop(),
                                             Controller.Options.StringComparison,
                                             ConstStrings.OPEN_SCOPE,
                                             ConstStrings.CLOSE_SCOPE);
            }
            else
            {
                var macro = _macros[line.Instruction];
                if (macro == null)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.MissingClosureMacro, line.Instruction);
                    return;
                }
                if (IsProcessing())
                {
                    _macroDefinitions.Peek().Remove(line);
                    _macroDefinitions.Peek().AddRange(macro.Expand(line).ToList());
                }
                else
                {
                    _expandedSource = macro.Expand(line).ToList();
                }
            }
        }

        public void Reset()
        {
            _macroDefinitions.Clear();
            _expandedSource.Clear();
            _definitions.Clear();
        }

        public bool IsProcessing() => _definitions.Count > 0 || _macroDefinitions.Count > 0;

        public IEnumerable<SourceLine> GetProcessedLines() => _expandedSource;
    }
}
