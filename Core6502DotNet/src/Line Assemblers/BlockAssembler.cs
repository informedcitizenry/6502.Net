//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
    public sealed class BlockAssemblerException : Exception
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
    public sealed class ReturnException : ExpressionException
    {
        /// <summary>
        /// Creates the new instance of the exception.
        /// </summary>
        /// <param name="position">The token that caused the exception.</param>
        /// <param name="message">The exception message.</param>
        public ReturnException(int position, string message)
            : base(position, message) { }

        /// <summary>
        /// Creates the new instance of the exception.
        /// </summary>
        /// <param name="token">The token that caused the exception.</param>
        /// <param name="message">The exception message.</param>
        public ReturnException(Token token, string message)
            : base(token, message) { }
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
        readonly Dictionary<StringView, Function> _functionDefs;
        readonly HashSet<int> _scannedBlockIndices;
        readonly Dictionary<string, int> _gotoLabels;
        BlockProcessorBase _currentBlock;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a block assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public BlockAssembler(AssemblyServices services)
            : this(services, null, null)
        {

        }

        /// <summary>
        /// Constructs a new instance of a block assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="multiLineAssembler">The <see cref="MultiLineAssembler"/> context to which this
        /// <param name="blockAssembler">The <see cref="BlockAssembler"/> from which this block assembler is being called.</param>
        /// block assembler is attached.</param>
        public BlockAssembler(AssemblyServices services, MultiLineAssembler multiLineAssembler, BlockAssembler blockAssembler)
            : base(services)
        {
            MultiLineAssembler = multiLineAssembler;
            _blocks = new Stack<BlockProcessorBase>();
            _functionDefs = new Dictionary<StringView, Function>(services.StringViewComparer);
            _gotoLabels = new Dictionary<string, int>(services.StringComparer);
            _scannedBlockIndices = blockAssembler?._scannedBlockIndices ?? new HashSet<int>();
            _currentBlock = null;

            _openClosures = new Dictionary<StringView, StringView>(services.StringViewComparer)
            {
                { ".block",     ".endblock"     },
                { ".do",        ".whiletrue"    },
                { ".enum",      ".endenum"      },
                { ".for",       ".next"         },
                { ".foreach",   ".next"         }, 
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

            Reserved.DefineType("Functional",
                ".function", ".endfunction");

            Reserved.DefineType("NonOpens",
                ".break", ".case", ".continue", ".default", ".endblock", ".endenum", ".endif",
                ".endfunction", ".endpage", ".endnamespace", ".endrepeat", ".endswitch", ".return",
                ".endwhile", ".whiletrue", ".else", ".elseif", ".elseifdef", ".elseifdef", ".elseifndef", ".next");

            Reserved.DefineType("BreakContinue", ".break", ".continue");

            Reserved.DefineType("Goto", ".goto", ".label");

            ExcludedInstructionsForLabelDefines.Add(".function");
            ExcludedInstructionsForLabelDefines.Add(".block");
            ExcludedInstructionsForLabelDefines.Add(".label");

            Services.Evaluator.AddFunctionEvaluator(this);

            Services.IsReserved.Add(s => _functionDefs.ContainsKey(s));
        }

        #endregion

        #region Methods

        BlockProcessorBase GetProcessor(SourceLine line, int iterationIndex)
        {
            var name = line.Instruction.Name;
            var type = Services.Options.CaseSensitive ? name.ToString() : name.ToLower();
            return type switch
            {
                ".block"                        => new ScopeBlock(Services, iterationIndex),
                ".do"                           => new DoWhileBlock(Services, iterationIndex),
                ".enum"                         => new EnumBlock(Services, iterationIndex),
                ".for"                          => new ForNextBlock(Services, iterationIndex),
                ".foreach"                      => new ForEachBlock(Services, iterationIndex),
                ".if" or ".ifdef" or ".ifndef"  => new ConditionalBlock(Services, iterationIndex),
                ".namespace"                    => new NamespaceBlock(Services, iterationIndex),
                ".page"                         => new PageBlock(Services, iterationIndex),
                ".repeat"                       => new RepeatBlock(Services, iterationIndex),
                ".switch"                       => new SwitchBlock(Services, iterationIndex),
                ".while"                        => new WhileBlock(Services, iterationIndex),
                _                               => null,
            };
        }

        void ScanBlock(RandomAccessIterator<SourceLine> lines)
        {
            var ix = lines.Index;
            var line = lines.Current;
            var closures = new Stack<Token>();
            closures.Push(line.Instruction);
            while ((line = lines.GetNext()) != null && closures.Count > 0)
            {
                if (line.Instruction != null)
                {
                    var instructionName = line.Instruction.Name;
                    if (instructionName.Equals(_openClosures[closures.Peek().Name], Services.StringViewComparer))
                    {
                        closures.Pop();
                    }
                    else if (_openClosures.ContainsKey(instructionName))
                    {
                        closures.Push(line.Instruction);
                    }
                    else if (instructionName.Equals(".return", Services.StringViewComparer) || Reserved.IsOneOf("BreakContinue", instructionName))
                    {
                        if (instructionName.Equals(".return", Services.StringViewComparer) && MultiLineAssembler == null)
                            throw new ReturnException(line.Instruction,
                                        "The \".return\" directive is not valid in this context.");
                        else if ((instructionName.Equals(".break", Services.StringViewComparer) &&
                            !_blocks.Any(b => b.AllowBreak)) ||
                            (instructionName.Equals(".continue", Services.StringViewComparer) &&
                            !_blocks.Any(b => b.AllowContinue)))
                            throw new SyntaxException(line.Instruction,
                                $"Invalid use of \"{instructionName}\" directive.");
                    }
                }
            }
            if (closures.Count > 0)
                throw new SyntaxException(closures.Peek(),
                    $"Missing directive \"{_openClosures[closures.Peek().Name]}\" for directive \"{closures.Peek().Name}\".");
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
                _blocks.Push(block);
                if (_scannedBlockIndices.Add(lines.Index))
                    ScanBlock(lines);
                _currentBlock = block;
            }
            if (line.Instruction.Name.Equals(".block", Services.StringComparison) && line.Label != null)
            {
                DefineLabel(line.Label, LogicalPCOnAssemble, false);
            }
            else if (line.Instruction.Name.Equals(".return", Services.StringViewComparer))
            {
                if (line.Operands.Count > 0)
                {
                    var it = line.Operands.GetIterator();
                    try
                    {
                        var result = Services.Evaluator.Evaluate(it);
                        if (it.Current != null)
                            throw new SyntaxException(it.Current, "Unexpected expression.");
                        MultiLineAssembler.ReturnValue = result;
                    }
                    catch (IllegalQuantityException ex)
                    {
                        MultiLineAssembler.ReturnValue = ex.Quantity;
                    }
                }
                else
                {
                    MultiLineAssembler.ReturnValue = double.NaN;
                }
                MultiLineAssembler.Returning = true;
                return string.Empty;
            }
            else
            {
                var isBreakCont = Reserved.IsOneOf("BreakContinue", line.Instruction.Name);
                if (_currentBlock == null || (!isBreakCont && !_currentBlock.IsReserved(line.Instruction.Name)))
                    throw new SyntaxException(line.Instruction.Position,
                        $"\"{line.Instruction.Name}\" directive must come inside a block.");

                if (isBreakCont)
                {
                    if (line.Operands.Count > 0)
                        throw new SyntaxException(line.Operands[0], "Unexpected expression.");
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
                        {
                            var err = isBreak ? "break" : "continue";
                            throw new SyntaxException(line.Instruction,
                            $"No enclosing loop out of which to {err}.");
                        }
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
            if (line.Instruction.Name.Equals(".label", Services.StringViewComparer))
            {
                if (line.Label == null)
                    throw new SyntaxException(line.Instruction, "Missing label for \".label\" directive.");
                if (line.Operands.Count > 0)
                    throw new SyntaxException(line.Operands[0], "Unexpected expression.");
                var scopedName = Services.SymbolManager.GetScopedName(line.Label.Name);
                if (_gotoLabels.TryGetValue(scopedName, out var labelIndex))
                {
                    if (lines.Index != labelIndex)
                        throw new SyntaxException(line.Label, "Goto label previously defined.");
                }
                else
                {
                    Services.SymbolManager.DefineVoidSymbol(line.Label.Name);
                    _gotoLabels[scopedName] = lines.Index;
                }
                return string.Empty;
            }
            if (line.Operands.Count == 0)
                throw new SyntaxException(line.Instruction.Position,
                    "Destination not specified for \".goto\" directive.");

            var gotoExp = line.Operands[0].Name;
            if ((!char.IsLetter(gotoExp[0]) && gotoExp[0] != '_') || line.Operands.Count > 1)
                return Services.Log.LogEntry<string>(line.Operands[0],
                    "\".goto\" destination must be a label.", false, true);
            if (line.Label != null && gotoExp.Equals(line.Label.Name, Services.StringViewComparer))
                return Services.Log.LogEntry<string>(line.Instruction,
                    "Destination cannot be the same line as \".goto\" directive.");
            var scopedLabel = Services.SymbolManager.GetFullyQualifiedName(gotoExp);
            if (_gotoLabels.TryGetValue(scopedLabel, out var index))
            {
                if (index < lines.Index)
                    lines.Rewind(index - 1);
                else
                    lines.FastForward(index);
            }
            else
            {
                var iterCopy = new RandomAccessIterator<SourceLine>(lines, true);
                SourceLine currLine;
                if ((currLine = iterCopy.FirstOrDefault(l =>
                {
                    if (l.Instruction != null && _openClosures.ContainsKey(l.Instruction.Name))
                    {
                        if (l.Instruction.Name.Equals(".function", Services.StringComparison))
                            throw new SyntaxException(l.Instruction, "Function block cannot be inside another block.");
                        // leap over any blocks we find along the way we are not currently in.
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
                        return Services.Log.LogEntry<string>(line.Instruction,
                            $"\"{gotoExp}\" is not a valid destination.");
                    }
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
                    Services.Log.LogEntry(line.Operands[0], "Consider using the \".label\" directive for \".goto\" references.", false, false);
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
                    tokens.MoveNext();
                if (StringHelper.ExpressionIsAString(tokens, Services))
                    evalParms.Add(StringHelper.GetString(tokens, Services));
                else
                    evalParms.Add(Services.Evaluator.Evaluate(tokens, false));
                token = tokens.Current;
            }
            Services.SymbolManager.PushScopeEphemeral();
            var value = _functionDefs[functionName].Invoke(evalParms, this);
            Services.SymbolManager.PopScopeEphemeral();
            if (double.IsNaN(value) && returnValueExpected)
                throw new ReturnException(functionToken,
                    $"Function name \"{functionName}\" did not return a value.");
            return value;
        }

        void DefineFunction(RandomAccessIterator<SourceLine> lines)
        {
            if (Services.CurrentPass == 0)
            {
                var line = lines.Current;
                if (_currentBlock != null)
                    throw new SyntaxException(line.Instruction, "Function definition block cannot be inside another block.");
                if (line.Label == null)
                    throw new SyntaxException(line.Instruction, "Function name not specified");
                var functionName = line.Label.Name;
                if (_functionDefs.ContainsKey(functionName))
                    throw new SyntaxException(line.Label, $"Function name \"{functionName}\" was previous declared.");
                if (!Services.SymbolManager.SymbolIsValid(functionName))
                    throw new SyntaxException(line.Label, $"Invalid function name \"{functionName}\".");
                _functionDefs.Add(functionName, new Function(line.Label.Name, line.Operands, lines, Services, Services.Options.CaseSensitive));
            }
            else
            {
                new FunctionBlock(Services, lines.Index, false).SeekBlockEnd(lines);
            }
        }

        public override bool Assembles(StringView s) =>
            _openClosures.ContainsKey(s) || Reserved.IsReserved(s);

        public override bool IsReserved(StringView symbol) 
            => base.IsReserved(symbol) || _openClosures.ContainsKey(symbol) || _functionDefs.ContainsKey(symbol);

        public bool EvaluatesFunction(Token function) => _functionDefs.ContainsKey(function.Name);

        public double EvaluateFunction(RandomAccessIterator<Token> tokens) => CallFunction(tokens, true);

        public void InvokeFunction(RandomAccessIterator<Token> tokens) => CallFunction(tokens, false);

        public bool IsFunctionName(StringView symbol) => _functionDefs.ContainsKey(symbol);

        /// <summary>
        /// Gets whether the context of the block assembler is in a function block.
        /// </summary>
        public MultiLineAssembler MultiLineAssembler { get; }

        #endregion
    }
}