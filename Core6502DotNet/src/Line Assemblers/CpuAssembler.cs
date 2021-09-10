//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------
using System;
using System.Linq;

namespace Core6502DotNet
{

    /// <summary>
    /// Represents an invalid CPU error.
    /// </summary>
    public class InvalidCpuException : Exception
    {
        /// <summary>
        /// Construct a new invalid CPU exception.
        /// </summary>
        /// <param name="cpu">The CPU specified that caused the exception.</param>
        public InvalidCpuException(string cpu)
            : base($"Invalid CPU \"{cpu}\" specified.")
        {

        }
    }

    /// <summary>
    /// Represents an assembler for CPU-specified. instructions and operations. This class
    /// must be inherited.
    /// </summary>
    public abstract class CpuAssembler : AssemblerBase
    {
        #region Members

        readonly string _initCpu;
        string _cpu;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a CPU assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        protected CpuAssembler(AssemblyServices services)
            : base(services)
        {

            Reserved.DefineType("CPU", ".cpu");
            if (!string.IsNullOrEmpty(Services.CPU))
                CPU = Services.CPU;
            else
                _cpu = string.Empty;
            _initCpu = _cpu;
            OnSetCpu();

            Evaluations = new double[]
            {
                double.NaN, double.NaN, double.NaN
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resets the state of the <see cref="CpuAssembler"/>.
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// Actions to perform when the CPU is set. This method must be inherited.
        /// </summary>
        protected abstract void OnSetCpu();

        /// <summary>
        /// Assembles the instruction/mnemonic specific to the CPU.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> of the instruction.</param>
        /// <returns>The listing representation of the instruction.</returns>
        protected abstract string AssembleCpuInstruction(SourceLine line);

        /// <summary>
        /// Determines whether the CPU selected is valid. This method must be inherited.
        /// </summary>
        /// <param name="cpu">The CPU name.</param>
        /// <returns><c>true</c> if the CPU is valid, <c>false</c> otherwise.</returns>
        public abstract bool IsCpuValid(string cpu);

        void OnPassChanged(object sender, EventArgs args)
        {
            _cpu = _initCpu;
            OnReset();
        }

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (Reserved.IsOneOf("CPU", line.Instruction.Name))
            {
                var iterator = line.Operands.GetIterator();
                if (!iterator.MoveNext() || !StringHelper.IsStringLiteral(iterator) || iterator.PeekNext() != null)
                    Services.Log.LogEntry(line.Instruction,
                        "String expression expected.");
                else
                {
                    CPU = iterator.Current.Name.ToString().TrimOnce('"');
                    OnSetCpu();
                }
                return string.Empty;
            }
            Evaluations[0] = Evaluations[1] = Evaluations[2] = double.NaN;
            var next = lines.PeekNext();
            if (next != null && next.Instruction != null &&
                Calls.Contains(line.Instruction.Name.ToString()) &&
                Returns.Contains(next.Instruction.Name.ToString()))
                Services.Log.LogEntry(line,
                    $"Consider changing \"{line.Instruction}\" to a jump instruction.",
                    line.Instruction.Name.ToString(),
                    false);
            return AssembleCpuInstruction(line);
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the CPU.
        /// </summary>
        /// <exception cref="InvalidCpuException"></exception>
        protected string CPU
        {
            get => _cpu;
            private set
            {
                if (!IsCpuValid(value))
                    throw new InvalidCpuException(value);
                _cpu = value;
            }
        }

        /// <summary>
        /// Gets the individual evaluations in the expression.
        /// </summary>
        protected double[] Evaluations { get; }

        /// <summary>
        /// Gets the subroutine call mnemonics. This property must be implemented.
        /// </summary>
        protected abstract string[] Calls { get; }

        /// <summary>
        /// Gets the subroutine return mnemonics. This property must be implemented.
        /// </summary>
        protected abstract string[] Returns { get; }

        #endregion
    }
}
