//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an in-memory load of a binary file.
/// </summary>
public sealed class BinaryFile
{
    private readonly IBinaryFileReader _reader;
    private bool _opened;

    /// <summary>
    /// Constructs a new instance of a binary file load.
    /// </summary>
    /// <param name="filename">The filename of the binary file.</param>
    public BinaryFile(string filename, IBinaryFileReader reader)
    {
        Filename = filename;
        _reader = reader;
        Data = Array.Empty<byte>();
    }

    /// <summary>
    /// Opens the underlying file specified in the binary file's filenae.
    /// </summary>
    /// <returns><c>true</c> if the file was opened successfully, otherwise <c>false</c>.</returns>
    public bool Open()
    {
        if (!_opened)
        {
            try
            {
                Data = _reader.ReadAllBytes(Filename);
                _opened = true;
            }
            catch
            {

            }
        }
        return _opened;
    }

    /// <summary>
    /// Gets the filename of the binary file.
    /// </summary>
    /// <value>The filename.</value>
    public string Filename { get; private set; }

    /// <summary>
    /// Gets the binary file data.
    /// </summary>
    /// <value>The data.</value>
    public byte[] Data { get; private set; }

}

