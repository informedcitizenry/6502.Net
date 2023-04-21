//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of the function that reads a byte of
/// generated code at the specified offset.
/// </summary>
public sealed class PeekFunction : BuiltInFunctionObject
{
    private readonly CodeOutput _output;

    /// <summary>
    /// Construct a new instance of the <see cref="PeekFunction"/>.
    /// </summary>
    /// <param name="output">The code output object the function references.
    /// </param>
	public PeekFunction(CodeOutput output)
        : base("peek", 1)
    {
        _output = output;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        int addr = (int)parameters![0].AsDouble();
        if (addr < short.MinValue || addr > ushort.MaxValue)
        {
            throw new Error(callSite.exprList().expr()[0], "Illegal quantity");
        }
        return new NumericValue(_output.Peek(addr & 0xffff));
    }
}

