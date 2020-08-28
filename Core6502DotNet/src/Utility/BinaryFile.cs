//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.IO;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents an in-memory load of a binary file.
    /// </summary>
    public sealed class BinaryFile
    {
        #region Constructor

        /// <summary>
        /// Constructs a new instance of a binary file load.
        /// </summary>
        /// <param name="filename">The filename of the binary file.</param>
        public BinaryFile(string filename) => Filename = filename;

        #endregion

        #region Methods

        /// <summary>
        /// Opens the underlying file specified in the binary file's filenae.
        /// </summary>
        /// <returns><c>true</c> if the file was opened successfully, otherwise <c>false</c>.</returns>
        public bool Open()
        {
            try
            {
                var filename = Filename.TrimOnce('"');
                Data = File.ReadAllBytes(filename);
                Filename = filename;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Properties

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

        #endregion
    }
}