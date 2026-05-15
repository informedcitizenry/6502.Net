// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Encode;

using static EncodeUtil;

// ReSharper disable once InconsistentNaming
public static partial class M65xxEncoder
{
    public static string Decode
    (
        ReadOnlySpan<byte> bytes, 
        Cpu cpu, 
        bool m16, 
        bool x16, 
        ref int programCounter
    )
    {
        int opcodeHex = bytes[0];
        if (cpu == Cpu.M65 && opcodeHex is 0x42 or 0xea)
        {
            var i = 0;
            do
            {
                if (++i >= bytes.Length) break;
                opcodeHex |= bytes[i] << (i * 8);
            }
            while (bytes[i] is 0x42 or 0xea);
        }
        if (!TryGetDecodedInstruction(opcodeHex, cpu, out var instruction) ||
            instruction == null ||
            instruction.Size > bytes.Length)
        {
            programCounter++;
            return $".byte ${bytes[0]:x2}";
        }
        var operandVals = new object[instruction.Operands.Length];
        var opcodeSize = ((long)instruction.Opcode).Size();
        var offset = opcodeSize;
        for (var i = 0; i < instruction.Operands.Length; i++)
        {
            var operandSize = instruction.Operands[i];
            var operandVal = 0;
            for (var b = 0; b < operandSize && offset + b < bytes.Length; b++)
            {
                operandVal |= bytes[offset + b] << (8 * b);
            }
            offset += operandSize;
            operandVals[i] = operandVal;
        }
        if (instruction.IsRelative || instruction.Is16BitRelative)
        {
            var offs = (int)operandVals[^1];
            int maxValue = instruction.Is16BitRelative ? short.MaxValue : sbyte.MaxValue;
            var negativeMaxValue = instruction.Is16BitRelative ? ushort.MaxValue + 1 : byte.MaxValue + 1;
            if (offs > maxValue) offs -= negativeMaxValue;
            var fromPc = cpu == Cpu.M65Ce02 ? programCounter + 2 : programCounter + instruction.Size;
            operandVals[^1] = ((long)(fromPc + offs)).AsPositive();
            if (bytes.Length >= 5 && bytes[offset] == 0x4c)
            {
                // we are at a pseudo jmp
                var jmp = bytes[offset + 1] + 256 * bytes[offset + 2];
                var pseudo = string.Format(instruction.DisassemblyFormat, operandVals[0]);
                programCounter += 5;
                return $"{pseudo}:jmp ${jmp:x4} ";
            }
        }
        programCounter += instruction.Size;
        if (!ImmediateIs16(cpu, m16, x16, bytes[0]) || bytes.Length < 3 || operandVals.Length > 1)
        {
            return string.Format(instruction.DisassemblyFormat, operandVals);
        }
        programCounter++;
        var fmt = instruction.DisassemblyFormat.Replace("x2", "x4");
        var imm16 = (int)operandVals[0] + bytes[2] * 256;
        return string.Format(fmt, imm16);
    }
    
