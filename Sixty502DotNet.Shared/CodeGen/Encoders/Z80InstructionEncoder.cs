//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an encoder for the Intel i8080 and Zilog z80 microprocessors.
/// </summary>
public sealed partial class Z80InstructionEncoder : CpuEncoderBase
{
    private Dictionary<ulong, int> _opcodes;
    private Dictionary<int, Instruction> _disassembly;

    /// <summary>
    /// Construct a new instance of the <see cref="Z80InstructionEncoder"/>
    /// class.
    /// </summary>
    /// <param name="cpuid">The initial cpu to set the encoder.</param>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembly runtime.</param>
    public Z80InstructionEncoder(string cpuid, AssemblyServices services)
        : base(cpuid, services)
    {
        _opcodes = null!;
        _disassembly = null!;
    }

    private static bool IsPrefix(byte b) => b == 0xcb || b == 0xed || IsIXPrefix(b);

    private static bool IsIXPrefix(byte b) => b == 0xdd || b == 0xfd;

    private static bool IsGB80IndexC(SyntaxParser.ExpressionAdditiveContext addition)
    {
        return addition.rhs is SyntaxParser.ExpressionSimpleIdentifierContext &&
                addition.rhs.Start.Type == SyntaxParser.C;
    }

    private bool IsGB80IndexOffset(SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext expr, out SyntaxParser.ExpressionAdditiveContext? additiveContext)
    {
        if (Cpuid[0] == 'g' && 
            context.Start.Type == SyntaxParser.LD &&
            expr is SyntaxParser.ExpressionAdditiveContext addition &&
            addition.Plus() != null)
        {
            additiveContext = addition;
            try
            {
                return Services.Evaluator.SafeEvalAddress(addition.lhs) == 0xff00 || Services.State.PassNeeded;
            }
            catch
            {
                return false;
            }
        }
        additiveContext = null;
        return false;
    }

    private int GetIndexDisplacement(SyntaxParser.Z80IndexContext z80Index)
    {
        int displ = Services.Evaluator.SafeEvalNumber(z80Index.expr(), sbyte.MinValue, byte.MaxValue, 0);
        if (z80Index.Hyphen() != null)
        {
            displ = -displ;
        }
        if (displ < sbyte.MinValue)
        {
            throw new Error(z80Index.expr(), "Displacement value out of range");
        }
        return displ;
    }

    private int GetOpcode(int mnemonic, uint mode)
    {
        if (_opcodes.TryGetValue((ulong)mnemonic << 32 | mode, out int opcode))
        {
            return opcode;
        }
        return Bad;
    }

    private static uint GetRegisterMode(SyntaxParser.RegisterContext registerContext, bool secondPosition = false, bool indirect = false)
    {
        if (!s_firstPositionRegToMode.TryGetValue(registerContext.Start.Type, out uint mode))
        {
            throw new Error(registerContext, "Invalid register");
        }
        if (indirect)
        {
            mode |= Z80Modes.Ind0Flag;
        }
        if (secondPosition)
        {
            return mode << 8;
        }
        return mode;
    }

    private bool EmitSingleOperand(SyntaxParser.MnemonicContext mnemonicContext, SyntaxParser.CpuInstructionContext instructionContext, SyntaxParser.ExprContext expression)
    {
        int mnemonic = mnemonicContext.Start.Type;
        if (mnemonic.IsOneOf(SyntaxParser.IM, SyntaxParser.RST))
        {
            int opcodeSize = 1;
            if (expression is not SyntaxParser.ExpressionPrimaryContext primary)
            {
                throw new Error(expression, "Invalid operand");
            }
            int opcode;
            if (mnemonic == SyntaxParser.IM)
            {
                int imValue = Evaluator.EvalIntegerLiteral(primary.primaryExpr(), "Illegal quantity", 0, 3);
                opcode = 0xed | imValue switch
                {
                    0 => 0x4600,
                    1 => 0x5600,
                    _ => 0x5e00
                };
                opcodeSize++;
            }
            else
            {
                int rst;
                if (Cpuid[0] == 'i')
                {
                    rst = Evaluator.EvalIntegerLiteral(primary.primaryExpr(), "3-bit constant expected", 0, 8);
                    rst *= 8;
                }
                else
                {
                    rst = Evaluator.EvalIntegerLiteral(primary.primaryExpr(), 0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38);
                }
                opcode = 0xc7 + rst;
            }
            instructionContext.opcode = opcode;
            instructionContext.opcodeSize = opcodeSize;
            return true;
        }
        uint mode = Z80Modes.N160;
        int operandSize = 2;
        int code = GetOpcode(mnemonic, mode);
        if (mnemonic.IsOneOf(SyntaxParser.DJNZ, SyntaxParser.JR))
        {
            return EmitRelative(code, instructionContext, expression, 1);
        }
        if (code == Bad)
        {
            code = GetOpcode(mnemonic, Z80Modes.N80);
            operandSize = 1;
        }
        return EmitOpcode(code, instructionContext, expression, operandSize);
    }

