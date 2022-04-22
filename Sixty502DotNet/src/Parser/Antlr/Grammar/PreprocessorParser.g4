//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

parser grammar PreprocessorParser;

options {
    tokenVocab=Sixty502DotNetLexer;
}

macroBlock
    :   macroDeclaration macroBody macroEnd EOF?
    ;

macroDeclaration
    :   macroArgList (MacroArgNewline | MacroDefaultNewline)
    |   MacroArgNewline
    ;

macroArgList
    :   macroArg ((MacroArgComma | MacroDefaultComma) macroArg)*
    ;

macroArg
    :   MacroArg macroArgDefaultAssignExpr?
    ;

macroArgDefaultAssignExpr
    :   MacroArgDefaultBegin macroArgDefaultExpr
    ;

macroArgDefaultExpr
    :
    (   MacroDefaultString
    |   MacroDefaultChar
    |   MacroDefaultText
    |   MacroDefaultLeftGroup
    |   MacroDefaultRightGroup
    |   MacroDefaultBackslash
    )   +
    ;

macroBody
    :   macroBodyElement+
    ;

macroBodyElement
    :   MacroSubstitution
    |   MacroBlockParamString
    |   MacroBlockUserString
    |   MacroBlockUserChar
    |   MacroBlockUserText
    ;

macroEnd
    :   MacroBlockEnd
    ;

macroInvocation
    :   macroInvocationArgList? (MacroInvokeEnd | EOF)
    ;

macroInvocationArgList
    :   macroInvocationArg (MacroInvokeComma macroInvocationArg)*
    ;

macroInvocationArg
    :   macroInvokeArgElement+
    ;

macroInvokeArgElement
    :   MacroInvokeSubstitution
    |   MacroInvokeString
    |   MacroInvokeChar
    |   MacroInvokeText
    |   MacroInvokeLeftGroup
    |   MacroInvokeRightGroup
    |   MacroInvokeBackslash
    ;