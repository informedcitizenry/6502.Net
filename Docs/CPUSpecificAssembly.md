# CPU Specific Assembly

## CPUs Supported

6502.Net supports 16 CPU types (including 12 variants of the venerable 6502). The full list of supported CPUs by name and family are:

| Name      | Family	| Notable usage                                 |
|-----------|-----------|-----------------------------------------------|
| 45GS02	| 65xx	    | Commodore 65 (Unreleased C64 successor)       |
| 6502	    | 65xx	    | Many popular 8-bit computer & arcade games    |
| 6502i	    | 65xx	    | Undocumented 6502 instructions                |
| 65C02	    | 65xx	    | Apple IIc, Atari Lynx                         |
| 65CE02	| 65xx	    | Amiga serial port                             |
| c64dtv2   | 65xx	    | Commodore 64 on a chip                        |
| gb80      | i8080/z80 | Nintendo Game Boy                             |
| HuC6280   | 65xx	    | NEC PC Engine/Turbografix-16                  |
| i8080	    | i8080/z80 | S80-based micros (MITS Altair, IMSAI 8080)    |
| m65	    | 65xx	    | Mega65, an open source port of the C65        |
| m6800	    | m680x	    | Tektronix 4500 series                         |
| m6809	    | m680x	    | Tandy Color Computer & arcade games           |
| R65C02	| 65xx	    | Customized 65C02 used in Rockwell modems      |
| W65C02	| 65xx	    | WDC update to the 65C02                       |
| z80	    | i8080/z80	| Many popular 8-bit computers & arcade games   |

## Setting the Target CPU

There are two methods to specify which CPU (or family of CPUs) the source code target. The first and most straightforward is at the command line using the `-c`/`--CPU` option.

```
~$ dotnet 6502.Net.dll speccyprog.s -o speccyprog.bin --cpu=z80
```

The target cpu can also be set in source itself using the `.cpu` directive. 

```
            .cpu "65C02"
            ldx #0
            bra near_routine
```

This directive is particularly useful if you have a mix of source from different CPUs within the same family.

For non-65xx CPU types, this directive must be a top level directive, otherwise the assembler assumes the source is 65xx-based.

```
            lda #0
            .cpu "z80" // this will result in a syntax error
            call chrout 
```

## Addressing Mode Sizes

Because the 65xx and m680x CPUs use differing addressing modes for the same mnemonics, by default 6502.Net selects the appropriate instruction based on the minimum required size to express the operand. For instance the operand in `lda 42` can either be interpreted to be a zero-page or absolute address, but 6502.Net will choose zero-page. Similarly, for the 65C816 the operand in `lda $c000` could either be an absolute or long address, but 6502.Net will again choose the shorter instruction to assemble.

You can change this behavior explicitly by pre-fixing the operand with the width in bits of the desired addressing mode enclosed in square brackets.

```
            * = $c000
            // zero-page loadA
            lda 42          // > .c000 a5 2a

            // ensures that zpsym is only 8 bits else
            // the assembler will report a size error
            lda [8] zpsym   

            // absolute loadA
            lda [16] 42     // > .c002 ad 2a 00

            // long jsr to bank 0 $ffd2 for 65816
            jsr [24] $ffd2  // > .c005 22 d2 ff 00
```

## 65xx Far Branch Pseudo Mnemonics

For convenience, far branch instructions are available to the programmer, where the distance is calculated to the offset relative to the current instruction before assembly, and if the branch is too far, an absolute jump is created. The pseudo-mnemonics for these long branches are `jcc`, `jcs`, `jeq`, `jmi`, `jne`, `jpl`, `jvc`, and `jvs`.

```
            * = $c000
            lda ($19),y
            jne $c100
            ldx #0
            ...
            /* gets assembled as =>
            .c000  b1 19            lda ($19),y
            .c002  f0 03 4c 00 c1   beq $c007:jmp $c100
            .c007  a2 00	        ldx #$00
            */
```

There is one additional pseudo branch available for 6502 mode when the option `--enable-branch-always` is set. This re-interprets the `bra` mnemonic as `bvc`, which is effectively a branch always instruction.

Naturally, these pseudo-branch mnemonics break compatibility with nearly all other assemblers.

## 6502i Mnemonics and Addressing Modes

