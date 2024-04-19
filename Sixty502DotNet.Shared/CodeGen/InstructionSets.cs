//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A collection of various instruction sets, which are key-value pairs of
/// keywords to their lexical identifiers.
/// </summary>
public static class InstructionSets
{
    /// <summary>
    /// The C64dtv instructions.
    /// </summary>
    public static IDictionary<string, int> C64dtv2 =>
    new Dictionary<string, int>
    {
        { "sac", SyntaxParser.SAC },
        { "sir", SyntaxParser.SIR }
    };

    /// <summary>
    /// The Intel i8080 instructions.
    /// </summary>
    public static IDictionary<string, int> I8080 =>
    new Dictionary<string, int>
    {
        { "a",      SyntaxParser.A },
        { "aci",    SyntaxParser.ACI },
        { "adc",    SyntaxParser.ADC },
        { "add",    SyntaxParser.ADD },
        { "adi",    SyntaxParser.ADI },
        { "ana",    SyntaxParser.ANA },
        { "ani",    SyntaxParser.ANI },
        { "b",      SyntaxParser.B },
        { "call",   SyntaxParser.CALL },
        { "c",      SyntaxParser.C },
        { "cc",     SyntaxParser.CC },
        { "cm",     SyntaxParser.CM },
        { "cma",    SyntaxParser.CMA },
        { "cmc",    SyntaxParser.CMC },
        { "cmp",    SyntaxParser.CMP },
        { "cnc",    SyntaxParser.CNC },
        { "cnz",    SyntaxParser.CNZ },
        { "cp",     SyntaxParser.CP },
        { "cpe",    SyntaxParser.CPE },
        { "cpi",    SyntaxParser.CPI },
        { "cpo",    SyntaxParser.CPO },
        { "cz",     SyntaxParser.CZ },
        { "d",      SyntaxParser.D },
        { "daa",    SyntaxParser.DAA },
        { "dad",    SyntaxParser.DAD },
        { "dcr",    SyntaxParser.DCR },
        { "dcx",    SyntaxParser.DCX },
        { "di",     SyntaxParser.DI },
        { "e",      SyntaxParser.E },
        { "ei",     SyntaxParser.EI },
        { "h",      SyntaxParser.H },
        { "hlt",    SyntaxParser.HLT },
        { "in",     SyntaxParser.IN },
        { "inr",    SyntaxParser.INR },
        { "inx",    SyntaxParser.INX },
        { "jc",     SyntaxParser.JC },
        { "jm",     SyntaxParser.JM },
        { "jmp",    SyntaxParser.JMP },
        { "jnc",    SyntaxParser.JNC },
        { "jnz",    SyntaxParser.JNZ },
        { "jp",     SyntaxParser.JP },
        { "jpe",    SyntaxParser.JPE },
        { "jpo",    SyntaxParser.JPO },
        { "jz",     SyntaxParser.JZ },
        { "l",      SyntaxParser.L },
        { "lda",    SyntaxParser.LDA },
        { "ldax",   SyntaxParser.LDAX },
        { "lhld",   SyntaxParser.LHLD },
        { "lxi",    SyntaxParser.LXI },
        { "m",      SyntaxParser.M },
        { "mov",    SyntaxParser.MOV },
        { "mvi",    SyntaxParser.MVI },
        { "nop",    SyntaxParser.NOP },
        { "ora",    SyntaxParser.ORA },
        { "ori",    SyntaxParser.ORI },
        { "out",    SyntaxParser.OUT },
        { "pchl",   SyntaxParser.PCHL },
        { "pop",    SyntaxParser.POP },
        { "psw",    SyntaxParser.PSW },
        { "push",   SyntaxParser.PUSH },
        { "ral",    SyntaxParser.RAL },
        { "rar",    SyntaxParser.RAR },
        { "rc",     SyntaxParser.RC },
        { "ret",    SyntaxParser.RET },
        { "rlc",    SyntaxParser.RLC },
        { "rm",     SyntaxParser.RM },
        { "rnc",    SyntaxParser.RNC },
        { "rnz",    SyntaxParser.RNZ },
        { "rp",     SyntaxParser.RP },
        { "rpe",    SyntaxParser.RPE },
        { "rpo",    SyntaxParser.RPO },
        { "rrc",    SyntaxParser.RRC },
        { "rz",     SyntaxParser.RZ },
        { "sbb",    SyntaxParser.SBB },
        { "sbi",    SyntaxParser.SBI },
        { "shld",   SyntaxParser.SHLD },
        { "sphl",   SyntaxParser.SPHL },
        { "sta",    SyntaxParser.STA },
        { "stax",   SyntaxParser.STAX },
        { "stc",    SyntaxParser.STC },
        { "sub",    SyntaxParser.SUB },
        { "sui",    SyntaxParser.SUI },
        { "xchg",   SyntaxParser.XCHG },
        { "xra",    SyntaxParser.XRA },
        { "xri",    SyntaxParser.XRI },
        { "xthl",   SyntaxParser.XTHL }
    };

