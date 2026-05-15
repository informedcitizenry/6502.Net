// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Error;

public enum CompileExceptionType
{
    InvalidOperation,
    InvalidIntLiteral,
    InvalidFloatLiteral,
    ValueOverflow,
    ValueCannotBeZero,
    ValueNotConstant,
    InvalidCharLiteral,
    InvalidStringLiteral,
    InvalidEscapeSequence,
    FormatPreviouslySpecified,
    InvalidFormat,
    FormatChangedAfterCompilation,
    SectionDefinedAfterCompilation,
    SectionNotFound,
    NoObjectBytesForSection,
    OutputException,
    IdentifierExpected,
    IdentifierExpectedBeforeCommand,
    TypeMismatch,
    DivideByZero,
    IndexOutOfRange,
    KeyNotFound,
    DuplicateKeyInDictionary,
    CannotBeKey,
    MismatchKeyTypes,
    MismatchValueTypes,
    MismatchArrayValueType,
    AnonymousReferenceNotFound,
    HasNoMemberSymbol,
    NotCallable,
    CallDepth,
    NoValueReturned,
    CompilationNotValidInFunction,
    DirectiveNotValidInFunction,
    RefNotValidInFunction,
    CannotSubscript,
    NotVarAssignment,
    NotInRootScope,
    InvalidBreak,
    InvalidContinue,
    InvalidReturn,
    InvalidCpuSpecified,
    StringLiteralExpected,
    LabelExpectedForGoto,
    CannotFindGoto,
    TooFewArguments,
    TooManyArguments,
    RelativeOffsetTooFar,
    SymbolNotFound,
    UnresolvableReference,
    RegisterCannotBeEvaluated,
    SymbolRedefined,
    SymbolThisReserved,
    InvalidFormatSpecifier,
    AddressingModeNotSupported,
    ExpectedOpenBrace,
    ExpectedExpression,
    ExpectedType,
    ProgramOverflow,
    InvalidPokeAddress,
    InvalidPeekAddress,
    InvalidCodePoint,
    UnexpectedExpression,
    UnexpectedToken,
    DefaultSpecified,
    CaseSpecified,
    CaseLabelNotConstant,
    FileNotFound,
    OffsetAndLengthOutOfRange,
    PageBoundaryCrossed,
    ExpectedTokenException,
    ParameterExpected,
    MacroPreviousDefined,
    DefaultValueNotSpecified,
    MacroNotDefined,
    MacroNameNotPermitted,
    InvalidExpressionStatement,
    CommandRequired,
    DirectiveDoesNotCloseCommand,
    DirectiveNotInSwitch,
    CannotInitMem,
    AddressOverflow,
    InvalidProgramCounter,
    InvalidAlignAmount,
    InvalidFillAmount,
    InvalidJson
}

public static class CompileExceptionExtensions
{
    
