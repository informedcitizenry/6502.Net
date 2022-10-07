//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

parser grammar Sixty502DotNetParser;

options {
    tokenVocab=Sixty502DotNetLexer;
    superClass=ParserBase;
}

source
    :
    (   Newline+ block 
    |   block
    )   Newline* EOF
    ;

block
    locals [IScope scope = null]
    :   {   SetScope($ctx); }
    stat (eos stat)*
    ;

stat
    locals [int index = 0, IScope scope = null]
    :   
    (   label blockStat
    |   blockStat
    |   label asmStat { CreateLabel($ctx); }
    |   asmStat
    |   labelStat 
    )   {   SetAnnotations($ctx); }
    ;

labelStat
    :
    (   assignExpr 
    |   label op=Equ expr
    |   Ident op=Global expr
    |   label op=Label
    |   label  
    )   {   CreateLabel($ctx); }
    ;

asmStat
    :
    (   cpuStat
        |   pseudoOpStat
        |   directiveStat
    )   {   CheckCodeGenInFunction($ctx); }
    ;

blockStat
    locals [int endDirective = 0]
    :   enterBlock  { SetEndDirective($ctx); } block? exitBlock  
    |   enterEnum { EnterEnum($ctx); } enumDef+ exitEnum
    |   enterIf ifBlock? exitIf   
    |   enterSwitch switchBlock exitSwitch
    ;

enterBlock
    :
    (   (   directive=Block
        |   directive=Do
        |   directive=For induction=assignExpr? Comma cond=expr? Comma assignExprList
        |   directive=Foreach iter=expr Comma seq=expr
        |   directive=Page expr
        |   directive=Proc
        |   directive=Namespace namespace=identifier?
        |   directive=Repeat times=expr
        |   directive=While cond=expr
        )
    |   directive=Function argList? 
    )   eos
    {   EnterBlock($ctx); }
    ;

enterEnum
    :   directive=Enum eos
    ;

enumDef
    :   arg eos { CreateEnum($ctx); }
    ;

enterIf
    :   directive=
    (   If
    |   Ifconst
    |   Ifdef
    |   Ifnconst
    |   Ifndef
    )   expr eos
    ;

ifBlock
    :   block (eos enterElseIf block)* (eos enterElse block)?
    ;   

enterElseIf
    :
    directive=
    (   Elseif
    |   Elseifconst
    |   Elseifdef
    |   Elseifnconst
    |   Elseifndef
    )   expr eos
    ;

enterElse
    :
    directive=Else eos
    ;   

enterSwitch
    :   directive=Switch expr
    {   EnterSwitch(); }
    ;  

switchBlock
    :   caseBlock+
    ;

caseBlock
    :   eos enterCase+ block
    ;

enterCase
    :   
    (   directive=Case expr
    |   directive=Default
    )   eos
    ;

exitBlock
    :   eos label?
    (   directive=
        (   Endblock
        |   Endfunction   
        |   Endnamespace
        |   Endpage
        |   Endproc
        |   Endrepeat
        |   Endwhile
        |   Next
        )
        |
        (   directive=Whiletrue expr )
    )
    {   $ctx.directive.Type == $blockStat::endDirective }?
    {   ExitBlock($ctx); }
    ;

exitEnum
    :   Endenum { ExitEnum($ctx); }
    ;

exitIf
    :   eos Endif
    ;

exitSwitch
    :   eos Endswitch { ExitSwitch(); }
    ;

cpuStat
    :   autoIncIndexedStat
    |   autoDecIndexedStat
    |   indYStat
    |   indXStat
    |   indStat
    |   dirYStat
    |   dirIxStat
    |   dirStat
    |   regListStat
    |   regRegStat
    |   ixStat
    |   blockMoveStat
    |   tstMemoryStat
    |   bitMemoryStat
    |   immStat
    |   pseudoRelStat
    |   relStat
    |   zpAbsStat
    |   implStat
    |   cpuDirectiveStat
    |   z80IndRegExpr
    |   z80IndExpr
    |   z80RegIndExpr
    |   z80RegIx
    |   z80IxReg
    |   z80IxExpr
    |   z80RegExpr
    |   z80RegReg
    |   z80Expr
    |   z80Rel
    |   z80FlagsInstr
    |   z80ImRst
    |   z80Bit
    |   z80Implied
    ;

autoIncIndexedStat
    :   mnemonic LeftSquare autoIncIndexed RightSquare
    |   mnemonic autoIncIndexed
    ;

