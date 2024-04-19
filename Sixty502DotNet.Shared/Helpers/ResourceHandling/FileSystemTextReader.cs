//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Reflection;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Provides utility to read text files from the file system.
/// </summary>
public static class FileSystemTextReader
{
    /// <summary>
    /// Read a specified file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="includePath">The include path to search for the file.</param>
    /// <returns>The file path, if found.</returns>
    public static string? ReadFile(string path, string? includePath)
    {
        FileInfo fileInfo = new(path);
        if (!fileInfo.Exists && !string.IsNullOrEmpty(includePath))
        {
            path = Path.Combine(includePath, path);
            fileInfo = new(path);
        }
        if (fileInfo.Exists)
        {
            Uri? location = new(Assembly.GetEntryAssembly()?.Location ?? "");
            DirectoryInfo dirInfo = new(location.AbsolutePath);
            if (Path.GetDirectoryName(dirInfo.FullName)?.Equals(Path.GetDirectoryName(path)) == true)
            {
                return new FileInfo(path).Name;
            }
            return path;
        }
        return null;
    }

}