    /// <summary>
    /// The GameBoy instructions.
    /// </summary>
    public static IDictionary<string, int> GB80 =>
    new Dictionary<string, int>
    {
        { "a",      SyntaxParser.A },
        { "adc",    SyntaxParser.ADC },
        { "add",    SyntaxParser.ADD },
        { "af",     SyntaxParser.AF },   
        { "and",    SyntaxParser.AND },
        { "b",      SyntaxParser.B },
        { "bc",     SyntaxParser.BC },
        { "bit",    SyntaxParser.BITZ },
        { "c",      SyntaxParser.C },
        { "call",   SyntaxParser.CALL },
        { "ccf",    SyntaxParser.CCF },
        { "cp",     SyntaxParser.CP },
        { "cpl",    SyntaxParser.CPL },
        { "d",      SyntaxParser.D },
        { "daa",    SyntaxParser.DAA },
        { "de",     SyntaxParser.DE },
        { "dec",    SyntaxParser.DEC },
        { "di",     SyntaxParser.DI },
        { "e",      SyntaxParser.E },
        { "ei",     SyntaxParser.EI },
        { "h",      SyntaxParser.H },
        { "halt",   SyntaxParser.HALT },
        { "hl",     SyntaxParser.HL },
        { "inc",    SyntaxParser.INC },
        { "jp",     SyntaxParser.JP },
        { "jr",     SyntaxParser.JR },
        { "l",      SyntaxParser.L },
        { "ld",     SyntaxParser.LD },
        { "ldd",    SyntaxParser.LDD },
        { "ldi",    SyntaxParser.LDI },
        { "nc",     SyntaxParser.NC },
        { "nop",    SyntaxParser.NOP },
        { "nz",     SyntaxParser.NZ },
        { "or",     SyntaxParser.OR },
        { "pop",    SyntaxParser.POP },
        { "push",   SyntaxParser.PUSH },
        { "res",    SyntaxParser.RES },
        { "ret",    SyntaxParser.RET },
        { "reti",   SyntaxParser.RETI },
        { "rl",     SyntaxParser.RL },
        { "rla",    SyntaxParser.RLA },
        { "rlc",    SyntaxParser.RLC },
        { "rlca",   SyntaxParser.RLCA },
        { "rr",     SyntaxParser.RR },
        { "rra",    SyntaxParser.RRA },
        { "rrc",    SyntaxParser.RRC },
        { "rrca",   SyntaxParser.RRCA },
        { "rst",    SyntaxParser.RST },
        { "sbc",    SyntaxParser.SBC },
        { "scf",    SyntaxParser.SCF },
        { "set",    SyntaxParser.SET },
        { "sla",    SyntaxParser.SLA },
        { "sp",     SyntaxParser.SP },
        { "sra",    SyntaxParser.SRA },
        { "srl",    SyntaxParser.SRL },
        { "stop",   SyntaxParser.STOP },
        { "sub",    SyntaxParser.SUB },
        { "swap",   SyntaxParser.SWAP }, 
        { "xor",    SyntaxParser.XOR },
        { "z",      SyntaxParser.Z }
    };


    /// <summary>
    /// The M45GS02 instructions.
    /// </summary>
    public static IDictionary<string, int> M45GS02 =>
    new Dictionary<string, int>
    {
        { "map", SyntaxParser.MAP },
        { "eom", SyntaxParser.EOM }
    };

    /// <summary>
    /// The MOS 6502 instructions.
    /// </summary>
    public static IDictionary<string, int> M6502 =>
    new Dictionary<string, int>
    {
        { "a"  , SyntaxParser.A   },
        { "adc", SyntaxParser.ADC },
        { "and", SyntaxParser.AND },
        { "asl", SyntaxParser.ASL },
        { "bcc", SyntaxParser.BCC },
        { "bcs", SyntaxParser.BCS },
        { "beq", SyntaxParser.BEQ },
        { "bit", SyntaxParser.BIT },
        { "bmi", SyntaxParser.BMI },
        { "bne", SyntaxParser.BNE },
        { "bpl", SyntaxParser.BPL },
        { "brk", SyntaxParser.BRK },
        { "bvc", SyntaxParser.BVC },
        { "bvs", SyntaxParser.BVS },
        { "clc", SyntaxParser.CLC },
        { "cld", SyntaxParser.CLD },
        { "cli", SyntaxParser.CLI },
        { "clv", SyntaxParser.CLV },
        { "cmp", SyntaxParser.CMP },
        { "cpx", SyntaxParser.CPX },
        { "cpy", SyntaxParser.CPY },
        { "dec", SyntaxParser.DEC },
        { "dex", SyntaxParser.DEX },
        { "dey", SyntaxParser.DEY },
        { "eor", SyntaxParser.EOR },
        { "inc", SyntaxParser.INC },
        { "inx", SyntaxParser.INX },
        { "iny", SyntaxParser.INY },
        { "jcc", SyntaxParser.JCC },
        { "jcs", SyntaxParser.JCS },
        { "jeq", SyntaxParser.JEQ },
        { "jmi", SyntaxParser.JMI },
        { "jne", SyntaxParser.JNE },
        { "jpl", SyntaxParser.JPL },
        { "jmp", SyntaxParser.JMP },
        { "jsr", SyntaxParser.JSR },
        { "jvc", SyntaxParser.JVC },
        { "jvs", SyntaxParser.JVS },
        { "lda", SyntaxParser.LDA },
        { "ldx", SyntaxParser.LDX },
        { "ldy", SyntaxParser.LDY },
        { "lsr", SyntaxParser.LSR },
        { "nop", SyntaxParser.NOP },
        { "ora", SyntaxParser.ORA },
        { "pha", SyntaxParser.PHA },
        { "php", SyntaxParser.PHP },
        { "pla", SyntaxParser.PLA },
        { "plp", SyntaxParser.PLP },
        { "rol", SyntaxParser.ROL },
        { "ror", SyntaxParser.ROR },
        { "rti", SyntaxParser.RTI },
        { "rts", SyntaxParser.RTS },
        { "sbc", SyntaxParser.SBC },
        { "sec", SyntaxParser.SEC },
        { "sed", SyntaxParser.SED },
        { "sei", SyntaxParser.SEI },
        { "sta", SyntaxParser.STA },
        { "stx", SyntaxParser.STX },
        { "sty", SyntaxParser.STY },
        { "tax", SyntaxParser.TAX },
        { "tay", SyntaxParser.TAY },
        { "tsx", SyntaxParser.TSX },
        { "txa", SyntaxParser.TXA },
        { "txs", SyntaxParser.TXS },
        { "tya", SyntaxParser.TYA },
        { "x"  , SyntaxParser.X   },
        { "y"  , SyntaxParser.Y   }
    };

