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

using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Encode;

public static partial class I86Encoder
{
    public static bool Encode(CpuInstructionStatement statement, AssemblyState state)
    {
        var mnemonic = statement.Mnemonic.Type;
        var operand = statement.Operand.Type;
        return s_encodeFns.TryGetValue(operand, out var encodeFn) && 
               encodeFn(state, mnemonic, statement.Operand);
    }
    
    public static string Analyze(AssemblyOptions options, CodeAnalysisContext context, CodeAnalysisContext? context2)
    {
        if (options.WarnSimplifyCallReturn && 
            context.Statement.Mnemonic.Type is TokenType.Call &&
            context2?.Statement.Mnemonic.Type is TokenType.Ret)
        {
            return "Return following subroutine call can be simplified to a jump instruction";
        }
        return string.Empty;
    }
    
    private static bool EncodeImplied(AssemblyState state, TokenType directive, Operand _)
    {
        if (!s_implieds.TryGetValue(directive, out var hex))
        {
            return false;
        }
        state.Output.EmitValue(hex, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeRegister(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var reg = operand.Registers[0].Type;
        if (reg.IsStParenReg()) reg = reg.FromStParen();
        if (mnemonic.Is8087Opcode())
        {
            if (!s_8087RegOpcs.TryGetValue(mnemonic, out var opc8087) ||
                !s_8087StackRegisters.TryGetValue(reg, out var stHex))
            {
                return false;
            }
            stHex *= 0x100;
            state.Output.EmitValueSized(opc8087 | stHex, 2, ByteOrder.LittleEndian);
            return true;
        }
        if (s_singleRegOpcs.TryGetValue(mnemonic, out var opcHexes))
        {
            if (!s_8BitRegisters.TryGetValue(reg, out var regHex) &&
                !s_16BitRegisters.TryGetValue(reg, out regHex))
            {
                return false;
            }
            var opc = s_8BitRegisters.ContainsKey(reg) ? opcHexes.Item1 : opcHexes.Item2;
            if (opc > 0x100)
            {
                regHex *= 0x100;
            }
            state.Output.EmitValue(opc | regHex, ByteOrder.LittleEndian);
            return true;
        }
        if (s_16BitRegisters.TryGetValue(reg, out var reg16Hex) &&
            s_singleRegOpcs16.TryGetValue(mnemonic, out var opc16))
        {
            if (opc16 > 0x100)
            {
                reg16Hex *= 0x100;
            }
            state.Output.EmitValue(opc16 | reg16Hex, ByteOrder.LittleEndian);
            return true;
        }
        if ((mnemonic != TokenType.Push && mnemonic != TokenType.Pop) ||
            !reg.IsI86Segment() ||
            (mnemonic == TokenType.Pop && reg == TokenType.Cs))
        {
            return false;
        }
        var segOPc = mnemonic switch
        {
            TokenType.Push => reg switch
            {
                TokenType.Es => 0x06,
                TokenType.Ss => 0x16,
                TokenType.Cs => 0x0e,
                _ => 0x1e
            },
            _ => reg switch
            {
                TokenType.Es => 0x07,
                TokenType.Ss => 0x17,
                _ => 0x1f
            }
        };
        state.Output.EmitValue(segOPc, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeAddress(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (mnemonic is TokenType.Call or TokenType.Jmp)
        {
            return EncodeCallJumpToLabel(state, mnemonic, operand);
        }
        if (s_branching.TryGetValue(mnemonic, out var branchHex))
        {
            return EncodeBranch(state, branchHex, operand);
        }
        var eval = new Evaluator(state);
        var addr = eval.EvalInteger(operand.Expressions[0], short.MinValue, ushort.MaxValue);
        if (mnemonic == TokenType.Int && addr == 3)
        {
            return EncodeImplied(state, TokenType.Int3, operand);
        }
        var opsize = TokenType.N8;
        var size = 1;
        if (addr.Size() > 1)
        {
            opsize = TokenType.N16;
            size = 2;
        }
        if (!s_address.TryGetValue((mnemonic, opsize), out var hex))
        {
            if (opsize == TokenType.N8)
            {
                if (!s_address.TryGetValue((mnemonic, TokenType.N16), out hex))
                {
                    return false;
                }
                size = 2;
            }
            else if (addr.Size() > 1) return false;
            else
            {
                if (!s_address.TryGetValue((mnemonic, TokenType.N8), out hex))
                {
                    return false;
                }
                size = 1;
            }
        }
        state.Output.EmitValue(hex, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(addr, size, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeImmediateShift(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var reg = operand.Registers[0].Type;
        if (!s_8BitRegisters.TryGetValue(reg, out var regHex) &&
             !s_16BitRegisters.TryGetValue(reg, out regHex))
        {
            return false;
        }
        var eval = new Evaluator(state);
        var imm = eval.EvalInteger(operand.Expressions[0]);
        if (imm != 1)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                return false;
            }
            state.PassNeeded = true;
        }
        var opc = s_singleRegOpcs[mnemonic];
        var opcHex = s_8BitRegisters.ContainsKey(reg) ? opc.Item1 : opc.Item2;
        opcHex |= regHex * 256;
        state.Output.EmitValueSized(opcHex, 2, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeImmediate(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (s_shifts.ContainsKey(mnemonic)) return EncodeImmediateShift(state, mnemonic, operand);
        var reg = operand.Registers[0].Type;
        if (!s_immediateOps.TryGetValue(mnemonic, out var opcodes) ||
            (!s_8BitRegisters.TryGetValue(reg, out var regHex) &&
            !s_16BitRegisters.TryGetValue(reg, out regHex)))
        {
            return false;
        }
        var minVal = s_8BitRegisters.ContainsKey(reg) || mnemonic == TokenType.In ? sbyte.MinValue : short.MinValue;
        var maxVal = minVal >= sbyte.MinValue ? byte.MaxValue : ushort.MaxValue;
        var eval = new Evaluator(state);
        var imm = eval.EvalInteger(operand.Expressions[0], minVal, maxVal);
        
        var isAlu = s_alus.Contains(mnemonic);
        var immSize = mnemonic switch
        {
            TokenType.In => 1,
            TokenType.Mov or TokenType.Test => s_8BitRegisters.ContainsKey(reg) ? 1 : 2,
            _ => s_8BitRegisters.ContainsKey(reg) || imm is >= sbyte.MinValue and <= sbyte.MaxValue ? 1 : 2
        };
        var axAlu = reg == TokenType.Ax && isAlu && immSize > 1;
        var opc = opcodes.Item2;
        if (reg is TokenType.Al || 
            axAlu || 
            (reg is TokenType.Ax && mnemonic is TokenType.Test or TokenType.In))
        {
            opc = opcodes.Item1;
        }
        if (opc == -1) return false;
        
        if (s_16BitRegisters.ContainsKey(reg))
        {
            if (mnemonic == TokenType.Mov)
            {
                regHex += 8;
            }
            else
            {
                opc |= W;
                if (!axAlu && s_alus.Contains(mnemonic) && immSize == 1)
                {
                    opc |= D;
                }
            }
        }
        if (opc > 0xff)
        {
            regHex *= 256;
        }
        opc |= regHex;
        state.Output.EmitValue(opc, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(imm, immSize, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeCallJumpToLabel(AssemblyState state, TokenType callJump, Operand operand)
    {
        var eval = new Evaluator(state);
        var hex = s_branching[callJump];
        var size = 4; // 32-bit signed offset
        var label = eval.EvalInteger(operand.Expressions[0]);
        var dis8Offs = label - (state.Output.ProgramCounter + 2);
        var dis16Offs = label - (state.Output.ProgramCounter + 3);
        var actualOffs = label - (state.Output.ProgramCounter + 5);
        if (callJump == TokenType.Jmp && dis8Offs is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            hex = 0xeb;
            size = 1;
            actualOffs = dis8Offs;
        }
        else if (dis16Offs is >= short.MinValue and <= short.MaxValue || state.PassNeeded)
        {
            hex = callJump == TokenType.Jmp ? 0xe9 : 0xe8;
            size = 2;
            actualOffs = dis16Offs;
        }
        else if (actualOffs is < int.MinValue or > int.MaxValue)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
            }
            state.PassNeeded = true;
        }
        state.Output.EmitByte((byte)hex);
        state.Output.EmitValueSized(actualOffs, size, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeSegmentAbsoluteDirect(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (mnemonic != TokenType.Call && mnemonic != TokenType.Jmp)
        {
            return false;
        }
        var opc = mnemonic is TokenType.Call ? (byte)0x9a : (byte)0xea;
        var eval = new Evaluator(state);
        var segment = eval.EvalInteger(operand.Expressions[0], short.MinValue, ushort.MaxValue);
        var address = eval.EvalInteger(operand.Expressions[1]);
        state.Output.EmitByte(opc);
        state.Output.EmitValueSized(address, 4,  ByteOrder.LittleEndian);
        state.Output.EmitValueSized(segment, 2, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeBranch(AssemblyState state, int loopHex, Operand operand)
    {
        var eval = new Evaluator(state);
        var label = eval.EvalInteger(operand.Expressions[0]);
        var offs = label - (state.Output.ProgramCounter + 2);
        if (offs is < sbyte.MinValue or > sbyte.MaxValue)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
            }
            state.PassNeeded = true;
        }
        state.Output.EmitByte((byte)loopHex);
        state.Output.EmitValueSized(offs, 1, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeOut(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var register = operand.Registers[0].Type;
        if (mnemonic != TokenType.Out ||
            (register != TokenType.Al && register != TokenType.Ax))
        {
            return false;
        }
        var hex = register switch
        {
            TokenType.Al => 0xe6,
            _ => 0xe7
        };
        var eval = new Evaluator(state);
        var outVal = eval.EvalInteger(operand.Expressions[0], sbyte.MinValue, byte.MaxValue);
        state.Output.EmitValue(hex, ByteOrder.LittleEndian);
        state.Output.EmitValue(outVal, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeRegisterRegister(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (mnemonic is TokenType.In or TokenType.Out)
        {
            return EncodeInOut(state, mnemonic, operand);
        }
        if (s_shifts.ContainsKey(mnemonic))
        {
            return EncodeShiftRegisterRegister(state, mnemonic, operand);
        }
        if (mnemonic.Is8087Opcode())
        {
            return Encode8087RegisterRegister(state, mnemonic, operand);
        }
        var dest = operand.Registers[0].Type; 
        var src = operand.Registers[1].Type;
        
        int srcRegHex;
        if (!s_regRegOrImm.TryGetValue(mnemonic, out var opc))
        {
            return false;
        }

        var isSegment = false;
        if (s_16BitRegisters.TryGetValue(dest, out var destRegHex))
        {
            if (!s_16BitRegisters.TryGetValue(src, out srcRegHex))
            {
                if (mnemonic != TokenType.Mov || !s_segmentRegisters.TryGetValue(src, out srcRegHex))
                {
                    return false;
                }
                isSegment = true;
                opc = 0x8c;
                srcRegHex -= SegBase;
            }
        }
        else if (s_8BitRegisters.TryGetValue(dest, out destRegHex))
        {
            if (!s_8BitRegisters.TryGetValue(src, out srcRegHex))
            {
                return false;
            }
        }
        else
        {
            if (mnemonic != TokenType.Mov ||
                !s_segmentRegisters.TryGetValue(dest, out destRegHex) ||
                !s_16BitRegisters.TryGetValue(src, out srcRegHex))
            {
                return false;
            }
            isSegment = true;
            opc = 0x8e;
            destRegHex -= SegBase;
        }
        if (mnemonic == TokenType.Xchg)
        {
            if (src == TokenType.Ax || dest == TokenType.Ax)
            {
                state.Output.EmitValue(opc | destRegHex | srcRegHex, ByteOrder.LittleEndian);
                return true;
            }
            opc = 0x86;
        }
        if (s_16BitRegisters.ContainsKey(dest) && !isSegment)
        {
            opc |= W;
        }
        var mode = mnemonic switch
        {
            TokenType.Mov when isSegment => Mode.Reg2Reg | destRegHex | srcRegHex,
            _ => Mode.Reg2Reg | destRegHex | (srcRegHex << 3)
        };
        state.Output.EmitValue(opc, ByteOrder.LittleEndian);
        state.Output.EmitValue(mode,  ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeShiftRegisterRegister(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var dest = operand.Registers[0].Type;
        var src = operand.Registers[1].Type;
        if (src != TokenType.Cl)
        {
            return false;
        }
        // the top part of the opcode is the mod r/m byte, so for reg-reg we set mod to 0b11,
        // 8-bit register is source hence 0b10 in the lower two bits
        var opc = s_shifts[mnemonic] | Mode.Reg2Reg * 256 + D; 
        if (s_16BitRegisters.TryGetValue(dest, out var destHex))
        {
            opc |= W;
        }
        else if (!s_8BitRegisters.TryGetValue(dest, out destHex))
        {
            return false;
        }
        state.Output.EmitValueSized(opc | destHex * 256, 2,  ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool Encode8087RegisterRegister(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var dest = operand.Registers[0].Type;
        var src = operand.Registers[1].Type;
        if (dest.IsStParenReg()) dest = dest.FromStParen();
        if (src.IsStParenReg()) src = src.FromStParen();

        if (!s_regRegOrImm.TryGetValue(mnemonic, out var opc) ||
            !s_8087StackRegisters.TryGetValue(dest, out var destHex) ||
            !s_8087StackRegisters.TryGetValue(src, out var srcHex) ||
            (destHex != 0 && srcHex != 0) ||
            (srcHex != 0 && mnemonic is 
                TokenType.Faddp or
                TokenType.Fcomp or
                TokenType.Fdivp or 
                TokenType.Fdivrp or 
                TokenType.Fmulp or
                TokenType.Fsubp or 
                TokenType.Fsubrp))
        {
            return false;
        }
        if (destHex !=0)
        {
            opc |= 0x4;
            switch (mnemonic)
            {
                case TokenType.Fdiv or TokenType.Fsub:
                    opc += 0x800;
                    break;
                case TokenType.Fdivr or TokenType.Fsubr:
                    opc -= 0x800;
                    break;
            }
        }
        opc |= (destHex + srcHex) * 256;
        state.Output.EmitValueSized(opc,2, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeInOut(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var dest = operand.Registers[0].Type;
        var src = operand.Registers[1].Type;
        switch (mnemonic)
        {
            case TokenType.In:
                if (src != TokenType.Dx)
                {
                    return false;
                }
                switch (dest)
                {
                    case TokenType.Al: state.Output.EmitByte(0xec);
                        return true;
                    case TokenType.Ax: state.Output.EmitByte(0xed);
                        return true;
                    default:
                        return false;
                }
            default:
                if (dest != TokenType.Dx)
                {
                    return false;
                }
                switch (src)
                {
                    case TokenType.Al: state.Output.EmitByte(0xee);
                        return true;
                    case TokenType.Ax: state.Output.EmitByte(0xef);
                        return true;
                    default:
                        return false;
                }
        }
    }
    
    private static bool EncodeEffectiveAddress(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var is8087 = mnemonic.Is8087Opcode();
        int opc;
        switch (operand.CoercedSize)
        {
            case 0 when is8087:
            case 1:
                if (mnemonic == TokenType.Xlat)
                {
                    return EncodeStrings(state, mnemonic, operand);
                }
                if (!s_ea.TryGetValue(mnemonic, out opc)) return false;
                break;
            case 2:
                if (!s_ea16.TryGetValue(mnemonic, out opc) && 
                    (is8087 || !s_ea.TryGetValue(mnemonic, out opc)))
                {
                    return false; 
                }
                if (!is8087) opc |= W;
                break;
            case 4:
                if (!s_ea32.TryGetValue(mnemonic, out opc)) return false; break;
            case 8:
                if (!s_ea64.TryGetValue(mnemonic, out opc)) return false; break;
            case 10:
                if (!s_eaTenByte.TryGetValue(mnemonic, out opc)) return false; break;
            default:
                if (is8087 || !s_ea16.TryGetValue(mnemonic, out opc))
                {
                    return false; 
                }
                opc |= W;
                break;
        }
        var indeces = s_baseIndexRegIndeces[operand.Type];
        var (modeRm, displ, displSize) = GetModeRmAndDisplacement(state, operand);
        EmitSegmentOverride(state, operand, indeces[SegIx]);
        if (is8087 && (opc & 0xff) == 0x9b)
        {
            state.Output.EmitValueSized(opc | (modeRm * 65536), 2, ByteOrder.LittleEndian);
        }
        else
        {
            state.Output.EmitValueSized(opc | (modeRm * 256), 2, ByteOrder.LittleEndian);
        }
        state.Output.EmitValueSized(displ, displSize, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeEffectiveAddressRegister(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (operand.CoercedSize > 2 || mnemonic == TokenType.Lea)
        {
            return EncodeEffectiveAddressLeas(state, mnemonic, operand);
        }
        if (mnemonic is TokenType.Cmps or TokenType.Lods or TokenType.Movs or TokenType.Scas or TokenType.Stos)
        {
            return EncodeStrings(state, mnemonic, operand);
        }
        if (s_shifts.ContainsKey(mnemonic))
        {
            return EncodeShiftEffectiveAddressRegister(state, mnemonic, operand);
        }
        var type = operand.Type;
        var indeces = s_baseIndexRegIndeces[type];
        var regFirst = indeces[RegIx] == 0 && operand.Type != OperandType.DirectRegister;
        var reg = operand.Registers[indeces[3]].Type;
        int regHex;
        int opc;
        if (reg.IsI86Segment())
        {
            if (mnemonic != TokenType.Mov || operand.CoercedSize == 1 || operand.CoercedSize > 2) return false;
            opc = regFirst ? 0x8e : 0x8c;
            regHex = s_segmentRegisters[reg] - SegBase;
        }
        else
        {
            var isDivAcc = mnemonic is TokenType.Div or TokenType.Idiv && reg is TokenType.Al or TokenType.Ax;
            if (isDivAcc)
            {
                return EncodeEffectiveAddressDivAcc(state, mnemonic, operand);
            }
            var reg16First = s_reg16OpcodesFirst.Contains(mnemonic);
            if ((reg16First && (!regFirst || !s_16BitRegisters.ContainsKey(reg) || operand.CoercedSize == 1)) || 
                !s_aluMoveTestXchgEaReg.TryGetValue(mnemonic, out opc) ||
                (s_8BitRegisters.ContainsKey(reg) && operand.CoercedSize > 1) ||
                (s_16BitRegisters.ContainsKey(reg) && operand.CoercedSize == 1) ||
                (!s_8BitRegisters.TryGetValue(reg, out regHex) && !s_16BitRegisters.TryGetValue(reg, out regHex)))
            {
                return false;
            }
            regHex <<= 3;
        }
        var (modeRm, displ, displSize) = GetModeRmAndDisplacement(state, operand);
        modeRm |= regHex;
        var movAccDirect = false;
        if (s_aluMoveTestXchgEaReg.ContainsKey(mnemonic))
        {
            if (mnemonic == TokenType.Mov && modeRm == 0x6 && reg is TokenType.Al or TokenType.Ax)
            {
                movAccDirect = true;
                opc = regFirst ? 0xa0 : 0xa2;
            }
            else if (regFirst && mnemonic != TokenType.Test)
            {
                opc |= D;
            }
            if (s_16BitRegisters.ContainsKey(reg))
            {
                opc |= W;
            }
        }
        EmitSegmentOverride(state, operand, indeces[SegIx]);
        state.Output.EmitByte((byte)opc);
        if (!movAccDirect)
        {
            state.Output.EmitByte((byte)modeRm);
        }
        state.Output.EmitValueSized(displ, displSize, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeEffectiveAddressDivAcc(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var type = operand.Type;
        var indeces = s_baseIndexRegIndeces[type];
        var regFirst = indeces[RegIx] == 0;
        if (!regFirst || !s_ea.TryGetValue(mnemonic, out var opc))
        {
            return false;
        }
        var (modeRm, displ, displSize) = GetModeRmAndDisplacement(state, operand);
        state.Output.EmitValueSized(opc | (modeRm * 256), 2,  ByteOrder.LittleEndian);
        state.Output.EmitValueSized(displ, displSize, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeEffectiveAddressLeas(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if ((operand.CoercedSize != 0 && mnemonic == TokenType.Lea) ||
            (operand.CoercedSize != 4 && mnemonic != TokenType.Lea))
        {
            return false;
        }
        var type = operand.Type;
        var indeces = s_baseIndexRegIndeces[type];
        var regFirst = indeces[RegIx] == 0 && operand.Type != OperandType.DirectRegister;
        var reg = operand.Registers[indeces[3]].Type;
        if (!regFirst ||
            !s_16BitRegisters.TryGetValue(reg, out var regHex) ||
            !s_leas.TryGetValue(mnemonic, out var opc))
        {
            return false;
        }
        var (modeRm, displ, displSize) = GetModeRmAndDisplacement(state, operand);
        modeRm |= regHex << 3;
        EmitSegmentOverride(state, operand, indeces[SegIx]);
        state.Output.EmitByte((byte)opc);
        state.Output.EmitByte((byte)modeRm);
        state.Output.EmitValueSized(displ, displSize, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeShiftEffectiveAddressRegister(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        var indeces = s_baseIndexRegIndeces[operand.Type];
        if (indeces[RegIx] < 1 || 
            operand.Registers[indeces[RegIx]].Type != TokenType.Cl || 
            operand.CoercedSize == 0)
        {
            return false;
        }
        var opc = s_shifts[mnemonic] | D;
        if (operand.CoercedSize == 2)
        {
            opc |= W;
        }
        var (modeRm, displ, displSize) = GetModeRmAndDisplacement(state, operand);
        modeRm *= 256;
        EmitSegmentOverride(state, operand, indeces[SegIx]);
        state.Output.EmitValueSized(opc | modeRm, 2, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(displ, displSize, ByteOrder.LittleEndian);
        return true;
    }

    private static bool EncodeStrings(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (!s_strings86.TryGetValue(mnemonic, out var stringCommand) ||
            (mnemonic == TokenType.Xlat && operand.CoercedSize > 1) ||
            operand.CoercedSize == 0 ||
            operand.CoercedSize > 2 ||
            operand.Expressions.Count > 0 ||
            !stringCommand.RegistersMatch(operand.Registers.Select(t => t.Type)))
        {
            
            return false;
        }
        var hex = stringCommand.Hex;
        if (operand.CoercedSize == 2 || operand.Registers.Any(r => r.Type == TokenType.Ax))
        {
            hex |= W;
        }
        state.Output.EmitValue(hex, ByteOrder.LittleEndian);
        return true;
    }
    
    private static bool EncodeEffectiveAddressImmediate(AssemblyState state, TokenType mnemonic, Operand operand)
    {
        if (operand.CoercedSize > 2) return false;
        var isShift = false;
        if (!s_aluMovTestXchgEaImm.TryGetValue(mnemonic, out var opc))
        {
            if (!s_shifts.TryGetValue(mnemonic, out opc))
            {
                return false;
            }
            isShift = true;
        }
        var indeces = s_baseIndexRegIndeces[operand.Type];
        var (modeRm, displ, displSize) = GetModeRmAndDisplacement(state, operand);
        modeRm *= 256;
        var eval = new Evaluator(state);
        var (minValue, maxValue) = operand.CoercedSize switch
        {
            1 => (sbyte.MinValue, byte.MaxValue),
            _ => (short.MinValue, short.MaxValue)
        };
        var immVal = eval.EvalInteger(operand.Expressions[^1], minValue, maxValue);
        var immValSize = immVal.Size();
        if (operand.CoercedSize == 2)
        {
            opc |= W;
            if (mnemonic == TokenType.Mov) immValSize = 2;
        }
        if (isShift && immVal != 1)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                return false;
            }
            state.PassNeeded = true;
        }
        else if (!isShift &&
                 immVal is >= sbyte.MinValue and <= sbyte.MaxValue && 
                 operand.CoercedSize == 2 && 
                 mnemonic != TokenType.Mov)
        {
            opc |= D;
        }
        EmitSegmentOverride(state, operand, indeces[SegIx]);
        state.Output.EmitValueSized(opc | modeRm, 2, ByteOrder.LittleEndian);
        state.Output.EmitValueSized(displ, displSize, ByteOrder.LittleEndian);
        if (!isShift)
        {
            // don't output the `1` for, e.g. `rol BYTE PTR [bx+si],1`
            state.Output.EmitValueSized(immVal, immValSize, ByteOrder.LittleEndian);
        }
        return true;
    }

    private static (int modeRm, long displ, int displSize) GetModeRmAndDisplacement(AssemblyState state, Operand operand)
    {
        var indeces = s_baseIndexRegIndeces[operand.Type];
        var bas = indeces[BasIx] != NA ? operand.Registers[indeces[BasIx]].Type : TokenType.Eof;
        var inx = indeces[IndIx] != NA ? operand.Registers[indeces[IndIx]].Type : TokenType.Eof;
        var modeRm = Mode.NoDispl | s_rms[(bas, inx)];
        long displ = 0;
        if (operand.Expressions.Count > 0)
        {
            var isImm = operand.Type is OperandType.BaseDisplacementImm 
                or OperandType.BaseIndexDisplacementImm
                or OperandType.BaseIndexImm 
                or OperandType.DirectImm 
                or OperandType.IndirectRegister86Imm
                or OperandType.SegmentOverrideBaseDisplacementImm 
                or OperandType.SegmentOverrideBaseIndexDisplacementImm
                or OperandType.SegmentOverrideBaseIndexImm 
                or OperandType.SegmentOverrideDirectImm
                or OperandType.SegmentOverrideRegisterImm;
            if (!isImm || operand.Expressions.Count > 1)
            {
                var eval = new Evaluator(state);
                displ = eval.EvalInteger(operand.Expressions[0]);
                if (displ is < short.MinValue or > short.MaxValue)
                {
                    // other x86 assemblers don't seem to care about overflow, perhaps we shouldn't either?
                    displ &= 0xffff;
                }
            }
        }
        var displSize = 0;
        if (modeRm == Mode.IndAddr || displ is < sbyte.MinValue or > sbyte.MaxValue)
        {
            displSize = 2;
        }
        else if (displ != 0 || (bas == TokenType.Bp && inx == TokenType.Eof))
        {
            displSize = 1;
        }
        if (modeRm != Mode.IndAddr && displ != 0)
        {
            modeRm |= Mode.Displ8;
            if (displ is < sbyte.MinValue or > sbyte.MaxValue)
            {
                modeRm = (modeRm & ~Mode.Displ8) | Mode.Displ16;
            }
        }
        return (modeRm, displ, displSize);
    }

    private static void EmitSegmentOverride(AssemblyState state, Operand operand, int segIndex)
    {
        if (segIndex == NA)
        {
            return;
        }
        var seg = operand.Registers[segIndex].Type;
        var segHex = s_segmentRegisters[seg];
        if (segHex > 0)
        {
            state.Output.EmitByte((byte)segHex);
        }
    }
}