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
using System.Collections.Frozen;

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable IdentifierTypo

namespace Sixty502DotNet.Shared.Encode;

public static partial class ZilogIntelEncoder
{
    public static string Decode(ReadOnlySpan<byte> bytes, Cpu cpu, ref int programCounter)
    {
        var b = bytes[0];
        int opcode = b;
        var startPc = programCounter;var operandIx = 1;
        try
        {
            if (cpu != Cpu.I8080 && IsPrefix(b) && bytes.Length > 1)
            {
                b = bytes[1];
                operandIx++;
                opcode |= b * 256;
                if (cpu == Cpu.Z80 && HasDispl(bytes))
                {

                    var displ = bytes[2];
                    programCounter += 3;
                    if (bytes[1] == 0xcb && bytes.Length > 3)
                    {
                        opcode |= bytes[3] * 0x10000;
                        programCounter++;
                    }
                    else if (bytes[1] == 0x36 && bytes.Length > 3)
                    {
                        programCounter++;
                        return string.Format(s_z80AllOpcodes[opcode].DisassemblyFormat, displ, bytes[3]);
                    }
                    return string.Format(s_z80AllOpcodes[opcode].DisassemblyFormat, displ);
                
                }
            }
            var disassembly = cpu switch
            {
                Cpu.Gb80 => s_gb80AllOpcodes,
                Cpu.I8080 => s_i8080AllOpcodes,
                _ => s_z80AllOpcodes
            };
            if (!disassembly.TryGetValue(opcode, out var instruction) ||
                bytes.Length - instruction.Size < 0)
            {
                programCounter++;
                return $".byte ${bytes[0]:x2}";
            }
            var opcodeSize = opcode.Size();
            if (cpu != Cpu.I8080 && opcode == 0xcb)
            {
                opcodeSize++;
            }
            var operandSize = instruction.Size - opcodeSize;
            var operand = 0;
            for (var i = 0; i < operandSize; i++)
            {
                operand |= bytes[i + operandIx] << i * 8;
            }
            if (instruction.IsRelative)
            {
                if (operand > sbyte.MaxValue) operand -= byte.MaxValue + 1;
                operand = programCounter + instruction.Size + operand;
            }
            programCounter += instruction.Size;
            return string.Format(instruction.DisassemblyFormat, operand);
        }
        catch
        {
            programCounter = startPc + 1;
            return $".byte ${bytes[0]:x2}";
        }
    }
    
