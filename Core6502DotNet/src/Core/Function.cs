//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// A class encapsulating a function definition.
    /// </summary>
    public sealed class Function : ParameterizedSourceBlock
    {
        #region Members

        readonly IList<SourceLine> _definedLines;
        AssemblyServices _services;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the Function class.
        /// </summary>
        /// <param name="name">The function's name.</param>
        /// <param name="parameterList">The list of parameters for the function.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator to traverse to define the function block.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="caseSensitive">Determines whether to compare the passed parameters
        /// to the source block's own defined parameters should be case-sensitive.</param>
        /// <exception cref="SyntaxException"></exception>
        public Function(StringView name,
                        List<Token> parameterList,
                        RandomAccessIterator<SourceLine> iterator,
                        AssemblyServices services,
                        bool caseSensitive)
            : base(parameterList,
                  caseSensitive)
        {
            Name = name;
            _services = services;
            _definedLines = new List<SourceLine>();
            SourceLine line;
            while ((line = iterator.GetNext()) != null)
            {
                if (line.Label != null && line.Label.Name.Equals("+"))
                {
                    _services.Log.LogEntry(line.Label,
                        "Anonymous labels are not supported inside functions.", false);
                }
                if (line.Instruction != null)
                {
                    if (line.Instruction.Name.Equals(".global", _services.StringViewComparer))
                        throw new SyntaxException(line.Instruction,
                            $"Directive \".global\" not allowed inside a function block.");
                    if (line.Instruction.Name.Equals(".endfunction", _services.StringViewComparer))
                    {
                        if (line.Operands.Count > 0)
                            throw new SyntaxException(line.Operands[0],
                                "Unexpected expression found after \".endfunction\" directive.");
                        break;
                    }
                }
                _definedLines.Add(line);
            }
            if (line == null)
                throw new SyntaxException(iterator.Current.Instruction,
                                          "Function definition does not have a closing \".endfunction\" directive.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invokes the function from its definition, starting assembly at the first line.
        /// </summary>
        /// <param name="parameterList">The parameters of the function call.</param>
        /// <returns>A <see cref="double"/> of the function return.</returns>
        /// <exception cref="Exception"></exception>
        public double Invoke(List<object> parameterList)
        {
            if (parameterList.Count > Params.Count)
                throw new SyntaxException(parameterList.Count, $"Unexpected argument passed to function \"{Name}\".");
            for (var i = 0; i < Params.Count; i++)
            {
                if (i >= parameterList.Count || parameterList[i] == null)
                {
                    if (Params[i].DefaultValue.Count == 0)
                        throw new SyntaxException(i + 1, $"Missing argument \"{Params[i].Name}\" for function \"{Name}\".");
                    var it = Params[i].DefaultValue.GetIterator();
                    if (StringHelper.ExpressionIsAString(it, _services))
                        _services.SymbolManager.DefineSymbol(Params[i].Name, StringHelper.GetString(it, _services));
                    else
                        _services.SymbolManager.DefineSymbol(Params[i].Name,
                            _services.Evaluator.Evaluate(it));
                }
                else
                {
                    var parm = parameterList[i];
                    if (parm is string)
                        _services.SymbolManager.DefineSymbol(Params[i].Name, parm.ToString().TrimOnce('"'));
                    else
                        _services.SymbolManager.DefineSymbol(Params[i].Name, (double)parm);
                }
            }
            var iterator = _definedLines.GetIterator();
            var assemblers = new List<AssemblerBase>
                {
                    new AssignmentAssembler(_services),
                    new BlockAssembler(_services),
                    new EncodingAssembler(_services),
                    new MiscAssembler(_services)
                };
            return new MultiLineAssembler()
                .WithAssemblers(assemblers)
                .WithOptions(new MultiLineAssembler.Options()
                {
                    AllowReturn = true,
                    DisassembleAll = false,
                    ErrorHandler = ErrorHandler,
                    Evaluator = _services.Evaluator,
                    StopDisassembly = () => _services.PrintOff
                }).AssembleLines(_definedLines.GetIterator(), out string disassembly);
        }

        bool ErrorHandler(AssemblerBase assembler, SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            if (reason == AssemblyErrorReason.ExceptionRaised)
            {
                if (_services.CurrentPass > 0)
                {
                    _services.Log.LogEntry(line.Instruction, ex.Message);
                    return false;
                }
                return true;
            }
            if (reason == AssemblyErrorReason.NotFound)
                _services.Log.LogEntry(line.Instruction,
                        $"Directive \"{line.Instruction}\" not allowed inside a function block.");
            return false;
        }
        #endregion

        #region

        public StringView Name { get; }

        #endregion
    }

    /// <summary>
    /// Represents a placeholder function block processor.
    /// </summary>
    public sealed class FunctionBlock : BlockProcessorBase
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a function block processor placeholder.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public FunctionBlock(AssemblyServices services, int index)
            : base(services, index) { }

        #endregion

        #region Properties

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override IEnumerable<string> BlockOpens => new string[] { ".function" };

        public override string BlockClosure => ".endfunction";

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> iterator) { }

        #endregion
    }
}
