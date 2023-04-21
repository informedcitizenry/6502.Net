//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Text;
using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an encoder for the Intel i8080 and Zilog z80 microprocessors.
/// </summary>
public sealed partial class Z80InstructionEncoder : CpuEncoderBase
{
    private Dictionary<int, Dictionary<int, Instruction>> _currentIS;
    private Dictionary<int, Instruction> _opcodes;
    private bool _i8080;

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
        _currentIS = _i8080 ? s_i8080 : s_z80;
        _opcodes = _i8080 ? s_i8080allOpcodes : s_z80AllOpcodes;
    }

    private static bool IsPrefix(byte b) => b == 0xcb || b == 0xed || IsIXPrefix(b);

    private static bool IsIXPrefix(byte b) => b == 0xdd || b == 0xfd;

    private int GetIndexDisplacement(SyntaxParser.Z80IndexContext z80Index)
    {
        int displ = Services.Evaluator.SafeEvalNumber(z80Index.expr(), sbyte.MinValue, byte.MaxValue, 0);
        if (z80Index.Hyphen() != null)
        {
            displ = -displ;
        }
        if (displ < sbyte.MinValue)
        {
            throw new Error(z80Index, "Displacement value out of range");
        }
        return displ;
    }

    private Instruction? GetInstruction(int mode, int mnemonic)
    {
        if (_currentIS.TryGetValue(mode, out var lookup) &&
            lookup.TryGetValue(mnemonic, out Instruction? instruction))
        {
            return instruction;
        }
        return null;
    }

    private static int GetRegisterMode(SyntaxParser.RegisterContext registerContext, bool secondPosition = false, bool indirect = false)
    {
        if (!s_firstPositionRegToMode.TryGetValue(registerContext.Start.Type, out int mode))
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

    private bool VisitSingleValue(SyntaxParser.MnemonicContext mnemonicContext, SyntaxParser.CpuInstructionContext instructionContext, SyntaxParser.ExprContext expression)
    {
        int mnemonic = mnemonicContext.Start.Type;
        if (mnemonic.IsOneOf(SyntaxParser.IM, SyntaxParser.RST))
        {
            var typos = expression.GetType();
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
                int rst = Evaluator.EvalIntegerLiteral(primary.primaryExpr(), 0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38);
                opcode = 0xc7 + rst;
            }
            instructionContext.opcode = opcode;
            instructionContext.opcodeSize = opcodeSize;
            return true;
        }
        int mode = Z80Modes.N80;
        int val = Services.Evaluator.SafeEvalNumber(expression, short.MinValue, ushort.MaxValue, 0);
        int valSize = val.Size();
        if (valSize == 2)
        {
            mode = Z80Modes.N160;
        }
        Instruction? code = GetInstruction(mode, mnemonic);
        if (code != null)
        {
            if (code.IsRelative)
            {
                val = Services.State.Output.GetRelativeOffset(val, 2);
                if (!Services.State.PassNeeded && (val < sbyte.MinValue || val > sbyte.MaxValue))
                {
                    throw new Error(expression, "Relative offset too far");
                }
                valSize = 1;
            }
            instructionContext.opcode = code.Opcode;
            instructionContext.opcodeSize = code.Opcode.Size();
            instructionContext.operand = val;
            instructionContext.operandSize = code.Size - instructionContext.opcodeSize;
            return true;
        }
        return false;
    }

    protected override (string, int) OnDecode(byte[] bytes, bool _, int offset, int programCounter)
    {
        byte b = bytes[offset];
        int opcode = b;
        int operandIx = 1;
        if (IsPrefix(b) && Cpuid.StartsWith('z') && bytes.Length + offset > 1)
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
        if (_opcodes.TryGetValue(opcode, out Instruction? instruction))
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
        int displ = 6;
        if (hasReg)
        {
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
        int mnemonic = context.Start.Type;
        baseCode = mnemonic switch
        {
            SyntaxParser.BIT => (0x40 + baseCode) * 0x100,
            SyntaxParser.RES => (0x80 + baseCode) * 0x100,
            SyntaxParser.SET => (0xc0 + baseCode) * 0x100,
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
        return VisitSingleValue(context.mnemonic(), context, context.exprList().expr()[0]);
    }

    public override bool VisitCpuInstructionImplied([NotNull] SyntaxParser.CpuInstructionImpliedContext context)
    {
        if (_currentIS[Z80Modes.Implied].TryGetValue(context.Start.Type, out Instruction? opcode))
        {
            context.opcode = opcode.Opcode;
            context.opcodeSize = opcode.Size;
            return true;
        }
        return false;
    }

    private bool VisitExprSecond(SyntaxParser.MnemonicContext mnemonic,
                                  SyntaxParser.CpuInstructionContext instructionCtx,
                                  SyntaxParser.RegisterContext reg,
                                  SyntaxParser.ExprContext expr,
                                  bool indirect)
    {
        int mnemonicType = mnemonic.Start.Type;
        int regMode = GetRegisterMode(reg);
        if (s_R8s.Contains(reg.Start.Type) &&
            !s_Z80ConditionalJumps.Contains(mnemonicType) &&
            (!indirect || mnemonicType.IsOneOf(SyntaxParser.IN, SyntaxParser.OUT)))
        {
            // ld r8,n
            regMode |= Z80Modes.N81;
        }
        else
        {
            regMode |= Z80Modes.N161;
        }
        if (indirect)
        {
            regMode |= Z80Modes.Ind1Flag;
        }

        Instruction? instruction = GetInstruction(regMode, mnemonicType);
        if (instruction != null)
        {
            int val;
            int valSize = 2;
            double minValue = short.MinValue, maxValue = ushort.MaxValue;
            if (instruction.Size - instruction.Opcode.Size() == 1 && !instruction.IsRelative)
            {
                minValue = sbyte.MinValue;
                maxValue = byte.MaxValue;
                valSize = 1;
            }
            val = Services.Evaluator.SafeEvalNumber(expr, minValue, maxValue);
            if (instruction.IsRelative)
            {
                valSize = 1;
                val = Services.State.Output.GetRelativeOffset(val, 2);
            }
            instructionCtx.opcode = instruction.Opcode;
            instructionCtx.opcodeSize = instruction.Size - valSize;
            instructionCtx.operand = val;
            instructionCtx.operandSize = valSize;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionIndirectExpressionSecond([NotNull] SyntaxParser.CpuInstructionIndirectExpressionSecondContext context)
    {
        SyntaxParser.RegisterContext reg = context.register();
        SyntaxParser.ExprContext expr = context.expr();
        return VisitExprSecond(context.mnemonic(), context, reg, expr, true);
    }

    public override bool VisitCpuInstructionZ80Immediate([NotNull] SyntaxParser.CpuInstructionZ80ImmediateContext context)
    {
        int regType = context.register().Start.Type;
        double minValue = short.MinValue, maxValue = ushort.MaxValue;
        int operandSize = 2;
        int exprMode = Z80Modes.N161;
        if (s_R8s.Contains(regType))
        {
            operandSize = 1;
            exprMode = Z80Modes.N81;
        }
        int operand = Services.Evaluator.SafeEvalNumber(context.expr(), minValue, maxValue, 0);
        int regMode = GetRegisterMode(context.register(), false);
        Instruction? instruction = GetInstruction(exprMode | regMode, context.Start.Type); ;
        if (instruction == null)
        {
            return false;
        }
        if (instruction.IsRelative)
        {
            operandSize = 1;
            operand = Services.State.Output.GetRelativeOffset(operand, instruction.Size);
            if (!Services.State.PassNeeded && (operand < sbyte.MinValue || operand > sbyte.MaxValue))
            {
                throw new Error(context, "Relative offset too far");
            }
        }
        context.opcode = instruction.Opcode;
        context.opcodeSize = instruction.Size - operandSize;
        context.operand = operand;
        context.operandSize = operandSize;
        return true;
    }

    public override bool VisitCpuInstructionZ80IndirectIndexed([NotNull] SyntaxParser.CpuInstructionZ80IndirectIndexedContext context)
    {
        double minValue = short.MinValue, maxValue = ushort.MaxValue;
        int operandSize = 2;
        int exprMode = Z80Modes.IndN160;
        if (s_R8s.Contains(context.register().Start.Type) && context.mnemonic().Start.Type.IsOneOf(SyntaxParser.IN, SyntaxParser.OUT))
        {
            minValue = sbyte.MinValue;
            maxValue = byte.MaxValue;
            operandSize--;
            exprMode = Z80Modes.IndN80;
        }
        int operand = Services.Evaluator.SafeEvalNumber(context.expr(), minValue, maxValue, 0);
        int regMode = GetRegisterMode(context.register(), true);

        Instruction? instruction = GetInstruction(exprMode | regMode, context.Start.Type); ;
        if (instruction == null)
        {
            return false;
        }
        context.opcode = instruction.Opcode;
        context.opcodeSize = instruction.Size - operandSize;
        context.operand = operand;
        context.operandSize = operandSize;
        return true;
    }

    public override bool VisitCpuInstructionIndirectRegisterFirst([NotNull] SyntaxParser.CpuInstructionIndirectRegisterFirstContext context)
    {
        SyntaxParser.RegisterContext[] regs = context.register();
        int mode = GetRegisterMode(regs[0], false, true);

        int val = int.MinValue;
        int valSize = 0;
        if (regs.Length > 1)
        {
            mode |= GetRegisterMode(regs[1], true);
        }
        else if (context.expr() != null)
        {
            if (mode == Z80Modes.IndC0)
            {
                mode |= Z80Modes.N81;
                SyntaxParser.ExpressionPrimaryContext? e = context.expr() as SyntaxParser.ExpressionPrimaryContext;
                if (e != null)
                {
                    _ = Evaluator.EvalIntegerLiteral(e.primaryExpr(), 0);
                    Instruction? instr = GetInstruction(mode, context.Start.Type);
                    if (instr != null)
                    {
                        context.opcode = instr.Opcode;
                        context.opcodeSize = instr.Size;
                        return true;
                    }
                }
                return false;
            }
            val = Services.Evaluator.SafeEvalNumber(context.expr(), sbyte.MinValue, byte.MaxValue);
            mode |= Z80Modes.N81;
            valSize = 1;
        }
        SyntaxParser.MnemonicContext mnemonicCtx = context.mnemonic();
        Instruction? code = GetInstruction(mode, mnemonicCtx.Start.Type);
        if (code != null)
        {
            context.opcode = code.Opcode;
            context.operand = val;
            context.operandSize = valSize;
            context.opcodeSize = code.Size - context.operandSize;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionIndirectRegisterSecond([NotNull] SyntaxParser.CpuInstructionIndirectRegisterSecondContext context)
    {
        int mode = GetRegisterMode(context.ind, true, true);
        mode |= GetRegisterMode(context.register()[0]);
        Instruction? code = GetInstruction(mode, context.Start.Type);
        if (code != null)
        {
            context.opcode = code.Opcode;
            context.opcodeSize = code.Size;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionRegisterList([NotNull] SyntaxParser.CpuInstructionRegisterListContext context)
    {
        SyntaxParser.RegisterContext[] regs = context.register();
        int mode = GetRegisterMode(regs[0]);
        if (regs.Length > 1)
        {
            if (regs.Length > 2)
            {
                return false;
            }
            mode |= GetRegisterMode(regs[1], true);
        }
        Instruction? opcode = GetInstruction(mode, context.Start.Type);
        if (opcode != null)
        {
            context.opcode = opcode.Opcode;
            context.opcodeSize = opcode.Size;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionZ80Index([NotNull] SyntaxParser.CpuInstructionZ80IndexContext context)
    {
        int mode;
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
        Instruction? opcode = GetInstruction(mode, context.Start.Type);
        if (opcode != null)
        {
            int displ = GetIndexDisplacement(context.z80Index());
            displ *= 0x10000;
            displ |= opcode.Opcode & 0xffff;
            int opcodeSize = opcode.Size;
            if (val > int.MinValue)
            {
                context.operand = val;
                context.operandSize = 1;
                opcodeSize--;
            }
            else if (opcode.Size > 3)
            {
                displ |= ((opcode.Opcode & 0xff0000) * 256);
            }
            context.opcode = displ;
            context.opcodeSize = opcodeSize;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionZPAbsolute([NotNull] SyntaxParser.CpuInstructionZPAbsoluteContext context)
    {
        if (VisitSingleValue(context.mnemonic(), context, context.expr()))
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

    public override bool HandleDirective(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        return false;
    }

    protected override void OnReset()
    {

    }

    protected override void OnSetCpu(string cpuid)
    {
        _i8080 = cpuid.StartsWith('i') || cpuid.StartsWith('I');
        _currentIS = _i8080 ? s_i8080 : s_z80;
        _opcodes = _i8080 ? s_i8080allOpcodes : s_z80AllOpcodes;
    }

    public override void Analyze(IList<CodeAnalysisContext> contexts)
    {
        for (int i = 0; i < contexts.Count; i++)
        {
            if (i < contexts.Count - 1)
            {
                AnalyzeCallReturn(contexts[i], contexts[i + 1], 0xcd, 0xc9);
            }
        }
    }
}