    public static bool Encode(CpuInstructionStatement statement, AssemblyState state)
    {
        if (statement.Operand.CoercedSize > 0)
        {
            state.Logger.LogWarning("Bitwidth modifier ignored for this CPU type", statement.Operand);
        }
        return statement.Operand.Type switch
        {
            OperandType.Implied => EncodeImplied(statement.Mnemonic.Type, state),
            OperandType.Address => EncodeSingleOperand
            (
                statement.Mnemonic.Type, 
                statement.Operand.Expressions[0], 
                state
            ),
            OperandType.Indirect => EncodeIndirectExpression
            (
                statement.Mnemonic.Type, 
                statement.Operand.Expressions[0], 
                state
            ),
            OperandType.Register => EncodeRegister
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                false, 
                state
            ),
            OperandType.IndirectRegister => EncodeRegister
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                true, 
                state
            ),
            OperandType.RegisterRegister => EncodeRegisterRegister
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Registers[1].Type, 
                state
            ),
            OperandType.Immediate80 => EncodeImmediate80
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Expressions[0], 
                state
            ),
            OperandType.RegisterIndirect => EncodeRegisterIndirectExpression
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Expressions[0], 
                state
            ),
            OperandType.RegisterIndirectRegister => EncodeRegisterIndirectRegister
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Registers[1].Type, 
                state
            ),
            OperandType.IndirectRegisterImmediate => EncodeIndirectRegisterExpression
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Expressions[0],
                state
            ),
            OperandType.IndirectRegisterRegister => EncodeIndirectRegisterRegister
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Registers[1].Type, 
                state
            ),
            OperandType.IndirectIndexed => EncodeIndirectIndexed
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Expressions[0],
                state
            ),
            OperandType.Indexed => EncodeBitRegister
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Expressions[0], 
                state
            ),
            OperandType.Indexed80 => EncodeIndex
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type, 
                (statement.Operand.Expressions[0] as UnaryOpExpression)!, 
                state
            ),
            OperandType.IndirectIndexed80 => EncodeRegisterAndIndex
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[1].Type,
                statement.Operand.Registers[0].Type, 
                false,
                (statement.Operand.Expressions[0] as UnaryOpExpression)!, 
                state
            ),
            OperandType.RegisterIndirectIndexed80 => EncodeRegisterAndIndex
            (
                statement.Mnemonic.Type, 
                statement.Operand.Registers[0].Type,
                statement.Operand.Registers[1].Type, 
                true,
                (statement.Operand.Expressions[0] as UnaryOpExpression)!, 
                state
            ),
            OperandType.Indexed80Bit => EncodeBitIndexed
            (
                statement.Mnemonic.Type, 
                statement.Operand.Expressions[0], 
                statement.Operand.Registers[0].Type, 
                (statement.Operand.Expressions[1] as UnaryOpExpression)!, 
                state
            ),
            OperandType.IndirectIndexed80Immediate => EncodeIndexAndExpression
            (
                statement.Mnemonic.Type,
                statement.Operand.Expressions[1],
                statement.Operand.Registers[0].Type,
                (statement.Operand.Expressions[0] as UnaryOpExpression)!, 
                state
            ),
            OperandType.IndirectIndexed80Bit => EncodeBitRegisterIndex
            (
                statement.Mnemonic.Type, 
                statement.Operand.Expressions[0], 
                statement.Operand.Registers[0].Type, 
                statement.Operand.Registers[1].Type,
                (statement.Operand.Expressions[1] as UnaryOpExpression)!, 
                state
            ),
            OperandType.IndirectHlBit => EncodeHlBit
            (
                statement.Mnemonic.Type, 
                statement.Operand.Expressions[0], 
                state
            ),
            OperandType.GbAccumulatorHlIncrement => EncodeGbAccumulatorHlIncrement(state, true, true),
            OperandType.GbAccumulatorHlDecrement => EncodeGbAccumulatorHlIncrement(state, true, false),
            OperandType.GbHlIncrementAccumulator => EncodeGbAccumulatorHlIncrement(state, false, true),
            OperandType.GbHlDecrementAccumulator => EncodeGbAccumulatorHlIncrement(state, false, false),
            OperandType.GbIndirect => EncodeGbBaseOffset
            (
                (statement.Operand.Expressions[0] as BinaryOpExpression)!, 
                false, 
                state
            ),
            OperandType.GbImmediateIndirect => EncodeGbBaseOffset
            (
                (statement.Operand.Expressions[0] as BinaryOpExpression)!, 
                true, 
                state
            ),
            OperandType.GbIndirectIndexed => EncodeGbBaseCOffset
            (
                (statement.Operand.Expressions[0] as BinaryOpExpression)!, 
                false, 
                state
            ),
            OperandType.GbImmediateIndirectIndexed => EncodeGbBaseCOffset
            (
                (statement.Operand.Expressions[0] as BinaryOpExpression)!, 
                true, 
                state
            ),
            OperandType.GbStackOffset => EncodeGbStackOffset
            (
                (statement.Operand.Expressions[0] as BinaryOpExpression)!, 
                state
            ),
            _ => false
        };
    }
    
    public static string Analyze(AssemblyOptions options, CodeAnalysisContext context, CodeAnalysisContext? context2)
    {
        if (options.WarnSimplifyCallReturn && 
            context.Statement.Mnemonic.Type is TokenType.Call &&
            context2?.Statement.Mnemonic.Type is TokenType.Ret)
        {
            return "Return following subroutine call can be simplified to a jump instruction";
        }
        if (options.WarnOptimizeResetReg &&
            context.Statement.Mnemonic.Type is TokenType.Ld &&
            context.Statement.Operand.Type == OperandType.Immediate80 &&
            context.Statement.Operand.Registers[0].Type == TokenType.A &&
            context.ObjectCode[1] == 0)
        {
            return "Instruction can be optimized to `xor a,a`";
        }
        return string.Empty;
    }
    
    private static bool IsPrefix(byte b) => b == 0xcb || b == 0xed || IsIxPrefix(b);

    private static bool IsIxPrefix(byte b) => b is 0xdd or 0xfd;

    private static bool HasDispl(ReadOnlySpan<byte> bytes)
    {
        if (!IsIxPrefix(bytes[0]) || bytes.Length < 3)
        {
            return false;
        }
        if (bytes[1] == 0xcb ||
            (bytes[1] > 0x40 && bytes[1] < 0xc0 && (bytes[1] & 0xf) is 0x6 or 0xe))
        {
            return true;
        }
        return bytes[1] switch
        {
            >= 0x34 and <= 0x36 => true,
            >= 0x70 and <= 0x77 => true,
            _ => false
        };
    }
    
    private static bool EncodeImplied(TokenType mnemonic, AssemblyState state)
    {
        var instructions = state.Cpu switch
        {
            Cpu.Gb80 => s_gb80Implieds,
            Cpu.I8080 => s_i8080Implieds,
            _ => s_z80Implieds
        };
        if (!instructions.TryGetValue(mnemonic, out var opcodeHex))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeSingleOperand(TokenType mnemonic, Expression operand, AssemblyState state)
    {
        var eval = new Evaluator(state);
        var operandVal = eval.EvalInteger(operand, -32768, 65535);
        switch (mnemonic)
        {
            case TokenType.Im when state.Cpu == Cpu.I8080:
                return false;
            case TokenType.Im:
            {
                if (operandVal is < 0 or > 2)
                {
                    if (!state.PassNeeded)
                    {
                        return false;
                    }
                    operandVal = 0;
                }
                var imHex = operandVal switch
                {
                    0 => 0x46ed,
                    1 => 0x56ed,
                    _ => 0x5eed
                };
                state.Output.EmitValue(imHex, ByteOrder.LittleEndian);
                return true;
            }
            case TokenType.Rst:
            {
                if ((state.Cpu == Cpu.I8080 && operandVal is < 0 or > 8) ||
                    (state.Cpu != Cpu.I8080 && (operandVal is < 0 or > 56 || operandVal % 8 != 0)))
                {
                    if (!state.PassNeeded)
                    {
                        return false;
                    }
                    operandVal = 0;
                }
                if (state.Cpu == Cpu.I8080) operandVal *= 8;
                state.Output.EmitValue(operandVal | 0xc7, ByteOrder.LittleEndian);
                return true;
            }
        }

        var instructions = state.Cpu switch
        {
            Cpu.Gb80 => s_gb80OneOperands,
            Cpu.I8080 => s_i8080OneOperands,
            _ => s_z80OneOperands
        };
        var size = 2;
        if (mnemonic is TokenType.Djnz or TokenType.Jr)
        {
            operandVal -= state.Output.ProgramCounter + 2;
            if (operandVal is < -126 or > 129) // per Z80 manual
            {
                if (state is { PassNeeded: false, Passes: > 3 })
                {
                    throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
                }
                state.PassNeeded = true;
            }
            size = 1;
        }
        if (!instructions.TryGetValue((mnemonic, TokenType.N16), out var opcodeHex))
        {
            size = 1;
            if (!instructions.TryGetValue((mnemonic, TokenType.N8), out opcodeHex))
            {
                return false;
            }
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(operandVal, size, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeIndirectExpression(TokenType mnemonic, Expression operand, AssemblyState state)
    {
        if (state.Cpu == Cpu.I8080) return false;
        var instructions = state.Cpu switch
        {
            Cpu.Gb80 => s_gb80OneOperands,
            _ => s_z80OneOperands
        };
        var size = 2;
        if (!instructions.TryGetValue((mnemonic, TokenType.N16), out var opcodeHex))
        {
            size = 1;
            if (!instructions.TryGetValue((mnemonic, TokenType.N8), out opcodeHex))
            {
                return false;
            }
        }
        var operandVal = new Evaluator(state).EvalInteger(operand, short.MinValue, ushort.MaxValue);
        if (size < operandVal.Size() && !state.PassNeeded)
        {
            throw new IntegerOverflowException(2, short.MinValue, ushort.MaxValue, operand);
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(operandVal, size, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeImmediate80(TokenType mnemonic, TokenType register, Expression operand,
        AssemblyState state)
    {
        var instructions = state.Cpu switch
        {
            Cpu.Gb80 => s_gb80TwoOperands,
            Cpu.I8080 => s_i8080TwoOperands,
            _ => s_z80TwoOperands
        };
        return EncodeRegisterAndOperand
        (
            instructions,
            mnemonic,
            register,
            true,
            false,
            operand,
            state
        );
    }

    private static bool EncodeRegister
    (
        TokenType mnemonic, 
        TokenType register, 
        bool isIndirect, 
        AssemblyState state
    )
    {
        FrozenDictionary<(TokenType, TokenType), int> instructions;
        if (isIndirect)
        {
            if (state.Cpu == Cpu.I8080) return false;
            instructions = state.Cpu == Cpu.Gb80 ? s_gb80OneOperandsInd : s_z80OneOperandsInd;
        }
        else
        {
            instructions = state.Cpu switch
            {
                Cpu.Gb80 => s_gb80OneOperands,
                Cpu.I8080 => s_i8080OneOperands,
                _ => s_z80OneOperands
            };
        }
        if (!instructions.TryGetValue((mnemonic, register), out var opcodeHex))
        {
            return false;
        }
        if ((opcodeHex & 0xff) == 0xcb)
        {
            state.Output.EmitValueSized(opcodeHex, 2, ByteOrder.LittleEndian);
            return true;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeRegisterRegister
    (
        TokenType mnemonic, 
        TokenType reg0, 
        TokenType reg1, 
        AssemblyState state
    )
    {
        var instructions = state.Cpu switch
        {
            Cpu.Gb80 => s_gb80TwoOperands,
            Cpu.I8080 => s_i8080TwoOperands,
            _ => s_z80TwoOperands
        };
        if (!instructions.TryGetValue((mnemonic, reg0, reg1), out var opcodeHex))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeRegisterIndirectRegister
    (
        TokenType mnemonic,
        TokenType register1,
        TokenType register2,
        AssemblyState state
    )
    {
        if (state.Cpu == Cpu.I8080) return false;
        var instructions = state.Cpu == Cpu.Gb80
            ? s_gb80TwoOperandsIndSecond
            : s_z80TwoOperandsIndSecond;
        if (!instructions.TryGetValue((mnemonic, register1, register2), out var opcodeHex))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeGbAccumulatorHlIncrement(AssemblyState state, bool accFirst, bool increment)
    {
        byte opcodeHex;
        if (accFirst)
        {
            opcodeHex = (byte)(increment ? 0x2a : 0x3a);
        }
        else
        {
            opcodeHex = (byte)(increment ? 0x22 : 0x32);
        }
        state.Output.EmitByte(opcodeHex);
        return true;
    }
    
    private static bool EncodeIndirectRegisterRegister
    (
        TokenType mnemonic,
        TokenType register1,
        TokenType register2,
        AssemblyState state
    )
    {
        if (state.Cpu == Cpu.I8080) return false;
        var instructions = state.Cpu == Cpu.Gb80
            ? s_gb80TwoOperandsIndFirst
            : s_z80TwoOperandsIndFirst;
        if (!instructions.TryGetValue((mnemonic, register1, register2), out var opcodeHex))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeIndirectIndexed
    (
        TokenType mnemonic, 
        TokenType register, 
        Expression operand, 
        AssemblyState state
    )
    {
        if (state.Cpu ==  Cpu.I8080) return false;
        var instructions = state.Cpu == Cpu.Gb80 
            ? s_gb80TwoOperandsIndFirst 
            : s_z80TwoOperandsIndFirst;
        return EncodeRegisterAndOperand
        (
            instructions,
            mnemonic,
            register,
            false,
            mnemonic != TokenType.Out,
            operand,
            state
        );
    }
    
    private static bool EncodeIndirectRegisterExpression
    (
        TokenType mnemonic, 
        TokenType register, 
        Expression operand, 
        AssemblyState state
    )
    {
        if (state.Cpu ==  Cpu.I8080) return false;
        if (mnemonic == TokenType.Out)
        {
            if (register != TokenType.C ||
                operand.Value.AsInt() != 0)
            {
                return false;
            }
            EncodeUtil.EnforceBit(operand);
            state.Output.EmitValue(0x71ed,  ByteOrder.LittleEndian);
            return true;
        }
        var instructions = state.Cpu == Cpu.Gb80 
            ? s_gb80TwoOperandsIndFirst 
            : s_z80TwoOperandsIndFirst;
        return EncodeRegisterAndOperand
        (
            instructions,
            mnemonic,
            register,
            true,
            false,
            operand,
            state
        );
    }
    
    private static bool EncodeRegisterIndirectExpression
    (
        TokenType mnemonic, 
        TokenType register, 
        Expression operand, 
        AssemblyState state
    )
    {
        if (state.Cpu ==  Cpu.I8080) return false;
        var instructions = state.Cpu == Cpu.Gb80 
            ? s_gb80TwoOperandsIndSecond 
            : s_z80TwoOperandsIndSecond;
        return EncodeRegisterAndOperand
        (
            instructions,
            mnemonic,
            register,
            true,
            mnemonic != TokenType.In && mnemonic != TokenType.Out,
            operand,
            state
        );
    }

    private static bool EncodeRegisterAndOperand
    (
        FrozenDictionary<(TokenType, TokenType, TokenType), int> instructions,
        TokenType mnemonic,
        TokenType register,
        bool registerFirst,
        bool indirect,
        Expression operand,
        AssemblyState state
    )
    {
        var sizeType = (s_8BitRegisters.Contains(register) && !indirect && mnemonic != TokenType.Jp && mnemonic != TokenType.Call) ||
                       mnemonic is TokenType.Jr 
            ? TokenType.N8 : TokenType.N16;
        var expctSize = sizeType == TokenType.N8 ? 1 : 2;
        var lookup = registerFirst
            ? (mnemonic, register, sizeType)
            : (mnemonic, sizeType, register);
        var eval = new Evaluator(state);
        var operandVal = eval.EvalInteger(operand, -32768, 65535);
        if (mnemonic == TokenType.Jr)
        {
            operandVal -= (state.Output.ProgramCounter + 2);
            if (operandVal is < -128 or > 127)
            {
                if (state is { PassNeeded: false, Passes: > 3 })
                {
                    throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
                }
                state.PassNeeded = true;
                operandVal = 0;
            }
        }
        if (!instructions.TryGetValue(lookup, out var opcodeHex) &&
            lookup.Item3 is TokenType.N8 or TokenType.N16)
        {
            lookup.Item3 = lookup.Item3 == TokenType.N8 ? TokenType.N16 : TokenType.N8;
            expctSize = expctSize == 2 ? 1 : 2;
        }
        if ((expctSize < operandVal.Size() && !state.PassNeeded) ||
            !instructions.TryGetValue(lookup, out opcodeHex))
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(operandVal, expctSize, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeIndex
    (
        TokenType mnemonic,
        TokenType indexRegister,
        UnaryOpExpression index,
        AssemblyState state
    )
    {
        if (state.Cpu != Cpu.Z80 ||
            !s_z80OneOperandsIndexed.TryGetValue((mnemonic, indexRegister), out var opcodeHex))
        {
            return false;
        }
        var eval = new Evaluator(state);
        var offs = (int)eval.EvalInteger(index, -128, 255) * 0x10000;
        offs |= (opcodeHex & 0x00ffff);
        offs |= (opcodeHex & 0xff0000) * 256;
        state.Output.EmitValue(offs, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeIndexAndExpression
    (
        TokenType mnemonic,
        Expression operand,
        TokenType indexRegister,
        UnaryOpExpression index,
        AssemblyState state
    )
    {
        if (state.Cpu != Cpu.Z80 ||
            !s_z80TwoOperandsIndexedFirst.TryGetValue((mnemonic, indexRegister, TokenType.N8), out var opcodeHex))
        {
            return false;
        }
        var eval = new Evaluator(state);
        var offs = (int)eval.EvalInteger(index, -128, 255) * 0x10000;
        var operandVal = (int)eval.EvalInteger(operand, -128, 255);
        offs |= (opcodeHex & 0x00ffff);
        state.Output.EmitValueSized(offs, opcodeHex.Size() + 1, ByteOrder.LittleEndian);
        state.Output.EmitValue(operandVal, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeRegisterAndIndex
    (
        TokenType mnemonic,
        TokenType register,
        TokenType indexRegister,
        bool registerFirst,
        UnaryOpExpression displExpr,
        AssemblyState state
    )
    {
        if (state.Cpu != Cpu.Z80) return false;  
        var key = registerFirst 
            ? (mnemonic, register, indexRegister) 
            : (mnemonic, indexRegister, register);
        var instructions = registerFirst
            ? s_z80TwoOperandsIndexedSecond
            : s_z80TwoOperandsIndexedFirst;
        if (!instructions.TryGetValue(key, out var opcodeHex))
        {
            return false;
        }
        var eval = new Evaluator(state);
        var offs = (int)eval.EvalInteger(displExpr, -128, 255) * 0x10000;
        offs |= (opcodeHex & 0x00ffff);
        offs |= (opcodeHex & 0xff0000) * 256;
        var size = opcodeHex.Size() + 1;
        if ((opcodeHex & 0xffff) == 0xcbdd ||
            (opcodeHex & 0xffff) == 0xcbfd)
        {
            size = 4;
        }
        state.Output.EmitValueSized(offs, size, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeGbStackOffset(BinaryOpExpression operand, AssemblyState state)
    {
        var eval = new Evaluator(state);
        state.Output.EmitByte(0xf8);
        state.Output.EmitByte(eval.EvalByte(operand.Right));
        return true;
    }

    private static bool EncodeGbBaseOffset
    (
        BinaryOpExpression operand, 
        bool accFirst, 
        AssemblyState state
    )
    {
        var eval = new Evaluator(state);
        var baseVal = eval.EvalInteger(operand.Left);
        if (baseVal != 0xff00)
        {
            state.PassNeeded |= state.Passes < 4;
            var instructions = accFirst 
                ? s_gb80TwoOperandsIndSecond 
                : s_gb80TwoOperandsIndFirst;
            return EncodeRegisterAndOperand
            (
                instructions, 
                TokenType.Ld, 
                TokenType.A, 
                accFirst, 
                true,
                operand, 
                state
            );
        }
        var offsetVal =  eval.EvalInteger(operand) - 0xff00;
        if (offsetVal is < -128 or > 255)
        {
            if (!state.PassNeeded)
            {
                throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
            }

            offsetVal = 0;
        }
        state.Output.EmitByte(accFirst ? (byte)0xf0 : (byte)0xe0);
        state.Output.EmitValue(offsetVal, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeGbBaseCOffset
    (
        BinaryOpExpression operand, 
        bool accFirst, 
        AssemblyState state
    )
    {
        var eval = new Evaluator(state);
        var baseVal = eval.EvalInteger(operand.Left);
        if (baseVal != 0xff00)
        {
            if (state.Passes >= 4)
            {
                return false;
            }
            state.PassNeeded = true;
            var instructions = accFirst 
                ? s_gb80TwoOperandsIndSecond 
                : s_gb80TwoOperandsIndFirst;
            return EncodeRegisterAndOperand
            (
                instructions, 
                TokenType.Ld, 
                TokenType.A, 
                accFirst, 
                true,
                operand, 
                state
            );
        }
        state.Output.EmitByte(accFirst ? (byte)0xf2 : (byte)0xe2);
        return true;
    }

    private static bool EncodeBitIndexed
    (
        TokenType mnemonic,
        Expression bit,
        TokenType indexRegister,
        UnaryOpExpression displExpr,
        AssemblyState state
    )
    {
        if (state.Cpu == Cpu.I8080 ||
            (mnemonic != TokenType.Bit && mnemonic != TokenType.Res && mnemonic != TokenType.Set))
        {
            return false;
        }
        EncodeUtil.EnforceBit(bit);
        var eval = new Evaluator(state);
        var displ = (int)eval.EvalInteger(displExpr, -128, 255) & 0xff;
        var baseOpcode = GetBitBaseOpcode(mnemonic, bit, 6) << 16;
        baseOpcode |= 0x10000 * displ;
        baseOpcode |= 0xcb00;
        baseOpcode |= indexRegister == TokenType.Ix ? 0xdd : 0xfd;
        state.Output.EmitValue(baseOpcode, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeHlBit
    (   
        TokenType mnemonic,
        Expression bit,
        AssemblyState state
    )
    {
        if (state.Cpu == Cpu.I8080 ||
            (mnemonic != TokenType.Bit && mnemonic != TokenType.Res && mnemonic != TokenType.Set))
        {
            return false;
        }
        EncodeUtil.EnforceBit(bit);
        state.Output.EmitValue(GetBitBaseOpcode(mnemonic, bit, 6) | 0xcb, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeBitRegisterIndex
    (
        TokenType mnemonic,
        Expression bit,
        TokenType indexRegister,
        TokenType register,
        UnaryOpExpression index,
        AssemblyState state
    )
    {
        if (state.Cpu == Cpu.I8080 ||
            (mnemonic != TokenType.Res && mnemonic != TokenType.Set))
        {
            return false;
        }
        EncodeUtil.EnforceBit(bit);
        var displ = GetBitRegisterDisplacement(register);
        if (displ < 0) return false;
        var eval = new Evaluator(state);
        var offs = (int)eval.EvalInteger(index, -128, 255) & 0xff;
        var baseOpcode = GetBitBaseOpcode(mnemonic, bit, displ) << 16;
        baseOpcode |= 0x10000 * offs;
        baseOpcode |= 0xcb00;
        baseOpcode |= indexRegister == TokenType.Ix ? 0xdd : 0xfd;
        state.Output.EmitValue(baseOpcode, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeBitRegister
    (
        TokenType mnemonic, 
        TokenType register, 
        Expression bit, 
        AssemblyState state
    )
    {
        if (state.Cpu == Cpu.I8080 ||
            (mnemonic != TokenType.Bit && mnemonic != TokenType.Res && mnemonic != TokenType.Set))
        {
            return false;
        }
        EncodeUtil.EnforceBit(bit);
        var displ = GetBitRegisterDisplacement(register);
        if (displ < 0) return false;
        var baseOpcode = GetBitBaseOpcode(mnemonic, bit, displ) | 0xcb;
        state.Output.EmitValue(baseOpcode, ByteOrder.LittleEndian);
        return true;
    }

    private static int GetBitRegisterDisplacement(TokenType register)
    {
        return register switch
        {
            TokenType.A => 7,
            TokenType.B => 0,
            TokenType.C => 1,
            TokenType.D => 2,
            TokenType.E => 3,
            TokenType.H => 4,
            TokenType.L => 5,
            _ => -1
        };
    }
    
    private static int GetBitBaseOpcode(TokenType mnemonic, Expression bit, int displ)
    {
        var bitVal = (int)bit.Value.AsInt() * 8 + displ;
        return mnemonic switch
        {
            TokenType.Bit => (0x40 + bitVal) * 0x100,
            TokenType.Res => (0x80 + bitVal) * 0x100,
            _ =>             (0xc0 + bitVal) * 0x100
        };
    }
}