autoDecIndexedStat
    :   mnemonic LeftSquare autoDecIndexed RightSquare
    |   mnemonic autoDecIndexed
    ;

indStat
    :   mnemonic LeftParen expr RightParen
    ;

indYStat
    :   mnemonic LeftParen expr (Comma innerIndex=(S | SP))? RightParen Comma Newline? outerIndex=(Y | Z)
    ;

indXStat
    :   mnemonic LeftParen expr Comma X RightParen
    ;

dirYStat
    :   mnemonic LeftSquare expr RightSquare Comma Newline? index=(Y | Z)
    ;

dirIxStat
    :   mnemonic bitwidth? LeftSquare expr? Comma index=(PC|PCR|S|U|X|Y) RightSquare
    ;

dirStat
    :   mnemonic LeftSquare expr RightSquare
    ;

regListStat
    :   mnemonic lhs=m6809Reg (Comma Newline? rhs=m6809Reg)+
    ;

regRegStat
    :   mnemonic src=(B|CC|D|PC|S|U|X|Y) (Comma Newline? dst=(A|B|CC|D|DP|PC|S|U|X|Y))?
    |   mnemonic src=A Comma Newline? dst=(PC|S|U|X|Y)
    |   mnemonic LeftSquare src=(A|B|D) Comma dst=(S|U|X|Y) RightSquare
    ;

ixStat
    :   mnemonic bitwidth? expr Comma Newline? index=(PC|PCR|S|U|X|Y)
    ;

blockMoveStat
    :   mnem=(MVN | MVP) expr Comma expr
    ;

bitMemoryStat
    :   bitMem bitExpr=expr Comma Newline? expr (Comma Newline? expr)?
    ;

tstMemoryStat
    :   immStat Comma Newline? expr (Comma Newline? X)?
    |   mnemonic expr Comma Newline? expr Comma Newline? expr
    ;

immStat
    :   mnemonic bitwidth? Hash expr { CheckImm($ctx); }
    ;

pseudoRelStat
    :   pseudoRel expr
    ;

relStat
    :   relative expr
    |   relative16 expr
    ;

zpAbsStat
    :   mnemonic bitwidth? expr
    ;

implStat
    :   mnemonic A?
    ;

cpuDirectiveStat
    :   cpuDirective expr?
    ;

autoIncIndexed
    :   Comma Newline? index=(S|U|X|Y) anonymousLabel?
    ;

autoDecIndexed
    :   Comma Newline? anonymousLabel index=(S|U|X|Y)
    ;

m6809Reg
    :   A | B | CC | D | DP | PC | PCR | S | U | X | Y
    ;

bitMem
    :   BBR | BBS | RMB | SMB
    ;

pseudoRel
    :   JCC | JCS | JEQ | JMI | JNE | JPL | JVC | JVS
    ;

relative16
    :   BRL  | BLT  | BGE  | LBCC | LBCS | LBEQ
    |   LBGE | LBGT | LBHI | LBHS | LBLE | LBLO
    |   LBLS | LBLT | LBMI | LBNE | LBPL | LBRA
    |   LBRN | LBSR | LBVC | LBVS | PER
    ;

relative
    :   BCC | BCS | BEQ | BGT | BHI | BHS | BLE | BLO 
    |   BLS | BLT | BMI | BNE | BPL | BRA | BRN | BSR 
    |   BVC | BVS
    ;

bitwidth
    :   LeftSquare expr RightSquare
    ;

