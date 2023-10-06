# Control Assembly

## Conditional Assembly

Certain directives permit the programmer to assemble code conditionally. The most basic conditional assembly statement, where an expression is tested to be true or false, is:

```
        .if * % 256 != 0    // if program counter not page aligned
            nop             // output 2 nops
            nop
        .endif
```

Alternate conditions are possible where previous conditions are not met.

```
        .if * % 256 == 0
            jmp tightloop
        .else
            nop
            nop
        .endif

        .if BACKGROUND == 1
            lda #1
        .elseif BACKGROUND == 2
            lda #2
        .else
            lda #0
        .endif
```

Other conditions can check if a symbol is defined or if an expression is constant.

```
        .ifdef DEBUGMODE
            brk
        .endif

        .ifconst MYCONST_EXPRESSION
            lda #0
        .endif

        .ifndef RELEASE // RELEASE not defined
            rts
        .endif
```

`.else` versions exist for each of these conditional directives.

```
        .ifdef CBM
chrout      = $ffd2
        .elseifdef APPLEII
chrout      = $fded
        .endif
```

## Jump Assembly

### Goto

The programmer can direct the assembler to jump to other parts of source. The `.goto` directive will commence assembly at the specified label. The destination label must either be terminated by a newline or colon, or must be specified as a jump label with the `.label` directive.

```
        .ifdef CBM
            .goto commodore
        .endif
        //....
commodore .label // .label is optional so long as "commodore" is not followed by another statement
        jsr $ffd2

```

### Switch

Switch statements allow more compact forms of conditional assembly. For each `.switch` directive one more more `.case` labels follows where the case condition is tested for equality with the `.switch` argument.

```
    .switch CPU_NAME
        .case "65816"
            jsr long_address
            .break
        .case "45GS02"
        .case "65CE02"
        .case "m65"
            jmp long_address
            .break
        .default
            jmp long_address
    .endswitch
afterswitch nop
```

In the above example the `.break` directives cause assembly to resume to the `afterswitch` label. If ommitted assembly would fall through to the next case.

## Loop Assembly

### Repeat

Assembly repetitions are possible using the `.repeat` directive, where a block of code will be assembled a specified number of times.

```
ldx #$00
    .repeat 3
        inx
    .endrepeat
    rts
    /* will assemble as:
    ldx #$00
    inx
    inx
    inx
    rts
    */
```

### Do and While

The `.do` and .`while` directives allow repeat assembly conditionally.

```
        num := 0
        .while num < 256
            ld b,num
            ld (hl),b
            inc hl
            num++
        .endwhile
```

`.do` and `.while` are nearly identical, except that for `.do` the condition occurs after the first iteration of the block assembly, so the code block process at least once.

```
        * = 0
        .do
            nop
        .whiletrue * < 0 // emits a nop

        .while * < 1
            nop
        .endwhile       // no code generated
```

The `.while` version of the above prevents execution because the condition is evaluated before the code block.

### For and Foreach

For loops are a common feature in higher level languages. 6502.Net provides two variants. The `.for` directive is C-like, where a variable is assigned, a condition is tested, and then one or more one or more assignment expressions are evaluated.

```
        .for i = 0, i < 5, i++
            nop
        .next // five nops
```

The initial variable assignment and condition are optional. An alternative way to perform the above is:

```
        .let i = 0
        .for ,,i++
            .if i == 5
                .break
            .endif
            nop
        .next
```

The `.foreach` takes a more modern approach to the `.for` loop, where loop assembly occurs during the iteration of a string or collection.

```
high_scores = [10000,5000,3000,2000,1000]
    .foreach score, high_scores
        .long score
    .next

// The iteration in the dictionary is a key/value pair
prizes = {.cherry: 100, .strawberry: 200, .peach: 300}
        .foreach prize, prizes
            .string prize.key
            .long prize.value
        .next
```

Any variables declared in the `.for` and `.foreach` directives are local in scope to the directive block unless previously declared.

### Break And Continue

The `.break` and `.continue` directive can appear within the code block of any loop directive. As expected, `.continue` will return processing to the beginning of the block while `.break` takes the assembler out of the loop altogether.

```
        .for addr=0x100,,addr++
            .if addr >= 0x200
                .break // no more output at page change
            .endif
            .byte $11
        .next
```

```
        .for i = 0, i < 5, i++
            .if i % 2 == 0
                .continue   // do not process on even counts
            .endif
            .byte i
        .next
```

## Loop Assembly and Labels

Because labels might change address values between passes, and 6502.Net is a multi-pass assembler, this can cause an issue if a label is declared inside a loop assembly block.

```
        .while * < 5
start   nop
        .endwhile
```

The above code is technically "legal", but what happens is the address of `start` recalculates at each loop, and the assembler attempts another pass to resolve a label to a final address. The above code therefore causes a loop in the assembler itself and it will generate a "Too many passes attempted" error.

The solution is to declare all labels before the block.

```
start
        .while * < 5
        nop
        .endwhile
```

Alternatively, you can use an anonymous label if there is a a need to branch to a specific instruction inside the loop.

```
        .repeat 5
            ldx #0
-           stx *+$100
            bne -
        .endrepeat
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
* [Macros](/Docs/Macros.md)
* [File Inclusions](/Docs/FileInclusions.md)
* [Diagnostics](/Docs/Diagnostics.md)
* [Disassembler](/Docs/Disassembler.md)
* [Command-line Options](/Docs/CommandLineOptions.md)
* [Technical Info](/Docs/TechnicalInfo.md)