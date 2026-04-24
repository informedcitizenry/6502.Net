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
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Encode;

public static class EmitData
{
    public static void EncodeStringify(AssemblyState state, IList<Expression> expressions)
    {
        var eval = new Evaluator(state);
        for (var i = 0; i < expressions.Count; i++)
        {
            var val = eval.Visit(expressions[i]);
            if (val == null)
            {
                state.Output.Fill(1);
                continue;
            }
            AsmString asmString;
            if (val.AsAsmString() is {} asm)
            {
                asmString = asm;
            }
            else
            {
                asmString = new AsmString(TextEncodingType.Default, val.ToString());
            }
            var valBytes = state.TextEncodingCollection.GetEncodedBytes(asmString);
            state.Output.EmitBytes(valBytes, ByteOrder.LittleEndian);
        }
    }
    
    public static void Encode(AssemblyState state, PseudoOpStatement statement)
    {
        switch (statement.PseudoOp.Type)
        {
            case TokenType.CstringKw 
                or TokenType.LstringKw 
                or TokenType.NstringKw 
                or TokenType.PstringKw 
                or TokenType.StringKw:
                EncodeStrings(state, statement);
                return;
            case TokenType.BstringKw or TokenType.HstringKw:
                EncodeBinHexStrings(state, statement);
                return;
            case TokenType.CbmfltKw 
                or TokenType.CbmfltpKw 
                or TokenType.DoubleKw
                or TokenType.QwordKw
                or TokenType.TbyteKw:
                EncodeReal(state, statement);
                return;
        }
        var evaluator = new Evaluator(state);
        Func<long, long>? transform = null;
        long maxValue = uint.MinValue;
        long minValue = uint.MaxValue;
        var size = 4;
        switch (statement.PseudoOp.Type)
        {
            case TokenType.AddrKw:
            case TokenType.WordKw:
                maxValue = ushort.MaxValue;
                minValue = ushort.MinValue;
                size = 2;
                break;
            case TokenType.BankBytesKw:
                maxValue = uint.MaxValue;
                minValue = int.MinValue;
                size = 1;
                transform = v => ((int)v & 0xff0000) / 0x10000;
                break;
            case TokenType.ByteKw:
            case TokenType.CharKw:
            case TokenType.SbyteKw:
                if (statement.PseudoOp.Type == TokenType.ByteKw)
                {
                    maxValue = byte.MaxValue;
                    minValue = byte.MinValue;
                }
                else
                {
                    maxValue = sbyte.MaxValue;
                    minValue = sbyte.MinValue;
                }
                size = 1;
                break;
            case TokenType.DwordKw:
                maxValue = uint.MaxValue;
                minValue = uint.MinValue;
                break;
            case TokenType.HibytesKw:
                maxValue = uint.MaxValue;
                minValue = int.MinValue;
                size = 1;
                transform = v => ((int)v & 0xff00) / 256;
                break;
            case TokenType.HiwordsKw:
                maxValue = uint.MaxValue;
                minValue = int.MinValue;
                size = 2;
                transform = v => ((int)v & 0xffff00) / 256;
                break;
            case TokenType.LintKw:
            case TokenType.LongKw:
                size = 3;
                if (statement.PseudoOp.Type == TokenType.LintKw)
                {
                    maxValue = 0x7f_ffff;
                    minValue = -0x80_0000;
                }
                else
                {
                    maxValue = 0xff_ffff;
                    minValue = 0;
                }
                break;        
            case TokenType.LobytesKw:
                maxValue = uint.MaxValue;
                minValue = int.MinValue;
                size = 1;
                transform = v => (int)v & 0xff;
                break;
            case TokenType.LowordsKw:
                maxValue = uint.MaxValue;
                minValue = int.MinValue;
                size = 2;
                transform = v => (int)v & 0xffff;
                break;
            case TokenType.RtaKw:
                maxValue = ushort.MaxValue + 1;
                minValue = ushort.MinValue + 1;
                size = 2;
                transform = (input) => input - 1;
                break;
            case TokenType.ShortKw:
            case TokenType.SintKw:
                maxValue = short.MaxValue;
                minValue = short.MinValue;
                size = 2;
                break;
        }

        for (var i = 0; i < statement.Expressions.Count; i++)
        {
            var expression = statement.Expressions[i];
            if (expression == null)
            {
                state.Output.Fill(size);
            }
            else
            {
                var val = evaluator.Visit(expression);
                if (val == null)
                {
                    state.Output.Fill(size);
                    continue;
                }
                EncodeValue
                (
                    state, 
                    statement.PseudoOp.Type, 
                    expression, 
                    transform, 
                    val, 
                    size, 
                    minValue, 
                    maxValue
                );
            }
        }  
    }
    
