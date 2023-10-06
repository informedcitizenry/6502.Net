# Command Line Options

Most options have a simple form, a single - followed by a character, though several have longer, alternate forms, with two -- and a name.

For options that expect an argument, the argument can either follow the option token or appear as an assignment.

```
6502.net myprog.asm -o myprog.prg
6502.net myprog.asm -o=myprog.prg
```

## Output Options

**`-a`**, **`--no-assembly`**

For the listing file, do not generate assembly bytes.

Example listing:

```
.c000                    lda #$00          LDA #ZERO // reset .A
```

**`-d`**, **`--no-disassembly`**

For the listing file, do not generate a disassembly.

Example listing:

```
.c000     a9 00                            LDA #ZERO // reset .A
```

**`--disassemble--`**

Disassemble the input file, treating the input as binary machine code, and outputting the disassembly to the specified output file.

**`--disassembly-start`**

Specify the start address of the disassembly output if the `--format` option is not set. Otherwise disassembly assumes the binary is CBM, with the address in the header.

**`--disassembly-offset`**

Specify the offset in the input binary to begin disassembly.

**`-E`**

Preprocess source to output stream. **NOTE**: This feature is no longer available.

**`-e`**, **`--error`**

Dump all diagnostic messages to the file specified by the argument.

**`-f`**, **`--format`**

Specify code output format. Setting this option overrides the .format directive in source.

```
dotnet 6502.dot.dll mydisk.asm -o mydisk.d64 -format d64
```

**`-l`**, **`--labels`**

Output all constants and labels to the file specified by the argument.

**`--labels-addresses-only`**

Used with `-l`, it restricts output only to address labels.

**`-L`**, **`--list`**

Output listing file to the file specified by the argument.

Example listing:

```
.c000     a9 00          lda #$00          LDA #ZERO // reset .A
```

**`-o`**, **`--output`**

Specify code output file name. If this option is not given, the default output is `a.out`.

**`--output-section`**

Generate output for the section name in the argument only to the object file.

```
dotnet 6502.net.dll myprog.asm -o myprog.prg --output-section text
```

**`-p`**, **`--patch`**

Patch the existing output file at the offset specified. Mostly for bug fixes. The offset can be any valid constant expression that evaluates to an address.

```
6502.net.exe myfix.asm -o mygame.prg --patch=$452b
```

**`-s`**, **`--no-source`**

For the listing file, do not append the original source.

Example listing:

```
.c000     a9 00          lda #$00
```

**`-t`**, **`--truncate-assembly`**

Truncate the assembly bytes to one line in the listing, rather than expanding to the full series of bytes.

Example listing:

```
>c100     48 45 4c 4c 4f 20 57 4f ...          .string "HELLO WORLD, HOW ARE YOU DOING TODAY?"      
```

**`-v`**, **`--verbose-asm`**

Include all whitespaces, including extraneous newlines, directives and comments, in listing.

Example listing:

```
.c000     a9 00          lda #$00          LDA #ZERO //* here we will
         reset the accumulator before we begin proper execution */
```

**`--vice-labels`**

Used with `-l`. The label listing format conforms to VICE.

```
~$ dotnet 6502.Net.dll myprog.a65 -o myprog.prg -l labels.txt --vice-labels
```

## Directing Assembly

**`--autosize-registers`**

In 65816 mode, whenever a rep or sep is assembled, the assembler will inspect the flags and adjust the register size.

```
    // --autosize-registers
    rep #%0010_0000
    lda #$c000
```

This behavior can be turned off with `.manual`.

**`-b`**, **`--enable-branch-always`**

Allow the mnemonic `bra` for 6502 mode. This will assemble to `bvc`.

**`-C`**, **`--case-sensitive`**

Process all symbols and reserve words case-sensitive. This flag does not affect preprocessor commands, such as .end, .include and .macro.

**`-c`**, **`--cpu`**

Specify the target CPU and instruction set. This option overrides the `.cpu` directive if the directive appears at the header.

```
dotnet 6502.net.dll myz80.s -o myz80.bin --cpu=z80
```

**`-D`**, **`--define`**

Define a constant. The assignment expression itself must be constant.

```
dotnet 6502.net.dll mysource.asm -o myprog.prg --define=DEBUG_MODE=true
```

**`--dsections`**

Define one or more sections. 

```
6502.net myprog.asm -o myprog.prg --dsections zp,$02,$100 himem,$f000
```

**`I`**, **`--include-path`**

Include the path in the argument when attempting to open filenames, for instance with `.binary` or `.include`.

```
6502.dot.exe myprog.asm -o myprog.prg -I C:\Users\informedcitizenry\Projects\asm\shared
```

**`--long-addressing`**

