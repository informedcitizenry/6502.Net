# Macros

Macros are textual substitutions that make writing code easier. They are syntactically and somewhat functionally similar to functions, but provide more flexibility in code reuse.

Every macro definition requires a named identifier and can accept zero or more arguments to pass into the macro during its expansion.

```
cr  .macro
    lda #'\r'
    jsr $ffd2
    .endmacro
```

Macros are invoked with a leading . preceding the macro name, much like a directive.

```
    .cr // will expand to:
/*
    lda #'\'r
    jsr $ffd2
*/
```

Arguments are referenced in macro definitions with a leading `\`, either by name or by number in the parameter list.

```
/* macro with named parameter */
inc16 .macro addr
    inc \addr
    bne +
    inc \addr+1
+   .endmacro

/* macro with parameters referenced by one-based parameter index number */
dec16 .macro
    bne +
    dec \1+1
+   dec \1
    .endmacro

/* include all parameters in the expansion */
mymacro .macro
    .byte \* 
    .endmacro

    .mymacro 1,2,3,4,5
/* expands to:

    .byte 1,2,3,4,5
*/
```

Parameters can be given default values to make them optional upon invocation.

```
basic .macro sob=2049, start="2061"
    * = \sob
    .word eob,10
    .byte $93
    .cstring $"{start}"
eob .word 0

    .basic 
/* expands to:
    * = 2049
    .word eob,10
    .byte $93
    .cstring "2061"
eob .word 0
*/
```

An example of the above with passed parameters would be:

```
   .basic $1000, "4108"
```

All symbols in the macro definition are local, because when expanded they are placed in their own scope blocks, so macros can be re-used with no symbol clashes.

If a label precedes a macro invocation, all constants expanded inside the macro are part of that label's scope. In this way, macros can be used like C structs.

```
point   .macro x=0, y=0
x_coord .char \x
y_coord .char \y
        .endmacro

player_pos .point 
enemy_pos  .point 64,127

        lda #player_pos.x_coord
        cmp #enemy_pos.x_coord
```

Macros can also reference other macros.

```
inc16 .macro
    inc \1
    bne +
    inc \1+1
+   .endmacro
inc24 .macro
    .inc16 \1
    bne +
    inc \1+2
+   .endmacro
    .inc24 $fb
/* expands to:
    inc $fb
    bne +
    inc $fb+1
+   bne +
    inc $fb+2
+   
*/
```

Arguments passed to macros do not necessarily have to be expressions--they can be anything the assembler recognizes as valid source. This allows even code to be passed as an argument:

```
long_branch .macro mnemif, mnemifnot, dest
    offset := \dest-(* + 2)
    .if offset < INT8_MIN || offset > INT8_MAX
        \mnemifnot * + 5
        jmp \dest
    .else
        \mnemif \dest
    .endif
    .endmacro
    .long_branch bne,beq,$2000
```


