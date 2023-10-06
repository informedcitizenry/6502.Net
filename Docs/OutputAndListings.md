# Output And Listings

## Output Formats

Source can be outputted in several binary formats. By default the format adhere's to the Commodore 8-bit binary, with the first two bytes the load address of the code, followed by the object code itself.

Set the format at the command line with the `--format` option.

```
~$ dotnet 6502.net.dll myprog --format=cbm
```

Alternatively you can specify with the `.format` directive.

```
            .cpu "z80"
            .format "zx"
            * = $8000
            xor a,a
```

Many formats are specific to the CPU family of the source, though some are available to all CPU types. The full list of valid formats are:

| Format        | CPU Family    | Description                                               |
|---------------|---------------|-----------------------------------------------------------|
| amsdos	    | i8080/z80	    | Amstrad CPC DOS (disk)                                    |
| amstap        | i8080/z80     | Amstrad CPC DOS (tape)                                    |
| apple2        | 65xx          | Apple ][ binary with Apple DOS header                     |
| atari-xex     | 65xx          | Atari 8-bit binary with XEX header                        |
| bytesource	| All	        | Refactor source as .byte statements                       |
| cart          | 65xx          | Vice/CCS64-compatible cartridge image                     |
| cbm           | 65xx          | Commodore DOS binary with load address header (default)   |
| d64           | 65xx          | Commodore emulator D64 disk image format                  |
| flat          | All           | Flat binary with no header                                |
| hex           | All           | Hex dump of the binary output as plain text               |
| msx	        | i8080/z80	    | MSX                                                       |
| srec          | All           | Motorola S-record                                         |
| srecmos	    | All           | MOS Technology formatted file (S-record variant)          |
| t64	        | 65xx          | Commodore emulator T64 tape image format                  |
| zx            | i8080/z80     | ZX Spectrum                                               |

## Section Output and Patching

If a section is defined in source or as an option output can be limited to that section with the `--output-section` argument.

```
-$ dotnet 6502.Net.dll myprog.asm --dsection=text,$0801 -o myprog.prg --output-section=text
```

Existing object files can be patched if a bugfix is required to be applied. The applied offset must be an address.

```
6502.Net.exe myfix.asm -o mygame.prg --patch=$452b
```

## Program Listings

6502.Net can generate program listings. The `-l`/`--list` option specifies an output file.

```
6502.Net.exe myprog.s -o myprog.prg --list=myprog_list.s
```

By default, the listing will contain monitor code, disassembly, and original source per line.

```
        * = $033c
start   lda $0380 ; swap $0380 and $0381
        ldx $0381 ; load each into registers
        sta $0381 ; and store each
        stx $0380
/* listing is:
.033c                                                           * = $033c
.033c    ad 80 03               lda $0380               start   lda $0380 ; swap $0380 and $0381
.033f    ae 81 03               ldx $0381                       ldx $0381 ; load each into registers
.0342    8d 81 03               sta $0381                       sta $0381 ; and store each
.0345    8e 80 03               stx $0380                       stx $0380
*/
```

Different command line options control the listing of each element. The `-a` option will remove monitor code, the `-d` option will remove disassembly, and `-s` will remove source code.

To control individual statements or blocks of statements from appearing in program listing, use the `.proff` directive, and to resume listing use `.pron`.

```
        .proff
        .include "mylib.s" // contents of "mylib.s" will not be included in listing
        .pron
        lda #$00
        //...
```

## Symbol Listings

In addition to program listing, it is possible to generate a list of all defined symbols with the `-L`/`--label` option.

```
// contents of "myprog.s":
chrout  = $ffd2
        * = $033c
start   lda $0380


// dotnet 6502.Net.dll myprog.s -o myprog.prg --label=myprog_labels.s

// contents of "myprog_labels.s":
chrout                                            = $ffd2 (65490)
start                                             = 033c (828)
```

By default all constants and labels will list. If you desire only to include labels, use the `--labels-addresses-only` option.

The `--vice-labels` option sets the output in the VICE label format for use in debugging code in that emulator.

```
al 33c .start
```

## Other Topics

* [Getting Started](/Docs/GettingStarted.md)
* [Memory And Addressing](/Docs/MemoryAndAddressing.md)
* [CPU Specific Assembly](/Docs/CPUSpecificAssembly.md)
* [Data Generation](/Docs/DataGeneration.md)
* [Expressions](/Docs/Expressions.md)
* [Symbols And Scopes](/Docs/SymbolsAndScopes.md)
* [Built-In Symbols](/Docs/BuiltInSymbols.md)
* [Control Assembly](/Docs/ControlAssembly.md)
* [Macros](/Docs/Macros.md)
* [File Inclusions](/Docs/FileInclusions.md)
* [Diagnostics](/Docs/Diagnostics.md)
* [Disassembler](/Docs/Disassembler.md)
* [Command-line Options](/Docs/CommandLineOptions.md)
* [Technical Info](/Docs/TechnicalInfo.md)