//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// A helper class for reading source files.
    /// </summary>
    public static class SourceHelper
    {
        /// <summary>
        /// Read and return the source file's text from the specified path.
        /// </summary>
        /// <param name="fileInfo">A <see cref="FileInfo"/> object representing the source file's information.</param>
        /// <returns>The source text of the file.</returns>
        public static string ReadSource(string fileName, out string filePath)
        {
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists)
            {
                using (var fs = fileInfo.OpenText())
                {
                    filePath = fileInfo.FullName;
                    return fs.ReadToEnd();
                }
            }
            if (!string.IsNullOrEmpty(Assembler.Options.IncludePath))
            {
                filePath = Path.Combine(Assembler.Options.IncludePath, fileName);
                if (File.Exists(filePath))
                    return File.ReadAllText(filePath);
            }
            throw new FileNotFoundException($"Source \"{fileInfo.FullName}\" not found.");
        }
    }
}