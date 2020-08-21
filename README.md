6502.Net, A Simple .Net-Based 65xx and Z80 Cross-Assembler

Version 2.2

## Overview

The 6502.Net Macro Assembler is a simple cross-assembler targeting several CPUs from the 8-bit era of computing including the MOS 6502 and its variants, as well as the Zilog Z80. It has several advanced features, such as conditional assembly, macros and custom defined fuctions. With the aim of cross-platform compatibility, it is targeted for .Net Core 3.1.

Invoke the assembler from the command line like so: `/6502.Net.exe myprg.asm`. To specify output file name use the `-o <file>` or `--output=<file>` option.

As with other assemblers, source statements are typically in the format:

```asm
label   mnemonic    operands
```

Comments are either C/C++ style `/*`/`*/` for block comments, or `//` for line comments. For compatibillty purposes, semi-colons can also be used for line comments.

For full usage, please see the [wiki](https://github.com/informedcitizenry/6502.Net/wiki).