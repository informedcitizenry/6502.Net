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

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Arch.Formats;
using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Function;
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Lex;
using System.Text;

namespace Sixty502DotNet.Shared;

public sealed class AssemblyState
(
    AssemblyOptions assemblyOptions,
    ISourceFactory sourceFactory,
    SymbolTable symTable, 
    ErrorLogger logger, 
    StringComparer comparer
)
{
    public SymbolTable SymbolTable { get; } = symTable;

    public void Reset()
    {
        Output.Reset();
        CachedCalls.Clear();
        AnalysisContexts.Clear();
        DirectPageOff = true;
        DirectPage = 0;
        Bank = 0;
        BankOff = true;
        Format = FormatLookup.ByName(AssemblyOptions.Format) ?? OutputFormat.None;
        PassNeeded = false;
        Listings.Length = 0;
        Cpu = InitialCpu ?? Cpu.M6502;
        PrintOn = true;
        M16 = InitialCpu == Cpu.M65816 && AssemblyOptions.M16;
        X16 = InitialCpu == Cpu.M65816 && AssemblyOptions.X16;
        AutosizeRegisters = AssemblyOptions.AutosizeRegisters;
        TextEncodingCollection.SelectDefaultEncoding();
    }
    
    public AssemblyOptions AssemblyOptions { get; } = assemblyOptions;

    public Output Output { get; } = new(comparer);

    public ISourceFactory SourceFactory { get; } = sourceFactory;
    
    public bool DirectPageOff { get; set; } = true;
    
    public long DirectPage { get; set; }

    public long Bank { get; set; }

    public bool BankOff { get; set; } = true;
    
    public int Passes { get; set; }

    public OutputFormat Format { get; set; } = OutputFormat.None;
    
    public Cpu? InitialCpu { get; set; }
    
    public Cpu Cpu { get; set; } = Cpu.M6502;
    
    public Dictionary<CallRecord, Value> CachedCalls { get; } = new();

    public bool AutosizeRegisters { get; set; }
    
    public bool PassNeeded { get; set; }

    public StringBuilder Listings { get; } = new();

    public StringComparison Comparison { get; }
        = comparer == StringComparer.Ordinal 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

    public StringComparer Comparer { get; } = comparer;
    
    public bool PrintOn { get; set; } = true;

    public bool M16 { get; set; }

    public bool X16 { get; set; }
    
    public bool PrintListing => PrintOn && !PassNeeded && !SymbolTable.InFunction;
    
    public List<Token> UnexpandedMacros { get; } = [];
    
    public TextEncodingCollection TextEncodingCollection { get; } 
        = new (comparer);

    public ErrorLogger Logger { get; } = logger;

    public IList<CodeAnalysisContext> AnalysisContexts { get; } = new List<CodeAnalysisContext>();
}