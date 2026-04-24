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

using Sixty502DotNet.Shared;

namespace Sixty502DotNet.CLI;

public sealed class ParsedCliOptions
{
    public string[] PassedOptions { get; init; } = [];
    
    public bool Quiet { get; init; }
    
    public IList<string> InputPaths { get; init; } = new List<string>();
    
    public string? Cpu { get; init; }
    
    public string? Patch { get; init; }

    public string Output { get; init; } = "a.out";
    
    public bool NoHighlighting { get; init; }
    
    public bool NoStats { get; init; }
    
    public bool NoWarnings { get; init; }

    public string? ListingFile { get; init; }

    public string? LabelFile { get; init; }
    
    public string? ErrorFile { get; init; }
    
    public AssemblyOptions? AssemblyOptions { get; init; }
    
    public DisassemblyOptions? DisassemblyOptions { get; init; }
}