    /// <summary>
    /// The MOS 6502i (undocumented) instructions.
    /// </summary>
    public static IDictionary<string, int> M6502i =>
    new Dictionary<string, int>
    {
        { "anc", SyntaxParser.ANC },
        { "ane", SyntaxParser.ANE },
        { "arr", SyntaxParser.ARR },
        { "asr", SyntaxParser.ASR },
        { "dcp", SyntaxParser.DCP },
        { "dop", SyntaxParser.DOP },
        { "isb", SyntaxParser.ISB },
        { "jam", SyntaxParser.JAM },
        { "las", SyntaxParser.LAS },
        { "lax", SyntaxParser.LAX },
        { "rla", SyntaxParser.RLA },
        { "rra", SyntaxParser.RRA },
        { "sax", SyntaxParser.SAX },
        { "sha", SyntaxParser.SHA },
        { "shx", SyntaxParser.SHX },
        { "shy", SyntaxParser.SHY },
        { "slo", SyntaxParser.SLO },
        { "sre", SyntaxParser.SRE },
        { "stp", SyntaxParser.STP },
        { "tas", SyntaxParser.TAS },
        { "top", SyntaxParser.TOP }
    };

    /// <summary>
    /// The WDC 65816 instructions.
    /// </summary>
    public static IDictionary<string, int> M65816 =>
    new Dictionary<string, int>
    {
        { "brl", SyntaxParser.BRL },
        { "cop", SyntaxParser.COP },
        { "jml", SyntaxParser.JML },
        { "jsl", SyntaxParser.JSL },
        { "mvn", SyntaxParser.MVN },
        { "mvp", SyntaxParser.MVP },
        { "pea", SyntaxParser.PEA },
        { "pei", SyntaxParser.PEI },
        { "per", SyntaxParser.PER },
        { "phb", SyntaxParser.PHB },
        { "phd", SyntaxParser.PHD },
        { "phk", SyntaxParser.PHK },
        { "plb", SyntaxParser.PLB },
        { "pld", SyntaxParser.PLD },
        { "rep", SyntaxParser.REP },
        { "rtl", SyntaxParser.RTL },
        { "s",   SyntaxParser.S   },
        { "sep", SyntaxParser.SEP },
        { "tcd", SyntaxParser.TCD },
        { "tcs", SyntaxParser.TCS },
        { "tdc", SyntaxParser.TDC },
        { "tsc", SyntaxParser.TSC },
        { "txy", SyntaxParser.TXY },
        { "tyx", SyntaxParser.TYX },
        { "wdm", SyntaxParser.WDM },
        { "xba", SyntaxParser.XBA },
        { "xce", SyntaxParser.XCE },
        { "z",   SyntaxParser.Z   }
    };

    /// <summary>
    /// The MOS 65C02 instructions.
    /// </summary>
    public static IDictionary<string, int> M65C02 =>
    new Dictionary<string, int>
    {
        { "bra", SyntaxParser.BRA },
        { "phx", SyntaxParser.PHX },
        { "phy", SyntaxParser.PHY },
        { "plx", SyntaxParser.PLX },
        { "ply", SyntaxParser.PLY },
        { "stz", SyntaxParser.STZ },
        { "trb", SyntaxParser.TRB },
        { "tsb", SyntaxParser.TSB }
    };

    /// <summary>
    /// The 65CE02 instructions.
    /// </summary>
    public static IDictionary<string, int> M65CE02 =>
    new Dictionary<string, int>
    {
        { "asw", SyntaxParser.ASW },
        { "bge", SyntaxParser.BGE },
        { "blt", SyntaxParser.BLT },
        { "bsr", SyntaxParser.BSR },
        { "cle", SyntaxParser.CLE },
        { "cpz", SyntaxParser.CPZ },
        { "dez", SyntaxParser.DEZ },
        { "dew", SyntaxParser.DEW },
        { "inw", SyntaxParser.INW },
        { "inz", SyntaxParser.INZ },
        { "ldz", SyntaxParser.LDZ },
        { "phw", SyntaxParser.PHZ },
        { "plz", SyntaxParser.PLZ },
        { "row", SyntaxParser.ROW },
        { "rtn", SyntaxParser.RTN },
        { "see", SyntaxParser.SEE },
        { "tab", SyntaxParser.TAB },
        { "taz", SyntaxParser.TAZ },
        { "tba", SyntaxParser.TBA },
        { "tsy", SyntaxParser.TSY },
        { "tys", SyntaxParser.TYS },
        { "tza", SyntaxParser.TZA },
        { "sp",  SyntaxParser.SP },
        { "z",   SyntaxParser.Z }
    };

    /// <summary>
    /// The Hudson Soft HuC6280 instructions.
    /// </summary>
    public static IDictionary<string, int> HuC6280 =>
    new Dictionary<string, int>
    {
        { "cla", SyntaxParser.CLA },
        { "clx", SyntaxParser.CLX },
        { "cly", SyntaxParser.CLY },
        { "csh", SyntaxParser.CSH },
        { "csl", SyntaxParser.CSL },
        { "say", SyntaxParser.SAY },
        { "st1", SyntaxParser.ST1 },
        { "st2", SyntaxParser.ST2 },
        { "sxy", SyntaxParser.SXY },
        { "tai", SyntaxParser.TAI },
        { "tam", SyntaxParser.TAM },
        { "tdd", SyntaxParser.TDD },
        { "tia", SyntaxParser.TIA },
        { "tii", SyntaxParser.TII },
        { "tin", SyntaxParser.TIN },
        { "tma", SyntaxParser.TMA },
        { "tst", SyntaxParser.TST }
    };

