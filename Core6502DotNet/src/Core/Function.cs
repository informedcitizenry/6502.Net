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
        #region Constructors
        /// <summary>
        /// Creates a new instance of the Function class.
        /// </summary>
        /// definition and invocations.</param>
        /// <param name="parameterList">The list of parameters for the function.</param>
        public Function(Token parameterList)
            : base(parameterList, 
                   Assembler.LineIterator.Current.ParsedSource, 
                   Assembler.Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
        {
            StartIndex = EndIndex = -1;
            Define();
        }

        #endregion

        #region Methods

        void Define()
        {
            StartIndex = Assembler.LineIterator.Index;
            SourceLine line;
            Assembler.LineIterator.Current.Label = null;
            while ((line = Assembler.LineIterator.GetNext()) != null)
            {
                if (line.LabelName.Equals("+"))
                {
                    Assembler.Log.LogEntry(line, line.LabelName, "Anonymous labels are not supported inside functions.", false);
                }
                else if (line.InstructionName.Equals(".global"))
                {
                    Assembler.Log.LogEntry(line, line.Instruction, $"Directive \".global\" not allowed inside a function block.");
                    break;
                }
                else if (line.InstructionName.Equals(".function"))
                {
                    Assembler.Log.LogEntry(line, line.Instruction, "Function definition not allowed inside a function block.");
                    break;
                }
                else if (line.InstructionName.Equals(".endfunction"))
                {
                    if (line.OperandHasToken)
                        Assembler.Log.LogEntry(line, line.Operand, "Unexpected expression found after \".endfunction\" directive.");
                    break;
                }
            }
            if (line == null)
                throw new ExpressionException(StartIndex, "Function definition does not have a closing \".endfunction\" directive.");
            EndIndex = Assembler.LineIterator.Index;
        }

        /// <summary>
        /// Invokes the function from its definition, starting assembly at the first line.
        /// </summary>
        /// <param name="mla">The <see cref="MultiLineAssembler"/> reponsible for making
        /// the function call.</param>
        /// <param name="parameterList">The parameters of the function call.</param>
        /// <returns>A <see cref="double"/> of the function return.</returns>
        public double Invoke(MultiLineAssembler mla, List<object> parameterList)
        {
            var invokeLine = Assembler.LineIterator.Current;
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
                    mla
                };
            var returnIx = Assembler.LineIterator.Index;
            if (returnIx < StartIndex)
                Assembler.LineIterator.FastForward(StartIndex);
            else
                Assembler.LineIterator.Rewind(StartIndex);

            foreach (SourceLine line in Assembler.LineIterator)
            {
                if (Assembler.LineIterator.Index == EndIndex)
                    break;
                if (line.InstructionName.Equals(".return"))
                {
                    if (line.OperandHasToken)
                        return Evaluator.Evaluate(line.Operand);
                    return double.NaN;
                }
                var asm = assemblers.FirstOrDefault(asm => asm.AssemblesLine(line));
                if (asm == null && line.Instruction != null)
                {
                    Assembler.Log.LogEntry(line, line.Instruction,
                        $"Directive \"{line.InstructionName}\" not allowed inside a function block.");
                    break;
                }
                else if (asm != null)
                {
                    asm.AssembleLine(line);
                    if (line.InstructionName.Equals(".goto") &&
                        (Assembler.LineIterator.Index < StartIndex || Assembler.LineIterator.Index > EndIndex))
                    {
                        Assembler.Log.LogEntry(line, line.Instruction,
                            $"\".goto\" destination \"{line.OperandExpression}\" is outside of function block.");
                        break;
                    }
                }
            }
            return double.NaN;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the start index in the <see cref="SourceLine"/> iterator in which the function is defined.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Gets the end index in the <see cref="SourceLine"/> iterator in which the function is defined.
        /// </summary>
        public int EndIndex { get; set; }

        #endregion
    }


    public class FunctionBlock : BlockProcessorBase
    {
        public FunctionBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
        }

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override bool ExecuteDirective() => throw new NotImplementedException();
    }
}
