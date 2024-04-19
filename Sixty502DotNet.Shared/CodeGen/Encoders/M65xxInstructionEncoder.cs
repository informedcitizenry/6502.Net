//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an encoder for several 8-bit CPUs in the MOS 6502/WDC family,
/// including the original 6502, successors like the 65C02 and 65816, as well
/// as vendor specific variants like the Hudson C6280, and Commodore 65CE02.
/// </summary>
public sealed partial class M65xxInstructionEncoder : CpuEncoderBase
{
    private readonly bool _enable6502BranchAlways;
    private List<Dictionary<int, Instruction>> _disassembly;
    private bool _auto;
    private bool _a16;
    private bool _x16;

    private Dictionary<int, M6xxOpcode> _opcodes;

    /// <summary>
    /// Construct a new instance of the <see cref="M65xxInstructionEncoder"/>
    /// class.
    /// </summary>
    /// <param name="cpuid">The initial cpu to set the encoder.</param>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembly runtime.</param>
    public M65xxInstructionEncoder(string cpuid, AssemblyServices services)
        : base(cpuid, services)
    {
        _opcodes = null!;
        _enable6502BranchAlways = services.ArchitectureOptions.BranchAlways;
        _disassembly = null!;
        _truncateToDp = false;
        _dp = 0;
    }

    /// <summary>
    /// Construct a new instance of the <see cref="M65xxInstructionEncoder"/>
    /// class.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembly runtime.</param>
    public M65xxInstructionEncoder(AssemblyServices services)
        : this("6502", services)
    {

    }

    private static Instruction? LookupOpcode(int key, List<Dictionary<int, Instruction>> lookups)
    {
        for (int i = lookups.Count - 1; i >= 0; i--)
        {
            if (lookups[i].TryGetValue(key, out Instruction? opcode))
            {
                return opcode;
            }
        }
        return null;
    }

    private bool IsPrefix(int code)
    {
        return (code == 0xea || code == 0x42) &&
                (Cpuid.Equals("45GS02") ||
                Cpuid.Equals("65CE02") ||
                Cpuid.Equals("m65"));
    }

    private Instruction? FetchOpcode(byte[] bytes, int offset)
    {
        int code = bytes[offset];
        if (IsPrefix(code) && bytes.Length > 1)
        {
            code |= bytes[offset + 1] * 256;
            if (bytes[offset + 1] == 0x42 && bytes.Length > 2)
            {
                code |= bytes[offset + 2] * 0x10000;
                if (bytes[offset + 2] == 0xea && bytes.Length > 3)
                {
                    unchecked
                    {
                        code |= bytes[offset + 3] * 0x1000000;
                    }
                }
            }
        }
        return LookupOpcode(code, _disassembly);
    }

    private Instruction? FetchOpcode(byte[] bytes)
    {
        return FetchOpcode(bytes, 0);
    }

    public override void Analyze(IList<CodeAnalysisContext> contexts)
    {
        string previousCpuid = string.Empty;
        for (int i = 0; i < contexts.Count; i++)
        {
            if (!string.IsNullOrEmpty(contexts[i].Report)) continue;
            if (i < contexts.Count - 1)
            {
                AnalyzeCallReturn(contexts[i], contexts[i + 1], 0x20, 0x60);
                if (Cpuid.Equals("65816"))
                {
                    AnalyzeCallReturn(contexts[i], contexts[i + 1], 0x22, 0x40);
                }
            }
            if (Services.DiagnosticOptions.WarnJumpBug &&
                !Services.State.PassNeeded &&
                contexts[i].ObjectCode.First() == 0x6e &&
                contexts[i].Cpuid.StartsWith("6502") &&
                contexts[i].ObjectCode.ToArray()[1] == 0xff)
            {
                contexts[i].Report = "Indirect jump at page boundary has a defect";
            }
        }
        if (!string.IsNullOrEmpty(previousCpuid))
        {
            SetCpu(previousCpuid);
        }
    }

