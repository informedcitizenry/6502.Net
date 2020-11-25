//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
     /// <summary>
    /// An assembler for Motorola m68xx and MOS 65xx-based CPUs. This class must be inherited.
    /// </summary>
    public abstract class MotorolaBase : AssemblerBase
    {
        #region Constants

        /// <summary>
        /// The column padding for disassembly.
        /// </summary>
        protected const int Padding = 25;

        /// <summary>
        /// An enumeration of all possible addressing modes for m68xx and m65xx-based CPUs.
        /// </summary>
        protected enum Modes 
        {
            Implied      = 0b000000000000000000000,
            ZeroPage     = 0b000000000000000000001,
            Absolute     = 0b000000000000000000011,
            Long         = 0b000000000000000000111,
            SizeMask     = 0b000000000000000000111,
            Indirect     = 0b000000000000000001000,
            DirectPage   = 0b000000000000000010000,
            IndexedX     = 0b000000000000000100000,
            InnerX       = 0b000000000000001000000,
            IndexedY     = 0b000000000000010000000,
            IndexedZ     = 0b000000000000100000000,
            IndexedS     = 0b000000000001000000000,
            IndexedSp    = 0b000000000010000000000,
            TwoOpBit     = 0b000000000100000000000,
            TwoOperand   = 0b000000000100000000001,
            ThreeOperand = 0b000000001000000000000,
            Bit0         = 0b000000010000000000000,
            Bit1         = 0b000000110000000000000,
            Bit2         = 0b000001010000000000000,
            Bit3         = 0b000001110000000000000,
            Bit4         = 0b000010010000000000000,
            Bit5         = 0b000010110000000000000,
            Bit6         = 0b000011010000000000000,
            Bit7         = 0b000011110000000000000,
            Immediate    = 0b000100000000000000001,
            RelativeBit  = 0b001000000000000000000,
            Relative     = 0b001000000000000000001,
            PostByteBit  = 0b010000000000000000000,
            MemModMask   = 0b011111111111111111000,
            ModeMask     = 0b011111111111111111111,
            ForceWidth   = 0b100000000000000000000,
            AbsoluteX    = Absolute     | IndexedX,
            AbsoluteY    = Absolute     | IndexedY,
            ZeroPageX    = ZeroPage     | IndexedX,
            ZeroPageY    = ZeroPage     | IndexedY,
            ZeroPageS    = ZeroPage     | IndexedS,
            LongX        = Long         | IndexedX,
            IndX         = ZeroPage     | InnerX    | Indirect,
            IndY         = ZeroPageY    | Indirect,
            IndZ         = ZeroPage     | Indirect  | IndexedZ,
            IndS         = IndY         | IndexedS,
            IndSp        = IndY         | IndexedSp,
            IndZp        = ZeroPage     | Indirect,
            IndAbs       = Absolute     | Indirect,
            IndAbsX      = Absolute     | Indirect  | InnerX,
            Dir          = ZeroPage     | DirectPage,
            DirAbs       = Absolute     | DirectPage,
            DirY         = Dir          | IndexedY,
            DirIndMask   = Indirect     | DirectPage,
            RelativeAbs  = Relative     | Absolute,
            ThreeOpRel   = ThreeOperand | RelativeBit,
            ThreeOpAbs   = ThreeOperand | Absolute,
            Zp0          = TwoOperand   | Bit0,
            Zp1          = TwoOperand   | Bit1,
            Zp2          = TwoOperand   | Bit2,
            Zp3          = TwoOperand   | Bit3,
            Zp4          = TwoOperand   | Bit4,
            Zp5          = TwoOperand   | Bit5,
            Zp6          = TwoOperand   | Bit6,
            Zp7          = TwoOperand   | Bit7,
            ThreeOpRel0  = ZeroPage     | ThreeOpRel | Bit0,
            ThreeOpRel1  = ZeroPage     | ThreeOpRel | Bit1,
            ThreeOpRel2  = ZeroPage     | ThreeOpRel | Bit2,
            ThreeOpRel3  = ZeroPage     | ThreeOpRel | Bit3,
            ThreeOpRel4  = ZeroPage     | ThreeOpRel | Bit4,
            ThreeOpRel5  = ZeroPage     | ThreeOpRel | Bit5,
            ThreeOpRel6  = ZeroPage     | ThreeOpRel | Bit6,
            ThreeOpRel7  = ZeroPage     | ThreeOpRel | Bit7,
            ImmAbs       = Immediate    | Absolute,
            TestBitZp    = Immediate    | TwoOperand,
            TestBitAbs   = Immediate    | TwoOperand | Absolute,
            TestBitZpX   = Immediate    | TwoOperand | IndexedX,
            TestBitAbsX  = Immediate    | TwoOperand | Absolute | IndexedX,
            TestBitFlag  = TestBitZp    & MemModMask
        };

        #endregion

        #region Members

        static readonly IReadOnlyDictionary<Modes, string> s_modeFormats = new Dictionary<Modes, string>
        {
            { Modes.Implied,        string.Empty                },
            { Modes.Immediate,      "#${0:x2}"                  },
            { Modes.ImmAbs,         "#${0:x4}"                  },
            { Modes.ZeroPage,       "${0:x2}"                   },
            { Modes.ZeroPageS,      "${0:x2},s"                 },
            { Modes.ZeroPageX,      "${0:x2},x"                 },
            { Modes.ZeroPageY,      "${0:x2},y"                 },
            { Modes.Relative,       "${0:x4}"                   },
            { Modes.RelativeAbs,    "${0:x4}"                   },
            { Modes.Absolute,       "${0:x4}"                   },
            { Modes.AbsoluteX,      "${0:x4},x"                 },
            { Modes.AbsoluteY,      "${0:x4},y"                 },
            { Modes.Long,           "${0:x6}"                   },
            { Modes.LongX,          "${0:x6},x"                 },
            { Modes.IndZp,          "(${0:x2})"                 },
            { Modes.IndS,           "(${0:x2},s),y"             },
            { Modes.IndSp,          "(${0:x2},sp),y"            },
            { Modes.IndX,           "(${0:x2},x)"               },
            { Modes.IndY,           "(${0:x2}),y"               },
            { Modes.IndZ,           "(${0:x2}),z"               },
            { Modes.IndAbs,         "(${0:x4})"                 },
            { Modes.IndAbsX,        "(${0:x4},x)"               },
            { Modes.Dir,            "[${0:x2}]"                 },
            { Modes.DirY,           "[${0:x2}],y"               },
            { Modes.DirAbs,         "[${0:x4}]"                 },
            { Modes.Zp0,            "0,${0:x2}"                 },
            { Modes.Zp1,            "1,${0:x2}"                 },
            { Modes.Zp2,            "2,${0:x2}"                 },
            { Modes.Zp3,            "3,${0:x2}"                 },
            { Modes.Zp4,            "4,${0:x2}"                 },
            { Modes.Zp5,            "5,${0:x2}"                 },
            { Modes.Zp6,            "6,${0:x2}"                 },
            { Modes.Zp7,            "7,${0:x2}"                 },
            { Modes.TwoOperand,     "${0:x2},${1:x2}"           },
            { Modes.TestBitZp,      "#${0:x2},${1:x2}"          },
            { Modes.TestBitZpX,     "#${0:x2},${1:x2},x"        },
            { Modes.TestBitAbs,     "#${0:x2},${1:x4}"          },
            { Modes.TestBitAbsX,    "#${0:x2},${1:x4},x"        },
            { Modes.ThreeOpRel0,    "0,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel1,    "1,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel2,    "2,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel3,    "3,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel4,    "4,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel5,    "5,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel6,    "6,${0:x2},${1:x4}"         },
            { Modes.ThreeOpRel7,    "7,${0:x2},${1:x4}"         },
            { Modes.ThreeOpAbs,     "${0:x4},${1:x4},${2:x4}"   }
        };

        #endregion

        #region Constructors

        public MotorolaBase(AssemblyServices services)
            :base(services)
        {
            Reserved.DefineType("CPU", ".cpu");
            Reserved.DefineType("RelativeSecond");
            Reserved.DefineType("SwapOperands");
            
            Evaluations = new double[]
            {
                double.NaN, double.NaN, double.NaN
            };
            if (!string.IsNullOrEmpty(Services.CPU))
            {
                CPU = Services.CPU;
                if (!IsCpuValid(CPU))
                    throw new Exception($"Invalid CPU \"{CPU}\" specified.");
            }
            else
            {
                CPU = string.Empty;
            }
            OnSetCpu();
            Services.PassChanged += OnPassChanged;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reset the state of the <see cref="MotorolaBase"/> assembler. This method must be inherited.
        /// </summary>
        protected abstract void OnReset();

        void OnPassChanged(object sender, EventArgs args)
            => OnReset();

        /// <summary>
        /// Evaluate whether the given token with the total expression count
        /// constitutes a forced width specifier, and return that 
        /// forced width result.
        /// </summary>
        /// <param name="firstToken">The first token in the expression.</param>
        /// <param name="expressionCount">The total expression count.</param>
        /// <returns>The forced width size, if the token is a forced width specifier.</returns>
        /// <exception cref="ExpressionException"></exception>
        protected Modes GetForcedModifier(RandomAccessIterator<Token> tokens)
        {
            var ix = tokens.Index;
            if (tokens.MoveNext())
            {
                var firstToken = tokens.Current;

                if (firstToken.Name.Equals("["))
                {
                    var forcedGroup = Token.GetGroup(tokens).Skip(1).SkipLast(1).GetIterator();
                    if (!Token.IsEnd(tokens.Current))
                    {
                        var opSize = Services.Evaluator.Evaluate(forcedGroup, 8, 24);
                        if (forcedGroup.Current != null)
                            throw new SyntaxException(forcedGroup.Current.Position, "Unexpected expression.");
                        var forcedMode = opSize switch
                        {
                            8    => Modes.ZeroPage,
                            16   => Modes.Absolute,
                            24   => Modes.Long,
                            _    => throw new ExpressionException(firstToken.Position,
                                    $"Illegal quantity {opSize} for bit-width specifier."),
                        };
                        tokens.Rewind(tokens.Index - 1);
                        return forcedMode | Modes.ForceWidth;
                    }

                }
            }
            tokens.Reset();
            return Modes.Implied;
        }

        /// <summary>
        /// Evaluates the current expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens in the current expression.</param>
        /// <param name="doNotAdvance">Do not advance the iterator past its current point when beginning evaluation.</param>
        /// <param name="evaluationIndex">The index of the evaluation.</param>
        /// <returns></returns>
        protected Modes Evaluate(RandomAccessIterator<Token> tokens, bool doNotAdvance, int evaluationIndex)
        {
            var mode = Modes.ZeroPage;
            var result = Services.Evaluator.Evaluate(tokens, doNotAdvance, Int24.MinValue, UInt24.MaxValue);
            if (result < sbyte.MinValue || result > byte.MaxValue)
            {
                mode |= Modes.Absolute;
                if (result < short.MinValue || result > ushort.MaxValue)
                    mode |= Modes.Long;
            }
            Evaluations[evaluationIndex] = result;
            return mode;
        }

        /// <summary>
        /// Evaluates the current expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens in the current expression.</param>
        /// <param name="evaluationIndex">The index of the evaluation.</param>
        /// <returns></returns>
        protected Modes Evaluate(RandomAccessIterator<Token> tokens, int evaluationIndex)
            => Evaluate(tokens, true, evaluationIndex);

        /// <summary>
        /// Parses the operand expression into its addressing mode and saves the evaluation.
        /// This method must be inherited.
        /// </summary>
        /// <param name="line">The parsed source lne.</param>
        /// <returns>The <see cref="Modes"/>.</returns>
        protected abstract Modes ParseOperand(SourceLine line);

        (Modes mode, CpuInstruction instruction) GetModeInstruction(SourceLine line)
        {
            Evaluations[0] = Evaluations[1] = Evaluations[2] = double.NaN;
            var instruction = line.Instruction.Name.ToLower();
            var mnemmode = (Mnem: instruction, Mode: ParseOperand(line));

            // remember if force width bit was set
            var forceWidth = mnemmode.Mode.HasFlag(Modes.ForceWidth);

            // drop the force width bit off
            mnemmode.Mode &= Modes.ModeMask;
            if (!ActiveInstructions.TryGetValue(mnemmode, out CpuInstruction foundInstruction) && !forceWidth)
            {
                var sizeModeBit = (int)Modes.ZeroPage;
                while (!ActiveInstructions.TryGetValue(mnemmode, out foundInstruction))
                {
                    sizeModeBit <<= 1;
                    if (sizeModeBit > 7)
                        break;
                    mnemmode.Mode = (mnemmode.Mode & Modes.MemModMask) | (Modes)sizeModeBit | Modes.ZeroPage;
                }
            }
            return (mnemmode.Mode, foundInstruction);
        }

        /// <summary>
        /// Actions to perform when the CPU is set. This method must be inherited.
        /// </summary>
        protected abstract void OnSetCpu();

        /// <summary>
        /// Determines whether the CPU selected is valid. This method must be inherited.
        /// </summary>
        /// <param name="cpu">The CPU name.</param>
        /// <returns><c>true</c> if the CPU is valid, <c>false</c> otherwise.</returns>
        protected abstract bool IsCpuValid(string cpu);

        internal override int GetInstructionSize(SourceLine line)
        {
            if (Reserved.IsOneOf("CPU", line.Instruction.Name))
                return 0;
            try
            {
                return GetModeInstruction(line).instruction.Size;
            }
            catch
            {
                return 2;
            }
        }

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (Reserved.IsOneOf("CPU", line.Instruction.Name))
            {
                var iterator = line.Operands.GetIterator();
                if (!iterator.MoveNext() || !iterator.Current.IsDoubleQuote() || iterator.PeekNext() != null)
                    Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                        "String expression expected.");
                else
                {
                    CPU = iterator.Current.Name.ToString().TrimOnce('"');
                    OnSetCpu();
                }
                return string.Empty;
            }
            var modeInstruction = GetModeInstruction(line);
            if (!string.IsNullOrEmpty(modeInstruction.instruction.CPU))
            {
                if (modeInstruction.mode.HasFlag(Modes.RelativeBit))
                {
                    int evalIx;
                    if (Reserved.IsOneOf("RelativeSecond", line.Instruction.Name))
                        evalIx = 1;
                    else
                        evalIx = 0;
                    var offsIx = modeInstruction.instruction.Size;
                    try
                    {
                        Evaluations[evalIx] = modeInstruction.mode == Modes.RelativeAbs
                            ? Convert.ToInt16(Services.Output.GetRelativeOffset((int)Evaluations[evalIx], offsIx))
                            : Convert.ToSByte(Services.Output.GetRelativeOffset((int)Evaluations[evalIx], offsIx));
                    }
                    catch (OverflowException)
                    {
                        // don't worry about overflows for relative offsets if passes are still needed
                        if (!Services.PassNeeded)
                        {
                            var errorMsg = "Relative offset for branch was too far.";
                            if (PseudoBranchSupported)
                                Services.Log.LogEntry(line.Filename, line.LineNumber, line.Operands[0].Position,
                                    $"{errorMsg}. Consider using a pseudo branch directive.");
                            else
                                Services.Log.LogEntry(line.Filename, line.LineNumber, line.Operands[0].Position, errorMsg);
                            return string.Empty;
                        }
                        Evaluations[evalIx] = 0;
                    }
                }
                var operandSize = (modeInstruction.mode & Modes.SizeMask) switch
                {
                    Modes.Implied  => 0,
                    Modes.ZeroPage => 1,
                    Modes.Absolute => 2,
                    _              => 3
                };
                // start adding to the output
                var opcode = modeInstruction.instruction.Opcode;
                Services.Output.Add(opcode, opcode.Size());

                if (operandSize > 0)
                {
                    // add operand bytes
                    for (int i = 0; i < 3 && !double.IsNaN(Evaluations[i]); i++)
                    {
                        if ((modeInstruction.mode & Modes.TestBitFlag) != 0 &&
                             modeInstruction.mode.HasFlag(Modes.TwoOpBit) &&
                                i == 0)
                        { // The Hudson test bit instructions
                            if (Evaluations[i] >= sbyte.MinValue && Evaluations[i] <= byte.MaxValue)
                                Services.Output.Add(Evaluations[i], 1);
                        }
                        else
                        {
                            if (Evaluations[i].Size() > operandSize)
                                break;
                            else
                                Services.Output.Add(Evaluations[i], operandSize);
                        }
                    }
                }
                var instructionSize = Services.Output.LogicalPC - PCOnAssemble;
                if (!Services.PassNeeded && instructionSize != modeInstruction.instruction.Size)
                    Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                        "Mode not supported in selected CPU.");
            }
            else
            {
                if (ActiveInstructions.Keys.Any(k => k.Mnem.Equals(line.Instruction.Name, Services.StringComparison)))
                {
                    if (!Services.PassNeeded)
                        Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                            "Mode not supported in selected CPU.");
                }
                else
                {
                    Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                        "Mnemonic not supported for selected CPU.");
                }
                return string.Empty;
            }
            if (Services.PassNeeded || string.IsNullOrEmpty(Services.Options.ListingFile))
                return string.Empty;
            var sb = new StringBuilder();
            if (!Services.Options.NoAssembly)
            {
                var byteString = Services.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.');
                sb.Append(byteString.PadRight(Padding));
            }
            else
            {
                sb.Append($".{PCOnAssemble:x4}                        ");
            }
            if (!Services.Options.NoDisassembly)
            {
                var disSb = new StringBuilder();
                if (sb.Length > 29)
                    disSb.Append(' ');
                disSb.Append(line.Instruction.Name.ToLower());
                if (modeInstruction.mode != Modes.Implied)
                {
                    disSb.Append(' ');
                    var memoryMode = modeInstruction.mode & Modes.MemModMask;
                    var size = modeInstruction.mode & Modes.SizeMask;

                    if (memoryMode.HasFlag(Modes.RelativeBit))
                        size |= Modes.Absolute;
                    int eval2 = 0, eval3 = 0;
                    int eval1;
                    if (size == Modes.Long)
                    {
                        eval1 = (int)Evaluations[0] & 0xFFFFFF;
                    }
                    else if (size >= Modes.Absolute)
                    {
                        if (memoryMode.HasFlag(Modes.RelativeBit))
                        {
                            if (Reserved.IsOneOf("RelativeSecond", line.Instruction.Name))
                            {
                                eval1 = (int)Evaluations[0] & 0xFF;
                                eval2 = Services.Output.LogicalPC + (int)Evaluations[1];
                                eval2 &= 0xFFFF;
                            }
                            else
                            {
                                eval1 = Services.Output.LogicalPC + (int)Evaluations[0];
                                eval1 &= 0xFFFF;
                            }
                        }
                        else
                        {
                            eval1 = (int)Evaluations[0] & 0xFFFF;
                            if (!double.IsNaN(Evaluations[1]))
                                eval2 = (int)Evaluations[1] & 0xFFFF;
                            if (!double.IsNaN(Evaluations[2]))
                                eval3 = (int)Evaluations[2] & 0xFFFF;
                        }
                    }
                    else if (Reserved.IsOneOf("SwapOperands", line.Instruction.Name))
                    {
                        eval1 = (int)Evaluations[1] & 0xFF;
                        eval2 = (int)Evaluations[0] & 0xFF;
                    }
                    else
                    {
                        eval1 = (int)Evaluations[0] & 0xFF;
                        if (!double.IsNaN(Evaluations[1]))
                            eval2 = (int)Evaluations[1] & 0xFF;
                        if (!double.IsNaN(Evaluations[2]))
                            eval3 = (int)Evaluations[2] & 0xFF;
                    }
                    disSb.AppendFormat(s_modeFormats[modeInstruction.mode], eval1, eval2, eval3);
                }
                sb.Append(disSb.ToString().PadRight(18));
            }
            else
            {
                sb.Append("                  ");
            }
            if (!Services.Options.NoSource)
                sb.Append(line.Source);
            return sb.ToString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether pseud-branch mnemonics are supported.
        /// This property must be inherited.
        /// </summary>
        protected abstract bool PseudoBranchSupported { get; }

        /// <summary>
        /// Gets the CPU.
        /// </summary>
        protected string CPU { get; private set; }

        /// <summary>
        /// Gets the individual evaluations in the expression.
        /// </summary>
        protected double[] Evaluations { get; }

        /// <summary>
        /// Gets or sets the active instruction set for the selected CPU.
        /// </summary>
        protected IReadOnlyDictionary<(string Mnem, Modes Mode), CpuInstruction> 
            ActiveInstructions { get; set; }

        #endregion
    }
}
