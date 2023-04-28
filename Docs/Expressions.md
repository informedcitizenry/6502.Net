
# Expressions

As a cross-assembler, 6502.Net offers a powerful expression engine, allowing complex expressions similar to those found in higher level languages.

## Data Types and Methods

Expressions evaluate to objects of specific types. Values of different types can perform different operations and have various methods associated to them.

### Numeric and Boolean Types

Most expressions in 6502.Net source code are numeric, that is they evaluate to numeric data. Number literals can be represented in several ways.

| Notation          | Examples                  |
|-------------------|---------------------------|
| Decimal Integer   | `42`                      |
| Decimal Float     | `3.14`, `6.549E+4`, `NaN` |
| Binary Integer    | `%00101010`, `0b00101010` |
| Binary Float      | `%01e-12`, `0b10.10`      |
| Octal Integer     | `052`, `0o77`             |
| Octal Float       | `07.201`, `0o1p+4`        |
| Hexadecimal       | `$ffd2`, `0x2a`           |
| Hexadecimal Float | `0x23.80`, `$1.ffa4p+15`  |

Use underscores to separate digits to aid in reading (e.g, `0xffff_ffff`). In addition, floating point representations of NaN can be expressed using `NaN`.

Be aware that decimal numbers with leading zeros are considered octal (and so any 8 and 9 digits that followed would be illegal).

Binary numbers can be alternatively represented as a series of dots (`.`) and octothorpes (`#`), which is useful when bit patterns represent other types of data, like pixels:

```
    .byte %...###..
    .byte %..####..
    .byte %.#####..
    .byte %...###..
    .byte %...###..
    .byte %...###..
    .byte %...###..
    .byte %.#######
```

Like many high level languages 6502.Net reserves the keywords `false` and `true` as boolean literals.

```
SUPPORTS_HIGH_SCORES = true
```

#### Methods

| Method name | Purpose                                             | Example                   |
|-------------|-----------------------------------------------------|---------------------------|
| `toCbmFlt`  | Get the number value as a CBM-encoded byte array    | `3.141592653.toCbmFlt()`  |
| `toCbmFltp` | Get the number value as a packed CBM-encoded array  | `3.141592653.toCbmFltp()` |
| `size`      | Get the size (in bytes) of the number value         | `65490.size() // 2`       |
| `toString`  | Get the value as a string                           | `true.toString()`         |

Note the `toCbmFlt`, `toCbmFltp`, and the `size` methods are not available to boolean values.

#### Numeric Unary Operations

| Operator  | Type                          | Example               |
|-----------|-------------------------------|-----------------------|
| `~`       | Positive floor (floats)       | `~3.14` (`3`)         |
| `~`       | Bitwise NOT (integers)        | `~$ff` (`-256`)       |
| `+`       | Positive number               | `+42`                 |
| `-`       | Negation                      | `-42`                 |
| `++`      | Pre/postfix increment         | `++numvar`/`numvar++` |
| `--`      | Pre/postfix decrement         | `--numvar`/`numvar--` |
| `<`       | Least significant byte	    | `<$ffd2` (`$d2`)      |
| `>`       | Most significant byte	        | `>$ffd2` (`$ff`)      |
| `&`       | Least significant word	    | `&$10ffff` (`$ffff`)  |
| `^^`      | Most significant word         | `^^$10ffff` (`$10ff`) |
| `^`       | Bank byte                     | `^$10ffff` (`$10`)    |

The extraction operations (`<`, `>`, `&`, `^^`, and `^`) are evaluated last. The expressions `<($ffd2+1)` and `<$ffd2+1` are effectively the same.

#### Arithmetic and Bitwise Binary Operations

The math operations below evaluate in the order listed.

| Operator          | Type                          | Example                                       |
|-------------------|-------------------------------|-----------------------------------------------|
| `^^`              | Exponentiation                | `3.14 ^^ 24`                                  |
| `*`,`/`,`%`       | Multiplicative                | `8 * 5`, `256 % 16`, `8 / 2`                  |
| `+`, `-`          | Additive                      | `5 + 1`, `7 - 32.5`                           |
| `<<`,`>>`,`>>>`   | Shifts                        | `2 << 3`, `-4 >> 1` (`-2`), `-4 >>> 1` (`2`)  |
| `&`               | Bitwise AND                   | `2323 & $0f`                                  |
| `^`               | Bitwise XOR                   | `255 ^ 1`                                     |
| `\|`              | Bitwise OR                    | `127` \| `0x80`                               |

The right shift operator `>>` preserves the sign during shift while `>>>` does not.

