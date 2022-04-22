//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace Sixty502DotNet
{
    /// <summary>
    /// Provides helper functionality for reading files.
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Searches for and returns the correct path from the given filename
        /// and optional path to include in the search. If the search finds the
        /// filename in the current runtime directory, the value of the include
        /// path is ignored.
        /// </summary>
        /// <param name="fileName">The filename path.</param>
        /// <param name="includePath">The file include path.</param>
        /// <returns>The filepath, if found, for the filename.</returns>
        public static string? GetPath(string fileName, string? includePath)
        {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists && !string.IsNullOrEmpty(includePath))
            {
                fileName = Path.Combine(includePath, fileName);
                fileInfo = new FileInfo(fileName);
            }
            if (fileInfo.Exists)
            {
                var location = new Uri(Assembly.GetEntryAssembly()?.GetName().CodeBase ?? "");
                var dirInfo = new DirectoryInfo(location.AbsolutePath);
                if (Path.GetDirectoryName(dirInfo.FullName)?.Equals(Path.GetDirectoryName(fileName)) == true)
                    return new FileInfo(fileName).Name;
                return fileName;
            }
            return null;
        }
    }
}
