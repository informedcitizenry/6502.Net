//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
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
    private List<Dictionary<int, Instruction>> _opcodes;

    private List<Dictionary<int, Instruction>> _implied, _immediate, _accumulator,
                                _zeroPage, _zeroPageX, _zeroPageY,
                                _absolute, _absoluteX, _absoluteY,
                                _indirectX, _indirectY,
                                _indirectAbsolute;
    private List<Dictionary<int, Instruction>>? _indirectZeroPage, _indirectS, _indirectSP,
                                    _immediateAbs, _indirectZ, _twoOperand,
                                     _long, _longX, _directAbs, _direct, _directY, _directZ,
                                     _indirectAbsX, _bitOperand, _bitOperand2,
                                    _threeOperand, _test, _testX, _testAbs, _testAbsX, _zeroPageS;

    private int _dp;
    private bool _truncateToDp;
    private bool _auto;

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
        _enable6502BranchAlways = services.ArchitectureOptions.BranchAlways;
        _implied = null!;
        _accumulator = null!;
        _immediate = null!;
        _absolute = null!;
        _absoluteX = null!;
        _absoluteY = null!;
        _zeroPage = null!;
        _zeroPageX = null!;
        _zeroPageY = null!;
        _indirectAbsolute = null!;
        _indirectX = null!;
        _indirectY = null!;
        _opcodes = null!;
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

    private static Instruction? LookupOpcode(int key, List<Dictionary<int, Instruction>>? lookups)
    {
        for (int i = 0; i < lookups?.Count; i++)
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
        return LookupOpcode(code, _opcodes);
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
            if (contexts[i].ObjectCode.First() == 0x6e &&
                contexts[i].Cpuid.StartsWith("6502") &&
                contexts[i].ObjectCode.ToArray()[1] == 0xff &&
                Services.DiagnosticOptions.WarnJumpBug)
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
        return (string.Format(instr.DisassemblyFormat, operandVals), instr.Size);
    }

    public override bool VisitCpuInstructionBit([NotNull] SyntaxParser.CpuInstructionBitContext context)
    {
        if (context.expr()?.Length < 1)
        {
            return false;
        }
        SyntaxParser.ExprContext[] operands = context.expr();
        List<Dictionary<int, Instruction>>? lookup;
        if (operands.Length == 1)
        {
            lookup = _bitOperand;
        }
        else
        {
            lookup = _bitOperand2;
        }
        Instruction? bitOpcode = LookupOpcode(context.Start.Type, lookup);
        if (bitOpcode != null)
        {
            int operandValues = 0;
            for (int i = 0; i < operands.Length; i++)
            {
                int operandValue;
                if (i == 1)
                {
                    operandValue = Services.Evaluator.SafeEvalAddress(operands[i], _truncateToDp, _dp);
                    if (bitOpcode.IsRelative)
                    {
                        operandValue = Services.State.Output.GetRelativeOffset(operandValue, bitOpcode.Size);
                    }
                }
                else
                {
                    operandValue = Services.Evaluator.SafeEvalNumber(operands[i], sbyte.MinValue, byte.MaxValue, _truncateToDp, _dp);
                }
                operandValues += operandValue << (i * 8);
            }
            int bitValue = Evaluator.EvalIntegerLiteral(context.DecLiteral(), "Valid bit constant [0-7] expected", 0, 8) * 0x10;
            context.opcodeSize = bitOpcode.Opcode.Size();
            context.opcode = bitOpcode.Opcode | bitValue;
            context.operand = operandValues;
            context.operandSize = bitOpcode.Size - context.opcodeSize;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionDirect([NotNull] SyntaxParser.CpuInstructionDirectContext context)
    {
        List<List<Dictionary<int, Instruction>>?> lookups = new()
        {
            _direct,
            _directAbs
        };
        return VisitVariableLength(lookups, context.mnemonic(), context, context.expr(), null);
    }

    public override bool VisitCpuInstructionDirectIndex([NotNull] SyntaxParser.CpuInstructionDirectIndexContext context)
    {
        Instruction? opcode;
        if (context.Y() != null)
        {
            opcode = LookupOpcode(context.Start.Type, _directY);
        }
        else
        {
            opcode = LookupOpcode(context.Start.Type, _directZ);
        }
        if (opcode == null)
        {
            return false;
        }
        context.operand = Services.Evaluator.SafeEvalNumber(context.expr(), sbyte.MinValue, byte.MaxValue, _truncateToDp, _dp);
        context.opcode = opcode.Opcode;
        context.opcodeSize = opcode.Opcode.Size();
        context.operandSize = 1;
        return true;
    }

    private bool VisitVariableLength(
        List<List<Dictionary<int, Instruction>>?> lookups,
        SyntaxParser.MnemonicContext mnemonic,
        SyntaxParser.CpuInstructionContext instructionContext,
        SyntaxParser.ExprContext expression,
        SyntaxParser.BitwidthModifierContext? bitwidthModifier)
    {
        bool evalAsAddress = instructionContext is SyntaxParser.CpuInstructionZPAbsoluteContext ||
                            instructionContext is SyntaxParser.CpuInstructionIndexContext;
        int exprVal;
        if (evalAsAddress)
        {
            exprVal = Services.Evaluator.SafeEvalAddress(expression, _truncateToDp, _dp);
        }
        else
        {
            exprVal = Services.Evaluator.SafeEvalNumber(expression, short.MinValue, ushort.MaxValue, 0, _truncateToDp, _dp);
        }
        int exprSize = exprVal.Size();
        if (bitwidthModifier != null)
        {
            int bwSize = BitwidthSize(bitwidthModifier);
            if (bwSize < exprSize && !Services.State.PassNeeded)
            {
                throw new Error(mnemonic, "Expression is greater than specified size");
            }
            exprSize = bwSize;
            for (int i = 0; i < bwSize - 1; i++)
            {
                lookups[i] = null;
            }
            for (int i = bwSize; i < lookups.Count; i++)
            {
                lookups[i] = null;
            }
        }
        Instruction? instruction = null;
        for (int i = exprSize - 1; i < lookups.Count && instruction == null; i++)
        {
            instruction = LookupOpcode(mnemonic.Start.Type, lookups[i]);
        }
        if (instruction == null)
        {
            if (Services.State.PassNeeded && evalAsAddress)
            {
                instructionContext.opcodeSize = 1;
                instructionContext.operandSize = 2;
                return true;
            }
            return false;
        }
        int opcode = instruction.Opcode;
        int size = instruction.Size;
        if (instruction.IsRelative)
        {
            int offset = Services.State.Output.GetRelativeOffset(exprVal, size);
            exprSize = instruction.Is16BitRelative ? 2 : 1;
            double minVal = instruction.Is16BitRelative ? short.MinValue : sbyte.MinValue;
            double maxVal = instruction.Is16BitRelative ? short.MaxValue : sbyte.MaxValue;
            if (!Services.State.PassNeeded && (offset < minVal || offset > maxVal))
            {
                if (!mnemonic.Start.Type.IsOneOf(SyntaxParser.JCC,
                                                SyntaxParser.JCS,
                                                SyntaxParser.JEQ,
                                                SyntaxParser.JMI,
                                                SyntaxParser.JNE,
                                                SyntaxParser.JPL,
                                                SyntaxParser.JVC,
                                                SyntaxParser.JVS))
                {
                    throw new Error(expression, "Relative offset too far");
                }
                opcode = s_6502PseudoToReal[mnemonic.Start.Type] + 256 * 3;
                int jmp = instruction.Is16BitRelative ? 0x5c : 0x4c;
                exprVal = exprVal.AsPositive() * 256 + jmp;
                exprSize++;
                size = 5;
            }
            else
            {
                exprVal = offset;
            }
        }
        if (exprSize < size - opcode.Size())
        {
            exprSize = size - opcode.Size();
        }
        if (size - exprSize < 0)
        {
            return false;
        }
        instructionContext.opcode = opcode;
        instructionContext.opcodeSize = size - exprSize;
        instructionContext.operand = exprVal.AsPositive();
        instructionContext.operandSize = exprSize;
        return true;
    }

    public override bool VisitCpuInstructionExpressionList([NotNull] SyntaxParser.CpuInstructionExpressionListContext context)
    {
        SyntaxParser.ExprContext[] exprs = context.exprList().expr();
        if (exprs.Length == 1)
        {
            List<List<Dictionary<int, Instruction>>?> lookups;
            if (exprs[0] is SyntaxParser.ExpressionGroupedContext)
            {
                lookups = new()
                {
                    _indirectZeroPage,
                    _indirectAbsolute
                };
            }
            else
            {
                lookups = new()
                {
                    _zeroPage,
                    _absolute,
                    _long
                };
            }
            return VisitVariableLength(lookups, context.mnemonic(), context, exprs[0], null);
        }
        double minValue = sbyte.MinValue, maxValue = byte.MaxValue;
        List<Dictionary<int, Instruction>>? lookup;
        int step;
        int i;
        switch (exprs.Length)
        {
            case 2:
                lookup = _twoOperand;
                step = -1;
                i = exprs.Length - 1;
                break;
            case 3:
                lookup = _threeOperand;
                minValue = short.MinValue;
                maxValue = ushort.MaxValue;
                step = 1;
                i = 0;
                break;
            default:
                return false;
        }
        Instruction? opcode = LookupOpcode(context.Start.Type, lookup);
        if (opcode == null)
        {
            return false;
        }
        long value = 0;
        int size = maxValue.Size();
        int shiftSize = 8 * size;
        for (int s = 0; s < exprs.Length; i += step, s++)
        {
            int shift = shiftSize * s;
            value |= (long)Services.Evaluator.SafeEvalNumber(exprs[i], minValue, maxValue, _truncateToDp, _dp) << shift;
        }
        int totalSize = size * exprs.Length;
        if (opcode.Size - opcode.Opcode.Size() - totalSize < 0)
        {
            throw new Error(context.Start, "Addressing mode not supported");
        }
        context.operand = value;
        context.operandSize = opcode.Size - opcode.Opcode.Size();
        context.opcodeSize = opcode.Opcode.Size();
        context.opcode = opcode.Opcode;
        return true;
    }

    public override bool VisitCpuInstructionImmmediate([NotNull] SyntaxParser.CpuInstructionImmmediateContext context)
    {
        if (context.expr().Length == 1)
        {
            if (!VisitVariableLength(new List<List<Dictionary<int, Instruction>>?>
                {
                    _immediate,
                    _immediateAbs
                },
                context.mnemonic(),
                context,
                context.imm,
                context.bitwidthModifier()))
            {
                return false;
            }
            if (_auto && context.Start.Type.IsOneOf(SyntaxParser.REP, SyntaxParser.SEP))
            {
                int size = context.Start.Type == SyntaxParser.REP ? 2 : 1;
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
        SyntaxParser.ExprContext[] operands = context.expr();
        List<Dictionary<int, Instruction>>? lookup = context.X() != null ? _testX : _test;

        int testImm = Services.Evaluator.SafeEvalNumber(context.imm, sbyte.MinValue, byte.MaxValue);
        int jump = Services.Evaluator.SafeEvalAddress(operands[1], _truncateToDp, _dp);
        if (jump.Size() > 1)
        {
            lookup = ReferenceEquals(lookup, _test) ? _testAbs : _testAbsX;
        };
        Instruction? opcode = LookupOpcode(context.Start.Type, lookup);
        if (opcode == null)
        {
            if (ReferenceEquals(lookup, _test))
            {
                lookup = _testAbs;
            }
            if (ReferenceEquals(lookup, _testX))
            {
                lookup = _testAbsX;
            }
            opcode = LookupOpcode(context.Start.Type, lookup);
            if (opcode == null)
            {
                return false;
            }
        }
        context.operand = testImm + jump * 256;
        context.operandSize = opcode.Size - opcode.Opcode.Size();
        context.opcode = opcode.Opcode;
        context.opcodeSize = opcode.Opcode.Size();
        return true;
    }

    public override bool VisitCpuInstructionImplied([NotNull] SyntaxParser.CpuInstructionImpliedContext context)
    {
        Instruction? opcode = LookupOpcode(context.Start.Type, _implied);
        if (opcode == null)
        {
            return false;
        }
        context.opcode = opcode.Opcode;
        context.opcodeSize = opcode.Size;
        context.operandSize = 0;
        return true;
    }

    public override bool VisitCpuInstructionIndex([NotNull] SyntaxParser.CpuInstructionIndexContext context)
    {
        List<Dictionary<int, Instruction>>? zp, abs, lng = null;
        switch (context.register().Start.Type)
        {
            case SyntaxParser.S:
                zp = _zeroPageS;
                abs = null;
                break;
            case SyntaxParser.X:
                zp = _zeroPageX;
                abs = _absoluteX;
                lng = _longX;
                break;
            case SyntaxParser.Y:
                zp = _zeroPageY;
                abs = _absoluteY;
                break;
            default:
                return false;
        }
        List<List<Dictionary<int, Instruction>>?> lookups = new()
        {
            zp,abs,lng
        };
        return VisitVariableLength(lookups, context.mnemonic(), context, context.expr(), context.bitwidthModifier());
    }

    public override bool VisitCpuInstructionIndexedIndirect([NotNull] SyntaxParser.CpuInstructionIndexedIndirectContext context)
    {
        int indirectVal = Services.Evaluator.SafeEvalAddress(context.expr(), _truncateToDp, _dp);
        List<Dictionary<int, Instruction>>? lookup;
        if (indirectVal.Size() == 2 && !Services.State.PassNeeded)
        {
            lookup = _indirectAbsX;
        }
        else
        {
            lookup = _indirectX;
        }
        Instruction? instruction = LookupOpcode(context.Start.Type, lookup);
        if (instruction == null)
        {
            return false;
        }
        context.opcode = instruction.Opcode;
        context.opcodeSize = instruction.Opcode.Size();
        context.operand = indirectVal;
        context.operandSize = instruction.Size - context.opcodeSize;
        return true;
    }

    public override bool VisitCpuInstructionIndirectIndexed([NotNull] SyntaxParser.CpuInstructionIndirectIndexedContext context)
    {
        List<Dictionary<int, Instruction>>? lookup = _indirectY;
        if (context.S() != null)
        {
            lookup = _indirectS;
        }
        else if (context.SP() != null)
        {
            lookup = _indirectSP;
        }
        else if (context.Z() != null)
        {
            if (context.ix0 != null)
            {
                return false;
            }
            lookup = _indirectZ;
        }
        Instruction? instruction = LookupOpcode(context.Start.Type, lookup);
        if (instruction == null)
        {
            return false;
        }
        context.opcode = instruction.Opcode;
        context.opcodeSize = instruction.Size - 1;
        context.operand = Services.Evaluator.SafeEvalNumber(context.expr(), sbyte.MinValue, byte.MaxValue, _truncateToDp, _dp);
        context.operandSize = 1;
        return true;
    }

    public override bool VisitCpuInstructionRegisterList([NotNull] SyntaxParser.CpuInstructionRegisterListContext context)
    {
        List<Dictionary<int, Instruction>>? lookup = null;
        if (context.register().Length == 1 &&
            context.register()[0].Start.Type == SyntaxParser.A)
        {
            lookup = _accumulator;
        }
        Instruction? opcode = LookupOpcode(context.Start.Type, lookup);
        if (opcode == null)
        {
            return false;
        }
        context.opcode = opcode.Opcode;
        context.opcodeSize = opcode.Size;
        return true;
    }

    public override bool VisitCpuInstructionZPAbsolute([NotNull] SyntaxParser.CpuInstructionZPAbsoluteContext context)
    {
        List<List<Dictionary<int, Instruction>>?> lookups;
        if (context.expr() is SyntaxParser.ExpressionGroupedContext)
        {
            lookups = new()
            {
                _indirectZeroPage,
                _indirectAbsolute
            };
        }
        else
        {
            lookups = new()
            {
                _zeroPage,
                _absolute,
                _long
            };
        }
        return VisitVariableLength(lookups, context.mnemonic(), context, context.expr(), context.bitwidthModifier());
    }

    protected override void OnSetCpu(string cpuid)
    {
        _implied = new List<Dictionary<int, Instruction>>() { s_6502Implied };
        _accumulator = new List<Dictionary<int, Instruction>>() { s_m6502Accumulators };
        _immediate = new List<Dictionary<int, Instruction>>() { new Dictionary<int, Instruction>(s_6502Immediate) };
        _absolute = new List<Dictionary<int, Instruction>>
        {
            s_6502Absolute,
            s_6502Relative,
            s_6502PseudoRelative

        };
        if (_enable6502BranchAlways && cpuid.Equals("6502"))
        {
            _absolute.Add(s_6502BranchAlways);
        }
        _absoluteX = new List<Dictionary<int, Instruction>> { s_6502AbsoluteX };
        _absoluteY = new List<Dictionary<int, Instruction>> { s_6502AbsoluteY };
        _zeroPage = new List<Dictionary<int, Instruction>>()
        {
            s_6502ZeroPage,
            s_6502Relative,
        };
        if (!cpuid.Equals("45GS02") && !cpuid.Equals("65CE02") && !cpuid.Equals("m65"))
        {
            _zeroPage.Add(s_6502PseudoRelative);
        }
        _zeroPageX = new List<Dictionary<int, Instruction>>() { s_6502ZeroPageX };
        _zeroPageY = new List<Dictionary<int, Instruction>>() { s_6502ZeroPageY };
        _indirectAbsolute = new List<Dictionary<int, Instruction>>() { s_6502IndAbs };
        _indirectX = new List<Dictionary<int, Instruction>>() { s_6502IndX };
        _indirectY = new List<Dictionary<int, Instruction>>() { s_6502IndY };
        _opcodes = new List<Dictionary<int, Instruction>>() { new Dictionary<int, Instruction>(s_6502AllOpcodes) };
        _dp = -1;
        _direct =
        _directAbs =
        _directY =
        _directZ =
        _immediateAbs =
        _indirectZeroPage =
        _indirectS =
        _indirectSP =
        _indirectZ =
        _indirectAbsX =
        _test = _testX =
        _testAbs = _testAbsX =
        _zeroPageS =
        _long = _longX =
        _twoOperand =
        _threeOperand =
        _bitOperand =
        _bitOperand2 = null;
        switch (cpuid)
        {
            case "6502i":
                _implied.Add(s_6502iImplied);
                _immediate.Add(s_6502iImmediate);
                _absolute.Add(s_6502iAbsolute);
                _absoluteX.Add(s_6502iAbsoluteX);
                _absoluteY.Add(s_6502iAbsoluteY);
                _zeroPage.Add(s_6502iZeroPage);
                _zeroPageX.Add(s_6502iZeroPageX);
                _zeroPageY.Add(s_6502iZeroPageY);
                _indirectX.Add(s_6502iIndX);
                _indirectY.Add(s_6502iIndY);
                _opcodes.Add(s_6502iAllOpcodes);
                break;
            case "c64dtv2":
                _immediate.Add(s_c64dtv2Immediate);
                _absolute.Add(s_c64dtv2Relative);
                _opcodes.Add(s_c64dtv2AllOpcodes);
                break;
            case "45GS02":
            case "65816":
            case "65C02":
            case "65CE02":
            case "65CS02":
            case "HuC6280":
            case "m65":
            case "R65C02":
            case "W65C02":
                _implied.Add(s_65c02Implied);
                _immediate.Add(s_65c02Immediate);
                _absolute.Add(s_65c02Absolute);
                _absolute.Add(s_65c02Relative);
                _absoluteX.Add(s_65c02AbsoluteX);
                _accumulator.Add(s_65c02Accumulator);
                _zeroPage.Add(s_65c02ZeroPage);
                _zeroPageX.Add(s_65c02ZeroPageX);
                _indirectAbsX = new() { s_65c02IndAbsX };
                _indirectZeroPage = new() { s_65c02IndZp };
                _opcodes.Add(s_65c02AllOpcodes);
                switch (cpuid)
                {
                    case "65816":
                    case "65CS02":
                    case "HuC6280":
                    case "W65C02":
                        _implied.Add(s_w65c02Implied);
                        _opcodes.Add(s_w65c02AllOpcodes);
                        switch (cpuid)
                        {
                            case "65816":
                                _implied.Add(s_65816Implied);
                                _immediate.Add(s_65816Immediate);
                                _immediateAbs = new() { new Dictionary<int, Instruction>(s_65816ImmAbs) };
                                _indirectAbsX.Add(s_65816IndAbsX);
                                _indirectZeroPage.Add(s_65816IndZp);
                                _absolute.Add(s_65816Absolute);
                                _absolute.Add(s_65816RelativeAbs);
                                _opcodes.Add(s_65816AllOpcodes);
                                _direct = new() { s_65816Dir };
                                _directY = new() { s_65816DirY };
                                _directAbs = new() { s_65816DirAbs };
                                _indirectS = new() { s_65816IndS };
                                _long = new() { s_65816Long };
                                _longX = new() { s_65816LongX };
                                _zeroPageS = new() { s_65816ZeroPageS };
                                _twoOperand = new() { s_65816TwoOperand };
                                _dp = 0;
                                break;
                            case "HuC6280":
                                _implied.Add(s_huC6280Implied);
                                _immediate.Add(s_huC6280Immediate);
                                _indirectAbsolute.Add(s_huC6280IndAbs);
                                _opcodes.Add(s_huC6280AllOpcodes);
                                _test = new() { s_huC6280TestBitZp };
                                _testAbs = new() { s_huC6280TestBitAbs };
                                _testX = new() { s_huC6280TestBitZpX };
                                _testAbsX = new() { s_huC6280TestBitAbsX };
                                _threeOperand = new() { s_huC6280ThreeOpAbs };
                                break;
                        }
                        break;
                }
                switch (cpuid)
                {
                    case "45GS02":
                    case "65CE02":
                    case "HuC6280":
                    case "m65":
                    case "R65C02":
                    case "W65C02":
                        _opcodes.Add(s_r65c02AllOpcodes);
                        _bitOperand2 = new()
                        {
                            s_r65c02ThreeOpRel0,
                            s_r65c02ThreeOpRel1,
                            s_r65c02ThreeOpRel2,
                            s_r65c02ThreeOpRel3,
                            s_r65c02ThreeOpRel4,
                            s_r65c02ThreeOpRel5,
                            s_r65c02ThreeOpRel6,
                            s_r65c02ThreeOpRel7
                        };
                        _bitOperand = new()
                        {
                            s_r65c02Zp0,
                            s_r65c02Zp1,
                            s_r65c02Zp2,
                            s_r65c02Zp3,
                            s_r65c02Zp4,
                            s_r65c02Zp5,
                            s_r65c02Zp6,
                            s_r65c02Zp7
                        };

                        switch (cpuid)
                        {
                            case "45GS02":
                            case "65CE02":
                            case "m65":
                                _immediate.Add(s_65ce02Immediate);
                                _implied.Add(s_65ce02Implied);
                                _absolute = new()
                                {
                                    s_6502Absolute,
                                    s_65c02Absolute,
                                    s_65ce02Absolute,
                                    s_65ce02RelativeAbs
                                };
                                _absoluteX.Add(s_65ce02AbsoluteX);
                                _absoluteY.Add(s_65ce02AbsoluteY);
                                _zeroPage = new()
                                {
                                    // remove the 6502 relative branches
                                    s_6502ZeroPage,
                                    s_65c02ZeroPage
                                };
                                _indirectAbsolute.Add(s_65ce02IndAbs);
                                _indirectAbsX.Add(s_65ce02IndAbsX);
                                _zeroPage.Add(s_65ce02ZeroPage);
                                _zeroPageX.Add(s_65ce02ZeroPageX);
                                _opcodes.Add(s_65ce02AllOpcodes);
                                _immediateAbs = new() { s_65ce02ImmAbs };
                                _indirectSP = new() { s_65ce02IndSp };
                                _indirectZ = new() { s_65ce02IndZ };
                                if (cpuid[0] != '6')
                                {
                                    // remove default 'NOP' instruction
                                    _implied[0].Remove(SyntaxParser.NOP);
                                    _implied.Add(s_m65Implied);
                                    _absolute.Add(s_m65Absolute);
                                    _absoluteX.Add(s_m65AbsoluteX);
                                    _absoluteY.Add(s_m65AbsoluteY);
                                    _zeroPage.Add(s_m65ZeroPage);
                                    _zeroPageX.Add(s_m65ZeroPageX);
                                    _indirectZeroPage.Add(s_m65IndZp);
                                    _indirectY.Add(s_m65IndY);
                                    _indirectS = new() { s_m65IndS };
                                    _direct = new() { s_m65Dir };
                                    _directZ = new() { s_m65DirZ };
                                    _opcodes.Add(s_m65AllOpcodes);
                                }
                                break;
                        }
                        break;
                }
                break;
            default:
                if (!cpuid.Equals("6502"))
                    throw new ArgumentException("Invalid cpuid", nameof(cpuid));
                break;
        }
    }

    private void MX(int size, Func<int, bool> selector)
    {
        char oldformat = size == 1 ? '4' : '2';
        char newformat = size == 1 ? '2' : '4';
        foreach (KeyValuePair<int, Instruction> instr in s_6502Immediate)
        {
            string format = instr.Value.DisassemblyFormat.Replace(oldformat, newformat);
            Instruction instruction = new(format, instr.Value.Opcode, size + 1);
            if (selector(instr.Value.Opcode & 0xf))
            {
                _opcodes[0][instr.Value.Opcode] = instruction;
                if (size == 1)
                {
                    _immediate[0][instr.Key] = instruction;
                    _immediateAbs![0].Remove(instr.Key);
                }
                else
                {
                    _immediate[0].Remove(instr.Key);
                    _immediateAbs![0][instr.Key] = instruction;
                }
            }
        }
    }

    private void M(int size)
    {
        MX(size, o => o == 9);
    }

    private void X(int size)
    {
        MX(size, o => o != 9);
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
                    Services.State.Warnings.Add(new Warning(directive.Start, "Directive ignored for non-65816 CPU"));
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
        _truncateToDp = Cpuid.Equals("65816");
        _dp = 0;
        _auto = Services.ArchitectureOptions.AutosizeRegisters;
    }
}

