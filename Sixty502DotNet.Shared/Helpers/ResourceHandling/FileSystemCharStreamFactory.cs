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
/// the source as a filepath, and is similar in function to the static
/// <see cref="CharStreams.fromPath(string)"/> method, but is itself not a
/// <see cref="ICharStream"/> and therefore can be re-used to get a
/// <see cref="ICharStream"/> as often as necessary.
/// </summary>
public sealed class FileSystemCharStreamFactory : ICharStreamFactory
{
    private readonly string? _includePath;

    /// <summary>
    /// Construct a new instance of a <see cref="FileSystemCharStreamFactory"/>
    /// class.
    /// </summary>
    /// <param name="includePath">The include path to append when accessing
    /// the source.</param>
	public FileSystemCharStreamFactory(string? includePath)
    {
        _includePath = includePath;
    }

    public ICharStream GetStream(string source)
    {
        string? sourcePath = FileSystemTextReader.ReadFile(source, _includePath);
        if (!string.IsNullOrEmpty(sourcePath))
        {
            return CharStreams.fromPath(sourcePath);
        }
        throw new IOException();
    }
}