    protected override (string, int) OnDecode(byte[] bytes, bool _, int offset, int programCounter)
    {
        byte b = bytes[offset];
        int opcode = b;
        int operandIx = 1;
        if (IsPrefix(b) && bytes.Length + offset > 1)
        {
            b = bytes[offset + 1];
            if (bytes[offset] != 0xcb)
            {
                opcode |= b * 256;
                if (IsIXPrefix(bytes[offset]) && bytes.Length + offset > 2 && b >= 0x30)
                {
                    byte displ = bytes[offset + 2];
                    if (bytes[offset + 1] == 0xcb && bytes.Length + offset > 3)
                    {
                        opcode |= bytes[offset + 3] * 0x10000;
                    }
                    if (bytes.Length + offset >= 4)
                    {
                        return (string.Format(s_z80AllOpcodes[opcode].DisassemblyFormat, displ, bytes[offset + 3]), 4);
                    }
                    return (string.Format(s_z80AllOpcodes[opcode].DisassemblyFormat, displ), 3);
                }
                operandIx++;
            }
            else
            {
                opcode |= b * 0x100;
            }
        }
        if (_disassembly.TryGetValue(opcode, out Instruction? instruction))
        {
            int operand = 0;
            if (bytes.Length + offset - opcode.Size() > 0)
            {
                byte[] operandBytes = bytes.Skip(offset + operandIx).ToArray();
                for (int i = 0; i < operandBytes.Length; i++)
                {
                    operand |= operandBytes[i] << i * 8;
                }
                if (instruction.IsRelative)
                {
                    operand = CodeOutput.GetEffectiveAddress(programCounter + 2, operand, sbyte.MaxValue)
                                                   .AsPositive();
                }
            }
            return (string.Format(instruction.DisassemblyFormat, operand), instruction.Size);
        }
        return (string.Empty, 0);
    }

    public override bool VisitCpuInstructionBit([NotNull] SyntaxParser.CpuInstructionBitContext context)
    {
        bool isIx = context.z80Index() != null;
        bool hasReg = context.register() != null;
        if (!isIx && !hasReg)
        {
            return false;
        }
        int mnemonic = context.Start.Type;
        int displ = 6;
        if (hasReg)
        {
            if (isIx && mnemonic == SyntaxParser.BITZ)
            {
                return false;
            }
            int reg = context.register().Start.Type;
            if (reg == SyntaxParser.HL)
            {
                if (context.LeftParen() == null || isIx)
                {
                    return false;
                }
            }
            else
            {
                displ = reg switch
                {
                    SyntaxParser.A => 7,
                    SyntaxParser.B => 0,
                    SyntaxParser.C => 1,
                    SyntaxParser.D => 2,
                    SyntaxParser.E => 3,
                    SyntaxParser.H => 4,
                    SyntaxParser.L => 5,
                    _              => throw new Error(context.Start, "Addressing mode not supported")
                };
            }
        }
        int baseCode = (Evaluator.EvalIntegerLiteral(context.DecLiteral(), "Valid bit constant [0-7] expected", 0, 8) * 8) + displ;
        baseCode = mnemonic switch
        {
            SyntaxParser.BITZ => (0x40 + baseCode) * 0x100,
            SyntaxParser.RES  => (0x80 + baseCode) * 0x100,
            SyntaxParser.SET  => (0xc0 + baseCode) * 0x100,
            _                => throw new Error(context.Start, "Addressing mode not supported")
        };
        if (isIx)
        {
            displ = GetIndexDisplacement(context.z80Index());
            baseCode <<= 16;
            baseCode |= 0x10000 * displ.AsPositive();
            baseCode |= 0xcb00;
            baseCode |= context.z80Index().IX() != null ? 0xdd : 0xfd;
        }
        else
        {
            baseCode |= 0xcb;
        }
        context.opcode = baseCode;
        context.opcodeSize = baseCode.Size();
        return true;
    }