Support long (24-bit) addressing mode. If set, output can exceed the 64KiB boundary that by default would cause an error. Even though long addressing mode can be supported this way, the program counter still can only be assigned values between 0 and 65535.

**`--reset-pc-on-bank`**

Reset the program counter on execution of the `.bank` directive. By default, changing the bank value will not reset the program counter.

```
    * = $c000
    lda #0
    .bank 1
    nop     // nop address essentially is 01:c002
```

This option changes that behavior.

```
    // --reset-pc-on-bank
    * = $c000
    lda #0
    .bank 1
    nop     // nop address essentially is 01:0000
```

Note that, since banks can be set multiple times, this option overwrites previously generated output.

## Diagnostics Options

**`-?`**, **`-h`**, **`--help`**

Display all command-line options.

**`--checksum`**

Display MD5 hash of output if assembly is successful.

**`--echo-each-pass`**

Send output from `.echo` directives on each pass. By default, messages printed by `.echo` to the console are only sent on first pass.

**`--no-highlighting`**

Do not highlight causes of errors or warnings in source when reporting errors and warnings.

**`--no-stats`**

Do not report assembly statistics, such as program start and end addresses, and bytes written.

**`--quiet`**

Do not send any diagnostic or other information when assembling.

**`-V`**, **`--version`**

Report the version of of the 6502.Net assembly.

**`-w`**, **`--no-warn`**

Suppress the display of all warnings.

**`--Wall`**

Enable all warnings. This is a compact alternative to sending these individual flags to the CLI:

```
--Wambiguous-zp, --Wcase-mismatch, --Werror, --Wjump-bug, --Wleft, --Wno-unused-sections, --Wsimplify-call-return, --Wtext-in-non-text-pseudo-ops
```

**`--Wambiguous-zp`**

Warn whenever a statement can either be a zero/direct page or absolute addressing mode.

```
    // --Wambiguous-zp
    lda $20,x // this could also be lda $0020,x, so warn
```

**`--Wcase-mismatch`**

Warn if a symbol reference does not match the case of the definition

```
MyMixedCaseLabel = 3

    lda #mymixedcaselabel // creates warning
```

**`--Werror`**e

Treat all warnings as errors.

**`--Wjump-bug`**

Warn if the assembly generates an indirect jmp bug if the assembler is in 6502 mode.

```
    jmp ($01ff) // warn
```

**`--Wleft`**

Warn when a whitespace precedes the label in a statement.

```
  nonleft lda #0 // warn
```

**`--Wno-unused-sections`**

If any defined sections are not used, do not emit a warning.

```
    .dsection "zp",2,256
    .dsection "text",2049

    .section "text"
    lda #0

    // do not warn if "zp" is never used
```

**`--Wsimplify-call-return`**

If a return instruction immediately follows a call instruction, warn that the two instructions can be combined into a single jump instruction.

```
   // for z80
   call mysub
   ret

   // for 65xx/m68xx
   jsr mysub
   rts
```

**`--Wtext-in-non-text-pseudo-ops`**

Warn if strings are used in pseudo-ops that generate numeric data.

```
    .byte "H" // warn
```

**`--Wunreferenced-symbols`**

Issue warnings for symbols that have been defined but never used.

```
// --- SOF ---
unusedlabel = 3 // warn
    nop
    nop
// --- EOF ---
```

## Config Options

**`--config`**

Set options from the configuration file. The file format is a JSON string. A typical configuration might look like this:

```json
{
    "target": {
        "binaryFormat": "zx",
        "cpu": "z80"
	},
    "sources": [
        "zxlib.s",
        "my_app.s"
	],
    "outputFile": "program.bin"
}
```

**`--createconfig`**

Generate a configuration file from the arguments provided.

```
6502.Net.exe mysource.s /lib/mylib.s --output=myoutput.bin --error=errors.txt --list=mylistfile.s --format=cbm --cpu=6502i --dsections=zp,2,256 text,$0801,$a000 --createconfig=a
```

creates a config file with the contents:

```json
{
    "listingOptions": {
        "listPath": "mylistfile.s"
    },
    "loggingOptions": {
        "errorPath": "errors.txt"
    },
    "outputFile": "myoutput.bin",
    "sources": [
        "mysource.s",
        "/lib/mylib.s"
    ],
    "sections": {
        "zp": {
            "starts": 2,
            "ends": 256
        },
        "text": {
            "starts": "$0801",
            "ends": "$a000"
        }
    },
    "target": {
        "binaryFormat": "cbm",
        "cpu": "6502i"
    }
}
```

## Other Topics

* [Getting Started](/Docs/GettingStarted.md)
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
* [Technical Info](/Docs/TechnicalInfo.md)