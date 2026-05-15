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

using Sixty502DotNet.Shared.Lex;

namespace Sixty502DotNet.Shared;

public sealed class AssemblyOptions
{
    public required IList<string> Defines { get; init; }
    
    public required IList<string> Sections { get; init; }
    
    public bool AutosizeRegisters { get; init; }
    
    public bool UseLegacyBlocks { get; init; }
    
    public bool CaseSensitive { get; init; }
    
    public bool BraFor6502 { get; init; }
    
    public bool PseudoBranches6502 { get; init; }
    
    public bool M16 { get; init; }
    
    public bool X16 { get; init; }
    
    public bool TruncateListing { get; init; }
    
    public bool WarningsAsErrors { get; init; }
    
    public bool WarnAmbiguousZp { get; init; }
    
    public bool WarnLeftSpaceOfLabel { get; init; }
    
    public bool WarnCaseMismatch { get; init; }
    
    public bool DoNotWarnBankCrossed { get; init; }
    
    public bool DoNotWarOnIntToFloat { get; init; }
    
    public bool WarnJumpBug { get; init; }
    
    public bool WarnOptimizeResetReg { get; init; }
    
    public bool DoNotWarnOnUnusedSections { get; init; }
    
    public bool WarnRegistersAsIdent { get; init; }
    
    public bool WarnSimplifyCallReturn { get; init; }
    
    public bool WarnTextInNonTextPseudoOp { get; init; }
    
    public bool WarnUnreferencedSymbols { get; init; }
    
    public bool ViceLabels { get; init; }
    
    public bool LabelsAddressesOnly { get; init; }
    
    public bool ListLineNumber { get; init; }
    
    public bool ListMonitorCode { get; init; }
    
    public bool ListDisassembly { get; init; }
    
    public bool ListSourceCode { get; init; }
    
    public bool VerboseList { get; init; }
    
    public bool Checksum { get; init; }
    
    public string? OutputSection { get; init; }
    
    public string? Format { get; init; }
    
    public string? Cpu { get; init; }
    
    public string? IncludePath { get; init; }

    public LexerBehavior LexerBehavior
    {
        get
        {
            if (CaseSensitive)
            {
                return UseLegacyBlocks 
                    ? LexerBehavior.LegacyDelimitersCaseSensitive 
                    : LexerBehavior.DefaultCaseSensitive;
            }
            return UseLegacyBlocks 
                ? LexerBehavior.LegacyDelimitersCaseInsensitive 
                : LexerBehavior.DefaultCaseInsensitive;
        }
    }
}