#### Arithmetic Operations and Special Symbols

Certain arithmetic operators share the same form as special symbols, and therefore the developer needs to take care not to confuse the parser. Consider the example below:

```
        lda *%10 // get remainder of pc divided by 10
```

This would raise a syntax error because the `*` is interpreted as the multiply operator, not the program counter. The fix in this case is to add a space between the modulo operator and the right-hand side expression.

```
        lda *% 10 // no errors!
        sta * % 10 // even better
```

A similar caution is needed for anonymous labels, which can erroneously be interpreted as operators:

```
        lda -+3 // this is negative 3
        lda (-)+3 // this is anonymous label plus 3
```

#### Boolean Unary Operations

| Operator  | Type                          | Example               |
|-----------|-------------------------------|-----------------------|
| `!`       | Logical NOT                   | `!true`               |

#### Boolean and Relational Binary Operations

| Operator                  | Type          | Example                               |
|---------------------------|---------------|---------------------------------------|
| `<`,`<=`,'`>`,`>=`, `<=>` | Relational    | `5 < 8`, `9 >= 3`, `6 <=> 2` (`1`)    |
| `==`,`!=`,`===`,`!==`     | Equality      | `9 == 3`, `arr1 === arr2` (`false`)   |
| `&&`                      | Logical AND   | `true && false`                       |
| `\|\|`                    | Logical OR    | `false \|\| true`                     |

The `<=>` operator is the signum, where `6 <=> 2` is `1` while `2 <=> 6` is `-1`, and `6 <=> 6` is '0'. The `===` and `!==` operators perform identity comparisons. The left hand of the expression is only considered identical to the right hand side if it was assigned, and the right hand side is complex. 

#### Boolean Ternary Operations

| Operator      | Type          | Example               |
|---------------|---------------|-----------------------|
| `?:`          | Conditional   | `true ? $ff : $d2`    |

### Character and String Types

Character literals are expressed in single quotes, i.e. `'H'`, while strings are enclosed in double quotation marks and have variant length. Strings can be either be represented as single-line or multiline. Single-line strings begin and end with a single quotation mark:

```
    .string "HELLO, WORLD"
```

Multiline strings begin and end with three consecutive quotation marks, and can contain carriage returns and line feeds:

```
    .string """
   Press <F1> For Options
   Press <FIRE> Button To Start
   Press <ESC> To Exit To BASIC
"""
```

#### Encodings

Character and string encodings deal with binary representation of characters and strings. The default encoding is UTF-8, but this behavior can be changed. The `.encoding` directive switches the current active encoding for all strings and characters. There are four built-in encodings.

| Encoding      | Description               |
|---------------|---------------------------|
| `none`        | UTF-8 (default)           |
| `atascreen`	| Atari screen codes        |
| `cbmscreen`	| Commodore screen codes    |
| `petscii` 	| PETSCII                   |

```
    * = $0400
    .encoding "cmbscreen"
    .string "HELLO, WORLD!" // > 08 05 0c 0c 0f 2c 20 17  
                            // > 0f 12 0c 04 21
```

Custom encodings are created simply by selecting them:

```
    .encoding "myencoding"
```

Newly created encodings will generate UTF-8 output. Use the `.map` directive to map specific glyphs or codepoints to custom encoding values, 

```
    .map "A", 0 // Uppercase 'A' now outputs zeros

    .string "ABC" // > 00 42 43
    lda #'A' // > a9 00

    .map "\u03c0", $7e // petscii encoding of pi
```

The map arguments can take a few forms. In the examples above, the first argument in the argument list are single-character string literals. But these can also be numeric values (the codepoints). The second argument is the output value.

Ranges of characters can be mapped in two ways. If the first argument is a two character string, then this defines a range, So long as the second character has a higher Unicode codepoint than the first. The second argument begins the encoding value.

```
    .map "AZ", 0

    .string "HELLO" // > 04 07 0b 0b 0e
```

Alternatively a range can be specified by a start value and an end value.

```
.map 0x48, 0x5a, 0x00 // all characters from U+0048 and U+005a
```

All encodings except "none" can be changed using the .map directive.

The `.unamp` directive will delete the custom encoding for the codepoint or range of codepoints and revert to UTF-8.

```
    .encoding "myencoding"
    .unmap "π" // revert to UTF-8 encoding
    .unmap "AZ" // unmap the range
    .unmap "A","Z" // or this way
```

The assembler can be directed to encode string literals explicitly regardless of the active encoding, according to their prefix:

