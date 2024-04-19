//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that implements a built-in callable object. This class must be
/// inherited.
/// </summary>
public abstract class BuiltInFunctionObject : FunctionObject
{
    /// <summary>
    /// Construct a new instance of the <see cref="BuiltInFunctionObject"/>.
    /// </summary>
    /// <param name="name">The callable's name.</param>
    /// <param name="arity">The callable's arity. If the value is -1 this means
    /// the callable accepts a non-fixed number of arguments.</param>
	protected BuiltInFunctionObject(string name, int arity)
    {
        Name = name;
        Arity = arity;
    }

    public override int Arity { get; init; }

    /// <summary>
    /// Invoke the built-in callable.
    /// </summary>
    /// <param name="callSite">The original callsite context.</param>
    /// <param name="parameters">The evaluated parameters, if any.</param>
    /// <returns>A <see cref="ValueBase"/>.</returns>
    /// <exception cref="Error"></exception>
	public ValueBase Invoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        if (parameters == null)
        {
            throw new Error(callSite.expr(), "Too few parameters for function");
        }
        if (Arity >= 0)
        {
            if (parameters?.Count != Arity)
            {
                if (parameters?.Count < Arity)
                {
                    throw new Error(callSite.expr(), "Too few parameters for function");
                }
                throw new Error(callSite.exprList(), "Too many parameters for function");
            }
        }
        return OnInvoke(callSite, parameters);
    }

    /// <summary>
    /// The actual implementation of the invoke. This method must be implemented
    /// by the derived class.
    /// </summary>
    /// <param name="callSite">The original callsite context.</param>
    /// <param name="parameters">The evaluated parameters, if any.</param>
    /// <returns>A <see cref="ValueBase"/>.</returns>
    protected abstract ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters);
}

