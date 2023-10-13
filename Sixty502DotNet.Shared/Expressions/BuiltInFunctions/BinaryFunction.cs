//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of a function that reads a binary file and
/// returns its contents as an <see cref="ArrayValue"/> of
/// <see cref="NumericValue"/>s.
/// </summary>
public sealed class BinaryFunction : BuiltInFunctionObject
{
    public BinaryFunction()
        : base("binary", -1)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (parameters.Count == 0)
        {
            throw new Error(callSite, "Too few parameters to function");
        }
        int offset = 0;
        int size = 0;
        if (parameters.Count > 1)
        {
            if (parameters.Count > 2)
            {
                if (parameters.Count > 3)
                {
                    throw new Error(callSite.exprList().expr()[^1], "Too many parameters to function");
                }
                size = parameters[2].AsInt();
            }
            offset = parameters[1].AsInt();
        }
        if (BinaryCollection == null)
        {
            throw new ArgumentNullException("Assembler error", nameof(BinaryCollection));
        }
 
        BinaryFile? file = BinaryCollection.Get(parameters[0].AsString());
        if (file?.Open() != true)
        {
            throw new Error(callSite.exprList().expr()[0], $"Could not open file {parameters[0]} for reading");
        }
        if (size == 0)
        {
            size = file.Data.Length;
        }
        if (offset + size > file.Data.Length)
        {
            throw new Error(callSite.exprList(), "Offset and/or size specified too large");
        }
        ArrayValue bytes = new();
        for (int i = 0; i < size; i++)
        {
            bytes.Add(new NumericValue(file.Data[i + offset]));
        }
        return bytes;
    }

    /// <summary>
    /// Get or set the binary file reader responsible for reading a resource
    /// path or URI for its binary contents.
    /// </summary>
    public IBinaryFileReader? BinaryFileReader { get; set; }

    /// <summary>
    /// Get or set the <see cref="BinaryFileCollection"/> that handles opening and reading of a
    /// <see cref="BinaryFile"/>.
    /// </summary>
    public BinaryFileCollection? BinaryCollection { get; set; }
}

