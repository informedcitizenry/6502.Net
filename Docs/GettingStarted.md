# Getting Started

## Overview

The 6502.Net Assembler is a cross-assembler for several CPUs from the 8-bit era of computing including the Motorola 6800 and 6809, the MOS 6502 and its variants, the Intel 8080, and the Zilog Z80.

6502.Net is a multi-pass assembler that supports user comments, symbolic and macro assembly, as well as advanced features like conditional and iterative assembly. Its dialect and style generally follows that of the [TASS/TMP assemblers](http://turbo.style64.org/), and to a lesser extent [ca65](https://cc65.github.io/doc/ca65.html).

This document does assume basic knowledge of assembly language, though the examples and explanations incorporating assembly code throughout should be easy to follow along even for the less familiar.

## Usage

The executable requires at least one input file as an argument.

```
dotnet 6502.net.dll mygame.asm
```

Several inputs can be specified at a time, processed in the order they are listed. The output filename can be set with the `-o`/`--output` option.

```
6502.net.exe mygame.asm -o mygame.prg
```

If not set, the output filename defaults to `a.out`.

## General Syntax

A typical assembly statement takes the form of:

```
label        instruction        operands
```

This format should feel familiar to anyone who has developed using other assemblers. Labels are optional in statements. Though a common requirement in other assemblers, a label does not to be followed by a colon `:`, though this is allowed. 

## Hello, World

A "Hello, World" example for a Commodore 8-bit computer demonstrates the major features of the language.

```
            // select output format
            .format "cbm"

            // common CBM symbols and routines
            .include "cbmlib.s"

            // macro for a BASIC loader
            .basicstub start 
start:
            ldx #0
-           lda helloMessage,x  // next char
            beq +               // if 0 finish
            jsr CHROUT          // output to screen
            inx
            jmp -
+           rts
helloMessage:
            .cstring "HELLO, WORLD!"
```

Statements are typically terminated by newline characters, but can also be separated inline by colons.

```
    lda #$41:jsr $ffd2:rts
```

There are five conditions under which a newline character does not terminate a statement.

```
// When preceded by a backslash:

    lda \
      #$41
    jsr \
    $ffd2
    rts

// After the operator in an infix expression:

    lda #$40 +
        1

// Inside an expression group, array declaration, or dictionary declaration:
    lda #(
        $41
    ) + 3

// After a comma in an expression or argument list:
    .string "hello",
            "world"

// Inside a multiline string:
   .string \
"""
    PRESS <RETURN>
    TO BEGIN
 """
```

## Comments

### Single-line

All characters after the first occurrence of `//` or `;` are treated as comments and are ignored until the newline:

```
    lda #$41  ;load character 'A'
    jsr $ffd2 // output to screen
```

### Multiline

All characters between `/*` and `*/`, including newline characters, are comments and are not processed.

```
    /* 
    the following code
    will print the letter "A"
    to the screen
    */
    lda #$41
    jsr $ffd2
```

## Case-sensitivity

By default, 6502.Net processes text case-insensitive.

```
    // both forms are acceptable
    lda #$41
    LDA #$41
```

Passing command-line option `-C` forces all tokens to be evaluated case-sensitive. This is useful for instance when you want to create symbols that are the same name but vary by case.

```
// with -C the two labels are different
Loop   lda #$41
loop   jsr $ffd2
```

## Other Topics

* [Memory And Addressing](/Docs/MemoryAndAddressing.md)
* [CPU Specific Assembly](/Docs/CPUSpecificAssembly.md)
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

