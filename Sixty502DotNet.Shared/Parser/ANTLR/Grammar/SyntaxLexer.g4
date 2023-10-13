//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

lexer grammar SyntaxLexer;

options {
    superClass=LexerBase;
}

tokens {
    /* constants */
    True, False, NaN,

    /* directives */
    Addr, Align, Assert, Auto, Bank, Bankbytes, Binary, Binclude, 
    Block, Break, Bstring, Byte, Case, Cbmflt, Cbmfltp, Char, 
    Continue, Cpu, Cstring, Default, Dint, Do, DotEor, Dp, Dsection, 
    Dword, Echo, Encoding, End, Endblock, Endenum, Endfunction, Endif,
    Endmacro, Endnamespace, Endpage, Endproc, Endrelocate, Endrepeat, 
    Endswitch, Endwhile, Enum, Else,Elseif, Elseifconst, Elseifdef, 
    Elseifnconst, Elseifndef, Equ, Error, Errorif, Fill, For,
    Forcepass, Foreach, Format, Function, Global, Goto, Hibytes, 
    Hstring, Hiwords, If, Ifconst, Ifdef, Ifnconst, Ifndef,  
    Import, Include, Initmem, Invoke, Label, Lobytes, Let, Lint,
    Long, Lstring, Lowords, Manual, Macro, Map, M8, M16, MX8, MX16,
    Namespace, Next, Nstring, Org, Page, Proc, Proff, Pron, Pseudopc, 
    Pstring, Realpc, Relocate, Repeat, Return, Rta, Sbyte, Section, 
    Short, Sint, String, Stringify, Switch, Tfradp, Tfrbdp, Unmap, 
    Warn, Warnif, Word, While, Whiletrue, X8, X16,

    /* 45GS02 */
    EOM, MAP,

    /* 6502 */
    ADC, AND, ASL, BCC, BCS, BEQ, BIT, BMI, BNE, BPL, BRK, BVC,
    BVS, CLC, CLD, CLI, CLV, CMP, CPX, CPY, DEC, DEX, DEY, EOR,
    INC, INX, INY, JMP, JSR, LDA, LDX, LDY, LSR, NOP, ORA, PHA,
    PHP, PLA, PLP, ROL, ROR, RTI, RTS, SBC, SEC, SED, SEI, STA,
    STX, STY, TAX, TAY, TSX, TXA, TXS, TYA,

    /* 6502i */
    ANC, ANE, ARR, ASR, DCP, DOP, ISB, JAM, LAS, LAX, SAX, SHA,
    SHX, SHY, SLO, SRE, STP, TAS, TOP,

    /* 65816 */
    BRL, COP, JML, JSL, MVN, MVP, PEA, PEI, PER, PHB, PHD, PHK,
    PLB, PLD, REP, RTL, SEP, TCD, TCS, TDC, TSC, TXY, TYX, WDM,
    XBA, XCE,

    /* 65C02 */
    BRA, PHX, PHY, PLX, PLY, STZ, TRB, TSB,

    /* 65CE02 */
    ASW, BGE, BLT, BSR, CLE, CPZ, DEZ, DEW, INW, INZ, LDZ, PHW,
    PHZ, PLZ, ROW, RTN, SEE, TAB, TAZ, TBA, TSY, TYS, TZA,

    /* 6800 */
    ABA,  ADCA, ADCB, ADDA, ADDB, ANDA, ANDB, ASLA, ASLB, ASRA,
    ASRB, BGT,  BHI,  BITA, BITB, BLE,  BLS,  CBA,  CLR,  CLRA,
    CLRB, CMPA, CMPB, COM,  COMA, COMB, COMPA,CPXA, DECA, DECB,
    DES,  EORA, EORB, INCA, INCB, INS,  LDAA, LDAB, LDS,  LSRA,
    LSRB, NEGA, NEGB, ORAA, ORAB, PSHA, PSHB, PULA, PULB, ROLA,
    ROLB, RORA, RORB, SBA,  SBCA, SBCB, SEV,  STAA, STAB, STS,
    SUBA, SUBB, SWI,  TAP,  TPA,  TSTA, TSTB,

    /* 6809 */
    ABX,  ADDD, ANDCC, BHS,  BLO,  BRN,  CMPD, CMPS, CMPU, CMPX,
    CMPY, CWAI, DAA,   EXG,  LBCC, LBCS, LBEQ, LBGE, LBGT, LBHI,
    LBHS, LBLE, LBLO,  LBLS, LBLT, LBMI, LBNE, LBPL, LBRA, LBRN,
    LBSR, LBRSR,LBVC,  LBVS, LDB,  LDD,  LDU,  LEAS, LEAU, LEAX, 
    LEAY, LSL,  LSLA,  LSLB, MUL,  NEG,  ORB,  ORCC, PSHS, PSHU,
    PULS, PULU, SEX,   STB,  STD,  STU,  SUBD, SWI2, SWI3, SYNC,
    TFR,  DP,   PC,    PCR,  U,

    /* c64dtv2 */
    SAC, SIR,

    /* GB80 */
    STOP, SWAP,

    /* HuC6280 */
    CLA, CLX, CLY, CSH, CSL, SAY, ST1, ST2, SXY, TAI, TAM, TDD,
    TIA, TII, TIN, TMA, TST,

    /* i8080 */
    ACI, ADI,  ANA,  ANI,  CM,   CMA, CMC, CNC,  CNZ,  CPE,  CPO,
    CZ,  DAD,  DCR,  DCX,  HLT,  INR, INX, JC,   JM,   JNC,  JNZ,
    JPE, JPO,  JZ,   LDAX, LHLD, LXI, MOV, MVI,  ORI,  PCHL, PSW,
    RAL, RAR,  RC,   RM,   RNC,  RNZ, RP,  RPE,  RPO,  RZ,   SBB,
    SBI, SHLD, SPHL, STAX, STC,  SUI, XCHG, XRA, XRI,  XTHL,

    /* m65 */
    ADCQ, ANDQ, ASLQ, ASRQ, BITQ, CPQ, DEQ, EORQ, INQ, LDQ, 
    LSRQ, ORQ, ROLQ, RORQ, SBCQ, STQ,

    /* psuedo6502 */
    JCC, JCS, JEQ, JMI, JNE, JPL, JVC, JVS,

    /* R65C02 */
    BBR, BBS, RMB, SMB,

    /* W65C02 */
    WAI,

    /* Z80 */
    ADD,  BITZ, CALL, CCF,  CP,   CPD,  CPDR, CPI,  CPIR, CPL,  
    DI,   DJNZ, EI,   EX,   EXX,  HALT, IM,   IN,   IND,  INDR, 
    INI,  INIR, JP,   JR,   LD,   LDD,  LDDR, LDI,  LDIR, OR,  
    OTDR, OTIR, OUT,  OUTD, OUTI, POP,  PUSH, RES,  RET,  RETI, 
    RETN, RL,   RLA,  RLC,  RLCA, RLD,  RR,   RRA,  RRC,  RRCA,
    RRD,  RST,  SCF,  SET,  SLA,  SLL,  SRA,  SRL,  SUB,  XOR,
    AF,   C,    BC,   E,    DE,   H,    L,    HL,   I,    IX,
    IXH,  IXL,  IY,   IYH,  IYL,  M,    N,    NC,   NZ,   P,
    PE,   PO,   R,

    /* shared */
    A, B, CC, D, S, SP, X, Y, Z
}

