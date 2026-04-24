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

namespace Sixty502DotNet.Shared.Parse.Ast;

public interface IExpressionVisitor
{
    Value? VisitPrimaryExpression(PrimaryExpression expression);

    Value? VisitAnonymousRefExpression(AnonymousRefExpression expression);
    
    Value? VisitBinaryOpExpression(BinaryOpExpression expression);
    
    Value? VisitTernaryExpression(TernaryExpression expression);
    
    Value? VisitUnaryOpExpression(UnaryOpExpression expression);
    
    Value? VisitSubscriptExpression(SubscriptExpression expression);
    
    Value? VisitCallExpression(CallExpression expression);
    
    Value? VisitMemberExpression(MemberExpression expression);
    
    Value? VisitArrayInitExpression(ArrayInitExpression expression);
    
    Value? VisitDictionaryInitExpression(DictionaryInitExpression expression);
    
    Value? VisitFunctionExpression(FunctionExpression expression);
    
    Value? VisitInterpolationExpression(InterpolationExpression expression);
    
    Value? Visit(Expression expression);
}