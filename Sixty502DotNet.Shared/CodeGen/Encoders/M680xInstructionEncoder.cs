//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;
using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an encoder for the Motorola 6800/6809 microprocessors.
/// </summary>
public sealed partial class M680xInstructionEncoder : CpuEncoderBase
{
    private int _dp;
    private bool _truncateToDp;
    private Dictionary<int, Instruction> _all, _implied, _absolute,
                                        _immediateAbs, _immediate,
                                        _zeroPage;

    /// <summary>
    /// Construct a new instance of the <see cref="M680xInstructionEncoder"/>
    /// class.
    /// </summary>
    /// <param name="cpuid">The initial cpu to set the encoder.</param>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembly runtime.</param>
    public M680xInstructionEncoder(string cpuid, AssemblyServices services)
        : base(cpuid, services)
    {
        _dp = 0;
        _absolute = null!;
        _all = null!;
        _immediate = null!;
        _immediateAbs = null!;
        _implied = null!;
        _zeroPage = null!;
    }

    public override void Analyze(IList<CodeAnalysisContext> contexts)
    {
        for (int i = 0; i < contexts.Count; i++)
        {
            if (i < contexts.Count - 1)
            {
                if (contexts[i].Cpuid[^1] == '9')
                {
                    AnalyzeCallReturn(contexts[i], contexts[i + 1], 0x9d, 0x39);

                }
                AnalyzeCallReturn(contexts[i], contexts[i + 1], 0xbd, 0x39);
            }
        }
    }

    private static string DecodeRegisters(byte[] bytes, string disasm)
    {
        IEnumerable<string> regs;
        int encoded = bytes[1];
        if (bytes[0] == 0x1e || bytes[0] == 0x1f)
        {
            regs = new List<string>();
            if (s_exchangeRegsLu.TryGetValue(encoded / 16, out string? reg))
            {
                ((List<string>)regs).Add(reg);
            }
            if (s_exchangeRegsLu.TryGetValue(encoded & 15, out reg))
            {
                ((List<string>)regs).Add(reg);
            }
        }
        else
        {
            regs = new HashSet<string>();
            for (int i = 128; i > 0; i >>= 1)
            {
                if (s_pushPullRegsLu.TryGetValue(encoded & i, out string? reg))
                {
                    if (reg.Equals("su"))
                    {
                        reg = bytes[0] < 0x36 ? "u" : "s";
                    }
                    ((HashSet<string>)regs).Add(reg);
                }
            }
        }
        return $"{disasm} {string.Join(',', regs)}";
    }