    /// <summary>
    /// The M65 (Mega-65) instructions.
    /// </summary>
    public static IDictionary<string, int> M65 =>
    new Dictionary<string, int>
    {
        { "adcq", SyntaxParser.ADCQ },
        { "andq", SyntaxParser.ANDQ },
        { "aslq", SyntaxParser.ASLQ },
        { "asrq", SyntaxParser.ASRQ },
        { "bitq", SyntaxParser.BITQ },
        { "cpq", SyntaxParser.CPQ },
        { "deq", SyntaxParser.DEQ },
        { "eorq", SyntaxParser.EORQ },
        { "inq", SyntaxParser.INQ },
        { "ldq", SyntaxParser.LDQ },
        { "lsrq", SyntaxParser.LSRQ },
        { "orq", SyntaxParser.ORQ },
        { "rolq", SyntaxParser.ROLQ },
        { "rorq", SyntaxParser.RORQ },
        { "sbcq", SyntaxParser.SBCQ },
        { "stq", SyntaxParser.STQ }
    };

    /// <summary>
    /// The Motorola 6800 instructions.
    /// </summary>
    public static IDictionary<string, int> M6800 =>
    new Dictionary<string, int>
    {
        { "aba",    SyntaxParser.ABA },
        { "adca",   SyntaxParser.ADCA },
        { "adcb",   SyntaxParser.ADCB },
        { "adda",   SyntaxParser.ADDA },
        { "addb",   SyntaxParser.ADDB },
        { "anda",   SyntaxParser.ANDA },
        { "andb",   SyntaxParser.ANDB },
        { "asl",    SyntaxParser.ASL },
        { "asla",   SyntaxParser.ASLA },
        { "aslb",   SyntaxParser.ASLB },
        { "asr",    SyntaxParser.ASR },
        { "asra",   SyntaxParser.ASRA },
        { "asrb",   SyntaxParser.ASRB },
        { "bcc",    SyntaxParser.BCC },
        { "bcs",    SyntaxParser.BCS },
        { "beq",    SyntaxParser.BEQ },
        { "bge",    SyntaxParser.BGE },
        { "bgt",    SyntaxParser.BGT },
        { "bhi",    SyntaxParser.BHI },
        { "bita",   SyntaxParser.BITA },
        { "bitb",   SyntaxParser.BITB },
        { "ble",    SyntaxParser.BLE },
        { "bls",    SyntaxParser.BLS },
        { "blt",    SyntaxParser.BLT },
        { "bmi",    SyntaxParser.BMI },
        { "bne",    SyntaxParser.BNE },
        { "bpl",    SyntaxParser.BPL },
        { "bra",    SyntaxParser.BRA },
        { "bsr",    SyntaxParser.BSR },
        { "bvc",    SyntaxParser.BVC },
        { "bvs",    SyntaxParser.BVS },
        { "cba",    SyntaxParser.CBA },
        { "clc",    SyntaxParser.CLC },
        { "cli",    SyntaxParser.CLI },
        { "clr",    SyntaxParser.CLR },
        { "clra",   SyntaxParser.CLRA },
        { "clrb",   SyntaxParser.CLRB },
        { "clv",    SyntaxParser.CLV },
        { "cmpa",   SyntaxParser.CMPA },
        { "cmpb",   SyntaxParser.CMPB },
        { "com",    SyntaxParser.COM },
        { "coma",   SyntaxParser.COMA },
        { "comb",   SyntaxParser.COMB },
        { "cpxa",   SyntaxParser.CPXA },
        { "daa",    SyntaxParser.DAA },
        { "dec",    SyntaxParser.DEC },
        { "deca",   SyntaxParser.DECA },
        { "decb",   SyntaxParser.DECB },
        { "des",    SyntaxParser.DES },
        { "dex",    SyntaxParser.DEX },
        { "eora",   SyntaxParser.EORA },
        { "eorb",   SyntaxParser.EORB },
        { "inc",    SyntaxParser.INC },
        { "inca",   SyntaxParser.INCA },
        { "incb",   SyntaxParser.INCB },
        { "ins",    SyntaxParser.INS },
        { "inx",    SyntaxParser.INX },
        { "jmp",    SyntaxParser.JMP },
        { "jsr",    SyntaxParser.JSR },
        { "ldaa",   SyntaxParser.LDAA },
        { "ldab",   SyntaxParser.LDAB },
        { "lds",    SyntaxParser.LDS },
        { "ldx",    SyntaxParser.LDX },
        { "lsr",    SyntaxParser.LSR },
        { "lsra",   SyntaxParser.LSRA },
        { "lsrb",   SyntaxParser.LSRB },
        { "neg",    SyntaxParser.NEG },
        { "nega",   SyntaxParser.NEGA },
        { "negb",   SyntaxParser.NEGB },
        { "nop",    SyntaxParser.NOP },
        { "oraa",   SyntaxParser.ORAA },
        { "orab",   SyntaxParser.ORAB },
        { "psha",   SyntaxParser.PSHA },
        { "pshb",   SyntaxParser.PSHB },
        { "pula",   SyntaxParser.PULA },
        { "pulb",   SyntaxParser.PULB },
        { "rol",    SyntaxParser.ROL },
        { "rola",   SyntaxParser.ROLA },
        { "rolb",   SyntaxParser.ROLB },
        { "ror",    SyntaxParser.ROR },
        { "rora",   SyntaxParser.RORA },
        { "rorb",   SyntaxParser.RORB },
        { "rti",    SyntaxParser.RTI },
        { "rts",    SyntaxParser.RTS },
        { "sba",    SyntaxParser.SBA },
        { "sbca",   SyntaxParser.SBCA },
        { "sbcb",   SyntaxParser.SBCB },
        { "sec",    SyntaxParser.SEC },
        { "sei",    SyntaxParser.SEI },
        { "sev",    SyntaxParser.SEV },
        { "staa",   SyntaxParser.STAA },
        { "stab",   SyntaxParser.STAB },
        { "sts",    SyntaxParser.STS },
        { "stx",    SyntaxParser.STX },
        { "suba",   SyntaxParser.SUBA },
        { "subb",   SyntaxParser.SUBB },
        { "swi",    SyntaxParser.SWI },
        { "tab",    SyntaxParser.TAB },
        { "tap",    SyntaxParser.TAP },
        { "tba",    SyntaxParser.TBA },
        { "tpa",    SyntaxParser.TPA },
        { "tst",    SyntaxParser.TST },
        { "tsta",   SyntaxParser.TSTA },
        { "tstb",   SyntaxParser.TSTB },
        { "tsx",    SyntaxParser.TSX },
        { "txs",    SyntaxParser.TXS },
        { "wai",    SyntaxParser.WAI },
        { "x",      SyntaxParser.X }
    };

