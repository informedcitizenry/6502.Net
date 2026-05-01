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
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval.Function;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval;

public sealed class Evaluator(AssemblyState assemblyState) : IExpressionVisitor
{
    public const string FileConst = "__FILE__";

    public const string CpuIdConst = "__CPUID__";

    public const string LineConst = "__LINE__";
    
    public Value? VisitPrimaryExpression(PrimaryExpression expression)
    {
        if (!expression.Expr.Type.IsIdent())
        {
            return expression.Expr.Type switch
            {
                TokenType.DollarDollar
                    => new Value(new Label(assemblyState.Output.GetSectionOrProgramStart())),
                TokenType.Star 
                    => new Value(new Label(assemblyState.Output.ProgramCounter)),
                _ => new ExpressionFolder().Visit(expression)
            };
        }
        var symbol = expression.Expr.Text;
        if (symbol.Equals(CpuIdConst, assemblyState.Comparison))
        {
            return new Value(CpuLookup.ReverseLookup(assemblyState.Cpu));
        }
        if (symbol.Equals(FileConst, assemblyState.Comparison))
        {
            return new Value(expression.LeftToken.Source.Name);
        }
        if (symbol.Equals(LineConst, assemblyState.Comparison))
        {
            return new Value(expression.Expr.Line);
        }
        var symVal = assemblyState.SymbolTable.Lookup(symbol.ToString());
        if (symVal != null)
        {
            if (assemblyState.AssemblyOptions.WarnCaseMismatch &&
                assemblyState.SymbolTable.CaseMismatched(symbol.ToString(), true))
            {
                assemblyState.Logger.LogWarning("Mismatch case between reference and definition", expression);
            }
            return symVal;
        }
        assemblyState.PassNeeded |= assemblyState.Passes == 0;
        return assemblyState.PassNeeded 
            ? null 
            : throw new CompileException
            (
                CompileExceptionType.SymbolNotFound, 
                expression
            );
    }

    public Value? VisitAnonymousRefExpression(AnonymousRefExpression expression)
    {
        if (assemblyState.SymbolTable.LookupAnonymous
            (
                expression.Type, 
                expression.Places,
                expression.StatementIndex, 
                out var address))
        {
            if (address != Address.BadAddress) return new Value(new Label(address));
            if (assemblyState is { PassNeeded: false, Passes: > 3 })
            {
                throw new CompileException(CompileExceptionType.AnonymousReferenceNotFound, expression);
            }
            assemblyState.PassNeeded = true;
            address = assemblyState.Output.ProgramCounter;
            return new Value(new Label(address));
        }
        assemblyState.PassNeeded |= assemblyState.Passes == 0;
        return assemblyState.PassNeeded 
            ? null 
            : throw new CompileException
                (
                    CompileExceptionType.AnonymousReferenceNotFound, 
                    expression
                );
    }
    
    public Value? VisitBinaryOpExpression(BinaryOpExpression expression)
    {
        var leftValue = Visit(expression.Left);
        if (leftValue == null || 
            (expression.Operator.Type == TokenType.AndAnd && 
             leftValue.TypeTag == TypeTag.Boolean && 
             !leftValue.AsBoolean()) ||
            (expression.Operator.Type == TokenType.OrOr &&
             leftValue.AsBoolean()))
        {
            return leftValue;
        }
        var rightValue = Visit(expression.Right);
        if (expression.Operator.Type != TokenType.InterpolationStart)
        {
            return EvalValues.BinaryOp(leftValue, rightValue, expression, assemblyState.TextEncodingCollection);
        }
        var middleStr = ValueHelper.GetString(expression.Operator.Text.ToString());
        return middleStr == null 
            ? throw new CompileException(CompileExceptionType.InvalidStringLiteral, expression.Operator)
            : EvalValues.ConcatStrings
            (
                leftValue, 
                middleStr,
                rightValue, 
                assemblyState.TextEncodingCollection
            );
    }
    
