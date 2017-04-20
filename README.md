# 6502.Net, A Simple .Net-Based 6502 Cross-Assembler

## Introduction

The 6502.Net Macro Assembler is a simple cross-assembler targeting the MOS 6502 and related CPU architectures. It is written for .Net (Version 4.5.1) and supports all of the published (legal) instructions of 6502-based CPUs. The MOS 6502 was a popular choice for video game system and microcomputer manufacturers in the 1970s and mid-1980s, due to its cost and efficient design. Among hobbyists and embedded systems manufacturers today it still sees its share of use. 

## Quick Overview

The 6502.Net assembler is simple to use. Invoke it from a command line with the assembly source and (optionally) the output filename in the parameters. For instance, a `/6502.Net myprg.asm` command ill output assembly listing in `myprgm.asm` to binary output. To specify output file name use the `-o <file>` or `--output=<file>` option, otherwise the default output filename will be `a.out`.

You can specify as many source files as assembly input as needed. For instance, `/6502.Net mylib.asm myprg.asm` will assemble both the `mylib.asm` and `myprgm.asm` files sequentially to output. Be aware that if both files define the same symbol an assembler error will result.

## General Features
### Mathematical and Numerical Expressions

Integral constants can be expressed as decimal, hexadecimal, and binary. Decimal numbers are written as is, hex are prefixed with a `$` and binary are prefixed with a `%`. Constant characters are enclosed in single-quotes:
```
            65490 = 65490
            $ffd2 = 65490
%1111111111010010 = 65490
              'E' = 69
```
Negative numbers are assembled according to twos-complement rules, with the highest bits set. Binary strings can alternatively be expressed as `.` for `0` and `#` for `1`, which is helpful for laying out pixel data:
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
Operands can also be mathematical expressions, even referencing labels. Math expressions can be nested in paranetheses, except in some limited cases (`lda ($02,x)` or `jmp ($ea31)`, for instance). Several operations are available.
### Binary Operations
<table>
<tr><th>Operator</th><th>Meaning</th></tr>
<tr><td>+</td><td>Add</td></tr>
<tr><td>-</td><td>Subtract</td></tr>
<tr><td>*</td><td>Multiply</td></tr>
<tr><td>/</td><td>Divide</td></tr>
<tr><td>%</td><td>Modulo (remainder)</td></tr>
<tr><td>**</td><td>Raise to the power of</td></tr>
<tr><td>&amp;</td><td>Bitwise AND</td></tr>
<tr><td>|</td><td>Bitwise OR</td></tr>
<tr><td>^</td><td>Bitwise XOR</td></tr>
<tr><td>&lt;&lt;</td><td>Bitwise left shift</td></tr>
<tr><td>&gt;&gt;></td><td>Bitwise right shift</td></tr>
</table>

### Unary Operations
<table>
<tr><th>Operator</th><th>Meaning</th></tr>
<tr><td>~</td><td>Bitwise complementary</td></tr>
<tr><td>&lt;</td><td>Least significant byte</td></tr>
<tr><td>&gt;</td><td>Most significant (second) byte</td></tr>
<tr><td>^</td><td>Bankbyte (third byte)</td></tr>
</table>

Expressions evaluate internally as 64-bit signed integers, but **must** fit to match the expected operand size.
```
    lda #4*23       ; okay
    sta $1000<<8    ; will not assemble!
```
There are several math functions that can also be called as part of the expressions. 
```
    lda #sqrt(25)
```
See the section below on functions for a full list of available functions.

In addition, certain assembler directives expect conditional expressions. Compound conditions are allowed. C-style operators are used:

<table>
<tr><th>Operator</th><th>Meaning</th></tr>
<tr><td>&lt;</td><td>Less than</td></tr>
<tr><td>&lt;=</td><td>Less than or equal to</td></tr>
<tr><td>==</td><td>Equal to</td></tr>
<tr><td>&gt;=</td><td>Greater than or equal to</td></tr>
<tr><td>&gt;</td><td>Greater than</td></tr>
<tr><td>!=</td><td>Not equal to</td></tr>
<tr><td>&amp;&amp;</td><td>Logical AND</td></tr>
<tr><td>||</td><td>Logical OR</td></tr>
<tr><td>!</td><td>Logical NOT</td></tr>
</table>