    /// <summary>
    /// The Motorola 6809 instructions.
    /// </summary>
    public static IDictionary<string, int> M6809 =>
    new Dictionary<string, int>
    {
        { "a",      SyntaxParser.A   },
        { "abx",    SyntaxParser.ABX },
        { "adca",   SyntaxParser.ADCA },
        { "adcb",   SyntaxParser.ADCB },
        { "adda",   SyntaxParser.ADDA },
        { "addb",   SyntaxParser.ADDB },
        { "addd",   SyntaxParser.ADDD },
        { "anda",   SyntaxParser.ANDA },
        { "andb",   SyntaxParser.ANDB },
        { "andcc",  SyntaxParser.ANDCC },
        { "asl",    SyntaxParser.ASL },
        { "asla",   SyntaxParser.ASLA },
        { "aslb",   SyntaxParser.ASLB },
        { "asr",    SyntaxParser.ASR },
        { "asra",   SyntaxParser.ASRA },
        { "asrb",   SyntaxParser.ASRB },
        { "b",      SyntaxParser.B },
        { "bcc",    SyntaxParser.BCC },
        { "bcs",    SyntaxParser.BCS },
        { "beq",    SyntaxParser.BEQ },
        { "bge",    SyntaxParser.BGE },
        { "bgt",    SyntaxParser.BGT },
        { "bhi",    SyntaxParser.BHI },
        { "bhs",    SyntaxParser.BHS },
        { "bita",   SyntaxParser.BITA },
        { "bitb",   SyntaxParser.BITB },
        { "ble",    SyntaxParser.BLE },
        { "blo",    SyntaxParser.BLO },
        { "bls",    SyntaxParser.BLS },
        { "blt",    SyntaxParser.BLT },
        { "bmi",    SyntaxParser.BMI },
        { "bne",    SyntaxParser.BNE },
        { "bpl",    SyntaxParser.BPL },
        { "bra",    SyntaxParser.BRA },
        { "brn",    SyntaxParser.BRN },
        { "bsr",    SyntaxParser.BSR },
        { "bvc",    SyntaxParser.BVC },
        { "bvs",    SyntaxParser.BVS },
        { "cc",     SyntaxParser.CC },
        { "clr",    SyntaxParser.CLR },
        { "clra",   SyntaxParser.CLRA },
        { "clrb",   SyntaxParser.CLRB },
        { "cmpa",   SyntaxParser.CMPA },
        { "cmpb",   SyntaxParser.CMPB },
        { "cmpd",   SyntaxParser.CMPD },
        { "cmps",   SyntaxParser.CMPS },
        { "cmpu",   SyntaxParser.CMPU },
        { "cmpx",   SyntaxParser.CMPX },
        { "cmpy",   SyntaxParser.CMPY },
        { "com",    SyntaxParser.COM },
        { "coma",   SyntaxParser.COMA },
        { "comb",   SyntaxParser.COMB },
        { "cpxa",   SyntaxParser.CPXA },
        { "cwai",   SyntaxParser.CWAI },
        { "d",      SyntaxParser.D },
        { "daa",    SyntaxParser.DAA },
        { "dec",    SyntaxParser.DEC },
        { "deca",   SyntaxParser.DECA },
        { "decb",   SyntaxParser.DECB },
        { "dp",     SyntaxParser.DP },
        { "eora",   SyntaxParser.EORA },
        { "eorb",   SyntaxParser.EORB },
        { "exg",    SyntaxParser.EXG },
        { "inc",    SyntaxParser.INC },
        { "inca",   SyntaxParser.INCA },
        { "incb",   SyntaxParser.INCB },
        { "jmp",    SyntaxParser.JMP },
        { "jsr",    SyntaxParser.JSR },
        { "lbcc",   SyntaxParser.LBCC },
        { "lbcs",   SyntaxParser.LBCS },
        { "lbeq",   SyntaxParser.LBEQ },
        { "lbge",   SyntaxParser.LBGE },
        { "lbgt",   SyntaxParser.LBGT },
        { "lbhi",   SyntaxParser.LBHI },
        { "lbhs",   SyntaxParser.LBHS },
        { "lble",   SyntaxParser.LBLE },
        { "lblo",   SyntaxParser.LBLO },
        { "lbls",   SyntaxParser.LBLS },
        { "lblt",   SyntaxParser.LBLT },
        { "lbmi",   SyntaxParser.LBMI },
        { "lbne",   SyntaxParser.LBNE },
        { "lbpl",   SyntaxParser.LBPL },
        { "lbra",   SyntaxParser.LBRA },
        { "lbrn",   SyntaxParser.LBRN },
        { "lbsr",   SyntaxParser.LBSR },
        { "lbvc",   SyntaxParser.LBVC },
        { "lbvs",   SyntaxParser.LBVS },
        { "lda",    SyntaxParser.LDA },
        { "ldb",    SyntaxParser.LDB },
        { "ldd",    SyntaxParser.LDD },
        { "lds",    SyntaxParser.LDS },
        { "ldu",    SyntaxParser.LDU },
        { "ldx",    SyntaxParser.LDX },
        { "ldy",    SyntaxParser.LDY },
        { "leas",   SyntaxParser.LEAS },
        { "leau",   SyntaxParser.LEAU },
        { "leax",   SyntaxParser.LEAX },
        { "leay",   SyntaxParser.LEAY },
        { "lsl",    SyntaxParser.LSL },
        { "lsla",   SyntaxParser.LSLA },
        { "lslb",   SyntaxParser.LSLB },
        { "lsr",    SyntaxParser.LSR },
        { "lsra",   SyntaxParser.LSRA },
        { "lsrb",   SyntaxParser.LSRB },
        { "mul",    SyntaxParser.MUL },
        { "neg",    SyntaxParser.NEG },
        { "nega",   SyntaxParser.NEGA },
        { "negb",   SyntaxParser.NEGB },
        { "nop",    SyntaxParser.NOP },
        { "ora",    SyntaxParser.ORA },
        { "oraa",   SyntaxParser.ORAA },
        { "orb",    SyntaxParser.ORB },
        { "orcc",   SyntaxParser.ORCC },
        { "pc",     SyntaxParser.PC },
        { "pcr",    SyntaxParser.PCR },
        { "pshs",   SyntaxParser.PSHS },
        { "pshu",   SyntaxParser.PSHU },
        { "puls",   SyntaxParser.PULS },
        { "pulu",   SyntaxParser.PULU },
        { "rol",    SyntaxParser.ROL },
        { "rola",   SyntaxParser.ROLA },
        { "rolb",   SyntaxParser.ROLB },
        { "ror",    SyntaxParser.ROR },
        { "rora",   SyntaxParser.RORA },
        { "rorb",   SyntaxParser.RORB },
        { "rti",    SyntaxParser.RTI },
        { "rts",    SyntaxParser.RTS },
        { "s",      SyntaxParser.S },
        { "sbca",   SyntaxParser.SBCA },
        { "sbcb",   SyntaxParser.SBCB },
        { "sex",    SyntaxParser.SEX },
        { "sta",    SyntaxParser.STA },
        { "stb",    SyntaxParser.STB },
        { "std",    SyntaxParser.STD },
        { "sts",    SyntaxParser.STS },
        { "stu",    SyntaxParser.STU },
        { "stx",    SyntaxParser.STX },
        { "sty",    SyntaxParser.STY },
        { "suba",   SyntaxParser.SUBA },
        { "subb",   SyntaxParser.SUBB },
        { "subd",   SyntaxParser.SUBD },
        { "swi",    SyntaxParser.SWI },
        { "swi2",   SyntaxParser.SWI2 },
        { "swi3",   SyntaxParser.SWI3 },
        { "sync",   SyntaxParser.SYNC },
        { "tfr",    SyntaxParser.TFR },
        { ".tfradp", SyntaxParser.Tfradp },
        { ".tfrbdp", SyntaxParser.Tfrbdp },
        { "tst",    SyntaxParser.TST },
        { "tsta",   SyntaxParser.TSTA },
        { "tstb",   SyntaxParser.TSTB },
        { "u",      SyntaxParser.U },
        { "x",      SyntaxParser.X },
        { "y",      SyntaxParser.Y }
    };

