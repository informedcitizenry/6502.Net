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
using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Arch;

public static class Keywords
{
    public static bool GetCaseSensitive(string ident, Cpu cpu, bool bra6502, bool pseudoBra6502, out TokenType type)
    {
        while (true)
        {
            switch (cpu)
            {
                case Cpu.C64Dtv2:
                    if (!s_c64DtvKeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.Gb80:
                    return s_gb80KeywordsCaseSensitive.TryGetValue(ident, out type);
                case Cpu.HuC6280:
                    if (!s_huc6280KeywordsCaseSensitive.TryGetValue(ident, out type) && 
                        !s_r65C02KeywordsCaseSensitive.TryGetValue(ident, out type) && 
                        !s_m65C02KeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.I8080:
                    return s_i8080KeywordsCaseSensitive.TryGetValue(ident, out type);
                case Cpu.I86:
                    return s_i86KeywordsCaseSensitive.TryGetValue(ident, out type);
                case Cpu.M65:
                    if (!s_m65KeywordsCaseSensitive.TryGetValue(ident, out type) && 
                        !s_m65Ce02KeywordsCaseSensitive.TryGetValue(ident, out type) && 
                        !s_r65C02KeywordsCaseSensitive.TryGetValue(ident, out type) && 
                        !s_m65C02KeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M6502I:
                    if (!s_m6502IKeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M65816:
                    if (!s_m65816KeywordsCaseSensitive.TryGetValue(ident, out type) && !s_m65C02KeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M65C02:
                    if (!s_m65C02KeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M65Ce02:
                    if (!s_m65Ce02KeywordsCaseSensitive.TryGetValue(ident, out type) && !s_r65C02KeywordsCaseSensitive.TryGetValue(ident, out type) && !s_m65C02KeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M6800:
                    return s_m6800KeywordsCaseSensitive.TryGetValue(ident, out type);
                case Cpu.M6809:
                    return s_m6809KeywordsCaseSensitive.TryGetValue(ident, out type);
                case Cpu.R65C02:
                    if (!s_r65C02KeywordsCaseSensitive.TryGetValue(ident, out type) && !s_m65C02KeywordsCaseSensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.Z80:
                    return s_z80KeywordsCaseSensitive.TryGetValue(ident, out type);
                case Cpu.M6502:
                default:
                    if (bra6502 && s_m6502BraCaseSensitive.TryGetValue(ident, out type)) return true;
                    if (pseudoBra6502 && s_m6502PseudoBraCaseSensitive.TryGetValue(ident, out type)) return true;
                    return s_m6502KeywordsSensitive.TryGetValue(ident, out type);
            }
            return true;
        }
    }

    public static bool GetCaseInsensitive(string ident, Cpu cpu, bool bra6502, bool pseudoBra6502, out TokenType type)
    {
        while (true)
        {
            switch (cpu)
            {
                case Cpu.C64Dtv2:
                    if (!s_c64DtvKeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.Gb80:
                    return s_gb80KeywordsCaseInsensitive.TryGetValue(ident, out type);
                case Cpu.HuC6280:
                    if (!s_huc6280KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_r65C02KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_m65C02KeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.I8080:
                    return s_i8080KeywordsCaseInsensitive.TryGetValue(ident, out type);
                case Cpu.I86:
                    return s_i86KeywordsCaseInsensitive.TryGetValue(ident, out type);
                case Cpu.M65:
                    if (!s_m65KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_m65Ce02KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_r65C02KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_m65C02KeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M6502I:
                    if (!s_m6502IKeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M65816:
                    if (!s_m65816KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_m65C02KeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M65C02:
                    if (!s_m65C02KeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M65Ce02:
                    if (!s_m65Ce02KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_r65C02KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_m65C02KeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.M6800:
                    return s_m6800KeywordsCaseInsensitive.TryGetValue(ident, out type);
                case Cpu.M6809:
                    return s_m6809KeywordsCaseInsensitive.TryGetValue(ident, out type);
                case Cpu.R65C02:
                    if (!s_r65C02KeywordsCaseInsensitive.TryGetValue(ident, out type) &&
                        !s_m65C02KeywordsCaseInsensitive.TryGetValue(ident, out type))
                    {
                        cpu = Cpu.M6502;
                        continue;
                    }
                    break;
                case Cpu.Z80:
                    return s_z80KeywordsCaseInsensitive.TryGetValue(ident, out type);
                case Cpu.M6502:
                default:
                    if (bra6502 && s_m6502BraCaseInsensitive.TryGetValue(ident, out type)) return true;
                    if (pseudoBra6502 && s_m6502PseudoBraCaseInsensitive.TryGetValue(ident, out type)) return true;
                    return s_m6502KeywordsInsensitive.TryGetValue(ident, out type);
            }
            return true;
        }
        
    }

    private static readonly KeyValuePair<string, TokenType>[] s_legacyDirective =
    [
        new( ".endblock", TokenType.EndblockKw ),
        new( ".endenum", TokenType.EndenumKw ),
        new( ".endfunction", TokenType.EndfunctionKw ),
        new( ".endif", TokenType.EndifKw ),
        new( ".endmacro", TokenType.EndmacroKw ),
        new( ".endnamespace", TokenType.EndnamespaceKw ),
        new( ".endpage", TokenType.EndpageKw ),
        new( ".endproc", TokenType.EndprocKw ),
        new( ".endrepeat", TokenType.EndrepeatKw ),
        new( ".endswitch",  TokenType.EndswitchKw ),
        new( ".endwhile", TokenType.EndwhileKw ),
        new( ".next", TokenType.NextKw )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_directives =
    [
        new( ".align", TokenType.AlignKw ),
        new( ".assert", TokenType.AssertKw ),
        new( ".auto",  TokenType.AutoKw ),
        new( ".bank", TokenType.BankKw ),
        new( ".binary", TokenType.BinaryKw ),
        new( ".binclude", TokenType.BincludeKw ),
        new( ".break", TokenType.BreakKw ),
        new( ".continue", TokenType.ContinueKw ),
        new( ".cpu", TokenType.CpuKw ),
        new( ".echo", TokenType.EchoKw ),
        new( ".eor", TokenType.EorKw ),
        new( ".dp", TokenType.DpKw ),
        new( ".map", TokenType.MapKw ),
        new( ".dsection", TokenType.DsectionKw ),
        new( ".encoding", TokenType.EncodingKw ),
        new( ".end", TokenType.EndKw ),
        new( ".endrelocate", TokenType.EndrelocateKw ),
        new( ".equ", TokenType.EquKw ),
        new( ".error", TokenType.ErrorKw ),
        new( ".errorif", TokenType.ErrorifKw ),
        new( ".fill", TokenType.FillKw ),
        new( ".forcepass", TokenType.ForcepassKw ),
        new( ".format", TokenType.FormatKw ),
        new( ".global", TokenType.GlobalKw ),
        new( ".goto", TokenType.GotoKw ),
        new( ".import", TokenType.ImportKw ),
        new( ".include", TokenType.IncludeKw ),
        new( ".initmem", TokenType.InitmemKw ),
        new( ".invoke", TokenType.InvokeKw ),
        new( ".let", TokenType.LetKw ),
        new( ".m8", TokenType.M8Kw ),
        new( ".m16", TokenType.M16Kw ),
        new( ".mx8", TokenType.Mx8Kw ),
        new( ".mx16", TokenType.Mx16Kw ),
        new( ".manual",  TokenType.ManualKw ),
        new( ".pseudopc", TokenType.PseudoPcKw ),
        new( ".realpc", TokenType.RealPcKw ),
        new( ".relocate", TokenType.RelocateKw ),
        new( ".return", TokenType.ReturnKw ),
        new( ".section",  TokenType.SectionKw ),
        new( ".stringify", TokenType.StringifyKw ),
        new( ".unmap",  TokenType.UnmapKw ),
        new( ".warn", TokenType.WarnKw ),
        new( ".warnif", TokenType.WarnifKw ),
        new( ".x8", TokenType.X8Kw ),
        new( ".x16", TokenType.X16Kw ),
        new( ".block", TokenType.BlockKw ),
        new( ".case", TokenType.CaseKw ),
        new( ".do", TokenType.DoKw ),
        new( ".default",  TokenType.DefaultKw ),
        new( ".else", TokenType.ElseKw ),
        new( ".elseif", TokenType.ElseifKw ),
        new( ".elseifdef", TokenType.ElseifdefKw ),
        new( ".elseifndef", TokenType.ElseifndefKw ),
        new( ".enum", TokenType.EnumKw ),
        new( ".for", TokenType.ForKw ),
        new( ".foreach", TokenType.ForeachKw ),
        new( ".function", TokenType.FunctionKw ),
        new( ".if", TokenType.IfKw ),
        new( ".ifdef", TokenType.IfdefKw ),
        new( ".ifndef", TokenType.IfndefKw ),
        new( ".macro", TokenType.MacroKw ),
        new( ".namespace", TokenType.NamespaceKw ),
        new( ".org",  TokenType.OrgKw ),
        new( ".page", TokenType.PageKw ),
        new( ".proc", TokenType.ProcKw ),
        new( ".proff", TokenType.ProffKw ),
        new( ".pron", TokenType.PronKw ),
        new( ".repeat", TokenType.RepeatKw ),
        new( ".switch", TokenType.SwitchKw ),
        new( ".while", TokenType.WhileKw ),
        new( ".whiletrue", TokenType.WhiletrueKw ),
        new( ".addr", TokenType.AddrKw ),
        new( ".bankbytes", TokenType.BankBytesKw ),
        new( ".bstring", TokenType.BstringKw ),
        new( ".byte", TokenType.ByteKw ),
        new( ".cbmflt", TokenType.CbmfltKw ),
        new( ".cbmfltp", TokenType.CbmfltpKw ),
        new( ".char", TokenType.CharKw ),
        new( ".cstring", TokenType.CstringKw ),
        new( ".dint", TokenType.DintKw ),
        new( ".double", TokenType.DoubleKw ),
        new( ".dword", TokenType.DwordKw ),
        new( ".hibytes", TokenType.HibytesKw ),
        new( ".hiwords", TokenType.HiwordsKw ),
        new( ".hstring", TokenType.HstringKw ),
        new( ".lint", TokenType.LintKw ),
        new( ".lobytes", TokenType.LobytesKw ),
        new( ".long", TokenType.LongKw ),
        new( ".lowords", TokenType.LowordsKw ),
        new( ".lstring", TokenType.LstringKw ),
        new( ".nstring", TokenType.NstringKw ),
        new( ".pstring", TokenType.PstringKw ),
        new( ".rta", TokenType.RtaKw ),
        new( ".sbyte", TokenType.SbyteKw ),
        new( ".short", TokenType.ShortKw ),
        new( ".sint", TokenType.SintKw ),
        new( ".string", TokenType.StringKw ),
        new( ".word", TokenType.WordKw ),
        new( ".double", TokenType.DoubleKw ),
        new( ".qword", TokenType.QwordKw ),
        new( ".tbyte", TokenType.TbyteKw )
    ];
    
    private static readonly KeyValuePair<string, TokenType>[] s_m6502Keywords =
    [
        new( "a", TokenType.A ),
        new( "adc", TokenType.Adc ),
        new( "and", TokenType.And ),
        new( "asl", TokenType.Asl ),
        new( "bcc", TokenType.Bcc ),
        new( "bcs", TokenType.Bcs ),
        new( "beq", TokenType.Beq ),
        new( "bit", TokenType.Bit ),
        new( "bmi", TokenType.Bmi ),
        new( "bne", TokenType.Bne ),
        new( "bpl", TokenType.Bpl ),
        new( "brk", TokenType.Brk ),
        new( "bvc", TokenType.Bvc ),
        new( "bvs", TokenType.Bvs ),
        new( "clc", TokenType.Clc ),
        new( "cld", TokenType.Cld ),
        new( "cli", TokenType.Cli ),
        new( "clv", TokenType.Clv ),
        new( "cmp", TokenType.Cmp ),
        new( "cpx", TokenType.Cpx ),
        new( "cpy", TokenType.Cpy ),
        new( "dec", TokenType.Dec ),
        new( "dex", TokenType.Dex ),
        new( "dey", TokenType.Dey ),
        new( "eor", TokenType.Eor ),
        new( "inc", TokenType.Inc ),
        new( "inx", TokenType.Inx ),
        new( "iny", TokenType.Iny ),
        new( "jmp", TokenType.Jmp ),
        new( "jsr", TokenType.Jsr ),
        new( "lda", TokenType.Lda ),
        new( "ldx", TokenType.Ldx ),
        new( "ldy", TokenType.Ldy ),
        new( "lsr", TokenType.Lsr ),
        new( "nop", TokenType.Nop ),
        new( "ora", TokenType.Ora ),
        new( "pha", TokenType.Pha ),
        new( "php", TokenType.Php ),
        new( "pla", TokenType.Pla ),
        new( "plp", TokenType.Plp ),
        new( "rol", TokenType.Rol ),
        new( "ror", TokenType.Ror ),
        new( "rti", TokenType.Rti ),
        new( "rts", TokenType.Rts ),
        new( "sbc", TokenType.Sbc ),
        new( "sec", TokenType.Sec ),
        new( "sed", TokenType.Sed ),
        new( "sei", TokenType.Sei ),
        new( "sta", TokenType.Sta ),
        new( "stx", TokenType.Stx ),
        new( "sty", TokenType.Sty ),
        new( "tax", TokenType.Tax ),
        new( "tay", TokenType.Tay ),
        new( "tsx", TokenType.Tsx ),
        new( "txa", TokenType.Txa ),
        new( "txs", TokenType.Txs ),
        new( "tya", TokenType.Tya ),
        new( "x", TokenType.X ),
        new( "y", TokenType.Y )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m6502Bra =
    [
        new( "bra", TokenType.Bra )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m6502PseudoBra =
    [
        new( "jcc", TokenType.Jcc ),
        new( "jcs", TokenType.Jcs ),
        new( "jeq", TokenType.Jeq ),
        new( "jne", TokenType.Jne ),
        new( "jmi", TokenType.Jmi ),
        new( "jpl", TokenType.Jpl ),
        new( "jvc", TokenType.Jvc ),
        new( "jvs", TokenType.Jvs )
    ];

    
    // ReSharper disable once InconsistentNaming
    private static readonly KeyValuePair<string, TokenType>[] s_c64dtvKeywords =
    [
        new( "ane", TokenType.Ane ),
        new( "arr", TokenType.Arr ),
        new( "bra", TokenType.Bra ),
        new( "dcp", TokenType.Dcp ),
        new( "gra", TokenType.Gra ),
        new( "isb", TokenType.Isb ),
        new( "lax", TokenType.Lax ),
        new( "rla", TokenType.Rla ),
        new( "rra", TokenType.Rra ),
        new( "sir", TokenType.Sir ),
        new( "slo", TokenType.Slo ),
        new( "sre", TokenType.Sre ),
        new( "stp", TokenType.Stp )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m6502IKeywords =
    [
        new( "anc", TokenType.Anc ),
        new( "ane", TokenType.Ane ),
        new( "arr", TokenType.Arr ),
        new( "asr", TokenType.Asr ),
        new( "dcp", TokenType.Dcp ),
        new( "dop", TokenType.Dop ),
        new( "isb", TokenType.Isb ),
        new( "jam", TokenType.Jam ),
        new( "las", TokenType.Las ),
        new( "lax", TokenType.Lax ),
        new( "rla", TokenType.Rla ),
        new( "rra", TokenType.Rra ),
        new( "sax", TokenType.Sax ),
        new( "sha", TokenType.Sha ),
        new( "shx", TokenType.Shx ),
        new( "shy", TokenType.Shy ),
        new( "slo", TokenType.Slo ),
        new( "sre", TokenType.Sre ),
        new( "stp", TokenType.Stp ),
        new( "tas", TokenType.Tas ),
        new( "top", TokenType.Top )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m65816Keywords =
    [
        new( "bra", TokenType.Bra ),
        new( "brl", TokenType.Brl ),
        new( "cop", TokenType.Cop ),
        new( "jml", TokenType.Jml ),
        new( "jsl", TokenType.Jsl ),
        new( "mvn", TokenType.Mvn ),
        new( "mvp", TokenType.Mvp ),
        new( "pea", TokenType.Pea ),
        new( "pei", TokenType.Pei ),
        new( "per", TokenType.Per ),
        new( "phb", TokenType.Phb ),
        new( "phd", TokenType.Phd ),
        new( "phk", TokenType.Phk ),
        new( "phx", TokenType.Phx ),
        new( "phy", TokenType.Phy ),
        new( "plb", TokenType.Plb ),
        new( "pld", TokenType.Pld ),
        new( "plx", TokenType.Plx ),
        new( "ply", TokenType.Ply ),
        new( "rep", TokenType.Rep ),
        new( "rtl", TokenType.Rtl ),
        new( "s", TokenType.S ),
        new( "sep", TokenType.Sep ),
        new( "stp", TokenType.Stp ),
        new( "stz", TokenType.Stz ),
        new( "tcd", TokenType.Tcd ),
        new( "tcs", TokenType.Tcs ),
        new( "tdc", TokenType.Tdc ),
        new( "trb", TokenType.Trb ),
        new( "tsb", TokenType.Tsb ),
        new( "tsc", TokenType.Tsc ),
        new( "txy", TokenType.Txy ),
        new( "tyx", TokenType.Tyx ),
        new( "wai", TokenType.Wai ),
        new( "wdm", TokenType.Wdm ),
        new( "xba", TokenType.Xba ),
        new( "xce", TokenType.Xce )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m65C02Keywords =
    [
        new( "bra", TokenType.Bra ),
        new( "phx", TokenType.Phx ),
        new( "phy", TokenType.Phy ),
        new( "plx", TokenType.Plx ),
        new( "ply", TokenType.Ply ),
        new( "stz", TokenType.Stz ),
        new( "trb", TokenType.Trb ),
        new( "tsb", TokenType.Tsb ),
        new( "stp", TokenType.Stp ),
        new( "wai", TokenType.Wai )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_r65C02Keywords =
    [
        new( "bbr", TokenType.Bbr ),
        new( "bbs", TokenType.Bbs ),
        new( "rmb", TokenType.Rmb ),
        new( "smb", TokenType.Smb )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m65Ce02Keywords =
    [
        new( "asr", TokenType.Asr ),
        new( "asw", TokenType.Asw ),
        new( "bge", TokenType.Bge ),
        new( "blt", TokenType.Blt ),
        new( "bsr", TokenType.Bsr ),
        new( "cle", TokenType.Cle ),
        new( "cpz", TokenType.Cpz ),
        new( "dez", TokenType.Dez ),
        new( "dew", TokenType.Dew ),
        new( "eom", TokenType.Eom ),
        new( "inw", TokenType.Inw ),
        new( "inz", TokenType.Inz ),
        new( "ldz", TokenType.Ldz ),
        new( "map", TokenType.Map ),
        new( "neg", TokenType.Neg ),
        new( "phw", TokenType.Phw ),
        new( "phz", TokenType.Phz ),
        new( "plz", TokenType.Plz ),
        new( "row", TokenType.Row ),
        new( "rtn", TokenType.Rtn ),
        new( "see", TokenType.See ),
        new( "sp", TokenType.Sp ),
        new( "tab", TokenType.Tab ),
        new( "taz", TokenType.Taz ),
        new( "tba", TokenType.Tba ),
        new( "tsy", TokenType.Tsy ),
        new( "tys", TokenType.Tys ),
        new( "tza", TokenType.Tza ),
        new( "z", TokenType.Z )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_huc6280Keywords =
    [
        new( "cla", TokenType.Cla ),
        new( "clx", TokenType.Clx ),
        new( "cly", TokenType.Cly ),
        new( "csh", TokenType.Csh ),
        new( "csl", TokenType.Csl ),
        new( "say", TokenType.Say ),
        new( "set", TokenType.Set ),
        new( "st1", TokenType.St1 ),
        new( "st2", TokenType.St2 ),
        new( "sxy", TokenType.Sxy ),
        new( "tai", TokenType.Tai ),
        new( "tam", TokenType.Tam ),
        new( "tdd", TokenType.Tdd ),
        new( "tia", TokenType.Tia ),
        new( "tii", TokenType.Tii ),
        new( "tin", TokenType.Tin ),
        new( "tma", TokenType.Tma ),
        new( "tst", TokenType.Tst )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m65Keywords =
    [
        new( "adcq", TokenType.Adcq ),
        new( "andq", TokenType.Andq ),
        new( "aslq", TokenType.Aslq ),
        new( "asrq", TokenType.Asrq ),
        new( "bitq", TokenType.Bitq ),
        new( "cpq", TokenType.Cpq ),
        new( "deq", TokenType.Deq ),
        new( "eorq", TokenType.Eorq ),
        new( "inq", TokenType.Inq ),
        new( "ldq", TokenType.Ldq ),
        new( "lsrq", TokenType.Lsrq ),
        new( "orq", TokenType.Orq ),
        new( "rolq", TokenType.Rolq ),
        new( "rorq", TokenType.Rorq ),
        new( "sbcq", TokenType.Sbcq ),
        new( "stq", TokenType.Stq )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m6800Keywords =
    [
        new( "aba", TokenType.Aba ),
        new( "adca", TokenType.Adca ),
        new( "adcb", TokenType.Adcb ),
        new( "adda", TokenType.Adda ),
        new( "addb", TokenType.Addb ),
        new( "anda", TokenType.Anda ),
        new( "andb", TokenType.Andb ),
        new( "asl", TokenType.Asl ),
        new( "asla", TokenType.Asla ),
        new( "aslb", TokenType.Aslb ),
        new( "asr", TokenType.Asr ),
        new( "asra", TokenType.Asra ),
        new( "asrb", TokenType.Asrb ),
        new( "bcc", TokenType.Bcc ),
        new( "bcs", TokenType.Bcs ),
        new( "beq", TokenType.Beq ),
        new( "bge", TokenType.Bge ),
        new( "bgt", TokenType.Bgt ),
        new( "bhi", TokenType.Bhi ),
        new( "bita", TokenType.Bita ),
        new( "bitb", TokenType.Bitb ),
        new( "ble", TokenType.Ble ),
        new( "bls", TokenType.Bls ),
        new( "blt", TokenType.Blt ),
        new( "bmi", TokenType.Bmi ),
        new( "bne", TokenType.Bne ),
        new( "bpl", TokenType.Bpl ),
        new( "bra", TokenType.Bra ),
        new( "bsr", TokenType.Bsr ),
        new( "bvc", TokenType.Bvc ),
        new( "bvs", TokenType.Bvs ),
        new( "cba", TokenType.Cba ),
        new( "clc", TokenType.Clc ),
        new( "cli", TokenType.Cli ),
        new( "clr", TokenType.Clr ),
        new( "clra", TokenType.Clra ),
        new( "clrb", TokenType.Clrb ),
        new( "clv", TokenType.Clv ),
        new( "cmpa", TokenType.Cmpa ),
        new( "cmpb", TokenType.Cmpb ),
        new( "com", TokenType.Com ),
        new( "coma", TokenType.Coma ),
        new( "comb", TokenType.Comb ),
        new( "cpxa", TokenType.Cpxa ),
        new( "daa", TokenType.Daa ),
        new( "dec", TokenType.Dec ),
        new( "deca", TokenType.Deca ),
        new( "decb", TokenType.Decb ),
        new( "des", TokenType.Des ),
        new( "dex", TokenType.Dex ),
        new( "eora", TokenType.Eora ),
        new( "eorb", TokenType.Eorb ),
        new( "inc", TokenType.Inc ),
        new( "inca", TokenType.Inca ),
        new( "incb", TokenType.Incb ),
        new( "ins", TokenType.Ins ),
        new( "inx", TokenType.Inx ),
        new( "jmp", TokenType.Jmp ),
        new( "jsr", TokenType.Jsr ),
        new( "ldaa", TokenType.Ldaa ),
        new( "ldab", TokenType.Ldab ),
        new( "lds", TokenType.Lds ),
        new( "ldx", TokenType.Ldx ),
        new( "lsr", TokenType.Lsr ),
        new( "lsra", TokenType.Lsra ),
        new( "lsrb", TokenType.Lsrb ),
        new( "neg", TokenType.Neg ),
        new( "nega", TokenType.Nega ),
        new( "negb", TokenType.Negb ),
        new( "nop", TokenType.Nop ),
        new( "oraa", TokenType.Oraa ),
        new( "orab", TokenType.Orab ),
        new( "psha", TokenType.Psha ),
        new( "pshb", TokenType.Pshb ),
        new( "pula", TokenType.Pula ),
        new( "pulb", TokenType.Pulb ),
        new( "rol", TokenType.Rol ),
        new( "rola", TokenType.Rola ),
        new( "rolb", TokenType.Rolb ),
        new( "ror", TokenType.Ror ),
        new( "rora", TokenType.Rora ),
        new( "rorb", TokenType.Rorb ),
        new( "rti", TokenType.Rti ),
        new( "rts", TokenType.Rts ),
        new( "sba", TokenType.Sba ),
        new( "sbca", TokenType.Sbca ),
        new( "sbcb", TokenType.Sbcb ),
        new( "sec", TokenType.Sec ),
        new( "sei", TokenType.Sei ),
        new( "sev", TokenType.Sev ),
        new( "staa", TokenType.Staa ),
        new( "stab", TokenType.Stab ),
        new( "sts", TokenType.Sts ),
        new( "stx", TokenType.Stx ),
        new( "suba", TokenType.Suba ),
        new( "subb", TokenType.Subb ),
        new( "swi", TokenType.Swi ),
        new( "tab", TokenType.Tab ),
        new( "tap", TokenType.Tap ),
        new( "tba", TokenType.Tba ),
        new( "tpa", TokenType.Tpa ),
        new( "tst", TokenType.Tst ),
        new( "tsta", TokenType.Tsta ),
        new( "tstb", TokenType.Tstb ),
        new( "tsx", TokenType.Tsx ),
        new( "txs", TokenType.Txs ),
        new( "wai", TokenType.Wai ),
        new( "X", TokenType.X )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_m6809Keywords =
    [
        new( "a", TokenType.A ),
        new( "abx", TokenType.Abx ),
        new( "adca", TokenType.Adca ),
        new( "adcb", TokenType.Adcb ),
        new( "adda", TokenType.Adda ),
        new( "addb", TokenType.Addb ),
        new( "addd", TokenType.Addd ),
        new( "anda", TokenType.Anda ),
        new( "andb", TokenType.Andb ),
        new( "andcc", TokenType.Andcc ),
        new( "asl", TokenType.Asl ),
        new( "asla", TokenType.Asla ),
        new( "aslb", TokenType.Aslb ),
        new( "asr", TokenType.Asr ),
        new( "asra", TokenType.Asra ),
        new( "asrb", TokenType.Asrb ),
        new( "b", TokenType.B ),
        new( "bcc", TokenType.Bcc ),
        new( "bcs", TokenType.Bcs ),
        new( "beq", TokenType.Beq ),
        new( "bge", TokenType.Bge ),
        new( "bgt", TokenType.Bgt ),
        new( "bhi", TokenType.Bhi ),
        new( "bhs", TokenType.Bhs ),
        new( "bita", TokenType.Bita ),
        new( "bitb", TokenType.Bitb ),
        new( "ble", TokenType.Ble ),
        new( "blo", TokenType.Blo ),
        new( "bls", TokenType.Bls ),
        new( "blt", TokenType.Blt ),
        new( "bmi", TokenType.Bmi ),
        new( "bne", TokenType.Bne ),
        new( "bpl", TokenType.Bpl ),
        new( "bra", TokenType.Bra ),
        new( "brn", TokenType.Brn ),
        new( "bsr", TokenType.Bsr ),
        new( "bvc", TokenType.Bvc ),
        new( "bvs", TokenType.Bvs ),
        new( "cc", TokenType.CcReg ),
        new( "clr", TokenType.Clr ),
        new( "clra", TokenType.Clra ),
        new( "clrb", TokenType.Clrb ),
        new( "cmpa", TokenType.Cmpa ),
        new( "cmpb", TokenType.Cmpb ),
        new( "cmpd", TokenType.Cmpd ),
        new( "cmps", TokenType.Cmps ),
        new( "cmpu", TokenType.Cmpu ),
        new( "cmpx", TokenType.Cmpx ),
        new( "cmpy", TokenType.Cmpy ),
        new( "com", TokenType.Com ),
        new( "coma", TokenType.Coma ),
        new( "comb", TokenType.Comb ),
        new( "cpxa", TokenType.Cpxa ),
        new( "cwai", TokenType.Cwai ),
        new( "d", TokenType.D ),
        new( "daa", TokenType.Daa ),
        new( "dec", TokenType.Dec ),
        new( "deca", TokenType.Deca ),
        new( "decb", TokenType.Decb ),
        new( "dp", TokenType.Dp ),
        new( "eora", TokenType.Eora ),
        new( "eorb", TokenType.Eorb ),
        new( "exg", TokenType.Exg ),
        new( "inc", TokenType.Inc ),
        new( "inca", TokenType.Inca ),
        new( "incb", TokenType.Incb ),
        new( "jmp", TokenType.Jmp ),
        new( "jsr", TokenType.Jsr ),
        new( "lbcc", TokenType.Lbcc ),
        new( "lbcs", TokenType.Lbcs ),
        new( "lbeq", TokenType.Lbeq ),
        new( "lbge", TokenType.Lbge ),
        new( "lbgt", TokenType.Lbgt ),
        new( "lbhi", TokenType.Lbhi ),
        new( "lbhs", TokenType.Lbhs ),
        new( "lble", TokenType.Lble ),
        new( "lblo", TokenType.Lblo ),
        new( "lbls", TokenType.Lbls ),
        new( "lblt", TokenType.Lblt ),
        new( "lbmi", TokenType.Lbmi ),
        new( "lbne", TokenType.Lbne ),
        new( "lbpl", TokenType.Lbpl ),
        new( "lbra", TokenType.Lbra ),
        new( "lbrn", TokenType.Lbrn ),
        new( "lbsr", TokenType.Lbsr ),
        new( "lbvc", TokenType.Lbvc ),
        new( "lbvs", TokenType.Lbvs ),
        new( "lda", TokenType.Lda ),
        new( "ldb", TokenType.Ldb ),
        new( "ldd", TokenType.Ldd ),
        new( "lds", TokenType.Lds ),
        new( "ldu", TokenType.Ldu ),
        new( "ldx", TokenType.Ldx ),
        new( "ldy", TokenType.Ldy ),
        new( "leas", TokenType.Leas ),
        new( "leau", TokenType.Leau ),
        new( "leax", TokenType.Leax ),
        new( "leay", TokenType.Leay ),
        new( "lsl", TokenType.Lsl ),
        new( "lsla", TokenType.Lsla ),
        new( "lslb", TokenType.Lslb ),
        new( "lsr", TokenType.Lsr ),
        new( "lsra", TokenType.Lsra ),
        new( "lsrb", TokenType.Lsrb ),
        new( "mul", TokenType.Mul ),
        new( "neg", TokenType.Neg ),
        new( "nega", TokenType.Nega ),
        new( "negb", TokenType.Negb ),
        new( "nop", TokenType.Nop ),
        new( "ora", TokenType.Ora ),
        new( "oraa", TokenType.Oraa ),
        new( "orb", TokenType.Orb ),
        new( "orcc", TokenType.Orcc ),
        new( "pc", TokenType.Pc ),
        new( "pcr", TokenType.Pcr ),
        new( "pshs", TokenType.Pshs ),
        new( "pshu", TokenType.Pshu ),
        new( "puls", TokenType.Puls ),
        new( "pulu", TokenType.Pulu ),
        new( "rol", TokenType.Rol ),
        new( "rola", TokenType.Rola ),
        new( "rolb", TokenType.Rolb ),
        new( "ror", TokenType.Ror ),
        new( "rora", TokenType.Rora ),
        new( "rorb", TokenType.Rorb ),
        new( "rti", TokenType.Rti ),
        new( "rts", TokenType.Rts ),
        new( "s", TokenType.S ),
        new( "sbca", TokenType.Sbca ),
        new( "sbcb", TokenType.Sbcb ),
        new( "sex", TokenType.Sex ),
        new( "sta", TokenType.Sta ),
        new( "stb", TokenType.Stb ),
        new( "std", TokenType.Std ),
        new( "sts", TokenType.Sts ),
        new( "stu", TokenType.Stu ),
        new( "stx", TokenType.Stx ),
        new( "sty", TokenType.Sty ),
        new( "suba", TokenType.Suba ),
        new( "subb", TokenType.Subb ),
        new( "subd", TokenType.Subd ),
        new( "swi", TokenType.Swi ),
        new( "swi2", TokenType.Swi2 ),
        new( "swi3", TokenType.Swi3 ),
        new( "sync", TokenType.Sync ),
        new( "tfr", TokenType.Tfr ),
        new( "tst", TokenType.Tst ),
        new( "tsta", TokenType.Tsta ),
        new( "tstb", TokenType.Tstb ),
        new( "u", TokenType.U ),
        new( "x", TokenType.X ),
        new( "y", TokenType.Y ),
        new( ".tfradp", TokenType.Tfradp ),
        new( ".tfrbdp", TokenType.Tfrbdp )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_gb80Keywords =
    [
        new( "a", TokenType.A ),
        new( "af", TokenType.Af ),
        new( "adc", TokenType.Adc ),
        new( "add", TokenType.Add ),
        new( "and", TokenType.And ),
        new( "b", TokenType.B ),
        new( "bc", TokenType.Bc ),
        new( "bit", TokenType.Bit ),
        new( "c", TokenType.C ),
        new( "call", TokenType.Call ),
        new( "ccf",  TokenType.Ccf ),
        new( "cp", TokenType.Cp ),
        new( "cpl", TokenType.Cpl ),
        new( "d", TokenType.D ),
        new( "daa", TokenType.Daa ),
        new( "de", TokenType.De ),
        new( "dec", TokenType.Dec ),
        new( "di", TokenType.Di80 ),
        new( "e", TokenType.E ),
        new( "ei", TokenType.Ei ),
        new( "h", TokenType.H ),
        new( "halt", TokenType.Halt ),
        new( "hl", TokenType.Hl ),
        new( "i", TokenType.I ),
        new( "inc", TokenType.Inc ),
        new( "jp", TokenType.Jp ),
        new( "jr", TokenType.Jr ),
        new( "l", TokenType.L ),
        new( "ld", TokenType.Ld ),
        new( "ldd", TokenType.Ldd ),
        new( "ldi", TokenType.Ldi ),
        new( "m", TokenType.M ),
        new( "nc", TokenType.Nc ),
        new( "nop", TokenType.Nop ),
        new( "nz", TokenType.Nz ),
        new( "or", TokenType.Or ),
        new( "p",  TokenType.P ),
        new( "pe", TokenType.Pe ),
        new( "po", TokenType.Po ),
        new( "pop", TokenType.Pop ),
        new( "push", TokenType.Push ),
        new( "res", TokenType.Res ),
        new( "ret", TokenType.Ret ),
        new( "reti", TokenType.Reti ),
        new( "rl", TokenType.Rl ),
        new( "rla", TokenType.Rla ),
        new( "rlc", TokenType.Rlc ),
        new( "rlca", TokenType.Rlca ),
        new( "rr", TokenType.Rr ),
        new( "rra", TokenType.Rra ),
        new( "rrc", TokenType.Rrc ),
        new( "rrca", TokenType.Rrca ),
        new( "rst", TokenType.Rst ),
        new( "sbc", TokenType.Sbc ),
        new( "scf", TokenType.Scf ),
        new( "set", TokenType.Set ),
        new( "sla", TokenType.Sla ),
        new( "sp", TokenType.Sp ),
        new( "sra", TokenType.Sra ),
        new( "srl", TokenType.Srl ),
        new( "stop", TokenType.Stop ),
        new( "sub", TokenType.Sub ),
        new( "swap", TokenType.Swap ),
        new( "xor", TokenType.Xor ),
        new( "z", TokenType.Z )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_i8080Keywords =
    [
        new( "a", TokenType.A ),
        new( "aci", TokenType.Aci ),
        new( "adc", TokenType.Adc ),
        new( "add", TokenType.Add ),
        new( "adi", TokenType.Adi ),
        new( "ana", TokenType.Ana ),
        new( "ani", TokenType.Ani ),
        new( "b", TokenType.B ),
        new( "c", TokenType.C ),
        new( "call", TokenType.Call ),
        new( "cc", TokenType.Cc ),
        new( "cm", TokenType.Cm ),
        new( "cma", TokenType.Cma ),
        new( "cmc", TokenType.Cmc ),
        new( "cmp", TokenType.Cmp ),
        new( "cnc", TokenType.Cnc ),
        new( "cnz", TokenType.Cnz ),
        new( "cp", TokenType.Cp ),
        new( "cpe", TokenType.Cpe ),
        new( "cpi", TokenType.Cpi ),
        new( "cpo", TokenType.Cpo ),
        new( "cz", TokenType.Cz ),
        new( "d", TokenType.D ),
        new( "daa", TokenType.Daa ),
        new( "dad", TokenType.Dad ),
        new( "dcr", TokenType.Dcr ),
        new( "dcx", TokenType.Dcx ),
        new( "di", TokenType.Di80 ),
        new( "e", TokenType.E ),
        new( "ei", TokenType.Ei ),
        new( "h", TokenType.H ),
        new( "hlt", TokenType.Hlt ),
        new( "in", TokenType.In ),
        new( "inr", TokenType.Inr ),
        new( "inx", TokenType.Inx ),
        new( "jc", TokenType.Jc ),
        new( "jm", TokenType.Jm ),
        new( "jmp", TokenType.Jmp ),
        new( "jnc", TokenType.Jnc ),
        new( "jnz", TokenType.Jnz ),
        new( "jp", TokenType.Jp ),
        new( "jpe", TokenType.Jpe ),
        new( "jpo", TokenType.Jpo ),
        new( "jz", TokenType.Jz ),
        new( "l", TokenType.L ),
        new( "lda", TokenType.Lda ),
        new( "ldax", TokenType.Ldax ),
        new( "lhld", TokenType.Lhld ),
        new( "lxi", TokenType.Lxi ),
        new( "m", TokenType.M ),
        new( "mov", TokenType.Mov ),
        new( "mvi", TokenType.Mvi ),
        new( "nop", TokenType.Nop ),
        new( "ora", TokenType.Ora ),
        new( "ori", TokenType.Ori ),
        new( "out", TokenType.Out ),
        new( "pchl", TokenType.Pchl ),
        new( "pop", TokenType.Pop ),
        new( "psw", TokenType.Psw ),
        new( "push", TokenType.Push ),
        new( "ral", TokenType.Ral ),
        new( "rar", TokenType.Rar ),
        new( "rc", TokenType.Rc ),
        new( "ret", TokenType.Ret ),
        new( "rlc", TokenType.Rlc ),
        new( "rm", TokenType.Rm ),
        new( "rnc", TokenType.Rnc ),
        new( "rnz", TokenType.Rnz ),
        new( "rp", TokenType.Rp ),
        new( "rpe", TokenType.Rpe ),
        new( "rpo", TokenType.Rpo ),
        new( "rrc", TokenType.Rrc ),
        new( "rst", TokenType.Rst ),
        new( "rz", TokenType.Rz ),
        new( "sbb", TokenType.Sbb ),
        new( "sbi", TokenType.Sbi ),
        new( "shld", TokenType.Shld ),
        new( "sp", TokenType.Sp ),
        new( "sphl", TokenType.Sphl ),
        new( "sta", TokenType.Sta ),
        new( "stax", TokenType.Stax ),
        new( "stc", TokenType.Stc ),
        new( "sub", TokenType.Sub ),
        new( "sui", TokenType.Sui ),
        new( "xchg", TokenType.Xchg ),
        new( "xra", TokenType.Xra ),
        new( "xri", TokenType.Xri ),
        new( "xthl", TokenType.Xthl )
    ];

    private static readonly KeyValuePair<string, TokenType>[] s_z80Keywords =
    [
        new( "a", TokenType.A ),
        new( "af", TokenType.Af ),
        new( "adc", TokenType.Adc ),
        new( "add", TokenType.Add ),
        new( "and", TokenType.And ),
        new( "b", TokenType.B ),
        new( "bc", TokenType.Bc ),
        new( "bit", TokenType.Bit ),
        new( "c", TokenType.C ),
        new( "call", TokenType.Call ),
        new( "ccf",  TokenType.Ccf ),
        new( "cp", TokenType.Cp ),
        new( "cpd", TokenType.Cpd ),
        new( "cpdr", TokenType.Cpdr ),
        new( "cpi", TokenType.Cpi ),
        new( "cpir", TokenType.Cpir ),
        new( "cpl", TokenType.Cpl ),
        new( "d", TokenType.D ),
        new( "daa", TokenType.Daa ),
        new( "de", TokenType.De ),
        new( "dec", TokenType.Dec ),
        new( "di", TokenType.Di80 ),
        new( "djnz", TokenType.Djnz ),
        new( "e", TokenType.E ),
        new( "ei", TokenType.Ei ),
        new( "ex", TokenType.Ex ),
        new( "exx", TokenType.Exx ),
        new( "h", TokenType.H ),
        new( "halt", TokenType.Halt ),
        new( "hl", TokenType.Hl ),
        new( "i", TokenType.I ),
        new( "im", TokenType.Im ),
        new( "in", TokenType.In ),
        new( "inc", TokenType.Inc ),
        new( "ind", TokenType.Ind ),
        new( "indr", TokenType.Indr ),
        new( "ini", TokenType.Ini ),
        new( "inir", TokenType.Inir ),
        new( "ix", TokenType.Ix ),
        new( "ixh", TokenType.Ixh ),
        new( "ixl", TokenType.Ixl ),
        new( "iy", TokenType.Iy ),
        new( "iyh", TokenType.Iyh ),
        new( "iyl", TokenType.Iyl ),
        new( "jp", TokenType.Jp ),
        new( "jr", TokenType.Jr ),
        new( "l", TokenType.L ),
        new( "ld", TokenType.Ld ),
        new( "ldd", TokenType.Ldd ),
        new( "lddr", TokenType.Lddr ),
        new( "ldi", TokenType.Ldi ),
        new( "ldir", TokenType.Ldir ),
        new( "m", TokenType.M ),
        new( "nc", TokenType.Nc ),
        new( "neg", TokenType.Neg ),
        new( "nop", TokenType.Nop ),
        new( "nz", TokenType.Nz ),
        new( "or", TokenType.Or ),
        new( "otdr", TokenType.Otdr ),
        new( "otir", TokenType.Otir ),
        new( "out", TokenType.Out ),
        new( "outd", TokenType.Outd ),
        new( "outi", TokenType.Outi ),
        new( "p",  TokenType.P ),
        new( "pe", TokenType.Pe ),
        new( "po", TokenType.Po ),
        new( "pop", TokenType.Pop ),
        new( "push", TokenType.Push ),
        new( "r", TokenType.R ),
        new( "res", TokenType.Res ),
        new( "ret", TokenType.Ret ),
        new( "reti", TokenType.Reti ),
        new( "retn", TokenType.Retn ),
        new( "rl", TokenType.Rl ),
        new( "rla", TokenType.Rla ),
        new( "rlc", TokenType.Rlc ),
        new( "rlca", TokenType.Rlca ),
        new( "rld", TokenType.Rld ),
        new( "rr", TokenType.Rr ),
        new( "rra", TokenType.Rra ),
        new( "rrc", TokenType.Rrc ),
        new( "rrca", TokenType.Rrca ),
        new( "rrd", TokenType.Rrd ),
        new( "rst", TokenType.Rst ),
        new( "sbc", TokenType.Sbc ),
        new( "scf", TokenType.Scf ),
        new( "set", TokenType.Set ),
        new( "sla", TokenType.Sla ),
        new( "sll", TokenType.Sll ),
        new( "sp", TokenType.Sp ),
        new( "sra", TokenType.Sra ),
        new( "srl", TokenType.Srl ),
        new( "sub", TokenType.Sub ),
        new( "xor", TokenType.Xor ),
        new( "z", TokenType.Z )
    ];

    
    private static readonly KeyValuePair<string, TokenType>[] s_i86Keywords =
    [
        new( "aaa", TokenType.Aaa ),
        new( "aad", TokenType.Aad ),
        new( "aam", TokenType.Aam ),
        new( "aas", TokenType.Aas ),
        new( "adc", TokenType.Adc ),
        new( "add", TokenType.Add ),
        new( "ah", TokenType.Ah ),
        new( "al", TokenType.Al ),
        new( "and", TokenType.And ),
        new( "ax", TokenType.Ax ),
        new( "bh", TokenType.Bh ),
        new( "bl", TokenType.Bl ),
        new( "bp", TokenType.Bp ),
        new( "bx", TokenType.Bx ),
        new( "BYTE", TokenType.Byte ),
        new( "call", TokenType.Call ),
        new( "cbw", TokenType.Cbw ),
        new( "ch", TokenType.Ch ),
        new( "cl", TokenType.Cl ),
        new( "clc", TokenType.Clc ),
        new( "cli", TokenType.Cli ),
        new( "cld", TokenType.Cld ),
        new( "cmc", TokenType.Cmc ),
        new( "cmp", TokenType.Cmp ),
        new( "cmps", TokenType.Cmps ),
        new( "cmpsb", TokenType.Cmpsb ),
        new( "cmpsw", TokenType.Cmpsw ),
        new( "cs", TokenType.Cs ),
        new( "cwd", TokenType.Cwd ),
        new( "cx", TokenType.Cx ),
        new( "daa", TokenType.Daa ),
        new( "das", TokenType.Das ),
        new( "dec", TokenType.Dec ),
        new( "dh", TokenType.Dh ),
        new( "di", TokenType.Di ),
        new( "div", TokenType.Div ),
        new( "dl", TokenType.Dl ),
        new( "ds", TokenType.Ds ),
        new( "DWORD", TokenType.Dword ),
        new( "dx", TokenType.Dx ),
        new( "es", TokenType.Es ),
        new( "esc", TokenType.Esc ),
        new( "f2xm1", TokenType.F2Xm1 ),
        new( "fadd", TokenType.Fadd ),
        new( "faddp", TokenType.Faddp ),
        new( "fabs", TokenType.Fabs ),
        new( "fbld", TokenType.Fbld ),
        new( "fbstp", TokenType.Fbstp ),
        new( "fchs", TokenType.Fchs ),
        new( "fclex", TokenType.Fclex ),
        new( "fcom", TokenType.Fcom ),
        new( "fcomp", TokenType.Fcomp ),
        new( "fcompp", TokenType.Fcompp ),
        new( "fcos", TokenType.Fcos ),
        new( "fdecstp", TokenType.Fdecstp ),
        new( "fdisi", TokenType.Fdisi ),
        new( "fdiv", TokenType.Fdiv ),
        new( "fdivp", TokenType.Fdivp ),
        new( "fdivr", TokenType.Fdivr ),
        new( "fdivrp", TokenType.Fdivrp ),
        new( "ficom", TokenType.Ficom ),
        new( "ficomp", TokenType.Ficomp ),
        new( "fidivr", TokenType.Fidivr ),
        new( "fidivrp", TokenType.Fdivrp ),
        new( "feni", TokenType.Feni ),
        new( "ffree", TokenType.Ffree ),
        new( "fiadd", TokenType.Fiadd ),
        new( "fidiv", TokenType.Fidiv ),
        new( "fild", TokenType.Fild ),
        new( "fimul", TokenType.Fimul ),
        new( "fincstp", TokenType.Fincstp ),
        new( "finit", TokenType.Finit ),
        new( "fist", TokenType.Fist ),
        new( "fistp", TokenType.Fistp ),
        new( "fisub", TokenType.Fisub ),
        new( "fisubr", TokenType.Fisubr ),
        new( "fld", TokenType.Fld ),
        new( "fld1", TokenType.Fld1 ),
        new( "fldcw", TokenType.Fldcw ),
        new( "fldenv", TokenType.Fldenv ),
        new( "fldl2e", TokenType.Fldl2E ),
        new( "fldl2t", TokenType.Fldl2T ),
        new( "fldlg2", TokenType.Fldlg2 ),
        new( "fldln2", TokenType.Fldln2 ),
        new( "fldpi", TokenType.Fldpi ),
        new( "fldz", TokenType.Fldz ),
        new( "fmul", TokenType.Fmul ),
        new( "fmulp", TokenType.Fmulp ),
        new( "fnclex", TokenType.Fnclex ),
        new( "fndisi", TokenType.Fndisi ),
        new( "fneni", TokenType.Fneni ),
        new( "fninit", TokenType.Fninit ),
        new( "fnop", TokenType.Fnop ),
        new( "fnsave", TokenType.Fnsave ),
        new( "fnstcw", TokenType.Fnstcw ),
        new( "fnstenv", TokenType.Fnstenv ),
        new( "fnstsw", TokenType.Fnstsw ),
        new( "fpatan", TokenType.Fpatan ),
        new( "fprem", TokenType.Fprem ),
        new( "frndint", TokenType.Frndint ),
        new( "fptan", TokenType.Fptan ),
        new( "frstor", TokenType.Frstor ),
        new( "fsave", TokenType.Fsave ),
        new( "fscale", TokenType.Fscale ),
        new( "fsin", TokenType.Fsin ),
        new( "fsqrt", TokenType.Fsqrt ),
        new( "fst", TokenType.Fst ),
        new( "fstcw", TokenType.Fstcw ),
        new( "fstenv", TokenType.Fstenv ),
        new( "fstp", TokenType.Fstp ),
        new( "fstsw", TokenType.Fstsw ),
        new( "fsub", TokenType.Fsub ),
        new( "fsubp", TokenType.Fsubp ),
        new( "fsubr", TokenType.Fsubr ),
        new( "fsubrp", TokenType.Fsubrp ),
        new( "ftst", TokenType.Ftst ),
        new( "fxch", TokenType.Fxch ),
        new( "fxtract", TokenType.Fxtract ),
        new( "fwait", TokenType.Fwait ),
        new( "fxam", TokenType.Fxam ),
        new( "fyl2x", TokenType.Fyl2X ),
        new( "fyl2xp1", TokenType.Fyl2Xp1 ),
        new( "hlt", TokenType.Hlt ),
        new( "idiv", TokenType.Idiv ),
        new( "imul", TokenType.Imul ),
        new( "in", TokenType.In ),
        new( "inc", TokenType.Inc ),
        new( "ins", TokenType.Ins ),
        new( "insb", TokenType.Insb ),
        new( "insw", TokenType.Insw ),
        new( "int", TokenType.Int ),
        new( "int3", TokenType.Int3 ),
        new( "into", TokenType.Into ),
        new( "iret", TokenType.Iret ),
        new( "ja", TokenType.Ja ),
        new( "jae", TokenType.Jae ),
        new( "jb", TokenType.Jb ),
        new( "jbe", TokenType.Jbe ),
        new( "jc", TokenType.Jc ),
        new( "jcxz", TokenType.Jcxz ),
        new( "je", TokenType.Je ),
        new( "jg", TokenType.Jg ),
        new( "jge", TokenType.Jge ),
        new( "jl", TokenType.Jl ),
        new( "jle", TokenType.Jle ),
        new( "jmp", TokenType.Jmp ),
        new( "jmpf", TokenType.Jmpf ),
        new( "jmps", TokenType.Jmps ),
        new( "jna", TokenType.Jna ),
        new( "jnae", TokenType.Jnae ),
        new( "jnb", TokenType.Jnb ),
        new( "jnbe", TokenType.Jnbe ),
        new( "jnc", TokenType.Jnc ),
        new( "jne", TokenType.Jne ),
        new( "jng", TokenType.Jng ),
        new( "jnge", TokenType.Jnge ),
        new( "jnl", TokenType.Jnl ),
        new( "jnle", TokenType.Jnle ),
        new( "jno", TokenType.Jno ),
        new( "jnp", TokenType.Jnp ),
        new( "jns", TokenType.Jns ),
        new( "jnz", TokenType.Jnz ),
        new( "jo", TokenType.Jo ),
        new( "jp", TokenType.Jp ),
        new( "jpe", TokenType.Jpe ),
        new( "jpo", TokenType.Jpo ),
        new( "js", TokenType.Js ),
        new( "jz", TokenType.Jz ),
        new( "lahf", TokenType.Lahf ),
        new( "lds", TokenType.Lds ),
        new( "lea", TokenType.Lea ),
        new( "leave", TokenType.Leave ),
        new( "les", TokenType.Les ),
        new( "lock", TokenType.Lock ),
        new( "lods", TokenType.Lods ),
        new( "lodsb", TokenType.Lodsb ),
        new( "lodsw", TokenType.Lodsw ),
        new( "loop", TokenType.Loop ),
        new( "loope", TokenType.Loope ),
        new( "loopne", TokenType.Loopne ),
        new( "loopnz", TokenType.Loopnz ),
        new( "loopz", TokenType.Loopz ),
        new( "mov", TokenType.Mov ),
        new( "movs", TokenType.Movs ),
        new( "movsb", TokenType.Movsb ),
        new( "movsw", TokenType.Movsw ),
        new( "mul", TokenType.Mul ),
        new( "neg", TokenType.Neg ),
        new( "nop", TokenType.Nop ),
        new( "not", TokenType.Not ),
        new( "or", TokenType.Or ),
        new( "out", TokenType.Out ),
        new( "outs", TokenType.Outs ),
        new( "Outsb", TokenType.Outsb),
        new( "Outsw", TokenType.Outsw ),
        new( "pop", TokenType.Pop ),
        new( "popa", TokenType.Popa ),
        new( "popf", TokenType.Popf ),
        new( "PTR", TokenType.Ptr ),
        new( "push", TokenType.Push ),
        new( "pusha", TokenType.Pusha ),
        new( "pushf", TokenType.Pushf ),
        new( "QWORD", TokenType.Qword ),
        new( "rcl", TokenType.Rcl ),
        new( "rcr", TokenType.Rcr ),
        new( "rep", TokenType.Rep ),
        new( "repe", TokenType.Repe ),
        new( "repne", TokenType.Repne ),
        new( "repnz", TokenType.Repnz ),
        new( "repz", TokenType.Repz ),
        new( "ret", TokenType.Ret ),
        new( "retf", TokenType.Retf ),
        new( "rol", TokenType.Rol ),
        new( "ror", TokenType.Ror ),
        new( "sahf", TokenType.Sahf ),
        new( "sal", TokenType.Sal ),
        new( "salc", TokenType.Salc ),
        new( "sar", TokenType.Sar ),
        new( "sbb", TokenType.Sbb ),
        new( "scas", TokenType.Scas ),
        new( "scasb", TokenType.Scasb ),
        new( "scasw", TokenType.Scasw ),
        new( "shl", TokenType.ShlMnem ),
        new( "shr", TokenType.ShrMnem ),
        new( "si", TokenType.Si ),
        new( "sp", TokenType.Sp ),
        new( "ss", TokenType.Ss ),
        new( "st", TokenType.St ),
        new( "st0", TokenType.St0Reg ),
        new( "st1", TokenType.St1Reg ),
        new( "st2", TokenType.St2Reg ),
        new( "st3", TokenType.St3Reg ),
        new( "st4", TokenType.St4Reg ),
        new( "st5", TokenType.St5Reg ),
        new( "st6", TokenType.St6Reg ),
        new( "st7", TokenType.St7Reg ),
        new( "stc", TokenType.Stc ),
        new( "std", TokenType.Std ),
        new( "sti", TokenType.Sti ),
        new( "stos", TokenType.Stos ),
        new( "stosb", TokenType.Stosb ),
        new( "stosw", TokenType.Stosw ),
        new( "sub", TokenType.Sub ),
        new( "TBYTE", TokenType.Tbyte ),
        new( "test", TokenType.Test ),
        new( "wait", TokenType.Wait ),
        new( "WORD", TokenType.Word ),
        new( "xchg", TokenType.Xchg ),
        new( "xlat", TokenType.Xlat ),
        new( "xlatb", TokenType.Xlatb ),
        new( "xor", TokenType.Xor )
    ];
    
    public static readonly FrozenDictionary<string, TokenType> CaseInsensitiveLegacyDirectives
        = s_legacyDirective.Concat(s_directives)
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    public static readonly FrozenDictionary<string, TokenType> CaseSensitiveLegacyDirectives
        = s_legacyDirective.Concat(s_directives)
            .ToFrozenDictionary(StringComparer.Ordinal);

    public static readonly FrozenDictionary<string, TokenType> CaseSensitiveDirectives
        = s_directives.ToFrozenDictionary(StringComparer.Ordinal);
    
    public static readonly FrozenDictionary<string, TokenType> CaseInsensitiveDirectives
        = s_directives.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TokenType> s_m6502KeywordsInsensitive =
        s_m6502Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TokenType> s_m6502BraCaseInsensitive =
        s_m6502Bra.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TokenType> s_m6502PseudoBraCaseInsensitive =
        s_m6502PseudoBra.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m6502KeywordsSensitive =
        s_m6502Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m6502BraCaseSensitive =
        s_m6502Bra.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m6502PseudoBraCaseSensitive =
        s_m6502PseudoBra.ToFrozenDictionary(StringComparer.Ordinal);
    
    private static readonly FrozenDictionary<string, TokenType> s_c64DtvKeywordsCaseInsensitive
        = s_c64dtvKeywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_c64DtvKeywordsCaseSensitive
        = s_c64dtvKeywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m6502IKeywordsCaseInsensitive 
        = s_m6502IKeywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m6502IKeywordsCaseSensitive 
        = s_m6502IKeywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m65816KeywordsCaseInsensitive 
        = s_m65816Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m65816KeywordsCaseSensitive 
        = s_m65816Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m65C02KeywordsCaseInsensitive 
        = s_m65C02Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m65C02KeywordsCaseSensitive 
        = s_m65C02Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_r65C02KeywordsCaseInsensitive 
        = s_r65C02Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_r65C02KeywordsCaseSensitive 
        = s_r65C02Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m65Ce02KeywordsCaseInsensitive 
        = s_m65Ce02Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m65Ce02KeywordsCaseSensitive 
        = s_m65Ce02Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_huc6280KeywordsCaseInsensitive 
        = s_huc6280Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_huc6280KeywordsCaseSensitive 
        = s_huc6280Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m65KeywordsCaseInsensitive 
        = s_m65Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TokenType> s_m65KeywordsCaseSensitive
        = s_m65Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m6800KeywordsCaseInsensitive 
        = s_m6800Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m6800KeywordsCaseSensitive 
        = s_m6800Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_m6809KeywordsCaseInsensitive 
        = s_m6809Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_m6809KeywordsCaseSensitive 
        = s_m6809Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_gb80KeywordsCaseInsensitive 
        = s_gb80Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_gb80KeywordsCaseSensitive 
        = s_gb80Keywords.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TokenType> s_i8080KeywordsCaseInsensitive 
        = s_i8080Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TokenType> s_i8080KeywordsCaseSensitive 
        = s_i8080Keywords.ToFrozenDictionary(StringComparer.Ordinal);
    
    private static readonly FrozenDictionary<string, TokenType> s_z80KeywordsCaseInsensitive 
        = s_z80Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_z80KeywordsCaseSensitive 
        = s_z80Keywords.ToFrozenDictionary(StringComparer.Ordinal);
    
    private static readonly FrozenDictionary<string, TokenType> s_i86KeywordsCaseInsensitive
        = s_i86Keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static readonly FrozenDictionary<string, TokenType> s_i86KeywordsCaseSensitive
        = s_i86Keywords.ToFrozenDictionary(StringComparer.Ordinal);
}