## Comments

Comments can be added to source code in one of two ways, as single-line trailing source code, or as a block. Single-line comments start with a semi-colon. Any text written after the semi-colon is ignored.
```
    lda #0      ; 0 = color black
    sta $d020   ; set border color to accumulator
    lda #';'    ; the first semi-colon is a char literal so will be assembled
    jsr $ffd2   ; not a comment.
```
Block comments span multiple lines, enclosed in .comment and .endcomment directives. These are useful when you want to exclude unwanted code:
```
    .comment
    
    this will set the cpu on fire do not assemble!
    
    lda #$ff
    sta $5231

    .endcomment
```
## Labels and Symbols

When writing assembly code, it is preferable to use labels for branches and data than to use hard-coded addresses. Labels can be used to define constants, and as jump points in code. There is no restriction on name size, but all labels must begin with an underscore or letter, and can only contain underscores, letters, and digits, and they cannot be re-assigned:
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
In addition to explicit blocks, any label with a leading underscore is considered "local" to the most recent label without an underscore
```
printmessage     ldx #0
_loop           lda msg_ptr,x       ; _loop is local to printmessage
                beq _done           ; _done is local to printmessage
                jsr chrout
                inx
                bne _loop           ; will jump to printmessage's _loop
_done           rts                 

colormessage    ldx #0
_loop           lda color_ptr,x     ; This _loop is local to colormessage
                beq _done           ; as is this _done
                ...
```
Anonymous labels allow one to do away with the need to think of unique label names altogether. There are two types of anonymous labels: forward and backward. Forward anonymous labels are declared with a `+`, while backward anonymous labels are declared using a `-`. They are forward or backward to the current assembly line. They are referenced in the operand with one or more `+` or `-` symbols:
```
printmessage    ldx #0
-               lda msg_ptr,x
                beq +               ; jump to first forward anonymous from here
                jsr chrout
                inx
                bne -               ; jump to first backward anonymous from here
+               rts
-               nop
                jmp --              ; jump to the second backward anonymous from here
```
As you can see anonymous labels, though convenient, would hinder readability if used too liberally. They are best for small branch jumps, though can be used in the same was as labels:
```
-               .byte $01, $02, $03
                lda (-),x           ; best to put anonymous label reference inside paranetheses.
```
## Non-code (data) assembly

In addition to 6502 assembly, data can also be assembled. If the value given in the expression exceeds the data size, this will cause an illegal quantity error. The following pseudo-ops are available:

<table>
<tr><th>Directive</th><th>Size</th></tr>
<tr><td><code>.byte</code></td><td>One byte unsigned</td></tr>
<tr><td><code>.char</code></td><td>One byte signed</td></tr>
<tr><td><code>.addr</code></td><td>Two byte address</td></tr>
<tr><td><code>.sint</code></td><td>Two bytes signed</td></tr>
<tr><td><code>.word</code></td><td>Two bytes unsigned</td></tr>
<tr><td><code>.rta</code></td><td>Two byte return address</td></tr>
<tr><td><code>.lint</code></td><td>Three bytes signed</td></tr>
<tr><td><code>.long</code></td><td>Three bytes unsigned</td></tr>
<tr><td><code>.dint</code></td><td>Four bytes signed</td></tr>
<tr><td><code>.dword</code></td><td>Four bytes unsigned</td></tr>
<tr><td><code>.align</code></td><td>Zero or more bytes</td></tr>
<tr><td><code>.fill</code></td><td>One or bytes</td></tr>
<tr><td><code>.repeat</code></td><td>One or more bytes</td></tr>
</table>

