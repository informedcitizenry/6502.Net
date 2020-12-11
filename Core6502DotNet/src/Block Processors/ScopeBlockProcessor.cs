//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

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
        /// <param name="index">The index at which the block is defined.</param>
        public ScopeBlock(AssemblyServices services, int index)
            : base(services, index, false) => Reserved.DefineType("Directives", ".block", ".endblock");
        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (line.Operands.Count > 0)
                throw new SyntaxException(line.Operands[0], "Unexpected expression.");
            if (line.Instruction.Name.Equals(".block", Services.StringComparison))
            {
                StringView scopeName;
                if (line.Label != null && !line.Label.IsSpecialOperator())
                {
                    if (!char.IsLetter(line.Label.Name[0]) || (!char.IsLetterOrDigit(line.Label.Name[^1]) && line.Label.Name[^1] != '_'))
                        throw new SyntaxException(line.Label, $"Invalid name \"{line.Label.Name}\" for block.");
                    scopeName = line.Label.Name;
                }
                else
                {
                    scopeName = lines.Index.ToString();
                }
                Services.SymbolManager.PushScope(scopeName);
            }
            else
            {
                Services.SymbolManager.PopScope();
            }
        }

        public override void PopScope(RandomAccessIterator<SourceLine> unused) { }

        #endregion

        #region Properties

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override IEnumerable<string> BlockOpens => new string[] { ".block" };

        public override string BlockClosure => ".endblock";

        #endregion
    }

    public sealed class NamespaceBlock : BlockProcessorBase
    {
        public NamespaceBlock(AssemblyServices services, int index)
            : base(services, index, false) => Reserved.DefineType("Directives", ".namespace", ".endnamespace");

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            StringView scopeName = null;
            if (line.Instruction.Name.Equals(".namespace", Services.StringComparison))
            {
                // to avoid symbol clashes with later ".block" directives of the same name.
                Services.PassNeeded = Services.CurrentPass == 0; 

                if (line.Operands.Count > 0)
                {
                    if (line.Operands.Count > 1 ||
                        !char.IsLetter(line.Operands[0].Name[0]) || (!char.IsLetterOrDigit(line.Operands[0].Name[^1]) && line.Operands[0].Name[^1] != '_'))
                        throw new SyntaxException(line.Operands[1], "Invalid namespace name.");
                    scopeName = line.Operands[0].Name;
                }
                if (scopeName == null)
                {
                    scopeName = lines.Index.ToString();
                    Services.SymbolManager.PushScope(scopeName);
                }
                else if (Services.SymbolManager.SymbolExists(scopeName))
                {
                    Services.Log.LogEntry(line.Operands[0], $"Namespace name \"{scopeName}\" clashes with existing symbol name.");
                }
            }
            else
            {
                Services.SymbolManager.PopScope();
            }
        }

        public override IEnumerable<string> BlockOpens => new string[] { ".namespace" };

        public override string BlockClosure => ".endnamespace";

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

    }
}