    public override bool VisitCpuInstructionExpressionList([NotNull] SyntaxParser.CpuInstructionExpressionListContext context)
    {
        if (context.exprList().expr().Length > 1)
        {
            return false;
        }
        return EmitSingleOperand(context.mnemonic(), context, context.exprList().expr()[0]);
    }

    public override bool VisitCpuInstructionImplied([NotNull] SyntaxParser.CpuInstructionImpliedContext context)
    {
        int opcodeHex = GetOpcode(context.Start.Type, Z80Modes.Implied);
        if (context.Start.Type == SyntaxParser.STOP)
        {
            context.opcode = opcodeHex;
            context.opcodeSize = 2;
            return true;
        }
        return EmitOpcode(opcodeHex, context);
    }

    public override bool VisitCpuInstructionIndirectExpressionSecond([NotNull] SyntaxParser.CpuInstructionIndirectExpressionSecondContext context)
    {
        SyntaxParser.RegisterContext reg = context.register();
        SyntaxParser.ExprContext expr = context.expr();
        int mnemonicType = context.Start.Type;
        uint regMode = GetRegisterMode(reg);

        if (IsGB80IndexOffset(context, expr, out var addition))
        {
            if (IsGB80IndexC(addition!))
            {
                // ld a,($ff00+c)
                return EmitOpcode(0xf2, context);
            }
            // ld a,($ff00+n)
            return EmitOpcode(0xf0, context, addition!.rhs, 1);
        }
        int operandSize = 2;
        if (s_R8s.Contains(reg.Start.Type) &&
            mnemonicType == SyntaxParser.IN)
        {
            // in r8,(n)
            regMode |= Z80Modes.IndN81;
            operandSize = 1;
        }
        else
        {
            regMode |= Z80Modes.IndN161;
        }
        return EmitOpcode(GetOpcode(mnemonicType, regMode), context, expr, operandSize);
    }

    public override bool VisitCpuInstructionZ80Immediate([NotNull] SyntaxParser.CpuInstructionZ80ImmediateContext context)
    {
        int regType = context.register().Start.Type;
        int operandSize = 2;
        uint exprMode = Z80Modes.N161;
        bool signed8 = Cpuid[0] == 'g' && context.Start.Type == SyntaxParser.ADD && regType == SyntaxParser.SP;
        bool is16BitLxi = context.Start.Type == SyntaxParser.LXI && regType.IsOneOf(SyntaxParser.B, SyntaxParser.D, SyntaxParser.H);
        if ((s_R8s.Contains(regType) && !is16BitLxi && !s_Z80ConditionalJumps.Contains(context.Start.Type)) || signed8)
        {
            operandSize = 1;
            exprMode = Z80Modes.N81;
        }
        uint regMode = GetRegisterMode(context.register(), false);
        if (is16BitLxi)
        {
            regMode = regMode switch
            {
                Z80Modes.B0 => Z80Modes.BC0,
                Z80Modes.D0 => Z80Modes.DE0,
                _           => Z80Modes.HL0
            };
        }
        int code = GetOpcode(context.Start.Type, exprMode | regMode);
        if (context.Start.Type.IsOneOf(SyntaxParser.DJNZ, SyntaxParser.JR))
        {
            return EmitRelative(code, context, context.expr(), 1);
        }
        if (signed8)
        {
            return EmitOpcode(code, context, context.expr(), true);
        }
        return EmitOpcode(code, context, context.expr(), operandSize);
    }