| Prefix | Encoding | Example                            |
|--------|----------|------------------------------------|
| None   | Current  | `"HI" // by default 48 49`         |
| `u8`   | UTF-8    | `u8"HI" // 48 49`                  |
| `u`    | UTF-16   | `u"HI" // 48 00 49 00`             |
| `U`    | UTF-32   | `U"HI" // 48 00 00 00 49 00 00 00` |

#### Escape sequences

For characters and single-line strings, all of the .Net escape sequences are supported, where a backslash followed by an escape character or characters represent a textual element.

```
"He said, \"Hello,\" to me!"
```

| Escape        | Description                       |
|---------------|-----------------------------------|
| `\'`          | Single quote                      |
| `\"`          | Double quote                      |
| `\\`          | Backslash                         |
| `\?`          | Query                             |
| `\a`          | Bell                              |
| `\b`          | Backspace                         |
| `\f`          | Form feed                         |
| `\n`          | Line feed                         |
| `\r`          | Carriage return                   |
| `\t`          | Horizontal tab                    |
| `\v`          | Vertical tab                      |
| `\0`          | Terminator                        |
| `\ooo`        | ASCII character in octal notation |
| `\uhhhh`      | UTF-16 code unit (U+nnnn)         |
| `\Uhhhhhhhh`  | UTF-32 code unit (U+nnnnnn)       |
| `\xhhh-hhhh`  | ASCII character in hex notation   |

#### Interpolated String Literals

String literals can also be interpolated with expressions whose result is stringified and concatenated with the surrounding string. An interpolated string is prefixed with a `$` and the interpolated expression is surrounded by a pair of `{` and `}` braces.

```
        START = 49152
        .string $"Start address: {START}" // Becomes "Start address: 49152"
```

In the above example, the expression can be formatted with format specifiers:

```
        .string $"Start address: ${START:X4}" // Becomes "Start address: $C000"
```

#### Char Methods

| Method name | Purpose                                     | Example                   |
|-------------|---------------------------------------------|---------------------------|
| `size`      | Get the size (in bytes) of the character    | `'\u03c0'.size() // 2`    |
| `toString`  | Convert the character to a string           | `'H'.toString()`          |

#### String Methods

| Method name | Purpose                                     | Example                                   |
|-------------|---------------------------------------------|-------------------------------------------|
| `concat`    | Concatenate the string with another         | `"HELLO".concat("WORLD") // "HELLOWORLD"` |
| `contains`  | Test if the string contains a character     | `"HI".contains('I') // true`              |
| `indexOf`   | Get the index of a character in a string    | `"HELLO".indexOf('g') // -1 (not found)`  |
| `len`       | Get the length of the string                | `U"HELLO".len() // 5`                     |
| `size`      | Get the size (in bytes) of the string       | `U"HELLO".size() // 20`                   |
| `substring` | Get a substring                             | `"HELLO".substring(0, 2) // "HE"`         |
| `toArray`   | Convert the string to an array of chars     | `"HI".toArray() // ['H','I']`             |
| `toLower`   | Get a lowercase version of the string       | `"HELLO".toLower() // "hello"`            |
| `toString`  | Copy the string                             | `"WORLD".toString()`                      |
| `toUpper`   | Get an uppercase version of the string      | `"world".toUpper() // "WORLD"`            |

#### Character and String Operations

Characters and strings can play a dual role in expressions. Generally they are converted to their numeric equivalent (encoded value) in unary expressions and in binary expressions with numbers.

```
    -'I' // -73
    'H'+3 // 75
    4*"A" // 260
```
If the left hand side is a string, then the only operation available is concatenate `+`, and the right hand expression is converted to a string if necessary.

```
        .string "HELLO"+", WORLD" // evaluates as "HELLO, WORLD"
        .string "A"+2   // "A2"
```

Individual characters in strings can be accessed by index with the subscript (`[]`) operator. For accessing elements, positive index or range elements are zero-based, negative the n-to-the-last element, like in Python.

```
        "HELLO, WORLD"[4] // 'O'
```

A negative index accesses the string in reverse order.

```
        "HELLO, WORLD"[-1] // 'D'
```

String slicing is possible by passing a range instead of an index. The start index of the range is optional if the end is provided, and vice versa. The end index is exclusive unless preceded by a `^`.

```
        .string "GOODBYE, CRUEL WORLD"[..3] // "GOO"
        .string "GOODBYE, CRUEL WORLD"[..^3] // "GOOD"
        .string "HELLO, AGAIN!"[7..] // "AGAIN!"
```

### Array and Tuple Types

Arrays and tuples are collections of data elements that can be accessed according to their declared order. Arrays require all elements to share the same type, while tuples do not.