    protected override (string, int) OnDecode(byte[] bytes, bool isSingleInstruction, int offset, int programCounter)
    {
        Instruction? instr = FetchOpcode(bytes.Skip(offset).ToArray());
        if (instr == null)
        {
            return (string.Empty, 0);
        }
        offset += instr.Opcode.Size();
        object[] operandVals = new object[instr.Operands.Length];
        for (int i = 0; i < instr.Operands.Length; i++)
        {
            int operandSize = instr.Operands[i];
            int operandVal = 0;
            for (int b = 0; b < operandSize && offset + b < bytes.Length; b++)
            {
                operandVal |= bytes[offset + b] << (8 * b);
            }
            offset += operandSize;
            operandVals[i] = operandVal;
        }
        if (instr.IsRelative)
        {
            int maxValue = instr.Is16BitRelative ? short.MaxValue : sbyte.MaxValue;
            operandVals[^1] = CodeOutput.GetEffectiveAddress(programCounter + instr.Size, (int)operandVals[^1], maxValue).AsPositive();
            if (bytes.Length >= 5 && isSingleInstruction && bytes[offset] == 0x4c)
            {
                // we are at a pseudo jmp
                int jmp = bytes[offset + 1] + 256 * bytes[offset + 2];
                string pseudo = string.Format(instr.DisassemblyFormat, operandVals[0]);
                return ($"{pseudo}:jmp ${jmp:x4} ", 5);
            }
        }
        if (instr.Opcode == 0 && isSingleInstruction && bytes.Length == 2)
        {
            // brk #$2a
            return (string.Format("brk #${0:x2}", bytes[1]), 2);
        }
        return (string.Format(instr.DisassemblyFormat, operandVals), instr.Size);
    }

    public override bool VisitCpuInstructionBit([NotNull] SyntaxParser.CpuInstructionBitContext context)
    {
        if (context.expr()?.Length < 1)
        {
            return false;
        }
        M6xxOpcode opcode = _opcodes[context.Start.Type];
        SyntaxParser.ExprContext[] operands = context.expr();
        int opcodeHex;
        int bitOpcodeSize;
        if (operands.Length == 1)
        {
            opcodeHex = opcode.bitTest;
            bitOpcodeSize = 1;
        }
        else
        {
            opcodeHex = opcode.bitTestAbs;
            bitOpcodeSize = 2;
        }
        if (opcodeHex == Bad)
        {
            return false;
        }
        int operandValues = 0;

        for (int i = 0; i < operands.Length; i++)
        {
            int operandValue;
            if (i == 1)
            {
                operandValue = Services.Evaluator.SafeEvalAddress(operands[i], _truncateToDp, _dp);
                operandValue = Services.State.Output.GetRelativeOffset(operandValue, bitOpcodeSize + 1);
            }
            else
            {
                operandValue = Services.Evaluator.SafeEvalNumber(operands[i], sbyte.MinValue, byte.MaxValue, _truncateToDp, _dp);
            }
            operandValues += operandValue << (i * 8);
        }
        int bitValue = Evaluator.EvalIntegerLiteral(context.DecLiteral(), "Valid bit constant [0-7] expected", 0, 8) * 0x10;
        context.opcodeSize = 1;
        context.opcode = opcodeHex | bitValue;
        context.operand = operandValues;
        context.operandSize = bitOpcodeSize;
        return true;
    }

    public override bool VisitCpuInstructionDirect([NotNull] SyntaxParser.CpuInstructionDirectContext context)
    {
        M6xxOpcode opcode = _opcodes[context.Start.Type];
        return EmitOpcodeVariant(opcode.direct, opcode.directAbs, Bad, context, null, context.expr());
    }

    public override bool VisitCpuInstructionDirectIndex([NotNull] SyntaxParser.CpuInstructionDirectIndexContext context)
    {
        M6xxOpcode opcode = _opcodes[context.Start.Type];
        if (context.Y() != null)
        {
            return EmitOpcode(opcode.directIndexed, context, context.expr(), 1);
        }
        return EmitOpcode(opcode.directZ, context, context.expr(), 1);
    }

