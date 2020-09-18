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

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the Function class.
        /// </summary>
        /// <param name="parameterList">The list of parameters for the function.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator.</param>
        /// <exception cref="SyntaxException"></exception>
        public Function(AssemblyServices services,
                        Token parameterList,
                        RandomAccessIterator<SourceLine> iterator)
            : base(services,
                  parameterList, 
                  iterator.Current.ParsedSource)
        {
            _definedLines = new List<SourceLine>();
            SourceLine line;
            while ((line = iterator.GetNext()) != null)
            {
                if (line.LabelName.Equals("+"))
                {
                    Services.Log.LogEntry(line, line.Label, 
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
                _definedLines.Add(line);
            }
            if (line == null)
                throw new SyntaxException(iterator.Current.Instruction.Position, 
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
                throw new Exception("Unexpected argument passed to function.");
            for (var i = 0; i < Params.Count; i++)
            {
                if (i >= parameterList.Count || parameterList[i] == null)
                {
                    if (string.IsNullOrEmpty(Params[i].DefaultValue))
                        throw new Exception($"Missing argument \"{Params[i].Name}\" for function.");
                    if (Params[i].DefaultValue.EnclosedInDoubleQuotes())
                        Services.SymbolManager.Define(Params[i].Name,
                            Params[i].DefaultValue.TrimOnce('"'));
                    else
                        Services.SymbolManager.Define(Params[i].Name,
                            Services.Evaluator.Evaluate(Params[i].DefaultValue), true);
                }
                else
                {
                    var parm = parameterList[i];
                    if (parm.ToString().EnclosedInDoubleQuotes())
                        Services.SymbolManager.Define(Params[i].Name, parm.ToString().TrimOnce('"'), true);
                    else
                        Services.SymbolManager.Define(Params[i].Name, (double)parm, true);
                }
            }
            var iterator = _definedLines.GetIterator();
            var assemblers = new List<AssemblerBase>
                {
                    new AssignmentAssembler(Services),
                    new EncodingAssembler(Services),
                    new MiscAssembler(Services),
                    new BlockAssembler(Services, iterator)
                };
            return MultiLineAssembler.AssembleLines(iterator,
                                                    assemblers,
                                                    true,
                                                    null,
                                                    false,
                                                    ErrorHandler,
                                                    Services);
        }

        bool ErrorHandler(SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            if (reason == AssemblyErrorReason.ExceptionRaised)
            {
                if (Services.CurrentPass > 0)
                {
                    Services.Log.LogEntry(line, line.Instruction,
                                            ex.Message);
                    return false;
                }
                return true;
            }
            if (reason == AssemblyErrorReason.OutsideBounds)
                Services.Log.LogEntry(line, line.Instruction,
                            $"\".goto\" destination \"{line.OperandExpression}\" is outside of function block.");
            
            if (reason == AssemblyErrorReason.NotFound)
                Services.Log.LogEntry(line, line.Instruction,
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
        /// <param name="line">The <see cref="SourceLine"/> iterator to traverse when
        /// processing the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public FunctionBlock(AssemblyServices services,
                             RandomAccessIterator<SourceLine> iterator, 
                             BlockType type)
            : base(services, iterator, type, false) { }

        public override bool AllowBreak => false;
         
        public override bool AllowContinue => false;

        public override bool ExecuteDirective() => false;
    }
}