mnemonic
    /* 6502 */
    :   ADC | AND | ASL | BIT | BRK | CLC | CLD | CLI
    |   CLV | CMP | CPX | CPY | DEC | DEX | DEY | EOR
    |   INC | INX | INY | JMP | JSR | LDA | LDX | LDY
    |   LSR | NOP | ORA | PHA | PHP | PLA | PLP | ROL
    |   ROR | RTI | RTS | SBC | SEC | SED | SEI | STA
    |   STX | STY | TAX | TAY | TSX | TXA | TXS | TYA

    /* 6502i */
    |   ANC | ANE | ARR | ASR | DCP | DOP | ISB | JAM
    |   LAS | LAX | RLA | RRA | SAX | SHA | SHX | SHY
    |   SLO | SRE | STP | TAS | TOP

    /* 65C02 */
    |   PHX | PHY | PLX | PLY | STZ | TRB | TSB

    /* 65816 */
    |   COP | JML | JSL | MVN | MVP | PEA | PEI | PHB 
    |   PHD | PHK | PLB | PLD | REP | RTL | SEP | TCD 
    |   TCS | TDC | TSC | TXY | TYX | WDM | XBA | XCE

    /* 65CE02 */
    |   ASW | BSR | CLE | CPZ | DEZ | DEW | INW | INZ
    |   LDZ | PHW | PHZ | PLZ | ROW | RTN | SEE | TAB
    |   TAZ | TBA | TSY | TYS | TZA

    /* 6800 */
    |   ABA  | ADCA | ADCB  | ADDA | ADDB | ANDA
    |   ANDB | ASLA | ASLB  | ASRA | ASRB | BGT
    |   BHI  | BITA | BITB  | BLE  | BLS  | CBA
    |   CLR  | CLRA | CLRB  | CMPA | CMPB | COM
    |   COMA | COMB | COMPA | CPXA | DECA | DECB
    |   DES  | EORA | EORB  | INCA | INCB | INS
    |   LDAA | LDAB | LDS   | LSRA | LSRB | NEGA
    |   NEGB | ORAA | ORAB  | PSHA | PSHB | PULA
    |   PULB | ROLA | ROLB  | RORA | RORB | SBA
    |   SBCA | SBCB | SEV   | STAA | STAB | STS
    |   SUBA | SUBB | SWI   | TAP  | TPA  | TSTA
    |   TSTB

    /* 6809 */
    |   ABX  | ADDD  | ANDCC | BHS   | BLO   | BRN
    |   CMPS | CMPD  | CMPU  | CMPX  | CMPY  | CWAI 
    |   DAA  | EXG   | LBCC  | LBCS  | LBEQ  | LBGE 
    |   LBGT | LBHI  | LBHS  | LBLE  | LBLO  | LBLS 
    |   LBLT | LBMI  | LBNE  | LBPL  | LBRA  | LBRN 
    |   LBSR | LBRSR | LBVC  | LBVS  | LDB   | LDD  
    |   LDU  | LEAS  | LEAU  | LEAX  | LEAY  | LSL  
    |   LSLA | LSLB  | MUL   | NEG   | ORB   | ORCC
    |   PSHS | PSHU  | PULS  | PULU  | SEX   | STB
    |   STD  | STU   | SUBD  | SWI2  | SWI3  | SYNC
    |   TFR

    /* c64dtv2 */
    |   SAC | SIR

    /* HuC6280 */
    |   CLA | CLX | CLY | CSH | CSL | SAY | ST1 | ST2
    |   SXY | TAI | TAM | TDD | TIA | TII | TIN | TMA
    |   TST

    /* m65 */
    |   ADCQ | ANDQ | ASLQ | ASRQ | BITQ | CPQ 
    |   DEQ  | EORQ | INQ  | LDQ  | LSRQ | ORQ
    |   ROLQ | RORQ | SBCQ | STQ

    /* W65C02 */
    |   WAI
    ;

z80Bit
    :   z80BitMnemonic expr Comma Newline? z80IxOffset (Comma z80Reg)?
    |   z80BitMnemonic expr Comma Newline? (z80Reg | (LeftParen HL RightParen))
    ;

z80ImRst
    :   mnem=(IM | RST) expr
    ;

z80RegIx
    :   z80Mnemonic z80Reg Comma Newline? z80IxOffset
    ;

z80IxReg
    :   z80Mnemonic z80IxOffset (Comma Newline? z80Reg)?
    ;

z80IxExpr
    :   z80Mnemonic z80IxOffset Comma Newline? expr
    ;

z80RegIndExpr
    :   z80Mnemonic z80Reg Comma Newline? LeftParen expr RightParen
    ;

z80IndExpr
    :   z80Mnemonic LeftParen expr RightParen (Comma Newline? z80Reg)?
    ;

z80IndRegExpr
    :   z80Mnemonic LeftParen z80Reg RightParen (Comma Newline? expr)?
    ;

z80RegReg
    :   z80Mnemonic reg0=z80Reg Comma Newline? reg1=z80Reg
    |   z80Mnemonic reg0LParen=LeftParen reg0=z80Reg RightParen Comma Newline? reg1=z80Reg
    |   z80Mnemonic reg0=z80Reg Comma Newline? reg1LParen=LeftParen reg1=z80Reg RightParen
    ;

z80RegExpr
    :   z80Mnemonic z80Reg (Comma Newline? expr)?
    ;

z80Expr
    :   z80Mnemonic expr
    ;

z80FlagsInstr
    :   z80Mnemonic flag=(C|M|NC|NZ|P|PO|PE|Z) (Comma Newline? expr)?
    ;