    public Value? VisitTernaryExpression(TernaryExpression expression)
    {
        var condValue = Visit(expression.Condition);
        if (condValue == null) return null;
        if (condValue.TypeTag == TypeTag.Boolean)
        {
            if (expression.Condition.IsConstant())
            {
                assemblyState.Logger.LogWarning
                (
                    $"Expression is constant and will always evaluate to {condValue}", 
                    expression.Condition
                );
            }
            return condValue.AsBoolean() ? Visit(expression.Then) : Visit(expression.Else);
        }
        return assemblyState.PassNeeded 
            ? null : 
            throw new TypeException
                (
                    TypeTag.Boolean,
                    condValue,
                    expression.Condition
                );
    }
    

    public Value? VisitUnaryOpExpression(UnaryOpExpression expression) 
        => EvalValues.UnaryOp(Visit(expression.Expr),  expression, assemblyState.TextEncodingCollection);

    public Value? VisitSubscriptExpression(SubscriptExpression expression)
    {
        var targetVal = Visit(expression.Left);
        var startVal = expression.Index.Start != null ? Visit(expression.Index.Start) : null;
        var endVal = expression.Index.End != null ? Visit(expression.Index.End) : null;
        if (targetVal?.AsAddress() == null)
        {
            return EvalValues.Subscript
            (
                targetVal, 
                startVal, 
                endVal, 
                expression, 
                assemblyState.TextEncodingCollection,
                assemblyState.PassNeeded
            );
        }
        if (expression.Index.Type != RangeType.IsIndex)
        {
            throw new CompileException(CompileExceptionType.InvalidOperation, expression);
        }
        if (startVal == null) return null;
        try
        {
            return !startVal.IsNumber()
                ? throw new TypeException(TypeTag.Float, startVal, expression.Index)
                : new Value(assemblyState.Output.Peek((int)(targetVal.AsInt() + startVal.AsInt())));
        }
        catch (OutputException)
        {
            throw new CompileException(CompileExceptionType.InvalidPeekAddress, expression);
        }
    }

    public Value? VisitCallExpression(CallExpression expression)
    {
        var retVal = Invoke(expression);
        if (retVal == null)
        {
            return !assemblyState.PassNeeded 
                ? throw new CompileException
                (
                    CompileExceptionType.NoValueReturned, 
                    expression
                ) 
                : null;
        }
        return retVal;
    }
    
    public Value? VisitFunctionExpression(FunctionExpression expression)
    {
        var defaultValues = new List<Value>();
        for (var i = 0; i < expression.DefaultValues.Count; i++)
        {
            var val = Visit(expression.DefaultValues[i]);
            if (val == null) return null;
            defaultValues.Add(val);
        }
        return new Value(new UserFunction
        (
            assemblyState, 
            expression.Parameters, 
            defaultValues, 
            expression.Body,
            expression.SimpleExpr
        ));
    }

    public Value? VisitMemberExpression(MemberExpression expression)
    {
        var rootVal = Visit(expression.Left);
        if (rootVal == null) return null;
        var symbol = expression.Member.Text;
        var symVal = rootVal.AsResolver()?.LookupLocally(symbol.ToString());
        if (symVal != null) return symVal;
        var typeMembers = assemblyState.SymbolTable.LookupGlobally(rootVal.TypeTag.Name())?.AsResolver();
        symVal = typeMembers?.LookupLocally(symbol.ToString());
        if (symVal != null)
        {
            if (assemblyState.AssemblyOptions.WarnCaseMismatch &&
                assemblyState.SymbolTable.CaseMismatched(symbol.ToString(), false))
            {
                assemblyState.Logger.LogWarning("Mismatch case between reference and definition", expression);
            }
            if (symVal.AsFunction() is Method method)
            {
                return new Value(method.CreateInstance(rootVal));
            }
            return symVal;
        }
        assemblyState.PassNeeded |= assemblyState.Passes == 0;
        return assemblyState.PassNeeded 
            ? null 
            : throw new CompileException(CompileExceptionType.HasNoMemberSymbol, expression.Member);
    }
    
    public Value? VisitArrayInitExpression(ArrayInitExpression expression)
    {
        List<Value?> vals = [];
        vals.AddRange(expression.Expressions.Select(Visit));
        return EvalValues.ArrayInit(vals, expression);
    }

    public Value? VisitDictionaryInitExpression(DictionaryInitExpression expression)
    {
        List<(Value?,Value?)> kvps = [];
        kvps.AddRange(expression.Members.Select(t => (Visit(t.Key), Visit(t.Value))));
        return EvalValues.DictionaryInit(kvps, expression);
    }