    private bool EmitPseudoRelative(int mnemonic, int pseudo, SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext operand)
    {
        if (_opcodes.TryGetValue(mnemonic, out M6xxOpcode opcode))
        {
            return EmitRelative(opcode, context, operand);
        }
        int operandSize = 1;
        int address = Services.Evaluator.SafeEvalAddress(operand, _truncateToDp, _dp);
        int offs = address - (Services.State.Output.LogicalPC + 2);
        if (offs < sbyte.MinValue || offs > sbyte.MaxValue)
        {
            int jmp = 0x4c;
            mnemonic = s_6502PseudoToReal[mnemonic] + 256 * 3;
            offs = address.AsPositive() * 256 + jmp;
            operandSize = 3;
        }
        else
        {
            mnemonic = pseudo;
        }
        context.opcode = mnemonic;
        context.opcodeSize = mnemonic.Size();
        context.operand = offs;
        context.operandSize = operandSize;
        return true;
    }

    public override bool VisitCpuInstructionExpressionList([NotNull] SyntaxParser.CpuInstructionExpressionListContext context)
    {
        int mnemonic = context.Start.Type;
        SyntaxParser.ExprContext[] exprs = context.exprList().expr();

        if (s_6502PseudoRelative.TryGetValue(mnemonic, out int pseudo))
        {
            if (exprs.Length != 1)
            {
                return false;
            }
            return EmitPseudoRelative(mnemonic, pseudo, context, exprs[0]);
        }
        M6xxOpcode opcode = _opcodes[mnemonic];
        if (exprs.Length == 1)
        {
            if (opcode.relative != Bad || opcode.relativeAbs != Bad)
            {
                return EmitRelative(opcode, context, exprs[0]);
            }
            if (exprs[0] is SyntaxParser.ExpressionGroupedContext)
            {
                return EmitOpcodeVariant(opcode.indirectZeroPage, opcode.indirect, Bad, context, null, exprs[0]);
            }
            return EmitOpcodeVariant(opcode.zeroPage, opcode.absolute, opcode.longAddress, context, null, exprs[0]);
        }
        if (exprs.Length == 2)
        {
            return EmitOpcode(opcode.blockMove, context, null, exprs.Reverse().ToArray(), 1, 1);
        }
        if (exprs.Length > 3)
        {
            return false;
        }
        return EmitOpcode(opcode.threeOperand, context, null, exprs, 2, 2, 2);
    }

    public override bool VisitCpuInstructionImmmediate([NotNull] SyntaxParser.CpuInstructionImmmediateContext context)
    {
        int mnemonic = context.mnemonic().Start.Type;
        var opcode = _opcodes[mnemonic];
        int size = 1;
        if (context.expr().Length == 1)
        {
            if ((s_accumulators.Contains(mnemonic) && _a16) || (s_indexes.Contains(mnemonic) && _x16))
            {
                size = 2;
            }
            bool emitted = EmitOpcode(opcode.immediate, context, context.bitwidthModifier(), context.imm, size);
            if (!emitted)
            {
                return false;
            }
            if (_auto && context.Start.Type.IsOneOf(SyntaxParser.REP, SyntaxParser.SEP))
            {
                size = context.Start.Type == SyntaxParser.REP ? 2 : 1;
                if ((context.operand & 0b0010_0000) != 0)
                {
                    M(size);
                }
                if ((context.operand & 0b0001_0000) != 0)
                {
                    X(size);
                }
            }
            return true;
        }
        size = Services.Evaluator.SafeEvalAddress(context.expr()[1]).Size();
        if (size == 1)
        {
            return EmitOpcode(context.X() != null ? opcode.bitTestZpX : opcode.bitTest, context, null, context.expr(), 1, 1);
        }
        return EmitOpcode(context.X() != null ? opcode.bitTestAbsX : opcode.bitTestAbs, context, null, context.expr(), 1, 2);
    }

    public override bool VisitCpuInstructionImplied([NotNull] SyntaxParser.CpuInstructionImpliedContext context)
    {
        return EmitOpcode(_opcodes[context.Start.Type].implied, context);
    }

