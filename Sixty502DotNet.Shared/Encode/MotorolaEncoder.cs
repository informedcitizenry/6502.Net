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
using System.Text;

namespace Sixty502DotNet.Shared.Encode;

using static EncodeUtil;

internal static partial class MotorolaEncoder
{
    public static string Decode(ReadOnlySpan<byte> bytes, Cpu cpu, ref int programCounter)
    {
        var originalProgramCounter = programCounter;
        try
        {
            var offset = 0;
            int opcode = bytes[offset++];
            if (cpu == Cpu.M6809 && opcode is 0x10 or 0x11 && bytes.Length > 1)
            {
                opcode = opcode * 256 + bytes[offset++];
            }
            var disassembly = cpu == Cpu.M6800
                ? s_m6800Disassembly
                : s_m6809Disassembly;
            if (!disassembly.TryGetValue(opcode, out var instruction))
            {
                programCounter++;
                return $".byte ${bytes[0]:x2}";
            }

            if (s_regOpcodes.Contains(opcode))
            {
                programCounter += instruction.Size;
                return DecodeRegisters(bytes, instruction.DisassemblyFormat);
            }

            if (cpu == Cpu.M6809 && opcode is 0x86 or 0xc6 && bytes is [_, _, 0x1f, _, ..])
            {
                programCounter += 4;
                return bytes[0] == 0x86
                    ? $"lda #${bytes[1]:x2}:tfr a,dp"
                    : $"ldb #${bytes[1]:x2}:tfr b,dp";
            }

            var operand = 0;
            var operandSize = instruction.Size - opcode.Size();
            var postByte = -1;
            if (cpu == Cpu.M6809 && s_m6809IndexedCodes.Contains(opcode) && bytes.Length > 1)
            {
                postByte = bytes[offset++];
                operandSize = IndexOperandSize(postByte);
            }

            if (operandSize + (offset - 1) > bytes.Length)
            {
                programCounter = originalProgramCounter + 1;
                return $".byte ${bytes[0]:x2}";
            }

            while (operandSize > 0 && offset < bytes.Length)
            {
                operand = operand * 256 + bytes[offset++];
                operandSize--;
            }

            if (postByte >= 0)
            {
                return DecodePostByte(instruction, postByte, offset, operand, ref programCounter);
            }

            if (instruction.IsRelative)
            {
                int maxValue = sbyte.MaxValue;
                var negativeMaxValue = byte.MaxValue + 1;
                if (instruction.Is16BitRelative)
                {
                    maxValue = short.MaxValue;
                    negativeMaxValue = ushort.MaxValue + 1;
                }

                if (operand > maxValue) operand -= negativeMaxValue;
                operand = programCounter + offset + operand;
            }
            programCounter += instruction.Size;
            return string.Format(instruction.DisassemblyFormat, ((long)operand).AsPositive());
        }
        catch
        {
            programCounter = originalProgramCounter + 1;
            return $".byte ${bytes[0]:x2}";
        }
    }
    