    public override bool VisitCpuInstructionZ80IndirectIndexed([NotNull] SyntaxParser.CpuInstructionZ80IndirectIndexedContext context)
    {
        var expr = context.expr();
        if (IsGB80IndexOffset(context, expr, out var addition))
        {
            if (IsGB80IndexC(addition!))
            {
                // ld ($ff00+c),a
                return EmitOpcode(0xe2, context);
            }
            // ld ($ff00+n),a
            return EmitOpcode(0xe0, context, addition!.rhs, 1);
        }
        int operandSize = 2;
        uint exprMode = Z80Modes.IndN160;
        if (s_R8s.Contains(context.register().Start.Type) && context.mnemonic().Start.Type.IsOneOf(SyntaxParser.IN, SyntaxParser.OUT))
        {
            operandSize--;
            exprMode = Z80Modes.IndN80;
        }
        uint regMode = GetRegisterMode(context.register(), true);
        return EmitOpcode(GetOpcode(context.Start.Type, exprMode | regMode), context, context.expr(), operandSize);
    }

    public override bool VisitCpuInstructionIndirectRegisterFirst([NotNull] SyntaxParser.CpuInstructionIndirectRegisterFirstContext context)
    {
        SyntaxParser.RegisterContext[] regs = context.register();
        uint mode = GetRegisterMode(regs[0], false, true);
        if (context.expr() == null)
        {
            if (regs.Length > 1)
            {
                mode |= GetRegisterMode(regs[1], true);
            }
            return EmitOpcode(GetOpcode(context.Start.Type, mode), context);
        }
        if (mode == Z80Modes.IndC0)
        {
            mode |= Z80Modes.N81;
            if (context.expr() is SyntaxParser.ExpressionPrimaryContext e)
            {
                _ = Evaluator.EvalIntegerLiteral(e.primaryExpr(), 0);
                return EmitOpcode(GetOpcode(context.Start.Type, mode), context);
            }
            return false;
        }
        return EmitOpcode(GetOpcode(context.Start.Type, mode | Z80Modes.N81), context, context.expr());
    }

    public override bool VisitCpuInstructionIndirectRegisterSecond([NotNull] SyntaxParser.CpuInstructionIndirectRegisterSecondContext context)
    {
        uint mode = GetRegisterMode(context.ind, true, true);
        mode |= GetRegisterMode(context.register()[0]);
        return EmitOpcode(GetOpcode(context.Start.Type, mode), context);
    }

    public override bool VisitCpuInstructionRegisterList([NotNull] SyntaxParser.CpuInstructionRegisterListContext context)
    {
        SyntaxParser.RegisterContext[] regs = context.register();
        uint mode = GetRegisterMode(regs[0]);
        if (regs.Length > 1)
        {
            if (regs.Length > 2)
            {
                return false;
            }
            mode |= GetRegisterMode(regs[1], true);
        }
        int code = GetOpcode(context.Start.Type, mode);
        if (code != Bad)
        {
            context.opcode = code;
            context.opcodeSize = code.Size();
            if (code == 0xcb)
            {
                context.opcodeSize++;
            }
            return true;
        }
        if (regs.Length > 1 && RegisterIsSymbol(regs[1]))
        {
            var regType = regs[0].Start.Type;
            int operandSize = 2;
            uint exprMode = Z80Modes.N161;
            if (s_R8s.Contains(regType) ||
                (Cpuid[0] == 'g' && context.Start.Type == SyntaxParser.ADD && regType == SyntaxParser.SP))
            {
                operandSize = 1;
                exprMode = Z80Modes.N81;
            }
            mode = GetRegisterMode(regs[0]);
            code = GetOpcode(context.Start.Type, exprMode | mode);
            return EmitOpcode(code, context, regs[1].ToExpression(context), operandSize);
        }
        return false;
    }