    private static void EncodeStrings(AssemblyState state, PseudoOpStatement statement)
    {
        var evaluator = new Evaluator(state);
        var bytes = new List<byte?>();
        Func<byte, Expression, byte>? transform = statement.PseudoOp.Type switch
        {
            TokenType.LstringKw => (chr, e) =>
            {
                if (chr < 0x80) return (byte)(chr << 1);
                return state is { PassNeeded: true, Passes: <= 4 } 
                    ? chr 
                    : throw new CompileException(CompileExceptionType.ValueOverflow, e);
            },
            TokenType.NstringKw => (chr, e) =>
            {
                if (chr < 0x80) return chr;
                return state is { PassNeeded: true, Passes: <= 4 } 
                    ? chr 
                    : throw new CompileException(CompileExceptionType.ValueOverflow, e);
            },
            _ => null
        };
        for (var i = 0; i < statement.Expressions.Count; i++)
        {
            var expression = statement.Expressions[i];
            if (expression == null)
            {
                bytes.Add(null);
            }
            else
            {
                var val = evaluator.Visit(expression);
                if (val == null)
                {
                    state.Output.Fill(1);
                    continue;
                }
                var valBytes = GetStringBytes(state, expression, val);
                valBytes.ForEach(b =>
                {
                    bytes.Add(transform?.Invoke(b, expression) ?? b);
                });
            }
        }
        var indexOfNonNull = bytes.FindLastIndex(b => b != null);
        switch (statement.PseudoOp.Type)
        {
            case TokenType.CstringKw:
                bytes.Add(0x00);
                break;
            case TokenType.LstringKw:
                if (indexOfNonNull > -1 &&
                    bytes[indexOfNonNull] != null)
                {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                    bytes[indexOfNonNull] |= 1;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
                }
                break;
            case TokenType.NstringKw:
                if (indexOfNonNull > -1 &&
                    bytes[indexOfNonNull] != null)
                {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                    bytes[indexOfNonNull] |= 0x80;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
                }
                break;
            case TokenType.PstringKw when bytes.Count > 255:
            {
                var ast = (Ast?)statement.Expressions.Last(e => e != null) ?? statement;
                throw new CompileException(CompileExceptionType.ValueOverflow, ast);
            }
            case TokenType.PstringKw:
                bytes.Insert(0, (byte)bytes.Count);
                break;
        }
        for (var i = 0; i < bytes.Count; i++)
        {
            var b = bytes[i];
            if (b == null)
            {
                state.Output.Fill(1);
            }
            else
            {
                state.Output.EmitByte(b.Value);
            }
        }
    }

    private static void EncodeBinHexStrings(AssemblyState state, PseudoOpStatement statement)
    {
        var evaluator = new Evaluator(state);
        for (var i = 0; i <  statement.Expressions.Count; i++)
        {
            var expression = statement.Expressions[i];
            if (expression == null)
            {
                state.Output.Fill(1);
                continue;
            }
            var val = evaluator.Visit(expression);
            if (val == null)
            {
                state.Output.Fill(1);
                continue;
            }
            EncodeBinHexString(state, statement.PseudoOp.Type, expression, val);
        }
    }