    public static string Stringified(this CompileExceptionType type)
    {
        return type switch
        {
            CompileExceptionType.InvalidOperation => "Invalid operation",
            CompileExceptionType.InvalidIntLiteral => "Invalid integer literal",
            CompileExceptionType.InvalidFloatLiteral => "Invalid float literal",
            CompileExceptionType.ValueOverflow => "Expression exceeds maximum allowed value",
            CompileExceptionType.ValueCannotBeZero => "Value cannot be zero",
            CompileExceptionType.ValueNotConstant => "Value is not constant",
            CompileExceptionType.InvalidCharLiteral => "Invalid char literal",
            CompileExceptionType.InvalidStringLiteral => "Invalid string literal",
            CompileExceptionType.InvalidEscapeSequence => "String literal contains one or more invalid escape sequences",
            CompileExceptionType.FormatPreviouslySpecified => "Format previously specified",
            CompileExceptionType.InvalidFormat => "The output format specified is not valid",
            CompileExceptionType.FormatChangedAfterCompilation => "Format cannot be set after compilation",
            CompileExceptionType.SectionDefinedAfterCompilation => "Section cannot be defined after compilation",
            CompileExceptionType.SectionNotFound => "Section cannot be found",
            CompileExceptionType.NoObjectBytesForSection => "No object bytes could be retrieved for section",
            CompileExceptionType.OutputException => "Could not write bytes to the output",
            CompileExceptionType.IdentifierExpected => "Identifier expected",
            CompileExceptionType.IdentifierExpectedBeforeCommand => "Command requires an identifier before it",
            CompileExceptionType.TypeMismatch => "Type mismatch",
            CompileExceptionType.DivideByZero => "Divide by zero error",
            CompileExceptionType.IndexOutOfRange => "Index was out of range",
            CompileExceptionType.KeyNotFound => "Key was not found",
            CompileExceptionType.DuplicateKeyInDictionary => "Duplicate key in dictionary expression",
            CompileExceptionType.CannotBeKey => "Expression cannot be key due to its type",
            CompileExceptionType.MismatchKeyTypes => "Dictionary keys must be compatible types",
            CompileExceptionType.MismatchValueTypes => "Dictionary values must be compatible types",
            CompileExceptionType.MismatchArrayValueType => "Array values must be compatible types",
            CompileExceptionType.AnonymousReferenceNotFound => "Anonymous referenced was not found",
            CompileExceptionType.HasNoMemberSymbol => "Expression or scope has no member",
            CompileExceptionType.NotCallable => "Expression is not a callable type",
            CompileExceptionType.CallDepth => "Maximum depth exceeded on recursive call",
            CompileExceptionType.NotInRootScope => "Command is only valid in the global scope",
            CompileExceptionType.NoValueReturned => "No value returned from function call",
            CompileExceptionType.NotVarAssignment => "Variable ssignment expected here",
            CompileExceptionType.CannotSubscript => "Subscript assignment is not valid for this type",
            CompileExceptionType.CompilationNotValidInFunction => "Compilation is not permitted inside function blocks",
            CompileExceptionType.DirectiveNotValidInFunction => "Directive is not permitted inside function blocks",
            CompileExceptionType.RefNotValidInFunction => "Anonymous reference labels are not permitted inside function blocks",
            CompileExceptionType.InvalidBreak => "Cannot break here",
            CompileExceptionType.InvalidContinue => "Cannot continue here",
            CompileExceptionType.InvalidReturn => "Nothing to return to",
            CompileExceptionType.InvalidCpuSpecified => "Invalid CPU specified",
            CompileExceptionType.InvalidPeekAddress => "Invalid peek address",
            CompileExceptionType.InvalidPokeAddress => "Invalid poke address",
            CompileExceptionType.InvalidCodePoint => "Invalid codepoint",
            CompileExceptionType.StringLiteralExpected => "String literal expected",
            CompileExceptionType.LabelExpectedForGoto => "`.goto` command expects a label",
            CompileExceptionType.CannotFindGoto => "Cannot find label to `.goto`",
            CompileExceptionType.TooFewArguments => "Too few arguments provided",
            CompileExceptionType.TooManyArguments => "Too many arguments provided",
            CompileExceptionType.RelativeOffsetTooFar => "Relative offset is too far",
            CompileExceptionType.SymbolNotFound => "Symbol was not found",
            CompileExceptionType.UnresolvableReference => "Reference cannot be resolved after first pass",
            CompileExceptionType.RegisterCannotBeEvaluated => "Register cannot be evaluated in this context",
            CompileExceptionType.SymbolRedefined => "Symbol cannot be updated or redefined",
            CompileExceptionType.SymbolThisReserved => "`this` is a reserved symbol name and cannot be defined",
            CompileExceptionType.InvalidFormatSpecifier => "Invalid specifier in string format",
            CompileExceptionType.AddressingModeNotSupported => "Addressing mode or operation not supported for CPU",
            CompileExceptionType.ExpectedOpenBrace 
                => "Expected `{` before statement block. For backwards compatibility, enable legacy blocks option to override this behavior",
            CompileExceptionType.ExpectedExpression => "Expression expected",
            CompileExceptionType.ProgramOverflow => "Program compilation overflow",
            CompileExceptionType.UnexpectedExpression => "Unexpected expression",
            CompileExceptionType.UnexpectedToken => "Unexpected token",
            CompileExceptionType.DefaultSpecified => "The default case previously specified",
            CompileExceptionType.CaseSpecified => "Case label previously specified",
            CompileExceptionType.CaseLabelNotConstant => "Case label must be a constant value",
            CompileExceptionType.FileNotFound => "File could not be opened or was not found",
            CompileExceptionType.OffsetAndLengthOutOfRange => "The specified offset and length exceed the file size",
            CompileExceptionType.PageBoundaryCrossed => "A page boundary was crossed",
            CompileExceptionType.ExpectedTokenException => "Expected token not found",
            CompileExceptionType.ExpectedType => "Expected type",
            CompileExceptionType.ParameterExpected => "Parameter was expected but not specified",
            CompileExceptionType.MacroPreviousDefined => "Macro previously defined",
            CompileExceptionType.DefaultValueNotSpecified => "Default value was expected but not specified",
            CompileExceptionType.MacroNotDefined => "Unrecognized directive",
            CompileExceptionType.MacroNameNotPermitted => "Macro name is not permitted because its invocation matches an existing command name",
            CompileExceptionType.InvalidExpressionStatement => "Invalid expression statement",
            CompileExceptionType.CommandRequired => "Command required",
            CompileExceptionType.DirectiveDoesNotCloseCommand => "Directive does not close command",
            CompileExceptionType.DirectiveNotInSwitch => "Directive is only valid in a switch statement",
            CompileExceptionType.CannotInitMem => "Cannot initialize memory after compilation has started",
            CompileExceptionType.AddressOverflow => "Address overflow",
            CompileExceptionType.InvalidProgramCounter => "Invalid program counter specified",
            CompileExceptionType.InvalidAlignAmount => "Invalid align amount",
            CompileExceptionType.InvalidFillAmount => "Invalid fill amount",
            CompileExceptionType.InvalidJson => "Expression is not valid JSON",
            _ => "Unknown error"
        };
    }
}

public class CompileException : Exception
{
    public CompileException(CompileExceptionType exceptionType, Ast offender)
    {
        Type = exceptionType;
        Offender = offender;
    }

    public CompileException(CompileExceptionType exceptionType, Token offender)
    {
        Type = exceptionType;
        Offender = new PrimaryExpression(offender);
    }
    
    public CompileExceptionType Type { get; }
    
    public Ast Offender { get; }
}
