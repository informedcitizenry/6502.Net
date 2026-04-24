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

namespace Sixty502DotNet.Shared.Parse.Ast;

public enum RangeType
{
    IsIndex,
    IsRange,
    IsRangeIncludesEnd
}

public sealed class Range : Ast
{
    public Range(Expression start)
    {
        LeftToken = start.LeftToken;
        RightToken = start.RightToken;
        Start = start;
        End = null;
        Type = RangeType.IsIndex;
    }
    
    public Range(Expression? start, Expression? end, Token rangeOp, RangeType type)
    {
        LeftToken = start?.LeftToken ?? rangeOp;
        RightToken = end?.RightToken ?? rangeOp;
        Start = start;
        End = end;
        Type = type;
    }
    
    public Expression? Start { get; }
    
    public Expression? End { get; }

    public RangeType Type { get; }
}