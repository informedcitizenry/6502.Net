//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

parser grammar SyntaxParser;

options {
    tokenVocab = SyntaxLexer;
    superClass = ParserBase;
}

cpuStat
    :   NL* Cpu StringLiteral eos
    ;

preprocess
    :  
    (   { StartsAtNewline() }? preprocStat preprocEos
    |   NL
    |   nonPreProc
    )*  EOF
    ;

preprocEos
    :   NL
    |   EOF
    ;

preprocStat
    :   ident d=Macro macroParam? begin=NL+ ~(Endmacro)* end=NL+ label? Endmacro
    |   label? d=DotIdentifier macroParam?
    |   label? d=(Include | Binclude) filename=(StringLiteral | UnicodeStringLiteral)
    |   label? d=End
    ;

nonPreProc
    :   ~(Include|Binclude|Macro|End|NL|EOF)
    ; 

macroParam
    :   ~(NL|EOF)+
    ;

program
    :   block? EOF
    ;

block
    :   (stat | NL)+
    ;

stat
    :   ident equ=(Equ | Equal | Global) expr eos               # StatConstant
    |   ident Function argList? eos block? Endfunction eos      # StatFuncDecl
    |   ident Enum eos enumDef+ Endenum eos                     # StatEnumDecl
    |   name=label? b=Block eos block? end=label? Endblock eos  # StatBlock
    |   name=label? b=Proc eos block? end=label? Endproc eos    # StatBlock
    |   label? instruction eos                                  # StatInstruction
    |   label eos                                               # StatLabel
    |   expr eos                                                # StatExpr
    ;

enumDef
    :   ident ('=' primaryExpr)? eos
    ;

instruction
    :   cpuInstruction                                                                  # InstructionCpu
    |   directive exprList?                                                             # InstructionDirective
    |   pseudoOp pseudoOpArgList                                                        # InstructionPseudoOp
    |   Do eos block? label? Whiletrue while=expr                                       # InstructionWhileLoop
    |   ifBlock                                                                         # InstructionIf
    |   For init=expr? ',' NL* cond=expr? ',' NL* inc=exprList eos block? label? Next   # InstructionFor
    |   Foreach iterator=ident ',' NL* collection=expr eos block? label? Next           # InstructionForeach
    |   Namespace root=ident identifierPart* eos block? label? Endnamespace             # InstructionNamespace
    |   Page eos block? label? Endpage                                                  # InstructionPage
    |   Repeat repetition=expr eos block? label? Endrepeat                              # InstructionRepeat
    |   Switch expr eos caseBlock* Endswitch                                            # InstructionSwitch
    |   While while=expr eos block? label? Endwhile                                     # InstructionWhileLoop
    ;

label
    locals [bool visited = false]
    :   ident 
    |   '+'
    |   '-'
    ;

caseBlock
    :   ((Case expr | Default) eos)+ block
    ;

ifBlock
    :   (If | Ifconst | Ifdef | Ifnconst | Ifndef) expr eos block? 
        ((Elseif | Elseifconst | Elseifdef | Elseifnconst | Elseifndef) expr eos block?)*
        (Else eos block?)? Endif
    ;

eos
    :   NL+
    |   ':'
    |   EOF
    ;