The 6502i is just the 6502 but with extra mnemonics for undocumented/illegal instructions. 6502.Net closely follows those used by [VICE](https://vice-emu.sourceforge.io/), a popular Commodore 64 emulator.

## 65816 Immediate Mode Options

Immediate addressing in 65816 mode can emit different output based on the `p` status register. Eight-bit modes for registers are default, but 6502.Net can either infer the size when assembling `rep` and `sep` operations, or explicitly be told which size to use for which register in order to assemble the correct number of bytes for immediate mode operations.

For automatically attempting to infer the register size, you can pass the command-line option `--autosize-registers` or include the `.auto` directive in your source. Conversely, the `.manual` directive turns this behavior off.

```
            lda #$c0
            .auto
            rep #%0010_0000
            lda #$c000
```

The `.m8` and `.m16` directives specify explicitly whether accumulator immediate modes are eight or sixteen bits, while `.x8` and `.x16` specify the same for index immediate modes.

```
            .m16
            lda #$c0 // will assemble as lda #$00c0

            .x16
            ldy #$00 // > ldy #$0000
```

To set the modes for accumulator and index registers at once, use the `.mx8` or `.mx16` directives.

```
            .m16
            lda #$c0 // > lda #$00c0
            ldy #$00 // > ldy #$0000
```

## 65816 and Motorola 6809 Direct Page Addressing

Since both the 65816 and 6809 have direct page addressing, the `.dp` directive can tell the assembler how to calculate addresses, whether as direct page or absolute/extended addresses.

```
            .cpu "65816"
            .dp $ff
            lda $ffd2 // > 96 d2
            .dp 0	
            lda $ffd2 // > b6 ff d2
```

For the 6809 specifically, the pseudo instructions `.tfradp` and `.tfrbdp` combine setting either the A or B register and transfering to the direct page register.

```
            .tfradp $42
            /*
            same as:
            lda #$42
            tfr a,dp
            .dp $42
            */
```

## M65 Quad Modes

The MEGA65-based variant of the 65CE02 adds long and quad support through several new mnemonics:

```
adcq
aslq
andq
cpq
deq
eom
eorq
inq
ldq
lsrq
neg
orq
rolq
rorq
sbcq
stq
```

The conventions used to distinguish between base 6502/65CE02 mode and long/quad mode variants follows that of the [ACME](https://sourceforge.net/projects/acme-crossass/) cross-assembler.

```
            lda ($93),y // base 6502 indirect-indexed mode, assembles to: b1 93
            lda [$93],z // long mode, assembles to: ea b2 93
            ldq ($93),y // quad mode with new mnemonic, assembles to: 42 42 b1 93
            ldq [$93]   // quad-long mode, assembles to: 42 42 ea b2 93
```

Note also the `nop` mnemonic is unavailable in m65 mode because the opcode `$ea` is used to indicate long mode to the CPU.

## GB80 Indirect and IO-addressing

The LR35902 CPU used in the Game Boy is a variant of the Z80 that lacks certain addressing modes, such as the indexed modes. It also does not support the `in` and `out` IO commands. Due to this limitation, the Game Boy instead maps the upper page of memory for IO devices. Assemblers typically use the convention of addressing adding the high page address either to an offset or the `C` register. 6502.Net mimics this same form:

```
        ld ($ff00+c),a   // same as 'out (c), a', assembles to: e2
        ld a,($ff00+$1f) // same as 'in a,($1f)', assembles to: f0 1f
```

The high page offset can be a full expression, so though the form `$ff00` is recommended for clarity it is not required.

Most assemblers targeting this platform use square brackets `[`/`]` to denote indirect addressing. As reflected in the example above, 6502.Net instead requires parentheses to enforce consistency with z80 style. 

## Z80 Implied Mode

All of the Z80 instructions that are implied to work on the accumulator, such as `sub b`, can also be written as `sub a,b`. Similarly, the logical operations, such as `xor a` can also be written as `xor a,a`.

## Other Topics

* [Getting Started](/Docs/GettingStarted.md)
* [Memory And Addressing](/Docs/MemoryAndAddressing.md)
* [Data Generation](/Docs/DataGeneration.md)
* [Expressions](/Docs/Expressions.md)
* [Symbols And Scopes](/Docs/SymbolsAndScopes.md)
* [Built-In Symbols](/Docs/BuiltInSymbols.md)
* [Output And Listings](/Docs/OutputAndListings.md)
* [Control Assembly](/Docs/ControlAssembly.md)
* [Macros](/Docs/Macros.md)
* [File Inclusions](/Docs/FileInclusions.md)
* [Diagnostics](/Docs/Diagnostics.md)
* [Disassembler](/Docs/Disassembler.md)
* [Command-line Options](/Docs/CommandLineOptions.md)
* [Technical Info](/Docs/TechnicalInfo.md)