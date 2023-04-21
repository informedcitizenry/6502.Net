//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents the implementation of a function that takes a code point
/// argument and returns a <see cref="StringValue"/>.
/// </summary>
public sealed class CharFunction : BuiltInFunctionObject
{
    private readonly Encoding _encoding;

    /// <summary>
    /// Construct a new instance of the <see cref="CharFunction"/> with the
    /// encoding.
    /// </summary>
    /// <param name="encoding">The encoding for the function.</param>
	public CharFunction(Encoding encoding)
        : base("char", 1)
    {
        _encoding = encoding;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        if (parameters![0].AsDouble() >= 0 && parameters![0].AsDouble() <= 0x10FFFF)
        {
            return new StringValue($"\"{char.ConvertFromUtf32(parameters[0].AsInt())}\"")
            {
                TextEncoding = _encoding
            };
        }
        throw new Error(callSite.exprList().expr()[0], "Illegal quantity error");
    }
}

