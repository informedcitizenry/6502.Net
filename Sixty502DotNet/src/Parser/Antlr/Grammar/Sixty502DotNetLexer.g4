//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

lexer grammar Sixty502DotNetLexer;

options {
    superClass=LexerBase;
    caseInsensitive=true;
}

channels { PREPROCESSOR, INCLUDE_CHANNEL, ERROR }

tokens {
    /* directives */
    Addr, Align, Assert, Auto, BadMacro, Bank, Bankbytes, Binary, 
    Block, Break, Bstring, Byte, Case, Cbmflt, Cbmfltp, Char, 
    Continue, Cstring, Default, Dint, Do, DotEor, Dp, Dsection, 
    Dword, Echo, Encoding, Endblock, Endenum, Endfunction, Endif,
    Endnamespace, Endpage, Endproc, Endrelocate, Endrepeat, 
    Endswitch, Endwhile, Enum, Else,Elseif, Elseifconst, Elseifdef, 
    Elseifnconst, Elseifndef, Equ, Error, Errorif, Fill, For,
    Forcepass, Foreach, Format, Function, Global, Goto, Hibytes, 
    Hstring, Hiwords, If, Ifconst, Ifdef, Ifnconst, Ifndef, 
    IllegalCpu, Import, Initmem, Invoke, Label, Lobytes,Let, Lint,
    Long, Lstring, Lowords, Manual,Map, M8, M16, MX8, MX16,
    Namespace, Next, Nstring, Org, Page, Proc, Proff, Pron, Pseudopc, 
    Pstring, Realpc, Relocate, Repeat, Return, Rta, Sbyte, 
    Section, Short,Sint, String, Switch, Tfradp, Tfrbdp, Unmap, 
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
    TFR,  CC,   D,     DP,   PC,   PCR,  U,

    /* c64dtv2 */
    SAC, SIR,

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
    ADD,  CALL, CCF,  CP,   CPD,  CPDR, CPI,  CPIR, CPL,  DI,
    DJNZ, EI,   EX,   EXX,  HALT, IM,   IN,   IND,  INDR, INI,  
    INIR, JP,   JR,   LD,   LDD,  LDDR, LDI,  LDIR, OR,   OTDR,
    OTIR, OUT,  OUTD, OUTI, POP,  PUSH, RES,  RET,  RETI, RETN,
    RL,   RLA,  RLC,  RLCA, RLD,  RR,   RRA,  RRC,  RRCA, RRD,
    RST,  SCF,  SET,  SLA,  SLL,  SRA,  SRL,  SUB,  XOR,
    AF,   ShadowAF,   C,    BC,   E,    DE,   H,    L,    HL,
    I,    IX,   IXH,  IXL,  IY,   IYH,  IYL,  M,    N,    NC,
    NZ,   P,    PE,   PO,   R,

    /* shared */
    A, B, CC, S, SP, Y, X, Z
}

Cpu
    :   CpuDirective 
    {   SetCPU(); }
    ;

MacroDef
    :   Ident CommentWS+ '.macro'
    {   !MacroDefined(Text) }?
    {   DefineMacro(); }
    ;

Include
    :   IncludeDirective 
    {   ExpandInclude(); }
    ;

Directive
    :   ('.' Ident) { IsDirective() }?
    ;

MacroInvocation 
    :   ('.' Ident) { IsInvocation() }?
    {   ExpandMacro(); }
    ;

ShadowAF
    :   'af\''
    {   IsShadowAf() }?
    ;


Ident
    :   (Letter | '_') (Letter | [0-9_])*
    {   CheckReserved(); }
    ;

HexadecimalDouble
    :   Hexadecimal Dot HexDigit ('_'? HexDigit)* BinaryExponent?
    |   Hexadecimal BinaryExponent
    ;

BinaryLiteralDouble
    :   BinaryLiteral Dot BinaryDigits (Exponent | BinaryExponent)?
    |   BinaryLiteral (Exponent | BinaryExponent)
    |   Percent (BinaryDigits | BinaryDigitsDouble) (Exponent | BinaryExponent)
    ;