    public static bool Encode(CpuInstructionStatement statement, AssemblyState state)
    {
        if (statement.Operand.CoercedSize > 2) return false;
        if (statement.Mnemonic.Type is TokenType.Tfradp or TokenType.Tfrbdp)
        {
            return EncodeTfradp(state, statement.Mnemonic.Type, statement.Operand);
        }
        var instructions = state.Cpu == Cpu.M6800
            ? s_m6800Opcodes
            : s_m6809Opcodes;
        if (!instructions.TryGetValue(statement.Mnemonic.Type, out var opcode))
        {
            return false;
        }
        return statement.Operand.Type switch
        {
            OperandType.Implied => EncodeImplied
            (
                state, 
                opcode.implied, 
                Bad, 
                ByteOrder.BigEndian
            ),
            OperandType.Indexed when statement.Operand.CoercedSize == 1 &&state.Cpu == Cpu.M6800 =>
                statement.Operand.Registers[0].Type == TokenType.X &&
                EncodeSingleOperand
                (
                    state, 
                    opcode.zeroPageX, 
                    statement.Operand.Expressions[0],
                    1,
                    ByteOrder.BigEndian
                ),   
            OperandType.Indexed when statement.Operand.CoercedSize == 2 && state.Cpu ==  Cpu.M6800 =>
                statement.Operand.Registers[0].Type == TokenType.X &&
                EncodeSingleOperand
                (
                    state, 
                    opcode.absoluteX, 
                    statement.Operand.Expressions[0],
                    2,
                    ByteOrder.BigEndian
                ),
            OperandType.Indexed when state.Cpu == Cpu.M6800 => 
            statement.Operand.Registers[0].Type == TokenType.X &&
            EncodeVariantOperand
            (
                state,
                opcode.zeroPageX,
                opcode.absoluteX,
                Bad,
                Bad,
                statement.Operand.Expressions[0],
                ByteOrder.BigEndian
            ),
            
            OperandType.Address when statement.Operand.CoercedSize == 1 && 
                                     opcode.relative != Bad && opcode.relative == Bad =>
            EncodeRelative
            (
                state, 
                opcode,
                Bad, 
                statement.Operand.Expressions[0], 
                ByteOrder.BigEndian
            ),
            OperandType.Address when statement.Operand.CoercedSize == 2 && 
                                     opcode.relative == Bad && opcode.relativeAbsolute != Bad
            => EncodeRelative
            (
                state, 
                opcode, 
                Bad, 
                statement.Operand.Expressions[0], 
                ByteOrder.BigEndian
            ),
            OperandType.Address when opcode.relative != Bad || opcode.relativeAbsolute != Bad =>
            EncodeRelative
            (
                state, 
                opcode, 
                Bad, 
                statement.Operand.Expressions[0], 
                ByteOrder.BigEndian
            ),
            OperandType.Address when statement.Operand.CoercedSize == 1 
                => EncodeSingleOperand
            (
                state, 
                opcode.zeroPage, 
                statement.Operand.Expressions[0],
                1,
                ByteOrder.BigEndian
            ),
            OperandType.Address when statement.Operand.CoercedSize == 2 
                => EncodeSingleOperand
            (
                state, 
                opcode.absolute, 
                statement.Operand.Expressions[0], 
                2,
                ByteOrder.BigEndian
            ),
            OperandType.Address => EncodeVariantOperand
            (
                state, 
                opcode.zeroPage, 
                opcode.absolute, 
                Bad, 
                Bad, 
                statement.Operand.Expressions[0],
                ByteOrder.BigEndian
            ),
            OperandType.Immediate when statement.Operand.CoercedSize == 1 
                => EncodeSingleOperand
            (
                state,
                opcode.immediate,
                statement.Operand.Expressions[0],
                1,
                ByteOrder.BigEndian
            ),
            OperandType.Immediate when statement.Operand.CoercedSize == 2 
                => EncodeSingleOperand
            (
                state,
                opcode.immediate16Bit,
                statement.Operand.Expressions[0],
                2,
                ByteOrder.BigEndian
            ),
            OperandType.Immediate => EncodeVariantOperand
            (
                state, 
                opcode.immediate, 
                opcode.immediate16Bit, 
                Bad, 
                Bad, 
                statement.Operand.Expressions[0],
                ByteOrder.BigEndian
            ),
            OperandType.Indexed => EncodeM6809Indexed
            (
                state, 
                opcode.zeroPageX,  
                statement.Operand.Registers[0].Type,  
                statement.Operand.CoercedSize,
                false,
                statement.Operand.Expressions[0]
            ),
            OperandType.IndirectLong => EncodeM6809Indexed
            (
                state, 
                opcode.zeroPageX,  
                statement.Operand.Registers[0].Type,  
                statement.Operand.CoercedSize,
                true,
                statement.Operand.Expressions[0]
            ),
            OperandType.IndexedIndirect6809 => EncodeM6809Indexed
            (
                state, 
                opcode.zeroPageX,  
                statement.Operand.Registers[0].Type,  
                0,
                true,
                statement.Operand.Expressions[0]
            ),
            OperandType.RegisterOffset => EncodeRegisterOffsetOrIncrement
            (
                state, 
                opcode.zeroPageX, 
                0, 
                false, 
                statement.Operand.Registers[0].Type
            ),
            OperandType.AutoIncrement => EncodeRegisterOffsetOrIncrement
            (
                state, 
                opcode.zeroPageX, 
                1, 
                false, 
                statement.Operand.Registers[0].Type
            ),
            OperandType.AutoIncrement2 => EncodeRegisterOffsetOrIncrement
            (
                state, 
                opcode.zeroPageX, 
                2, 
                false, 
                statement.Operand.Registers[0].Type
            ),
            OperandType.AutoDecrement => EncodeRegisterOffsetOrIncrement
            (
                state, 
                opcode.zeroPageX, 
                -1, 
                false, 
                statement.Operand.Registers[0].Type
            ),
            OperandType.AutoDecrement2 => EncodeRegisterOffsetOrIncrement
            (
                state, 
                opcode.zeroPageX, 
                -2, 
                false, 
                statement.Operand.Registers[0].Type
            ),
            OperandType.IndirectRegisterOffset => EncodeRegisterOffsetOrIncrement
            (
                state,
                opcode.zeroPageX,
                0,
                true,
                statement.Operand.Registers[0].Type
            ),
            OperandType.IndirectAutoDecrement => EncodeRegisterOffsetOrIncrement
            (
                state,
                opcode.zeroPageX,
                -2,
                true,
                statement.Operand.Registers[0].Type
            ),
            OperandType.IndirectAutoIncrement => EncodeRegisterOffsetOrIncrement
            (
                state,
                opcode.zeroPageX,
                2,
                true,
                statement.Operand.Registers[0].Type
            ),
            OperandType.RegisterRegister when 
                statement.Mnemonic.Type is TokenType.Exg or TokenType.Tfr => 
                EncodeRegisterRegister
                (
                    state, 
                    opcode.zeroPage, 
                    statement.Operand.Registers[0].Type, 
                    statement.Operand.Registers[1].Type
                ),
            OperandType.RegisterRegister when opcode.zeroPageX != Bad => 
            EncodeAccumulatorRegisterOffset
            (
                state, 
                opcode.zeroPageX,
                statement.Operand.Registers[0].Type, 
               statement.Operand.Registers[1].Type, 
                false
            ),
            OperandType.Register or
            OperandType.RegisterRegister or 
            OperandType.RegisterList => EncodeRegisterList
            (
                state, 
                statement.Mnemonic.Type,
                opcode.zeroPage, 
                statement.Operand.Registers
            ),
            _ => false
        };
    }
    
