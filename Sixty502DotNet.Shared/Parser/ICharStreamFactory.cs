//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// An interface for a class responsible for converting a source parameter string,
/// wheter actual source code or a reference to source, into an ANTLR
/// <see cref="ICharStream"/>. 
/// </summary>
public interface ICharStreamFactory
{
    /// <summary>
    /// Get a stream from the specified source.
    /// </summary>
    /// <param name="source">The source parameter could be a
    /// a web service, a filename or some other URI resource, or even actual
    /// source code. The purpose of this interface is to abstract what the source
    /// itself actual is from the its representation.</param>
    /// <returns>A <see cref="ICharStream"/> suitable for use in tokenizing
    /// the source code.</returns>
    ICharStream GetStream(string source);
}