DotIdentifier
    : ('.' Identifier) { IsReservedWord(); }
    ;

MacroSub
    : '\\' [1-9] Digit*
    | '\\' Identifier
    | '\\*' 
    ;

ShadowAF
    :   IdentifierHead IdentifierHead ['] { IsShadowAF() }?
    ;

Identifier
    :   IdentifierHead (IdentifierHead | Digit)* { IsReservedWord(); }
    ;

MDoubleQuote
    :   '$"""' -> pushMode(MSTRING)
    ;

DoubleQuote
    :   '$"' -> pushMode(STRING)
    ;

UnicodeStringLiteral
    :   ('u' '8'? | 'U') StringLiteral
    ;

StringLiteral
    :   '"""' .+? '"""'
    |   '"' SChar+ '"'
    ;

CharLiteral
    :   '\'' CChar '\''
    ;

HexLiteral
    :   ('0' [xX] | '$') HexDigitString
    ;

HexFloatLiteral
    :   HexLiteral '.' HexDigitString Exponent?
    |   HexLiteral Exponent
    ;

BinLiteral
    :   '0' [bB] BinDigitString
    |   '%' BinDigitString { !PreviousIsExpr() }?
    ;

BinFloatLiteral
    :   BinLiteral '.' BinDigitString Exponent?
    |   BinLiteral Exponent
    ;

AltBinLiteral
    :   '%' [.#]+
    ;

DecLiteral
    :   '0'
    |   [1-9] ('_'* DigitString*)?
    ;

DecFloatLiteral
    :   DecLiteral '.' DigitString Exponent?
    |   DecLiteral Exponent
    ;

OctLiteral
    :   '0' ([oO] | '_'+)? OctalDigitString
    ;

OctFloatLiteral
    :   OctLiteral '.' OctalDigitString Exponent?
    |   OctLiteral Exponent
    ;

LineComment
    :   
    (   '//'
    |   ';'
    )   ~[\r\n]* -> skip
    ;

BlockComment
    :   '/*' .*? '*/' -> skip
    ;

ARShiftEqual:   '>>>='  ;
ARShift:        '>>>'   ;
RShiftEqual:    '>>='   ;
RShift:         '>>'    ;
GTE:            '>='    ;
RightAngle:     '>'     ;
LShiftEqual:    '<<='   ;
LShift:         '<<'    ;
Spaceship:      '<=>'   ;
LTE:            '<='    ;
LeftAngle:      '<'     ;
TripleEqual:    '==='   ;
DoubleEqual:    '=='    ;
Arrow:          '=>'    ;
Equal:          '='     ;
BangDoubleEqual:'!=='   ;
BangEqual:      '!='    ;
Bang:           '!'     ;
DoubleAmpersand:'&&'    ;
AmpersandEqual: '&='    ;
Ampersand:      '&'     ;
DoubleCaret:    '^^'    ;
CaretEqual:     '^='    ;
Caret:          '^'     ;
DoubleBar:      '||'    ;
BarEqual:       '|='    ;
Bar:            '|'     ;
AsteriskEqual:  '*=' { PreviousIsExpr() }?;
Asterisk:       '*'     ;
SolidusEqual:   '/='    ;
Solidus:        '/'     ;
PercentEqual:   '%='    ;
Percent:        '%'     ;
MultiPlus:      '+++'+  DoublePlus? Plus?;
DoublePlus:     '++'    ;
PlusEqual:      '+='    ;
Plus:           '+'     ;
MultiHyphen:    '---'+  ;
DoubleHyphen:   '--'    ;
HyphenEqual:    '-='    ;
Hyphen:         '-'     ;
ColonEqual:     ':='    ;
Colon:          ':'     ;
DoubleDot:      '..'    ;
Dot:            '.'     ;
Comma:          ','     ;
Query:          '?'     ;
Octothorpe:     '#'     ;
Tilde:          '~'     ;
LeftParen:      '(' { groups++; } ;
LeftSquare:     '[' { groups++; } ;
LeftCurly:      '{' { SaveGroups(); };
RightParen:     ')' { groups--; } ;
RightSquare:    ']' { groups--; } ;
RightCurly:     '}' { UnsaveGroups(); }; 

NL
    :   [\n\r]+ { SkipNewline(); }
    ;

WS
    :
    (   [ \t]+
    |   '\\' [\n\r]
    ) -> skip
    ;

UnclosedLiteral
    :   '"'
    |   [']
    ;

Unrecogized
    :   .
    ;

mode MSTRING;

MSInterpolStart
    :   '{' -> pushMode(DEFAULT_MODE)
    ;
  
MSText
    :   (~[{"] | InterpolEscape)+
    ;


EndMString
    :   '"""' -> type(MDoubleQuote), popMode
    ;

mode STRING;

InterpolStart
    :   '{' -> pushMode(DEFAULT_MODE)
    ;

IText
    :   InterpolSChar+ 
    ;

EndString
    :   '"' -> type(DoubleQuote), popMode
    ;

fragment
IdentifierHead
    :   '_'
    |   [\p{Letter}] 
    ;

fragment
InterpolSChar
    :   ~[\\"\n\r{]
    |   InterpolEscape
    ;

fragment
SChar
    :   ~[\\"\n\r]
    |   Escape
    |   UnicodeEscape
    ;

fragment
CChar
    :   ~[\\'\n\r]
    |   Escape
    ;

fragment
InterpolEscape
    :   Escape
    |   UnicodeEscape
    |   '{{'
    |   '}}'
    ; 

fragment
Escape
    :   '\\' 
    (   [\\?abfnrtv'"0]
    |   OctalDigit OctalDigit OctalDigit
    |   'u' HexDigit HexDigit HexDigit HexDigit
    |   'x' HexDigit HexDigit HexDigit HexDigit?
    )
    ;

fragment
UnicodeEscape
    :   '\\U' HexDigit HexDigit HexDigit HexDigit
              HexDigit HexDigit HexDigit HexDigit
    ;
    

fragment
Exponent
    :   [eEpP] [\-+]? DigitString
    ;

fragment
HexDigitString
    :   HexDigit+ ('_'* HexDigit)*
    ;

fragment
HexDigit
    :   [0-9A-Fa-f]
    ;

fragment
DigitString
    :   Digit ('_'* Digit)*
    ;

fragment
Digit
    :   '0' .. '9'
    ;

fragment
OctalDigitString
    :   OctalDigit+ ('_'* OctalDigit)*
    ;

OctalDigit
    :   '0' .. '7'
    ;

fragment
BinDigitString
    :   BinDigit+ ('_'* BinDigit)*
    ;

fragment
BinDigit
    :   '0' .. '1'
    ;

