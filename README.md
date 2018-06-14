# 6502.Net, A Simple .Net-Based 6502/65C02/W65C816S Cross-Assembler
### Version 1.12.0.1
## Introduction
The 6502.Net Macro Assembler is a simple cross-assembler targeting the MOS 6502, WDC 65C02, WDC 65C816 and related CPU architectures. It is written for .Net (Version 4.5.1). It can assemble both legal (published) and illegal (undocumented) 6502 instructions, as well instructions from its successors the 65C02 and 65C816.

The 6502 was a popular choice for video game system and microcomputer manufacturers in the 1970s and mid-1980s, due to its cost and efficient design. Among hobbyists and embedded systems manufacturers today it still sees its share of use. For more information, see the [wiki entry](https://en.wikipedia.org/wiki/MOS_Technology_6502) or [6502 resource page](http://6502.org) to learn more about this microprocessor.

The 65C02 is an enhancement to the 6502, offering some improvements, including unconditional relative branching and a fix to the infamous "indirect jump page wrap" defect. It was notable in the market as the brains behind the Apple *II*e and Apple IIc home computers, as well as the NEC TurboGrafx-16/PC Engine game system.

The W65C816S (or 65816 for short), is a true successor to the 6502, a fully backward compatible 16-bit CPU. It is mostly known for powering the Apple IIgs and the Super Nintendo game console.  
## Legal
* 6502.Net (c) 2017, 2018 informedcitizenry
* System.CommandLine, a [command-line argument parser](https://github.com/dotnet/corefxlab/tree/master/archived_projects/src/System.CommandLine) (c) Microsoft Corporation

See LICENSE and LICENSE_third_party for licensing information.
## Overview
The 6502.Net assembler is simple to use. Invoke it from a command line with the assembly source and (optionally) the output filename in the parameters. For instance, a `/6502.Net.exe myprg.asm` command will output assembly listing in `myprgm.asm` to binary output. To specify output file name use the `-o <file>` or `--output=<file>` option, otherwise the default output filename will be `a.out`.

You can specify as many source files as assembly input as needed. For instance, `6502.Net.exe mylib.asm myprg.asm` will assemble both the `mylib.asm` and `myprgm.asm` files sequentially to output. Be aware that if both files define the same symbol an assembler error will result.
## General Features
### Numeric constants
Integral constants can be expressed as decimal, hexadecimal, and binary. Decimal numbers are written as is, while hex numbers are prefixed with a `$` and binary numbers are prefixed with a `%`.
```
            65490 = 65490
            $ffd2 = 65490
%1111111111010010 = 65490
```
Negative numbers are assembled according to two's complement rules, with the highest bits set. Binary strings can alternatively be expressed as `.` for `0` and `#` for `1`, which is helpful for laying out pixel data:
```
number1     .byte %...###..
            .byte %..####..
            .byte %.#####..
            .byte %...###..
            .byte %...###..
            .byte %...###..
            .byte %...###..
            .byte %.#######
```                
### Labels, Symbols and Variables
When writing assembly code, hand-coding branches, addresses and constants can be time-consuming and lead to errors. Labels take care of this work for you! There is no restriction on name size, but all labels must begin with an underscore or letter, and can only contain underscores, letters, and digits, and they cannot be re-assigned:
```
            black   =   0

            lda #black      ; load black into acc.
            beq setborder   ; now set the border color
            ...
setborder:  sta $d020       ; poke border color with acc.
```
Trailing colons for jump instructions are optional.

Using the `.block`/`.endblock` directives, labels can be placed in scope blocks to avoid the problem of label reduplication:
```
            ...
endloop     lda #$ff    
            rts

myblock     .block
            jsr endloop     ; accumulator will be 0
            ...             ; since endloop is local to myblock
endloop     lda #0
            rts
            .endblock
```
Labels inside named scopes can be referenced with dot notation:
```
kernal      .block

chrin       = $ffcf
chrout      = $ffd2

            .endblock

            jsr kernal.chrout   ; call the subroutine whose label        
                                ; is defined in the kernal block
```
Blocks can also be nested. Labels in unnamed blocks are only visible in their own block, and are unavailable outside:
```
            .block
            jsr increment
            ...
increment   inc mem
            beq done
            inc mem+1
done        rts
            .endblock

            jsr increment ; will produce an assembler error
```
Anonymous labels allow one to do away with the need to think of unique label names altogether. There are two types of anonymous labels: forward and backward. Forward anonymous labels are declared with a `+`, while backward anonymous labels are declared using a `-`. They are forward or backward to the current assembly line and are referenced in the operand with one or more `+` or `-` symbols:
```
printmessage    
            ldx #0
-           lda msg_ptr,x
            beq +               ; jump to first forward anonymous from here
            jsr chrout
            inx
            bne -               ; jump to first backward anonymous from here
+           rts
-           nop
            jmp --              ; jump to the second backward anonymous from here
```
As you can see anonymous labels, though convenient, would hinder readability if used too liberally. They are best for small branch jumps, though can be used in expressions:
```
-           .byte $01, $02, $03
            lda (-),x           ; put anonymous label reference inside paranetheses.
```            
Label values are defined at first reference and cannot be changed. An alternative to labels are variables. Variables, like labels, are named references to values in operand expressions, but can be changed as often as required. A variable is declared with the `.let` directive, followed by an assignment expression. Variables and labels cannot share the same symbol name.
```
            .let myvar = 34
            lda #myvar
            .let myvar = myvar + 1
            ldx #myvar
```
Unlike labels, variables cannot be referenced in other expressions before they are declared, since variables are not preserved between passes.
```
            .let y = x  
            .let x = 3
```
In the above example, the assembler would error assuming `x` has never been declared before.
### Comments
Adding comments to source promotes readability, particularly in assembly. Comments can be added to source code in one of two ways, as single-line trailing source code, or as a block. Single-line comments start with a semi-colon. Any text written after the semi-colon is ignored, unless it is being expressed as a string or constant character.
```
            lda #0      ; 0 = color black
            sta $d020   ; set border color to accumulator
            lda #';'    ; the first semi-colon is a char literal so will be assembled
            jsr $ffd2   
```
Block comments span multiple lines, enclosed in `.comment` and `.endcomment` directives. These are useful when you want to exclude unwanted code:
```
            .comment

            this will set the cpu on fire do not assemble!

            lda #$ff
            sta $5231

            .endcomment
```
### Non-code (data) assembly
In addition to 6502 assembly, data can also be assembled. Expressions evaluate internally as 64-bit signed integers, but **must** fit to match the expected operand size; if the value given in the expression exceeds the data size, this will cause an illegal quantity error. The following pseudo-ops are available:

| Directive | Size                      |
| --------- | ------------------------- |
| `.byte`   | One byte unsigned         |
| `.sbyte`  | One byte signed           |
| `.addr`   | Two byte address          |
| `.sint`   | Two bytes signed          |
| `.word`   | Two bytes unsigned        |
| `.rta`    | Two byte return address   |
| `.lint`   | Three bytes signed        |
| `.long`   | Three bytes unsigned      |
| `.dint`   | Four bytes signed         |
| `.dword`  | Four bytes unsigned       |
| `.align`  | Zero or more bytes        |
| `.fill`   | One or more bytes         |   

Multi-byte directives assemble in little-endian order (the least significant byte first), which conforms to the 6502 architecture. Data is comma-separated, and each value can be a constant or expression:
```
sprite      .byte %......##,%########,%##......
jump        .word sub1, sub2, sub3, sub4
```
The `.addr` and `.rta` directives are the same as `.word`, but `.rta` is the expression minus one. This is useful for doing an "rts jump":
```
            lda #>jump  ; high byte ($07)
            pha
            lda #<jump  ; low byte ($ff)
            pha
            rts         ; do the jump
jump        .rta $0800  ; = $07ff
```
For `.fill` and `.align`, the assembler accepts either one or two arguments. The first is the quantity, while the second is the value. If the second is not given then it is assumed to be uninitialized data (see below). For `.fill`, quantity is number of bytes, for `.align` it is the number of bytes by which the program counter can be divided with no remainder:
```
unused      .fill 256,0 ; Assemble 256 bytes with the value 0

atpage      .align 256  ; The program counter is guaranteed to be at a page boundary
```
Sometimes it is desirable to direct the assembler to make a label reference an address, but without assembling bytes at that address. For instance, for program variables. Use the `?` instead of an expression:
```
highscore   .dword ?    ; set the symbol highscore to the program counter,
                        ; but do not output any bytes
```                             
Note that if uninitialized data is defined, but thereafter initialized data is defined, the output will fill bytes to the program counter from the occurrence of the uninitialized symbol:
```
highscore   .dword ?    ; uninitialized highscore variables
            lda #0      ; The output is now 6 bytes in size
```
### Text processing and encoding
#### Psuedo Ops
In addition to integral values, 6502.Net can assemble Unicode text. Text strings are enclosed in double quotes, character literals in single quotes.

Strings can be assembled in a few different ways, according to the needs of the programmer.

| Directive     | Meaning                                                                       |
| ------------- | ----------------------------------------------------------------------------- |
| `.string`     | A standard string literal                                                     |
| `.cstring`    | A C-style null-terminated string                                              |
| `.lsstring`   | A string with output bytes left-shifted and the low bit set on its final byte |
| `.nstring`    | A string with the negative (high) bit set on its final byte                   |
| `.pstring`    | A Pascal-style string, its size in the first byte                             |

Since `.pstring` strings use a single byte to denote size, no string can be greater than 255 bytes. Since `.nstring` and `.lsstring` make use of the high and low bits, bytes must not be greater in value than 127, nor less than 0.
#### String Functions
There are two special string functions. The first, `str()`, will convert an integral value to its equivalent in bytes:
```
start       = $c000

startstr    .string str(start) ; assembles as $34,$39,$31,$35,$32
                               ; literally the digits "4","9","1","5","2"
```      
The `format()` function allows you to convert non-string data to string data using a .Net format string:
```
stdout      = $ffd2
stdstring   .string format("The stdout routine is at ${0:X4}", stdout)
            ;; will assemble to:
            ;; "The stdout routine is at $FFD2

```
#### Encodings
Assembly source text is processed as UTF-8, and by default strings and character literals are encoded as such. You can change how text output with the `.encoding` and `.map` directives. Use `.encoding` to select an encoding, either pre-defined or custom. The encoding name follows the same rules as labels. There are four pre-defined encodings:

| Encoding      | Output bytes       |       
| ------------- |--------------------|
| `none`        | UTF-8              |
| `atascreen`   | Atari screen codes |
| `cbmscreen`   | CBM screen codes   |
| `petscii`     | CBM PETSCII        |

The default encoding is `none`. It is worth noting that, for the Commodore-specific encodings, several of the glyphs in those platforms can be represented in Unicode counterparts. For instance, for Petscii encoding, ♥ outputs to `D3` as is expected.

Text encodings are modified using the `.map` and `.unmap` directives. After selecting an encoding, you can map a Unicode character to a custom output code as follows:
```
            ;; select encoding
            .encoding myencoding

            ;; map A to output 0
            .map "A", 0

            .string "ABC"
            ;; > 00 42 43

            ;; char literals are also affected
            lda #'A'    ;; a9 00

            ;; you can use emoji too!
            .string "😁�"    ;; f0 9f 98 81
```
The output can be one to four bytes. Entire character sets can also be mapped, with the re-mapped code treated as the first in the output range. The start and endpoints in the character set to be re-mapped can either be expressed as a two-character string literal or as expressions.
```
            ;; output lower-case chars as uppercase
            .map "az", "A"

            ;; output digits as actual integral values
            .map "0","9", 0

            ;; alternatively:
            .map 48, 48+9, 0

            ;; escape sequences are acceptable too:
            .map "\u21d4", $9f
```
**Caution:** Operand expressions containing a character literal mapped to a custom code will evaluate the character literal accordingly. This may produce unexpected results:
```
            .map 'A', 'a'

            .map 'a', 'A' ;; this is now the same as .map 'a', 'a'
```
Instead express character literals as one-character strings in double-quotes, which will resolve to UTF-8 values.

A further note about encodings and source files. As mentioned, source files are read and processed as UTF-8. While it is true that the .Net StreamReader class can auto-detect other encodings, this cannot be guaranteed (for instance if the BOM is lacking in a UTF-16-encoded source). If the source does not assemble as expected, consider converting it to UTF-8 or at least ASCII. [This article](https://www.joelonsoftware.com/2003/10/08/the-absolute-minimum-every-software-developer-absolutely-positively-must-know-about-unicode-and-character-sets-no-excuses/) offers a good overview on the issues concerning text encodings.

#### Escape sequences

All [.Net escape sequences](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences) will also output, including Unicode.

```
            .string "He said, \"How are you?\""
            .byte '\t', '\''
```

Here are a few recognized escape sequences:

| Escape Sequence | ASCII/Unicode Representation |
| --------------- | ---------------------------- |
| `\n`            | Newline                      |
| `\r`            | Carriage return              |
| `\t`            | Tab                          |
| `\"`            | Double quotation mark        |
| `\unnnn`        | Unicode U+nnnn               |

### File inclusions

Other files can be included in final assembly, either as 6502.Net-compatible source or as raw binary. Source files are included using the `.include` and `.binclude` directives. This is useful for libraries or other organized source you would not want to include in your main source file. The operand is the file name (and path) enclosed in quotes. `.include` simply inserts the source at the directive.
```
            ;; inside "../lib/library.s"

            .macro  inc16 mem
            inc \mem
            bne +
            inc \mem+1
+           .endmacro
            ...
```
This file called `"library.s"` inside the path `../lib` contains a macro definition called `inc16` (See the [section below](#macros-and-segments) for more information about macros).
```
            .include "../lib/library.s"

            .inc16 $033c    ; 16-bit increment value at $033c and $033d
```
If the included library file also contained its own symbols, caution would be required to ensure no symbol clashes. An alternative to `.include` is `.binclude`, which resolves this problem by enclosing the included source in its own scoped block.
```
lib         .binclude "../lib/library.s"    ; all symbols in "library.s"
                                        ; are in the "lib" scope

            jsr lib.memcopy
```
If no label is prefixed to the `.binclude` directive then the block is anonymous and labels are not visible to your code.

External files containing raw binary that will be needed to be included in your final output, such as `.sid` files or sprite data, can be assembled using the `.binary` directive.
```
            * = $1000

            .binary "../rsrc/sprites.raw"

            ...

            lda #64     ; pointer to first sprite in "./rsrc/sprites.raw"
            sta 2040    ; set first sprite to that sprite shape
```
You can also control how the binary will be included by specifying the offset (number of bytes from the start) and size to include.
```
            * = $1000

            .binary "../rsrc/music.sid", $7e    ; skip first 126 bytes
                                                ; (SID header)

            .binary "../lib/compiledlib.bin", 2, 256    ; skip load header
                                                        ; and take 256 bytes
```

### Mathematical and Conditional Expressions

All non-string operands are treated as math or conditional expressions. Compound expressions are nested in paranetheses. There are several available operators for both binary and unary expressions.

#### Binary Operations

| Operator      | Meaning                        |
| :-----------: | ------------------------------ |
| +             | Add                            |
| -             | Subtract                       |
| *             | Multiply                       |
| /             | Divide                         |
| %             | Modulo (remainder)             |
| **            | Raise to the power of          |
| &             | Bitwise AND                    |
| &#124;        | Bitwise OR                     |
| ^             | Bitwise XOR                    |
| <<            | Bitwise left shift             |
| >>            | Bitwise right shift            |
| <             | Less than                      |
| <=            | Less than or equal to          |
| ==            | Equal to                       |
| !=            | Not equal to                   |
| >=            | Greater than or equal to       |
| >             | Greater than                   |
| &&            | Logical AND                    |
| &#124;&#124;  | Logical OR                     |
```
            .addr   HIGHSCORE + 3 * 2 ; the third address from HIGHSCORE
            .byte   * > $f000         ; if program counter > $f000, assemble as 1
                                      ; else 0

            ;; bounds check START_ADDR                          
            .assert START_ADDR >= MIN && START_ADDR <= MAX
```
#### Unary Operations
| Operator      | Meaning                        |
| :-----------: | ------------------------------ |
| ~             | Bitwise complementary          |
| <             | Least significant byte         |
| >             | Most significant (second) byte |
| &             | Word (first two bytes) value   |
| ^             | Bank (third) byte              |
| !             | Logical NOT                    |
```

            lda #>routine-1     ; routine MSB
            pha
            lda #<routine-1     ; routine LSB
            pha                 
            rts                 ; RTS jump to "routine"

routine     lda &long_address   ; load the absolute value of long_address
                                ; (truncate bank byte) into accummulator
```

Several built-in math functions that can also be called as part of the expressions.
```
            lda #sqrt(25)
```
See the section below on functions for a full list of available functions.

## Addressing model

By default, programs start at address 0, but you can change this by setting the program counter before the first assembled byte. 6502.Net uses the `*` symbol for the program counter. The assignment can be either a constant or expression:
```
            * = ZP + 1000       ; program counter now 1000 bytes offset from
                                ; the value of the constant ZP
```                
(Be aware of the pesky trap of trying to square the program counter using the `**` operator, i.e. `***`. This produces unexpected results. Instead consider the `pow()` function as described in the section on math functions below.)

As assembly continues, the program counter advances automatically. You can manually move the program counter forward, but keep in mind doing so will create a gap that will be filled if any bytes are added to the assembly from that point forward. For instance, consider:
```
            * = $1000
            lda #0
            jsr $1234

            * = $2004
            brk
```                
This will output 4096 bytes, with 4091 zeros. So this generally is not recommended unless this is the desired result.

To move the program counter forward for the purposes having the symbols use an address space that code will be relocated to later, you can use the `.relocate` directive:
```
            * = $0200
            newlocation = $a000

            lda #<torelocate
            sta $02
            lda #>torelocate
            sta $03
            lda #<newlocation
            sta $04
            lda #>newlocation
            sta $05
            ldy #0
            lda ($02),y
            sta ($04),y
            ....
torelocate:                                 
            .relocate newlocation   ; no gap created

            jsr relocatedsub    ; now in the "newlocation" address space
            ...
relocatedsub    lda #0
                ...
```                
To reset the program counter back to its regular position use the `.endrelocate` directive:
```
            jsr relocatedsub
            ...
            jmp finish
torelocate:
            relocate newlocation
            ...
            .endrelocate
            ;; done with movable code, do final cleanup
finish      rts
```
Because the 65xx architecture uses differing addressing modes for the same mnemonics, by default 6502.Net selects the appropriate instruction based on the minimum required size to express the operand. For instance `lda 42` can either be interpreted to be zero-page or absolute addressing, but 6502.Net will choose zero-page. Similarly, for the 65C816 `lda $c000` could either be an absolute or long address, but 6502.Net will again choose the shorter (and faster!) instruction to assemble. You can, however, force the assembler to choose the larger mode explicitly by pre-fixing the operand with the bit-size enclosed in square brackets.
```
            $c000

            ;; zero-page loadA
            lda 42          ; > .c000 a5 2a

            ;; absolute loadA
            lda [16] 42     ; > .c002 ad 2a 00

            ;; long jsr to bank 0 $ffd2
            jsr [24] $ffd2  ; > .c005 22 d2 ff 00
```
## Macros and segments
One of the more powerful features of the 6502.Net cross assembler is the ability to re-use code segments in multiple places in your source. You define a macro or segment once, and then can invoke it multiple times later in your source; the assembler simply expands the definition where it is invoked as if it is part of the source. Macros have the additional benefit of allowing you to pass parameters, so that the final outputted code can be easily modified for different contexts, behaving much like a function call in a high level language. For instance, one of the more common operations in 6502 assembly is to do a 16-bit increment. You could use a macro for this purpose like this:
```
inc16       .macro  address
            inc \address
            bne +
            inc \address+1
+           .endmacro
```
The macro is called `inc16` and takes a parameter called `address`. The code inside the macro references the parameter with a backslash `\` followed by the parameter name. The parameter is a textual subsitution; whatever you pass will be expanded at the reference point. Note the anonymous forward symbol at the branch instruction will be local to the block, as would any symbols inside the macro definition when expanded. To invoke a macro simply reference the name with a `.` in front:
```
myvariable  .word ?

            .inc16 myvariable
```        
This macro expands to:
```
            inc myvariable
            bne +
            inc myvariable+1
+           ...
```
Segments are conceptually identical to macros, except they do not accept parameters and are usually used as larger segments of relocatable code. Segments are defined between `.segment`/`.endsegment` blocks with the segment name after each closure directive.
```
            .segment zp

zpvar1      .word ?
zpvar2      .word ?
            ...
            .endsegment zp

            .segment code
            ldx #0
+           lda message,x
            jsr chrout
            inx
            cpx #msgsize
            bne +
            ...
            .endsegment code
```        
Then you would assemble defined segments as follows:
```
            * = $02
            .zp
            .errorif * > $ff, ".zp segment outside of zero-page!"

            * = $c000
            .code
```        
You can also define segments within other segment definitions. Note that doing this does not make them "nested." The above example would be re-written as:
```
            .segment program
            .segment zp
zpvar1      .word ?
zpvar2      .word ?
txtbuffer   .fill 80
            .endsegment zp
            .segment code
            ldx #0
            ...
            .segment bss
variables   .byte ?
            ...
            .endsegment bss
            .endsegment code
            .endsegment program

            * = $02
            .zp
            * = $033c
            .bss
            * = $c000
            .code
```
Macros and segments must be defined before they can be invoked.
## Flow Control
In cases where you want to control the flow of assembly, either based on certain conditions (environmental or target architecture) or in certain iterations, 6502.Net provides certain directives to handle this.
### Conditional Assembly
Conditional assembly is available using the `.if` and related directive.  Conditions can be nested, but expressions will be evaluated on first pass only.
```
            lda #$41
            .ifdef APPLE2   ; is the symbol APPLE2 defined?
                jsr $fbfd
            .else
                jsr $ffd2
            .endif
```
**Caution:** Be careful not to use the `.end` directive inside a conditional block, which terminates assembly, otherwise the `.endif` closure will never be reached, and the assembler will report an error.
### Basic Repetitions
On occasions where certain instructions will be repeatedly assembled, it is convenient to repeat their output in a loop. For instance, if you want to pad a series of `nop` instructions. The `.repeat` directive does just that.

```
            ;; will assemble $ea ten times
            .repeat 10
            nop
            .endrepeat

```
These repetitions can also be nested, as shown below.
```
            ;; print each letter of the alphabet 3 times
            * = $c000

            lda #$41
            .repeat 26
                .repeat 3
                    jsr $ffd2
                .endrepeat
                tax
                inx
                txa
            .endrepeat
            .repeat 3
               jsr $ffd2
            .endrepeat
            rts
```
### Loop Assembly
Repetitions can also be handled in for/next loops, where source can be emitted repeatedly until a condition is met. An iteration variable can optionally be initialized, with the advantage is the variable itself can be referenced inside the loop.
```
            lda #0
            .for i = $0400, i < $0800, i = i + 1
                sta i
            .next
```
A minimum two operands are required: The initial expression and the condition expression. A third iteration expression is option. The iteration expression can be blank, however.
```
            .let a = 0;
            .let n = 1;
            .for , n < 10
                .if a == 3
                    .let n = n + 1;
                .else
                    .let n = n + 5;
                .endif
                .echo format("{0}",n);
            .next

            .comment

            outputs:

            6
            11

            .endcomment
```
If required, loops can be broken out of using the `.break` directive
```
            .for i = 0, i < 256, i = i + 1
                .if * >= $1000
                    .break          ; make sure assembly does not go past $1000
                .endif
                lda #'A'
                jsr $ffd2
            .next
```
All expressions, including the condition, are only evaluated on the first pass.

**Caution:** Changing the value of the iteration variable inside the loop can cause the application to hang. 6502.Net does not restrict re-assigning the iteration variable inside its own or nested loops.

## Illegal operations, 65C02 and W65C816S support

By default, 6502.Net "thinks" like a 6502 assembler, compiling only the published 56 mnemonics and 151 instructions of that microprocessor. As of Version 1.7, 6502.Net can also compile illegal instructions as well as those of the successor WDC 65C02 and W65C816S processors. The `.cpu` directive tells the assembler the type of source and instruction set it is to assemble.
```
            .cpu "6502i"    ; enable illegal instructions

            ldx #0
            slo (zpvar,x)
```
There are four options for the `.cpu` directive: `6502`, `6502i`, `65C02` and `65816`. `6502` is default. You can also select the cpu in the command line by passing the `--cpu` option (detailed below). Note that only one CPU target can be selected at a time, though in the case of the `65816` selection this also includes 65C02 and 6502 (legal) instructions, since it is a superset of both.

Immediate mode on the 65816 differs based on register size. 6502.Net must be told which size to use for which register in order to assemble the correct number of bytes for immediate mode operations. Use `.m8` for 8-bit accumulator and `.m16` for 16-bit accumulator; `.x8` for 8-bit index registers and `.x16` for 16-bit index registers.
```
            rep #%00110000

            .m16
            lda #$c000      
            ldx #$03

            .x16
            ldy #$1000
            jml $1012000
```
Eight-bit modes for registers are default.

You can also set all registers to the same size with `.mx8` and `.mx16` respectively.
```
            sep #%00110000

            .mx8

            lda #$00
            ldx #$01
            ldy #$02
```
## Reference
### Instruction set
By default, the 6502.Net only recognizes the 151 published instructions of the original MOS Technology 6502. The following mnemonics are recognized:
```
adc,and,asl,bcc,bcs,beq,bit,bmi,bne,bpl,brk,bvc,bvs,clc,
cld,cli,clv,cmp,cpx,cpy,dec,dex,dey,eor,inc,inx,iny,jmp,
jsr,lda,ldx,ldy,lsr,nop,ora,pha,php,pla,plp,rol,ror,rti,
rts,sbc,sec,sed,sei,sta,stx,sty,tax,tay,tsx,txa,txs,tya
```
65C02 support adds the following additional mnemonics:
```
bra,phx,phy,plx,ply,trb,tsb
```
For 65816 compatibility the following mnemonics are recognized:
```
brl,cop,jml,jsl,mvn,mvp,pea,pei,per,phb,phd,phk,plb,pld,
rep,rtl,sep,stp,tcd,tcs,tdc,tsc,txy,tyx,wai,wdm,xba,xce
```
Since they are technically undocumented, mnemonics for illegal instructions vary among assemblers. 6502.Net closely follows those used by [VICE](http://vice-emu.sourceforge.net/), a popular Commodore 64 emulator. Illegal mnemonics, operations and opcodes are as follows:

<table>
<tr>
<td>
<table>
<tr><th>Mnemonic</th><th>Addressing Mode</th><th>Opcode</th></tr>
<tr><td>ANC</td><td>Immediate             </td><td>2B</td></tr>
<tr><td>ANE</td><td>Immediate             </td><td>8B</td></tr>
<tr><td>ARR</td><td>Immediate             </td><td>6B</td></tr>
<tr><td>ASR</td><td>Immediate             </td><td>4B</td></tr>
<tr><td>DCP</td><td>Indexed Indirect      </td><td>C3</td></tr>
<tr><td>DCP</td><td>Zero-Page             </td><td>C7</td></tr>
<tr><td>DCP</td><td>Absolute              </td><td>CF</td></tr>
<tr><td>DCP</td><td>Indirect Indexed      </td><td>D3</td></tr>
<tr><td>DCP</td><td>Zero-Page Indexed X   </td><td>D7</td></tr>
<tr><td>DCP</td><td>Absolute Indexed Y    </td><td>DB</td></tr>
<tr><td>DCP</td><td>Absolute Indexed X    </td><td>DF</td></tr>    
<tr><td>DOP</td><td>Implied/Immediate     </td><td>80</td></tr>
<tr><td>ISB</td><td>Indexed Indirect      </td><td>E3</td></tr>
<tr><td>ISB</td><td>Zero-Page             </td><td>E7</td></tr>
<tr><td>ISB</td><td>Absolute              </td><td>EF</td></tr>
<tr><td>ISB</td><td>Indirect Indexed      </td><td>F3</td></tr>
<tr><td>ISB</td><td>Zero-Page Indexed X   </td><td>F7</td></tr>
<tr><td>ISB</td><td>Absolute Indexed Y    </td><td>FB</td></tr>
<tr><td>ISB</td><td>Absolute Indexed X    </td><td>FF</td></tr>    
<tr><td>JAM*</td><td>Implied               </td><td>02</td></tr>
<tr><td>LAS</td><td>Absolute Indexed Y    </td><td>BB</td></tr>
<tr><td>LAX</td><td>Indexed Indirect      </td><td>A3</td></tr>
<tr><td>LAX</td><td>Zero-Page             </td><td>A7</td></tr>
<tr><td>LAX</td><td>Absolute              </td><td>AF</td></tr>
<tr><td>LAX</td><td>Indirect Indexed      </td><td>B3</td></tr>
<tr><td>LAX</td><td>Zero-Page Indexed X   </td><td>B7</td></tr>
<tr><td>LAX</td><td>Absolute Indexed Y    </td><td>BF</td></tr>  
<tr><td>RLA</td><td>Indexed Indirect      </td><td>23</td></tr>
<tr><td>RLA</td><td>Zero-Page             </td><td>27</td></tr>
<tr><td>RLA</td><td>Absolute              </td><td>2F</td></tr>
<tr><td>RLA</td><td>Indirect Indexed      </td><td>33</td></tr>
<tr><td>RLA</td><td>Zero-Page Indexed X   </td><td>37</td></tr>
<tr><td>RLA</td><td>Absolute Indexed Y    </td><td>3B</td></tr>   
</table>
</td>
<td>
<table>
<tr><th>Mnemonic</th><th>Addressing Mode</th><th>Opcode</th></tr>
<tr><td>RLA</td><td>Absolute Indexed X    </td><td>3F</td></tr>
<tr><td>RRA</td><td>Indexed Indirect      </td><td>63</td></tr>
<tr><td>RRA</td><td>Zero-Page             </td><td>67</td></tr>
<tr><td>RRA</td><td>Absolute              </td><td>6F</td></tr>
<tr><td>RRA</td><td>Indirect Indexed      </td><td>73</td></tr>
<tr><td>RRA</td><td>Zero-Page Indexed X   </td><td>77</td></tr>
<tr><td>RRA</td><td>Absolute Indexed Y    </td><td>7B</td></tr>
<tr><td>RRA</td><td>Absolute Indexed X    </td><td>7F</td></tr>    
<tr><td>SAX</td><td>Indexed Indirect      </td><td>83</td></tr>
<tr><td>SAX</td><td>Zero-Page             </td><td>87</td></tr>
<tr><td>SAX</td><td>Absolute              </td><td>8F</td></tr>
<tr><td>SAX</td><td>Zero-Page Indexed X   </td><td>97</td></tr>
<tr><td>SAX</td><td>Absolute Indexed Y    </td><td>9B</td></tr>
<tr><td>SHX</td><td>Absolute Indexed Y    </td><td>9E</td></tr>
<tr><td>SHY</td><td>Absolute Indexed X    </td><td>9C</td></tr>
<tr><td>SLO</td><td>Indexed Indirect      </td><td>03</td></tr>
<tr><td>SLO</td><td>Zero-Page             </td><td>07</td></tr>
<tr><td>SLO</td><td>Absolute              </td><td>0F</td></tr>
<tr><td>SLO</td><td>Indirect Indexed      </td><td>13</td></tr>
<tr><td>SLO</td><td>Zero-Page Indexed X   </td><td>17</td></tr>
<tr><td>SLO</td><td>Absolute Indexed Y    </td><td>1B</td></tr>
<tr><td>SLO</td><td>Absolute Indexed X    </td><td>1F</td></tr>    
<tr><td>SRE</td><td>Indexed Indirect      </td><td>43</td></tr>
<tr><td>SRE</td><td>Zero-Page             </td><td>47</td></tr>
<tr><td>SRE</td><td>Absolute              </td><td>4F</td></tr>
<tr><td>SRE</td><td>Indirect Indexed      </td><td>53</td></tr>
<tr><td>SRE</td><td>Zero-Page Indexed X   </td><td>57</td></tr>
<tr><td>SRE</td><td>Absolute Indexed Y    </td><td>5B</td></tr>
<tr><td>SRE</td><td>Absolute Indexed X    </td><td>5F</td></tr>
<tr><td>STP*</td><td>Implied</td><td>12</td></tr>
<tr><td>TAS</td><td>Absolute Indexed Y    </td><td>9B</td></tr>
<tr><td>TOP</td><td>Immediate/Absolute    </td><td>0C</td></tr>
<tr><td>TOP</td><td>Absolute Indexed X    </td><td>1C</td></tr>
</table>
</td>
</tr>
</table>

*-`JAM` and `STP` are essentially the same command; they both halt the CPU.

**Note:** Illegal mnemonics are only available if the `6502i` option is specified in the `--cpu` commandline or `.cpu` directive.

### Pseudo-Ops
Following is the detail of each of the 6502.Net pseudo operations, or psuedo-ops. A pseudo-op is similar to a mnemonic in that it tells the assembler to output some number of bytes, but different in that it is not part of the CPU's instruction set. For each pseudo-op description is its name, any aliases, a definition, arguments, and examples of usage. Optional arguments are in square brackets (`[` and `]`).

Note that every argument, unless specified, is a legal mathematical expression, and can include symbols such as labels (anonymous and named) and the program counter. Anonymous labels should be referenced in parantheses, otherwise the expression engine might misinterpret them. If the expression evaluates to a value greater than the maximum value allowed by the pseudo-op, the assembler will issue an illegal quantity error.

<p align="center"><b>Data/text insertions</b></p>
<table>
<tr><td><b>Name</b></td><td><code>.addr</code></td></tr>
<tr><td><b>Alias</b></td><td><code>.word</code></td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned 16-bit value or values between 0 and 65535 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address[, address2[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c000
mysub   lda #13                 ; output newline
        jsr chrout
        rts
        .addr mysub             ; >c006 00 c0
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.align</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set the program counter to a value divisible by the argument. If a second argument is specified, the
expressed bytes will be assembled until the point the program counter reaches its new value, otherwise is treated as uninitialized memory.</td></tr>
<tr><td><b>Arguments</b></td><td><code>amount[, fillvalue]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      * = $c023
      .align $10,$ff ; >c023 ff ff ff ff ff ff ff ff
                     ; >c02b ff ff ff ff ff
      .byte $23      ; >c030 23
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.binary</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a file as binary data into the assembly. Optional offset and file size arguments can be passed for greater flexibility.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename[, offset[, size]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .binary     "subroutines.prg",2  ; strip off start address
      .binary     "mybin.bin"          ; include all of 'mybin.bin'
      .binary     "soundtrack.sid",$7e ; skip SID-header
      .binary     "subroutines.prg",2,1000
                  ;; strip off start address, only take first
                  ;; 1000 bytes thereafter.
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.byte</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned byte-sized value or values between 0 and 255 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      * = $033c
      .byte $39, $38, $37, $36, $35, $34, $33, $32, $31
      ;; >033c 39 38 37 36 35 34 33 32
      ;; >0344 31
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.cstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a C-style null-terminated string into the assembly. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = 1000
        .cstring "hello, world!"    ; >1000 68 65 6c 6c 6f 2c 20 77
                                    ; >1008 6f 72 6c 64 21 00
        .cstring $93,"ALL CLEAR"    ; >100e 93 41 4c 4c 20 43 4c 45
                                    ; >1016 41 52 00
        .cstring $ffd2              ; >1019 d2 ff 00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.dint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 32-bit value or values between −2147483648 and 2147483647 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0801
        .dint   18000000      ; &gt;0801 80 a8 12 01
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.dword</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 32-bit value or values between 0 and 4294967295 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0801
        .dword  $deadfeed     ; &gt;0801 ed fe ad de
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.fill</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Fill the assembly by the specified amount. Similar to align, that if only one argument is passed then space is merely reserved. Otherwise the optional second argument indicates the assembly should be filled with bytes making up the expression, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>amount[, fillvalue]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .fill   23  ; reserve 23 bytes
        * = $1000
        .fill 11,$ffd2 ; >1000 d2 ff d2 ff d2 ff d2 ff
                       ; >1008 d2 ff d2
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.lint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 24-bit value or values between -8388608 and 8388607 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c100
        .lint   -80000    ; >c100 80 c7 fe
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.long</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 24-bit value or values between 0 and 16777215 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c100
        .long   $ffdd22   ; >c100 22 dd ff
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.lsstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly, each byte shifted to the left, with the lowest bit set on the last byte. See example of how this format can be used. If the highest bit of any output byte is set, the assembler will error. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        ldx #0
-       lda message,x
        lsr a               ; shift right
        php                 ; save carry flag
        jsr chrout          ; print
        plp                 ; restore carry flag
        bcs done            ; if set we printed last char
        inx                 ; increment pointer
        jmp -               ; get next
        ...
        * = $c100
message .lsstring "HELLO"   ; >c100 90 8a 98 98 9f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.nstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly, the negative (highest) bit set on the last byte. See example of how this format can be used. If the highest bit of the last byte is already set, the assembler will error. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        ldx #0
-       lda message,x
        php                 ; save negative flag
        and #%01111111      ; turn off high bit...
        jsr chrout          ; and print
        plp                 ; restore negative flag
        bmi done            ; if set we printed last char
        inx                 ; else increment pointer
        jmp -               ; get next
        ...
        * = $c100
message .nstring "hello"    ; >c100 68 65 6c 6c ef
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.pstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a Pascal-style string into the assembly, the first byte indicating the full string size. Note this size includes all arguments in the expression. If the size is greater than 255, the assembler will error. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $4000
        .pstring $23,$24,$25,$26,1024 ; >4000 06 23 24 25 26 00 04
        .pstring "hello"              ; >4007 05 68 65 6c 6c 6f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.rta</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned 16-bit value or values between 0 and 65535 into the assembly. Similar to <code>.addr</code> and <code>.word</code>, except the value is decremented by one, yielding a return address. This is useful for building "rts jump" tables. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address[, address2[, ...]</code></td></tr>
<tr><td><b>Example</b></td>
<td><pre>
chrin   = $ffcf
chrout  = $ffd2
        * = $c000
rtsjmp  txa                 ; .x := index of jump
        asl a               ; double it
        tax                 
        lda jumptable+1,x   ; push high byte
        pha
        lda jumptable,x     ; push low byte
        pha
        rts                 ; do the jump
jumptable
        .rta chrout, chrin  ; >c00b d1 ff ce ff
</pre></td>
</tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.sbyte</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned byte-sized value or values between -128 and 127 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $033c
        .sbyte 127, -3  ; >033c 7f fd
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.sint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 16-bit value or values between -32768 and 32767 into the assembly, little-endian. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c000
mysub   lda #13             ; output newline
        jsr chrout
        rts
        .sint -16384        ; >c006 00 c0
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.string</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = 1000
        .string "hello, world!"   ; >1000 68 65 6c 6c 6f 2c 20 77
                                  ; >1008 6f 72 6c 64 21
</pre>
</td></tr>
</table>
<p align="center"><b>Assembler directives</b></p>
<table>
<tr><td><b>Name</b></td><td><code>.assert</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Asserts the truth of a given expression. If the assertion fails, an error is logged. A custom error can optionally be specified.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition[, error]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0800
        nop
        .assert 5 == 6              ; standard assertion error thrown
        .assert * < $0801, "Uh oh!" ; custom error output
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.binclude</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Include a source file and enclose the expanded source into a scoped block. The specified file is 6502.Net-compatible source. If no name is given in front of the directive then all symbols inside the included source will be inaccessible. Note that to prevent infinite recursion, a source file can only be included once in the entire source, including from other included files.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
soundlib    .binclude "sound.s"
            jsr soundlib.play   ; Invoke the
                                ; play subroutine
                                ; inside the
                                ; sound.s source
            ;; whereas...
            .binclude "sound.s"
            jsr play            ; will not assemble!
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.block</code>/<code>.endblock</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Define a scoped block for symbols. Useful for preventing label definition clashes. Blocks can be nested as needed. Unnamed blocks are considered anonymous and all symbols defined within them are inaccessible outside the block. Otherwise symbols inside blocks can be accessed with dot-notation.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
kernal .block
        chrout = $ffd2
        chrin  = $ffcf
        .endblock
        ...
chrout  lda message,x       
        jsr kernal.chrout   ; this is a different
                            ; chrout!
done    rts                 ; this is not the done
                            ; below!                
        .block
        beq done            ; the done below!
        nop
        nop
done    rts                 
        .endblock
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.break</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Break out of the current for-next loop.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .for n = 0, n < 1000, n = n + 1
            .if * > $7fff   ; unless address >= $8000
                .break     
            .endif
            nop             ; do 1000 nops
        .next
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.cpu</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set the assembler to target the supported CPU. See the <code>--cpu</code> option in the command-line notes below for the available options.</td></tr>
<tr><td><b>Arguments</b></td><td><code>cpu</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .cpu "65816"
      clc
      xce
      rep #%00110000
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.comment</code>/<code>.endcomment</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set a multi-line comment block.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
    .comment
    My code pre-amble
    .endcomment
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.echo</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Send a message to the console output. Note if the assembler
is in quiet mode, no output will be given.</td></tr>
<tr><td><b>Arguments</b></td><td>message</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
    .echo "hi there!"
    ;; console will output "hi there!"
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.encoding</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Select the text encoding for assembly output. Four encodings are pre-defined:
<ul>
        <li><code>none</code>       - UTF-8 (default)</li>
        <li><code>atascreen</code>       - Atari screen codes</li>
        <li><code>cbmscreen</code>       - Commodore screen codes</li>
        <li><code>petscii</code>       - Commodore PETSCII</li>
</ul>
Note: <code>none</code> is default and will not be affected by <code>.map</code> and <code>.unmap</code> directives.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>encoding</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .encoding petscii
      .string "hello"       ; >> 45 48 4c 4c 4f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.end</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Terminate the assembly.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        jsr $ffd2
        beq done            ; oops!
        rts
        .end                ; stop everything
done    ...                 ; assembly will never
                            ; reach here!
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.eor</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>XOR output with 8-bit value. Quick and dirty obfuscation trick.</td></tr>
<tr><td><b>Arguments</b></td><td><code>xormask</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .eor $ff
      .byte 0,1,2,3       ; > ff fe fd fc
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.equ</code></td></tr>
<tr><td><b>Alias</b></td><td><code>=</code></td></tr>
<tr><td><b>Definition</b></td><td>Assign the label, anonymous symbol, or program counter to the expression. Note that there is an implied version of this directive, such that if the directive and expression are ommitted altogether, the label or symbol is set to the program counter.</td></tr>
<tr><td><b>Arguments</b></td><td><code>symbol, value</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
chrin      .equ $ffcf
chrout      =   $ffd2
          * .equ $c000
-           =   255
start       ; same as start .equ *
            ldx #$00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.error</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom error to the console. The error is treated like any assembler error and will cause failure of assembly.</td></tr>
<tr><td><b>Arguments</b></td><td><code>error</code></td></tr>
<tr><td><b>Example</b></td><td>
<code>.error "We haven't fixed this yet!" </code>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.errorif</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom error to the console if the condition is met. Useful for sanity checks and assertions. The error is treated like any assembler error and will cause failure of assembly. The condition is any logical expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition, error</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0800
        nop
        .errorif * > $0801, "Uh oh!" ; if program counter
                                    ; is greater than 2049,
                                    ; raise a custom error
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.[el]if[[n]def]</code>/<code>.endif</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>All source inside condition blocks are assembled if evaluated to true on the first pass. Conditional expressions follow C-style conventions. The following directives are available:
    <ul>
        <li><code>.if &lt;expression&gt;</code>   - Assemble if the expression is true</li>
        <li><code>.ifdef &lt;symbol&gt;</code>    - Assemble if the symbol is defined</li>
        <li><code>.ifndef &lt;symbol&gt;</code>   - Assemble if the symbol is not defined</li>
        <li><code>.elif &lt;expression&gt;</code> - Assemble if expression is true and previous conditions are false</li>
        <li><code>.elifdef &lt;symbol&gt;</code>  - Assemble if symbol is defined and previous conditions are false</li>
        <li><code>.elifndef &lt;symbol&gt;</code> - Assemble if symbol is not defined and previous conditions are false</li>
        <li><code>.else</code>                    - Assemble if previous conditions are false</li>
        <li><code>.endif</code>                   - End of condition block
    </ul>
</td></tr>
    <tr><td><b>Arguments</b></td><td><code>condition</code>/<code>symbol</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0400
        cycles = 1
        .if cycles == 1
            nop
        .elif cycles == 2
            nop
            nop
        .endif
        ;; will result as:
        ;;
        ;; nop
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.for</code>/<code>.next</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Repeat until codition is met. The iteration variable can be used in source like any other variable. The initialization expression can be blank. Multiple iteration expressions can be specified.</td></tr>
<tr><td><b>Arguments</b></td><td><code>[init_expression], condition[, iteration_expression[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        .let x = 0
        .for pages = $100, pages < $800, pages = pages + $100, x = x + 1
            ldx #x
            stx pages
        .next
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.include</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Include a source file into the assembly. The specified file is 6502.Net-compatible source. Note that to prevent infinite recursion, a source file can only be included once in the entire source, including from other included files.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .include "mylib.s"
      ;; mylib is now part of source
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.let</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Declares and assigns or re-assigns a variable to the given expression. Labels cannot be redefined as variables, and vice versa. In addition, variables cannot be forward-referenced, as they are reset each pass.</td></tr>
<tr><td><b>Arguments</b></td><td><code>expression</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
            .let myvar =    $ffd2
            jsr myvar
            .let myvar =    myvar-$1000
            lda myvar
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.m8</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the accumulator as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      sep #$20
      .m8
      lda #$13
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.m16</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the accumulator as 16-bit (two bytes). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      rep #$20
      .m16
      lda #$1234
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.mx8</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat all immediate mode operations as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      sep #$30
      .mx8
      lda #$13
      ldx #$14
      ldy #$15
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.mx16</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat all immediate mode operations as 16-bit (two bytes). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      rep #$30
      .mx16
      lda #$1234
      ldx #$5678
      ldy #$9abc
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.macro</code>/<code>.endmacro</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Define a macro that when invoked will expand into source. Must be named. Optional arguments are treated as parameters to pass as text substitutions in the macro source where referenced, with a leading backslash <code>\</code> and either the macro name or the number in the parameter list. Parameters can be given default values to make them optional upon invocation. Macros are called by name with a leading "." All symbols in the macro definition are local, so macros can be re-used with no symbol clashes.</td></tr>
<tr><td><b>Arguments</b></td><td><code>parameter[, parameter[, ...]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
inc16       .macro
            inc \1
            bne +
            inc \1+1
&#43;          .endmacro
            .inc16 $c000
            ;; expands to =>
            inc $c000
            bne +
            inc $c001
&#43;         
print       .macro  value = 13, printsub = $ffd2
            lda #\value     ; or lda #\1
            jsr \printsub   ; or jsr \2
            rts
            .endmacro
            .print
            ;; expands to =>
            ;; lda #$0d
            ;; jsr $ffd2
            ;; rts
            .print 'E',$fded
            ;; expands to =>
            ;; lda #$45
            ;; jsr $fded
            ;; rts
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.map</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Maps a character or range of characters to custom binary output in the selected encoding. Note: <code>none</code> is not affected by <code>.map</code> and <code>.unmap</code> directives. It is recommended to represent individual char literals as strings.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>start[, end]</code>,<code>code</code>/<br>
<code>"&lt;start&gt;&lt;end&gt;"</code>,<code>code</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .encoding myencoding
      .map "A", "a"
      .map "π", $5e
      .byte 'A', 'π' ;; >> 61 5e
      .map "09", $00
      .string "2017" ;; >> 02 00 01 07
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.relocate</code>/<code>.endrelocate</code></td></tr>
<tr><td><b>Alias</b></td><td><code>.pseudopc</code>/<code>.realpc</code></td></tr>
<tr><td><b>Definition</b></td><td>Sets the logical program counter to the specified address with the offset of the assembled output not changing. Useful for programs that relocate parts of themselves to different memory spaces.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
            * = $0801
            ;; create a Commodore BASIC stub
            ;; 10 SYS2061
SYS         = $9e
            .word eob, 10
            .cstring SYS, str(start)
eob         .word 0
start       ldx #0
-           lda highcode,x
            sta $c000,x
            inx
            bne -
            jmp $c000
highcode    
            .relocate $c000
            ldx #0
printloop   lda message,x
            beq done
            jsr $ffd2
            inx
            jmp printloop
done        rts
message     .cstring "HELLO, HIGH CODE!"
            .endrelocate
            ;; outputs the following =>
            .comment
            &gt;0801 0b 08 0a 00           
            &gt;0805 9e 32 30 36 31 00         
            &gt;080b 00 00             
            &gt;080d a2 00     ;           ldx #0
            &gt;080f bd 1b 08  ; -         lda highcode,x
            &gt;0812 9d 00 c0  ;           sta $c000,x
            &gt;0815 e8        ;           inx
            &gt;0816 d0 f7     ;           bne -
            &gt;0818 4c 00 c0  ;           jmp $c000
            &gt;081b a2 00     ;           ldx #$00        
            &gt;081d bd 0f c0  ; printloop lda message,x
            &gt;0820 f0 07     ;           beq done        
            &gt;0822 20 d2 ff  ;           jsr $ffd2         
            &gt;0825 e8        ;           inx               
            &gt;0826 4c 02 c0  ;           jmp printloop             
            &gt;0829 60        ; done      rts      
            ;; message
            &gt;082a 48 45 4c 4c 4f 2c 20 48       
            &gt;0832 49 47 48 20 43 4f 44 45     
            &gt;083a 21 00  
            .endcomment
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.repeat</code>/<code>.endrepeat</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Repeat the specified source the specified number of times. Can be nested, but must be terminated with an <code>.endrepeat</code>.</td></tr>
<tr><td><b>Arguments</b></td><td><code>repeatvalue</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0400
        ldx #$00
        .repeat 3
        inx
        .endrepeat
        rts
        ;; will assemble as:
        ;;
        ;; ldx #$00
        ;; inx
        ;; inx
        ;; inx
        ;; rts
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.segment</code>/<code>.endsegment</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Defines a block of code as a segment, to be invoked and expanded elsewhere. Similar to macros but takes no parameters and symbols are not local. Useful for building large mix of source code and data without needing to relocate code manually. Segments can be defined within other segment block definitions, but are not considered "nested." Segment closures require the segment name after the directive.</td></tr>
<tr><td><b>Arguments</b></td><td><code>segmentname</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
            .segment zp
zpvar1      .word ?
zpvar2      .word ?
txtbuf      .fill 80
            .endsegment zp
            .segment bss
variables   .dword ?, ?, ?, ?
            .endsegment bss
            .segment code
            .segment data
glyph             ;12345678
            .byte %....####
            .byte %..#####.
            .byte %.#####..
            .byte %#####...
            .byte %#####...
            .byte %.#####..
            .byte %..#####.
            .byte %....####
            .endsegment data
            .basic      ; macro that creates BASIC stub
            sei
            cld
            jsr init
            .endsegment code
            * = $80
            .zp
            * = $0100
            .bss
            * = $0801
            .code
            * = $0900
            .data
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.target</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Set the target architecture for the assembly output. See the <code>--arch</code> option in the command-line notes below for the available architectures.</td></tr>
<tr><td><b>Arguments</b></td><td><code>architecture</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .target "apple2"
      ;; the output binary will have an Apple DOS header
      ...
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.typedef</code></td></tr>
<tr><td><b>Note</b></td><td>This feature is currently disabled for now due to a technical issue that caused it not to work correctly in all cases.</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.unmap</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Unmaps a custom code for a character or range of characters in the selected encoding and reverts to UTF-8. Note: <code>none</code> is not affected by <code>.map</code> and <code>.unmap</code> directives. It is recommended to represent individual char literals as strings.
</td></tr>
<tr><td><b>Arguments</b></td><td><code>start[, end]</code>/<br>
<code>"&lt;start&gt;&lt;end&gt;"</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .encoding myencoding
      .unmap "A"
      .unmap "π"        ;; revert to UTF-8 encoding
      .byte 'A', 'π'    ;; >> 41 cf 80
      .unmap "09"
      .string "2017"    ;; >> 32 30 31 37
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.warn</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom warning to the console. The warning is treated like any assembler warning, and if warnings are treated as errors it will cause failure of assembly.</td></tr>
<tr><td><b>Arguments</b></td><td><code>warning</code></td></tr>
<tr><td><b>Example</b></td><td>
<code>.warn "We haven't fixed this yet!" </code>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.warnif</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom warning to the console if the condition is met. The warning is treated like any assembler warning, and if warnings are treated as errors it will cause failure of assembly The condition is any logical expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition, warning</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
    * = $0800
    nop
    .warnif   * > $0801, "Check bound"
    ;; if program counter
    ;; is greater than 2049,
    ;; raise a custom warning
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.x8</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the index registers as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      sep #$10
      .x8
      ldx #$14
      ldy #$15
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.x16</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Directs the assembler to treat immediate mode operations on the index registers as 8-bit (one byte). Useful for when the assembler is in <code>65816</code> mode.</td></tr>
<tr><td><b>Arguments</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      rep #$10
      .x16
      ldx #$5678
      ldy #$9abc
</pre>
</td></tr>
</table>

## Appendix
### Built-In functions
<table>
<tr><td><b>Name</b></td><td><code>abs</code></td></tr>
<tr><td><b>Definition</b></td><td>The absolute (positive sign) value of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.word abs(-2234)     ; > ba 08</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>acos</code></td></tr>
<tr><td><b>Definition</b></td><td>The arc cosine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte acos(1.0)      ; > 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>atan</code></td></tr>
<tr><td><b>Definition</b></td><td>The arc tangent of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte atan(0.0)      ; > 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>cbrt</code></td></tr>
<tr><td><b>Definition</b></td><td>The cubed root of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.long cbrt(2048383)   ; > 7f 00 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>ceil</code></td></tr>
<tr><td><b>Definition</b></td><td>Round up expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte ceil(1.1)       ; > 02</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>cos</code></td></tr>
<tr><td><b>Definition</b></td><td>The cosine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte cos(0.0)        ; > 01</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>cosh</code></td></tr>
<tr><td><b>Definition</b></td><td>The hyperbolic cosine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte cosh(0.0)       ; > 01</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>deg</code></td></tr>
<tr><td><b>Definition</b></td><td>Degrees from radians.</td></tr>
<tr><td><b>Arguments</b></td><td><code>radian</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte deg(1.0)        ; > 39</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>exp</code></td></tr>
<tr><td><b>Definition</b></td><td>Exponential of e.</td></tr>
<tr><td><b>Arguments</b></td><td><code>power</code></td></tr>
<tr><td><b>Example</b></td><td><code>.dint exp(16.0)       ; > 5e 97 87 00</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>floor</code></td></tr>
<tr><td><b>Definition</b></td><td>Round down expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.sbyte floor(-4.8)     ; > fb</code></td></tr>
</table>
<table>
<table>
<tr><td><b>Name</b></td><td><code>format</code></td></tr>
<tr><td><b>Definition</b></td><td>Converts objects to a string in the format specified. The format string must adhere to Microsoft .Net standards. Please see <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings">the documentation on standard .Net format strings</a> for more information.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.echo format("Program counter is ${0:x4}", *)</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>frac</code></td></tr>
<tr><td><b>Definition</b></td><td>The fractional part.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte frac(5.18)*100  ; > 12</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>hypot</code></td></tr>
<tr><td><b>Definition</b></td><td>Polar distance.</td></tr>
<tr><td><b>Arguments</b></td><td><code>pole1, pole2</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte hypot(4.0, 3.0) ; > 05</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>ln</code></td></tr>
<tr><td><b>Definition</b></td><td>Natural logarithm.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte ln(2048.0)      ; > 07</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>log10</code></td></tr>
<tr><td><b>Definition</b></td><td>Common logarithm.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte log($7fffff)    ; > 06</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>pow</code></td></tr>
<tr><td><b>Definition</b></td><td>Exponentiation.</td></tr>
<tr><td><b>Arguments</b></td><td><code>base, power</code></td></tr>
<tr><td><b>Example</b></td><td><code>.lint pow(2,16)       ; > 00 00 01</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>rad</code></td></tr>
<tr><td><b>Definition</b></td><td>Radians from degrees.</td></tr>
<tr><td><b>Arguments</b></td><td><code>degree</code></td></tr>
<tr><td><b>Example</b></td><td><code>.word rad(79999.9)    ; > 74 05</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>random</code></td></tr>
<tr><td><b>Definition</b></td><td>Generate a random number within the specified range of numbers. Both arguments can be negative or positive, but the second argument must be greater than the first, and the difference between them can be no greater than the maximum value of a signed 32-bit integer. This is a .Net limitation.</td></tr>
<tr><td><b>Arguments</b></td><td><code>range1, range2</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
 .word random(251,255)   ; generate a random # between
                         ; 251 and 255.
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>round</code></td></tr>
<tr><td><b>Definition</b></td><td>Round number.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, places]</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte round(18.21, 0) ; > 12</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sgn</code></td></tr>
<tr><td><b>Definition</b></td><td>The sign of the expression, returned as -1 for negative, 1 for positive, and 0 for no sign.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
 .sbyte sgn(-8.0), sgn(14.0), sgn(0)
 ;; > ff 01 00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sin</code></td></tr>
<tr><td><b>Definition</b></td><td>The sine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.sbyte sin(1003.9) * 14 ; > f2</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sinh</code></td></tr>
<tr><td><b>Definition</b></td><td>The hyperbolic sine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte sinh(0.0)        ; > f2</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sqrt</code></td></tr>
<tr><td><b>Definition</b></td><td>The square root of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte sqrt(65536) - 1  ; > ff</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>str</code></td></tr>
<tr><td><b>Definition</b></td><td>The expression as a text string. Only available for use with the string pseudo-ops.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.string str($c000)     ; > 34 39 31 35 32</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>tan</code></td></tr>
<tr><td><b>Definition</b></td><td>The tangent the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte tan(444.0)*5.0   ; > 08</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>tanh</code></td></tr>
<tr><td><b>Definition</b></td><td>The hyperbolic tangent the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte tanh(0.0)        ; > 00</code></td></tr>
</table>

### Command-line options

6502.Net accepts several arguments, requiring at least one. If no option flag precedes the argument, it is considered an input file. Multiple input files can be assembled. If no output file is specified, source is assembled to `a.out` within the current working directory. Below are the available option flags and their parameters. Mono users note for the examples you must put `mono` in front of the executable.

<table>
<tr><td><b>Option</b></td><td><code>-o</code></td></tr>
<tr><td><b>Alias</b></td><td>--output</td></tr>
<tr><td><b>Definition</b></td><td>Output the assembly to the specified output file. A valid output filename is a required parameter.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -o myoutput
6502.Net.exe myasm.asm -output=myoutput
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--arch</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Specify the target architecture of the binary output. Four options are available. If architecture not specified, the default is <code>cbm</code>. The options:
    <ul>
        <li><code>apple2</code>    - Apple ][ binary with Apple DOS header</li>
        <li><code>atari-xex</code> - Atari 8-bit binary with XEX header</li>
        <li><code>cbm</code>       - Commodore DOS binary with load address header (default)</li>
        <li><code>flat</code>      - Flat binary with no header</li>
    </ul>
</td></tr>
<tr><td><b>Parameter</b></td><td><code>architecture</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm -b --arch=flat flat.bin</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-b</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--big-endian</code></td></tr>
<tr><td><b>Definition</b></td><td>Assemble multi-byte values in big-endian order (highest order magnitude first).</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm -b -o bigend.bin</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-C</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--case-sensitive</code></td></tr>
<tr><td><b>Definition</b></td><td>Set the assembly mode to case-sensitive. All tokens, including assembly mnemonics, directives, and symbols, are treated as case-sensitive. By default, 6502.Net is not case-sensitive.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe mycsasm.asm -C
6502.Net.exe mycsasm.asm --case-sensitive
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--cpu</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Direct the assembler to the given cpu target By default, 6502.Net targets only legal 6502 instructions. The following options are available:
<ul>
        <li><code>6502</code>        - Legal 6502 instructions only (default)</li>
        <li><code>6502i</code>       - Legal and illegal 6502 instructions</li>
        <li><code>65C02</code>       - Legal 6502 and 65C02 instructions</li>
        <li><code>65816</code>       - Legal 6502, 65C02 and W65C816 instructions</li>
</ul>
</td></tr>
<tr><td><b>Parameter</b></td><td><code>cpu</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myillegalasm.asm --cpu=6502i
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-D</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--define</code></td></tr>
<tr><td><b>Definition</b></td><td>Assign a global label a value. Note that within the source the label cannot be redefined again. The value can be any expression 6502.Net can evaluate at assembly time. If no value is given the default value is 1.</td></tr>
<tr><td><b>Parameter</b></td><td><code>&lt;label&gt;=&lt;value&gt;</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe -D chrout=$ffd2 myasm.asm -o myoutput</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-h</code></td></tr>
<tr><td><b>Alias</b></td><td><code>-?, --help</code></td></tr>
<tr><td><b>Definition</b></td><td>Print all command-line options to console output.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -h
6502.Net.exe --help
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-q</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--quiet</code></td></tr>
<tr><td><b>Definition</b></td><td>Assemble in quiet mode, with no messages sent to console output, including errors and warnings.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -q myasm.asm
6502.Net.exe --quiet myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-w</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-warn</code></td></tr>
<tr><td><b>Definition</b></td><td>Suppress the display of all warnings.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -w myasm.asm
6502.Net.exe --no-warn myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--wnoleft</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Suppress warnings for lines where whitespaces precede labels.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe --wnoleft myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--werror</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Treat all warnings as errors.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe --werror myasm.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-l</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--labels</code></td></tr>
<tr><td><b>Definition</b></td><td>Dump all label definitions to listing.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -l labels.asm
6502.Net.exe myasm.asm --labels=labels.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-L</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--list</code></td></tr>
<tr><td><b>Definition</b></td><td>Output the assembly listing to the specified file.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -L listing.asm
6502.Net.exe myasm.asm --list=listing.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-a</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-assembly</code></td></tr>
<tr><td><b>Definition</b></td><td>Suppress assembled bytes from assembly listing.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -a -L mylist.asm
6502.Net.exe myasm.asm --no-assembly --list=mylist.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-d</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-disassembly</code></td></tr>
<tr><td><b>Definition</b></td><td>Suppress disassembly from assembly listing.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -d -L mylist.asm
6502.Net.exe myasm.asm --no-disassembly --list=mylist.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-s</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--no-source</code></td></tr>
<tr><td><b>Definition</b></td><td>Do not list original source in the assembly listing.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe myasm.asm -s -L mylist.asm
6502.Net.exe myasm.asm --no-source --list=mylist.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--verbose-asm</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Make the assembly listing verbose. If the verbose option is set then all non-assembled lines are included, such as blocks and comment blocks.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>6502.Net.exe myasm.asm --verbose-asm -L myverboselist.asm</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-V</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--version</code></td></tr>
<tr><td><b>Definition</b></td><td>Print the current version of 6502.Net to console output.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
6502.Net.exe -V
6502.Net.exe --version
</pre>
</td></tr>
</table>

### Error messages

`Assertion Failed` - An assertion failed due to the condition evaluating as false.

`Attempted to divide by zero.` - The expression attempted a division by zero.

`Cannot redefine type to <type> because it is already a type` - The type definition is already a type.

`Cannot resolve anonymous label` - The assembler cannot find the reference to the anonymous label.

`Closure does not close a block` - A block closure is present but no block opening.

`Closure does not close a macro` - A macro closure is present but no macro definition.

`Closure does not close a segment` - A segment closure is present but no segment definition.

`Could not process binary file` - The binary file could not be opened or processed.

`Directive takes no arguments` - An argument is present for a pseudo-op or directive that takes no arguments.

`Encoding is not a name or option` - The encoding selected is not a valid name.

`error: invalid option` - An invalid option was passed to the command-line.

`error: option requires a value` -  An option was passed in the command-line that expected an argument that was not supplied.

`<Feature> is depricated` - The instruction or feature is depricated (this is a warning by default).

`File previously included. Possible circular reference?` - An input file was given in the command-line or a directive was issued to include a source file that was previously include.

`Filename not specified` - A directive expected a filename that was not provided.

`Format is invalid.` - The format string passed to `format()` is not valid

`General syntax error` - A general syntax error.

`Illegal quantity` - The expression value is larger than the allowable size.

`Invalid constant assignment` - The constant could not be assigned to the expression.

`Invalid CPU specified` - An invalid CPU option was given at the command line or in the directive

`Invalid parameter reference` - The macro reference does not reference a defined parameter.

`Invalid Program Counter assignment` - An attempt was made to set the program counter to an invalid value.

`Label is not the leftmost character` - The label is not the leftmost character in the line (this is a warning by default).

`Macro or segment is being called recursively` - A macro or segment is being invoked in its own definition.

`Macro parameter not specified` - The macro expected a parameter that was not specified.

`Macro parameter reference must be a letter or digit` - The macro parameter was in an invalid format.

`Missing closure for block` - A block does not have a closure.

`Missing closure for macro` - The macro does not have a closure.

`Missing closure for segment` - A segment does not have a closure.

`Program Counter overflow` - The program counter overflowed passed the allowable limit.

`Pstring size too large` - The P-String size is more than the maximum 255 bytes.

`Quote string not enclosed` - The quote string was not enclosed.

`Redefinition of label` - A label is redefined or being re-assigned to a new value, which is not allowed.

`Redefinition of macro` - An attempt was made to redefine a macro.

`<Symbol> is not a valid symbol name` - The label or variable has one or more invalid characters.

`Symbol not found` - The expression referenced a symbol that was not defined.

`Index (zero based) must be greater than or equal to zero and less than the size of the argument list.` - A format item in the format string passed to `format()` does not match the parameter.

`The current CPU supports only 8-bit immediate mode instructions. The directive will not affect assembly` - Attempted use of the 65816-specific directives (this is a warning by default).

`Too few arguments for directive` - The assembler directive expected more arguments than were provided.

`Too many arguments for directive` - More arguments were provided to the directive than expected.

`Too many characters in character literal` - The character literal has too many characters.

`Type is unknown or not redefinable` - An attempt was made to define an unknown or non-definable type.

`Type name is a reserved symbol name` - A type definition failed because the definition is a reserved name.

`Unable to find binary file` - A directive was given to include a binary file, but the binary file was not found, either due to filesystem error or file not found.

`Unable to open source file` - A source file could not be opened, either due to filesystem error or file not found.

`Unknown architecture specified` - An invalid or unknown parameter was supplied to the `--arch` option in the command-line.

`Unknown instruction or incorrect parameters for instruction` - An directive or instruction was encountered that was unknown, or the operand provided is incorrect.

`Unknown or invalid expression` - There was an error evaluating the expression.