Multi-byte directives assemble in little-endian (least significant byte first). Data is comma-separated, and each value can be a constant or expression:
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
For `.fill` and `.align`, the assembler accepts either one or two arguments. The first is the quantity, the second is the value. If the second is not given then it is assumed to be uninitialized data (see below). For `.fill`, quantity is number of bytes, for `.align` it is the number of bytes by which the program counter can be divided with no remainder:
```
unused      .fill 256,0 ; Assemble 256 bytes with the value 0

atpage      .align 256  ; The program counter is guaranteed to be at a page boundary
```
Sometimes it is desirable to direct the assembler to make a label reference an address, but without assembling bytes at that address. For instance, this is useful for program variables. Use the "?" instead of an expression:
```
highscore   .dword ?    ; set the symbol highscore to the program counter,
                        ; but do not output any bytes 
```                             
Note that if uninitialized data is defined, but thereafter initialized data is defined, the output will fill bytes to the program counter from the occurrence of the uninitialized symbol:
```
highscore   .dword ?    ; uninitialized highscore variables
            lda #0      ; The output is now 6 bytes in size 
``` 
In addition to integral values, 6502.Net can handle text strings. All strings are enclosed in double-quotes. Escapes are not recognized, so embedded quotes must be "broken out":
```
"He said, ",'"',"How are you?",'"'
```
Strings can be assembled in a few different ways, according to the needs of the programmer. 

<table>
<tr><th>Directive</th><th>Meaning</th></tr>
<tr><td><code>.string</code></td><td>A standard string literal</td></tr>
<tr><td><code>.cstring</code></td><td>A C-style null-terminated string</td></tr>
<tr><td><code>.lsstring</code></td><td>An ASCII string left-shifted with the low bit set on its final byte</td></tr>
<tr><td><code>.nstring</code></td><td>A string with the negative (high) bit set on its final byte</td></tr>
<tr><td><code>.pstring</code></td><td>A Pascal-style string, its size in the first byte</td></tr>
</table>

Since `.pstring` strings use a single byte to denote size, no string can be greater than 255 bytes. Since `.nstring` and `.lsstring` make use of the high and low bits, bytes must not be greater in value than 127, nor less than 0. 

A special function called `str()` will convert an integral value to its ASCII equivalent in bytes:
```
start       = $c000

startstr    .string str(start) ; assembles as $34,$39,$31,$35,$32
                               ; literally the digits "4","9","1","5","2"
```                  
## Addressing model

By default, programs start at address 0, but you can change this by setting the program counter before the first assembled byte. 6502.Net uses the `*` symbol for the program counter. The assignment can be either a constant or expression:
```
                * = ZP + 1000       ; program counter now 1000 bytes offset from the value of the constant ZP
```                
(Be aware of the pesky trap of trying to square the program counter using the `**` operator, i.e. `***`. This produces unexpected results. Instead consider the `pow()` function as described in the section on math functions below.)

As assembly continues, the program counter advances automatically. You can manually move the program counter forward, but keep in mind doing so will create a gap that will be filled if any bytes are added to the assembly from that point forward. For instance:
```
                * = $1000
                
                lda #0
                jsr $1234
                    
                * = $2004
                
                brk
```                
Will output 4096 bytes, with 4091 zeros. So this generally is not recommended unless this is the desired result. 

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
                jsr relocate
                ...
                jmp finish
torelocate:
                relocate newlocation
                
                ...
                
                .endrelocate
                ;; done with movable code, do final cleanup
finish          rts
```
## Macros and segments

One of the more powerful features of the 6502.Net cross assembler is the ability to re-use code segments in multiple places in your source. You define a macro or segment once, and then can invoke it multiple times later in your source; the assembler simply expands the definition where it is invoked as if it is part of the source. Macros have the additional benefit of allowing you to pass parameters, so that the final outputted code can be easily modified for certain contexts. For instance, one of the more common operations in 6502 assembly is to do a 16-bit increment. You could use a macro for this purpose like this:
```
inc16   .macro  address
        inc \address
        bne +
        inc \address+1