    public override bool VisitCpuInstructionIndex([NotNull] SyntaxParser.CpuInstructionIndexContext context)
    {
        int mnemonic = context.Start.Type;
        int zeroPageHex = Bad, absoluteHex = Bad, longHex = Bad;

        switch (context.register().Start.Type)
        {
            case SyntaxParser.S:
                return EmitOpcode(_opcodes[mnemonic].zeroPageS, context, context.expr());
            case SyntaxParser.X:
                zeroPageHex = _opcodes[mnemonic].zeroPageX;
                absoluteHex = _opcodes[mnemonic].absoluteX;
                longHex = _opcodes[mnemonic].longX;
                break;
            case SyntaxParser.Y:
                zeroPageHex = _opcodes[mnemonic].zeroPageY;
                absoluteHex = _opcodes[mnemonic].absoluteY;
                break;
            default: break;
        }
        return EmitOpcodeVariant(zeroPageHex, absoluteHex, longHex, context, context.bitwidthModifier(), context.expr());
    }

    public override bool VisitCpuInstructionIndexedIndirect([NotNull] SyntaxParser.CpuInstructionIndexedIndirectContext context)
    {
        M6xxOpcode opcode = _opcodes[context.Start.Type];
        return EmitOpcodeVariant(opcode.indexedIndirect, opcode.indexedIndirectAbs, Bad, context, context.bitwidthModifier(), context.expr());
    }

    public override bool VisitCpuInstructionIndirectIndexed([NotNull] SyntaxParser.CpuInstructionIndirectIndexedContext context)
    {
        M6xxOpcode opcode = _opcodes[context.Start.Type];
        int opcodeHex;
        if (context.ix0 != null)
        {
            if (context.ix1.Type != SyntaxParser.Y)
            {
                return false;
            }
            opcodeHex = context.ix0.Type == SyntaxParser.S ? opcode.indirectS : opcode.indirectSp;
        }
        else
        {
            opcodeHex = context.ix1.Type == SyntaxParser.Z ? opcode.indirectZ : opcode.indirectIndexed;
        }
        return EmitOpcode(opcodeHex, context, context.expr());
    }

    public override bool VisitCpuInstructionRegisterList([NotNull] SyntaxParser.CpuInstructionRegisterListContext context)
    {
        if (context.register().Length == 1 &&
            context.register()[0].Start.Type == SyntaxParser.A)
        {
            return EmitOpcode(_opcodes[context.Start.Type].accumulator, context);
        }
        return false;
    }

    public override bool VisitCpuInstructionZPAbsolute([NotNull] SyntaxParser.CpuInstructionZPAbsoluteContext context)
    {
        int mnemonic = context.Start.Type;
        if (s_6502PseudoRelative.TryGetValue(mnemonic, out int pseudo))
        {
            if (context.bitwidthModifier() != null)
            {
                return false;
            }
            return EmitPseudoRelative(mnemonic, pseudo, context, context.expr());
        }
        M6xxOpcode opcode = _opcodes[mnemonic];
        if (opcode.relative != Bad || opcode.relativeAbs != Bad)
        {
            return EmitRelative(opcode, context, context.expr());
        }
        return EmitOpcodeVariant(opcode.zeroPage, opcode.absolute, opcode.longAddress, context, context.bitwidthModifier(), context.expr());
    }

    protected override void OnSetCpu(string cpuid)
    {
        _disassembly = new List<Dictionary<int, Instruction>>() { new Dictionary<int, Instruction>(s_6502Disassembly) };
        switch (cpuid)
        {
            case "45GS02":
                _opcodes = s_45gs02Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(s_rc6502Diassembly);
                _disassembly.Add(s_65ce02Disassembly);
                break;
            case "6502":
                _opcodes = new Dictionary<int, M6xxOpcode>(s_6502Opcodes);
                break;
            case "6502i":
                _opcodes = new Dictionary<int, M6xxOpcode>(s_6502iOpcodes);
                _disassembly.Add(s_6502iDisassembly);
                break;
            case "65816":
                _opcodes = s_65816Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(new Dictionary<int, Instruction>(s_65816Disassembly));
                _disassembly.Add(s_w65c02Disassembly);
                break;
            case "65C02":
                _opcodes = s_65c02Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                break;
            case "65CE02":
                _opcodes = s_65ce02Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(s_rc6502Diassembly);
                _disassembly.Add(s_65ce02Disassembly);
                break;
            case "c64dtv":
                _opcodes = new Dictionary<int, M6xxOpcode>(s_c64dtvOpcodes);
                _disassembly.Add(s_c64dtvDisassembly);
                break;
            case "HuC6280":
                _opcodes = s_huc6280Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(s_rc6502Diassembly);
                _disassembly.Add(s_w65c02Disassembly);
                _disassembly.Add(s_huc6280Disassembly);
                break;
            case "m65":
                _opcodes = s_m65Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(s_rc6502Diassembly);
                _disassembly.Add(s_65ce02Disassembly);
                _disassembly.Add(s_m65Disassembly);
                break;
            case "R65C02":
                _opcodes = s_r65c02Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(s_rc6502Diassembly);
                break;
            case "W65C02":
                _opcodes = s_w65c02Opcodes;
                _disassembly.Add(s_65c02Disassembly);
                _disassembly.Add(s_rc6502Diassembly);
                _disassembly.Add(s_w65c02Disassembly);
                break;
            default: throw new ArgumentException("Invalid cpuid", nameof(cpuid));
        }
        if ((cpuid.StartsWith("6502", StringComparison.Ordinal) || cpuid.Equals("c64dtv")) && _enable6502BranchAlways)
        {
            _opcodes.Add(SyntaxParser.BRA, new M6xxOpcode(Bad, Bad, Bad, 0x50, Bad, Bad, Bad, Bad, Bad, Bad, Bad, Bad, Bad));
        }

        _dp = -1;
    }

