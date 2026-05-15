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

public sealed class ParserException(TokenType expected, Token found)
    : CompileException(CompileExceptionType.ExpectedTokenException, new PrimaryExpression(found))
{
    public TokenType Expected { get; } = expected;
    public TokenType Found { get; } = found.Type;
}

public sealed class UnresolvedDeclException
(
    CompileExceptionType type,
    TokenType expected, 
    Token originDeclBeginning, 
    Token originDeclEnding, 
    Token offenderPoint
) : CompileException(type, new PrimaryExpression(originDeclBeginning))
{
    public TokenType Expected { get; } = expected;
    
    public Token OriginDeclBeginning { get; } = originDeclBeginning;
    
    public Token OriginDeclEnding { get; } = originDeclEnding;
    
    public Token OffenderPoint { get; } = offenderPoint;
}