An array is declared like so:

```
HIGH_SCORES = [7650, 6100, 5950, 5050, 4300]
```
Arrays can be multi-dimensional as well.

```
maze_data = [
    [ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ],
    [ -1,  0,  0,  0, -1, -1,  0,  0,  0, -1 ],
    [ -1,  0, -1, -1, -1, -1, -1, -1,  0, -1 ],
    [ -1,  0, -1,  0,  0,  0,  0, -1,  0, -1 ],
    ...
]
```
A tuple is declared likewise:

```
("HISCORE", 7650)

// tuple of tuples
(("HISCORE", 7650), ("1UP", "00"))
```

#### Array Methods

| Method name | Purpose                                                      | Example                                              |
|-------------|--------------------------------------------------------------|------------------------------------------------------|
| `concat`    | Concatenate the array with another                           | `[1,2].concat([3,4]) // [1,2,3,4]`                   |
| `contains`  | Test if the array contains an element                        | `[1,2].contains(3) // false`                         |
| `every`     | Test if every element satisfies a condition in the predicate | `[1,2,3,4].every((num) => num % 2 == 0) // false`    |
| `filter`    | Get all elements that satisfy the condition in the predicate | `[1,2,3,4].filter((num) => num % 2 == 0) // [2,4]`   |
| `indexOf`   | Get the index of an element in an array                      | `[4,5,6].contains(3) // -1 (not found)`              |
| `intersect` | Get the intersecting elements of this array and another      | `[1,2,3,4].intersect([2,4,6,8]) // [2,4]`            |
| `len`       | Get the length of the array                                  | `[1,2,3].len() // 3`                                 |
| `map`       | Get a new array transformed by the applied callback          | `[1,2,3].map((n) => n * 2) // [2,4,6]`               |
| `reduce`    | Perform a singular operation on all elements in the array    | `[1,2,3].reduce((n1, n2) => n1 + n2) // 6`           |
| `reverse`   | Reverse the array                                            | `[1,2].reverse() // [2,1]`                           |
| `size`      | Get the size (in bytes) of the array                         | `[23,400].size() // 3`                               |
| `skip`      | Get a subsequence skipping n elements                        | `[1,2,3,4].skip(2) // [3,4]`                         |
| `some`      | Test if one or more element satisifies a condition           | `[1,2,3,4].some((num) => num % 2 == 0) // true`      |
| `sort`      | Sort the array optionally with a custom sorter function      | `1,2].sort(mycomparer)`                              |
| `take`      | Get the first n elements of the array                        | `[1,2,3,4].take(2) // [1,2]`                         |
| `toString`  | Get a string representation of the array                     | `[1,2].toString() // "[1,2]"`                        |
| `toTuple`   | Convert the array to a tuple                                 | `[1,2].toTuple() // (1,2)`                           |
| `union`     | Merge this array with another, removing duplicates           | `[1,2,3,4].union([3,4,5]) // [1,2,3,4,5]`            | 

#### Tuple Methods

| Method name | Purpose                                                      | Example                                                              |
|-------------|--------------------------------------------------------------|----------------------------------------------------------------------|
| `contains`  | Test if the tuple contains an element                        | `(1,2).contains(3) // false`                                         |
| `len`       | Get the length of the array                                  | `[1,2,3].len() // 3`                                                 |
| `size`      | Get the size (in bytes) of the tuple                         | `(23,400).size() // 3`                                               |
| `skip`      | Get a subsequence skipping n elements                        | `(1,2,3,4).skip(2) // (3,4)`                                         |
| `take`      | Get the first n elements of the array                        | `(1,2,3,4).take(2) // (1,2)`                                         |
| `toArray`   | Convert the tuple to an array (if possible)                  | `(1,2).toArray() // [1,2]`                                           |
| `toString`  | Get a string representation of the tuple                     | `(1,2).toString() // "(1,2)"`                                        |

#### Array and Tuple Operations

Element access and collection slicing for arrays and tuples takes the same forms as that of strings. Individual elements are referenced by index, subsequences by range. 

```
myarray = [1, 2, 3, 4, 5]
   lda #myarray[1] // same as lda #2
mytuple = ("something","else")
    .string mytuple[-1] // same as "else"

mysubarray := ["first","second","third","fourth"][1..2] 
// mysubarray is ["second","third"]
```
In the examples above, each parameter of the range is optional. The end position is understood as "exclusive of" unless preceded by a `^`:

```
[1,2,3,4,5][2..^4] // [3,4,5]
```

#### Array Operations

