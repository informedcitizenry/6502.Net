//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.IO;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents an in-memory load of a binary file.
    /// </summary>
    public sealed class BinaryFile
    {
        /// <summary>
        /// Constructs a new instance of a binary file load.
        /// </summary>
        /// <param name="filename">The filename of the binary file.</param>
        public BinaryFile(string filename) =>
            (Filename, Data) = (filename, Array.Empty<byte>());

        /// <summary>
        /// Opens the underlying file specified in the binary file's filenae.
        /// </summary>
        /// <returns><c>true</c> if the file was opened successfully, otherwise <c>false</c>.</returns>
        public bool Open()
        {
            try
            {
                Data = File.ReadAllBytes(Filename);
                return true;
            }
            catch
            {
                return false;
            }
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
}
