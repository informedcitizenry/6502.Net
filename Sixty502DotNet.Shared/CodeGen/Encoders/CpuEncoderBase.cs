//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class responsible for encoding parsed instructions into machine language
/// output.
/// </summary>
public abstract class CpuEncoderBase : SyntaxParserBaseVisitor<bool>
{
    /// <summary>
    /// Construct a new instance of the <see cref="CpuEncoderBase"/>.
    /// </summary>
    /// <param name="cpuid">The target architecture cpuid.</param>
    /// <param name="services">The shared <see cref="AssemblyServices"/>
    /// for assembly runtime.</param>
    protected CpuEncoderBase(string cpuid, AssemblyServices services)
    {
        InitialCpuid =
        Cpuid = cpuid;
        Services = services;
    }

    /// <summary>
    /// Analyze a list of <see cref="CodeAnalysisContext"/>s for possible
    /// recomendations on code refactoring. This method must be implemented.
    /// </summary>
    /// <param name="contexts">The list of <see cref="CodeAnalysisContext"/>s.
    /// </param>
    public abstract void Analyze(IList<CodeAnalysisContext> contexts);

    /// <summary>
    /// Decode an instruction or instructions within the given byte buffer. This method must
    /// be implemented by the derived class.
    /// </summary>
    /// <param name="bytes">The bytes of the instruction.</param>
    /// <param name="isSingleInstruction">Get whether this is a single instruction.</param>
    /// <param name="offset">The within the byte buffer to start the decoding.</param>
    /// <param name="programCounter">The program counter.</param>
    /// <returns></returns>
    protected abstract (string, int) OnDecode(byte[] bytes, bool isSingleInstruction, int offset, int programCounter);

    /// <summary>
    /// Handle the non-mnemonic directive. This method must be implemented.
    /// </summary>
    /// <param name="directive">The parsed context for the directive.</param>
    /// <param name="operands">The accompanying operands, which might be null.</param>
    /// <returns><c>true</c> if the encoder handles the directive, <c>false</c>
    /// otherwise.</returns>
    public abstract bool HandleDirective(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands);

    /// <summary>
    /// Decode a single instruction.
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="programCounter"></param>
    /// <returns>A string representation of the instruction (assembly code).</returns>
    public string DecodeInstruction(byte[] bytes, int programCounter)
    {
        return OnDecode(bytes, true, 0, programCounter).Item1;
    }

    /// <summary>
    /// Decode the machine language in the buffer into a string representation (assembly code).
    /// </summary>
    /// <param name="code">The code to decode.</param>
    /// <param name="offset">The offset into the code buffer.</param>
    /// <param name="origin">The origin (program counter). This is useful for properly
    /// <param name="noDisassembly">Suppression output of disassembly.</param>
    /// calculating the effective address of relative addressing instructions.</param>
    /// <returns>A string representation of the code.</returns>
    public string DecodeBuffer(byte[] code, int offset, int origin, bool noDisassembly)
    {
        if (code.Length <= offset)
        {
            return string.Empty;
        }
        int pc = origin;
        int linePc = pc;
        StringBuilder sb = new();
        sb.AppendLine($"        * = ${pc:x4}");
        try
        {
            while (offset < code.Length)
            {
                int startOffset = offset;
                (string disasm, int size) = OnDecode(code, false, offset, pc);
                pc += size;
                offset += size;
                if (size > 0)
                {
                    if (!noDisassembly)
                    {
                        sb.Append($".{linePc:x4} ");
                        for (int i = 0; i < 7; i++)
                        {
                            if (i < size)
                            {
                                sb.Append($"{code[startOffset + i]:x2} ");
                            }
                            else
                            {
                                sb.Append("   ");
                            }
                        }
                        sb.AppendLine(disasm);
                    }
                    else
                    {
                        sb.AppendLine($"{disasm} // ${linePc:x4}");
                    }
                    linePc = pc;
                    continue;
                }
                int bytesOnLine = 0;
                while (offset < code.Length && size == 0)
                {
                    if (bytesOnLine > 0)
                    {
                        sb.Append(',');
                    }
                    else
                    {
                        sb.Append($".{linePc:x4} .byte");
                    }
                    sb.Append($" ${code[offset++]:x2}");
                    pc++;
                    if (++bytesOnLine == 8)
                    {
                        sb.AppendLine();
                        bytesOnLine = 0;
                        linePc = pc;
                    }
                    if (offset == code.Length) break;
                    (disasm, size) = OnDecode(code, false, offset, pc);
                }
                sb.AppendLine();
                linePc = pc;
            }
            return sb.ToString();
        }
        catch (Exception)
        {
            return $"We had an error at: Offset=0x{offset:x16}, Program Counter=0x{pc:x16}";

        }
    }