| Operator          | Type                          | Example                                       |
|-------------------|-------------------------------|-----------------------------------------------|
| `+`               | Concatenate                   | `[1,2] + [3,4] ([1,2,3,4])`                   |

#### Tuple Operations 

Tuple assignments can look like other assignment expressions, where a single constant or variable is assigned to the tuple object itself.

```
mytuple = (200, "TWO HUNDRED")
```

In addition, each tuple element can be assigned to a corresponding symbol, such that the left hand side and right hand side expressions are both tuples.

```
(points, message) = (200, "TWO HUNDRED")

        .byte points
        .string message
```

### Dictionary Types

Dictionaries (also called hash tables or maps in other languages) are associative arrays of key-value pairs, where each key in the collection is unique and corresponds to a value.

```
points = { 10: "GOOD", 100: "GREAT", 1000: "FANTASTIC!" }
```

Keys in a dictionary must share the same type, as must values, though keys do not need to be of the same type as their values. Key types can only be primitives (booleans, characters and numbers) or strings. Values can be any type.

If the dictionary key type is a string and a string key begins with a letter or underscore, the dictionary can be initialized as:

```
city_populations = { 
    .london: 9_000_000, 
    .paris: 10_000_000,
    .new_york: 20_000_000,
    .shanghai: 40_000_000
}

    // internally keys are strings so are accessed accordingly
    .echo city_populations["london"] // 9000000
```

Likewise, such a key's value can be accessed through dot notation:

```
    .echo city_populations.new_york // 20000000
```

#### Dictionary Methods

| Method name   | Purpose                                           | Example                                       |
|---------------|---------------------------------------------------|-----------------------------------------------|
| `concat`      | Concatenate the dictionary with another           | `{"k1":1}.concat({"k2:2}) // {"k1":1,"k2":2}` |
| `containsKey` | Test if the dictionary contains a given key       | `{"k1":1}.containsKey("k2") // false`         |
| `keys`        | Get the dictionary keys as an array               | `{"k1":1,"k2":2}.keys() // ["k1","k2"]`       |
| `len`         | Get the length of the dictionary                  | `{"k1":1,"k2":2}.len() // 2`                  |
| `size`        | Get the size (in bytes) of the dictionary values  | `{"k1":u8"HELLO"}.size() // 5`                |
| `toString`    | Get a string representation of the dictionary     | `{"k1":1}.toString() // "{"k1":1}"`           |

#### Dictionary Operations

| Operator          | Type                          | Example                                       |
|-------------------|-------------------------------|-----------------------------------------------|
| `+`               | Concatenate                   | `{"1":1}+{"2":2} ({"1":1,"2":2})`             |

### Functions

Functions are objects that encapsulate code or some functionality. They might accept parameters and return a value. Neither is required in the definition, but if the function is called as part of an expression then it must return a value.

Functions share some similarities with [macros](/Docs/Macros.md), particularly in that they can have arguments, including default arguments. Unlike macros, function bodies cannot contain any assembly code or pseudo-ops.

A function can be declared in one of two ways. The first way is to use the `.function` directive.

```
timestwo .function num
        .return num * 2
        .endfunction
```

A function that is declared this way must have a unique identifier and can only be declared in the global scope. The function body itself only accepts full statements. 

Parameters can be assigned optional default values.

```
calculate_basic \
    .function sob=2049, eob=0xa000
        .return eob-sob
    .endfunction
basic_size = calculate_basic() // 38911
custom_size = calculate_basic(0x900) // 38656

```

A function can call itself recursively.

```
factorial   .function n
            .return n < 2 ? 1 : n * factorial(n - 1) 
            .endfunction
```

Another way to define a function is with arrow `=>` notation, a more compact and "modern" approach. The function can be an expression body or statement block.

```
timestwo = (n) => n * 2 // single expression

// a compare function as a block
compare = (n1, n2) {
    .if n2 < n1
        .return -1
    .elseif n2 > n1
        .return 1
    .endif
    .return 0
}
```

Functions can be passed as parameters to other functions and methods, and can be assigned to constants and variables as well.

```
timestwo = (n) => n * 2
t2 = timestwo

    .echo t2(4) // prints "8"
```

In the above example the constant `t2` is treated as a reference to the original function `timestwo`. 

Use `.invoke` to call a function or method as a standalone statement. The return value (if any) is discarded.

```
        .invoke myfunnyfunction()
```

#### Function Methods

| Method name   | Purpose                                           | Example           | 
|---------------|---------------------------------------------------|-------------------|
| `toString`    | Get a string representation of the function type  | myfunc.toString() |

The `toString` method only reports runtime information about the function's type and parameter count.