    public static string Analyze(AssemblyOptions options, CodeAnalysisContext context, CodeAnalysisContext? context2)
    {
        if (options.WarnSimplifyCallReturn && 
            context.Statement.Mnemonic.Type is TokenType.Jsr &&
            context2?.Statement.Mnemonic.Type is TokenType.Rts)
        {
            return "Return following subroutine call can be simplified to a jump instruction";
        }
        if (options.WarnAmbiguousZp && 
            context.ObjectCode.Count > 1 && 
            context.Statement.Operand is { CoercedSize: 0, Type: OperandType.Address })
        {
            var instructions = context.Cpuid == Cpu.M6800
                ? s_m6800Opcodes
                : s_m6809Opcodes;
            if (!instructions.TryGetValue(context.Statement.Mnemonic.Type, out var opcode))
            {
                return string.Empty;
            }
            var dpHex = context.Statement.Operand.Type switch
            {
                OperandType.Address => opcode.zeroPage,
                _ => Bad
            };
            var absHex = context.Statement.Operand.Type switch
            {
                OperandType.Address => opcode.absolute,
                _ => Bad
            };
            if (absHex != Bad && dpHex != Bad && context.ObjectCode.Count - dpHex.Size() < 2)
            {
                return "Address size is ambiguous between direct page and absolute addressing. Consider coercing the operand size using [8] or [16] specifier";
            }
        }
        return string.Empty;
    }
    
