//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

grammar Json;

json
    :   value EOF
    ;

object
    :   '{' members? '}'
    ;

array
    :   '[' elements? ']'
    ;

members
    :   member (',' member)*
    ;

member
    :   String ':' value
    ;

elements
    :   value (',' value)*
    ;

value
    :   object
    |   array
    |   String
    |   Number
    |   'true'
    |   'false'
    |   'null'
    ;

String
    :   '"'  SChar* '"'
    ;

Number
    :   '-'? Integer ('.' Digit+)? Exponent?
    ;

WS
    :   [ \n\r\t] -> skip
    ;

fragment
SChar
    :   ~[\\\u0000-\u001f"]
    |   Escape
    ;

fragment
Escape
    :   '\\' 
    (   [\\bfnrt/'"]
    |   'u' HexDigit HexDigit HexDigit HexDigit
    )
    ;

fragment
Exponent
    :   [eE] [\-+]? Digit+
    ;

fragment
HexDigit
    :   '0' .. '9'
    |   'a' .. 'f'
    |   'A' .. 'F'
    ;

fragment
Integer
    :   '0'
    |   '1' .. '9' Digit*
    ;

fragment
Digit
    :   '0' .. '9'
    ;