    private void TransformDisassembly(int size, Func<int, bool> func)
    {
        char oldWidth = size == 2 ? '2' : '4';
        char newWidth = size == 2 ? '4' : '2';
        for (int i = 0; i < _disassembly.Count; i++)
        {
            Dictionary<int, Instruction> disassembly = _disassembly[i];
            foreach (var kvp in disassembly)
            {
                if (func(kvp.Value.Opcode))
                {
                    disassembly[kvp.Key] = new Instruction(kvp.Value.DisassemblyFormat.Replace(oldWidth, newWidth),
                                                kvp.Value.Opcode, size);
                }
            }
        }
    }

    private void M(int size)
    {
        _a16 = size == 2;
        TransformDisassembly(size, opcode => (opcode & 0xf) == 9 && (opcode & 0xf0) / 16 % 2 == 0);
    }

    private void X(int size)
    {
        _x16 = size == 2;
        TransformDisassembly(size, opcode => opcode == 0xa0 || opcode == 0xa2 || opcode == 0xc0 || opcode == 0xe0);
    }

    public override bool HandleDirective(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        switch (directive.Start.Type)
        {
            case SyntaxParser.Auto:
            case SyntaxParser.Dp:
            case SyntaxParser.M8:
            case SyntaxParser.M16:
            case SyntaxParser.Manual:
            case SyntaxParser.MX8:
            case SyntaxParser.MX16:
            case SyntaxParser.X8:
            case SyntaxParser.X16:
                if (!Cpuid.Equals("65816"))
                {
                    if (Services.DiagnosticOptions.WarningsAsErrors)
                    {
                        return false;
                    }
                    Services.State.Warnings.Add(new Warning(directive, "Directive ignored for non-65816 CPU"));
                    return true;
                }
                switch (directive.Start.Type)
                {
                    case SyntaxParser.Auto:
                    case SyntaxParser.Manual:
                        _auto = directive.Start.Type == SyntaxParser.Auto;
                        break;
                    case SyntaxParser.Dp:
                        if (operands == null || operands.expr().Length != 1)
                        {
                            return false;
                        }
                        _dp = Services.Evaluator.SafeEvalNumber(operands.expr()[0], sbyte.MinValue, byte.MaxValue) & 0xff;
                        break;
                    case SyntaxParser.M8:
                        M(1);
                        break;
                    case SyntaxParser.M16:
                        M(2);
                        break;
                    case SyntaxParser.MX8:
                        M(1);
                        X(1);
                        break;
                    case SyntaxParser.MX16:
                        M(2);
                        X(2);
                        break;
                    case SyntaxParser.X8:
                        X(1);
                        break;
                    case SyntaxParser.X16:
                        X(2);
                        break;
                }
                break;
            default:
                return false;
        }
        return true;
    }

    protected override void OnReset()
    {
        _a16 = _x16 = false;
        _truncateToDp = Cpuid.Equals("65816");
        _dp = 0;
        _auto = Services.ArchitectureOptions.AutosizeRegisters;
    }
}