z80Rel
    :   JR (flag=(C|NC|NZ|Z) Comma Newline?)? expr
    |   DJNZ expr
    ;

z80Implied
    :   z80Mnemonic
    ;

z80IxOffset
    :   LeftParen ixReg=(IX|IY) sign=(Plus|Hyphen) expr RightParen
    ;

z80BitMnemonic
    :   BIT | RES | SET
    ;

z80Mnemonic
    /* z80 */
    :   ADC  | ADD  | AND  | CALL | CCF  | CP   | CPD 
    |   CPDR | CPI  | CPIR | CPL  | DAA  | DEC  | DI 
    |   EI   | EX   | EXX  | HALT | IN   | INC  | IND  
    |   INDR | INI  | INIR | JP   | LD   | LDD  | LDDR 
    |   LDI  | LDIR | NOP  | OR   | OTDR | OTIR | OUT
    |   OUTD | OUTI | POP  | PUSH | RET  | RETI | RETN
    |   RL   | RLA  | RLC  | RLCA | RLD  | RR   | RRA
    |   RRC  | RRCA | RRD  | SBC  | SCF  | SLA  | SLL
    |   SRA  | SRL  | SUB  | XOR

    /* i8080 */
    |   ACI  | ADI  | ANA  | ANI  | CC   | CM   | CMA
    |   CMC  | CMP  | CNC  | CNZ  | CPE  | CPO  | CZ
    |   DAD  | DCR  | DCX  | HLT  | INR  | INX  | JC
    |   JM   | JMP  | JNC  | JNZ  | JPE  | JPO  | JZ
    |   LDA  | LDAX | LHLD | LXI  | MOV  | MVI  | NOP
    |   ORA  | ORI  | PCHL | RAL  | RAR  | RC   | RM
    |   RNC  | RNZ  | RP   | RPE  | RPO  | RZ   | SBB
    |   SBI  | SHLD | SPHL | STA  | STAX | STC  | SUI
    |   XCHG | XRA  | XRI  | XTHL
    ;

z80Reg
    :   A   | AF  | ShadowAF | B | BC  | C   | D   | DE 
    |   E   | H   | HL       | I | IX  | IXH | IXL | IY  
    |   IYH | IYL | L        | M | PSW | R   | SP
    ;

cpuDirective
    :   Auto    | Dp     | Manual | M8     | M16
    |   MX8     | MX16   | Tfradp | Tfrbdp | X8
    |   X16
    ;

pseudoOpStat
    :   pseudoOp pseudoOpList
    ;

pseudoOp
    :   directive=
    (   Addr    | Align     | Bankbytes | Binary  | Bstring
    |   Byte    | Cbmflt    | Cbmfltp   | Char    | Cpu
    |   Cstring | Dint      | Dword     | DotEor  | Fill
    |   Hibytes | Hstring   | Hiwords   | Initmem | Lobytes
    |   Lint    | Long      | Lstring   | Lowords | Nstring
    |   Pstring | Rta       | Sbyte     | Short   | Sint
    |   String  | Stringify | Word
    )
    ;

directiveStat
    :   control=Break           { CheckBreak($ctx); }
    |   control=Continue        { CheckContinue($ctx); }
    |   control=Return  expr?   { CheckReturn($ctx); }
    |   control=Goto goto=Ident
    |   Import identifier       { CheckImport($ctx); }
    |   asmDirective 
    ;

asmDirective
    :   directive=
    (   Assert      | BadMacro  | Dsection  | Echo      | Encoding
    |   Endrelocate | Error     | Errorif   | Forcepass | Format
    |   Invoke      | Label     | Let       | Map       | Org     
    |   Proff       | Pron      | Pseudopc  | Realpc    | Relocate    
    |   Section     | Unmap     | Warn      | Warnif 
    )   expressionList?
    ;

label
    :   Ident 
    |   programCounter
    |   anonymousLabel { CreateAnonymousLabel($ctx); }
    ;

eos
    :
    (   Newline
    |   Colon
    )+
    ;

pseudoOpList
    :   pseudoOpArg (Comma Newline* pseudoOpArg)*
    ;

pseudoOpArg
    :   expr | Query
    ;

argList
    :   arg (Comma Newline? arg)*
    ;

arg
    :   assignExpr
    |   name=Ident
    ;

assignExprList
    :   assignExpr (Comma Newline? assignExpr)*
    ;

