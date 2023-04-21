# Built-In Symbols

## Constants

| Name          | Value                                     |
|---------------|-------------------------------------------|
| CURRENT_PASS  | The current assembly pass                 |
| INT8_MAX	    | Signed 8-bit maximum value (127)          |
| INT8_MIN	    | Signed 8-bit mininum value (-128)         |
| INT16_MAX	    | Signed 16-bit maximum value (32768)       |
| INT16_MIN	    | Signed 16-bit minimum value (-32768)      |
| INT24_MAX	    | Signed 24-bit maximum value (8388607)     |
| INT24_MIN	    | Signed 24-bit minimum value (-8388608)    |
| INT32_MAX	    | Signed 32-bit maximum value (2147483647)  |
| INT32_MIN     | Signed 32-bit minimum value (-2147483648) |
| MATH_E	    | Constant *e*                              |
| MATH_PI	    | Constant π                                |
| MATH_TAU	    | Constant τ (2×π)                          |
| UINT8_MAX	    | Unsigned 8-bit maximum value (255)        |
| UINT8_MIN	    | Unsigned 8-bit minimum value (0)          |
| UINT16_MAX    | Unsigned 16-bit maximum value (65535)     |
| UINT16_MIN	| Unsigned 16-bit minimum value (0)         |
| UINT24_MAX	| Unsigned 24-bit maximum value (16777215)  |
| UINT24_MIN	| Unsigned 24-bit minimum value (0)         |
| UINT32_MAX	| Unsigned 32-bit maximum value (4294967295)|     
| UINT32_MIN	| Unsigned 32-bit minimum value (0)         |

## Functions

Several built-in functions are available to the programmer.

### **`abs(`** *`value`* **`)`**

The absolute value of a number.

```
    abs(-1) // 1
```

### **`acos(`** *`value`* **`)`**

The arccosine, or inverse cosine, of a number.

```
    acos(-1) // 3.141592653589793
```

### **`atan(`** *`value`* **`)`**

The arctangent of a number.

```
    atan(1) // 0.7853981633974483
```

### **`byte(`** *`value`* **`)`**

Casts a signed number between -128 and 127 into its unsigned form.

```
    byte(-127) // 81
```

### **`cbmflt(`** *`address`* **`)`**

Converts (or attempts to convert) binary output in unpacked CBM/MBF floating point format into a double floating point number, starting from the address parameter.

```
    // assume in the assembled output, $d000-$d005 contains 82 00 49 0f da
    cbmflt($d000) // 3.1415926534682512
    
```

### **`cbmfltp(`** *`address`* **`)`**

Converts (or attempts to convert) binary output in packed CBM/MBF floating point format into a double floating point number, starting from the address parameter.

```
    // assume $d000-$d004 contains 82 49 0f da
    cbmflt($d000) // 3.1415926534682512
    
```

### **`cbrt(`** *`value`* **`)`**

The cubed root of a number

```
    cbrt(2048383) // 127
```

### **`ceil(`** *`value`* **`)`**

Round up expression.

```
    ceil(0.1) // 1
```

### **`char(`** *`codepoint`* **`)`**

Converts the codepoint argument into a string. The codepoint must be valid Unicode.

```
    char(0x41) // "A"
```

### **`cos(`** *`value`* **`)`**

The cosine of a number.

```
    cos(1) // 0.54030230586
```

### **`cosh(`** *`value`* **`)`**

The hyperbolic cosine of a number.

```
    cosh(1) // 1.54308063482
```
### **`deg(`** *`value`* **`)`**

The degree from radians.

```
    deg(1) // 57.2958
```
### **`dword(`** *`value`* **`)`**

Casts a signed number between -2147483648 and 2147483647 into its unsigned form.

```
    dword(-32768) // 32768
```

### **`exp(`** *`value`* **`)`**

The exponential of *e*.

```
    exp(16) // 8886110.52051
```

### **`floor(`** *`value`* **`)`**

Round down.