    private static void EncodeBinHexString
    (
        AssemblyState state,
        TokenType pseudoOp,
        Expression expression,
        Value value
    )
    {
        var valArray = value.AsArray();
        if (valArray != null && value.TypeTag == TypeTag.Array)
        {
            for (var i = 0; i < valArray.Count; i++)
            {
                EncodeBinHexString(state, pseudoOp, expression, valArray[i]);
            }
            return;
        }
        var size = pseudoOp == TokenType.HstringKw ? 2 : 8;
        var radix = pseudoOp == TokenType.HstringKw ? 16 : 2;
        var len = size - 1;
        var binHexDigits = new List<string>();
        var startIndex = 0;
        var binHexString = ExpressionFolder.EvalStringLiteral(expression);
        while (startIndex < binHexString.Length)
        {
            binHexDigits.Add(startIndex + len >= binHexString.Length
                ? binHexString[startIndex..]
                : binHexString.Substring(startIndex, size));
            startIndex += size;
        }
        try
        {
            binHexDigits.ForEach(d => state.Output.EmitByte(Convert.ToByte(d, radix)));
        }
        catch (FormatException)
        {
            throw new CompileException(CompileExceptionType.InvalidStringLiteral, expression);
        }
    }
    
    private static void EncodeReal(AssemblyState state, PseudoOpStatement statement)
    {
        if (statement.PseudoOp.Type == TokenType.CbmfltpKw)
        {
            state.Logger.LogWarning
            (
                "`.cbmfltp` is deprecated. Both `.cbmflt` and `.cbmfltp` compile the same data",
                new PrimaryExpression(statement.PseudoOp)
            );
        }
        var fillAmount = statement.PseudoOp.Type switch
        {
            TokenType.CbmfltKw or 
            TokenType.CbmfltpKw => 5,
            TokenType.DoubleKw or
            TokenType.QwordKw => 8,
            _ => 10
        };
        var evaluator = new Evaluator(state);
        for (var i = 0; i < statement.Expressions.Count; i++)
        {
            var expression = statement.Expressions[i];
            if (expression == null)
            {
                state.Output.Fill(fillAmount);
                continue;
            }
            var val = evaluator.Visit(expression);
            if (val == null)
            {
                state.Output.Fill(fillAmount);
                continue;
            }
            EncodeReal
            (   state, 
                statement.PseudoOp.Type, 
                expression, 
                val
            );
        }
    }
    
    private static void EncodeReal
    (
        AssemblyState state,
        TokenType pseudoOp,
        Ast expression,
        Value value
    )
    {
        if (!value.IsNumber())
        {
            var arr = value.AsArray();
            if (arr == null || value.TypeTag is not (TypeTag.Array or TypeTag.Tuple))
                throw new TypeException(TypeTag.Float, value, expression);
            var arrExpression = expression as ArrayInitExpression;
            for (var i = 0; i < arr.Count; i++)
            {
                EncodeReal
                (
                    state, 
                    pseudoOp, 
                    arrExpression !=  null ? arrExpression.Expressions[i] : expression, 
                    arr[i]
                );
            }
        }
        if (state.AssemblyOptions.WarnTextInNonTextPseudoOp && value.TypeTag is TypeTag.String)
        {
            state.Logger.LogWarning($"String text used in non-text pseudo-op {pseudoOp.Stringified()}", expression);
        }
        switch (pseudoOp)
        {
            case TokenType.DoubleKw:
                state.Output.EmitIeee754Value(value.AsDouble(state.TextEncodingCollection), ByteOrder.LittleEndian);
                break;
            case TokenType.QwordKw:
            {
                var i128 = value.AsInt128();
                if ((i128 < long.MinValue || i128 > ulong.MaxValue) && !state.PassNeeded)
                {
                    throw new CompileException(CompileExceptionType.ValueOverflow, expression);
                }
                var bytes = BitConverter.GetBytes(i128).Take(8).ToArray();
                state.Output.EmitBytes(bytes, state.Cpu.ByteOrder());
                break;
            }
            case TokenType.TbyteKw:
            {
                var i128 = value.AsInt128();
                if (i128.ExceedsTByte() && !state.PassNeeded)
                {
                    throw new CompileException(CompileExceptionType.ValueOverflow, expression);
                }
                var bytes = BitConverter.GetBytes(i128).Take(10).ToArray();
                state.Output.EmitBytes(bytes, ByteOrder.LittleEndian);
                break;
            }
            default:
            {
                var doubleValue = value.AsDouble(state.TextEncodingCollection);
                if (doubleValue is < CbmFloat.MinValue or > CbmFloat.MaxValue && !state.PassNeeded)
                {
                    throw new CompileException(CompileExceptionType.ValueOverflow, expression);
                }
                var cbmFlt = ValueHelper.GetCbmFloat(doubleValue, true);
                state.Output.EmitValueSized(cbmFlt, 5, ByteOrder.BigEndian);
                break;
            }
        }
    }
    