    /// <summary>
    /// The Rockwell 65C02 instructions.
    /// </summary>
    public static IDictionary<string, int> R65C02 =>
    new Dictionary<string, int>
    {
        { "bbr", SyntaxParser.BBR },
        { "bbs", SyntaxParser.BBS },
        { "rmb", SyntaxParser.RMB },
        { "smb", SyntaxParser.SMB }
    };

    /// <summary>
    /// The WDC 65C02 instructions.
    /// </summary>
    public static IDictionary<string, int> W65C02 =>
    new Dictionary<string, int>
    {
        { "stp", SyntaxParser.STP },
        { "wai", SyntaxParser.WAI }
    };

    /// <summary>
    /// The Zilog Z80 instructions.
    /// </summary>
    public static IDictionary<string, int> Z80 =>
    new Dictionary<string, int>
    {
        { "a",      SyntaxParser.A },
        { "adc",    SyntaxParser.ADC },
        { "add",    SyntaxParser.ADD },
        { "and",    SyntaxParser.AND },
        { "af'",    SyntaxParser.ShadowAF },
        { "af",     SyntaxParser.AF },
        { "b",      SyntaxParser.B },
        { "bc",     SyntaxParser.BC },
        { "bit",    SyntaxParser.BITZ },
        { "c",      SyntaxParser.C },
        { "call",   SyntaxParser.CALL },
        { "ccf",    SyntaxParser.CCF },
        { "cp",     SyntaxParser.CP },
        { "cpd",    SyntaxParser.CPD },
        { "cpdr",   SyntaxParser.CPDR },
        { "cpi",    SyntaxParser.CPI },
        { "cpir",   SyntaxParser.CPIR },
        { "cpl",    SyntaxParser.CPL },
        { "d",      SyntaxParser.D },
        { "daa",    SyntaxParser.DAA },
        { "de",     SyntaxParser.DE },
        { "dec",    SyntaxParser.DEC },
        { "di",     SyntaxParser.DI },
        { "djnz",   SyntaxParser.DJNZ },
        { "e",      SyntaxParser.E },
        { "ei",     SyntaxParser.EI },
        { "ex",     SyntaxParser.EX },
        { "exx",    SyntaxParser.EXX },
        { "h",      SyntaxParser.H },
        { "halt",   SyntaxParser.HALT },
        { "hl",     SyntaxParser.HL },
        { "i",      SyntaxParser.I },
        { "im",     SyntaxParser.IM },
        { "in",     SyntaxParser.IN },
        { "inc",    SyntaxParser.INC },
        { "ind",    SyntaxParser.IND },
        { "indr",   SyntaxParser.INDR },
        { "ini",    SyntaxParser.INI },
        { "inir",   SyntaxParser.INIR },
        { "ix",     SyntaxParser.IX },
        { "ixh",    SyntaxParser.IXH },
        { "ixl",    SyntaxParser.IXL },
        { "iy",     SyntaxParser.IY },
        { "iyh",    SyntaxParser.IYH },
        { "iyl",    SyntaxParser.IYL },
        { "jp",     SyntaxParser.JP },
        { "jr",     SyntaxParser.JR },
        { "l",      SyntaxParser.L },
        { "ld",     SyntaxParser.LD },
        { "ldd",    SyntaxParser.LDD },
        { "lddr",   SyntaxParser.LDDR },
        { "ldi",    SyntaxParser.LDI },
        { "ldir",   SyntaxParser.LDIR },
        { "m",      SyntaxParser.M },
        { "n",      SyntaxParser.N },
        { "nc",     SyntaxParser.NC },
        { "neg",    SyntaxParser.NEG },
        { "nop",    SyntaxParser.NOP },
        { "nz",     SyntaxParser.NZ },
        { "or",     SyntaxParser.OR },
        { "otdr",   SyntaxParser.OTDR },
        { "otir",   SyntaxParser.OTIR },
        { "out",    SyntaxParser.OUT },
        { "outd",   SyntaxParser.OUTD },
        { "outi",   SyntaxParser.OUTI },
        { "p",      SyntaxParser.P },
        { "pe",     SyntaxParser.PE },
        { "po",     SyntaxParser.PO },
        { "pop",    SyntaxParser.POP },
        { "push",   SyntaxParser.PUSH },
        { "r",      SyntaxParser.R },
        { "res",    SyntaxParser.RES },
        { "ret",    SyntaxParser.RET },
        { "reti",   SyntaxParser.RETI },
        { "retn",   SyntaxParser.RETN },
        { "rl",     SyntaxParser.RL },
        { "rla",    SyntaxParser.RLA },
        { "rlc",    SyntaxParser.RLC },
        { "rlca",   SyntaxParser.RLCA },
        { "rld",    SyntaxParser.RLD },
        { "rr",     SyntaxParser.RR },
        { "rra",    SyntaxParser.RRA },
        { "rrc",    SyntaxParser.RRC },
        { "rrca",   SyntaxParser.RRCA },
        { "rrd",    SyntaxParser.RRD },
        { "rst",    SyntaxParser.RST },
        { "sbc",    SyntaxParser.SBC },
        { "scf",    SyntaxParser.SCF },
        { "set",    SyntaxParser.SET },
        { "sla",    SyntaxParser.SLA },
        { "sll",    SyntaxParser.SLL },
        { "sp",     SyntaxParser.SP },
        { "sra",    SyntaxParser.SRA },
        { "srl",    SyntaxParser.SRL },
        { "sub",    SyntaxParser.SUB },
        { "xor",    SyntaxParser.XOR },
        { "z",      SyntaxParser.Z }
    };

