//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet;

/// <summary>
/// Represents a parsing error not originating from the generated parser. 
/// </summary>
public sealed class CustomParseError : RecognitionException
{
    /// <summary>
    /// Construct a new instance of a <see cref="CustomParseError"/>.
    /// </summary>
    public CustomParseError()
        : base(null, null)
    {

    }
}