    private static void EncodeValue
    (
        AssemblyState state,
        TokenType pseudoOp,
        Expression expression,
        Func<long,long>? transform,
        Value value,
        int size,
        long minValue,
        long maxValue
    )
    {
        if (!value.IsNumber())
        {
            var arr = value.AsArray();
            if (arr == null || value.TypeTag is not (TypeTag.Array or TypeTag.Tuple))
                throw new TypeException(TypeTag.Float, value, expression);
            var arrExpression = expression as ArrayInitExpression;
            for (var i = 0; i < arr.Count; i++)
            {
                EncodeValue
                (
                    state, 
                    pseudoOp, 
                    arrExpression != null ? arrExpression.Expressions[i] : expression, 
                    transform, 
                    arr[i], 
                    size, 
                    minValue, 
                    maxValue
                );
            }
            return;
        }

        var asmStr = value.AsAsmString();
        var intValue = asmStr != null
            ? state.TextEncodingCollection.GetEncodedValue(asmStr) 
            : value.AsInt(state.TextEncodingCollection);
        if (state.AssemblyOptions.WarnTextInNonTextPseudoOp && value.TypeTag is TypeTag.String)
        {
            state.Logger.LogWarning($"String text used in non-text pseudo-op {pseudoOp.Stringified()}", expression);
        }
        intValue = transform?.Invoke(intValue) ?? intValue;
        if ((intValue < minValue || intValue > maxValue) && !state.PassNeeded)
        {
            throw new IntegerOverflowException(size, minValue, maxValue, expression);
        }
        state.Output.EmitValueSized(intValue, size, state.Cpu.ByteOrder());
    }

    private static List<byte> GetStringBytes
    (
        AssemblyState state, 
        Ast ast,
        Value value
    )
    {
        var bytes = new List<byte>();
        var asmStr = value.AsAsmString();
        if (asmStr != null)
        {
            bytes.AddRange(state.TextEncodingCollection.GetEncodedBytes(asmStr));
            return bytes;
        }
        if (value.IsNumber())
        {
            var valNumber = value.AsInt(state.TextEncodingCollection);
            if (valNumber is < int.MinValue or > uint.MaxValue)
            {
                return !state.PassNeeded 
                    ? throw new IntegerOverflowException(4, int.MinValue, uint.MaxValue, ast) 
                    : bytes;
            }
            bytes.AddRange(valNumber.ToBytes(state.Cpu.ByteOrder()));
            return bytes;
        }
        var arr = value.AsArray();
        if (arr == null || value.TypeTag is not (TypeTag.Array or TypeTag.Tuple))
            return state.PassNeeded
                ? bytes
                : throw new TypeException(TypeTag.String, value, ast);
        var arrExpr = ast as ArrayInitExpression;
        for (var i = 0; i < arr.Count; i++)
        {
            bytes.AddRange(GetStringBytes(state, arrExpr != null ? arrExpr.Expressions[i] : ast, arr[i]));
        }
        return bytes;
    }
    
}