    /// <summary>
    /// The assembler directives.
    /// </summary>
    public static IDictionary<string, int> Directives =>
    new Dictionary<string, int>
    {
        { ".addr",          SyntaxParser.Addr },
        { ".align",         SyntaxParser.Align },
        { ".auto",          SyntaxParser.Auto },
        { ".assert",        SyntaxParser.Assert },
        { ".bank",          SyntaxParser.Bank },
        { ".bankbytes",     SyntaxParser.Bankbytes },
        { ".binary",        SyntaxParser.Binary },
        { ".binclude",      SyntaxParser.Binclude },
        { ".block",         SyntaxParser.Block },
        { ".break",         SyntaxParser.Break },
        { ".bstring",       SyntaxParser.Bstring },
        { ".byte",          SyntaxParser.Byte },
        { ".case",          SyntaxParser.Case },
        { ".char",          SyntaxParser.Char },
        { ".cbmflt",        SyntaxParser.Cbmflt },
        { ".cbmfltp",       SyntaxParser.Cbmfltp },
        { ".continue",      SyntaxParser.Continue },
        { ".cpu",           SyntaxParser.Cpu },
        { ".cstring",       SyntaxParser.Cstring },
        { ".default",       SyntaxParser.Default },
        { ".dint",          SyntaxParser.Dint },
        { ".do",            SyntaxParser.Do },
        { ".dp",            SyntaxParser.Dp  },
        { ".dsection",      SyntaxParser.Dsection },
        { ".dword",         SyntaxParser.Dword },
        { ".echo",          SyntaxParser.Echo },
        { ".else",          SyntaxParser.Else },
        { ".elseif",        SyntaxParser.Elseif },
        { ".elseifconst",   SyntaxParser.Elseifconst },
        { ".elseifdef",     SyntaxParser.Elseifdef },
        { ".elseifnconst",  SyntaxParser.Elseifnconst },
        { ".elseifndef",    SyntaxParser.Elseifndef },
        { ".encoding",      SyntaxParser.Encoding },
        { ".end",           SyntaxParser.End },
        { ".endblock",      SyntaxParser.Endblock },
        { ".endenum",       SyntaxParser.Endenum },
        { ".endfunction",   SyntaxParser.Endfunction },
        { ".endif",         SyntaxParser.Endif },
        { ".endmacro",      SyntaxParser.Endmacro },
        { ".endnamespace",  SyntaxParser.Endnamespace },
        { ".endpage",       SyntaxParser.Endpage },
        { ".endproc",       SyntaxParser.Endproc },
        { ".endrelocate",   SyntaxParser.Endrelocate },
        { ".endrepeat",     SyntaxParser.Endrepeat },
        { ".endswitch",     SyntaxParser.Endswitch },
        { ".endwhile",      SyntaxParser.Endwhile },
        { ".enum",          SyntaxParser.Enum },
        { ".eor",           SyntaxParser.DotEor },
        { ".equ",           SyntaxParser.Equ },
        { ".error",         SyntaxParser.Error },
        { ".errorif",       SyntaxParser.Errorif },
        { ".fill",          SyntaxParser.Fill },
        { ".for",           SyntaxParser.For },
        { ".forcepass",     SyntaxParser.Forcepass },
        { ".foreach",       SyntaxParser.Foreach },
        { ".format",        SyntaxParser.Format },
        { ".function",      SyntaxParser.Function },
        { ".global",        SyntaxParser.Global },
        { ".goto",          SyntaxParser.Goto },
        { ".hibytes",       SyntaxParser.Hibytes },
        { ".hstring",       SyntaxParser.Hstring },
        { ".hiwords",       SyntaxParser.Hiwords },
        { ".if",            SyntaxParser.If },
        { ".ifconst",       SyntaxParser.Ifconst },
        { ".ifdef",         SyntaxParser.Ifdef },
        { ".ifnconst",      SyntaxParser.Ifnconst },
        { ".ifndef",        SyntaxParser.Ifndef },
        { ".import",        SyntaxParser.Import },
        { ".include",       SyntaxParser.Include },
        { ".initmem",       SyntaxParser.Initmem },
        { ".invoke",        SyntaxParser.Invoke },
        { ".label",         SyntaxParser.Label },
        { ".lobytes",       SyntaxParser.Lobytes },
        { ".let",           SyntaxParser.Let },
        { ".lint",          SyntaxParser.Lint },
        { ".long",          SyntaxParser.Long },
        { ".lstring",       SyntaxParser.Lstring },
        { ".lowords",       SyntaxParser.Lowords },
        { ".m8",            SyntaxParser.M8 },
        { ".m16",           SyntaxParser.M16 },
        { ".macro",         SyntaxParser.Macro },
        { ".manual",        SyntaxParser.Manual },
        { ".map",           SyntaxParser.Map },
        { ".mx8",           SyntaxParser.MX8 },
        { ".mx16",          SyntaxParser.MX16 },
        { ".namespace",     SyntaxParser.Namespace },
        { ".next",          SyntaxParser.Next },
        { ".nstring",       SyntaxParser.Nstring },
        { ".org",           SyntaxParser.Org },
        { ".page",          SyntaxParser.Page },
        { ".proc",          SyntaxParser.Proc },
        { ".proff",         SyntaxParser.Proff },
        { ".pron",          SyntaxParser.Pron },
        { ".pseudopc",      SyntaxParser.Pseudopc },
        { ".pstring",       SyntaxParser.Pstring },
        { ".realpc",        SyntaxParser.Realpc },
        { ".relocate",      SyntaxParser.Relocate },
        { ".repeat",        SyntaxParser.Repeat },
        { ".return",        SyntaxParser.Return },
        { ".rta",           SyntaxParser.Rta },
        { ".sbyte",         SyntaxParser.Sbyte },
        { ".section",       SyntaxParser.Section },
        { ".short",         SyntaxParser.Short },
        { ".sint",          SyntaxParser.Sint },
        { ".string",        SyntaxParser.String },
        { ".stringify",     SyntaxParser.Stringify },
        { ".switch",        SyntaxParser.Switch },
        { ".unmap",         SyntaxParser.Unmap },
        { ".warn",          SyntaxParser.Warn },
        { ".warnif",        SyntaxParser.Warnif },
        { ".while",         SyntaxParser.While },
        { ".whiletrue",     SyntaxParser.Whiletrue },
        { ".word",          SyntaxParser.Word },
        { ".x8",            SyntaxParser.X8 },
        { ".x16",           SyntaxParser.X16 },
        { "false",          SyntaxParser.False },
        { "NaN",            SyntaxParser.NaN },
        { "true",           SyntaxParser.True },
    };