+       .endmacro
```
The macro is called `inc16` and takes a parameter called `address`. The code inside the macro references the parameter with a backslash `\` followed by the parameter name. The parameter is a textual subsitution; whatever you pass will be expanded at the reference point. Note the anonymous forward symbol at the branch instruction will be local to the block, as would any symbols inside the macro definition when expanded. To invoke a macro simply reference the name with a `.` in front:
```
myvariable .word ?

        .inc16 myvariable
```        
This macro expands to:
```
        inc myvariable
        bne +
        inc myvariable+1
+       ...
```
Segments are conceptually identical to macros, except they do not accept parameters and are usually used as larger segments of relocatable code. Segments are defined between `.segment`/`.endsegment` blocks with the segment name after each closure directive.
```
        .segment zp

zpvar1  .word ?
zpvar2  .word ?
        ...
        .endsegment zp
        
        .segment code
        ldx #0
+       lda message,x
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
        .cerror * > $ff, ".zp segment outside of zero-page!"
        
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
## Details
### Instruction set

At this time, 6502.Net only recognizes the 151 published instructions of the original MOS Technology 6502. Illegal opcodes must be invoked using the pseudo-ops .byte, .word, etc. The following mnemonics are legal:
<pre>
adc,and,asl,bcc,bcs,beq,bit,bmi,bne,bpl,brk,bvc,bvs,
clc,cli,clv,cmp,cpx,cpy,dec,dex,dey,eor,inc,inx,iny,
jmp,jsr,lda,ldx,ldy,lsr,nop,ora,pha,php,pla,plp,rol,
ror,rti,rts,sbc,sbc,sec,sed,sei,sta,stx,sty,tax,tay,
tsx,txa,txs,tya
</pre>

### Pseudo-Ops

Following is the detail of each of the 6502.Net pseudo operations, or psuedo-ops. A pseudo-op is similar to a mnemonic in that it tells the assembler to output some number of bytes, but different in that it is not part of the CPU's instruction set. For each pseudo-op description is its name, any aliases, a definition, arguments, and examples of usage. Optional arguments are in square brackets (`[` and `]`).

Note that every argument, unless specified, can be any legal mathematical expression, and can include symbols such as labels (anonymous and named) and the program counter. Anonymous labels should be referenced in parantheses, otherwise the expression engine might misinterpret them. If the expression evaluates to a value greater than the maximum value allowed by the pseudo-op, the assembler will issue an illegal quantity error.

#### Data/text insertions

<table>
<tr><td><b>Name</b></td><td><code>.addr</code></td></tr>
<tr><td><b>Alias</b></td><td><code>.word</code></td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned 16-bit value or values between 0 and 65535 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address[, address2[, ...]]</code></td></tr>
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
<tr><td><b>Arguments</b></td><td><code>filename[, offset[, size]]</code></td></tr>
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
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      * = $033c
      .byte $39, $38, $37, $36, $35, $34, $33, $32, $31
      ;; >033c 39 38 37 36 35 34 33 32 
      ;; >0344 31
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.char</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned byte-sized value or values between -128 and 127 into the assembly. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $033c
        .char 127, -3  ; >033c 7f fd
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.cstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a C-style null-terminated string into the assembly. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order. The text encoding can be controlled using the <code>.enc</code> directive. By default text is treated as ASCII.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
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
<tr><td><b>Definition</b></td><td>Insert a signed 32-bit value or values between −2147483648 and 2147483647 into the assembly, little-endian Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0801
        .dint   18000000      ; >0801 80 a8 12 01
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.dword</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 32-bit value or values between 0 and 4294967295 into the assembly, little-endian Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0801
        .dword  $deadfeed     ; >0801 ed fe ad de
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.fill</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Fill the assembly by the specified amount. Similar to align, if only one argument then space is merely 
reserved. Otherwise the optional second argument indicates the assembly should be filled repeated amounts of the expression, little-endian, if more than one byte in size.</td></tr>
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
<tr><td><b>Definition</b></td><td>Insert a signed 24-bit value or values between -8388608 and 8388607 into the assembly, little-endian Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c100
        .lint   -80000    ; >c100 80 c7 fe 
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.long</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 24-bit value or values between 0 and 16777215 into the assembly, little-endian Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c100
        .long   $ffdd22   ; >c100 22 dd ff
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.lsstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly, each byte shifted to the left, with the lowest bit set on the last byte. See example of how this format can be used. If the highest bit in each value is set, the assembler will error. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order. The text encoding can be controlled using the <code>.enc</code> directive. By default text is treated as ASCII.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        ldx #0
+       lda message,x
        lsr a               ; shift right
        php                 ; save carry flag
        jsr chrout          ; print 
        plp                 ; restore carry flag
        bcs done            ; if set we printed last char
        inx                 ; increment pointer
        jmp +               ; get next
        ...
