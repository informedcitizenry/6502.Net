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

using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval;

internal static class EvalValues
{
    public static Value? ConcatStrings
    (
        Value? left,
        string interpolation,
        Value? right,
        TextEncodingCollection? encoding
    )
    {
        if (left == null || right == null) return null;
        if (left.TypeTag != TypeTag.String && right.TypeTag != TypeTag.String)
        {
            return new Value(left.AsInt(encoding) + right.AsInt(encoding));
        }
        var concat = new AsmString();
        if (left.AsAsmString() is {} leftStr)
        {
            concat.Strings.AddRange(leftStr.Strings);
        }
        else
        {
            concat.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, left.AsString()));
        }
        concat.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, interpolation));
        if (right.AsAsmString() is { } rightStr)
        {
            concat.Strings.AddRange(rightStr.Strings);
        }
        else
        {
            concat.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, right.AsString()));
        }
        return new Value(concat);
    }
    
    public static Value? BinaryOp
    (
        Value? left,
        Value? right,
        BinaryOpExpression expression,
        TextEncodingCollection? encoding
    )
    {
        if (left == null || right == null) return null;
        var op = expression.Operator.Type;
        if (!left.IsRValue() || !right.IsRValue())
        {
            throw new InvalidBinaryOperationException(left, right, expression);
        }
        if (left.TypeTag == TypeTag.Boolean && 
            ((op == TokenType.AndAnd && !left.AsBoolean()) ||
             (op == TokenType.OrOr && left.AsBoolean())))
        {
            return left;
        }

        var isStringConcat = left.IsCharOrString() && op == TokenType.Plus &&
                              (right.IsCharOrString() || right.IsNumber());
        if (left.IsCompatibleType(right) || isStringConcat)
        {
            switch (op)
            {
                case TokenType.Plus:
                    if (isStringConcat)
                    {
                        return ConcatStrings(left, string.Empty, right, encoding);
                    }
                    if (left.AsArray() is { } lArray && right.AsArray() is { } rArray && left.TypeTag == TypeTag.Array)
                    {
                        return new Value(lArray.Concat(rArray).ToList(), TypeTag.Array);
                    }
                    if (left.AsDictionary() is { } lDict && right.AsDictionary() is { } rDict)
                    {
                        return new Value(new Dictionary(lDict.Concat(rDict).ToDictionary()));   
                    }
                    break;
                case TokenType.EqEq: 
                    return new Value(left.Equals(right));
                case TokenType.EqEqEq:
                    return new Value(left.IsIdenticalTo(right));
                case TokenType.BangEq:
                    return new Value(!left.Equals(right));
                case TokenType.BangEqEq:
                    return new Value(!left.IsIdenticalTo(right));
                case TokenType.AndAnd when left.TypeTag == TypeTag.Boolean:
                    return new Value(left.AsBoolean() && right.AsBoolean());
                case TokenType.OrOr when left.TypeTag == TypeTag.Boolean:
                    return new Value(left.AsBoolean() || right.AsBoolean());
            }
        }
        if (!left.IsNumber() || !right.IsNumber())
            throw new InvalidBinaryOperationException(left, right, expression);
        if (op == TokenType.Slash && right.AsInt(encoding) == 0)
            throw new CompileException(CompileExceptionType.DivideByZero, expression.Right);
        if (expression.Operator.Type.IsIntOperator() || 
            (left.IsInt() && right.IsInt()))
        {
            if (left.TypeTag == TypeTag.Int128 || right.TypeTag == TypeTag.Int128)
            {
                try
                {
                    var left128 = left.AsInt128(encoding);
                    var right128 = right.AsInt128(encoding);
                    switch (op)
                    {
                        case TokenType.Star: return new Value(left128 * right128);
                        case TokenType.Percent: return new Value(left128 % right128);
                        case TokenType.Plus: return new Value(left128 + right128);
                        case TokenType.Minus: return new Value(left128 - right128);
                        case TokenType.Shl: return new Value(left128 << (int)right128);
                        case TokenType.Shr: return new Value(left128 >> (int)right128);
                        case TokenType.Ashr:
                            return new Value(left128 > 0
                                ? left128 >> (int)right128
                                : -(left128 >> (int)right128));
                        case TokenType.BitwiseAnd: return new Value(left128 & right128);
                        case TokenType.Caret: return new Value(left128 ^ right128);
                        case TokenType.BitwiseOr: return new Value(left128 | right128);
                    }
                }
                catch (OverflowException)
                {
                    throw new CompileException(CompileExceptionType.ValueOverflow, expression);
                }
            }
            try
            {
                var leftInt = left.AsInt(encoding);
                var rightInt = right.AsInt(encoding);
                switch (op)
                {
                    case TokenType.Star: return new Value(leftInt * rightInt);
                    case TokenType.Percent: return new Value(leftInt % rightInt);
                    case TokenType.Plus: return new Value(leftInt + rightInt);
                    case TokenType.Minus: return new Value(leftInt - rightInt);
                    case TokenType.Shl: return new Value(leftInt << (int)rightInt);
                    case TokenType.Shr: return new Value(leftInt >> (int)rightInt);
                    case TokenType.Ashr:
                        return new Value(leftInt > 0
                            ? leftInt >> (int)rightInt
                            : -(leftInt >> (int)rightInt));
                    case TokenType.BitwiseAnd: return new Value(leftInt & rightInt);
                    case TokenType.Caret: return new Value(leftInt ^ rightInt);
                    case TokenType.BitwiseOr: return new Value(leftInt | rightInt);
                }
            }
            catch (OverflowException)
            {
                throw new CompileException(CompileExceptionType.ValueOverflow, expression);
            }
        }
        var leftNum = left.AsDouble(encoding);
        var rightNum = right.AsDouble(encoding);
        return op switch
        {
            TokenType.CaretCaret => new Value(Math.Pow(leftNum, rightNum)),
            TokenType.Star => new Value(leftNum * rightNum),
            TokenType.Slash => new Value(leftNum / rightNum),
            TokenType.Plus => new Value(leftNum + rightNum),
            TokenType.Minus => new Value(leftNum - rightNum),
            TokenType.Lt => new Value(leftNum.FloatLt(rightNum)),
            TokenType.Le => new Value(leftNum.FloatLe(rightNum)),
            TokenType.Gt => new Value(leftNum.FloatGt(rightNum)),
            TokenType.Ge => new Value(leftNum.FloatGe(rightNum)),
            TokenType.Spaceship => new Value(Math.Sign(leftNum - rightNum)),
            TokenType.EqEq => new Value(leftNum.FloatEq(rightNum)),
            TokenType.BangEq => new Value(!leftNum.FloatEq(rightNum)),
            _ => throw new InvalidBinaryOperationException(left, right, expression)
        };
    }

    public static Value? UnaryOp
    (
        Value? value, 
        UnaryOpExpression expression, 
        TextEncodingCollection? encoding
    )
    {
        if (value == null || (value.IsCharOrString() && encoding == null)) return null;
        var op = expression.Operator.Type;
        switch (op)
        {
            case TokenType.Bang:
                return value.TypeTag != TypeTag.Boolean 
                    ? throw new InvalidUnaryOperationException(value, expression) 
                    : new Value(!value.AsBoolean());
            case TokenType.InterpolationStart or TokenType.InterpolationEnd:
            {
                var s = ValueHelper.GetString(expression.Operator.Text.ToString());
                if (s == null)
                {
                    throw new CompileException(CompileExceptionType.InvalidStringLiteral, expression.Operator);
                }

                AsmString asmString;
                if (expression.IsPostfix && value.AsAsmString() is { } asmStr)
                {
                    asmString = asmStr;
                }
                else
                {
                    asmString = new AsmString();
                }
                if (expression.IsPostfix)
                {
                    if (value.TypeTag != TypeTag.String)
                    {
                        asmString.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, value.AsString()));
                    }
                    asmString.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, s));
                }
                else
                {
                    asmString.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, s));
                    asmString.Strings.Add(new EncodingTaggedString(TextEncodingType.Default, value.AsString()));
                }
                return new Value(asmString);
            }
        }

        if (!value.IsNumber())
            throw new InvalidUnaryOperationException(value, expression);
        if (value.TypeTag == TypeTag.Float)
        {
            switch (op)
            {
                case TokenType.Tilde: return new Value(Math.Floor(value.AsDouble(encoding)));
                case TokenType.Plus: return value;
                case TokenType.Minus: return new Value(-value.AsDouble(encoding));
            }
        }
        if (value.TypeTag == TypeTag.Int128)
        {
            switch (op)
            {
                case TokenType.Tilde: return new Value(~value.AsInt128(encoding));
                case TokenType.Plus: return value;
                case TokenType.Minus: return new Value(value.AsInt128(encoding));
            }
        }
        var asInt = value.TypeTag == TypeTag.Int128
            ? (long)(value.AsInt128(encoding) & 0xFFFFFFFFFFFFFFFF)
            : value.AsInt(encoding);
        return op switch
        {
            TokenType.Minus => new Value(-asInt),
            TokenType.Tilde => new Value(~asInt),
            TokenType.Lt => new Value(asInt & 0xff),
            TokenType.Gt => new Value((asInt / 0x100) & 0xff),
            TokenType.BitwiseAnd => new Value(asInt & 0xffff),
            TokenType.Caret => new Value((asInt / 0x10000) & 0xff),
            TokenType.CaretCaret => new Value((asInt / 0x100) & 0xffff),
            _ => value
        };
    }

    public static Value? Subscript
    (
        Value? target,
        Value? rangeStartValue,
        Value? rangeEndValue,
        SubscriptExpression expression,
        TextEncodingCollection? encoding,
        bool ignoreOutOfRangeException
    )
    {
        if (target == null) return null;
        if (target.TypeTag != TypeTag.Array && 
            (target.TypeTag != TypeTag.Tuple ||
            (target.TypeTag == TypeTag.Tuple && expression.Index.Type is RangeType.IsRange or RangeType.IsRangeIncludesEnd)) &&
            target.TypeTag != TypeTag.String && 
            target.TypeTag != TypeTag.Dictionary)
        {
            throw new CompileException(CompileExceptionType.InvalidOperation, expression);
        }
        if (target.AsDictionary() is {} dict)
        {
            if (expression.Index.Type is RangeType.IsRange or RangeType.IsRangeIncludesEnd)
            {
                throw new CompileException(CompileExceptionType.InvalidOperation, expression);
            }
            if (rangeStartValue == null) return null;
            return !dict.TryGetValue(rangeStartValue, out var value) 
                ? throw new CompileException(CompileExceptionType.KeyNotFound, expression.Index.Start as Ast ?? expression.Index) 
                : value;
        }
        if (rangeStartValue == null && rangeEndValue == null) return null;
        long start = 0;
        long end = target.Length;
        var size = end;
        if (rangeStartValue != null)
        {
            if (rangeStartValue.IsCharOrString() && encoding == null)
            {
                return null;
            }
            if (!rangeStartValue.IsNumber())
            {
                throw new TypeException(TypeTag.Float, rangeStartValue, expression.Index.Start as Ast ?? expression.Index);
            }
            start = rangeStartValue.AsInt(encoding);
        }
        if (rangeEndValue != null)
        {
            if (rangeEndValue.IsCharOrString() && encoding == null)
            {
                return null;
            }
            if (!rangeEndValue.IsNumber())
            {
                throw new TypeException(TypeTag.Float, rangeEndValue, expression.Index.End as Ast ?? expression.Index);
            }
            end = rangeEndValue.AsInt(encoding);
        }
        if (expression.Index.Type is RangeType.IsRange or RangeType.IsRangeIncludesEnd)
        {
            if (start < 0)
            {
                start = target.Length + start;
                if (end < 0)
                {
                    return ignoreOutOfRangeException 
                        ? null 
                        : throw new CompileException(CompileExceptionType.IndexOutOfRange, expression);
                }
            }
            if (expression.Index.Type == RangeType.IsRangeIncludesEnd) end++;
            if (start < 0 || end < 0 || start >= end || end > size)
                throw new CompileException(CompileExceptionType.IndexOutOfRange, expression);
            var intStart = (int)start;
            var len = (int)(end - start);
            if (target.AsArray() is { } arr)
            {
                return new Value(arr
                    .Skip(intStart)
                    .Take(len)
                    .ToList(), TypeTag.Array);
            }
            if (target.AsAsmString() is { } asmStr)
            {
                return new Value(asmStr
                        .Substring(intStart, len));
            }
            return null;
        }
        if (rangeStartValue == null) return null;
        if (start < 0) start = size + start;
        if (start < 0 || start >= size)
            return ignoreOutOfRangeException 
                ? null 
                : throw new CompileException(CompileExceptionType.IndexOutOfRange, expression);
        if (target.AsArray() is { } rangeTarget)
        {
            return rangeTarget[(int)start];
        }
        if (target.AsString() is { } rangeStrTarget)
        {
            return new Value(rangeStrTarget[(int)start]);
        }
        return null;
    }

    public static Value? ArrayInit(IList<Value?> values, ArrayInitExpression expression)
    {
        var elements = new List<Value>();
        var firstValue = values.FirstOrDefault();
        if (firstValue == null) return null;
        for (var i = 0; i < values.Count; i++)
        {
            var element = values[i];
            if (element == null) return null;
            if (!element.IsRValue())
            {
                throw new CompileException
                (
                    CompileExceptionType.TypeMismatch,
                    expression.Expressions[i]
                );
            }
            if (i > 0 && !expression.IsTuple && !element.IsCompatibleType(firstValue))
            {
                throw new CompileException
                    (
                        CompileExceptionType.MismatchArrayValueType, 
                        expression.Expressions[i]
                    );
            }
            elements.Add(element);
        }
        return new Value(elements, expression.IsTuple ? TypeTag.Tuple : TypeTag.Array);
    }
    
    public static Value? DictionaryInit
    (
        IList<(Value?, Value?)> keyValuePairs, 
        DictionaryInitExpression expression
    )
    {
        var dictValue = new Dictionary();
        Value? firstKey = null;
        Value? firstValue = null;
        for (var i = 0; i < keyValuePairs.Count; i++)
        {
            var (key, value) = keyValuePairs[i];
            if (key == null) return null;
            if (value == null) return null;
            if (!key.CanBeKey())
            {
                throw new CompileException
                (
                    CompileExceptionType.CannotBeKey, 
                    expression.Members[i].Key
                );
            }

            if (!value.IsRValue())
            {
                throw new CompileException
                    (
                        CompileExceptionType.TypeMismatch, 
                        expression.Members[i].Value
                    );
            }
            if (firstKey == null)
            {
                firstKey = key;
            }
            else if (!firstKey.IsCompatibleType(key))
            {
                throw new CompileException
                    (
                        CompileExceptionType.MismatchKeyTypes, 
                        expression.Members[i].Key
                    );
            }
            if (firstValue == null)
            {
                firstValue = value;
            }
            else if (!firstValue.IsCompatibleType(value))
            {
                throw new CompileException
                    (
                        CompileExceptionType.MismatchValueTypes,
                        expression.Members[i].Value
                    );
            }
            if (!dictValue.TryAdd(key, value))
            {
                throw new CompileException
                    (
                        CompileExceptionType.DuplicateKeyInDictionary,
                        expression.Members[i].Key
                    );
            }
        }
        return new Value(dictValue);
    }
    
    public static Value? EvalAssign
    (
        Value? left, 
        Value? right, 
        BinaryOpExpression expression,
        TextEncodingCollection? encoding
    )
    {
        if (right == null) return null;
        var assignType = expression.Operator.Type switch
        {
            TokenType.StarEq => TokenType.Star,
            TokenType.PercentEq => TokenType.Percent,
            TokenType.SlashEq => TokenType.Slash,
            TokenType.PlusEq => TokenType.Plus,
            TokenType.MinusEq => TokenType.Minus,
            TokenType.ShlEq => TokenType.Shl,
            TokenType.ShrEq => TokenType.Shr,
            TokenType.AshrEq => TokenType.Ashr,
            TokenType.AndEq => TokenType.BitwiseAnd,
            TokenType.CaretEq => TokenType.Caret,
            TokenType.OrEq => TokenType.BitwiseOr,
            _ => expression.Operator.Type
        };
        
        if (assignType == expression.Operator.Type) return right;
        if (left?.TypeTag == TypeTag.Boolean && assignType is TokenType.BitwiseAnd or TokenType.BitwiseOr)
        {
            assignType = assignType switch
            {
                TokenType.BitwiseAnd => TokenType.AndAnd,
                _ => TokenType.OrOr
            };
        }
        var binAssign = new BinaryOpExpression
        (
            expression.Left, 
            expression.Operator.CopyWithType(assignType), 
            expression.Right
        );
        return BinaryOp(left, right, binAssign, encoding);
    }

    public static Value? EvalInterpolExpression
    (
        Value? interpolationVal, 
        Value? width, 
        string? format, 
        InterpolationExpression expression,
        TextEncodingCollection encoding
    )
    {
        if (interpolationVal == null) return null;
        var widthFormat = string.Empty;
        if (width != null)
        {
            if (!width.IsInt())
            {
                throw new TypeException(TypeTag.Int, width, expression.Width ?? expression);
            }
            widthFormat = $", {width.AsInt()}";
        }
        var specifier = !string.IsNullOrEmpty(format) ? $":{format}" : string.Empty;
        var fullFormat = $"{{0{widthFormat}{specifier}}}";
        try
        {
            var strVal = string.Format(new ValueFormatter(encoding), fullFormat, interpolationVal);
            return new Value(strVal, TextEncodingType.Default);
        }
        catch
        {
            throw new CompileException(CompileExceptionType.InvalidFormat, expression);
        }
    }

    public static void CheckTypes(Value v1, Value v2, Expression v2Expression)
    {
        if (v1.IsCompatibleType(v2)) return;
        if (v1.AsArray() is {} v1Array)
        {
            if (v2.AsArray() is {} v2Array)
            {
                if (v1.TypeTag != v2.TypeTag)
                    throw new TypeException(v1.TypeTag, v2, v2Expression);
                    
                if (v2Expression is ArrayInitExpression arrayExpression)
                {
                    throw new TypeException(v1Array[0].TypeTag, v2Array[0], arrayExpression.Expressions[0]);
                }
                throw new TypeException(v1Array[0].TypeTag, v2Array[0], v2Expression);
            }
        }
        if (v1.AsDictionary() is not { } v1Dict || v2.AsDictionary() is not { } v2Dict)
        {
            throw new TypeException(v1.TypeTag, v2, v2Expression);
        }
        var v1Key = v1Dict.Keys.First();
        var v2Key = v2Dict.Keys.First();
        if (!v1Key.IsCompatibleType(v2Key))
        {
            v1 = v1Dict.Keys.First();
            v2 = v2Dict.Keys.First();
            if (v2Expression is DictionaryInitExpression dictExpression)
            {
                v2Expression = dictExpression.Members[0].Key;
            }
        }
        else
        {
            v1 = v1Dict.Values.First();
            v2 = v2Dict.Values.First();
            if (v2Expression is DictionaryInitExpression dictExpression)
            {
                v2Expression = dictExpression.Members[0].Value;
            }
        }
        throw new TypeException(v1.TypeTag, v2, v2Expression);
    }
}