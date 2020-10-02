//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Core6502DotNet
{
    /// <summary>
    /// A class containing basic information about the environment and application.
    /// </summary>
    public static class Assembler
    {
        #region Properties

        /// <summary>
        /// Gets the version of the assembler. This can and should be set
        /// by the client code.
        /// </summary>
        public static string AssemblerVersion
        {
            get
            {
                var assemblyName = Assembly.GetEntryAssembly().GetName();
                return $"Version {assemblyName.Version.Major}.{assemblyName.Version.Minor} Build {assemblyName.Version.Build}";
            }
        }

        /// <summary>
        /// Gets the assembler (product) name. This can and should be set
        /// by the client code.
        /// </summary>
        public static string AssemblerName
        {
            get
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                return $"{versionInfo.Comments}\n{versionInfo.LegalCopyright}";
            }
        }

        /// <summary>
        /// Gets the assembler's simple name, based on the AssemblerName
        /// property.
        /// </summary>
        public static string AssemblerNameSimple
        {
            get
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                return $"{versionInfo.Comments}";
            }
        }
        #endregion
    }

    /// <summary>
    /// The base class for all core 6502 classes.
    /// </summary>
    public class Core6502Base
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of the core 6502 base class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public Core6502Base(AssemblyServices services)
            => Services = services;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the shared <see cref="AssemblyServices"/> object.
        /// </summary>
        protected AssemblyServices Services { get; }

        #endregion
    }

    /// <summary>
    ///The base line assembler class. This class must be inherited.
    /// </summary>
    public abstract class AssemblerBase : Core6502Base
    {

        #region Constructors

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        protected AssemblerBase(AssemblyServices services)
            :base(services)
        {
            ExcludedInstructionsForLabelDefines = new HashSet<string>(services.StringComparer);
            Reserved = new ReservedWords(services.StringComparer);
            services.IsReserved.Add(Reserved.IsReserved);
            services.SymbolManager.AddValidSymbolNameCriterion(s => !Reserved.IsReserved(s));
            services.InstructionLookupRules.Add(s => Assembles(s));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the token is a reserved word to the assembler object.
        /// </summary>
        /// <param name="token">The token to check if reserved</param>
        /// <returns><c>true</c> if reserved, otherwise <c>false</c>.</returns>
        public virtual bool IsReserved(string token) => Reserved.IsReserved(token);

        /// <summary>
        /// Determines whether the assembler assembles the keyword.
        /// </summary>
        /// <param name="s">The symbol/keyword name.</param>
        /// <returns><c>true</c> if the assembler assembles the keyword, 
        /// otherwise <c>false</c>.</returns>
        public virtual bool Assembles(string s) => IsReserved(s);

        /// <summary>
        /// Determines whether the assembler assembles the given source line.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <returns><c>true</c> if the assembler assembles the line, 
        /// otherwise <c>false</c>.</returns>
        public virtual bool AssemblesLine(SourceLine line) 
            => (!string.IsNullOrEmpty(line.LabelName) && line.Instruction == null)
            || IsReserved(line.InstructionName);

        /// <summary>
        /// Assemble the <see cref="SourceLine"/>. This method must be inherited.
        /// </summary>
        /// <param name="line">The source line.</param>
        protected abstract string OnAssembleLine(SourceLine line);

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
        public string AssembleLine(SourceLine line)
        {
            bool isSpecial = line.LabelName.IsSpecialOperator();
            PCOnAssemble = Services.Output.LogicalPC;
            if (line.Label != null && !line.LabelName.Equals("*"))
            {
                if (isSpecial)
                    Services.SymbolManager.DefineLineReference(line.LabelName, PCOnAssemble);
                else if (line.Instruction == null || 
                          !ExcludedInstructionsForLabelDefines.Contains(line.InstructionName))
                    Services.SymbolManager.DefineSymbolicAddress(line.LabelName);
            }
            if (line.Instruction != null)
                return OnAssembleLine(line);
            if (line.Label != null && !isSpecial)
            {
                var labelValue = Services.SymbolManager.GetNumericValue(line.LabelName);
                if (!double.IsNaN(labelValue))
                    return string.Format(".{0}{1}",
                                 ((int)labelValue).ToString("x4").PadRight(42),
                                 Services.Options.NoSource ? string.Empty : line.UnparsedSource);
            }
            return string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of instructions where the <see cref="AssemblerBase"/> should 
        /// not define any line label as a symbolic address or reference when 
        /// performing the <see cref="AssemblerBase.AssembleLine(SourceLine)"/> action.
        /// </summary>
        protected HashSet<string> ExcludedInstructionsForLabelDefines { get; }

        /// <summary>
        /// Gets the reserved keywords of the <see cref="AssemblerBase"/> object.
        /// </summary>
        protected ReservedWords Reserved { get; }

        /// <summary>
        /// Gets the state of the Program Counter for the <see cref="Assembler"/>'s <see cref="BinaryOutput"/>
        /// object when OnAssemble was invoked.
        /// </summary>
        protected int PCOnAssemble { get; private set; }

        #endregion
    }
}