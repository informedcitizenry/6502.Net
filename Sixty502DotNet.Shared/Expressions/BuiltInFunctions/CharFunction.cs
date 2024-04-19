//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
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
        int codePoint = parameters![0].AsInt();
        if (codePoint >= 0 && codePoint <= 0x10FFFF)
        {
            if (_encoding is AsmEncoding enc)
            {
                return new StringValue($"\"{enc.CodepointToString(codePoint)}\"",
                                       _encoding,
                                       enc.EncodingName);
            }
            return new StringValue($"\"{char.ConvertFromUtf32(codePoint)}\"");
        }
        throw new Error(callSite.exprList().expr()[0], "Illegal quantity error");
    }
}