    private static string DecodeRegisters(ReadOnlySpan<byte> bytes, string disasm)
    {
        IEnumerable<string> regs;
        int encoded = bytes[1];
        if (bytes[0] == 0x1e || bytes[0] == 0x1f)
        {
            regs = new List<string>();
            if (s_exchangeRegsLu.TryGetValue(encoded / 16, out var reg))
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
            for (var i = 1; i <= 128; i <<= 1)
            {
                if (!s_pushPullRegsLu.TryGetValue(encoded & i, out var reg))
                {
                    continue;
                }
                if (reg.Equals("su"))
                {
                    reg = bytes[0] < 0x36 ? "u" : "s";
                }
                ((HashSet<string>)regs).Add(reg);
            }
        }
        return $"{disasm} {string.Join(',', regs)}";
    }
    
    private static string DecodePostByte
    (
        DecodedInstruction instruction,
        int postByte, 
        int offset, 
        int operand, 
        ref int programCounter
    )
    {
        var extended = postByte == PostByteFlags.Extended;
        var indirect = (postByte & PostByteFlags.Indirect) == PostByteFlags.Indirect;
        var is5BitOffset = (postByte & 128) == 0;
        var is8BitOffset = (postByte & PostByteFlags.OffsetMask) == PostByteFlags.Offset8Bit;
        var is16BitOffset = (postByte & PostByteFlags.OffsetMask) == PostByteFlags.Offset16;
        var isPc8BitOffset = (postByte & PostByteFlags.OffsetMask) == PostByteFlags.Pc8BitOffs;
        var isPc16BitOffset = (postByte & PostByteFlags.OffsetMask) == PostByteFlags.Pc16Bit;
        var hasOffset = is5BitOffset || is8BitOffset || is16BitOffset || isPc8BitOffset || isPc16BitOffset;
        int operandSize = 0;
        StringBuilder ixFormat = new();
        if (indirect)
        {
            ixFormat.Append('[');
            if (extended)
            {
                operandSize = 2;
                ixFormat.Append("${0:x4}");
            }
        }
        if (hasOffset)
        {
            var negativeOffset = false;
            if (is5BitOffset)
            {
                operand = postByte & PostByteFlags.Offset5Bit;
            }
            var offsetAdjust =  operand switch
            {
                > short.MaxValue => 65536,
                > sbyte.MaxValue => 256,
                _ => 32
            };
            if (isPc8BitOffset || isPc16BitOffset)
            {
                var maxvalue = isPc8BitOffset ? 127 : short.MaxValue;
                operandSize = isPc8BitOffset ? 1 : 2;
                if (operand > maxvalue)
                {
                    operand -= offsetAdjust;
                }
                operand = programCounter + offset + operand;
            }
            if ((is16BitOffset && operand > short.MaxValue) ||
                (is8BitOffset && operand > sbyte.MaxValue) ||
                (is5BitOffset && operand > 15))
            {
                negativeOffset = true;
                operand = offsetAdjust - operand;
                if (!is5BitOffset)
                {
                    operandSize = is16BitOffset ? 2 : 1;
                }
            }
            if (negativeOffset)
            {
                ixFormat.Append('-');
            }
            ixFormat.Append("${0:x");
            ixFormat.Append(is5BitOffset || is8BitOffset || isPc8BitOffset ? '2' : '4');
            ixFormat.Append('}');
        }
        else switch (postByte & PostByteFlags.OffsetMask)
        {
            case PostByteFlags.AccDOffset:
                ixFormat.Append('d');
                break;
            case PostByteFlags.AccAOffset:
                ixFormat.Append('a');
                break;
            case PostByteFlags.AccBOffset:
                ixFormat.Append('b');
                break;
        }
        if (!extended)
        {
            ixFormat.Append(',');
            var autoDec = (postByte & PostByteFlags.OffsetMask) == PostByteFlags.AutoDec1 ||
                           (postByte & PostByteFlags.OffsetMask) == PostByteFlags.AutoDec2;
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
            else ixFormat.Append(s_indexRegsReverseLu[postByte & PostByteFlags.IndexMask]);

            if ((postByte & PostByteFlags.OffsetMask) == PostByteFlags.AutoInc1 ||
                (postByte & PostByteFlags.OffsetMask) == PostByteFlags.AutoInc2)
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
        programCounter += instruction.Size + operandSize;
        var disassemblyFormat = $"{instruction.DisassemblyFormat} {ixFormat}";
        return string.Format(disassemblyFormat, ((long)operand).AsPositive());
    }
    
    private static int IndexOperandSize(int postByte)
    {
        if (postByte < 128)
        {
            return 1; // 5-bit offset
        }
        if ((postByte & 0b1000) != 0)
        {
            if ((postByte & PostByteFlags.Extended) == PostByteFlags.Extended)
            {
                return 2;
            }
            if ((postByte & PostByteFlags.OffsetMask) == PostByteFlags.AccDOffset)
            {
                return 0;
            }
            return 1 + (postByte & 1);
        }
        return 0;
    }
    
    private static bool EncodeM6809Indexed
    (
        AssemblyState state, 
        int opcodeHex, 
        TokenType register,
        int coercedSize,
        bool indirect,
        Expression operand
    )
    {
        if (opcodeHex == Bad || !TryGetIndexOrPc(register, out var index))
        {
            return false;
        }
        long operandVal;
        int operandSize;
        
        var postFix = (byte)index;
        if (register != TokenType.Pc && register != TokenType.Pcr)
        {
            operandVal = new Evaluator(state).EvalPagedBanked(operand, short.MinValue, ushort.MaxValue);
            operandSize = operandVal.Size();
            if (coercedSize > 0 && operandSize > coercedSize)
            {
                if (!state.PassNeeded)
                {
                    throw new IntegerOverflowException
                    (
                        coercedSize == 1 ? 1 : 2,
                        coercedSize == 1 ? byte.MinValue : ushort.MinValue, 
                        coercedSize == 1 ? byte.MaxValue : ushort.MaxValue,
                        operand
                    );
                }
                operandVal = coercedSize == 1 ? 16 : 256;
            }
            if (operandVal is >= -16 and <= 15 && !indirect && coercedSize == 0)
            {
                state.Output.EmitValue(opcodeHex, ByteOrder.BigEndian);
                state.Output.EmitByte((byte)((operandVal & 31) | (byte)index));
                return true;
            }
            postFix |= PostByteFlags.Offset8Bit;
        }
        else
        {
            operandVal = new Evaluator(state).EvalInteger(operand, short.MinValue, ushort.MaxValue);
            operandSize = 1;
            var offs = operandVal - (state.Output.ProgramCounter + opcodeHex.Size() + 2);
            if (offs is < sbyte.MinValue or > sbyte.MaxValue)
            {
                operandSize = 2;
                offs = operandVal - (state.Output.ProgramCounter + + opcodeHex.Size() + 3);
                if (offs is < short.MinValue or > short.MaxValue)
                {
                    if (state is { PassNeeded: false, Passes: > 3 } || coercedSize == 1)
                    {
                        throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
                    }
                    state.PassNeeded = true;
                    offs = 32768;
                }
            }
            operandVal = offs;
        }
        if (operandSize == 2 || coercedSize == 2)
        {
            postFix |= PostByteFlags.Offset16;
        }
        if (indirect)
        {
            postFix |= PostByteFlags.Indirect;
        }

        state.Output.EmitValue(opcodeHex, ByteOrder.BigEndian);
        state.Output.EmitByte(postFix);
        state.Output.EmitValueSized(operandVal, coercedSize > 0 ? coercedSize : operandSize, ByteOrder.BigEndian);
        return true;
    }

    private static bool EncodeAccumulatorRegisterOffset
    (
        AssemblyState state,
        int opcodeHex,
        TokenType register1,
        TokenType register2,
        bool indirect
    )
    {
        if (!TryGetAccumulator(register1, out var accumulator) ||
            !TryGetIndex(register2, out var index))
        {
            return false;
        }
        var postFix = (byte)(accumulator | index);
        if (indirect) postFix |= PostByteFlags.Indirect;
        state.Output.EmitValue(opcodeHex, ByteOrder.BigEndian);
        state.Output.EmitByte(postFix);
        return true;
    }
    
    private static bool EncodeRegisterOffsetOrIncrement
    (
        AssemblyState state, 
        int opcodeHex,
        int increment,
        bool indirect,
        TokenType register
    )
    {
        if (opcodeHex == Bad || !TryGetIndex(register, out var index))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.BigEndian);
        var postFix = increment switch
        {
            -2 => (byte)(PostByteFlags.AutoDec2 | index),
            -1 => (byte)(PostByteFlags.AutoDec1 | index),
            +1 => (byte)(PostByteFlags.AutoInc1 | index),
            +2 => (byte)(PostByteFlags.AutoInc2 | index),
            _ => (byte)(PostByteFlags.ZeroOffset | index)
        };
        if (indirect) postFix |= PostByteFlags.Indirect;
        state.Output.EmitByte(postFix);
        return true;
    }