assignExpr
    :   pc=assignPcOp expr
    |   programCounter assignOp expr
    |   identifier (assignOp | assignPcOp) expr
    |   postfixExpr
    |   prefixExpr
    ;

expressionList
    :   expr (Comma Newline? expr)*
    ;

arrowFunc
    :   LeftParen argList? RightParen Arrow Newline? LeftCurly Newline? {BeginArrow();} block Newline? RightCurly {EndArrow();} 
    |   LeftParen argList? RightParen Arrow Newline? {BeginArrow();} expr {EndArrow();}
    ;

expr
    :   op=(Plus|Hyphen|Bang|Tilde)                                       rhs=expr
    |   lhs=expr op=(Asterisk|Solidus|Percent) Newline?                   rhs=expr
    |   lhs=expr op=(Plus|Hyphen) Newline?                                rhs=expr
    |   lhs=expr op=(LeftShift|RightShift|RightSignShift) Newline?        rhs=expr
    |   lhs=expr op=(LTE|LeftAngle|RightAngle|GTE|Spaceship) Newline?     rhs=expr
    |   lhs=expr op=(BangEqual|DoubleEqual) Newline?                      rhs=expr
    |   lhs=expr op=(TripleEqual|BangDoubleEqual) Newline?                rhs=expr
    |   lhs=expr op=Ampersand Newline?                                    rhs=expr
    |   lhs=expr op=Caret Newline?                                        rhs=expr
    |   lhs=expr op=Pipe Newline?                                         rhs=expr
    |   lhs=expr op=DoubleAmpersand Newline?                              rhs=expr
    |   lhs=expr op=DoublePipe Newline?                                   rhs=expr
    |   lhs=expr op=DoubleCaret Newline?                                  rhs=expr
    |   <assoc=right>cond=expr op=Query Newline? then=expr Colon Newline? els=expr
    |   <assoc=right>op=(LeftAngle|RightAngle|Ampersand|Caret)            rhs=expr
    |   assignExpr
    |   refExpr
    |   designator
    |   grouped
    |   primaryExpr
    ;

primaryExpr
    locals [Value value = Value.Undefined]
    :
    (   constExpr=HexadecimalDouble
    |   constExpr=BinaryLiteralDouble
    |   constExpr=OctalDouble
    |   constExpr=Double
    |   constExpr=Hexadecimal
    |   constExpr=Integer
    |   constExpr=Octal
    |   constExpr=BinaryLiteral
    |   constExpr=AltBinary
    |   constExpr=CharLiteral
    |   constExpr=StringLiteral
    )   {  SetPrimaryExprValue($ctx); }
    ;

prefixExpr
    :   op=BackwardReference (identifier | programCounter)
    |   op=ForwardReference  (identifier | programCounter)
    ;


postfixExpr
    :   (identifier | programCounter) op=BackwardReference
    |   (identifier | programCounter) op=ForwardReference
    ;

designator
    :   designator (LeftSquare range RightSquare)+
    |   arrowFunc (LeftParen expressionList? RightParen)?
    |   array
    |   dictionary
    |   StringLiteral LeftSquare range RightSquare
    ;

refExpr
    :   identifier
    |   programCounter
    |   anonymousLabel
    ;

identifier
    :   lhs=identifier (op=LeftSquare range RightSquare)+
    |   lhs=identifier op=LeftParen expressionList? RightParen
    |   lhs=identifier op=Dot rhs=identifier
    |   name=Ident
    ;

programCounter
    :   Asterisk
    ;

anonymousLabel
    :   ForwardReference
    |   Plus
    |   BackwardReference
    |   Hyphen
    ;

array
    :   LeftSquare expressionList Comma? RightSquare
    ;

dictionary
    :   LeftCurly keyValuePair (Comma keyValuePair)* Comma? RightCurly
    ;

grouped
    :   LeftParen expr RightParen
    ;

keyValuePair
    :   key=expr Colon val=expr
    ;

range
    :   startIndex=expr (DoubleDot endIndex=expr?)?
    |   startIndex=expr? DoubleDot endIndex=expr
    ;

assignOp
    :
    (   Equal
    |   ColonEqual
    |   SolidusEqual
    |   PlusEqual
    |   HyphenEqual
    |   PercentEqual
    |   LeftShiftEqual
    |   RightShiftEqual
    |   RightSignShiftEq
    |   AmpersandEqual
    |   CaretEqual
    |   PipeEqual
    )   Newline?
    ;

assignPcOp
    :   AsteriskEq Newline?
    ;