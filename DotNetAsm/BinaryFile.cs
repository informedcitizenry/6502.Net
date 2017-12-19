//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetAsm
{
    /// <summary>
    /// Represents an in-memory load of a binary file.
    /// </summary>
    public class BinaryFile : IEquatable<BinaryFile>
    {
        #region Constructor

        /// <summary>
        /// Constructs a new instance of a binary file load.
        /// </summary>
        /// <param name="filename">The filename of the binary file.</param>
        public BinaryFile(string filename)
        {
            Filename = filename;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens the underlying file specified in the binary file's filenae.
        /// </summary>
        /// <returns><c>True</c> if the file was opened successfully, otherwise <c>false</c>.</returns>
        public bool Open()
        {
            try
            {
                string filename = Filename.Trim('"');

                using (BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
                {
                    long length = reader.BaseStream.Length;
                    byte[] buffer = new byte[length];
                    reader.Read(buffer, 0, (int)length);
                    Data = new List<byte>(buffer);
                }
                this.Filename = filename;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region IEquatable

        /// <summary>
        /// Determines whether this binary file is equal to the other,
        /// based on filename only.
        /// </summary>
        /// <param name="other">The other file.</param>
        /// <returns><c>True</c> if the files (filenames) are equal, otherwise <c>false</c>.</returns>
        public bool Equals(BinaryFile other) => this.Filename == other.Filename;

        #endregion

        #region Override Methods

        /// <summary>
        /// Determines whether this binary file is equal to the other,
        /// based on filename only.
        /// </summary>
        /// <param name="obj">The other file.</param>
        /// <returns><c>True</c> if the files (filenames) are equal, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            BinaryFile other = obj as BinaryFile;
            return this.Filename == other.Filename;
        }

        /// <summary>
        /// Gets the binary file's unique hash.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Filename.GetHashCode();

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
        public List<byte> Data { get; private set; }

        #endregion

    }
}