cpuInstruction
    locals [int opcode = -1,
            int opcodeSize = 0,
            long operand = 0,
            int operandSize = 0]
    :   bitMnemonic DecLiteral ',' NL* z80Index (',' NL* register)?     # CpuInstructionBit
    |   bitMnemonic DecLiteral ',' NL* '(' register ')'                 # CpuInstructionBit
    |   bitMnemonic DecLiteral ',' NL* register                         # CpuInstructionBit
    |   bitMnemonic DecLiteral ',' NL* expr (',' NL* expr)?             # CpuInstructionBit
    |   mnemonic bitwidthModifier? '#' imm=expr                         # CpuInstructionImmmediate
    |   mnemonic '#' imm=expr ',' NL* expr (',' NL* X)?                 # CpuInstructionImmmediate
    |   LD gb0=gb80Index ',' NL* a1=A                                   # CpuInstructionGB80Index
    |   LD a0=A ',' NL* gb1=gb80Index                                   # CpuInstructionGB80Index
    |   mnemonic ix0=z80Index (',' NL* (r1=register | e1=expr))?        # CpuInstructionZ80Index
    |   mnemonic r0=register ',' NL* ix1=z80Index                       # CpuInstructionZ80Index
    |   mnemonic bitwidthModifier? '(' expr ',' X ')'                   # CpuInstructionIndexedIndirect
    |   mnemonic '(' expr (',' ix0=(S | SP))? ')' ',' NL* ix1=(Y | Z)   # CpuInstructionIndirectIndexed
    |   LD HL ',' NL* SP ('+'|'-') NL* expr                             # CpuInstructionGB80StackOffset
    |   LD a0=A ',' NL* '(' HL inc=('-' | '+') ')'                      # CpuInstructionGB80AccIncrement
    |   LD '(' HL inc=('-' | '+') ')' ',' NL* a1=A                      # CpuInstructionGB80AccIncrement
    |   mnemonic register (',' NL* register)*                           # CpuInstructionRegisterList
    |   mnemonic '(' ind=register ')' (',' NL* (register | expr))?      # CpuInstructionIndirectRegisterFirst
    |   mnemonic (register | expr) ',' NL* '(' ind=register ')'         # CpuInstructionIndirectRegisterSecond
    |   mnemonic register ',' NL* '(' expr ')'                          # CpuInstructionIndirectExpressionSecond
    |   mnemonic register ',' NL* expr                                  # CpuInstructionZ80Immediate
    |   mnemonic '(' expr ')' ',' NL* register                          # CpuInstructionZ80IndirectIndexed
    |   mnemonic '[' expr ']' ',' NL* reg=(Y | Z)                       # CpuInstructionDirectIndex
    |   mnemonic '[' expr ']'                                           # CpuInstructionDirect
    |   mnemonic bitwidthModifier? expr ',' NL* register                # CpuInstructionIndex
    |   mnemonic '[' ',' inc='--' reg=register ']'                      # CpuInstructionAutoIncrement
    |   mnemonic '[' ',' reg=register inc='++' ']'                      # CpuInstructionAutoIncrement
    |   mnemonic ',' NL* inc=('-' | '--') reg=register                  # CpuInstructionAutoIncrement
    |   mnemonic ',' NL* reg=register inc=('+' | '++')                  # CpuInstructionAutoIncrement
    |   mnemonic '[' acc=(A | B | D)? ',' ix=register ']'               # CpuInstructionRegisterOffset
    |   mnemonic '[' expr ',' register ']'                              # CpuInstructionIndirectIndexM6809
    |   mnemonic ',' NL* ix=register                                    # CpuInstructionRegisterOffset
    |   mnemonic exprList                                               # CpuInstructionExpressionList
    |   mnemonic bitwidthModifier? expr                                 # CpuInstructionZPAbsolute
    |   mnemonic                                                        # CpuInstructionImplied
    ;

bitMnemonic
    :   BBR | BBS | RMB | SMB | BITZ | RES | SET
    ;

mnemonic
    /* 6502 */
    :   ADC | AND | ASL | BIT | BRK | CLC | CLD | CLI
    |   CLV | CMP | CPX | CPY | DEC | DEX | DEY | EOR
    |   INC | INX | INY | JMP | JSR | LDA | LDX | LDY
    |   LSR | NOP | ORA | PHA | PHP | PLA | PLP | ROL
    |   ROR | RTI | RTS | SBC | SEC | SED | SEI | STA
    |   STX | STY | TAX | TAY | TSX | TXA | TXS | TYA
    |   BCC | BCS | BEQ | BNE | BMI | BPL | BVC | BVS
    /* 6502i */
    |   ANC | ANE | ARR | ASR | DCP | DOP | ISB | JAM
    |   LAS | LAX | RLA | RRA | SAX | SHA | SHX | SHY
    |   SLO | SRE | STP | TAS | TOP
    /* 65C02 */
    |   BRA | PHX | PHY | PLX | PLY | STZ | TRB | TSB
    /* 65816 */
    |   BRL | COP | JML | JSL | MVN | MVP | PEA | PEI
    |   PER | PHB | PHD | PHK | PLB | PLD | REP | RTL
    |   SEP | TCD | TCS | TDC | TSC | TXY | TYX | WDM
    |   XBA | XCE
    /* 65CE02 */
    |   ASW | BGE | BLT | BSR | CLE | CPZ | DEZ | DEW 
    |   INW | INZ | LDZ | PHW | PHZ | PLZ | ROW | RTN
    |   SEE | TAB | TAZ | TBA | TSY | TYS | TZA
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
    |   TFR  | Tfradp| Tfrbdp
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
    /* pseudo 6502 */
    |   JCC | JCS | JEQ | JMI | JNE | JPL | JVC | JVS
    /* W65C02 */
    |   WAI
     /* z80 */
    |   ADD  | CALL | CCF  | CP   | CPD  | CPDR | CPI  
    |   CPIR | CPL  | DAA  | DI   | DJNZ | EI   | EX  
    |   EXX  | HALT | IM   | IN   | IND  | INDR | INI  
    |   INIR | JP   | JR   | LD   | LDD  | LDDR | LDI  
    |   LDIR | OR   | OTDR | OTIR | OUT  | OUTD | OUTI 
    |   POP  | PUSH | RET  | RETI | RETN | RL   | RLA 
    |   RLC  | RLCA | RLD  | RR   | RRA  | RRC  | RRCA 
    |   RRD  | RST  | SCF  | SLA  | SLL  | SRA  | SRL  
    |   SUB  | XOR 
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
    /* GB80 */
    |   STOP | SWAP
    ;