    public static bool Encode(CpuInstructionStatement statement, AssemblyState state)
    {
        var mnemonic = statement.Mnemonic.Type;
        if (mnemonic == TokenType.Bra && 
            state.AssemblyOptions.BraFor6502 && 
            state.Cpu is Cpu.M6502 or Cpu.M6502I)
        {
            mnemonic = TokenType.Bvc;
        }
        if (state.AssemblyOptions.PseudoBranches6502 &&
            s_6502PseudoRelative.TryGetValue(mnemonic, out var pseudoHex))
        {
            if (statement.Operand.Type != OperandType.Address)
            {
                return false;
            }
            return EncodePseudoRelative
            (
                state, 
                statement.Mnemonic.Type, 
                pseudoHex, 
                statement.Operand.Expressions[0]
            );
        }
        if (!TryGetOpcode(mnemonic, state.Cpu, out var opcode))
        {
            return false;
        }
        var coercedSize = statement.Operand.CoercedSize;
        return statement.Operand.Type switch
        {
            OperandType.Implied => EncodeImplied(state, opcode.implied, Bad),
            OperandType.Register =>
                statement.Operand.Registers[0].Type == TokenType.A &&
                EncodeImplied(state, opcode.accumulator, Bad),
            OperandType.Address when coercedSize == 1 && opcode.relative != Bad && opcode.relativeAbsolute == Bad => 
            EncodeRelative
            (
                state, 
                opcode, 
                Bad, 
                statement.Operand.Expressions[0]
            ),
            OperandType.Address when coercedSize == 2 && opcode.relative == Bad && opcode.relativeAbsolute != Bad 
                => EncodeRelative
            (
                state, 
                opcode, 
                Bad, 
                statement.Operand.Expressions[0]
            ),
            OperandType.Address when coercedSize == 1 => EncodeSingleOperand
            (
                state,
                opcode.zeroPage,
                statement.Operand.Expressions[0],
                1
            ),
            OperandType.Address when coercedSize == 2 => EncodeSingleOperand
            (
                state,
                opcode.absolute,
                statement.Operand.Expressions[0],
                2
            ),
            OperandType.Address when coercedSize == 3 => EncodeSingleOperand
            (
                state,
                opcode.longAddress,
                statement.Operand.Expressions[0],
                3
            ),
            OperandType.Address when opcode.relative != Bad || opcode.relativeAbsolute != Bad
            => EncodeRelative
            (
                state, 
                opcode, 
                Bad, 
                statement.Operand.Expressions[0]
            ),
            OperandType.Address => EncodeVariantOperand
            (
                state,
                opcode.zeroPage,
                opcode.absolute,
                opcode.longAddress,
                Bad,
                statement.Operand.Expressions[0]
            ),
            OperandType.Immediate when coercedSize < 2 && state.AutosizeRegisters && 
                     statement.Mnemonic.Type is TokenType.Rep or TokenType.Sep
            => EncodeRepSep
            (
                state,
                statement.Mnemonic.Type,
                opcode.immediate,
                statement.Operand.Expressions[0]
            ),
            OperandType.Immediate when coercedSize == 1 => EncodeSingleOperand
            (
                state,
                opcode.immediate,
                statement.Operand.Expressions[0],
                1
            ),
            OperandType.Immediate when coercedSize == 2 => EncodeImmediate16
            (
                state,
                opcode.immediate,
                opcode.immediate16Bit,
                statement.Operand.Expressions[0]
            ),
            OperandType.Immediate => EncodeImmediate
            (
                state,
                opcode.immediate,
                opcode.immediate16Bit,
                statement.Operand.Expressions[0]
            ),
            OperandType.ImmediateBranch when coercedSize == 1 => EncodeHuImmediateZeroPageOperand
            (
                state,
                opcode.bitTest,
                statement.Operand.Expressions
            ), 
            OperandType.ImmediateBranch when coercedSize == 2 => EncodeHuImmediateAbsoluteOperand
            (
                state,
                opcode.bitTestBranch,
                statement.Operand.Expressions
            ),
            OperandType.ImmediateBranch => EncodeHuImmediateVariantOperand
            (
                state,
                opcode.bitTest,
                opcode.bitTestBranch,
                statement.Operand.Expressions
            ),
            OperandType.ImmediateBranchIndexed when coercedSize == 1 
                => EncodeHuImmediateZeroPageOperand
            (
                state,
                opcode.immediateZeroPageIndexed,
                statement.Operand.Expressions
            ),
            OperandType.ImmediateBranchIndexed when coercedSize == 2
                => EncodeHuImmediateAbsoluteOperand
            (
                state,
                opcode.immediateAbsoluteIndexed,
                statement.Operand.Expressions
            ),
            OperandType.ImmediateBranchIndexed => EncodeHuImmediateVariantOperand
            (
                state,
                opcode.immediateZeroPageIndexed,
                opcode.immediateAbsoluteIndexed,
                statement.Operand.Expressions
            ),
            OperandType.Indexed when statement.Operand.CoercedSize > 0 => EncodeIndexedOperand
            (
                state,
                opcode,
                statement.Operand.Registers[0],
                statement.Operand.Expressions[0],
                statement.Operand.CoercedSize
            ),
            OperandType.Indexed => EncodeVariantIndexedOperand
            (
                state,
                opcode,
                statement.Operand.Registers[0],
                statement.Operand.Expressions[0]
            ),
            OperandType.IndexedIndirect when statement.Operand.CoercedSize > 0 => EncodeIndexedIndirectOperand //IndexedIndirectZeroPage => EncodeIndexedIndirectOperand
            (
                state,
                opcode,
                statement.Operand,
                statement.Operand.CoercedSize
            ),
            OperandType.IndexedIndirect => EncodeIndexedIndirectVariantOperand
            (
                state,
                opcode,
                statement.Operand
            ),
            OperandType.IndexedIndirectIndexed => EncodeIndexedIndirectIndexed
            (
                state,
                opcode,
                statement.Operand.Registers[0],
                statement.Operand.Expressions[0]
            ),
            OperandType.Indirect when statement.Operand.CoercedSize == 1 => EncodeSingleOperand
            (
                state,
                opcode.indirectZeroPage,
                statement.Operand.Expressions[0],
                1
            ),
            OperandType.Indirect when statement.Operand.CoercedSize == 2 => EncodeSingleOperand
            (
                state,
                opcode.indirectAbsolute,
                statement.Operand.Expressions[0],
                2
            ),
            OperandType.Indirect => EncodeVariantOperand
            (
                state,
                opcode.indirectZeroPage,
                opcode.indirectAbsolute,
                Bad,
                Bad,
                statement.Operand.Expressions[0]
            ),
            OperandType.IndirectIndexed => EncodeIndirectIndexedOperand
            (
                state,
                opcode,
                statement.Operand.Registers[0],
                statement.Operand.Expressions[0]
            ),
            OperandType.IndirectLong when statement.Operand.CoercedSize == 1 => EncodeSingleOperand
            (
                state,
                opcode.indirectLong,
                statement.Operand.Expressions[0],
                1
            ),
            OperandType.IndirectLong when statement.Operand.CoercedSize == 2 => EncodeSingleOperand
            (
                state,
                opcode.indirectLongAbsolute,
                statement.Operand.Expressions[0],
                2
            ),
            OperandType.IndirectLong => EncodeVariantOperand
            (
                state,
                opcode.indirectLong,
                opcode.indirectLongAbsolute,
                Bad,
                Bad,
                statement.Operand.Expressions[0]
            ),
            OperandType.IndirectLongIndexed => EncodeSingleOperand
            (
                state,
                opcode.indirectLongIndexed,
                statement.Operand.Expressions[0],
                1
            ),
            OperandType.IndirectLongZ => EncodeSingleOperand
            (
                state,
                opcode.indirectLongZ,
                statement.Operand.Expressions[0],
                1
            ),
            OperandType.TwoExpression => EncodeTwoExpressionOperands
            (
                state,
                opcode,
                statement.Operand.Expressions
            ),
            OperandType.ThreeExpression => EncodeThreeExpressionsOperands
            (
                state,
                opcode,
                statement.Operand.Expressions
            ),
            _ => false
        };
    }

