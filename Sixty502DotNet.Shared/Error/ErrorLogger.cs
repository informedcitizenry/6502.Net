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

using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Error;

public sealed class ErrorLogger(bool warningsAsErrors)
{
    private readonly HashSet<CompileError> _errors = [];

    private readonly HashSet<CompileError> _warnings = [];


    public void LogError(CompileException exception) 
        => LogError(exception.Type.Stringified(), exception.Offender);

    public void LogError(ParserException exception)
    {
        var message = $"Expected {exception.Expected.Stringified()} but found {exception.Found.Stringified()}";
        LogError(message, exception.Offender);
    }

    public void LogError(InvalidUnaryOperationException exception)
    {
        var message =
            $"{exception.Type.Stringified()} on {exception.Val.TypeDisplayName()}";
        LogError(message, exception.Offender);
    }
    
    public void LogError(InvalidBinaryOperationException exception)
    {
        var message =
            $"{exception.Type.Stringified()} on {exception.LeftVal.TypeDisplayName()} and {exception.RightVal.TypeDisplayName()}";
        LogError(message, exception.Offender);
    }

    public void LogError(TypeException exception)
    {
        var expected = exception.Expected.Stringified();
        var found = exception.Found.TypeDisplayName();
        var message = $"{exception.Type.Stringified()}: Expected {expected} but found {found}";
        LogError(message, exception.Offender);
    }
    
    public void LogError(IntegerOverflowException exception)
    {
        var article = exception.Bit == 8 ? "an " : "a ";
        if (exception.SignedOrUnsigned)
        {
            LogError($"Value must be able to be expressed as {article}{exception.Bit}-bit integer", exception.Offender);
            return;
        }
        var signPrefix = !exception.Signed ? "un" : string.Empty;
        var message = $"Value must be able to be expressed as {article}{exception.Bit}-bit {signPrefix}signed integer";
        LogError(message, exception.Offender);
    }
    
    public void LogError(UnresolvedDeclException exception)
    {
        var message = exception.OffenderPoint.Type switch
        {
            TokenType.Eof =>
                $"Expected {exception.Expected.Stringified()} but found end of file",
            _ => exception.Type.Stringified()
        };
        
        var originalDeclHighlightLength = exception.OriginDeclEnding.Location.Start + 
                                 exception.OriginDeclEnding.Text.Length -
                                    exception.OriginDeclBeginning.Location.Start;
        
        var highlightLength = exception.OffenderPoint.Type switch
        {
            TokenType.Eof => 1,
            _ => exception.OffenderPoint.Text.Length
        };
        var entry = new CompileError
        {
            Path = exception.OffenderPoint.Source.Name,
            Line = exception.OffenderPoint.Line,
            Column = exception.OffenderPoint.Column,
            Length = highlightLength,
            LineText = exception.OffenderPoint.GetLineText().ToString(),
            Message = message,
            IsError = true,
            IsFatal = false,
            Inclusions = exception.OriginDeclBeginning.Inclusions,
            OriginalDeclarationLine = exception.OriginDeclBeginning.Line,
            OriginalDeclarationColumn = exception.OriginDeclBeginning.Column,
            OriginalDeclarationLength = originalDeclHighlightLength,
            OriginalDeclarationLineText = exception.OriginDeclBeginning.GetLineText().ToString()
        };
        _errors.Add(entry);
    }

    public void LogError(string message, Ast ast) => LogEntry(message, ast, true);

    public void LogFatalError(string message)
    {
        var error = new CompileError
        { 
            IsError = true,
            IsFatal = true,
            Message = message,
            Length = message.Length,
            Line = 0,
            Column = 0,
            LineText = string.Empty,
            Inclusions = [],
            Path = string.Empty
        };
        _errors.Add(error);
    }

    public void LogGeneralWarning(string message) 
        => _warnings.Add(new CompileError{Message = message, IsFatal = true, IsError = false, Inclusions = []});

    public void LogWarning(string message, Ast ast)
        => LogEntry(message, ast, false);

    private void LogEntry(string message, Ast ast, bool isError)
    {
        isError |= warningsAsErrors;
        if (!isError && CheckLogWarnings != null && !CheckLogWarnings()) return;
        var entry = new CompileError
        {
            Path = ast.LeftToken.Source.Name,
            Line = ast.LeftToken.Line,
            Column = ast.LeftToken.AdjustedColumn,
            Length = GetErrorLineTextLength(ast.LeftToken, ast.RightToken),
            LineText = ast.LeftToken.GetLineText().ToString(),
            Message = message,
            IsError = isError,
            IsFatal = false,
            Inclusions = ast.LeftToken.Inclusions
        };
        if (isError)
        {
            _errors.Add(entry);
        }
        else
        {
            _warnings.Add(entry);
        }
    }

    private static int GetErrorLineTextLength(Token leftToken, Token rightToken)
    {
        var end = leftToken.Location.Start;
        while (end < rightToken.Location.End && !leftToken.Source.Text[end].IsVerticalWhitespace())
        {
            end++;
        }
        var length = end - leftToken.Location.Start;
        return length > 80 ? 80 : length;
    }
    
    public int ErrorCount => _errors.Count;
    
    public Func<bool>? CheckLogWarnings { get; set; }
    
    public IReadOnlyList<CompileError> GetErrors() => _errors.ToList().AsReadOnly();

    public IReadOnlyList<CompileError> GetWarnings() => _warnings.ToList().AsReadOnly();
}