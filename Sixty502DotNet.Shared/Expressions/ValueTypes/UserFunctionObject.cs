//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents a parsed function expression or declared function, including
/// parameters and body.
/// </summary>
public sealed class UserFunctionObject : FunctionObject
{
	public UserFunctionObject(SyntaxParser.ArgListContext? args,
                            SyntaxParser.BlockContext? statements,
                            SyntaxParser.ExprContext? expr,
                            IScope? closure)
	{
        Arguments = args;
        Statements = statements;
        SingleExpression = expr;
        Arity = GetArgumentCount(args);
        Closure = closure;
    }

    private static int GetArgumentCount(SyntaxParser.ArgListContext? argList)
    {
        if (argList == null)
        {
            return 0;
        }
        int argc = 0;
        if (argList.argList() != null)
        {
            argc += GetArgumentCount(argList.argList());
        }
        if (argList.defaultArgList() != null)
        {
            argc += argList.defaultArgList().ident().Length;
        }
        return argc + argList.ident().Length;
    }

    /// <summary>
    /// Get the parsed arguments.
    /// </summary>
    public SyntaxParser.ArgListContext? Arguments { get; init; }

    /// <summary>
    /// Get the parsed block of statements.
    /// </summary>
    public SyntaxParser.BlockContext? Statements { get; init; }

    /// <summary>
    /// Get the parsed single expression.
    /// </summary>
    public SyntaxParser.ExprContext? SingleExpression { get; init; }

    /// <summary>
    /// Get the closure.
    /// </summary>
    public IScope? Closure { get; init; }

    public override int Arity { get; init; }
}

