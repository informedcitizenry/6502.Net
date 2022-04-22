//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.IO;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// A collection class that defines and stores references to binary files
    /// imported with the <c>.binary</c> pseudo-op in assembled source.
    /// </summary>
    public class BinaryFileCollection
    {
        private readonly Dictionary<string, BinaryFile> _includedBinaries;

        /// <summary>
        /// Initialize a new instance of the <see cref="BinaryFileCollection"/>
        /// class.
        /// </summary>
        public BinaryFileCollection()
            => _includedBinaries = new Dictionary<string, BinaryFile>();

        public BinaryFile? Get(string filename)
            => Get(filename, string.Empty);

        /// <summary>
        /// Get the <see cref="BinaryFile"/> object by file name, if the file
        /// exists.
        /// </summary>
        /// <param name="filename">The binary file name.</param>
        /// <param name="includePath">Include a path to search for the
        /// file name.</param>
        /// <returns>A <see cref="BinaryFile"/> in the collection with the
        /// contents of the file specified by the file name if successful,
        /// otherwise <c>null</c>.</returns>
        public BinaryFile? Get(string filename, string includePath)
        {
            filename = filename.TrimOnce('"');
            includePath = includePath.TrimOnce('"');
            if (!_includedBinaries.TryGetValue(filename, out var binaryFile) &&
                !string.IsNullOrEmpty(includePath))
            {
                filename = Path.Combine(includePath, filename);
                _ = _includedBinaries.TryGetValue(filename, out binaryFile);
            }
            try
            {
                if (binaryFile == null)
                {
                    binaryFile = new BinaryFile(filename);
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
                return null;
            }
        }
    }
}
