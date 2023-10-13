//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of the function to directly update the
/// generated code at the specified offset.
/// </summary>
public sealed class PokeFunction : BuiltInFunctionObject
{
    private readonly CodeOutput _codeOutput;

    /// <summary>
    /// Construct a new instance of the <see cref="PokeFunction"/>.
    /// </summary>
    /// <param name="output">The code output the function references.</param>
	public PokeFunction(CodeOutput output)
        : base("name", 2)
    {
        _codeOutput = output;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        int addr = (int)parameters![0].AsDouble();
        int value = (int)parameters![1].AsDouble();
        if (addr < short.MinValue || addr > ushort.MaxValue || value < sbyte.MinValue || value > byte.MaxValue)
        {
            throw new IllegalQuantityError(callSite.exprList().expr()[0]);
        }
        ValueBase current = new NumericValue(_codeOutput.Peek(addr & 0xffff));
        _codeOutput.Poke(addr & 0xffff, (byte)(value & 0xff));
        return current;
    }
}