.enc petscii    ; turn on petscii-encoding
        * = $c100
message .lsstring "hello"   ; >c100 90 8a 98 98 9f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.nstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly, the negative (highest) bit set on the last byte. See example of how this format can be used. If the highest bit on the last byte is already set, the assembler will error. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order. The text encoding can be controlled using the <code>.enc</code> directive. By default text is treated as ASCII.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        ldx #0
+       lda message,x
        php                 ; save negative flag
        and #%01111111      ; turn off high bit...
        jsr chrout          ; and print 
        plp                 ; restore negative flag
        bmi done            ; if set we printed last char
        inx                 ; else increment pointer
        jmp +               ; get next
        ...
        * = $c100
message .nstring "hello"    ; >c100 68 65 6c 6c ef
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.pstring</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a Pascal-style string into the assembly, the first byte indicating the full string size. Note this size includes all arguments in the expression. If the size is greater than 255, the assembler will error. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order. The text encoding can be controlled using the <code>.enc</code> directive. By default text is treated as ASCII.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $4000
        .pstring $23,$24,$25,$26,1024 ; >4000 06 23 24 25 26 00 04
        .pstring "hello"              ; >4007 05 68 65 6c 6c 6f
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.repeat</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Repeat the specified number of times the specified value. Similar to fill, except that both arguments are required, the first argument the number of times to repeat the expression in the second argument, little-endian if more than one byte in size.</td></tr>
<tr><td><b>Arguments</b></td><td><code>amount, repeatvalue</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0400
        .repeat 4,$ea       ; >0400 ea ea ea ea
        .repeat 3,$ffd2     ; >0404 d2 ff d2 ff d2 ff
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.rta</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert an unsigned 16-bit value or values between 0 and 65535 into the assembly. Similar to <code>.addr</code> and <code>.word</code>, except the value is decremented by one, yielding a return address. This is useful for building "rts jump" tables. Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>address[, address2[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
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
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.sint</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a signed 16-bit value or values between -32768 and 32767 into the assembly, little-endian Multiple arguments can be passed as needed. If <code>?</code> is passed then the data is uninitialized.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $c000
mysub   lda #13             ; output newline
        jsr chrout
        rts
        .addr mysub         ; >c006 00 c0
</pre>
</td></tr></table>
<table>
<tr><td><b>Name</b></td><td><code>.string</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Insert a string into the assembly. Multiple arguments can be passed, with a null only inserted at the end of the argument list. If <code>?</code> is passed then the data is an uninitialized byte. Enclosed text is assembled as string-literal while expressions are assembled to the minimum number of bytes required for storage, in little-endian byte order. The text encoding can be controlled using the <code>.enc</code> directive. By default text is treated as ASCII.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value[, value[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = 1000
        .string "hello, world!"   ; >1000 68 65 6c 6c 6f 2c 20 77
                                  ; >1008 6f 72 6c 64 21
</pre>
</td></tr>
</table>

#### Assembler directives

<table>
<tr><td><b>Name</b></td><td><code>.binclude</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Include a source file and enclose the expanded source into a scoped block. The specified file is 6502.Net-compatible source. If no name is given in front of the directive then all symbols inside the included source will be inaccessible.</td></tr>
<tr><td><b>Arguments</b></td><td><code>Filename</code></td></tr>
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
<tr><td><b>Name</b></td><td><code>.cerror</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom error to the console if the condition is met. Useful for sanity checks and assertions. The error is treated like any assembler error and will cause failure of assembly. The condition is any logical expression using C-style operators, and can be compound.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition, error</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0800
        nop
        .cerror * > $0801, "Uh oh!" ; if program counter
                                    ; is greater than 2049,
                                    ; raise a custom error
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
<tr><td><b>Name</b></td><td><code>.cwarn</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom warning to the console if the condition is met. The warning is treated like any assembler warning, and if warnings are treated as errors it will cause failure of assembly The condition is any logical expression using C-style operators, and can be compound.</td></tr>
<tr><td><b>Arguments</b></td><td><code>condition, error</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
        * = $0800
        nop
        .warn   * > $0801, "Check bound" 
        ;; if program counter
        ;; is greater than 2049,
        ;; raise a custom warning
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.enc</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td> Set the text encoding, how to interpret character literals. Currently only relevant to Commodore  targets. Three options are available:
<pre>
        petscii - convert ASCII/UTF8 to Commodore PETSCII
        screen  - convert ASCII/UTF8 to Commodore screen codes
        none    - treat as raw ASCII/UTF-8
</pre>
</td></tr>
<tr><td><b>Arguments</b></td><td><code>encoding</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
       * = $4000
      .enc screen
      .byte 'A'                   ; >4000 01
      .cstring "HELLO "           ; >4001 08 05 0c 0c 0f 20 00
      .enc none
      .string "goodbye, cruel"    ; >4008 67 6f 6f 64 62 79 65 2c             
                                  ; >4010 20 63 72 75 65 6c                     
      .enc petscii
      .dword 'a'                  ; >4016 41 00 00 00
      .pstring "world"            ; >401a 05 57 4f 52 4c 44 
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
<tr><td><b>Name</b></td><td><code>.error</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom error to the console Useful for sanity checks and assertions. The error is treated like any assembler error and will cause failure of assembly.</td></tr>
<tr><td><b>Arguments</b></td><td><code>error</code></td></tr>
<tr><td><b>Example</b></td><td>
<code>.error "We haven't fixed this yet!" </code>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.include</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Include a source file into the assembly. The specified file is 6502.Net-compatible source.</td></tr>
<tr><td><b>Arguments</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
      .include "mylib.s"
      ;; mylib is now part of source
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>.macro</code>/<code>.endmacro</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Define a macro that when invoked will expand into source. Must be named. Optional arguments are treated as parameters to pass as text substitutions in the macro source where referenced, with a leading backslash "\" and either the macro name or the number in the parameter list. Parameters can be given default values to make them optional upon invocation. Macros are called by name with a leading "." All symbols in the macro definition are local, so macros can be re-used with no symbol clashes.</td></tr>
<tr><td><b>Arguments</b></td><td><code>parameter[, parameter[, ...]]</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
inc16       .macro
            inc \1
            bne +
            inc \1+1
&#43;       .endmacro
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
eob	        .word 0
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
            ;; outputs the following =>
            .comment
            >0801 0b 08 0a 00           
            >0805 9e 32 30 36 31 00         
            >080b 00 00             
            >080d a2 00     ;           ldx #0
            >080f bd 1b 08  ; -         lda highcode,x
            >0812 9d 00 c0  ;           sta $c000,x
            >0815 e8        ;           inx
            >0816 d0 f7     ;           bne -
            >0818 4c 00 c0  ;           jmp $c000
            >081b a2 00     ;           ldx #$00        
            >081d bd 0f c0  ; printloop lda message,x 
            >0820 f0 07     ;           beq done        
            >0822 20 d2 ff  ;           jsr $ffd2         
            >0825 e8        ;           inx               
            >0826 4c 02 c0  ;           jmp printloop             
            >0829 60        ; done      rts      
            ;; message
            >082a 48 45 4c 4c 4f 2c 20 48       
            >0832 49 47 48 20 43 4f 44 45     
            >083a 21 00  
            .endcomment
</pre>
</td></tr>
</table>
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
            .byte %00001111
            .byte %00111110
            .byte %01111100
            .byte %11111000
            .byte %11111000
            .byte %01111100
            .byte %00111110
            .byte %00001111
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
<tr><td><b>Name</b></td><td><code>.warn</code></td></tr>
<tr><td><b>Alias</b></td><td>None</td></tr>
<tr><td><b>Definition</b></td><td>Prints a custom warning to the console. The warning is treated like any assembler warning, and if warnings are treated as errors it will cause failure of assembly.</td></tr>
<tr><td><b>Arguments</b></td><td><code>error</code></td></tr>
<tr><td><b>Example</b></td><td>
<code>.error "We haven't fixed this yet!" </code>
</td></tr>
</table>

## Future ideas

Some features may be introduced in a future release, such as conditional assembly, for-next loops, and flow control, though there is no plan at this time.

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
<tr><td><b>Example</b></td><td><code>.char floor(-4.8)     ; > fb</code></td></tr>
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
<tr><td><b>Arguments</b></td><td><code>value, places</code></td></tr>
<tr><td><b>Example</b></td><td><code>.byte round(18.21, 0) ; > 12</code></td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sgn</code></td></tr>
<tr><td><b>Definition</b></td><td>The sign of the expression, returned as -1 for negative, 1 for positive, and 0 for no sign.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
     .char sgn(-8.0), sgn(14.0), sgn(0)
     ;; > ff 01 00
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Name</b></td><td><code>sin</code></td></tr>
<tr><td><b>Definition</b></td><td>The sine of the expression.</td></tr>
<tr><td><b>Arguments</b></td><td><code>value</code></td></tr>
<tr><td><b>Example</b></td><td><code>.char sin(1003.9) * 14 ; > f2</code></td></tr>
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
<tr><td><b>Example</b></td><td><code>.byte sqrt(65536) - 1  ; > ff</code></td></tr>
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

6502.Net accepts several arguments, requiring at least one. If no option flag precedes the argument, it is considered an input file. Multiple input files can be assembled. If no output file is specified, source is assembled to `a.out` within the current working directory. Below are the available option flags and their parameters.

<table>
<tr><td><b>Option</b></td><td><code>-o</code></td></tr>
<tr><td><b>Alias</b></td><td>--output</td></tr>
<tr><td><b>Definition</b></td><td>Output the assembly to the specified output file. Quote-enclosed filename is a required parameter.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
/6502.Net myasm.asm -o myoutput
/6502.Net myasm.asm -output=myoutput
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-b</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--nostart</code></td></tr>
<tr><td><b>Definition</b></td><td>Do not set the header of the output file to the start address of the assembly, which is the Commodore DOS format for executables.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>/6502.Net myasm.asm -b -o notcbm.bin</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-C</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--case-sensitive</code></td></tr>
<tr><td><b>Definition</b></td><td>Set the assembly mode to case-sensitive. All tokens, including assembly mnemonics, directives, and symbols, are treated as case-sensitive. By default, 6502.Net is not case-sensitive.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
/6502.Net mycsasm.asm -C
/6502.Net mycsasm.asm --case-sensitive
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-C</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--case-sensitive</code></td></tr>
<tr><td><b>Definition</b></td><td>Set the assembly mode to case-sensitive. All tokens, including assembly mnemonics, directives, and symbols, are treated as case-sensitive. By default, 6502.Net is not case-sensitive.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
/6502.Net mycsasm.asm -C
/6502.Net mycsasm.asm --case-sensitive
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-D</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--define</code></td></tr>
<tr><td><b>Definition</b></td><td>Assign a global label a value. Note that within the source the label cannot be redefined again. The value can be any expression 6502.Net can evaluate at assembly time.</td></tr>
<tr><td><b>Parameter</b></td><td><code>{label}={value}</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>/6502.Net -D chrout=$ffd2 myasm.asm -o myoutput</pre>
</td></tr>
</table>
<table>
<table>
<tr><td><b>Option</b></td><td><code>-h</code></td></tr>
<tr><td><b>Alias</b></td><td><code>-?, --help</code></td></tr>
<tr><td><b>Definition</b></td><td>Print all command-line options to console output.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
/6502.Net -h
/6502.Net --help
</pre>
</td></tr>
</table>
<tr><td><b>Option</b></td><td><code>-l</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--labels</code></td></tr>
<tr><td><b>Definition</b></td><td>Dump all label definitions to listing.</td></tr>
<tr><td><b>Parameter</b></td><td><code>filename</code></td></tr>
<tr><td><b>Example</b></td><td>
<pre>
/6502.Net myasm.asm -l labels.asm
/6502.Net myasm.asm --labels=labels.asm
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
/6502.Net myasm.asm -L listing.asm
/6502.Net myasm.asm --list=listing.asm
</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>--verbose-list</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--list</code></td></tr>
<tr><td><b>Definition</b></td><td>Make listing output verbose. If the verbose option is set then all non-assembled lines are included, such as blocks and comment blocks.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>/6502.Net myasm.asm --verbose-list -L myverboselist.asm</pre>
</td></tr>
</table>
<table>
<tr><td><b>Option</b></td><td><code>-V</code></td></tr>
<tr><td><b>Alias</b></td><td><code>--version</code></td></tr>
<tr><td><b>Definition</b></td><td>Print the current version of 6502.Net to console output.</td></tr>
<tr><td><b>Parameter</b></td><td>None</td></tr>
<tr><td><b>Example</b></td><td>
<pre>
/6502.Net -V
/6502.Net --version
</pre>
</td></tr>
</table>

### Error messages

`Bad expression` - An error in the expression.

`Cannot resolve anonymous label` - The assembler cannot find the reference to the anonymous label.

`Closure does not close a block` - A block closure is present but no block opening.

`Constant expression in LValue` - Expression attempting to assign a value to a constant.

`Could not process binary file` - The binary file could not be opened or processed.

`Directve takes no arguments` - An argument is present for a pseudo-op or directive that takes no arguments.

`Filename not specified` - A directive expected a filename that was not provided.

`General syntax error` - A general syntax error.

`Illegal quantity` - The expression value is larger than the allowable size.

`Invalid constant assignment` - The constant could not be assigned to the expression.

`Invalid parameter reference` - The macro reference does not reference a defined parameter.

`Invalid Program Counter assignment` - An attempt was made to set the program counter to an invalid value.

`Macro or segment is being called recursively` - A macro or segment is being invoked in its own definition.

`Macro parameter not specified` - The macro expected a parameter that was not specified.

`Missing closure for block` - A block does not have a closure.

`Most significant bit should not be set` - A pseudo-op cannot set the most-significant bit because it is already set.

`Program Counter overflow` - The program counter overflowed passed the allowable limit.

`Pstring size too large` - The P-String size is more than the maximum 255 bytes.

`Quote string not enclosed` - The quote string was not enclosed.

`Relative branch out of range` - The relative branch jump was being the allowable 128 bytes.

`Redefinition of label` - A label is redefined or being re-assigned to a new value, which is not allowed.

`Redefinition of macro` - An attempt was made to redefine a macro.

`Too few arguments for directive` - The assembler directive expected more arguments than were provided.

`Too many argumnets for directive` - More arguments were provided to the directive than expected.

`Unknown instruction or incorrect parameters for instruction` - An directive or instruction was encountered that was unknown, or the operand provided is incorrect.