    public override bool VisitCpuInstructionZ80Index([NotNull] SyntaxParser.CpuInstructionZ80IndexContext context)
    {
        uint mode;
        int val = int.MinValue;
        if (context.ix0 != null)
        {
            mode = context.z80Index().IX() != null
                ? Z80Modes.IndIX0Offs : Z80Modes.IndIY0Offs;
            if (context.e1 != null)
            {
                val = Services.Evaluator.SafeEvalNumber(context.expr(), sbyte.MinValue, byte.MaxValue);
                mode |= Z80Modes.N81;
            }
            if (context.r1 != null)
            {
                mode |= GetRegisterMode(context.r1, true);
            }
        }
        else
        {
            mode = context.z80Index().IX() != null
                ? Z80Modes.IndIX1Offs : Z80Modes.IndIY1Offs;
            mode |= GetRegisterMode(context.r0);
        }
        int opcode = GetOpcode(context.Start.Type, mode);
        if (opcode != Bad)
        {
            int displ = GetIndexDisplacement(context.z80Index());
            displ *= 0x10000;
            displ |= opcode & 0xffff;
            if (val > int.MinValue)
            {
                context.operand = val;
                context.operandSize = 1;
            }
            else
            {
                displ |= (opcode & 0xff0000) * 256;
            }
            context.opcode = displ;
            context.opcodeSize = opcode.Size() + 1;
            if (opcode == 0xcbdd || opcode == 0xcbfd)
            {
                context.opcodeSize++;
            }
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionZPAbsolute([NotNull] SyntaxParser.CpuInstructionZPAbsoluteContext context)
    {
        if (EmitSingleOperand(context.mnemonic(), context, context.expr()))
        {
            if (context.bitwidthModifier() != null)
            {
                Services.State.Warnings.Add(
                new Warning(context.bitwidthModifier(),
                            "Bitwidth modifier ignored for this CPU type"));
            }
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionGB80AccIncrement([NotNull] SyntaxParser.CpuInstructionGB80AccIncrementContext context)
    {
        if (Cpuid[0] != 'g')
        {
            return false;
        }
        int opcodeHex;
        if (context.a0 != null)
        {
            opcodeHex = context.inc.Type == SyntaxParser.Plus ? 0x2a : 0x3a;
        }
        else
        {
            opcodeHex = context.inc.Type == SyntaxParser.Plus ? 0x22 : 0x32;
        }
        return EmitOpcode(opcodeHex, context);
    }

    public override bool VisitCpuInstructionGB80StackOffset([NotNull] SyntaxParser.CpuInstructionGB80StackOffsetContext context)
    {
        if (Cpuid[0] != 'g')
        {
            return false;
        }
        return EmitOpcode(0xf8, context, context.expr(), 1);
    }

    public override bool HandleDirective(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        return false;
    }

    protected override void OnReset() { }

    protected override void OnSetCpu(string cpuid)
    {
        switch (cpuid)
        {
            case "i8080":
                _opcodes = s_i8080;
                _disassembly = s_i8080allOpcodes;
                break;
            case "gb80":
                _opcodes = s_gb80;
                _disassembly = s_gb80AllOpcodes;
                break;
            default:
                _opcodes = s_z80;
                _disassembly = s_z80AllOpcodes;
                break;
        }
    }

    public override void Analyze(IList<CodeAnalysisContext> contexts)
    {
        for (int i = 0; i < contexts.Count; i++)
        {
            if (i < contexts.Count - 1)
            {
                AnalyzeCallReturn(contexts[i], contexts[i + 1], 0xcd, 0xc9);
            }
            if (contexts[i].ObjectCode.First() == 0x3e &&
                contexts[i].ObjectCode.ToArray()[1] == 0x00)
            {
                contexts[i].Report = "Use 'xor a,a' to reset the 'a' register";
            }
            else if (contexts[i].ObjectCode.First() == 0xfe && 
                     contexts[i].ObjectCode.ToArray()[1] == 0x00)
            {
                contexts[i].Report = "Use 'and a,a' or 'or a,a' here";
            }
        }
    }
}
