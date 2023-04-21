# Diagnostics

## General Output Options

All diagnostic messages, including errors and warnings, can be suppressed with the `--quiet` option. To report errors and warnings but not other assembly status information, pass the `--no-stats` option. The `--checksum` option prints an MD5 hash of the code generated.

## Controlling Error and Warning Output

Options exist specifically for controlling error and warning reporting. To suppress only warnings but not errors, use the `--no-warn` option. To output error and warning messages to a file (without using the operating system's built-in facilities for this), you can also set the `-e`/`--error` option.

```
6502.net.exe source.asm -o program.prg --error=errors.txt
```

When an error or warning is reported to the standard output, typically the source where the error occurred is highlighted.

```
-$ dotnet 6502.net.dll x86code.s

bugs.s(1:12): error: Unexpected: 'eax'
        mov eax,ebx
            ^--
```

Use `--no-highlighting` to disable this feature. 

### Warning Options

There are several options specific to warning messages. To enable all warning types, set the option `--Wall`, while to treat all warnings as errors, pass `--werror`. See the reference on options for full information on all warning options available.

## Assertions and Conditional and Custom Errors

The developer can make assertions in their code, optionally adding a custom message if the assertion fails

```
        .assert * < $100 // make sure we are in zero page
        .assert * < $100, "Zero Page Boundary Crossed!" // add a helpful message
```

In a similar vein, the `.errorif` directive will raise an error but with the condition being met and a custom error being required.

```
        .errorif * >= $0100, "Zero Page Boundary Crossed!"
```

This can be turned into warning using `.warnif`.

```
        .warnif * >= $0100, "Caution: Zero Page Boundary Crossed!"
```

Errors and warnings can be raised unconditionally.

```
        .error "This does not work!"
```

```
        .warn "We need to refactor this!"
```

## Echo Directive

To print merely informational messages, use the `.echo` directive.

```
        .echo $"Program Counter is ${*}"
```

`.echo` will only print the message on first pass but if the message is desired to printed each pass set the option `--echo-each-pass` is set.

