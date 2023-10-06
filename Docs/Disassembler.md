# Disassembler

6502.Net also provides a simple disassembler. If the `disassemble` option is set, input files are considered to be machine code, and the output file will be the disassembly of that machine code.

```
~$ dotnet 6502.Net.dll myprog.prg -o myprog_disassembly.txt --disassemble
```

By default the program origin is the first two bytes of the input file, as that is the default output format for 6502.Net in asssembly mode. To specify a different format, use the `--format` option.

```
6502.Net.exe myxexprog.prg -o myprog_disassembly.txt --disassemble --format=atari-xex
```

At this time only the following output options are supported:

* `apple2`
* `atari-xes`
* `cbm`
* `flat`

If the input files are flat (no header or program load address encoded into the file), you can manually specify the program start with the `--disassembly-start` option. Note this value must be a decimal integer between 0 and 65535. Similarly, if you want to specify the machine language CPU, pass the `--cpu` option.

```
~$ dotnet 6502.Net.dll myspeccyprog.bin -o myspeccyprog_disassembly.txt --disassemble --cpu=z80 --disassembly-start=4096
```

Finally, to disassemble the code at a specific offset, you can pass the `--disassembly-offset` option.

```
6502.Net.exe myprog.prg -o myprog_disassembly.txt --disassemble --cpu=65C02 --format=flat --disassembly-start=4096 --disassembly-offset=128
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
* [Command-line Options](/Docs/CommandLineOptions.md)
* [Technical Info](/Docs/TechnicalInfo.md)