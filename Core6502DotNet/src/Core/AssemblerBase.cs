//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Core6502DotNet
{
    /// <summary>
    ///The base line assembler class. This class must be inherited.
    /// </summary>
    public abstract class AssemblerBase
    {

        #region Constructors

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        protected AssemblerBase(AssemblyServices services)
        {
            Services = services;
            ExcludedInstructionsForLabelDefines = new HashSet<StringView>(services.StringViewComparer);
            ExcludeReservedTypesFromLabelDefines = new HashSet<string>(services.StringComparer);
            Reserved = new ReservedWords(services.StringViewComparer);
            services.IsReserved.Add(Reserved.IsReserved);
            services.SymbolManager.AddValidSymbolNameCriterion(s => !Reserved.IsReserved(s));
            services.InstructionLookupRules.Add(s => Assembles(s));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the token is a reserved word to the assembler object.
        /// </summary>
        /// <param name="symbol">The symbol name to check if reserved</param>
        /// <returns><c>true</c> if reserved, otherwise <c>false</c>.</returns>
        public virtual bool IsReserved(StringView symbol) => Reserved.IsReserved(symbol);

        /// <summary>
        /// Determines whether the assembler assembles the keyword.
        /// </summary>
        /// <param name="s">The symbol/keyword name.</param>
        /// <returns><c>true</c> if the assembler assembles the keyword, 
        /// otherwise <c>false</c>.</returns>
        public virtual bool Assembles(StringView s) => IsReserved(s.ToString());

        /// <summary>
        /// Determines whether the assembler assembles the given source line.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <returns><c>true</c> if the assembler assembles the line, 
        /// otherwise <c>false</c>.</returns>
        public virtual bool AssemblesLine(SourceLine line)
            => (line.Label != null && line.Instruction == null)
            || (line.Instruction != null && IsReserved(line.Instruction.Name));

        /// <summary>
        /// Assemble the parsed <see cref="SourceLine"/>.
        /// </summary>
        /// <param name="line">The line to assembly.</param>
        /// <returns>The disassembly output from the assembly operation.</returns>
        /// <exception cref="BlockClosureException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="DivideByZeroException"/>
        /// <exception cref="ExpressionException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="InvalidPCAssignmentException"/>
        /// <exception cref="BlockAssemblerException"/>
        /// <exception cref="OverflowException"/>
        /// <exception cref="ProgramOverflowException"/>
        /// <exception cref="ReturnException"/>
        /// <exception cref="SectionException"/>
        /// <exception cref="SymbolException"/>
        /// <exception cref="SyntaxException"/>
        public string Assemble(RandomAccessIterator<SourceLine> lines)
        {
            var first = lines.Current;
            bool isSpecial = first.Label != null && first.Label.IsSpecialOperator();
            LogicalPCOnAssemble = Services.Output.LogicalPC;
            LongLogicalPCOnAssemble = Services.Output.LongLogicalPC;
            PCOnAssemble = Services.Output.ProgramCounter;
            LongPCOnAssemble = Services.Output.LongProgramCounter;
            if (first.Label != null && !first.Label.Name.Equals("*"))
            {
                if (isSpecial)
                    Services.SymbolManager.DefineLineReference(first.Label, LogicalPCOnAssemble);
                else if (first.Instruction == null || 
                    !ExcludedInstructionsForLabelDefines.Contains(first.Instruction.Name))
                    DefineLabel(first.Label, LogicalPCOnAssemble, true);
            }
            if (first.Instruction != null)
                return OnAssemble(lines);
            if (first.Label != null && !isSpecial)
            {
                var symbol = Services.SymbolManager.GetSymbol(first.Label, false);
                if (symbol != null && symbol.IsNumeric && symbol.StorageType == StorageType.Scalar && !double.IsNaN(symbol.NumericValue))
                    return $".{(int)symbol.NumericValue,-42:x4}{(Services.Options.NoSource ? string.Empty : first.Source)}";
            }
            return string.Empty;
        }

        /// <summary>
        /// Define the label in the symbol table.
        /// </summary>
        /// <param name="label">The label <see cref="Token"/>.</param>
        /// <param name="address">The address (value) of the label.</param>
        /// <param name="setLocalScope">Set the label as local scope.</param>
        /// <exception cref="SymbolException"></exception>
        protected void DefineLabel(Token label, double address, bool setLocalScope)
        {
            if (Services.SymbolManager.SymbolExists(label.Name, false))
            {
                if (Services.CurrentPass == 0)
                    throw new SymbolException(label, SymbolException.ExceptionReason.Redefined);
                var existingLabel = Services.SymbolManager.GetSymbol(label, false);
                if (existingLabel.NumericValue != address)
                {
                    Services.PassNeeded = true;
                    existingLabel.NumericValue = address;
                }
            }
            else
            {
                Services.SymbolManager.DefineSymbolicAddress(label, address, Services.Output.CurrentBank);
            }
            if (setLocalScope)
                Services.SymbolManager.LocalScope = label.Name.ToString();
        }

        /// <summary>
        /// Gets the approximate instruction size of a given <see cref="SourceLine"/>.
        /// </summary>
        /// <param name="line">The parsed <see cref="SourceLine"/>.</param>
        /// <returns>The size of the instruction</returns>
        internal virtual int GetInstructionSize(SourceLine line) => 0;

        /// <summary>
        /// Assemble the collection of parsed <see cref="SourceLine"/> objects. This method must be inherited.
        /// </summary>
        /// <param name="lines">A <see cref="RandomAccessIterator{T}"/> of source lines.</param>
        /// <returns>The disassembly output from the assembly operation.</returns>
        /// <exception cref="BlockClosureException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="DivideByZeroException"/>
        /// <exception cref="ExpressionException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="InvalidPCAssignmentException"/>
        /// <exception cref="BlockAssemblerException"/>
        /// <exception cref="OverflowException"/>
        /// <exception cref="ProgramOverflowException"/>
        /// <exception cref="ReturnException"/>
        /// <exception cref="SectionException"/>
        /// <exception cref="SymbolException"/>
        /// <exception cref="SyntaxException"/>
        protected abstract string OnAssemble(RandomAccessIterator<SourceLine> lines);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of instructions where the <see cref="AssemblerBase"/> should 
        /// not define any line label as a symbolic address or reference when 
        /// performing the <see cref="AssemblerBase.AssembleLine(SourceLine)"/> action.
        /// </summary>
        protected HashSet<StringView> ExcludedInstructionsForLabelDefines { get; }

        /// <summary>
        /// Gets the collection of instructions the <see cref="AssemblerBase"/> will 
        /// not define any line label as a symbolic address or reference when
        /// performing the <see cref="AssemblerBase.AssemblesLine(SourceLine)"/> action.
        /// </summary>
        protected HashSet<string> ExcludeReservedTypesFromLabelDefines { get; }

        /// <summary>
        /// Gets the reserved keywords of the <see cref="AssemblerBase"/> object.
        /// </summary>
        protected ReservedWords Reserved { get; }

        /// <summary>
        /// Gets the state of the Logical Program Counter for the <see cref="Assembler"/>'s <see cref="BinaryOutput"/>
        /// object when OnAssemble was invoked.
        /// </summary>
        protected int LogicalPCOnAssemble { get; private set; }

        /// <summary>
        /// Gets the state of the long Logical Program Counter for the <see cref="Assembler"/>'s <see cref="BinaryOutput"/>
        /// object when OnAssemble was invoked.
        /// </summary>
        protected int LongLogicalPCOnAssemble { get; private set; }

        /// <summary>
        /// Gets the state of the real Program Counter for the <see cref="Assembler"/>'s <see cref="BinaryOutput"/>
        /// object when OnAssemble was invoked.
        /// </summary>
        protected int PCOnAssemble { get; private set; }

        /// <summary>
        /// Gets the state of the long real Program Counter for the <see cref="Assembler"/>'s <see cref="BinaryOutput"/>
        /// object when OnAssemble was invoked.
        /// </summary>
        protected int LongPCOnAssemble { get; private set; }

        /// <summary>
        /// The shared assembly services.
        /// </summary>
        protected AssemblyServices Services { get; }

        #endregion
    }
}
