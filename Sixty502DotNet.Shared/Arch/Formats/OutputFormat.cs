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

using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Arch.Formats;

public enum OutputFormat
{
    None,
    Flat,
    Cbm,
    D64,
    T64,
    Cart,
    Xex,
    Msx,
    Apple2,
    ByteSource,
    SRecord,
    SRecMos,
    AmsTap,
    AmsDos,
    Hex,
    Zx,
    Cpm,
    Mz,
    Hex86
}

public static class FormatLookup
{
    private static readonly FrozenDictionary<string, OutputFormat> s_formats 
        = new Dictionary<string, OutputFormat>
    {
        { "none", OutputFormat.None },
        { "amsdos",  OutputFormat.AmsDos },
        { "amstap", OutputFormat.AmsTap },
        { "apple2", OutputFormat.Apple2 },
        { "atari-xex", OutputFormat.Xex },
        { "bytesource", OutputFormat.ByteSource },
        { "cart", OutputFormat.Cart },
        { "cbm", OutputFormat.Cbm },
        { "cmd", OutputFormat.Cpm },
        { "com", OutputFormat.Flat },
        { "cpm", OutputFormat.Cpm },
        { "d64", OutputFormat.D64 },
        { "exe", OutputFormat.Mz },
        { "flat", OutputFormat.Flat },
        { "hex", OutputFormat.Hex },
        { "hex86",  OutputFormat.Hex86 },
        { "msx", OutputFormat.Msx },
        { "mz", OutputFormat.Mz },
        { "srec", OutputFormat.SRecord },
        { "srecmos", OutputFormat.SRecMos },
        { "t64", OutputFormat.T64 },
        { "zx", OutputFormat.Zx }
    }.ToFrozenDictionary();
    
    public static OutputFormat? ByName(string? format)
    {
        if (string.IsNullOrEmpty(format) ||
            !s_formats.TryGetValue(format, out var result)) return null;
        return result;
    }
}