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
        public BlockAssemblerException(SourceLine line) : base($"Illegal use of {line.Instruction.Name}.") { }

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
    public class BlockAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Members

        readonly Stack<BlockProcessorBase> _blocks;
        readonly Dictionary<StringView, StringView> _openClosures;
        BlockProcessorBase _currentBlock;
        readonly Dictionary<StringView, Function> _functionDefs;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a block assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public BlockAssembler(AssemblyServices services)
            : base(services)
        {
            _blocks = new Stack<BlockProcessorBase>();
            _functionDefs = new Dictionary<StringView, Function>(services.StringViewComparer);

            _currentBlock = null;

            _openClosures = new Dictionary<StringView, StringView>(services.StringViewComparer)
            {
                { ".block",     ".endblock"     },
                { ".for",       ".next"         },
                { ".function",  ".endfunction"  },
                { ".if",        ".endif"        },
                { ".ifdef",     ".endif"        },
                { ".ifndef",    ".endif"        },
                { ".namespace", ".endnamespace" },
                { ".page",      ".endpage"      },
                { ".repeat",    ".endrepeat"    },
                { ".switch",    ".endswitch"    },
                { ".while",     ".endwhile"     }
            };

            Reserved.DefineType("Scope",
                ".block", ".endblock", ".namespace", ".endnamespace");

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

            Services.IsReserved.Add(s => _functionDefs.ContainsKey(s));
        }

        #endregion

        #region Methods

        BlockProcessorBase GetProcessor(SourceLine line, int iterationIndex)
        {
            var name = line.Instruction.Name;
            var type = Services.Options.CaseSensitive ? name.ToString() : name.ToLower();
            switch (type)
            {
                case ".block":
                    return new ScopeBlock(Services, iterationIndex);
                case ".for":
                    return new ForNextBlock(Services, iterationIndex);
                case ".if":
                case ".ifdef":
                case ".ifndef":
                    return new ConditionalBlock(Services, iterationIndex);
                case ".namespace":
                    return new NamespaceBlock(Services, iterationIndex);
                case ".repeat":
                    return new RepeatBlock(Services, iterationIndex);
                case ".switch":
                    return new SwitchBlock(Services, iterationIndex);
                case ".while":
                    return new WhileBlock(Services, iterationIndex);
                default:
                    return null;
            }
        }

        void ScanBlock(RandomAccessIterator<SourceLine> lines)
        {
            var ix = lines.Index;
            var open = lines.Current.Instruction.Name;
            var closure = _openClosures[open];
            var opens = 1;
            while (lines.MoveNext() && opens > 0)
            {
                if (lines.Current.Instruction != null)
                {
                    if (lines.Current.Instruction.Name.Equals(closure, Services.StringViewComparer))
                        opens--;
                    else if (lines.Current.Instruction.Name.Equals(open, Services.StringViewComparer))
                        opens++;
                }
            }
            if (opens != 0)
            {
                if (lines.Current == null)
                {
                    lines.Reset();
                    lines.MoveNext();
                }
                else
                {
                    lines.Rewind(ix);
                }
                throw new SyntaxException(lines.Current.Instruction.Position,
                    $"Missing closure \"{closure}\" for block \"{open}\".");
            }
            lines.SetIndex(ix);
        }

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (Reserved.IsOneOf("Goto", line.Instruction.Name))
                return DoGoto(lines);

            if (Reserved.IsOneOf("Functional", line.Instruction.Name))
            {
                if (line.Instruction.Name.Equals(".function", Services.StringComparison))
                    DefineFunction(lines);
                else if (_currentBlock != null)
                    throw new SyntaxException(line.Instruction.Position,
                        "Directive \".endfunction\" can only be made inside function block.");
                return string.Empty;
            }
            if (_openClosures.ContainsKey(line.Instruction.Name))
            {
                var block = GetProcessor(line, lines.Index);
                if (_blocks.Count == 0)
                    ScanBlock(lines);
                _blocks.Push(block);
                _currentBlock = block;
            }
            if (_currentBlock == null)
                throw new SyntaxException(line.Instruction.Position,
                    $"\"{line.Instruction.Name}\" directive must come inside a block.");
            var isBreakCont = Reserved.IsOneOf("BreakContinue", line.Instruction.Name);
            if (isBreakCont)
            {
                var isBreak = line.Instruction.Name.Equals(".break", Services.StringComparison);
                if ((!_currentBlock.AllowContinue && line.Instruction.Name.Equals(".continue", Services.StringComparison)) ||
                    (!_currentBlock.AllowBreak && isBreak))
                {
                    while (_currentBlock != null)
                    {
                        var allowBreak = false;
                        _currentBlock.SeekBlockEnd(lines);
                        if (isBreak)
                            allowBreak = _currentBlock.AllowBreak;
                        else if (!isBreak && _currentBlock.AllowContinue)
                            break;
                        DoPop(lines);
                        if (isBreak && allowBreak)
                            return string.Empty;
                    }
                    if (_currentBlock == null)
                        throw new SyntaxException(line.Instruction.Position,
                           "No enclosing loop out of which to continue.");
                }
                else if (isBreak)
                {
                    DoPop(lines);
                    return string.Empty;
                }
                else
                {
                    _currentBlock.SeekBlockEnd(lines);
                }
            }
            _currentBlock.ExecuteDirective(lines);
            if (lines.Current.Instruction != null && lines.Current.Instruction.Name.Equals(_currentBlock.BlockClosure, Services.StringComparison))
                DoPop(lines);
            if (line.Label != null)
                return $".{Services.Output.LogicalPC,-42:x4}{line.Source.Substring(line.Label.Position - 1, line.Label.Name.Length)}";
            return string.Empty;
        }

        string DoGoto(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (line.Operands.Count == 0)
                throw new SyntaxException(line.Instruction.Position,
                    "Destination not specified for \".goto\" directive.");

            var gotoExp = line.Operands[0].Name;
            if ((!char.IsLetter(gotoExp[0]) && gotoExp[0] != '_') || line.Operands.Count > 1)
            {
                Services.Log.LogEntry(line.Operands[0],
                    "\".goto\" operand must be a label.");
            }
            else if (line.Label != null && gotoExp.Equals(line.Label.Name, Services.StringViewComparer))
            {
                Services.Log.LogEntry(line.Instruction,
                    "Destination cannot be the same line as \".goto\" directive.");
            }
            else
            {
                var iterCopy = new RandomAccessIterator<SourceLine>(lines, true);
                SourceLine currLine;
                if ((currLine = iterCopy.FirstOrDefault(l =>
                {
                    if (l.Instruction != null && _openClosures.ContainsKey(l.Instruction.Name))
                    {
                        if (!_blocks.Any(b => b.Index == iterCopy.Index))
                            GetProcessor(l, iterCopy.Index).SeekBlockEnd(iterCopy);
                        return false;
                    }
                    return l.Label != null && l.Label.Name.Equals(gotoExp, Services.StringViewComparer);
                })) != null)
                {
                    if (currLine.Instruction != null &&
                        (currLine.Instruction.Name.Contains('=') ||
                         currLine.Instruction.Name.Equals(".equ", Services.StringComparison) ||
                         currLine.Instruction.Name.Equals(".global", Services.StringComparison)
                        )
                       )
                    {
                        Services.Log.LogEntry(line.Instruction,
                            $"\"{gotoExp}\" is not a valid destination.");
                    }
                    else
                    {
                        while (_currentBlock != null)
                        {
                            // if where we landed lies outside of the current block scope
                            // we need to pop out of that scope.
                            _currentBlock.SeekBlockEnd(lines);
                            if (iterCopy.Index > _currentBlock.Index)
                            {
                                // did we land in a place still within the block scope?
                                if (iterCopy.Index > lines.Index)
                                    // no, pop out
                                    DoPop(lines);
                                else
                                    // we're still within the current block, don't pop it
                                    break;
                            }
                            else
                            {
                                // we went backwards, pop out of current scope
                                DoPop(lines);
                            }
                        }
                        if (iterCopy.Index >= lines.Index)
                            lines.FastForward(iterCopy.Index);
                        else if (iterCopy.Index == 0)
                            lines.Reset();
                        else
                            lines.Rewind(iterCopy.Index - 1);
                    }
                }
                else
                {
                    Services.Log.LogEntry(line.Instruction,
                        $"Could not find destination \"{gotoExp}\".");
                }
            }
            return string.Empty;
        }

        void DoPop(RandomAccessIterator<SourceLine> lines)
        {
            _currentBlock.PopScope(lines);
            _blocks.Pop();
            if (_blocks.Count > 0)
                _currentBlock = _blocks.Peek();
            else
                _currentBlock = null;
        }

        double CallFunction(RandomAccessIterator<Token> tokens, bool returnValueExpected)
        {
            var functionToken = tokens.Current;
            var functionName = functionToken.Name;
            tokens.MoveNext();
            var evalParms = new List<object>();
            Token token = tokens.GetNext();

            while (!token.Name.Equals(")"))
            {
                if (token.IsSeparator())
                    token = tokens.GetNext();
                if (StringHelper.ExpressionIsAString(tokens, Services))
                    evalParms.Add(StringHelper.GetString(tokens, Services));
                else
                    evalParms.Add(Services.Evaluator.Evaluate(tokens, false));
                token = tokens.Current;
            }
            Services.SymbolManager.PushScopeEphemeral();
            var value = _functionDefs[functionName].Invoke(evalParms);
            Services.SymbolManager.PopScopeEphemeral();
            if (double.IsNaN(value) && returnValueExpected)
                throw new ReturnException(functionToken.Position,
                    $"Function name \"{functionName}\" did not return a value.");
            return value;
        }

        void DefineFunction(RandomAccessIterator<SourceLine> lines)
        {
            if (Services.CurrentPass == 0)
            {
                if (_currentBlock != null)
                    throw new ExpressionException(1, "Function block cannot be inside another block.");

                var line = lines.Current;
                if (line.Label == null)
                    throw new ExpressionException(1, "Function name not specified");
                var functionName = line.Label.Name;
                if (_functionDefs.ContainsKey(functionName))
                    throw new ExpressionException(line.Label.Position, $"Function name \"{functionName}\" was previous declared.");
                if (!Services.SymbolManager.SymbolIsValid(functionName))
                    throw new ExpressionException(line.Label.Position, $"Invalid function name \"{functionName}\".");
                _functionDefs.Add(functionName, new Function(line.Operands, lines, Services, Services.Options.CaseSensitive));
            }
            else
            {
                new FunctionBlock(Services, 1).SeekBlockEnd(lines);
            }
        }

        public bool EvaluatesFunction(Token function) => _functionDefs.ContainsKey(function.Name);

        public double EvaluateFunction(RandomAccessIterator<Token> tokens) => CallFunction(tokens, true);

        public void InvokeFunction(RandomAccessIterator<Token> tokens) => CallFunction(tokens, false);

        public bool IsFunctionName(StringView symbol) => _functionDefs.ContainsKey(symbol);

        #endregion
    }
}