    public Value? VisitInterpolationExpression(InterpolationExpression expression)
    {
        var exprVal = Visit(expression.Expr);
        var width = expression.Width != null ? Visit(expression.Width) : null;
        var format = expression.FormatString;
        return EvalValues.EvalInterpolExpression
        (
            exprVal, 
            width, 
            format, 
            expression, 
            assemblyState.TextEncodingCollection
        );
    }

    public Value? Invoke(CallExpression expression)
    {
        var callee =  Visit(expression.Callee);
        if (callee == null) return null;
        var func = callee.AsFunction();
        if (func is null or Method)
        {
            return !assemblyState.PassNeeded 
                ? throw new CompileException
                (
                    CompileExceptionType.NotCallable, 
                    expression.Callee
                ) 
                : null;
        }
        var argCount = expression.Arguments.Count;
        if (func.Arity != argCount)
        {
            if (func.Arity - func.DefaultValues > argCount)
            {
                throw new CompileException(CompileExceptionType.TooFewArguments, expression);
            }
            if (func.Arity < argCount && !func.IsVariant)
            {
                throw new CompileException(CompileExceptionType.TooManyArguments, expression);
            }
        }
        List<Value> args = [];
        
        for (var i = 0; i < argCount; i++)
        {
            var arg = Visit(expression.Arguments[i]);
            if (arg == null) return null;
            args.Add(arg);
        }
        return func.Invoke(args, expression);
    }
    
    public Value? Visit(Expression expression)
    {
        if (!expression.Value.IsDefined)
        {
            return expression.Accept(this);
        }
        if (expression.Value.TypeTag == TypeTag.Float && 
            expression.LeftToken.Type == TokenType.IntLiteral &&
            !assemblyState.AssemblyOptions.DoNotWarOnIntToFloat &&
            expression is PrimaryExpression)
        {
            assemblyState.Logger.LogWarning
            (
                "Number literal is too large to represent as an integer and has been converted to a double. This may lead to precision loss",
                expression
            );
        }
        return expression.Value;
    }

    public long EvalInteger
    (
        Expression expression, 
        long minValue = int.MinValue, 
        long maxValue = uint.MaxValue
    )
    {
        var value = Visit(expression);
        if (value == null) return maxValue;
        if (!value.IsNumber())
            throw new TypeException(TypeTag.Float, value, expression);
        if (value.IsCharOrString())
        {
            try
            {
                var encodedInt = value.AsAsmString() is {} asmString
                    ? assemblyState.TextEncodingCollection.GetEncodedValue(asmString)
                    : assemblyState.TextEncodingCollection.GetEncodedValue(value.AsString());
                if (encodedInt >= minValue && encodedInt <= maxValue)
                {
                    return encodedInt;
                }
            }
            catch { /* ignored */ }
            throw new IntegerOverflowException
            (
                maxValue.Size(),
                minValue, 
                maxValue,
                expression
            );
        }
        var asInt = value.AsInt(assemblyState.TextEncodingCollection);
        if (asInt < minValue || asInt > maxValue)
        {
            if (assemblyState is { PassNeeded: false, Passes: > 0 })
                throw new IntegerOverflowException(maxValue.Size(), minValue, maxValue, expression);
            assemblyState.PassNeeded |= assemblyState.Passes == 0;
        }
        return asInt;
    }

    public byte EvalByte(Expression expression)
        => (byte)(EvalInteger(expression, sbyte.MinValue, byte.MaxValue) & 0xff);

    public long EvalPagedBanked(Expression expression, long minValue = -8388608, long maxValue = 16777215)
    {
        var value = EvalInteger(expression, minValue, maxValue);
        if (!assemblyState.BankOff && value / 0x10000 == assemblyState.Bank)
        {
            value &= 0xffff;
        }
        if (!assemblyState.DirectPageOff && value / 0x100 == assemblyState.DirectPage)
        {
            return value & 0xff;
        }
        return value;
    }
    
    public bool TryFetchSymbolValue(Expression symbol, out Value? value)
    {
        while (true)
        {
            if (symbol is SubscriptExpression subscript)
            {
                symbol = subscript.Left;
                continue;
            }
            value = Visit(symbol);
            return value != null;
        }
    }
}