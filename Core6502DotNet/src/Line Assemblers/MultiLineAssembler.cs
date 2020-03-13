//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// Responsible for handling directives that handle assembly over multiple lines, such as
    /// repetition and conditional directives.
    /// </summary>
    public sealed class MultiLineAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Subclasses



        static readonly Dictionary<string, BlockType> _blockOpenTypes = new Dictionary<string, BlockType>
        {
            { ".block",     BlockType.Scope           },
            { ".if",        BlockType.Conditional     },
            { ".ifdef",     BlockType.ConditionalDef  },
            { ".ifndef",    BlockType.ConditionalNdef },
            { ".for",       BlockType.ForNext         },
            { ".function",  BlockType.Functional      },
            { ".repeat",    BlockType.Repeat          },
            { ".switch",    BlockType.Switch          },
            { ".while",     BlockType.While           },
            { ".goto",      BlockType.Goto            },
        };

        #endregion

        #region Members

        readonly Stack<BlockProcessorBase> _blocks;
        BlockProcessorBase _currentBlock;
        readonly Dictionary<string, Function> _functionDefs;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the multiline assembler class.
        /// </summary>
        public MultiLineAssembler()
        {
            _blocks = new Stack<BlockProcessorBase>();

            _functionDefs = new Dictionary<string, Function>();

            _currentBlock = null;

            Reserved.DefineType("Scope", ".block", ".endblock");

            Reserved.DefineType("Conditional",
                ".if", ".ifdef", ".ifndef", ".else", ".elseif",
                ".elseifdef", ".elseifndef", ".endif");

            Reserved.DefineType("SwitchCase",
                ".switch", ".case", ".default", ".endswitch");

            Reserved.DefineType("Functional",
                ".function", ".endfunction", ".invoke", ".return");

            Reserved.DefineType("ForNext", ".for", ".next");

            Reserved.DefineType("While", ".while", ".endwhile");

            Reserved.DefineType("Repeat", ".repeat", ".endrepeat");

            Reserved.DefineType("BreakContinue", ".break", ".continue");

            Reserved.DefineType("GotoEnd", ".goto", ".end");

            Evaluator.AddFunctionEvaluator(this);

            Assembler.PassChanged += CheckCurrentLineAtPass;
        }

        #endregion

        void CheckCurrentLineAtPass(object sender, System.EventArgs args)
        {
            if (_currentBlock != null)
            {
                var open = BlockDirective.Directives[_currentBlock.Type].Open;
                throw new Exception($"End of assembly reached without closing \"{open}\".");
            }
        }

        void DefineFunction(SourceLine line)
        {
            if (Assembler.CurrentPass == 0)
            {
                if (_currentBlock != null)
                {
                    Assembler.Log.LogEntry(line, line.Instruction,
                        $"Function cannot be defined inside a \"{_currentBlock.Line.InstructionName}\" block.");
                    return;
                }
                var fcnName = line.LabelName;
                if (!char.IsLetter(fcnName[0]))
                    Assembler.Log.LogEntry(line, line.Label, $"Invalid function name \"{fcnName}\".");
                else if (_functionDefs.ContainsKey(fcnName))
                    Assembler.Log.LogEntry(line, line.Label, $"Duplicate function declaration \"{fcnName}\".");
                else
                    _functionDefs.Add(fcnName, new Function(line.Operand));
            }
            else
            {
                new FunctionBlock(line, BlockType.Functional).SeekBlockEnd();
            }
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            if (line.InstructionName.Equals(".end"))
            {
                while (Assembler.LineIterator.MoveNext()) { }
                return string.Empty;
            }
            if (Reserved.IsOneOf("Functional", line.InstructionName))
            {

                if (line.InstructionName.Equals(".function"))
                {
                    DefineFunction(line);
                }
                else if (line.InstructionName.Equals(".invoke"))
                {
                    if (!line.OperandHasToken)
                    {
                        Assembler.Log.LogEntry(line, line.Operand, "Missing function name from invocation directive.");
                    }
                    else
                    {
                        var fcnName = line.Operand.Children[0].Children[0].Name;
                        if (line.Operand.Children[0].Children.Count > 2 ||
                            line.Operand.Children[0].Children[0].OperatorType != OperatorType.Function)
                            Assembler.Log.LogEntry(line, line.Operand.LastChild, "Bad function call.");
                        else if (!_functionDefs.ContainsKey(fcnName))
                            Evaluator.Evaluate(line.Operand.Children[0]);
                        else
                            _ = CallFunction(line.Operand.Children[0].Children[0],
                                                     line.Operand.Children[0].Children[1],
                                                     false);
                    }
                }
                else
                {
                    Assembler.Log.LogEntry(line, line.Instruction, $"Directive \"{line.InstructionName}\" can only be made inside function block.");
                }
                return string.Empty;
            }
            if (_blockOpenTypes.ContainsKey(line.InstructionName))
            {
                BlockProcessorBase block;
                BlockType type = _blockOpenTypes[line.InstructionName];
                switch (type)
                {
                    case BlockType.ForNext:
                        block = new ForNextBlock(line, type);
                        break;
                    case BlockType.Repeat:
                        block = new RepeatBlock(line, type);
                        break;
                    case BlockType.Conditional:
                    case BlockType.ConditionalDef:
                    case BlockType.ConditionalNdef:
                        block = new ConditionalBlock(line, type);
                        break;
                    case BlockType.Scope:
                        block = new ScopeBlock(line, type);
                        break;
                    case BlockType.Goto:
                        DoGoto(line);
                        return string.Empty;
                    case BlockType.Switch:
                        block = new SwitchBlock(line, type);
                        break;
                    default:
                        block = new WhileBlock(line, type);
                        break;
                }
                _blocks.Push(block);
                _currentBlock = block;
            }
            if (_currentBlock != null)
            {
                if (Assembler.CurrentLine.InstructionName.Equals(".break") ||
                    Assembler.CurrentLine.InstructionName.Equals(".continue"))
                {
                    SourceLine contBreakLine = Assembler.LineIterator.Current;
                    if (!_currentBlock.AllowBreak)
                    {
                        _currentBlock.SeekBlockEnd();
                        while (_currentBlock != null)
                        {
                            _currentBlock.SeekBlockEnd();
                            if (_currentBlock.AllowBreak)
                            {
                                if (contBreakLine.InstructionName.Equals(".break"))
                                {
                                    PopBlock();
                                    return string.Empty;
                                }
                                break;
                            }
                            else
                            {
                                PopBlock();
                            }
                        }
                        if (_currentBlock == null)
                        {
                            Assembler.Log.LogEntry(contBreakLine, contBreakLine.Instruction,
                               "No enclosing loop out of which to break or continue.");
                            return string.Empty;
                        }
                    }
                    else
                    {
                        _currentBlock.SeekBlockEnd();
                        if (contBreakLine.Instruction.Equals(".break"))
                            return string.Empty;
                    }
                }
                _currentBlock.ExecuteDirective();

                if (Assembler.CurrentLine == null ||
                    Assembler.CurrentLine.InstructionName.Equals(BlockDirective.Directives[_currentBlock.Type].Closure))
                {
                    if (Assembler.CurrentLine == null)
                    {
                        line = SourceLineHelper.GetLastInstructionLine();
                        Assembler.Log.LogEntry(line,
                            $"Missing closure for \"{BlockDirective.Directives[_currentBlock.Type].Open}\" directive.", true);
                    }
                    DoPop();
                }
            }
            else
            {
                Assembler.Log.LogEntry(line, line.Instruction.Position, $"\"{line.InstructionName}\" directive must come inside a block.");
            }
            if (line.Label != null)
                return $".{Assembler.Output.LogicalPC,-42:x4}{line.UnparsedSource.Substring(line.Label.Position - 1, line.Label.Name.Length)}";
            return string.Empty;
        }

        void DoGoto(SourceLine line)
        {

            if (!line.OperandHasToken)
            {
                Assembler.Log.LogEntry(line, line.Instruction, "Destination not specified for \".goto\" directive.");
                return;
            }
            var gotoExp = line.OperandExpression;
            if (gotoExp.Equals(line.LabelName))
            {
                Assembler.Log.LogEntry(line, line.Instruction, "Destination cannot be the same line as \".goto\" directive.");
                return;
            }

            var iterCopy = new RandomAccessIterator<SourceLine>(Assembler.LineIterator);
            iterCopy.Reset();

            SourceLine currLine;
            while ((currLine = iterCopy.Skip(l => !l.LabelName.Equals(gotoExp))) != null)
            {
                if (currLine.InstructionName.Contains("=") ||
                    currLine.InstructionName.Equals(".equ") ||
                    currLine.Instruction.Equals(".let"))
                {
                    Assembler.Log.LogEntry(line, line.Instruction, $"\"{gotoExp}\" is not a valid destination.");
                    return;
                }
                if (iterCopy.Index >= Assembler.LineIterator.Index)
                {
                    Assembler.LineIterator.FastForward(iterCopy.Index);
                }
                else
                {
                    if (iterCopy.Index == 0)
                        Assembler.LineIterator.Reset();
                    else
                        Assembler.LineIterator.Rewind(iterCopy.Index);
                }
                return;
            }
            Assembler.Log.LogEntry(line, line.Instruction,
                    $"Could not find destination \"{gotoExp}\".");
        }

        void DoPop()
        {
            if (_currentBlock != null)
                _currentBlock.Pop();
            _blocks.Pop();
            if (_blocks.Count > 0)
                _currentBlock = _blocks.Peek();
            else
                _currentBlock = null;
        }

        void PopBlock()
        {
            if (!Assembler.CurrentLine.InstructionName.Equals(_currentBlock.Directive.Closure))
            {
                Assembler.Log.LogEntry(Assembler.CurrentLine, Assembler.CurrentLine.Instruction,
                    $"Missing closure for \"{_currentBlock.Directive.Open}\" directive.");
                _currentBlock = null;
            }
            else
            {
                DoPop();
            }
        }

        public double CallFunction(Token function, Token parameters, bool returnValueExpected)
        {
            if (!_functionDefs.ContainsKey(function.Name))
                throw new ExpressionException(function.Position, $"Unknown function name \"{function.Name}\".");

            var evalParms = new List<object>(parameters.Children.Count);
            foreach (Token parm in from parm in parameters.Children
                                   where parm.HasChildren
                                   select parm)
            {
                if (parm.ToString().EnclosedInDoubleQuotes())
                    evalParms.Add(parm.ToString());
                else
                    evalParms.Add(Evaluator.Evaluate(parm));
            }
            // save pre-call context
            var returnIndex = Assembler.LineIterator.Index;
            BlockProcessorBase lastBlock = _currentBlock;
            Assembler.SymbolManager.PushScopeEphemeral();

            var fcnBlock = new FunctionBlock(Assembler.LineIterator.Current, BlockType.Functional);
            _blocks.Push(fcnBlock);
            _currentBlock = fcnBlock;

            // invoke the function, get the return
            var value = _functionDefs[function.Name].Invoke(this, evalParms);

            // restore context post-call
            while (_currentBlock != lastBlock)
            {
                _currentBlock.SeekBlockEnd();
                PopBlock();
            }
            Assembler.SymbolManager.PopScope();
            Assembler.LineIterator.SetIndex(returnIndex);

            if (double.IsNaN(value) && returnValueExpected)
                throw new ExpressionException(function.Position, $"Function \"{function.Name}\" did not return a value.");

            return value;
        }

        public bool EvaluatesFunction(Token function) => _functionDefs.ContainsKey(function.Name);

        public double EvaluateFunction(Token function, Token parameters)
            => CallFunction(function, parameters, true);

        /// <summary>
        /// Gets the flag that determines if the multiline assembler is actively evaluating a directive.
        /// This indicates that a corresponding closure to a block opening has not been encountered.
        /// </summary>
        public bool InAnActiveBlock => _currentBlock != null;

        /// <summary>
        /// The active block line.
        /// </summary>
        public SourceLine ActiveBlockLine
        {
            get
            {
                if (_currentBlock != null)
                    return _currentBlock.Line;
                return null;
            }
        }
    }
}
