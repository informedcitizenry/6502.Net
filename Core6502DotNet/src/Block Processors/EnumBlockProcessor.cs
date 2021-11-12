//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing enumerations defined in .enum/.endenum blocks.
    /// </summary>
    public sealed class EnumBlock : BlockProcessorBase
    {
        #region Constructors

        /// <summary>
        /// Create a new instance of the enum block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public EnumBlock(AssemblyServices services, int index)
            : base(services, index, false)
        {
            Reserved.DefineType("Directives", ".enum", "=");
        }
        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            SourceLine line = lines.Current;
            if (line.Operands.Count != 1)
            {
                if (line.Operands.Count == 0)
                    Services.Log.LogEntry(line.Instruction, "Missing \".enum\" identifier.");
                else
                    Services.Log.LogEntry(line.Operands[1], "Unexpected expression.", false, true);
            }
            else if (!Services.SymbolManager.SymbolIsValid(line.Operands[0].Name))
            {
                Services.Log.LogEntry(line.Operands[0], "\".enum\" must be a valid identifier.");
            }
            else if (Services.SymbolManager.SymbolExists(line.Operands[0].Name))
            {
                Services.Log.LogEntry(line.Operands[0], "\".enum\" identifier clashes with existing symbol.");
            }
            else
            {
                Services.SymbolManager.PushScope(line.Operands[0].Name);
                double defaultEnumValue = 0;
                var enumValues = new Dictionary<double, StringView>();
                while ((line = lines.GetNext()).Instruction == null ||
                    !line.Instruction.Name.Equals(BlockClosure, Services.StringComparison))
                {
                    if (line.Instruction != null)
                    {
                        if (line.Label == null)
                        {
                            Services.Log.LogEntry(line, 1, "Definition name was not provided.");
                        }
                        else if (!Reserved.IsReserved(line.Instruction.Name))
                        {
                            Services.Log.LogEntry(line, line.Instruction.Position, "Only definitions are allowed in an \".enum\" block.");
                        }
                        else if (line.Operands.Count == 0)
                        {
                            Services.Log.LogEntry(line.Instruction, "Expected expression.");
                        }
                        else
                        {
                            var it = line.Operands.GetIterator();
                            defaultEnumValue = Services.Evaluator.Evaluate(it);
                            if (it.Current != null)
                                Services.Log.LogEntry(it.Current, "Unexpected expression.");
                            if (!defaultEnumValue.IsInteger())
                            {
                                Services.Log.LogEntry(line.Operands[2], "Definition must be an integer value.");
                                continue;
                            }
                        }
                    }
                    if (line.Label != null)
                    {
                        if (!Services.SymbolManager.SymbolIsValid(line.Label.Name))
                        {
                            Services.Log.LogEntry(line.Label, "Illegal definition name.");
                        }
                        else if (enumValues.TryGetValue(defaultEnumValue, out var definition))
                        {
                            Services.Log.LogEntry(line, line.Label.Position, $"Definition has same enumeration value as \"{definition}\" ({defaultEnumValue}).");
                        }
                        else
                        {
                            Services.SymbolManager.DefineSymbol(line.Label.Name, defaultEnumValue);
                            enumValues[defaultEnumValue] = line.Label.Name;
                        }
                        defaultEnumValue++;
                    }
                }
                Services.SymbolManager.PopScope();
            }
        }
        #endregion

        #region Properties
        public override IEnumerable<string> BlockOpens => new string[] { ".enum" };
        
        public override string BlockClosure => ".endenum";
        
        public override bool AllowBreak => false;
        
        public override bool AllowContinue => false;

        #endregion
    }
}
