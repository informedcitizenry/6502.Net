# Data Generation

6502.Net offers several ways to control data output in addition to assembly instructions through pseudo operations, or pseudo-ops. Unless noted, the pseudo-ops listed below can take string, numeric, and list (variable and tuple) expressions in their argument lists.

# Pseudo Operation Arguments

The `.align` and `.fill` pseudo-ops take one or two numeric expressions as arguments. All other pseudo-ops take one or more arguments, with each argument either an expression or a wildcard (`?`) to indicate uninitialized data whose size is determined by the pseudo-op. The expression argument can take a number, string or list (array or tuple). 

```
        .byte 1,2,3,4,5,6,7
myarray = [1,2,3,4,5,6,7]
        .byte myarray // alternative to .byte 1,2,3,4,5,6,7
```

All non-list expressions (including non-list elements within list expressions) are constrained to a numeric size determined by the pseudo-op. The "numeric" pseudo-ops (such as `.byte`) can take string expressions, but the encoded value must meet the size and sign requirements of the operation. For instance, the following will assemble:

```
    .byte "h","e","l","l","o"
    .dword "help"
```

But this will not:

```
    .byte "hello" // cannot fit in a byte
```

This behavior differs from many other assemblers, where data directives can take a mix of text strings and values. Therefore string expressions have their own pseudo-ops.

```
        .string "hello" // will not error
```

# Pseudo Operation Reference

**`.addr`**

Generate one or more values with byte order according to CPU endianness. The accepted value range is normally 0 to 65535, but can be longer if the option to allow long addressing is set, or if the current bank matches that of the expression. Wildcard arguments `?`s represent uninitialized data, two bytes in size.

```
    .addr $2000,$3000,$4000
    .addr ? // uninitialized
```

**`.align`**

Align the program counter to the argument. The first expression is the alignment value, a positive number between 0 and 65536. The second value is the fill bytes, up to the alignment.

```
    * = $c037
    .align $100, $ea  // 201 nops
```

**`.bankbytes`**

Extract the bank byte from expressions in the list to output. This directive performs an operation equivalent to the `^` unary operation for each expression. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .bankbytes $0dffff, $020300
    // > $0d, $02
```

**`.bstring`**

Output binary data from the text representation in the arguments. Wildcard `?`s represent uninitialized data, one byte in size.

```
.bstring "001110010011100000110111001101100011010100110100001100110011001000110001"
      // > 39 38 37 36 35 34 33 32 31
```

**`.byte`**

Generate one or more values between 0 and 255 into the output. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .byte 1,2,3,4,?,?
```

**`.cbmflt`**

Generate the "unpacked" CBM/MBF encoded representation of one or more floating point numbers. This format is similar to the IEEE-754 floating point specification, and was adopted by several 8-bit home computers. Wildcard `?`s represent uninitialized data, 6 byte in size.

```
    .cbmflt 3.141592653
    // > 82 00 49 0f da a1
```

**`.cbmfltp`**

Generate the "packed" CBM/MBF encoded representation of one or more floating point numbers. This format is similar to the IEEE-754 floating point specification, and was adopted by several 8-bit home computers. Wildcard `?`s represent uninitialized data, 5 byte in size.

```
    .cbmfltp 3.141592653
    // > 82 49 0f da a1
```

**`.char`**

Generate one or more values between -128 and 127 into the output. This directive is identical to `.sbyte`. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .char '3', -2, 84  // > 03 fe 54
```

**`.cstring`**

Insert a C-style null-terminated string into the assembly, with a zero value inserted at the end of the argument list. If the expression is a string, the output bytes are per the active encoding. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .cstring "hello, world!"    // > 68 65 6c 6c 6f 2c 20 77
                                // > 6f 72 6c 64 21 00
    .cstring $ffd2              // > d2 ff 00
```

**`.dint`**

Generate one or more values between -2147483648 and 2147483647 into the output, with byte order according to CPU endianness. Wildcard `?`s represent uninitialized data, 4 bytes in size.

```
    .dint 18000000      // > 80 a8 12 01
```

**`.dword`**

Generate one or more values between 0 and 4294967295 into the output, with byte order according to CPU endianness. Wildcard `?`s represent uninitialized data, 4 bytes in size.

```
    // 65xx 
    .dword $deadfeed     // > ed fe ad de
    // 6809
    .dword $deadfeed     // > de ad fe ed
```

**`.fill`**

Fill the output a specified number of times, advancing the program counter accordingly. The first expression is the number to advance the program counter, a positive number between 0 and 65536. The second, optional expression is the fill bytes, up to the count (but not beyond).

```
    .fill 5,$ffd2 // > d2 ff d2 ff d2
```

If not second expression is given, then the fill operation leaves data uninitialized.

```
    * = $1000
playerdata   .fill 32 // program counter is $1020
```

**`.hibytes`**

Extract the most significant byte from expressions in the list to output. This directive is equivalent to the `>` unary operation for each of the expressions. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .hibytes $1000,$2000,$3000 // > 10 20 30
```

**`.hiwords`**

Extract the most significant word-sized data from expressions in the list to output. This directive is equivalent to the `^^` unary operatino for each of the expressions. Wildcard `?`s represent uninitialized data, two bytes in size.

```
    .hiwords $1100ff, $2200ff // > 00 11 00 22