    public static string Analyze
    (
        AssemblyOptions options, 
        CodeAnalysisContext context, 
        CodeAnalysisContext? context2
    )
    {
        if (options.WarnJumpBug &&
            context.Cpuid is Cpu.M6502 or Cpu.M6502I && 
            context.Statement.Mnemonic.Type == TokenType.Jmp && 
            context.Statement.Operand.Type == OperandType.Indirect &&
            context.ObjectCode.Count > 1 + context.Offset && 
            context.ObjectCode[context.Offset + 1] == 0xff)
        {
            return "Indirect jump at page boundary has a defect";
        }
        if (options.WarnSimplifyCallReturn && 
            context.Statement.Mnemonic.Type is TokenType.Jsr or TokenType.Jsl &&
            context2?.Statement.Mnemonic.Type is TokenType.Rts or TokenType.Rtl)
        {
            return "Return following subroutine call can be simplified to a jump instruction";
        }
        if (options.WarnAmbiguousZp && IsAmbiguousSize(context))
        {
            return "Address size is ambiguous between direct/zero page and absolute addressing. Consider coercing the operand size using `[8]` or `[16]` specifier";
        }
        return string.Empty;
    }
    
    private static bool EncodePseudoRelative
    (
        AssemblyState state, 
        TokenType mnemonicType, 
        int pseudoHex, 
        Expression operandExpression
    )
    {
        var evaluator = new Evaluator(state);
        var size = 1;
        var address = (int)evaluator.EvalInteger(operandExpression, short.MinValue, ushort.MaxValue);
        var offs = address - (state.Output.ProgramCounter + 2);
        if (offs is < sbyte.MinValue or > sbyte.MaxValue)
        {
            var jmp = 0x4c;
            pseudoHex = s_6502PseudoToReal[mnemonicType] + 256 * 3;
            offs = (address & 0xffff) * 256 + jmp;
            size = 3;
        }
        state.Output.EmitValue(pseudoHex, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(offs, size, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeRepSep
    (
        AssemblyState state,
        TokenType mnemonic,
        int opcodeHex, 
        Expression operand
    )
    {
        if (opcodeHex == Bad) return false; // this...shouldn't happen?
        var evaluator = new Evaluator(state);
        var val = (int)evaluator.EvalInteger(operand, sbyte.MinValue, byte.MaxValue);
        if ((val & 0b0010_0000) != 0)
        {
            state.M16 = mnemonic == TokenType.Rep;
        }

        if ((val & 0b0001_0000) != 0)
        {
            state.X16 = mnemonic == TokenType.Rep;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValue(val, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeImmediate
    (
        AssemblyState state,
        int immediate8, 
        int immediate16, 
        Expression operand
    )
    {
        var size = 1;
        var opcodeHex = immediate8 == Bad ? immediate16 : immediate8;
        if (immediate16 != Bad || ImmediateIs16(state.Cpu, state.M16, state.X16, opcodeHex))
        {
            size = 2;
        }
        return opcodeHex != Bad && EncodeSingleOperand(state, opcodeHex, operand, size);
    }
    
    private static bool EncodeImmediate16
    (
        AssemblyState state,
        int immediate8,
        int immediate16,
        Expression operand
    )
    {
        var opcodeHex = ImmediateIs8(state.Cpu, state.M16, state.X16, immediate8) ? immediate8 : immediate16;
        return opcodeHex != Bad && EncodeSingleOperand(state, opcodeHex, operand, 2);
    }
    
    
    private static bool ImmediateIs8(Cpu cpu, bool m16, bool x16, int opcodeHex)
    {
        return cpu switch
        {
            Cpu.M65816 when s_accumulatorCodes.Contains(opcodeHex) => !m16,
            Cpu.M65816 when s_indexCodes.Contains(opcodeHex) => !x16,
            _ => false
        };
    }

    private static bool ImmediateIs16(Cpu cpu, bool m16, bool x16, int opcodeHex)
    {
        return cpu switch
        {
            Cpu.M65816 when s_accumulatorCodes.Contains(opcodeHex) => m16,
            Cpu.M65816 when s_indexCodes.Contains(opcodeHex) => x16,
            _ => false
        };
    }

    private static bool EncodeIndexedOperand
    (
        AssemblyState state, 
        M6xxOpcode opcode, 
        Token register, 
        Expression operand,
        int size
    )
    {
        var opcodeHex = Bad;
        switch (register.Type)
        {
            case TokenType.X:
                opcodeHex = size switch
                {
                    1 => opcode.zeroPageX,
                    2 => opcode.absoluteX,
                    _ => opcode.longX
                };
                break;
            case TokenType.Y:
                opcodeHex = size switch
                {
                    1 => opcode.zeroPageY,
                    2 => opcode.absoluteY,
                    _ => opcodeHex
                };
                break;
            case TokenType.S:
                if (size == 1) opcodeHex = opcode.stackRelative;
                break;
            default:
                return false;
        }
        return EncodeSingleOperand(state, opcodeHex, operand, size);
    }
    
    private static bool EncodeVariantIndexedOperand
    (
        AssemblyState state,
        M6xxOpcode opcode,
        Token register,
        Expression operand
    )
    {
        var zeroPageHex = Bad;
        var absoluteHex = Bad;
        var longHex = Bad;
        switch (register.Type)
        {
            case TokenType.S:
                zeroPageHex = opcode.stackRelative;
                break;
            case TokenType.X:
                zeroPageHex = opcode.zeroPageX;
                absoluteHex = opcode.absoluteX;
                longHex = opcode.longX;
                break;
            case TokenType.Y:
                zeroPageHex = opcode.zeroPageY;
                absoluteHex = opcode.absoluteY;
                break;
        }
        return EncodeVariantOperand(state, zeroPageHex, absoluteHex, longHex, Bad, operand);
    }

    private static bool EncodeIndexedIndirectVariantOperand
    (
        AssemblyState state,
        M6xxOpcode opcode,
        Operand operand
    )
    {
        var zeroPageHex = Bad;
        var absoluteHex = Bad;
        if (operand.Registers.Count > 1 && operand.Registers[1].Type == TokenType.Y)
        {
            zeroPageHex = operand.Registers[0].Type switch
            {
                TokenType.S => opcode.indirectStackRelativeIndexed,
                TokenType.Sp => opcode.indirectStackPointerIndexed,
                _ => zeroPageHex
            };
        }
        else if (operand.Registers[0].Type == TokenType.X)
        {
            zeroPageHex = opcode.indexedZeroPageIndirect;
            absoluteHex = opcode.indexedAbsoluteIndirect;
        }
        return EncodeVariantOperand
        (
            state, 
            zeroPageHex, 
            absoluteHex, 
            Bad,
            Bad,
            operand.Expressions[0]
        );
    }
    
    private static bool EncodeIndexedIndirectOperand
    (
        AssemblyState state, 
        M6xxOpcode opcode, 
        Operand operand, 
        int size
    )
    {
        var opcodeHex = Bad;
        if (operand.Registers.Count > 1)
        {
            if (operand.Registers[1].Type != TokenType.Y || size != 1) return false;
            opcodeHex = operand.Registers[0].Type switch
            {
                TokenType.S => opcode.indirectStackRelativeIndexed,
                TokenType.Sp => opcode.indirectStackPointerIndexed,
                _ => opcodeHex
            };
        }
        else
        {
            if (operand.Registers[0].Type != TokenType.X) return false;
            opcodeHex = size switch
            {
                1 => opcode.indexedZeroPageIndirect,
                2 => opcode.indexedAbsoluteIndirect,
                _ => opcodeHex
            };
        }
        return EncodeSingleOperand(state, opcodeHex, operand.Expressions[0], size);
    }

    private static bool EncodeIndirectIndexedOperand
    (
        AssemblyState state, 
        M6xxOpcode opcode, 
        Token register,
        Expression operand
    )
    {
        return register.Type switch
        {
            TokenType.Y => EncodeSingleOperand(state, opcode.indirectIndexed, operand, 1),
            TokenType.Z => EncodeSingleOperand(state, opcode.indirectZ, operand, 1),
            _ => false
        };
    }

    private static bool EncodeIndexedIndirectIndexed
    (
        AssemblyState state,
        M6xxOpcode opcode,
        Token register,
        Expression operand
    )
    {
        return register.Type switch
        {
            TokenType.S => EncodeSingleOperand(state, opcode.indirectStackRelativeIndexed, operand, 1),
            TokenType.Sp => EncodeSingleOperand(state, opcode.indirectStackPointerIndexed, operand, 1),
            _ => false
        };
    }
    
    private static bool EncodeTwoExpressionOperands
    (
        AssemblyState state, 
        M6xxOpcode opcode, 
        IList<Expression> operands
    )
    {
        if (opcode.bitTest != Bad)
            return EncodeBitTestOperand(state, opcode.bitTest, operands);
        if (opcode.blockMove == Bad)
            return false;
        var evaluator = new Evaluator(state);
        var operand1 = evaluator.EvalPagedBanked(operands[0], sbyte.MinValue, byte.MaxValue);
        var operand2 = evaluator.EvalPagedBanked(operands[1], sbyte.MinValue, byte.MaxValue);
        
        state.Output.EmitValue(opcode.blockMove, ByteOrder.LittleEndian);
        state.Output.EmitValue(operand2, ByteOrder.LittleEndian);
        state.Output.EmitValue(operand1, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeThreeExpressionsOperands    
    (
        AssemblyState state, 
        M6xxOpcode opcode, 
        IList<Expression> operands
    )
    {
        if (opcode.bitTestBranch != Bad)
            return EncodeBitTestBranchOperand(state, opcode.bitTestBranch, operands);
        if (opcode.threeOperand == Bad) return false;
        var evaluator = new  Evaluator(state);
        var operand1 = evaluator.EvalPagedBanked(operands[0], short.MinValue, ushort.MaxValue);
        var operand2 = evaluator.EvalPagedBanked(operands[1], short.MinValue, ushort.MaxValue);
        var operand3 = evaluator.EvalPagedBanked(operands[2], short.MinValue, ushort.MaxValue);
        state.Output.EmitValue(opcode.threeOperand, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(operand1, 2, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(operand2, 2, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(operand3, 2, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeBitTestOperand(AssemblyState state, int opcodeHex, IList<Expression> operands)
    {
        EnforceBit(operands[0]);
        var evaluator = new Evaluator(state);
        var bitVal = operands[0].Value.AsInt();
        var testVal = evaluator.EvalPagedBanked(operands[1], sbyte.MinValue, sbyte.MaxValue);
        opcodeHex |= (int)bitVal * 0x10;
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValue(testVal, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeBitTestBranchOperand
    (
        AssemblyState state, 
        int opcodeHex, 
        IList<Expression> operands
    )
    {
        EnforceBit(operands[0]);
        var evaluator = new Evaluator(state);
        var bitVal = operands[0].Value.AsInt();
        var testVal = evaluator.EvalPagedBanked(operands[1], sbyte.MinValue, byte.MaxValue);
        var branchVal = evaluator.EvalInteger(operands[2], short.MinValue, ushort.MaxValue);
        var offs = branchVal - (state.Output.ProgramCounter + 3);
        if (offs is < -128 or > 127)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operands[2]);
            }
            state.PassNeeded = true;
        }
        opcodeHex |= (int)bitVal * 0x10;
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValue(testVal, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(offs, 1, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeHuImmediateVariantOperand
    (
        AssemblyState state,
        int zeroPageHex,
        int absoluteHex,
        IList<Expression> operands)
    {
        var evaluator = new Evaluator(state);
        var addrVal = evaluator.EvalPagedBanked(operands[1], short.MinValue, ushort.MaxValue);
        var opcodeHex = zeroPageHex;
        var size = 1;
        if (addrVal is < -128 or > 127 || opcodeHex == Bad)
        {
            size = 2;
            opcodeHex = absoluteHex;
        }
        if (opcodeHex == Bad)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                return false;
            }
            state.PassNeeded = true;
            opcodeHex = zeroPageHex;
        }
        return EncodeHuImmediateOperand(state, opcodeHex, operands[0], addrVal, size);
    }

    private static bool EncodeHuImmediateZeroPageOperand
    (
        AssemblyState state,
        int opcodeHex,
        IList<Expression> operands
    )
    {
        var evaluator = new Evaluator(state);
        return EncodeHuImmediateOperand
        (
            state, 
            opcodeHex, 
            operands[0], 
            evaluator.EvalInteger(operands[1], sbyte.MinValue, byte.MaxValue),
            1
        );
    }
    
    private static bool EncodeHuImmediateAbsoluteOperand
    (
        AssemblyState state,
        int opcodeHex,
        IList<Expression> operands
    )
    {
        var evaluator = new Evaluator(state);
        return EncodeHuImmediateOperand
        (
            state, 
            opcodeHex, 
            operands[0], 
            evaluator.EvalInteger(operands[1], short.MinValue, ushort.MaxValue),
            2
        );
    }

    private static bool EncodeHuImmediateOperand
    (
        AssemblyState state,
        int opcodeHex,
        Expression immediate,
        long address,
        int size
    )
    {
        if (opcodeHex == Bad) return false;
        var evaluator = new Evaluator(state);
        var immVal = evaluator.EvalInteger(immediate, sbyte.MinValue, byte.MaxValue);
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValue(immVal, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(address, size, ByteOrder.LittleEndian);
        return true;
    }

        private static bool IsAmbiguousSize(CodeAnalysisContext context)
    {
        var code = context.ObjectCode;
        if (context.Statement.Operand.CoercedSize > 0 ||
            code.Count < 2 || 
            !TryGetOpcode(context.Statement.Mnemonic.Type, context.Cpuid, out var opcode))
        {
            return false;
        }
        var operand = context.Statement.Operand.Type;
        var zpHex = operand switch
        {
            OperandType.Address => opcode.zeroPage,
            OperandType.Immediate when context.Cpuid == Cpu.M65Ce02 => opcode.immediate,
            OperandType.ImmediateBranch => opcode.bitTest,
            OperandType.ImmediateBranchIndexed => opcode.immediateZeroPageIndexed,
            OperandType.Indexed when context.Statement.Operand.Registers[0].Type == TokenType.X => opcode.zeroPageX,
            OperandType.Indexed => opcode.zeroPageY,
            OperandType.IndexedIndirect when context.Statement.Operand.Registers[0].Type == TokenType.X => opcode.indexedZeroPageIndirect,
            OperandType.Indirect when context.Cpuid is not Cpu.M6502 and Cpu.M6502I => opcode.indirectZeroPage,
            _ => Bad
        };
        var absHex = operand switch
        {
            OperandType.Address => opcode.absolute,
            OperandType.Immediate when context.Cpuid == Cpu.M65Ce02 => opcode.immediate16Bit,
            OperandType.ImmediateBranch => opcode.bitTestBranch,
            OperandType.ImmediateBranchIndexed => opcode.immediateAbsoluteIndexed,
            OperandType.Indexed when context.Statement.Operand.Registers[0].Type == TokenType.X => opcode.absoluteX,
            OperandType.Indexed => opcode.absoluteY,
            OperandType.IndexedIndirect when context.Statement.Operand.Registers[0].Type == TokenType.X => opcode.indexedAbsoluteIndirect,
            OperandType.Indirect when context.Cpuid is not Cpu.M6502 and Cpu.M6502I => opcode.indirectAbsolute,
            _ => Bad
        };
        return zpHex != Bad && absHex != Bad && code.Count - zpHex.Size() < 2;
    }
    
    private static bool TryGetOpcode(TokenType mnemonic, Cpu cpu, out M6xxOpcode opcode)
    {
        return cpu switch
        {
            Cpu.M6502I => s_6502iOpcodes.TryGetValue(mnemonic, out opcode),
            Cpu.M65816 => s_65816Opcodes.TryGetValue(mnemonic, out opcode),
            Cpu.M65C02 => s_65c02Opcodes.TryGetValue(mnemonic, out opcode),
            Cpu.M65Ce02 => s_65ce02Opcodes.TryGetValue(mnemonic, out opcode),
            Cpu.C64Dtv2 => s_c64dtvOpcodes.TryGetValue(mnemonic, out opcode),
            Cpu.HuC6280 => s_huc6280Opcodes.TryGetValue(mnemonic, out opcode),
            Cpu.M65 => s_m65Opcodes.TryGetValue(mnemonic, out opcode),
            Cpu.R65C02 => s_r65c02Opcodes.TryGetValue(mnemonic, out opcode),
            _ => s_6502Opcodes.TryGetValue(mnemonic, out opcode)
        };
    }

    private static bool TryGetDecodedInstruction(int hex, Cpu cpu, out DecodedInstruction? decoded)
    {
        return cpu switch
        {
            Cpu.C64Dtv2 => s_c64dtvDecoded.TryGetValue(hex, out decoded) ||
                          TryGetDecodedInstruction(hex, Cpu.M6502, out decoded),
            Cpu.M65 => s_m65Decoded.TryGetValue(hex, out decoded) ||
                       TryGetDecodedInstruction(hex, Cpu.M65Ce02, out decoded),
            Cpu.M65Ce02 => s_65ce02Decoded.TryGetValue(hex, out decoded) ||
                           TryGetDecodedInstruction(hex, Cpu.R65C02, out decoded),
            Cpu.HuC6280 => s_huc6280Decoded.TryGetValue(hex, out decoded) ||
                           TryGetDecodedInstruction(hex, Cpu.R65C02, out decoded),
            Cpu.R65C02 => s_r65c02Decoded.TryGetValue(hex, out decoded) ||
                          TryGetDecodedInstruction(hex, Cpu.M65C02, out decoded),
            Cpu.M65816 => s_65816Decoded.TryGetValue(hex, out decoded) ||
                          TryGetDecodedInstruction(hex, Cpu.M65C02, out decoded),
            Cpu.M65C02 => s_65c02Decoded.TryGetValue(hex, out decoded) ||
                          TryGetDecodedInstruction(hex, Cpu.M6502, out decoded),
            Cpu.M6502I => s_6502iDecoded.TryGetValue(hex, out decoded) ||
                          TryGetDecodedInstruction(hex, Cpu.M6502, out decoded),
            _ => s_6502Decoded.TryGetValue(hex, out decoded)
        };
    }
}