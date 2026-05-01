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

internal static class EncodeUtil
{
    public const int Bad = -1;
        
    public static void EnforceBit(Expression expr)
    {
        if (!(expr is PrimaryExpression { Expr.Type: TokenType.IntLiteral } bitExpr &&
              bitExpr.Value.IsInt() &&
              bitExpr.Value.AsInt() <= 7))
        {
            throw new IntegerOverflowException( 1, 0, 7, expr);
        }
    }

    public static bool EncodeImplied
    (
        AssemblyState state, 
        int opcodeHex, 
        int badOpcodeHex, 
        ByteOrder byteOrder = ByteOrder.LittleEndian
    )
    {
        if (opcodeHex == badOpcodeHex) return false;
        state.Output.EmitValue(opcodeHex, byteOrder);
        return true;
    }
    
    public static bool EncodeSingleOperand
    (
        AssemblyState state, 
        int opcodeHex, 
        Expression operand, 
        int operandSize,
        ByteOrder byteOrder = ByteOrder.LittleEndian
    )
    {
        if (opcodeHex == Bad) return false;
        var evaluator = new Evaluator(state);
        var operandVal = evaluator.EvalPagedBanked(operand);
        if (operandVal.Size() > operandSize && !state.PassNeeded)
        {
            return false;
        }
        state.Output.EmitValue(opcodeHex, byteOrder);
        state.Output.EmitValueSized(operandVal, operandSize, byteOrder);
        return true;
    }
    
    public static bool EncodeVariantOperand
    (
        AssemblyState state,
        int zeroPageHex,
        int absoluteHex,
        int longHex,
        int badHex,
        Expression operand,
        ByteOrder byteOrder = ByteOrder.LittleEndian
    )
    {
        var evaluator = new Evaluator(state);
        var operandVal =  evaluator.EvalPagedBanked(operand);
        var opcodeHex = zeroPageHex;
        var size = 1;
        if (opcodeHex == badHex || operandVal.Size() > 1)
        {
            size = 2;
            opcodeHex = absoluteHex;
            if (opcodeHex == badHex || operandVal.Size() > 2)
            {
                if (longHex != badHex)
                {
                    opcodeHex = longHex;
                    size = 3;
                }
                else if (state.PassNeeded)
                {
                    (opcodeHex, size) = absoluteHex == badHex 
                        ? (zeroPageHex, 1) 
                        : (absoluteHex, 2);
                    state.PassNeeded = true;
                }
                else
                {
                    return false;
                }
            }
        }
        if (opcodeHex == badHex && !state.PassNeeded)
            return false;
        state.Output.EmitValue(opcodeHex, byteOrder);
        state.Output.EmitValueSized(operandVal, size, byteOrder);
        return true;
    }
    
    public static bool EncodeRelative
    (
        AssemblyState state, 
        M6xxOpcode opcode, 
        int badHex, 
        Expression operand,
        ByteOrder byteOrder = ByteOrder.LittleEndian
    )
    {
        var evaluator = new Evaluator(state);
        var operandValue = evaluator.EvalInteger(operand);
        var offs = operandValue - (state.Output.ProgramCounter + 2);
        var opcodeHex = opcode.relative;
        var size = 1;
        if (offs < sbyte.MinValue || offs > sbyte.MaxValue || opcodeHex == badHex)
        {
            opcodeHex = opcode.relativeAbsolute;
            var relativeFrom = state.Cpu == Cpu.M65Ce02 ? 1 : 2;
            relativeFrom += opcodeHex.Size();
            if (!state.PassNeeded || opcode.relative == badHex)
            {
                size = 2;
            }
            offs = operandValue - (state.Output.ProgramCounter + relativeFrom);
            if (offs is < short.MinValue or > short.MaxValue) opcodeHex = badHex;
            if (state is { Cpu: Cpu.C64Dtv2, PassNeeded: false })
            {
                offs = operandValue;
                opcodeHex = opcode.absolute;
            }
        }
        if (opcodeHex == badHex)
        {
            if (state is { PassNeeded: false, Passes: > 3 })
            {
                throw new CompileException(CompileExceptionType.RelativeOffsetTooFar, operand);
            }

            state.PassNeeded = true;
            opcodeHex = opcode.relativeAbsolute != badHex 
                ? opcode.relativeAbsolute 
                : opcode.relative;
            size = opcode.relativeAbsolute != badHex ? 2 : 1;
        }
        state.Output.EmitValue(opcodeHex, byteOrder);
        state.Output.EmitValueSized(offs, size, byteOrder);
        return true;
    }
}