    /// <summary>
    /// The M65xx family of instruction sets.
    /// </summary>
	public static IDictionary<string, IDictionary<string, int>> M65xx =>
	new Dictionary<string, IDictionary<string, int>>
	{
        {
            "45GS02", M6502
               .Concat(M45GS02)
               .Concat(M65C02)
               .Concat(M65CE02)
               .Concat(R65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
		{
			"6502", M6502
		},
        {
            "6502i", M6502.Concat(M6502i).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "65816", M6502
             .Concat(M65C02)
             .Concat(M65816)
             .Concat(W65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "65C02", M6502.Concat(M65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "65CE02", M6502
              .Concat(M65C02)
              .Concat(M65CE02)
              .Concat(R65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "c64dtv2", M6502.Concat(C64dtv2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "HuC6280", M6502
              .Concat(M65C02)
              .Concat(HuC6280)
              .Concat(R65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "m65", M6502.Where(o => !o.Key.Equals("nop"))
           .Concat(M45GS02)
           .Concat(M65)
           .Concat(M65C02)
           .Concat(M65CE02)
           .Concat(R65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "R65C02", M6502
              .Concat(M65C02)
              .Concat(R65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        },
        {
            "W65C02", M6502
              .Concat(M65C02)
              .Concat(R65C02)
              .Concat(W65C02).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        }
	};

    /// <summary>
    /// The Motolora 680x family of instruction sets.
    /// </summary>
    public static IDictionary<string, IDictionary<string, int>> M680x =>
    new Dictionary<string, IDictionary<string, int>>
    {
        { "m6800", M6800 },
        { "m6809", M6809 }
    };

    /// <summary>
    /// The Intel/Zilog family of instruction sets.
    /// </summary>
    public static IDictionary<string, IDictionary<string, int>> IntelZilog =>
    new Dictionary<string, IDictionary<string, int>>
    {
        { "i8080", I8080 },
        { "z80", Z80 },
        { "gb80", GB80 }
    };

    /// <summary>
    /// Get the instruction set by cpuid.
    /// </summary>
    /// <param name="cpuid">The cpuid.</param>
    /// <returns>The instruction set of the valid cpuid.</returns>
    /// <exception cref="Error"></exception>
    public static IDictionary<string, int> GetByCpuid(string cpuid)
    {
        return (cpuid switch
        {
            "45GS02"    or
            "6502"      or
            "6502i"     or
            "65816"     or
            "65C02"     or
            "65CE02"    or
            "c64dtv"    or
            "HuC6280"   or
            "m65"       or
            "R65C02"    or
            "W65C02"    => M65xx[cpuid],

            "m6800"     or
            "m6809"     => M680x[cpuid],

            "z80"       or
            "gb80"      or
            "i8080"     => IntelZilog[cpuid],
            _           => throw new Error($"CPU \"{cpuid}\" not valid.")
        }).Concat(Directives).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static IDictionary<string, int> DefaultInstructionSet => GetByCpuid("6502");
}