    private static bool EncodeRegisterRegister
    (
        AssemblyState state, 
        int opcodeHex,
        TokenType register1, 
        TokenType register2
    )
    {
        if (register1 == register2 || 
            !s_exchangeModes.TryGetValue(register1, out var exgByte) ||
            !s_exchangeModes.TryGetValue(register2, out var toByte))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.BigEndian);
        state.Output.EmitByte((byte)((exgByte << 4) | toByte));
        return true;
    }
    
    private static bool EncodeRegisterList
    (
        AssemblyState state, 
        TokenType mnemonic, 
        int opcodeHex, 
        IList<Token> registers
    )
    {
        if ((mnemonic != TokenType.Pshs && mnemonic != TokenType.Puls &&
             mnemonic != TokenType.Pshu && mnemonic != TokenType.Pulu) ||
            (mnemonic is TokenType.Pshs or TokenType.Puls && registers.Any(r => r.Type == TokenType.S)) ||
            (mnemonic is TokenType.Pshu or TokenType.Pulu && registers.Any(r => r.Type == TokenType.U)))
        {
            return false;
        }

        byte postFix = 0;
        var pushPull = new HashSet<byte>();
        for (var i = 0; i < registers.Count; i++)
        {
            if (!s_pushPullModes.TryGetValue(registers[i].Type, out var regHex) ||
                !pushPull.Add(regHex))
            {
                return false;
            }
            postFix |= regHex;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.BigEndian);
        state.Output.EmitByte(postFix);
        return true;
    }

    private static bool EncodeTfradp
    (
        AssemblyState state,
        TokenType directive,
        Operand operand
    )
    {
        if (operand.Type != OperandType.Address)
        {
            return false;
        }
        var eval = new Evaluator(state);
        var dpVal = eval.EvalByte(operand.Expressions[0]);
        var hex = directive == TokenType.Tfradp ? (byte)0x86 : (byte)0xc6;
        var accReg = s_exchangeModes[directive];
        state.Output.EmitByte(hex);     // lda/ldb 
        state.Output.EmitByte(dpVal);   // #n
        state.Output.EmitByte(0x1f); // tfr
        state.Output.EmitByte(accReg);  // a,dp/b,dp
        state.DirectPage = dpVal;
        state.DirectPageOff = false;
        return true;
    }
    
    private static bool TryGetAccumulator(TokenType register, out int accumulator)
    {
        accumulator = register switch
        {
            TokenType.A => PostByteFlags.AccAOffset,
            TokenType.B => PostByteFlags.AccBOffset,
            TokenType.D => PostByteFlags.AccDOffset,
            _ => -1
        };
        return accumulator != -1;
    }

    private static bool TryGetIndex(TokenType register, out int index)
    {
        index = register switch
        {
            TokenType.S => PostByteFlags.S,
            TokenType.U => PostByteFlags.U,
            TokenType.X => PostByteFlags.X,
            TokenType.Y => PostByteFlags.Y,
            _ => -1
        };
        return index != -1;
    }
    
    private static bool TryGetIndexOrPc(TokenType register, out int index)
    {
        index = register switch
        {
            TokenType.S => PostByteFlags.S,
            TokenType.U => PostByteFlags.U,
            TokenType.X => PostByteFlags.X,
            TokenType.Y => PostByteFlags.Y,
            TokenType.Pc or 
            TokenType.Pcr => PostByteFlags.Pc8BitOffs,
            _ => -1
        };
        return index != -1;
    }
}