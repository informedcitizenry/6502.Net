# File Inclusions

## Source Inclusion

6502.Net source can be included (such as reusable library files) with `.include`.

```
        .include "mylib.s"
        // mylib is now part of source
```

To guard against symbol collisions the `.binclude` directive includes the source in its own private scope.

```
        // all labels and variables in "mylib.s" are in their own scope and cannot be accessed outside.
        .bincluce "mylib.s"

        // all labels and variables in "routines.s" are in the scope "included" and can be accessed
        // accordingly.
included .binclude "routines.s"
```

## Binary File Inclusion

To insert binary data in the output use the `.binary` directive.

```
    .binary     "subroutines.prg",2  // strip off start address
    .binary     "mybin.bin"          // include all of 'mybin.bin'
    .binary     "subroutines.prg",2,1000
                  // strip off start address, only take first
                  // 1000 bytes thereafter.
```

The first argument is the file path. The second and third optional expressions are the offset and length of bytes to read from the file, respectively.
