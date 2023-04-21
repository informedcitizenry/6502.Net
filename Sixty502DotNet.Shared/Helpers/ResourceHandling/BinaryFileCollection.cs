//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A collection class that defines and stores references to binary files
/// imported with the <c>.binary</c> pseudo-op in assembled source.
/// </summary>
public sealed class BinaryFileCollection
{
    private readonly Dictionary<string, BinaryFile> _includedBinaries;
    private readonly IBinaryFileReader _fileReader;

    /// <summary>
    /// Initialize a new instance of the <see cref="BinaryFileCollection"/>
    /// class.
    /// </summary>
    public BinaryFileCollection(IBinaryFileReader reader)
    {
        _fileReader = reader;
        _includedBinaries = new Dictionary<string, BinaryFile>();
    }

    /// <summary>
    /// Get the <see cref="BinaryFile"/> object by file name, if the file
    /// exists.
    /// </summary>
    /// <param name="filename">The binary file name.</param>
    /// <returns>A <see cref="BinaryFile"/> in the collection with the
    /// contents of the file specified by the file name if successful,
    /// otherwise <c>null</c>.</returns>
    public BinaryFile? Get(string filename)
    {
        filename = filename.TrimOnce('"');

        if (!_includedBinaries.TryGetValue(filename, out var binaryFile))
        {
            try
            {
                if (binaryFile == null)
                {
                    binaryFile = new BinaryFile(filename, _fileReader);
                    if (!binaryFile.Open())
                    {
                        return null;
                    }
                    _includedBinaries[filename] = binaryFile;
                }
                return binaryFile;
            }
            catch
            {

            }
        }
        return binaryFile;
    }
}