pseudoOp
    :   Addr    | Bankbytes | Bstring   | Byte      | Cbmflt
    |   Cbmfltp | Char      | Cstring   | Dint      | Dword      
    |   Hibytes | Hstring   | Hiwords   | Lint      | Long
    |   Lstring | Lobytes   | Lowords   | Nstring   | Pstring   
    |   Rta     | Sbyte     | Short     | Sint      | String
    |   Word
    ;

directive
    :   Align       | Assert    | Auto      | Binary
    |   Break       | Continue  | Cpu       | DotEor     
    |   Dp          | Dsection  | Echo      | Encoding
    |   Endrelocate | Error     | Errorif   | Fill
    |   Forcepass   | Format    | Goto      | Import
    |   Initmem     | Invoke    | Label     | Let
    |   M8          | M16       | Manual    | Map
    |   MX8         | MX16      | Org       | Proff
    |   Pron        | Pseudopc  | Realpc    | Relocate
    |   Return      | Section   | Stringify | Unmap
    |   Warn        | Warnif    | X8        | X16
    ;

z80Index
    :   '(' (IX | IY) ('+' | '-') expr ')'
    ;

gb80Index
    :   '(' io=expr '+' reg=C ')'
    ;

bitwidthModifier
    :   '[' DecLiteral ']'
    ;

register
    :   A  | AF | ShadowAF | B   | BC  | C   | CC  | D   | DE  | DP | E 
    |   H  | HL | L   | I  | IX  | IXH | IXL | IY  | IYH | IYL | M  | N  
    |   NC | NZ | P   | PC | PCR | PE  | PO  | PSW | R   | S   | SP | U 
    |   X  | Y  | Z
    ;

argList
    :   argList ',' NL* defaultArgList
    |   defaultArgList
    |   ident (',' NL* ident)*
    ;

ident
    :   Identifier
    |   registerAsIdentifier
    ;

defaultArgList
    :   ident '=' expr (',' NL* ident '=' expr)*
    ;

pseudoOpArgList
    :   pseudoOpArg (',' NL* pseudoOpArg)*
    ;

pseudoOpArg
    :   expr | '?'
    ;

exprList
    :   expr (',' NL* expr)*
    ;

defineAssign:   ident ('=' expr)?;
defineSection:  ident ',' NL* start=expr (',' NL* end=expr)?;

