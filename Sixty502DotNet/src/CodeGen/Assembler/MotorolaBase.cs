//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that provides a base implementation to support classes
    /// responsible for assembling 65xx and m680x code. This class must be
    /// inherited.
    /// </summary>
    public abstract class MotorolaBase : InstructionSet
    {
        protected const int
            Implied      = 0b000000000000000000000,
            ZeroPage     = 0b000000000000000000001,
            AbsoluteFlag = 0b000000000000000000010,
            Absolute     = 0b000000000000000000011,
            LongFlag     = 0b000000000000000000100,
            Long         = 0b000000000000000000111,
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
            SizeMask     = Long,
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
            DirZ         = Dir          | IndexedZ,
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
            TestBitFlag  = TestBitZp    & MemModMask;

        private readonly static Dictionary<int, string> s_disassemblyFormats = new()
        {
            { Implied,        string.Empty                 },
            { Immediate,      "#${0:x2}"                   },
            { ImmAbs,         "#${0:x4}"                   },
            { ZeroPage,       "${0:x2}"                    },
            { ZeroPageS,      "${0:x2},s"                  },
            { ZeroPageX,      "${0:x2},x"                  },
            { ZeroPageY,      "${0:x2},y"                  },
            { Relative,       "${0:x4}"                    },
            { RelativeAbs,    "${0:x4}"                    },
            { Absolute,       "${0:x4}"                    },
            { AbsoluteX,      "${0:x4},x"                  },
            { AbsoluteY,      "${0:x4},y"                  },
            { Long,           "${0:x6}"                    },
            { LongX,          "${0:x6},x"                  },
            { IndZp,          "(${0:x2})"                  },
            { IndS,           "(${0:x2},s),y"              },
            { IndSp,          "(${0:x2},sp),y"             },
            { IndX,           "(${0:x2},x)"                },
            { IndY,           "(${0:x2}),y"                },
            { IndZ,           "(${0:x2}),z"                },
            { IndAbs,         "(${0:x4})"                  },
            { IndAbsX,        "(${0:x4},x)"                },
            { Dir,            "[${0:x2}]"                  },
            { DirY,           "[${0:x2}],y"                },
            { DirZ,           "[${0:x2}],z"                },
            { DirAbs,         "[${0:x4}]"                  },
            { Zp0,            "0,${0:x2}"                  },
            { Zp1,            "1,${0:x2}"                  },
            { Zp2,            "2,${0:x2}"                  },
            { Zp3,            "3,${0:x2}"                  },
            { Zp4,            "4,${0:x2}"                  },
            { Zp5,            "5,${0:x2}"                  },
            { Zp6,            "6,${0:x2}"                  },
            { Zp7,            "7,${0:x2}"                  },
            { TwoOperand,     "${0:x2},${1:x2}"            },
            { TestBitZp,      "#${0:x2},${1:x2}"           },
            { TestBitZpX,     "#${0:x2},${1:x2},x"         },
            { TestBitAbs,     "#${0:x2},${1:x4}"           },
            { TestBitAbsX,    "#${0:x2},${1:x4},x"         },
            { ThreeOpRel0,    "0,${0:x2},${1:x4}"          },
            { ThreeOpRel1,    "1,${0:x2},${1:x4}"          },
            { ThreeOpRel2,    "2,${0:x2},${1:x4}"          },
            { ThreeOpRel3,    "3,${0:x2},${1:x4}"          },
            { ThreeOpRel4,    "4,${0:x2},${1:x4}"          },
            { ThreeOpRel5,    "5,${0:x2},${1:x4}"          },
            { ThreeOpRel6,    "6,${0:x2},${1:x4}"          },
            { ThreeOpRel7,    "7,${0:x2},${1:x4}"          },
            { ThreeOpAbs,     "${0:x4},${1:x4},${2:x4}"    }
        };

        /// <summary>
        /// Construct a new instance of the <see cref="MotorolaBase"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="initialArchitecture">The initial CPU/architecture
        /// to initialize the instruction set mnemonics.</param>
        protected MotorolaBase(AssemblyServices services, string initialArchitecture)
            : base(services, initialArchitecture) { }

        private bool Is6502() => string.IsNullOrEmpty(Services.CPU) || Services.CPU.Equals("6502");

        /// <summary>
        /// Calculate the bitwidth modifier expression in the statement.
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="operand"></param>
        /// <param name="bitwidth"></param>
        /// <returns></returns>
        protected int CalculateBitwidth(Sixty502DotNetParser.ExprContext expr, int operand, Sixty502DotNetParser.BitwidthContext bitwidth)
        {
            var bw = Services.ExpressionVisitor.Visit(bitwidth.expr());
            if (!bw.IsDefined)
            {
                if (Services.State.PassNeeded)
                {
                    Services.Output.AddUninitialized(3);
                }
                return Services.State.PassNeeded ? 0 : -1;
            }
            if (bw.IsIntegral)
            {
                var bwVal = bw.ToInt();
                if (bwVal >= 8 && bwVal <= 24 && bwVal % 8 == 0)
                {
                    var bwSize = bwVal / 8;
                    if (operand.Size() > bwSize)
                    {
                        Services.Log.LogEntry(expr, Errors.IllegalQuantity);
                        return -1;
                    }
                    return bwSize;
                }
                else
                {
                    Services.Log.LogEntry(bitwidth.expr(), "Bitwidth value not valid.");
                }
            }
            else
            {
                Services.Log.LogEntry(bitwidth.expr(), "Bitdwidth modifier expects integral value.");
            }
            return -1;
        }

        /// <summary>
        /// Process an assembler directive targeting a specific CPU.
        /// </summary>
        /// <param name="context">The parsed directive statement context.</param>
        public abstract void CpuDirectiveStatement(Sixty502DotNetParser.CpuDirectiveStatContext context);


        /// <summary>
        /// Set the page size for direct page operations.
        /// </summary>
        /// <param name="context">The parsed statement context.</param>
        public void SetPage(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            var expression = context.expr();
            if (expression == null)
            {
                Services.Log.LogEntry(context, Errors.ExpectedExpression);
                return;
            }
            if (SupportsDirectPage)
            {
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(expression, byte.MinValue, byte.MaxValue, out var page))
                {
                    Page = (int)page << 8;
                }
                else if (!Services.State.PassNeeded)
                {
                    Services.Log.LogEntry(expression, Errors.IllegalQuantity);
                }
            }
            else
            {
                Services.Log.LogEntry(context, "Directive ignored for CPU.", false);
            }
        }

        /// <summary>
        /// Generate code for the given instruction containing an operand.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="mode">The addressing mode.</param>
        /// <param name="expr">The parsed operand expression.</param>
        /// <param name="defaultUndefinedSize">The default size of the
        /// instruction if the operand expression result is an undefined
        /// value.</param>
        /// <param name="bitwidth">The parsed bitwidth modifier expression,
        /// if pressent in the statement.</param>
        /// <returns><c>true</c> if the operand is successful, <c>false</c>
        /// otherewise.</returns>
        protected bool GenOperand(IToken mnemonic,
                                int mode,
                                Sixty502DotNetParser.ExprContext expr,
                                int defaultUndefinedSize = 2,
                                Sixty502DotNetParser.BitwidthContext? bitwidth = null)
        {
            int mnemType = mnemonic.Type;
            var val = Services.ExpressionVisitor.Visit(expr);
            if (!val.IsDefined)
            {
                if (Services.State.PassNeeded)
                {
                    Services.Output.AddUninitialized(defaultUndefinedSize);
                }
                return Services.State.PassNeeded;
            }
            int operand;
            if (val.IsNumeric)
            {
                operand = val.ToInt();
            }
            else if (val.IsString || val.DotNetType == TypeCode.Char)
            {
                operand = Services.Encoding.GetEncodedValue(val.ToString(true));
            }
            else
            {
                Services.Log.LogEntry(expr, Errors.TypeMismatchError);
                return false;
            }
            if (bitwidth != null)
            {
                var bwSize = CalculateBitwidth(expr, operand, bitwidth);
                if (bwSize < 0)
                {
                    return false;
                }
                mode |= bwSize;
            }
            else
            {
                if (SupportsDirectPage && operand >= 0 && operand <= 0xffff && (operand & 0xff00) == Page)
                {
                    operand &= 0xff;
                }
                var modeSizeFlag = (ZeroPage << operand.Size()) - 1;
                mode = (mode & ~Long) | modeSizeFlag;
                while (!IsValid(mnemType, mode))
                {
                    modeSizeFlag <<= 1;
                    if (modeSizeFlag < 8)
                    {
                        mode |= modeSizeFlag;
                    }
                    else
                    {
                        break;
                    }
                }
                if (modeSizeFlag == ZeroPage &&
                    Services.Options.WarnAboutAmbiguousZeroPageOperands)
                {
                    var absMode = (mode & ~Long) | AbsoluteFlag;
                    var longMode = mode | LongFlag;
                    if (IsValid(mnemType, absMode) || IsValid(mnemType, longMode))
                    {
                        Services.Log.LogEntry(expr, "Addressing mode of expression is ambiguous.", false);
                    }
                }
            }
            if (IsValid(mnemType, mode))
            {
                var opc = Get(mnemType, mode);
                var operandSize = opc.size - opc.code.Size();
                Services.Output.Add(opc.code, opc.code.Size());
                Services.Output.Add(operand, operandSize);
                operand = operandSize switch
                {
                    1 => operand & 0xff,
                    2 => operand & 0xffff,
                    _ => operand & 0xffffff
                };
                if (mnemType == Sixty502DotNetParser.JMP &&
                    (mode & Indirect) == Indirect &&
                    Services.Options.WarnAboutJumpBug && Is6502() &&
                    (operand & 0xFF) == 0xFF &&
                    !Services.State.PassNeeded)
                {
                    Services.Log.LogEntry(expr, "Possible JMP bug detected in expression.", false);
                }
                var format = s_disassemblyFormats[mode];
                var disassembly = $"{mnemonic.Text.ToLower()} {string.Format(format, operand)}";
                BlockVisitor.GenLineListing(Services, disassembly);
                return true;
            }
            if (Services.State.PassNeeded)
            {
                Services.Output.AddUninitialized(defaultUndefinedSize);
                return true;
            }
            return false;
        }

        public override bool GenCpuStatement(Sixty502DotNetParser.CpuStatContext context)
        {
            var mnemonic = context.Start;
            // implStat: mnemonic ;
            if (context.implStat() != null)
            {
                if (IsValid(mnemonic.Type, Implied))
                {
                    var opc = Get(mnemonic.Type, Implied).code;
                    Services.Output.Add(opc, opc.Size());
                    if (Services.CPU?.StartsWith("m680") == true)
                    {
                        if (Services.CPU.EndsWith('9'))
                        {
                            CheckRedundantCallReturn(context, 0x9d, 2, 0x39); // zp jsr/rts
                        }
                        CheckRedundantCallReturn(context, 0xbd, 3, 0x39); // abs jsr/rts
                    }
                    BlockVisitor.GenLineListing(Services, context.Start.Text.ToLower());
                    return true;
                }
                return false;
            }
            // zpAbsStat: mnemonic bitwidth_modifier? expr ;
            if (context.zpAbsStat() != null)
            {
                var size = mnemonic.Type == Sixty502DotNetParser.JML || mnemonic.Type == Sixty502DotNetParser.JSL ? 4 : 3;
                return GenOperand(mnemonic, ZeroPage, context.zpAbsStat().expr(), size, context.zpAbsStat().bitwidth());
            }
            return false;
        }

        /// <summary>
        /// Get or set the flag indicating the current mode of the assembler
        /// supports converting absolute mode operations into direct page if the
        /// current page matches the page set with the <c>.dp</c> directive.
        /// </summary>
        public bool SupportsDirectPage { get; set; }
    }
}