    private static int IndexOperandSize(int postByte)
    {
        if (postByte < 128)
        {
            return 1; // 5-bit offset
        }
        if ((postByte & 0b1000) != 0)
        {
            if ((postByte & M6809IndexFlags.Extended) == M6809IndexFlags.Extended)
            {
                return 2;
            }
            if ((postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AccDOffset)
            {
                return 0;
            }
            return 1 + (postByte & 1);
        }
        return 0;
    }

    protected override (string, int) OnDecode(byte[] bytes, bool isSingleInstruction, int offset, int programCounter)
    {
        int originalOffset = offset;
        int opcode = bytes[offset++];
        bool is6809 = Cpuid[^1] == '9';
        if (is6809 && (opcode == 0x10 || opcode == 0x11) && bytes.Length > 1)
        {
            opcode = opcode * 256 + bytes[offset++];
        }
        if (_all.TryGetValue(opcode, out Instruction? instruction))
        {
            if (s_regOpcodes.Contains(opcode))
            {
                return (DecodeRegisters(bytes, instruction.DisassemblyFormat), instruction.Size);
            }
            if (is6809 && isSingleInstruction && (opcode == 0x86 || opcode == 0xc6) && bytes.Length - originalOffset >= 4)
            {
                if (bytes[originalOffset] == 0x86)
                {
                    return ($"lda #${bytes[1]:x2}:tfr a,dp", 4);
                }
                return ($"ldb #${bytes[1]:x2}:tfr b,dp", 4);
            }
            int operand = 0;
            int operandSize = instruction.Size - opcode.Size();
            int postByte = -1;
            if (is6809 && s_m6809IndexedCodes.Contains(opcode) && bytes.Length > 1)
            {
                postByte = bytes[offset++];
                operandSize = IndexOperandSize(postByte);
            }
            if (operandSize + (offset - 1) > bytes.Length)
            {
                return (string.Empty, 0);
            }
            while (operandSize > 0 && offset < bytes.Length)
            {
                operand = operand * 256 + bytes[offset++];
                operandSize--;
            }
            if (postByte >= 0)
            {
                bool extended = postByte == M6809IndexFlags.Extended;
                bool indirect = (postByte & M6809IndexFlags.Indirect) == M6809IndexFlags.Indirect;
                bool is5BitOffset = (postByte & 128) == 0;
                bool is8BitOffset = (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.Offset8bit;
                bool is16BitOffset = (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.Offset16;
                bool isPc8BitOffset = (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.PC8BitOffs;
                bool isPc16BitOffset = (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.PC16Bit;
                bool hasOffset = is5BitOffset || is8BitOffset || is16BitOffset || isPc8BitOffset || isPc16BitOffset;

                StringBuilder ixFormat = new();
                if (indirect)
                {
                    ixFormat.Append('[');
                    if (extended)
                    {
                        ixFormat.Append("${0:x4}");
                    }
                }
                if (hasOffset)
                {
                    bool negativeOffset = false;
                    if (is5BitOffset)
                    {
                        operand = postByte & M6809IndexFlags.Offset5bit;
                    }
                    else if (isPc8BitOffset || isPc16BitOffset)
                    {
                        int maxvalue = isPc8BitOffset ? 127 : short.MaxValue;
                        operand = CodeOutput.GetEffectiveAddress(programCounter + offset, operand, maxvalue);
                    }
                    if ((is16BitOffset && operand > short.MaxValue) ||
                        (is8BitOffset && operand > sbyte.MaxValue) ||
                        (is5BitOffset && operand > 15))
                    {
                        negativeOffset = true;
                        operand = operand switch
                        {
                            > short.MaxValue => 65536 - operand,
                            > sbyte.MaxValue => 256 - operand,
                            _                => 32 - operand
                        };
                    }
                    if (negativeOffset)
                    {
                        ixFormat.Append('-');
                    }
                    ixFormat.Append("${0:x");
                    ixFormat.Append(is5BitOffset || is8BitOffset || isPc8BitOffset ?
                        '2' : '4');
                    ixFormat.Append('}');
                }
                else if ((postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AccDOffset)
                {
                    ixFormat.Append('d');
                }
                else if ((postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AccAOffset)
                {
                    ixFormat.Append('a');
                }
                else if ((postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AccBOffset)
                {
                    ixFormat.Append('b');
                }
                if (!extended)
                {
                    ixFormat.Append(',');
                    bool autoDec = (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AutoDec1 ||
                                    (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AutoDec2;
                    if (autoDec)
                    {
                        ixFormat.Append('-');
                        if ((postByte & 1) == 1)
                        {
                            ixFormat.Append('-');
                        }
                    }
                    if (isPc8BitOffset || isPc16BitOffset)
                    {
                        ixFormat.Append("pc");
                    }
                    else ixFormat.Append(s_indexRegsReverseLu[postByte & M6809IndexFlags.IndexMask]);
                    if ((postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AutoInc1 ||
                        (postByte & M6809IndexFlags.OffsetMask) == M6809IndexFlags.AutoInc2)
                    {
                        ixFormat.Append('+');
                        if ((postByte & 1) == 1)
                        {
                            ixFormat.Append('+');
                        }
                    }
                }
                if (indirect)
                {
                    ixFormat.Append(']');
                }
                string disassemblyFormat = $"{instruction.DisassemblyFormat} {ixFormat}";
                return (string.Format(disassemblyFormat, operand.AsPositive()), instruction.Size);
            }
            if (instruction.IsRelative)
            {
                int maxValue = sbyte.MaxValue;
                if (instruction.Is16BitRelative)
                {
                    maxValue = short.MaxValue;
                }
                operand = CodeOutput.GetEffectiveAddress(programCounter + offset, operand, maxValue);
            }
            return (string.Format(instruction.DisassemblyFormat, operand.AsPositive()), instruction.Size);
        }
        return (string.Empty, 0);
    }

    private bool VisitVariableLength(List<Dictionary<int, Instruction>> lookups,
                                      SyntaxParser.MnemonicContext mnemonic,
                                      SyntaxParser.CpuInstructionContext instructionContext,
                                      SyntaxParser.ExprContext expr,
                                      SyntaxParser.BitwidthModifierContext? bitwidthModifier)
    {
        int bitwidthSize = BitwidthSize(bitwidthModifier);
        if (bitwidthSize > 2)
        {
            return false;
        }
        double minValue = bitwidthSize == 1 ? sbyte.MinValue : short.MinValue;
        double maxValue = bitwidthSize == 1 ? byte.MaxValue : ushort.MaxValue;
        int operand = Services.Evaluator.SafeEvalNumber(expr, minValue, maxValue, _truncateToDp, _dp);
        int operandSize = operand.Size();
        if (bitwidthSize > 0)
        {
            lookups.RemoveAt(bitwidthSize - 1);
        }
        else if (operandSize > 1)
        {
            lookups.RemoveAt(0);
        }
        Instruction? instruction = null;
        for (int i = 0; i < lookups.Count && !lookups[i].TryGetValue(mnemonic.Start.Type, out instruction); i++)
        {
        }
        if (instruction == null)
        {
            if (Services.State.PassNeeded)
            {
                instructionContext.opcodeSize = 1;
                instructionContext.operandSize = 2;
                return true;
            }
            return false;
        }
        if (instruction.IsRelative)
        {
            // recalculate operand without respect to direct page value
            operand = Services.Evaluator.SafeEvalNumber(expr, minValue, maxValue);
            operand = Services.State.Output.GetRelativeOffset(operand, instruction.Size);
            int minvalue = instruction.Is16BitRelative ? short.MinValue : sbyte.MinValue;
            int maxvalue = instruction.Is16BitRelative ? short.MaxValue : sbyte.MaxValue;
            if (operand < minvalue || operand > maxvalue)
            {
                if (!Services.State.PassNeeded)
                {
                    throw new Error(expr, "Relative offset too far");
                }
                operand = maxvalue;
            }
        }
        instructionContext.opcode = instruction.Opcode;
        instructionContext.opcodeSize = instruction.Opcode.Size();
        instructionContext.operand = operand;
        instructionContext.operandSize = instruction.Size - instructionContext.opcodeSize;
        return true;
    }

    private bool VisitM6809Index(SyntaxParser.MnemonicContext mnemonic,
                                 SyntaxParser.CpuInstructionContext instruction,
                                 bool indirect,
                                 int indexRegs, /* set to 0 if not present */
                                 int increments,
                                 int accumulator, /* set to -1 if not present */
                                 SyntaxParser.ExprContext? expression = null)
    {
        if (Cpuid.EndsWith('0') || !s_m6809IndexedX.TryGetValue(mnemonic.Start.Type, out Instruction? opcode))
        {
            return false;
        }
        int postfix = 128;
        int indexBits = 0;
        int value = -1;
        int size = 0;
        if (indirect)
        {
            postfix |= M6809IndexFlags.Indirect;
        }
        if (indexRegs > 0)
        {
            if (!s_indexRegs.TryGetValue(indexRegs, out indexBits))
            {
                if (!indexRegs.IsOneOf(SyntaxParser.PC, SyntaxParser.PCR) || expression == null || increments > 0)
                {
                    return false;
                }
                indexBits = M6809IndexFlags.PC8BitOffs;
            }
            postfix |= indexBits;
            if (increments != 0)
            {
                postfix |= s_increments[increments];
            }
            else if (expression == null && accumulator < 0)
            {
                postfix |= M6809IndexFlags.ZeroOffset;
            }
        }
        if (expression != null)
        {
            size++;
            value = Services.Evaluator.SafeEvalNumber(expression, short.MinValue, ushort.MaxValue);
            if (indexRegs > 0)
            {
                if (indexRegs.IsOneOf(SyntaxParser.PC, SyntaxParser.PCR))
                {
                    int opcodeSize = opcode.Opcode.Size();
                    int relOffs = Services.State.Output.GetRelativeOffset(value, opcodeSize + 2);
                    if (relOffs < sbyte.MinValue || relOffs > sbyte.MaxValue)
                    {
                        relOffs = Services.State.Output.GetRelativeOffset(value, opcodeSize + 3);
                    }
                    value = relOffs;
                }
                if (value < sbyte.MinValue || value > byte.MaxValue)
                {
                    if (!Services.State.PassNeeded && (value < short.MinValue || value > short.MaxValue))
                    {
                        throw new Error(expression, "Offset too far");
                    }
                    size++;
                    postfix |= M6809IndexFlags.Offset16;
                }
                else if (!indirect && value >= -16 && value <= 15 && !indexRegs.IsOneOf(SyntaxParser.PC, SyntaxParser.PCR))
                {
                    size--;
                    if (value == 0)
                    {
                        postfix = indexBits | M6809IndexFlags.ZeroOffset;
                    }
                    else
                    {
                        postfix = indexBits | (value & M6809IndexFlags.Offset5bit);
                    }
                    value = -1;
                }
                else
                {
                    postfix |= M6809IndexFlags.Offset8bit;
                }
            }
            else
            {
                postfix = M6809IndexFlags.Extended;
                size = 2;
            }
        }
        if (accumulator > -1)
        {
            postfix |= s_accumulatorRegs[accumulator];
        }
        instruction.opcode = (opcode.Opcode * 256) | postfix;
        instruction.operand = value;
        instruction.operandSize = size;
        instruction.opcodeSize = instruction.opcode.Size();
        return true;
    }

    private void Tfrdp(SyntaxParser.MnemonicContext mnemonic, SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext operand)
    {
        _dp = Services.Evaluator.SafeEvalNumber(operand, sbyte.MinValue, byte.MaxValue, 0).AsPositive();
        if (SyntaxParser.Tfradp == mnemonic.Start.Type)
        {
            context.opcode = 0x86;
            context.operand = _dp * 0x10000 + 0x1f8b;
        }
        else
        {
            context.opcode = 0xc6;
            context.operand = _dp * 0x10000 + 0x1f9b;
        }
        context.opcodeSize = 1;
        context.operandSize = 3;
    }

    public override bool VisitCpuInstructionAutoIncrement([NotNull] SyntaxParser.CpuInstructionAutoIncrementContext context)
    {
        return VisitM6809Index(context.mnemonic(),
                             context,
                             context.LeftSquare() != null,
                             context.reg.Start.Type,
                             context.inc.Type,
                             -1);
    }

    public override bool VisitCpuInstructionDirect([NotNull] SyntaxParser.CpuInstructionDirectContext context)
    {
        return VisitM6809Index(context.mnemonic(),
                             context,
                             true,
                             0,
                             0,
                             -1,
                             context.expr());
    }

    public override bool VisitCpuInstructionExpressionList([NotNull] SyntaxParser.CpuInstructionExpressionListContext context)
    {
        if (context.exprList().expr().Length != 1)
        {
            return false;
        }
        if (context.Start.Type.IsOneOf(SyntaxParser.Tfradp, SyntaxParser.Tfrbdp))
        {
            Tfrdp(context.mnemonic(), context, context.exprList().expr()[0]);
            return true;
        }
        List<Dictionary<int, Instruction>> lookups = new() { _zeroPage, _absolute };
        return VisitVariableLength(lookups, context.mnemonic(), context, context.exprList().expr()[0], null);
    }

    public override bool VisitCpuInstructionImmmediate([NotNull] SyntaxParser.CpuInstructionImmmediateContext context)
    {
        SyntaxParser.ExprContext[] expr = context.expr();
        if (expr.Length > 1)
        {
            return false;
        }
        List<Dictionary<int, Instruction>> lookups = new() { _immediate, _immediateAbs };
        return VisitVariableLength(lookups,
                                 context.mnemonic(),
                                 context,
                                 context.imm,
                                 context.bitwidthModifier());
    }

    public override bool VisitCpuInstructionImplied([NotNull] SyntaxParser.CpuInstructionImpliedContext context)
    {
        if (_implied.TryGetValue(context.Start.Type, out Instruction? instruction))
        {
            context.opcode = instruction.Opcode;
            context.opcodeSize = instruction.Size;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionIndex([NotNull] SyntaxParser.CpuInstructionIndexContext context)
    {
        if (Cpuid.EndsWith('0'))
        {
            if (context.register().Start.Type == SyntaxParser.X && s_m6800ZeroPageX.TryGetValue(context.Start.Type, out Instruction? instr))
            {
                context.operand = Services.Evaluator.SafeEvalNumber(context.expr(), sbyte.MinValue, byte.MaxValue, _truncateToDp, _dp);
                context.operandSize = 1;
                context.opcode = instr.Opcode;
                context.opcodeSize = instr.Opcode.Size();
                return true;
            }
            return false;
        }
        return VisitM6809Index(context.mnemonic(),
                             context,
                             false,
                             context.register().Start.Type,
                             0,
                             -1,
                             context.expr());
    }

    public override bool VisitCpuInstructionIndirectIndexed([NotNull] SyntaxParser.CpuInstructionIndirectIndexedContext context)
    {
        if (context.ix0 != null)
        {
            return false;
        }
        return VisitM6809Index(context.mnemonic(),
                                context,
                                false,
                                context.ix1.Type,
                                0,
                                -1,
                                context.expr());
    }

    public override bool VisitCpuInstructionIndirectIndexM6809([NotNull] SyntaxParser.CpuInstructionIndirectIndexM6809Context context)
    {
        return VisitM6809Index(context.mnemonic(),
                                context,
                                true,
                                context.register().Start.Type,
                                0,
                                -1,
                                context.expr()); 
    }

    public override bool VisitCpuInstructionRegisterOffset([NotNull] SyntaxParser.CpuInstructionRegisterOffsetContext context)
    {
        return VisitM6809Index(context.mnemonic(),
                               context, context.LeftSquare() != null,
                               context.ix.Start.Type,
                               0,
                               context.acc?.Type ?? -1);
    }

    public override bool VisitCpuInstructionRegisterList([NotNull] SyntaxParser.CpuInstructionRegisterListContext context)
    {
        if (Cpuid.EndsWith('0'))
        {
            return false;
        }
        SyntaxParser.RegisterContext[] regs = context.register();
        if (s_m6809IndexedX.ContainsKey(context.mnemonic().Start.Type))
        {
            if (regs.Length > 2 ||
                !s_accumulatorRegs.ContainsKey(regs[0].Start.Type) ||
                !s_indexRegs.ContainsKey(regs[1].Start.Type))
            {
                return false;
            }
            return VisitM6809Index(context.mnemonic(),
                                 context,
                                 false,
                                 regs[1].Start.Type,
                                 0,
                                 regs[0].Start.Type);
        }
        int mnemonic = context.mnemonic().Start.Type;
        bool isExchange = mnemonic.IsOneOf(SyntaxParser.EXG, SyntaxParser.TFR);
        if ((isExchange && regs.Length != 2) || regs.Length > 8)
        {
            return false;
        }
        Dictionary<int, int> lookup = isExchange ? s_exchangeModes : s_pushPullModes;
        int registers = byte.MinValue;
        HashSet<int> registersEvaled = new();
        for (int i = 0; i < regs.Length; i++)
        {
            int reg = regs[i].Start.Type;
            if (!lookup.TryGetValue(reg, out int postbyte) ||
                (!isExchange && context.mnemonic().Start.Text[^1] == regs[i].Start.Text[0]))
            {
                return false;
            }
            if ((registers & postbyte) == postbyte && !isExchange)
            {
                throw new Error(regs[i], "Duplicate register specified");
            }
            if (isExchange && i == 1)
            {
                registers <<= 4;
            }
            registers |= postbyte;
            registersEvaled.Add(reg);
        }
        if (_zeroPage.TryGetValue(mnemonic, out Instruction? instruction))
        {
            context.opcode = instruction.Opcode;
            context.operand = registers.AsPositive();
            context.operandSize = 1;
            context.opcodeSize = instruction.Size - 1;
            return true;
        }
        return false;
    }

    public override bool VisitCpuInstructionZPAbsolute([NotNull] SyntaxParser.CpuInstructionZPAbsoluteContext context)
    {
        if (context.Start.Type.IsOneOf(SyntaxParser.Tfradp, SyntaxParser.Tfrbdp))
        {
            if (context.bitwidthModifier() != null)
            {
                throw new Error(context.bitwidthModifier(), "Bitwidth modifier not valid for this directive");
            }
            Tfrdp(context.mnemonic(), context, context.expr());
            return true;
        }
        List<Dictionary<int, Instruction>> lookups = new() { _zeroPage, _absolute };
        return VisitVariableLength(lookups,
                                     context.mnemonic(),
                                     context,
                                     context.expr(),
                                     context.bitwidthModifier());
    }

    public override bool HandleDirective(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        if (Cpuid.EndsWith('9') && directive.Start.Type == SyntaxParser.Dp && operands?.expr().Length == 1)
        {
            _dp = Services.Evaluator.SafeEvalNumber(operands.expr()[0], sbyte.MinValue, byte.MaxValue) & 0xff;
            return true;
        }
        return false;
    }

    protected override void OnReset()
    {

    }

    protected override void OnSetCpu(string cpuid)
    {
        if (cpuid.EndsWith('0'))
        {
            _absolute = s_m6800Absolute;
            _all = s_m6800AllOpcodes;
            _immediate = s_m6800Immediate;
            _immediateAbs = s_m6800ImmAbs;
            _implied = s_m6800Implied;
            _zeroPage = s_m6800ZeroPage;
            _dp = -1;
            _truncateToDp = false;
        }
        else
        {
            _absolute = s_m6809Absolute;
            _all = s_m6809AllOpcodes;
            _immediate = s_m6809Immediate;
            _immediateAbs = s_m6809ImmAbs;
            _implied = s_m6809Implied;
            _zeroPage = s_m6809ZeroPage;
            _dp = 0;
            _truncateToDp = true;
        }
    }
}

