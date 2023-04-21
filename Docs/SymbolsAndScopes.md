# Symbols and Scope

## Labels

In assembly source code labels are symbolic addresses. They remove the need for the programmer to calculate addresses manually. There are two kinds of labels, named labels and anonymous labels.

## Named Labels

A named label is a string of alphanumeric characters that must begin with an underscore or a letter.

```
mylabel lda #0
```

Each named label must be unique. In many assembly languages it is customary for a label to be followed by a `:`. In 6502.Net, this is allowed but not necessary.

## Anonymous Labels

Because label names must be unique, and since branching can be a common task in assembly, it often becomes necessary for the programmer to be creative in assigning label names. One solution for this problem is to use anonymous labels, or backward and forward references.

Forward references are declared with a +, while backward reference labels are declared using a -. They are forward or backward from the current assembly line and are referenced in the operand with one or more + or - symbols:

```
printmessage
    ldx #0
-   lda msg_ptr,x
    beq +               // jump to first forward reference from here
    jsr chrout
    inx
    bne -               // jump to first backward reference from here
+   rts
-   nop
    jmp --              // jump to the second backward reference from here
```

Like standard labels, anonymous labels can be used in expressions as well:

```
-   .byte $01, $02, $03
    lda (-) + 2,x // read second from offset from backward reference.
```

# Constants and Variables

## Constants

Constants are symbols that, like labels, make it convenient to represent addresses or values referenced from other parts of code.

A constant is defined using the assignment `=` operator or the `.equ` directive.

```
CHROUT = $ffd2
CHRIN .equ $ffcf

    jsr CHROUT
```

Constants can be forward referenced, and can only be assigned once--as they are constant, their values cannot be changed.

## Variables

Variables are symbols assigned to values that can be changed as often as needed. The assignment `:=` operator can declares a new variable or re-assign the contents of an existing variable to a new value.

```
myvar := 32

myvar := 16 // now myvar is 16
```

Alternatively, one or more variables can be defined or have their values changed with the `.let` directive.

```
    .let myvar = 1, myvar2 = 2 // etc.
```

Unlike constants and labels, variables cannot be referenced before they are defined, as they are not preserved between passes. In the example below, assuming num has not previously been defined, the following code would error:

```
    lda #num
num := 3
```

When a variable is first defined, its type cannot change, only its value.

```
myint := 3 // good

myint := 5 // good

myint := [7] // type mismatch error
```

## Discard Symbol

A special type of constant is the discard, which is never defined and can never be referenced.

```
_       = $ffd2-3
```

This symbol often is not needed, but can be useful if evaluating an expression is desired without needing to save its value.

### Compound Assignments

Existing variables can be modified with compound assignments.

```
myvar = myvar + 1
    // can be rewritten as:

myvar += 1
```

Compound assignment operators are available for all arithmetic operations.

### Pre- and Postfix Operators

The example above can further be simplified using an increment operator.

```
myvar++

// -or-

++myvar
```

If the increment precedes the variable, the operation is the same except the expression performs the increment operation first before returning the value of the increment operation.

```
myvar := 2
othervar := myvar++ // othervar is 2, myvar is 3

othervar := ++myvar // othervar is now 4, same as myvar
```

# Symbol Scopes

All symbols have scope. Scopes group symbols together. The default scope is the global scope.

Symbols in separate scopes can share a common name.

## Cheap Local Symbols

Within their scope, labels and constant names must be unique. One way to re-use a name for such a symbol is to start it with an underscore. This turns the symbol into a cheap local, where it is considered local in scope to the most recent named label not beginning with an underscore.

```
printmessage    ldx #0
_loop           lda message,x
                //...
                jmp _loop // the _loop below printmessage
                          // not the one below

clearscreen     ldx #0
                lda #' '
_loop           jsr chrout
                //..
                jmp _loop // the _loop below clearscreen
                jsr printmessage._loop // the _loop below printmessage
```

In the example above, there are two labels named `_loop` label, but each is local to the most immediate non cheap local.

This same functionality works for constants and variables:

```
main
_local  = $19
        lda (_local,x)
//...
somewhere_else
_local  = $fb
        lda (_local,x)
```

## Named Scopes

### Blocks

Symbols can also be wrapped into scope blocks. Instead of using cheap locals, symbols can be wrapped inside `.block` or `.proc` directives.

```
routine1   .block
            beq done
            jsr $ffd2
            inx
            jmp routine1
done        rts
            .endblock
routine2    ldy flag
            beq done
            jmp dosomething
done        rts
```

The two done labels do not begin with an underscore, but are still tracked separately; the first label is in the scope owned by `routine1`'s scope block, the second is in the global scope.

Anonymous reference labels are also scoped, so any such labels are only accessable within their scope.

```
myblock     .block
            ldx #0
-           lda table,x
            beq +       // legal because myblock is enclosed in the global scope
            jsr process
            jmp -       
            .endblock
+           rts

            // if the only back reference occurs in myblock, this is not legal:
            jmp -
```

`.proc` is functionally the same as `.block` except that any code inside a `.proc` block will only assemble if one or more of its symbols are referenced outside.

```
unassemble  .proc

            nop
            .endproc

            //... end of file
```

In the above the `nop` would not end up in the output because nothing references the `unassemble` symbol. On the other hand:

```
assemble    .proc
            nop
            .endproc

            lda #<assemble
```

Since `assemble` is referenced later the entire contents inside the `.proc`/`.endproc` block will assemble.

### Namespace

An alternative way to create a named scope is to create a namespace with the `.namespace` directive. A namespace is a re-usable named scope that can have multiple nestings.

```
        .namespace simple
begin   nop
        .endnamespace
        jmp simple.begin

        .namespace more.complex
begin   nop
        .endnamespace
        jsr more.complex.begin
```

Note that, unlock a `.block` or `.proc` symbol, a namespace symbol can be appear multiple times in its scope. Also, unlike the `.blcok`/`.proc` root name, a namespace cannot be used as a label; it does not represent an address.

```
myblock .block
        brk
        .endblock
        jsr myblock // works (myblock is a label)

        .namespace mynamespace
        brk
        .endnamespace
        jsr mynamespace // will not work (it is a namespace)
```

# Importing Scopes

Named scopes can be imported into the current scope for the purposes of resolving symbol names. When referencing symbols defined in a different scope, by default it is necessary to resolve them using their fully qualified name. The `.import` directive allows one to refer them by their simple name.

```
scope   .block
label   nop
        .endblock

        .import scope
        jmp label // no need to write it as "jmp scope.label" because scope is imported
```

# Symbols and Reserved Words

User-defined symbols cannot share with mnemonics and registers of the current target CPU, unless their case does not match and the assembler is set to case-sensitive. For instance, in 6502 mode all of the 56 6502 mnemonics and `a`, `x`, and `y` are reserved and cannot be used for label or constant names.

```
lda nop // this would error "lda" cannot be used as a label

    // with option -C set
Lda nop // no error
```
