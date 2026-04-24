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
using Sixty502DotNet.Shared.Parse.Ast;
using Environment = Sixty502DotNet.Shared.Compile.Environment;

namespace Sixty502DotNet.Shared.Eval.Function;

public sealed class UserFunction : IFunction
{
    private readonly Environment _closure;
    private readonly AssemblyState _assemblyState;
    private readonly IList<PrimaryExpression> _parameters;
    private readonly IList<Value> _defaultValues;
    private readonly IList<Statement> _body;
    private readonly Expression? _simpleExpr;
    private readonly bool _pure;
    
    public UserFunction(AssemblyState assemblyState,
        IList<PrimaryExpression> parameters,
        IList<Value> defaultValues,
        IList<Statement> body,
        Expression? simpleExpr = null)
    {
        _assemblyState = assemblyState;
        _parameters = parameters;
        _defaultValues = defaultValues;
        _body = body;
        _simpleExpr = simpleExpr;
        _closure = assemblyState.SymbolTable.ActiveEnvironment;
        Arity = parameters.Count;
        DefaultValues = defaultValues.Count;
        var purityChecker = new PurityChecker(assemblyState);
        _pure = _simpleExpr != null || purityChecker.CheckBlock(_body);
    }
    
    public Value? Invoke(IList<Value> arguments, CallExpression callSite)
    {
        var rec = new CallRecord(this, arguments);
        if (_pure && _assemblyState.CachedCalls.TryGetValue(rec, out var retVal))
        {
            return retVal;
        }
        if (!_assemblyState.SymbolTable.PushStack(_closure))
        {
            throw new CompileException(CompileExceptionType.CallDepth, callSite);
        }
        try
        {
            for (var i = 0; i < _parameters.Count; i++)
            {
                var symbol = _parameters[i].LeftToken.Text.ToString();
                if (_assemblyState.SymbolTable.SymbolExistsInScope(symbol))
                    throw new CompileException(CompileExceptionType.SymbolRedefined, _parameters[i]);
                var value = i >= arguments.Count 
                    ? _defaultValues[i - (Arity - DefaultValues)]  
                    : arguments[i];
                _assemblyState.SymbolTable.DefineOrUpdateVariable(_parameters[i].LeftToken, value);
            }
            if (_simpleExpr != null)
            {
                var exprRetVal = new Evaluator(_assemblyState).Visit(_simpleExpr) ?? new Value();
                return exprRetVal;
            }
            var compiler = new Compiler(_assemblyState);
            var jump = compiler.CompileBlock(_body);

            if (jump.Type is JumpType.None or JumpType.Exit)
            {
                return null;
            }
            switch (jump.Type)
            {
                case JumpType.Break:
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    throw new CompileException(CompileExceptionType.InvalidBreak, jump.JumpStatement!);
                case JumpType.Continue:
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    throw new CompileException(CompileExceptionType.InvalidContinue, jump.JumpStatement!);
                case JumpType.Goto:
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    throw new CompileException(CompileExceptionType.CannotFindGoto, jump.JumpStatement!);
                default:
                    if (jump.ReturnValue == null)
                    {
                        return null;
                    }
                    _ = _assemblyState.CachedCalls.TryAdd(rec, jump.ReturnValue);
                    return new Value(jump.ReturnValue);
            }
        }
        finally
        {
            _assemblyState.SymbolTable.PopStack();
        }
    }
    
    public int Arity { get; }

    public int DefaultValues { get; }
    
    public bool IsVariant => false;
}