    protected bool EmitOpcode(int opcodeHex, SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext operand)
    {
        return EmitOpcode(opcodeHex, context, null, new SyntaxParser.ExprContext[] { operand }, 1);
    }

    protected bool EmitOpcode(int opcodeHex, SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext operand, int operandSize)
    {
        return EmitOpcode(opcodeHex, context, null, new SyntaxParser.ExprContext[] { operand }, operandSize);
    }

    protected bool EmitOpcode(int opcodeHex, SyntaxParser.CpuInstructionContext context, SyntaxParser.BitwidthModifierContext? bitwidth, SyntaxParser.ExprContext operand, int operandSize)
    {
        return EmitOpcode(opcodeHex, context, bitwidth, new SyntaxParser.ExprContext[] { operand }, operandSize);
    }

    protected bool EmitOpcode(int opcodeHex, SyntaxParser.CpuInstructionContext context)
    {
        return EmitOpcode(opcodeHex, context, null, Array.Empty<SyntaxParser.ExprContext>());
    }

    protected bool EmitOpcode(int opcodeHex, SyntaxParser.CpuInstructionContext context, SyntaxParser.BitwidthModifierContext? bitwidth, SyntaxParser.ExprContext[] operands, params int[] sizes)
    {
        if (opcodeHex == Bad)
        {
            return false;
        }
        context.opcode = opcodeHex;
        context.opcodeSize = opcodeHex.Size();
        int operandSize = 0;
        long operandValue = 0;
        for (int i = operands.Length - 1; i >= 0; i--)
        {
            double minValue, maxValue;
            int size = sizes[i];
            if (bitwidth != null)
            {
                size = BitwidthSize(bitwidth);
            }
            operandSize += size;
            switch (size)
            {
                case 2: minValue = short.MinValue; maxValue = ushort.MaxValue; break;
                case 3: minValue = Int24.MinValue; maxValue = UInt24.MaxValue; break;
                default: minValue = sbyte.MinValue; maxValue = byte.MaxValue; break;
            }
            operandValue <<= size * 8;
            if (i > 0 || context is not SyntaxParser.CpuInstructionImmmediateContext)
            {
                operandValue |= (long)Services.Evaluator.SafeEvalAddress(operands[i]);
                if (operandValue.Size() > 2 && !Services.State.PassNeeded)
                {
                    return false;
                }
            }
            else
            {
                operandValue |= (long)Services.Evaluator.SafeEvalNumber(operands[i], minValue, maxValue, _truncateToDp, _dp);
            }
        }
        context.operand = operandValue;
        context.operandSize = operandSize;
        return true;
    }

    protected bool EmitOpcodeVariant(int zeroPageHex, int absoluteHex, int longHex, SyntaxParser.CpuInstructionContext context, SyntaxParser.BitwidthModifierContext? bitwidth, SyntaxParser.ExprContext operand)
    {
        int operandValue;
        bool evalAsAddress = context is not SyntaxParser.CpuInstructionImmmediateContext;
        if (evalAsAddress)
        {
            operandValue = Services.Evaluator.SafeEvalAddress(operand, _truncateToDp, _dp);
        }
        else
        {
            operandValue = Services.Evaluator.SafeEvalNumber(operand, Int24.MinValue, UInt24.MaxValue);
        }
        if (Services.State.PassNeeded && absoluteHex == Bad)
        {
            context.operandSize =
            context.opcodeSize = 1;
            return true;
        }
        int bitwidthSize = BitwidthSize(bitwidth);
        int operandSize = operandValue.Size();
        if (bitwidthSize > 0)
        {
            if (operandSize > bitwidthSize && !Services.State.PassNeeded)
            {
                throw new Error(operand, "Operand value exceeds maximum due to bitwidth modifier constraints");
            }
            operandSize = bitwidthSize;
        }

        if (operandSize > 1 || zeroPageHex == Bad)
        {
            if (operandSize == 1) operandSize = 2;
            if (operandSize > 2 || absoluteHex == Bad)
            {
                if (operandSize > 3)
                {
                    return Services.State.PassNeeded;
                }
                operandSize = 3;
            }
        }
        int opcodeHex = operandSize switch
        {
            2 => absoluteHex,
            3 => longHex,
            _ => zeroPageHex,
        };
        if (opcodeHex == Bad)
        {
            return false;
        }
        context.opcode = opcodeHex;
        context.opcodeSize = opcodeHex.Size();
        context.operand = operandValue;
        context.operandSize = operandSize;
        return true;
    }

