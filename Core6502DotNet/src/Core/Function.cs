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

        readonly RandomAccessIterator<SourceLine> _lineIterator;
        readonly SourceLine _invokeLine;

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
            _invokeLine = iterator.Current;
            var startIndex = iterator.Index + 1;
            SourceLine line;
            while ((line = iterator.GetNext()) != null)
            {
                if (line.LabelName.Equals("+"))
                {
                    Assembler.Log.LogEntry(line, line.Label, 
                        "Anonymous labels are not supported inside functions.", false);
                }
                if (line.InstructionName.Equals(".global"))
                    throw new SyntaxException(line.Instruction.Position,
                        $"Directive \".global\" not allowed inside a function block.");
                if (line.InstructionName.Equals(".endfunction"))
                {
                    if (line.OperandHasToken)
                        throw new SyntaxException(line.Operand.Position, 
                            "Unexpected expression found after \".endfunction\" directive.");
                    break;
                }
            }
            if (line == null)
                throw new SyntaxException(iterator.Current.Instruction.Position, 
                                          "Function definition does not have a closing \".endfunction\" directive.");
            var count = iterator.Index - startIndex;
            _lineIterator = iterator.Skip(startIndex).Take(count).GetIterator();
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
            if (parameterList.Count > Params.Count)
            {
                Assembler.Log.LogEntry(_invokeLine, _invokeLine.Instruction, 
                    "Unexpected argument passed to function.");
                return double.NaN;
            }
            for (var i = 0; i < Params.Count; i++)
            {
                if (i >= parameterList.Count || parameterList[i] == null)
                {
                    if (!string.IsNullOrEmpty(Params[i].DefaultValue))
                    {
                        if (Params[i].DefaultValue.EnclosedInDoubleQuotes())
                            Assembler.SymbolManager.Define(Params[i].Name, 
                                Params[i].DefaultValue.TrimOnce('"'));
                        else
                            Assembler.SymbolManager.Define(Params[i].Name, 
                                Evaluator.Evaluate(Params[i].DefaultValue), true);
                    }
                    else
                    {
                        Assembler.Log.LogEntry(_invokeLine, _invokeLine.Instruction,
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
            var iteratorCopy = _lineIterator.GetIterator();
            var assemblers = new List<AssemblerBase>
                {
                    new AssignmentAssembler(),
                    new EncodingAssembler(),
                    new MiscAssembler(),
                    new BlockAssembler(iteratorCopy)
                };
            return MultiLineAssembler.AssembleLines(iteratorCopy,
                                                    assemblers,
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
