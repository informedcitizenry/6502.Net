# Memory and Addressing

## Program Counter (*) Symbol

As code is generated, the assembler internally keeps track of the program counter, which can be changed and referenced using the `*` symbol.

```
    * = $c000
    lda score
```

The `.org` directive serves the same purpose.

```
        .org $c100
        clc
```

All [label](/Docs/SymbolsAndScopes.md#Labels) addresses are calculated relative to the program counter value, and so each label following that update will be an offset of that.

When the program counter is manually set after code has generated, the gap will be automatically filled.

## Banks

Some systems (for example the Commodore 128 and 65816-based systems) are capable of running programs beyond the 16-bit address space. The `.bank` directive is available to get around this limitation.

```
        .bank $ff
        // effectively our program counter is $ff0000
```

When the bank value is set, by default the existing program counter value is unchanged. The option `--reset-pc-on-bank` will reset the program counter every time a `.bank` directive is encountered.

If a 24-bit address is encountered but the upper eight bits match the bank value, then the address is effectively is treated as a 16-bit address.

```
        .bank $12
        jsr $123456 // assembles as 20 56 34
```

## Sections

The programmer might want to predefine code sections for organizational and readability purposes. A section can be defined with the `.dsection` directive, or the `--dsection` option. A section definition requires a name in the form of a string, followed by a start address and optional end address. The start must be within the 64KiB addressing range. If the end addess is specified it must be greater than the start address, and is considered the first address outside the bounds of the section addressing range. Otherwise the section end is considered to be the end of the 64KiB address space.

```
    .dsection "zp",$02,$100 // "zp" section starts at $02 and ends at $100 exclusive
```

All sections must be defined before they are used, and before the program counter is advanced by any other means, either by code generation, selecting a defined section, or setting the program counter directly. Section address ranges cannot overlap.

To assign code to a section use the `.section` directive followed by the section name. Sections in source can be in any order, but a section can only be set once. Any gaps between assembled sections will be filled.

```
    .dsection "zp",     $02, $100
    .dsection "bss",  $2000,$8000
    .dsection "data", $8000,$a000
    .dsection "text", $c000

    .section "text"
    lda zpvar1
    sta screen
    sta colors

    .section "data"
message .cstring "SOME MESSAGE"

    .section "zp"
zpvar1 .byte ?
zpvar2 .byte ?
zpvar3 .byte ?
    
    .section "bss"
screen  .fill 1000
        .align 256
colors  .fill 1000
```

The program counter can be assigned within a section, but any assembly or re-assignment to the program counter that causes it to cross the section boundary (except with the `.relocate` directive) will cause an error.

## Relocatable Code

While assembly occurs, two internal program counters are kept, a real program counter that is the absolute offset from 0, and a "logical", or virtual program counter, to which all symbolic addresses are resolved.

Usually both are synchronized, but the logical program counter can be changed with `.relocate` without affecting the absolute offset in the program output, and then later resynchronized using `.endrelocate`. Therefore, this directive is primarily useful for programs that relocate parts of themselves to different memory spaces.

The example below illustrates how these directives work.

```
            * = $0801
            // create a Commodore BASIC stub
            // 10 SYS2061
SYS         = $9e
            .word eob, 10
            .cstring SYS, format("{0}", start)
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
program_end .endrelocate  // program_end is set to the "real" program end
            nop
            /* outputs the following =>
            >0801 0b 08 0a 00
            >0805 9e 32 30 36 31 00
            >080b 00 00       eob
            .080d a2 00       start     ldx #0
            .080f bd 1b 08    -         lda highcode,x
            .0812 9d 00 c0              sta $c000,x
            .0815 e8                    inx
            .0816 d0 f7                 bne -
            .0818 4c 00 c0              jmp $c000
            .c000.            highcode
            .c000 a2 00                 ldx #$00
            .c002 bd 0f c0    printloop lda message,x
            .c005 f0 07                 beq done
            .c007 20 d2 ff              jsr $ffd2
            .c00a e8                    inx
            .c00b 4c 02 c0              jmp printloop
            .c00e 60          done      rts
            ;; message
            >c00f 48 45 4c 4c 4f 2c 20 48
            >c017 49 47 48 20 43 4f 44 45
            >c01f 21 00  
	    .083b ea                    nop
            */
```

While it is good practice, using the `.endrelocate` directive is not required.

Note that the directive `.pseudopc` is an alias of `.relocate` and `.realpc` is an alias of `.endrelocate`.

## Guarding Against Page Boundary Crossings

There is a potential performance penalty when code cross page values (multiples of 256). To ensure certain blocks of code stay within the same page, use the `.page` and `.endpage` directives.

```
        .page
-       ldx #0
        nop
        nop
        jmp - 
        .endpage
```

In the code above, if in fact the jmp instruction (including its operands) occurred across a page boundary, the assembler would report an error.

## Initializing Memory

Whenever there are gaps between parts of code, whether due to an alignment or re-assignment of the program counter through one of the methods described above, the output is initialized to 0. This behavior can be changed with the `.initmem` directive.

```
        .initmem $ff // gaps will fill with $ff
        lda #$80
        jsr $ffef
        .align 8
        rts
        /* output will be:
        a9 80 20 ef ff ff ff ff ff 60
        */ 
```

## Transforming Output By XORing Bytes

Output can be transformed for each byte with `.eor` directive.

```
        .eor $ff
        jsr $ffd2
        /* output is:
        df 2d 00
        */
```

This can be a nice quick code obfuscation trick. 
