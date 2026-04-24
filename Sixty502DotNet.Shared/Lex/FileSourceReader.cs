// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Reflection;

namespace Sixty502DotNet.Shared.Lex;

public sealed class FileSourceReader(string? includePath) : ISourceReader
{
    public string? GetSource(string path)
    {
        var filePath = GetFilePath(path, includePath);
        return filePath is null ? null : File.ReadAllText(filePath);
    }

    public byte[]? GetBytes(string path)
    {
        var filePath = GetFilePath(path, includePath);
        return filePath is null ? null : File.ReadAllBytes(filePath);
    }
    
    public static string? GetFilePath(string path, string? includePath)
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
            return Path.GetDirectoryName(dirInfo.FullName)?.Equals(Path.GetDirectoryName(path)) == true 
                ? new FileInfo(path).Name 
                : path;
        }
        return null;
    }
}

public sealed class FileSourceReaderFactory(string? includePath) : ISourceFactory
{
    public ISourceReader CreateReader() => new FileSourceReader(IncludePath);

    public string? IncludePath { get; } = includePath;
}