    protected bool EmitRelative(int opcode, SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext expr, int operandSize)
    {
        int operand = Services.Evaluator.SafeEvalAddress(expr);
        int offs = operand - (Services.State.Output.LogicalPC + opcode.Size() + operandSize);
        if ((operandSize == 1 && (offs < sbyte.MinValue || offs > sbyte.MaxValue)) ||
            (operandSize == 2 && (offs < short.MinValue || offs > short.MaxValue)))
        {
            if (!Services.State.PassNeeded)
            {
                throw new Error(expr, "Relative offset too far");
            }
        }
        context.opcode = opcode;
        context.opcodeSize = opcode.Size();
        context.operand = offs;
        context.operandSize = operandSize;
        return true;
    }

    protected bool EmitRelative(M6xxOpcode opcode, SyntaxParser.CpuInstructionContext context, SyntaxParser.ExprContext expr)
    {
        int opcodeHex = opcode.relative != Bad ? opcode.relative : opcode.relativeAbs;
        return EmitRelative(opcodeHex, context, expr, opcodeHex == opcode.relative ? 1 : 2);
    }

    /// <summary>
    /// Perform analysis on successive instructions for call/return operations,
    /// and note where they can be simplified.
    /// </summary>
    /// <param name="first">The first context.</param>
    /// <param name="second">The second context.</param>
    /// <param name="callOpcode">The architecture's call opcode.</param>
    /// <param name="retOpcode">The architecture's return opcode.</param>
    protected void AnalyzeCallReturn(CodeAnalysisContext first, CodeAnalysisContext second, int callOpcode, int retOpcode)
    {
        if (Services.DiagnosticOptions.WarnSimplifyCallReturn)
        {
            int opcode0 = first.ObjectCode.First(), opcode1 = second.ObjectCode.First();
            if (opcode0 == callOpcode && opcode1 == retOpcode && second.Offset == first.ObjectCode.Count + first.Offset)
            {
                first.Report = "Return following subroutine call can be simplified to a jump instruction";
            }
        }
    }

    /// <summary>
    /// Handle the reset method. This method must be implemented.
    /// </summary>
    protected abstract void OnReset();

    /// <summary>
    /// Handle the set CPU method. This method must be implemented.
    /// </summary>
    /// <param name="cpuid">The cpuid to set.</param>
    protected abstract void OnSetCpu(string cpuid);

    /// <summary>
    /// Get the actual size in bytes of the parsed bitwidth specifier.
    /// </summary>
    /// <param name="bitwidthModifier">The parsed bitwidth specifier.</param>
    /// <returns>The value in bytes of the bitwidth specifier.</returns>
    /// <exception cref="Error"></exception>
    protected static int BitwidthSize(SyntaxParser.BitwidthModifierContext? bitwidthModifier)
    {
        if (bitwidthModifier != null)
        {
            string text = bitwidthModifier.DecLiteral().Symbol.Text;
            if (text.Equals("8") || text.Equals("16") || text.Equals("24"))
            {
                return int.Parse(text) / 8;
            }
            throw new Error(bitwidthModifier, "Invalid bitwidth specifier");
        }
        return 0;
    }

    /// <summary>
    /// Set the cpuid for the current encoder.
    /// </summary>
    /// <param name="cpuid">The cpuid.</param>
    public void SetCpu(string cpuid)
    {
        Cpuid = cpuid;
        OnSetCpu(cpuid);
    }

    /// <summary>
    /// Reset the state of the encoder.
    /// </summary>
    public void Reset()
    {
        SetCpu(InitialCpuid);
        OnReset();
    }

    /// <summary>
    /// Get the shared <see cref="AssemblyServices"/> for assembly runtime.
    /// </summary>
    protected AssemblyServices Services { get; init; }

    /// <summary>
    /// Get initial cpuid for the configured encoder.
    /// </summary>
    protected string InitialCpuid { get; init; }


    /// <summary>
    /// Get the current cpuid for the configured encoder.
    /// </summary>
    public string Cpuid { get; private set; }

    protected int _dp;
    protected bool _truncateToDp;

    public const int Bad = -1;
}