```
    floor(1.9) // 1
```

### **`format(`** *`format`* [ **`,`** *`expression`* { **`,`** *`expression`* } ] **`)`**

Converts objects to a string in the format specified. The format string must adhere to [Microsoft .Net standards](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).

```
    format("Address: ${0:X4}", 49152) // "$C000"
```

### **`frac(`** *`value`* **`)`**

The fractional part.

```
    frac(5.18) // 0.18
```

### **`hypot(`** *`pole1`* **`,`** *`pole2`* **`)`**

Polar distance.

```
    hypot(4,3) // 5
```

### **`ln(`** *`value`* **`)`**

The natural logarithm.

```
    ln(2048) // 7.62461898616
```

### **`log(`** *`value`* **`,`** *`base`* **`)`**

The logarithm in base.

```
    log(2048, 2) // 11
```

### **`log10(`** *`value`* **`)`**

The common logarithm.

```
    log(10000) // 4
```

### **`long(`** *`value`* **`)`**

Casts a signed number between -8388608 and 8388607 into its unsigned form.

```
    long(-32768) // 32768
```

### **`peek(`** *`offset`* **`)`**

Lookup generated code output at the offset.

```
    // assume $c000 is 0xa9
    peek($c000) // 0xa9
```

### **`poke(`** *`offset`* **`,`** *`value`* **`)`**

Mutate code generation at the offset and return the value.

### **`pow(`** *`value`* **`,`** *`power`* **`)`**

Raise to the power.

```
    pow(3, 3) // 9
```

### **`rad(`** *`value`* **`)`**

Radians from degrees.

```
    rad(180) // 3.141592653589793
```

### **`random(`** *`start`* **`,`** *`end`* **`)`**

Generate a random number within the specified range of numbers. Both arguments can be negative or positive, but the second argument must be greater than the first, and the difference between them can be no greater than the maximum value of a signed 32-bit integer. This is a .Net limitation.

```
    random(2, 35) // 17 (maybe)
```

### **`range(`** [ *`start`* **`,`** ] *`stop`* [ **`, `**`step`* ] **`)`**

Generate a sequence of integers until a specified stop point. By default the sequence starts at 0 and increments by a step of 1.

```
    range(5)        // [0,1,2,3,4]
    range(-1,-6,-1) // [-1,-2,-3,-4,-5]
```

### **`round(`** *`value`* **`)`**

Round number.

```
    round(18.6) // 19
```

### **`section(`** *`section`* **`)`**

The starting address of a section.

```
    .dsection "zp", $02, $100
    .echo section("zp") // $02
```

### **`sgn(`** *`value`* **`)`**

The signum.

```
    sgn(-3) // -1
    sgn(0)  //  0
    sgn(34) //  1
```

### **`sin(`** *`value`* **`)`**

The sine of a number.

```
    sin(34) // 0.52908268612
```

### **`sinh(`** *`value`* **`)`**

The hyperbolic sine of a number.

```
    sinh(4) // 27.2899171971
```

### **`sizeof(`** *`value`* **`)`**

The number of bytes generated by the expression. 

```
    sizeof(18404) // 2
    // same as 18404.size()
```

### **`sqrt(`** *`value`* **`)`**

The square root of a number.

```
    sqrt(25) // 5
```

### **`tan(`** *`value`* **`)`**

The tangent of a number.

```
    tan(1) // 1.55740772465
```

### **`tanh(`** *`value`* **`)`**

The hyperbolic tangent of a number.

```
    tanh(1) // 0.76159415595
```

### **`typeof(`** *`expression`* **`)`**

The string representation of the expression's type.

```
mynum = 3.2
myarray = [mynum, 4.5, 6.3]
mynumtype = typeof(mynum) // "String"
myarraytype = typeof(myarray) // "Array<Double>"
```

### **`word(`** *`value`* **`)`**

Casts a signed number between -32768 and 32767 into its unsigned form.

```
    word(-512) // 65024
```
