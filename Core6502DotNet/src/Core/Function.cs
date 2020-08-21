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
    /// A class encapsulating a function definition.
    /// </summary>
    public sealed class Function : ParameterizedSourceBlock
    {
        #region Members

        readonly int _startIndex;
        readonly int _endIndex;
        readonly RandomAccessIterator<SourceLine> _lineIterator;

        #endregion

        #region Constructors


        /// <summary>
        /// Creates a new instance of the Function class.
        /// </summary>
        /// <param name="parameterList">The list of parameters for the function.</param>
        public Function(Token parameterList)
            : this(parameterList, Assembler.LineIterator) { }

        /// <summary>
        /// Creates a new instance of the Function class.
        /// </summary>
        /// <param name="parameterList">The list of parameters for the function.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator.</param>
        public Function(Token parameterList,
                        RandomAccessIterator<SourceLine> iterator)
            : base(parameterList, 
                  iterator.Current.ParsedSource, 
                  Assembler.StringComparison)
        {
            _endIndex = -1;
            _startIndex = iterator.Index;
            SourceLine line;
            while ((line = iterator.GetNext()) != null)
            {
                if (line.LabelName.Equals("+"))
                {
                    Assembler.Log.LogEntry(line, line.Label, "Anonymous labels are not supported inside functions.", false);
                }
                if (line.InstructionName.Equals(".global"))
                    throw new SyntaxException(line.Instruction.Position,
                        $"Directive \".global\" not allowed inside a function block.");
                if (line.InstructionName.Equals(".endfunction"))
                {
                    if (line.OperandHasToken)
                        throw new SyntaxException(line.Operand.Position, "Unexpected expression found after \".endfunction\" directive.");
                    break;
                }
            }
            if (line == null)
                throw new SyntaxException(_startIndex, "Function definition does not have a closing \".endfunction\" directive.");
            _endIndex = iterator.Index - 1;
            _lineIterator = iterator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invokes the function from its definition, starting assembly at the first line.
        /// </summary>
        /// <param name="parameterList">The parameters of the function call.</param>
        /// <returns>A <see cref="double"/> of the function return.</returns>
        public double Invoke(List<object> parameterList)
        {
            var invokeLine = _lineIterator.Current;
            if (parameterList.Count > Params.Count)
            {
                Assembler.Log.LogEntry(invokeLine, invokeLine.Instruction, "Unexpected argument passed to function.");
                return double.NaN;
            }
            for (var i = 0; i < Params.Count; i++)
            {
                if (i >= parameterList.Count || parameterList[i] == null)
                {
                    if (!string.IsNullOrEmpty(Params[i].DefaultValue))
                    {
                        if (Params[i].DefaultValue.EnclosedInDoubleQuotes())
                            Assembler.SymbolManager.Define(Params[i].Name, Params[i].DefaultValue.TrimOnce('"'));
                        else
                            Assembler.SymbolManager.Define(Params[i].Name, Evaluator.Evaluate(Params[i].DefaultValue), true);
                    }
                    else
                    {
                        Assembler.Log.LogEntry(invokeLine, invokeLine.Instruction,
                                                    $"Missing argument \"{Params[i].Name}\" for function.");
                        return double.NaN;
                    }
                }
                else
                {
                    var parm = parameterList[i];
                    if (parm.ToString().EnclosedInDoubleQuotes())
                        Assembler.SymbolManager.Define(Params[i].Name, parm.ToString().TrimOnce('"'), true);
                    else
                        Assembler.SymbolManager.Define(Params[i].Name, (double)parm, true);
                }
            }
            var assemblers = new List<AssemblerBase>
                {
                    new AssignmentAssembler(),
                    new EncodingAssembler(),
                    new MiscAssembler(),
                    new BlockAssembler()
                };
            var returnIx = _lineIterator.Index;
            if (returnIx < _startIndex)
                _lineIterator.FastForward(_startIndex);
            else
                _lineIterator.Rewind(_startIndex);
            return MultiLineAssembler.AssembleLines(_lineIterator,
                                                    assemblers,
                                                    _startIndex,
                                                    _endIndex,
                                                    true,
                                                    null,
                                                    false,
                                                    ErrorHandler);

        }

        bool ErrorHandler(SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            if (reason == AssemblyErrorReason.ExceptionRaised)
            {
                if (Assembler.CurrentPass > 0)
                {
                    Assembler.Log.LogEntry(line, line.Instruction,
                                            ex.Message);
                    return false;
                }
                return true;
            }
            if (reason == AssemblyErrorReason.OutsideBounds)
                Assembler.Log.LogEntry(line, line.Instruction,
                            $"\".goto\" destination \"{line.OperandExpression}\" is outside of function block.");
            
            if (reason == AssemblyErrorReason.NotFound)
                Assembler.Log.LogEntry(line, line.Instruction,
                        $"Directive \"{line.InstructionName}\" not allowed inside a function block.");
            return false;
        }
        #endregion
    }

    /// <summary>
    /// Represents a placeholder function block processor.
    /// </summary>
    public sealed class FunctionBlock : BlockProcessorBase
    {
        /// <summary>
        /// Creates a new instance of a function block processor placeholder.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public FunctionBlock(SourceLine line, BlockType type)
            : base(line, type, false) { }

        public override bool AllowBreak => false;
         
        public override bool AllowContinue => false;

        public override bool ExecuteDirective() => false;
    }
}
