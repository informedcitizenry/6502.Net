//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Implements the <see cref="ICharStreamFactory"/> interface. This class treats
/// the source as actual source code, and is similar in function to the static
/// <see cref="CharStreams.fromString(string)"/> method, but is itself not a
/// <see cref="ICharStream"/> and therefore can be re-used to get a
/// <see cref="ICharStream"/> as often as necessary.
/// </summary>
public class StringSourceFactory : ICharStreamFactory
{
    public ICharStream GetStream(string source)
    {
        return CharStreams.fromString(source);
    }
}