```

**`.hstring`**

Output binary data from the text representation in the arguments, which are interpreted as hexadecimal numbers. Wildcard `?`s represent uninitialized data, one byte in size.

```
.hstring "393837363534333231"
      // > 39 38 37 36 35 34 33 32 31
```

**`.lint`**

Generate one or more values between -8388608 and 8388607 into the output, with byte order according to CPU endianness. Wildcard `?`s represent uninitialized data, 3 bytes in size.

```
    .lint  -80000    // > 80 c7 fe
```

**`.lobytes`**

Extract the least significant byte from expressions in the list to output. This directive is equivalent to the `<` unary operation for each of the expressions. Wildcard `?`s represent uninitialized data, one byte in size.

```
        .lobytes $ffcf, $ffd2 // > cf d2
```

**`.long`**

Generate one or more values between 0 and 16777215 into the output, with byte order according to CPU endianness. Wildcard `?`s represent uninitialized data, 3 bytes in size.

```
    .long   $ffdd22   // > 22 dd ff
```

**`.lowords`**

Extract the least significant word-sized data from expressions in the list to output. This directive is equivalent to the  an operation equivalent to the `&` unary operation for each of the expressions. Wildcard `?`s represent uninitialized data, two bytes in size.

```
    .lowords $30_1000, $40_2000 // > 00 10 00 20
```

**`.lstring`**

Insert a a string into the assembly, each byte shifted to the left. Multiple arguments can be passed, with the low bit set on the final byte of the argument list. If the highest bit of any output byte is set, the assembler will error. For string expressions, the output bytes are per the active encoding. Wildcard `?`s represent uninitialized data, one byte in size.

```
        and a               // clear carry
        ld  de,screenbuf
        ld  hl,message
-       ld  a,(hl)          // next char
        rrca                // shift right
        ld  (de),a          // save in buffer
        jr  c,done          // carry set on shift? done
        inc hl              // else next char
        inc de              // and buff
        jr  -               // get next
done    ret
message .lstring "HELLO"    // > 90 8a 98 98 9f
```

**`.nstring`**

Insert a string into the assembly. Multiple arguments can be passed. This directive is similar to .lstring, but with high bit set on the final byte of the argument list instead of the low bit. If the highest bit of any output byte is set, the assembler will error. For string expressions, the output bytes are per the active encoding. Wildcard `?`s represent uninitialized data, one byte in size.

```
        ld  de,screenbuf
        ld  hl,message
-       ld  a,(hl)          // next char
        ld  b,a             // copy to .b to test high bit
        and %01111111       // turn off high bit...
        ld  (de),a          // and print
        rlc b               // high bit into carry
        jr  c,done          // if set we printed final char
        inc hl:inc de       // else increment pointers
        jr -                // get next
done    ret
message .nstring "hello"    // > 68 65 6c 6c ef
```

**`.pstring`**

Insert a Pascal-style string into the assembly. Multiple arguments can be passed, with the first byte of the output denoting the total size in bytes of all expressions in the list. If the expression is a string, the output bytes are per the active encoding. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .pstring $23,$24,$25,$26,1024 // > 06 23 24 25 26 00 04
    .pstring "hello"              // > 05 68 65 6c 6c 6f
```

**`.rta`**

Generate one or more values according to CPU endianness. The accepted value range is normally 1 to 65536, but can be longer if the option to allow long addressing is set, or if the current bank matches that of the expression. Useful for so-called "return jump" tables for 6502-based CPUs. Wildcard `?`s represent uninitialized data, two bytes in size.

```
chrin   = $ffcf
chrout  = $ffd2
rtsjmp  txa                 // .x := index of jump
        asl a               // double it
        tax
        lda jumptable+1,x   // push high byte
        pha
        lda jumptable,x     // push low byte
        pha
        rts                 // do the jump
jumptable
        .rta chrout, chrin  // > d1 ff ce ff
```

**`.sbyte`**

Generate one or more values between -128 and 127 into the output. This directive is identical to `.char.` Wildcard `?`s represent uninitialized data, one byte in size.

```
    .sbyte 127, -3  // 7f fd
```

**`.short`**

Generate one or more values between -32768 and 32767 into the output, with byte order according to CPU endianness. This directive is identical to `.sint`. Wildcard `?`s represent uninitialized data, two bytes in size.

```
    .short -16384        // > 00 c0
```

**`.sint`**

Generate one or more values between -32768 and 32767 into the output, with byte order according to CPU endianness. This directive is identical to `.short`. Wildcard `?`s represent uninitialized data, two bytes in size.

```
    .short -32768        // > 00 80
```

**`.string`**

Output data into assembly. Multiple arguments can be passed. If the expression is a string, the output bytes are per the active encoding. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .string "hello, world!"    // > 68 65 6c 6c 6f 2c 20 77
                               // > 6f 72 6c 64 21 00
    .string $ffd2              // > d2 ff 00
```

**`.stringify`**

Output expressions into assembly in its stringified form. If the expression is a numeric or boolean value, the data is represented as ASCII characters. Multiple arguments can be passed. Wildcard `?`s represent uninitialized data, one byte in size.

```
    .stringify "hello, world!" // > 68 65 6c 6c 6f 2c 20 77
                               // > 6f 72 6c 64 21 00
    .stringify $ffd2           // > 36 35 34 39 30
```

**`.word`**

Generate one or more values between 0 and 65535 into the output, with byte order according to CPU endianness. Wildcard `?`s represent uninitialized data, two bytes in size.



