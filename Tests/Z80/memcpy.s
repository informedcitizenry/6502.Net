//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

/* 
Simple block copy
Inputs: BC - num
        DE - source
        HL - dest
*/
        .cpu "i8080"
memcpy  .block
        mov     a,b
        ora     c
        rz
loop    ldax    d
        mov     m,a
        inx     d
        inx     h
        dcx     b
        mov     a,b
        ora     c
        jnz     loop
        ret
        .endblock