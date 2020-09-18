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
    /// An error handling class for the <see cref="BlockAssembler"/>.
    /// </summary>
    public class BlockAssemblerException : Exception
    {
        /// <summary>
        /// Constructs a new instance of the <see cref="BlockAssemblerException"/>.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> that is the source of the exception.</param>
        public BlockAssemblerException(SourceLine line) : base($"Illegal use of {line.InstructionName}.") { }

        /// <summary>
        /// The <see cref="SourceLine"/> that is the source of the exception.
        /// </summary>
        public SourceLine Line { get; }   
    }

    /// <summary>
    /// Handles errors when function calls expect return values but
    /// none are returned.
    /// </summary>
    public class ReturnException : ExpressionException
    {
        /// <summary>
        /// Creates the new instance of the exception.
        /// </summary>
        /// <param name="position">The token that caused the exception.</param>
        /// <param name="message">The exception message.</param>
        public ReturnException(int position, string message)
            : base(position, message) { }
    }

    /// <summary>
    /// Responsible for handling directives that handle assembly over multiple lines, such as
    /// repetition and conditional directives.
    /// </summary>
    public sealed class BlockAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Data

        static readonly Dictionary<string, BlockType> s_blockOpenTypes
            = new Dictionary<string, BlockType>
        {
            { ".block",     BlockType.Scope           },
            { ".if",        BlockType.Conditional     },
            { ".ifdef",     BlockType.ConditionalDef  },
            { ".ifndef",    BlockType.ConditionalNdef },
            { ".for",       BlockType.ForNext         },
            { ".function",  BlockType.Functional      },
            { ".page",      BlockType.Page            },
            { ".repeat",    BlockType.Repeat          },
            { ".switch",    BlockType.Switch          },
            { ".while",     BlockType.While           },
            { ".goto",      BlockType.Goto            },
        };

        #endregion

        #region Members

        readonly Stack<BlockProcessorBase> _blocks;
        readonly RandomAccessIterator<SourceLine> _lineIterator;
        BlockProcessorBase _currentBlock;
        readonly Dictionary<string, Function> _functionDefs;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the multiline assembler class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="lineIterator">The line iterator.</param>
        public BlockAssembler(AssemblyServices services, 
                              RandomAccessIterator<SourceLine> lineIterator)
            :base(services)
        {
            _blocks = new Stack<BlockProcessorBase>();

            _lineIterator = lineIterator;

            _functionDefs = new Dictionary<string, Function>();

            _currentBlock = null;

            Reserved.DefineType("Scope", ".block", ".endblock");

            Reserved.DefineType("Conditional",
                ".if", ".ifdef", ".ifndef", ".else", ".elseif",
                ".elseifdef", ".elseifndef", ".endif");

            Reserved.DefineType("SwitchCase",
                ".switch", ".case", ".default", ".endswitch");

            Reserved.DefineType("Functional",
                ".function", ".endfunction");

            Reserved.DefineType("ForNext", ".for", ".next");

            Reserved.DefineType("While", ".while", ".endwhile");

            Reserved.DefineType("Repeat", ".repeat", ".endrepeat");

            Reserved.DefineType("BreakContinue", ".break", ".continue");

            Reserved.DefineType("Page", ".page", ".endpage");

            Reserved.DefineType("Goto", ".goto");

            ExcludedInstructionsForLabelDefines.Add(".function");

            Services.Evaluator.AddFunctionEvaluator(this);
        }

        #endregion

        #region Methods

        void DefineFunction(SourceLine line)
        {
            if (Services.CurrentPass == 0)
            {
                if (_currentBlock != null)
                    throw new SyntaxException(_currentBlock.Line.Instruction.Position,
                        $"Function cannot be defined inside a \"{_currentBlock.Line.InstructionName}\" block.");

                var fcnName = line.LabelName;
                if (string.IsNullOrEmpty(fcnName))
                    throw new SyntaxException(1, "Function name not specified.");
                if (!char.IsLetter(fcnName[0]))
                    throw new SyntaxException(line.Label.Position, $"Invalid function name \"{fcnName}\".");
                else if (_functionDefs.ContainsKey(fcnName))
                    throw new SyntaxException(line.Label.Position, $"Duplicate function declaration \"{fcnName}\".");
                else
                    _functionDefs.Add(fcnName, new Function(Services, line.Operand, _lineIterator));
            }
            else
            {
                new FunctionBlock(Services, _lineIterator, BlockType.Functional).SeekBlockEnd();
            }
        }

        BlockProcessorBase GetProcessor(BlockType type)
        {
            switch (type)
            {
                case BlockType.ForNext:
                    return new ForNextBlock(Services, _lineIterator, type);
                case BlockType.Repeat:
                    return new RepeatBlock(Services, _lineIterator, type);
                case BlockType.Conditional:
                case BlockType.ConditionalDef:
                case BlockType.ConditionalNdef:
                    return new ConditionalBlock(Services, _lineIterator, type);
                case BlockType.Scope:
                    return new ScopeBlock(Services, _lineIterator, type);
                case BlockType.Page:
                    return new PageBlockProcessor(Services, _lineIterator, type);
                case BlockType.Switch:
                    return new SwitchBlock(Services, _lineIterator, type);
                default:
                    return new WhileBlock(Services, _lineIterator, type);
            }
        }

        class BlockLineCount
        {
            public BlockLineCount(SourceLine line, int count)
            {
                Lines = new List<SourceLine> { line };
                Count = count;
            }
            public List<SourceLine> Lines { get; }
            public int Count { get; set;  }
        }

        void ScanBlock(BlockType type)
        {
            var index = _lineIterator.Index;
            SourceLine line = _lineIterator.Current;
            var blocks = new Stack<(SourceLine l, BlockDirective d)>();
            blocks.Push((line, BlockDirective.Directives[type]));
            while (((line = _lineIterator.FirstOrDefault(l =>
                BlockDirective.Directives.Values.Any(v => v.Open.Equals(l.InstructionName) || v.Closure.Equals(l.InstructionName)))) 
                != null) && blocks.Count > 0)
            {
                if (s_blockOpenTypes.TryGetValue(line.InstructionName, out type))
                {
                    blocks.Push((line, BlockDirective.Directives[type]));
                }
                else if (blocks.Peek().d.Closure.Equals(line.InstructionName))
                {
                    blocks.Pop();
                }
                else
                {
                    var blocksAsList = new List<(SourceLine l, BlockDirective d)>(blocks);
                    blocksAsList.RemoveAt(blocksAsList.FindIndex(blc => blc.d.Closure.Equals(line.InstructionName)));
                    blocksAsList.Reverse();
                    blocks = new Stack<(SourceLine l, BlockDirective d)>(blocksAsList);
                    Services.Log.LogEntry(line, line.Instruction,
                            $"Closure \"{line.Instruction}\" does not close block \"{blocks.Peek().d.Open}\".");
                }
            }
            _lineIterator.SetIndex(index);
            if (blocks.Count > 0)
            {
                var rev = new Stack<(SourceLine l, BlockDirective d)>();
                while (blocks.Count > 0)
                    rev.Push(blocks.Pop());
                while (rev.Count > 0)
                {
                    var (l, d) = rev.Pop();
                    Services.Log.LogEntry(l, l.Instruction,
                        $"Missing closure \"{d.Closure}\" for block \"{d.Open}\".");
                }
            }
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            if (Reserved.IsOneOf("Functional", line.InstructionName))
            {
                if (line.InstructionName.Equals(".function"))
                {
                    DefineFunction(line);
                }
                else if (_currentBlock != null)
                {
                    throw new SyntaxException(line.Instruction.Position,
                        $"Directive \"{line.InstructionName}\" can only be made inside function block.");
                }
                return string.Empty;
            }
            if (s_blockOpenTypes.TryGetValue(line.InstructionName, out BlockType type))
            {
                if (type == BlockType.Goto)
                    return DoGoto(line);

                var block = GetProcessor(type);
                if (_blocks.Count == 0)
                    ScanBlock(type);
                _blocks.Push(block);
                _currentBlock = block;
            }
            if (_currentBlock == null)
                throw new SyntaxException(line.Instruction.Position,
                    $"\"{line.InstructionName}\" directive must come inside a block.");
            var isBreakCont = _lineIterator.Current.InstructionName.Equals(".break") ||
                              _lineIterator.Current.InstructionName.Equals(".continue");
            if (isBreakCont)
            {
                var contBreakLine = _lineIterator.Current;
                if (!_currentBlock.AllowContinue &&
                    contBreakLine.InstructionName.Equals(".continue"))
                {
                    while (_currentBlock != null)
                    {
                        _currentBlock.SeekBlockEnd();
                        if (_currentBlock.AllowContinue)
                            break;
                        else
                            CheckClosureThenPop();
                    }
                    if (_currentBlock == null)
                        throw new SyntaxException(contBreakLine.Instruction.Position,
                           "No enclosing loop out of which to continue.");
                }
                else if (!_currentBlock.AllowBreak &&
                    contBreakLine.InstructionName.Equals(".break"))
                {
                    while (_currentBlock != null)
                    {
                        _currentBlock.SeekBlockEnd();
                        var allowBreak = _currentBlock.AllowBreak;
                        CheckClosureThenPop();
                        if (allowBreak)
                            return string.Empty;
                    }
                    if (_currentBlock == null)
                        throw new SyntaxException(contBreakLine.Instruction.Position,
                           "No enclosing loop out of which to break.");
                }
                else
                {
                    if (contBreakLine.InstructionName.Equals(".break"))
                    {
                        DoPop();
                        return string.Empty;
                    }
                    else
                    {
                        _currentBlock.SeekBlockEnd();
                    }
                }
            }
            var executeSuccess = _currentBlock.ExecuteDirective();
            if (!executeSuccess && !isBreakCont)
                throw new BlockAssemblerException(line);

            if (_lineIterator.Current == null ||
                _lineIterator.Current.InstructionName.Equals(BlockDirective.Directives[_currentBlock.Type].Closure))
            {

                if (_lineIterator.Current == null)
                {
                    line = SourceLineHelper.GetLastInstructionLine(_lineIterator);
                    Services.Log.LogEntry(line,
                        $"Missing closure for \"{BlockDirective.Directives[_currentBlock.Type].Open}\" directive.", true);
                }
                DoPop();
            }
            if (line.Label != null)
                return $".{Services.Output.LogicalPC,-42:x4}{line.UnparsedSource.Substring(line.Label.Position - 1, line.Label.Name.Length)}";
            return string.Empty;
        }

        string DoGoto(SourceLine line)
        {
            if (!line.OperandHasToken)
                throw new SyntaxException(line.Instruction.Position, 
                    "Destination not specified for \".goto\" directive.");

            var gotoExp = line.OperandExpression;
            if (!char.IsLetter(gotoExp[0]) && gotoExp[0] != '_')
            {
                Services.Log.LogEntry(line, line.Operand, "\".goto\" operand must be a label.");
            }
            else if (gotoExp.Equals(line.LabelName))
            {
                Services.Log.LogEntry(line, line.Instruction, "Destination cannot be the same line as \".goto\" directive.");
            }
            else
            {
                var iterCopy = new RandomAccessIterator<SourceLine>(_lineIterator, true);
                SourceLine currLine;
                if ((currLine = iterCopy.FirstOrDefault(l =>
                {
                    if (s_blockOpenTypes.TryGetValue(l.InstructionName, out BlockType type))
                    {
                        // skip over other blocks we are not calling .goto 
                        // from within (we can know this by checking if the block in the stack
                        // has an index matching the iterator's current index.
                        if (BlockDirective.Directives.ContainsKey(type) &&
                           !_blocks.Any(b => b.Index == iterCopy.Index))
                            GetProcessor(type).SeekBlockEnd(iterCopy);
                        return false;
                    }
                    return l.LabelName.Equals(gotoExp);
                })) != null)
                {
                    if (currLine.InstructionName.Contains("=") ||
                        currLine.InstructionName.Equals(".equ") ||
                        currLine.InstructionName.Equals(".global"))
                    {
                        Services.Log.LogEntry(line, line.Instruction, $"\"{gotoExp}\" is not a valid destination.");
                    }
                    else
                    {
                        while (_currentBlock != null)
                        {
                            // if where we landed lies outside of the current block scope
                            // we need to pop out of that scope.
                            _currentBlock.SeekBlockEnd();
                            if (iterCopy.Index > _currentBlock.Index)
                            {
                                // did we land in a place still within the block scope?
                                if (iterCopy.Index > _lineIterator.Index)
                                    // no, pop out
                                    DoPop();
                                else
                                    // we're still within the current block, don't pop it
                                    break;
                            }
                            else
                            {
                                // we went backwards, pop out of current scope
                                DoPop();
                            }
                        }
                        if (iterCopy.Index >= _lineIterator.Index)
                            _lineIterator.FastForward(iterCopy.Index);
                        else
                            _lineIterator.Rewind(iterCopy.Index - 1);
                    }
                }
                else
                {
                    Services.Log.LogEntry(line, line.Instruction,
                        $"Could not find destination \"{gotoExp}\".");
                }
            }
            return string.Empty;
        }

        void DoPop()
        {
            _currentBlock.PopScope();
            _blocks.Pop();
            if (_blocks.Count > 0)
                _currentBlock = _blocks.Peek();
            else
                _currentBlock = null;
        }

        void CheckClosureThenPop()
        {
            if (!_lineIterator.Current.InstructionName.Equals(_currentBlock.Directive.Closure))
            {
                _currentBlock = null;
                throw new SyntaxException(_lineIterator.Current.Instruction.Position,
                    $"Missing closure for \"{_currentBlock.Directive.Open}\" directive.");
            }
            DoPop();
        }

        double CallFunction(Token function, Token parameters, bool returnValueExpected)
        {
            if (!_functionDefs.ContainsKey(function.Name))
                throw new SyntaxException(function.Position, 
                    $"Unknown function name \"{function.Name}\".");

            var evalParms = parameters.Children.Where(p => p.Children.Count != 0)
                                               .Select(p => 
                                                    p.ToString().Trim().EnclosedInDoubleQuotes() ? 
                                                    p.ToString().Trim() : (object)Services.Evaluator.Evaluate(p))
                                               .ToList();
            // invoke the function, get the return
            Services.SymbolManager.PushScopeEphemeral();
            var value = _functionDefs[function.Name].Invoke(evalParms);
            Services.SymbolManager.PopScopeEphemeral();
            if (double.IsNaN(value) && returnValueExpected)
                throw new ReturnException(function.Position,
                    $"Function \"{function.Name}\" did not return a value.");
            return value;
        }

        public bool EvaluatesFunction(Token function) => _functionDefs.ContainsKey(function.Name);

        public double EvaluateFunction(Token function, Token parameters)
            => CallFunction(function, parameters, true);

        public void InvokeFunction(Token function, Token parameters)
            => CallFunction(function, parameters, false);

        public bool IsFunctionName(string symbol) => _functionDefs.ContainsKey(symbol);

        #endregion

        #region Properties

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

        #endregion
    }
}