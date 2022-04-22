//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class responsible for assembly of m680x source.
    /// </summary>
    public partial class M680x : MotorolaBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="M680x"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="initialArchitecture">The initial CPU/architecture
        /// to initialize the instruction set mnemonics.</param>
        public M680x(AssemblyServices services, string initialArchitecture)
            : base(services, initialArchitecture)
        {
            Set = initialArchitecture.EndsWith('9') ?
                new Dictionary<Instruction, Opcode>(s_m6809) :
                new Dictionary<Instruction, Opcode>(s_m6800);
            _exchangeModes = new Dictionary<string, byte>(Services.StringComparer)
            {
                { "d",   0b0000 },
                { "x",   0b0001 },
                { "y",   0b0010 },
                { "u",   0b0011 },
                { "s",   0b0100 },
                { "pc",  0b0101 },
                { "a",   0b1000 },
                { "b",   0b1001 },
                { "cc",  0b1010 },
                { "dp",  0b1011 }
            };

            _pushPullModes = new Dictionary<string, byte>(Services.StringComparer)
            {
                { "pc", 0b1000_0000 },
                { "s",  0b0100_0000 },
                { "u",  0b0100_0000 },
                { "y",  0b0010_0000 },
                { "x",  0b0001_0000 },
                { "dp", 0b0000_1000 },
                { "d",  0b0000_0110 },
                { "b",  0b0000_0100 },
                { "a",  0b0000_0010 },
                { "cc", 0b0000_0001 }
            };
            Services.Output.IsLittleEndian = false;
        }

        protected override IDictionary<string, int> OnGetMnemonics(string architecture)
        {
            if (architecture.EndsWith('9'))
            {
                SupportsDirectPage = true;
                Set = new Dictionary<Instruction, Opcode>(s_m6809);
                return new Dictionary<string, int>(s_m6809Mnemonics, Services.StringComparer);
            }
            SupportsDirectPage = false;
            Set = new Dictionary<Instruction, Opcode>(s_m6800);
            return new Dictionary<string, int>(s_m6800Mnemonics, Services.StringComparer);
        }

        private static bool IsPC(IToken? indexReg)
            => indexReg?.Type == Sixty502DotNetParser.PC || indexReg?.Type == Sixty502DotNetParser.PCR;
        // 6809 Post-Byte Bits for Indexed and Indirect Addressing:
        // +---------------------------------------------------------------+
        // |          Bit Number           |                               |
        // +---+---+---+---+---+---+---+---+       Addressing Mode         |
        // | 7 | 6 | 5 | 4 | 3 | 2 | 1 | 0 |                               |
        // +-------------------------------+-------------------------------+
        // | 0 | R | R | x | x | x | x | x | 5-Bit Offset                  |
        // | 1 | R | R | 0 | 0 | 0 | 0 | 0 | Autoimcrement by One          |
        // | 1 | R | R | I | 0 | 0 | 0 | 1 | Autoincrement by Two          |
        // | 1 | R | R | 0 | 0 | 0 | 1 | 0 | Autodecrement by One          |
        // | 1 | R | R | I | 0 | 0 | 1 | 1 | Autodecrement by Two          |
        // | 1 | R | R | I | 0 | 1 | 0 | 0 | Zero Offset                   |
        // | 1 | R | R | I | 0 | 1 | 0 | 1 | Accumulator B Offset          |
        // | 1 | R | R | I | 0 | 1 | 1 | 0 | Accumulator A Offset          |
        // | 1 | R | R | I | 1 | 0 | 0 | 0 | 8-Bit Offset                  |
        // | 1 | R | R | I | 1 | 0 | 0 | 1 | 16-Bit Offset                 |
        // | 1 | R | R | I | 1 | 0 | 1 | 1 | Accumulator D Offset          |
        // | 1 | x | x | I | 1 | 1 | 0 | 0 | Program Counter 8-Bit Offset  |
        // | 1 | x | x | I | 1 | 1 | 0 | 1 | Program Counter 16-Bit Offset |
        // | 1 | x | x | 1 | 1 | 1 | 1 | 1 | Extended Indirect             |
        // +---+---+---+---+---+---+---+---+-------------------------------+
        // |                                                               |
        // | Indirect Bit (bit 4):                                         |
        // |  I = 1 for Indirect, I = 0 for direct                         |
        // |                                                               |  
        // | Register Bits (bits 5 and 6):                                 |
        // |  00 R = X                                                     |
        // |  01 R = Y                                                     |
        // |  10 R = U                                                     |
        // |  11 R = S                                                     |
        // |                                                               |
        // +---------------------------------------------------------------+
        private bool Gen6809Indexed(IToken mnemonic,
                                    bool indirect,
                                    IToken? lhsIndex,
                                    Sixty502DotNetParser.BitwidthContext? bitwidth,
                                    Sixty502DotNetParser.ExprContext? expression,
                                    IToken? rhsIndex,
                                    Sixty502DotNetParser.AnonymousLabelContext? incDecCtx)
        {
            if (!IsValid(mnemonic.Type, IndexedX))
            {
                return false;
            }
            var opcode = Get(mnemonic.Type, IndexedX);
            int indexMode = 0;
            bool offset5Bit = false;
            double operand = double.NaN;
            int operandSize = 0;
            var disassembly = new StringBuilder($"{mnemonic.Text.ToLower()} ");
            if (indirect)
            {
                disassembly.Append('[');
            }
            if (incDecCtx != null)
            {
                var incDec = incDecCtx.Start.Text;
                if (incDec.Length > 2)
                {
                    Services.Log.LogEntry(incDecCtx, "Invalid expression.");
                    return false;
                }
                disassembly.Append(',');
                indexMode = incDec.Length - 1;
                if (indirect && indexMode == 0)
                {
                    return false;
                }
                if (incDec[0] == '-')
                {
                    disassembly.Append(incDecCtx.GetText());
                    disassembly.Append(rhsIndex!.Text.ToLower());
                    indexMode |= 2;
                }
                else
                {
                    disassembly.Append(rhsIndex!.Text.ToLower());
                    disassembly.Append(incDecCtx.GetText());
                }
            }
            else if (expression != null)
            {
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(expression, short.MinValue, ushort.MaxValue, out operand))
                {
                    operandSize = operand.Size();
                    var dispOperand = operand;
                    if (bitwidth != null)
                    {
                        operandSize = CalculateBitwidth(expression, (int)operand, bitwidth);
                        if (operandSize < 0 || operandSize > 2)
                        {
                            return false;
                        }
                        if (operandSize < 2 && (operand < sbyte.MinValue || operand > sbyte.MaxValue))
                        {
                            if (Services.State.PassNeeded)
                            {
                                Services.Output.AddUninitialized(3);
                                return true;
                            }
                            Services.Log.LogEntry(expression, Errors.IllegalQuantity);
                            return false;
                        }
                    }
                    else if (operand < sbyte.MinValue || operand > sbyte.MaxValue)
                    {
                        operandSize = 2;
                    }
                    else
                    {
                        offset5Bit = !indirect && operand >= -16 && operand <= 15 && !IsPC(rhsIndex);
                    }
                    if (indirect && rhsIndex == null)
                    {
                        indexMode |= 31;
                    }
                    else if (offset5Bit)
                    {
                        indexMode |= (int)operand & 0x1f;
                    }
                    else
                    {
                        indexMode |= 8;
                        if (IsPC(rhsIndex))
                        {
                            indexMode |= 4;
                            var offs = (int)operand;
                            operand = Services.Output.GetRelativeOffset(offs, opcode.size + 1);
                            if ((bitwidth != null && operandSize == 2) || operand > sbyte.MaxValue || operand <= sbyte.MinValue)
                            {
                                if (bitwidth != null && operandSize == 1)
                                {
                                    Services.Log.LogEntry(expression, "Offset too far.");
                                    return false;
                                }
                                operand = Services.Output.GetRelativeOffset(offs, opcode.size + 2);
                                operandSize = 2;
                                if (operand < short.MinValue || operand > short.MaxValue)
                                {
                                    if (!Services.State.PassNeeded)
                                    {
                                        Services.Log.LogEntry(expression, "Offset too far.");
                                        return false;
                                    }
                                    operand = short.MaxValue;
                                }
                                indexMode |= 1;
                            }
                            else
                            {
                                operandSize = 1;
                            }
                        }
                        else if (operandSize == 2)
                        {
                            indexMode++;
                        }
                    }
                    if (IsPC(rhsIndex))
                    {
                        disassembly.Append($"${(int)dispOperand & 0xffff:x4}");
                    }
                    else if ((offset5Bit || operandSize < 2) && operand < 0)
                    {
                        disassembly.Append($"-${(int)Math.Abs(dispOperand) & 0xff:x2}");
                    }
                    else if (operandSize == 1)
                    {
                        disassembly.Append($"${(int)dispOperand:x2}");
                    }
                    else
                    {
                        disassembly.Append($"${(int)dispOperand & 0xffff:x4}");
                    }
                }
                else if (Services.State.PassNeeded)
                {
                    Services.Output.AddUninitialized(3);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                indexMode |= 4;
                if (lhsIndex != null)
                {
                    if (lhsIndex.Type == Sixty502DotNetParser.A)
                    {
                        indexMode |= 2;
                    }
                    else if (lhsIndex.Type == Sixty502DotNetParser.B)
                    {
                        indexMode |= 1;
                    }
                    else if (lhsIndex.Type == Sixty502DotNetParser.D)
                    {
                        indexMode = (indexMode << 1) | 3;
                    }
                    else
                    {
                        Services.Log.LogEntry((Token)lhsIndex, "Accumulator register expected.");
                        return false;
                    }
                    disassembly.Append(lhsIndex.Text.ToLower());
                }
            }
            if (!offset5Bit)
            {
                indexMode |= 128;
            }
            if (rhsIndex != null)
            {
                indexMode |= rhsIndex.Type switch
                {
                    Sixty502DotNetParser.S => 96,
                    Sixty502DotNetParser.U => 64,
                    Sixty502DotNetParser.Y => 32,
                    _ => 0
                };
                if (incDecCtx == null)
                {
                    disassembly.Append($",{rhsIndex.Text.ToLower()}");
                }
            }
            if (indirect)
            {
                indexMode |= 16;
                disassembly.Append(']');
            }
            Services.Output.Add(opcode.code, opcode.code.Size());
            Services.Output.Add(indexMode, 1);
            if (!double.IsNaN(operand) && !offset5Bit)
            {
                Services.Output.Add(operand, operandSize);
            }
            BlockVisitor.GenLineListing(Services, disassembly.ToString());
            return true;
        }

        private bool GenPushPullExchange(IToken mnemonic, string mnemonicText, List<IToken> regs)
        {
            var isExchange = mnemonic.Type == Sixty502DotNetParser.EXG || mnemonic.Type == Sixty502DotNetParser.TFR;
            if (isExchange && regs.Count != 2)
            {
                var msg = regs.Count == 1 ? Errors.ExpectedConstant : Errors.UnexpectedExpression;
                Services.Log.LogEntry((Token)regs[^1], msg);
                return false;
            }
            var lookup = isExchange ? _exchangeModes : _pushPullModes;
            byte registers = byte.MinValue;
            var addedRegisters = new SortedSet<string>(new M6809PushPullComparer(!isExchange ? lookup : null));
            var registersEvaled = new HashSet<int>();
            foreach (var reg in regs)
            {
                if (!lookup.TryGetValue(reg.Text, out var postbyte))
                {
                    Services.Log.LogEntry((Token)reg, "Register not valid in this context.");
                    return false;
                }
                if (registersEvaled.Contains(reg.Type) ||
                    (s_accumulatorRegs.Contains(reg.Type) &&
                    ((reg.Type == Sixty502DotNetParser.D && (registersEvaled.Contains(Sixty502DotNetParser.A) || registersEvaled.Contains(Sixty502DotNetParser.B)))
                    ||
                    ((reg.Type == Sixty502DotNetParser.A || reg.Type == Sixty502DotNetParser.B) && registersEvaled.Contains(Sixty502DotNetParser.D)))))
                {
                    Services.Log.LogEntry((Token)reg, "Duplicate register.");
                    return false;
                }
                if (!isExchange && mnemonicText[^1] == reg.Text[0])
                {
                    Services.Log.LogEntry((Token)reg, $"Cannot use \"{mnemonicText}\" with registers \"{reg.Text}\".");
                    return false;
                }
                if (isExchange && addedRegisters.Count == 1)
                {
                    registers <<= 4;
                }
                registers |= postbyte;
                addedRegisters.Add(reg.Text.ToLower());
                registersEvaled.Add(reg.Type);
            }
            Services.Output.Add(Get(mnemonic.Type, ZeroPage).code, 1);
            Services.Output.Add(registers);
            var disassembly = $"{mnemonic.Text.ToLower()} {string.Join(',', addedRegisters)}";
            BlockVisitor.GenLineListing(Services, disassembly);
            return true;
        }

        private bool GenRelative(Sixty502DotNetParser.RelStatContext context)
        {
            var mode = Relative;
            int mnemonic = context.Start.Type;
            if (s_longBranches.Contains(mnemonic))
            {
                mode |= RelativeAbs;
            }
            if (!IsValid(mnemonic, mode))
            {
                return false;
            }
            var instruction = Get(mnemonic, mode);
            var offAdj = instruction.size;
            var opcSize = instruction.code.Size();
            if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr(), short.MinValue, ushort.MaxValue, out var target))
            { 
                try
                {
                    var relOffs = mode == RelativeAbs
                        ? Convert.ToInt16(Services.Output.GetRelativeOffset((int)target, offAdj))
                        : Convert.ToSByte(Services.Output.GetRelativeOffset((int)target, offAdj));
                    var relSize = offAdj - opcSize;
                    Services.Output.Add(instruction.code, opcSize);
                    Services.Output.Add(relOffs, relSize);
                    var disassembly = $"{context.Start.Text.ToLower()} ${(int)target & 0xffff:x4}";
                    BlockVisitor.GenLineListing(Services, disassembly);
                }
                catch (OverflowException)
                {
                    if (!Services.State.PassNeeded)
                    {
                        var relOffs = Services.Output.GetRelativeOffset((int)target, offAdj);
                        Services.Log.LogEntry(context.expr(), $"Relative branch too far ({Math.Abs(relOffs)} bytes).");
                        return false;
                    }
                    Services.Output.AddUninitialized(instruction.size);
                }
                return true;
            }
            if (Services.State.PassNeeded)
            {
                Services.Output.AddUninitialized(offAdj);
                return true;
            }
            return false;
        }

        public bool GenTfrdp(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            IToken mnemonic = context.Start;
            if (Set.First().Value.cpu.Equals("m6809"))
            {
                if (context.expr() != null)
                {
                    if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr(), byte.MinValue, byte.MaxValue, out var page))
                    {
                        byte opcode;
                        short dpopc;
                        var disSb = new StringBuilder("ld");
                        if (mnemonic.Type == Sixty502DotNetParser.Tfradp)
                        {
                            opcode = 0x86;
                            dpopc = 0x1f8b;
                            disSb.Append($"a #${(int)page & 0xff:x2}\n                         tfr a,dp");
                        }
                        else
                        {
                            opcode = 0xc6;
                            dpopc = 0x1f9b;
                            disSb.Append($"b #${(int)page & 0xff:x2}\n                         tfr b,dp");
                        }
                        Services.Output.Add(opcode);
                        Services.Output.Add(page, 1);
                        Services.Output.Add(dpopc, 2);
                        BlockVisitor.GenLineListing(Services, disSb.ToString());
                        return true;
                    }
                    if (Services.State.PassNeeded)
                    {
                        Services.Output.AddUninitialized(4);
                    }
                    else
                    {
                        Services.Log.LogEntry(context.expr(), Errors.IllegalQuantity);
                    }
                    return true;
                }
                Services.Log.LogEntry(context, Errors.ExpectedExpression);
                return true;
            }
            Services.Log.LogEntry(context.cpuDirective(), "Directive not supported for CPU.");
            return true;
        }

        public override void CpuDirectiveStatement(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            int directive = context.Start.Type;
            if (directive == Sixty502DotNetParser.Dp)
            {
                SetPage(context);
                return;
            }
            if (directive == Sixty502DotNetParser.Tfradp || directive == Sixty502DotNetParser.Tfrbdp)
            {
                GenTfrdp(context);
                return;
            }
            Services.Log.LogEntry(context, "Directive ignored for CPU.", false);
        }

        public override bool GenCpuStatement(Sixty502DotNetParser.CpuStatContext context)
        {
            var mnemonic = context.Start;
            // immStat: mnemonic ('[' expr ']')? '#' expr ;
            if (context.immStat() != null)
            {
                return GenOperand(mnemonic, Immediate, context.immStat().expr(), 2);
            }
            if (context.relStat() != null)
            {
                return GenRelative(context.relStat());
            }
            if (Services.CPU.EndsWith('9'))
            {
                // ixStat: mnemonic ('[' expr ']')? expr ',' ('s'|'u'|'x'|'y') ;
                if (context.ixStat() != null)
                {
                    return Gen6809Indexed(mnemonic, false, null, context.ixStat().bitwidth(), context.ixStat().expr(), context.ixStat().index, null);
                }
                // autoIncIndexedStat: mnemonic ',' ('s'|'u'|'x'|'y') ('+' '+'?)? ;
                if (context.autoIncIndexedStat() != null)
                {
                    var direct = context.autoIncIndexedStat().LeftSquare() != null;
                    var autoInc = context.autoIncIndexedStat().autoIncIndexed();
                    return Gen6809Indexed(mnemonic,
                                          direct,
                                          null,
                                          null,
                                          null,
                                          autoInc.index,
                                          autoInc.anonymousLabel());
                }
                // autoDecIndexedStat: mnemonic ',' '-' '-'? ('s'|'u'|'x'|'y') ;
                if (context.autoDecIndexedStat() != null)
                {
                    var direct = context.autoDecIndexedStat().LeftSquare() != null;
                    var autoDec = context.autoDecIndexedStat().autoDecIndexed();
                    return Gen6809Indexed(mnemonic,
                                           direct,
                                           null,
                                           null,
                                           null,
                                           autoDec.index,
                                           autoDec.anonymousLabel());
                }
                // dirIxStat: mnemonic '[' expr ',' ('pc'|'pcr'|'s'|'u'|'x'|'y') ']' ;
                if (context.dirIxStat() != null)
                {
                    return Gen6809Indexed(mnemonic, true, null, null, context.dirIxStat().expr(), context.dirIxStat().index, null);
                }
                // dirStat: mnemonic '[' expr ']'
                if (context.dirStat() != null)
                {
                    return Gen6809Indexed(mnemonic, true, null, null, context.dirStat().expr(), null, null);
                }
                // regListStat: mnemonic m6809Reg (',' m6809Reg)+ ;
                if (context.regListStat() != null)
                {
                    if (s_pushPullMnemonics.Contains(mnemonic.Type))
                    {
                        var regs = context.regListStat().m6809Reg().Select(ctx => ctx.Start).ToList();
                        return GenPushPullExchange(mnemonic, context.Start.Text, regs);
                    }
                    var rhs = context.regListStat().m6809Reg()[1];
                    if (s_accumulatorRegs.Contains(context.regListStat().lhs.Start.Type) &&
                        context.regListStat().m6809Reg().Length == 2 &&
                        (s_indexRegs.Contains(rhs.Start.Type) || IsPC(rhs.Start)))
                    {
                        return Gen6809Indexed(mnemonic, false, context.regListStat().lhs.Start, null, null, rhs.Start, null);
                    }
                }
                if (context.implStat()?.A() != null || context.regRegStat() != null)
                {
                    var src = context.implStat()?.A().Symbol ?? context.regRegStat().src;
                    if ((context.implStat() != null || s_pushPullRegs.Contains(src.Type))
                        && context.regRegStat()?.LeftSquare() == null
                        && s_pushPullMnemonics.Contains(mnemonic.Type))
                    {
                        // pushPullExchStat
                        //              :   mnemonic ('a'|'b'|'d'|'pc'|'s'|'u'|'x'|'y') ',' ('a'|'b'|'cc'|'d'|'dp'|'pc'|'s'|'u'|'x'|'y')
                        //              |   mnemonic ('a'|'b'|'d'|'pc'|'s'|'u'|'x'|'y')
                        //              ;
                        var regs = new List<IToken> { src };
                        if (context.regRegStat()?.dst != null)
                        {
                            regs.Add(context.regRegStat().dst);
                        }
                        return GenPushPullExchange(mnemonic, context.Start.Text, regs);
                    }
                    if (context.implStat() == null && s_accumulatorRegs.Contains(src.Type) && s_indexRegs.Contains(context.regRegStat().dst?.Type ?? -1))
                    {
                        return Gen6809Indexed(mnemonic,
                                              context.regRegStat().LeftSquare() != null,
                                              context.regRegStat().src,
                                              null,
                                              null,
                                              context.regRegStat().dst,
                                              null);
                    }
                    return false;
                }
            }
            else if (context.ixStat() != null && context.ixStat().X() != null)
            {
                return GenOperand(mnemonic, IndexedX, context.ixStat().expr(), 3, context.ixStat().bitwidth());
            }
            return base.GenCpuStatement(context);
        }
    }
}