expr
    locals [ValueBase value = new UndefinedValue()]
    :   arrow                                                           # ExpressionArrow
    |   target=expr identifierPart                                      # ExpressionDotMember
    |   target=expr '[' (ix=expr | range) ']'                           # ExpressionSubscript
    |   target=expr '(' exprList? ')'                                   # ExpressionCall
    |   expr postfix=('++' | '--')                                      # ExpressionIncDec
    |   ('++' | '--') expr                                              # ExpressionIncDec
    |   unary_op=('+' | '-' | '~' | '!') expr                           # ExpressionUnary
    |   <assoc=right> lhs=expr op='^^' rhs=expr                         # ExpressionNumericBinary
    |   lhs=expr op=('*' | '%' | '/') NL* rhs=expr                      # ExpressionNumericBinary
    |   lhs=expr op=('+' | '-') NL* rhs=expr                            # ExpressionAdditive
    |   lhs=expr op=('<<' | '>>' | '>>>') NL* rhs=expr                  # ExpressionNumericBinary
    |   lhs=expr op=('<' | '<=' | '>=' | '>' | '<=>') NL* rhs=expr      # ExpressionBooleanBinary
    |   lhs=expr op=('===' | '!==' | '==' | '!=') NL* rhs=expr          # ExpressionBooleanBinary
    |   lhs=expr op='&' NL* rhs=expr                                    # ExpressionNumericBinary
    |   lhs=expr op='^' NL* rhs=expr                                    # ExpressionNumericBinary
    |   lhs=expr op='|' NL* rhs=expr                                    # ExpressionNumericBinary
    |   lhs=expr op='&&' NL* rhs=expr                                   # ExpressionBooleanBinary
    |   lhs=expr op='||' NL* rhs=expr                                   # ExpressionBooleanBinary
    |   <assoc=right> cond=expr '?' NL* then=expr NL* ':' NL* els=expr  # ExpressionConditional
    |   <assoc=right> lhs=expr assignOp rhs=expr                        # ExpressionAssignment
    |   unary_op=('<' | '>' | '&' | '^' | '^^') expr                    # ExpressionUnary
    |   Identifier                                                      # ExpressionSimpleIdentifier
    |   registerAsIdentifier                                            # ExpressionSimpleIdentifier
    |   primaryExpr                                                     # ExpressionPrimary
    |   '*'                                                             # ExpressionProgramCounter
    |   anonymousLabel                                                  # ExpressionAnonymousLabel
    |   stringLiteral                                                   # ExpressionStringLiteral
    |   array                                                           # ExpressionCollection
    |   dictionary                                                      # ExpressionDictionary
    |   tuple                                                           # ExpressionCollection
    |   '(' expr ')'                                                    # ExpressionGrouped
    ;

identifierPart
    :   '.' NL* ident
    |   DotIdentifier
    |   pseudoOp
    |   directive
    ;

registerAsIdentifier
    :   A | AF | B   | BC  | C   | CC  | D   | DE  | DP | E | H  | HL  
    |   L | I  | IX  | IXH | IXL | IY  | IYH | IYL | M  | N | NC | NZ 
    |   P | PC | PCR | PE  | PO  | PSW | R   | S   | SP | U | X  | Y  
    |   Z  
    ;

primaryExpr
    :   AltBinLiteral
    |   BinFloatLiteral | BinLiteral
    |   CbmScreenCharLiteral
    |   CharLiteral
    |   DecFloatLiteral | DecLiteral
    |   HexFloatLiteral | HexLiteral
    |   OctFloatLiteral | OctLiteral
    |   PetsciiCharLiteral
    |   True | False | NaN
    ;

arrow
    :   '(' argList? ')' '=>'
    (   LeftCurly NL* block? RightCurly
    |   expr
    )
    ;

anonymousLabel
    :   MultiPlus
    |   '++'
    |   '+'
    |   MultiHyphen
    |   '--'
    |   '-'
    ;
    
array
    :   '[' exprList ','? ']'
    ;

dictionary
    :   LeftCurly NL* keyValuePair (',' NL* keyValuePair)* (',' NL*)? RightCurly
    ;

keyValuePair
    :   key NL* ':' NL* val=expr NL*
    ;

key
    :   primaryExpr
    |   identifierPart
    |   CbmScreenStringLiteral
    |   PetsciiStringLiteral
    |   StringLiteral
    |   UnicodeStringLiteral
    ;

tuple
    :   '(' expr ',' expr (',' expr)* ','? ')'
    ;

stringLiteral
    :   interpolString
    |   CbmScreenStringLiteral
    |   PetsciiStringLiteral
    |   StringLiteral
    |   UnicodeStringLiteral
    ;

interpolString
    :
    (   DoubleQuote interpolText+ DoubleQuote )
    |
    (   MDoubleQuote mInterpolText+ MDoubleQuote )
    ;

interpolText
    :   IText
    |   InterpolStart interpolExpr RightCurly
    ;

mInterpolText
    :   MSText
    |   MSInterpolStart interpolExpr RightCurly
    ;

interpolExpr
    :   NL* expr formatSpecifier? NL*
    ;

formatSpecifier
    :   (',' NL* expr)? ':' (Identifier | C | D | E | P | R | X)
    |   ',' NL* expr
    ;

range
    :   start=expr  '..' ('^'? end=expr)?
    |   start=expr? '..' '^'? end=expr
    ;

assignOp
    :   '='    | ':=' | '|=' | '^=' | '&=' | '<<=' | '>>='
    |   '>>>=' | '+=' | '-=' | '/=' | '%=' | '*='
    ;
