//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .block/.endblock blocks.
    /// </summary>
    public sealed class ScopeBlock : BlockProcessorBase
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a scoped block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public ScopeBlock(AssemblyServices services,
                          RandomAccessIterator<SourceLine> iterator,
                          BlockType type)
            : base(services, iterator, type, false)
        {
        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            var line = LineIterator.Current;
            string scopeName;
            if (line.InstructionName.Equals(".block") || 
                line.InstructionName.Equals(".namespace"))
            {
                var isBlock = line.InstructionName[1] == 'b';
                if (isBlock)
                    scopeName = line.LabelName;
                else
                    scopeName = line.OperandExpression.Trim();
                    
                if (string.IsNullOrEmpty(scopeName))
                    scopeName = LineIterator.Index.ToString();
                else if (!char.IsLetter(scopeName[0]) || (!char.IsLetterOrDigit(scopeName[^1]) && scopeName[^1] != '_'))
                {
                    var type = isBlock ? "scope block" : "namespace";
                    throw new SyntaxException(line.Label.Position,
                        $"Invalid name \"{scopeName}\" for {type}.");
                }
                else if (!isBlock && Services.SymbolManager.SymbolExists(scopeName) && Services.CurrentPass == 0)
                {
                    Services.Log.LogEntry(line, line.Operand, 
                        $"Namespace name \"{scopeName}\" clashes with existing symbol name.");
                    return true;
                }
                Services.SymbolManager.PushScope(scopeName);
                return true;
            }
            else if (line.InstructionName.Equals(".endblock") || 
                     line.InstructionName.Equals(".endnamespace"))
            {
                Services.SymbolManager.PopScope();
                return true;
            }
            return false;
        }

        public override void PopScope() { }

        #endregion

        #region Properties

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        #endregion
    }
}