BinaryDigitsDouble
    :   BinaryDigits Dot BinaryDigits
    ;

Double
    :   Integer Dot [0-9] TrailingDigits Exponent?
    ;

OctalDouble
    :   Octal Dot [0-7] ('_'? [0-7])* (Exponent | BinaryExponent)?
    |   Octal (Exponent | BinaryExponent)
    ;

Hexadecimal
    :   ('$' | '0x') HexDigit ('_'? HexDigit)* 
    ;

BinaryDigits
    :   [01] ('_'? [01])*
    ;

Integer
    :   '0'
    |   [1-9] TrailingDigits Exponent?
    ;

BinaryLiteral
    :   '0b' [01] ('_'? [01])*
    ;

Octal
    :   '0' 'o'? [0-7] ('_'? [0-7])*
    ;

AltBinary
    :   '%' [.#]+
    ;

StringLiteral 
    :   '"""' .+? '"""'
    |   '"' (~["\\\r\n] | StringEscapeSequence)+ '"'
    ;

CharLiteral
    :   '\'' (~['\\\r\n] | EscapeSequence) '\''
    ;

ForwardReference
    :   Plus Plus+
    ;

BackwardReference
    :   Hyphen Hyphen+
    ;

RightSignShiftEq:   '>>>='  ;
RightSignShift  :   '>>>'   ;
TripleEqual     :   '==='   ;
BangDoubleEqual :   '!=='   ;
LeftShiftEqual  :   '<<='   ;
RightShiftEqual :   '>>='   ;
Spaceship       :   '<=>'   ;
LeftShift       :   '<<'    ;
RightShift      :   '>>'    ;
PlusEqual       :   '+='    ;
HyphenEqual     :   '-='    ;
ColonEqual      :   ':='    ;
AsteriskEq      :   '*='    ;
SolidusEqual    :   '/='    ;
PercentEqual    :   '%='    ;
CaretEqual      :   '^='    ;
AmpersandEqual  :   '&='    ;
PipeEqual       :   '|='    ;
DoubleEqual     :   '=='    ;
BangEqual       :   '!='    ;
LTE             :   '<='    ;
GTE             :   '>='    ;
DoublePipe      :   '||'    ;
DoubleAmpersand :   '&&'    ;
DoubleCaret     :   '^^'    ;
DoubleDot       :   '..'    ;
Plus            :   '+'     ;
Hyphen          :   '-'     ;
Asterisk        :   '*'     ;
Solidus         :   '/'     ;
Comma           :   ','     ;
Caret           :   '^'     ;
Ampersand       :   '&'     ;
Percent         :   '%'     ;
Dollar          :   '$'     ;
Hash            :   '#'     ;
Bang            :   '!'     ;
Tilde           :   '~'     ;
LeftParen       :   '(' {Group();}   ;
RightParen      :   ')' {Ungroup();} ;
LeftCurly       :   '{' {Group();}   ;
RightCurly      :   '}' {Ungroup();} ;
LeftSquare      :   '[' {Group();}   ;
RightSquare     :   ']' {Ungroup();} ;
Query           :   '?'     ;
LeftAngle       :   '<'     ;
RightAngle      :   '>'     ;
Colon           :   ':'     ;
SingleQuote     :   '\''    ;
DoubleQuote     :   '"'     ;
Pipe            :   '|'     ;
Backslash       :   '\\'    ;
Equal           :   '='     ;
Dot             :   '.'     ;

End
    :   '.end' [ \t\r\n] .*? EOF -> channel(HIDDEN)
    ;

EndEof
    :   '.end' EOF -> channel(HIDDEN)
    ;

BlockComment
    :   Comment -> channel(HIDDEN)
    ;

LineComment
    :   (';'|'//') ~[\r\n]* -> channel(HIDDEN)
    ;

EscapedNewline
    :   '\\' [ \t]* [\r\n] -> channel(HIDDEN)
    ;

Newline
    :   [\r\n]+ {IsNewline();}
    ;

WS
    :   [ \t]+ -> channel(HIDDEN)
    ;

BadToken
    :   . -> channel(ERROR)
    ;

mode macroArgMode;

MacroArg
    :   MacroIdent -> channel(PREPROCESSOR)
    ;

MacroArgComma
    :   ',' -> channel(PREPROCESSOR)
    ;

MacroArgEscapedNewline
    :   '\\' [ \t]* [\r\n] -> channel(HIDDEN)
    ;

MacroArgNewline
    :   [\r\n] -> channel(PREPROCESSOR), mode(macroBlockMode)
    ;

MacroArgDefaultBegin
    :   '=' -> channel(PREPROCESSOR), mode(macroArgDefaultMode)
    ;

MacroArgWS
    :   [ \t]+ -> channel(HIDDEN)
    ;

MacroArgBlockComment
    :   Comment -> channel(HIDDEN)
    ;

MacroArgLineComment
    :   (';'|'//') ~[\r\n]* -> channel(HIDDEN)
    ;

MacroArgBad
    :   . -> channel(PREPROCESSOR)
    ;

mode macroArgDefaultMode;

MacroDefaultSpecial
    :   [\\/] -> more
    ;

MacroDefaultLeftGroup
    :
    (   '('
    |   '['
    |   '{'
    )   { Group(); } -> channel(PREPROCESSOR)
    ;

MacroDefaultRightGroup
    :
    (   ')'
    |   ']'
    |   '}'
    )   { Ungroup(); } -> channel(PREPROCESSOR)
    ; 

MacroDefaultComma
    :   ',' { IsMacroDefaultArgEnd(); }
    ;

MacroDefaultString
    :   '"' (~["\\\r\n] | StringEscapeSequence)* '"' -> channel(PREPROCESSOR)
    ;

MacroDefaultChar
    :   ['] (~['\\\r\n] | EscapeSequence) ['] -> channel(PREPROCESSOR)
    ;

MacroDefaultEscapedNewline
    :   '\\' [ \t]* [\r\n] -> channel(HIDDEN)
    ;

MacroDefaultBackslash
    :   '\\' -> channel(PREPROCESSOR)
    ;

MacroDefaultText
    :   ~[,\\([{)\]}"\r\n/;]+ -> channel(PREPROCESSOR)
    ;

MacroDefaultNewline
    :   [\r\n]+ -> channel(PREPROCESSOR), mode(macroBlockMode)
    ;

MacroDefaultBlockComment
    :   Comment -> channel(HIDDEN)
    ;

MacroDefaultLineComment
    :   (';'|'//') ~[\r\n]* -> channel(HIDDEN)
    ;

mode macroBlockMode;

MacroBlockEndDirective
    :   '.end' [ \t\r\n] .*? EOF -> channel(HIDDEN)
    ;

MacroBlockEndDirectiveEof
    :   '.end' EOF -> channel(HIDDEN)
    ;

MacroBlockEnd
    :   '.endmacro' { IsEndMacro() }? -> channel(PREPROCESSOR),
    mode(DEFAULT_MODE)
    ;

MacroBlockSpecialPrefix
    :   [\\"./] -> more
    ;

MacroSubstitution
    :   Substitution -> channel(PREPROCESSOR)
    ;

MacroBlockParamString
    :   '"' NonParamStrElement* ParamStrElement (~["\\\r\n] | StringEscapeSequence)*
        '"' -> channel(PREPROCESSOR)
    ;

MacroBlockUserString
    :   
    (   '"""' (~'"' | '""')* '"""'
    |   '"' (~["\\\r\n] | StringEscapeSequence)* '"'
    ) -> channel(PREPROCESSOR)
    ;

MacroBlockUserChar
    :   ['] (~['\\\r\n] | EscapeSequence) ['] -> channel(PREPROCESSOR)
    ;

MacroBlockUserText
    :   ~[\\"./;]+ -> channel(PREPROCESSOR)
    ;

MacroBlockBlockComment
    :   Comment -> channel(HIDDEN)
    ;

MacroBlockLineComment
    :   (';'|'//') ~[\r\n]* -> channel(HIDDEN)
    ;

mode macroInvokeArgMode;

MacroInvokeSpecial
    :   [\\/;] -> more
    ;

MacroInvokeLeftGroup
    :
    (   '('
    |   '['
    |   '{'
    )   { Group(); } -> channel(PREPROCESSOR)
    ;

MacroInvokeRightGroup
    :
    (   ')'
    |   ']'
    |   '}'
    )   { Ungroup(); } -> channel(PREPROCESSOR)
    ; 

MacroInvokeComma
    :   ',' { IsMacroInvokeArgEnd(); }
    ;

MacroInvokeSubstitution
    :   Substitution -> channel(PREPROCESSOR)
    ;

MacroInvokeString
    :   
    (   '"""' (~'"' | '""')* '"""'
    |   '"' (~["\\\r\n] | StringEscapeSequence)* '"'
    ) -> channel(PREPROCESSOR)
    ;

MacroInvokeChar
    :   ['] (~['\\\r\n] | EscapeSequence) ['] -> channel(PREPROCESSOR)
    ;

MacroInvokeText
    :   ~[,([{)\]}'"\\\r\n/;]+ -> channel(PREPROCESSOR)
    ;

MacroInvokeEscapedNewline
    :   '\\' [ \t]* [\r\n] -> channel(HIDDEN)
    ;

MacroInvokeBackslash
    :   '\\' -> channel(PREPROCESSOR)
    ;

MacroInvokeEnd
    :   
    (   [\r\n]
    |   EOF
    )   -> channel(PREPROCESSOR), popMode
    ;

MacroInvokeBlockComment
    :   Comment -> channel(HIDDEN)
    ;

MacroInvokeLineComment
    :   (';'|'//') ~[\r\n]* -> channel(HIDDEN)
    ;   

mode includeArgMode;

IncludeArgText
    :   '"' (~["\\\r\n] | StringEscapeSequence)* '"' -> channel(INCLUDE_CHANNEL)
    ;

IncludeArgWS
    :   [ \t]+ -> channel(HIDDEN)
    ;

IncludeArgEnd
    :   [\r\n] -> channel(INCLUDE_CHANNEL), popMode
    ;

IncludeArgBlockComment
    :   Comment -> channel(HIDDEN)
    ;

IncludeArgLineComment
    :   (';'|'//') ~[\r\n]* -> channel(HIDDEN)
    ;

IncludeArgBad
    :   . -> channel(ERROR)
    ;

fragment
Comment
    :   '/*' .*? '*/'
    ;

fragment
CommentWS
    :   Comment
    |   [ \t]
    |   '\\' [\r\n]
    ;

fragment
Exponent
    :   'e' [-+]? Integer
    ;

fragment
BinaryExponent
    :   'p' [-+]? Integer
    ;

fragment
StringEscapeSequence
    :   EscapeSequence
    |   '\\' 'U' HexQuad HexQuad
    ;

fragment
EscapeSequence
    :   '\\' ['"\\abfnrtv]
    |   '\\' [0-7] ([0-7] ([0-7])?)?
    |   '\\' 'u' HexQuad
    |   '\\' 'x' HexDigit (HexDigit (HexDigit)?)?
    ;

fragment
HexQuad
    :   HexDigit HexDigit HexDigit HexDigit
    ;

fragment
HexDigit
    :   [0-9a-f]
    ;

fragment
TrailingDigits
    :   ('_'? [0-9])*
    ;

fragment
ParamStrElement
    :   '@{' MacroIdent | ParamNumber '}'
    ;

fragment
NonParamStrElement
    :   ~[@"\\\r\n]
    |   '\\' .
    ;

fragment
Substitution
    :   '\\' (MacroIdent | ParamNumber) 
    ;

fragment
ParamNumber
    :   [1-9] [0-9]*
    ;

fragment
IncludeFilename
    :   '"' (~["\r\n] | '\\"' )* '"'
    ;

fragment
CpuDirective
    :   '.cpu'
    ;

fragment
IncludeDirective
    :   '.' 'b'? 'include'
    ;

fragment 
MacroIdent
    :   Letter (Letter | [0-9_])*
    ;

fragment
Letter
    :   [\p{Letter}] 
    ;
