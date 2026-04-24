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

namespace Sixty502DotNet.Shared.Parse.Ast;

public interface IStatementVisitor<out T>
{
    T VisitConstantAssignStatement(ConstantAssignStatement statement);
    
    T VisitVarAssignmentStatement(VarAssignmentStatement statement);
    
    T VisitCpuInstructionStatement(CpuInstructionStatement statement);

    T VisitI86RepInstructionStatement(I86RepInstructionStatement statement);
    
    T VisitSimpleDirectiveStatement(SimpleDirectiveStatement statement);
    
    T VisitMultiExpressionDirectiveStatement(MultiExpressionDirectiveStatement statement);
    
    T VisitSingleExpressionDirectiveStatement(SingleExpressionDirectiveStatement statement);
    
    T VisitPseudoOpStatement(PseudoOpStatement statement);
    
    T VisitLabelStatement(LabelStatement statement);
    
    T VisitLabeledBlockStatement(LabeledBlockStatement statement);
    
    T VisitNamespaceBlockStatement(NamespaceBlockStatement statement);
    
    T VisitEnumDeclaration(EnumDeclaration statement);
    
    T VisitPageBlockStatement(PageBlockStatement statement);
    
    T VisitAnonymousBlockStatement(AnonymousBlockStatement statement);
    
    T VisitFunctionDefinitionStatement(FunctionDefinitionStatement statement);
    
    T VisitIfStatement(IfStatement statement);
    
    T VisitForStatement(ForStatement statement);
    
    T VisitForeachStatement(ForeachStatement statement);
    
    T VisitSwitchStatement(SwitchStatement statement);
    
    T VisitExpressionBlockStatement(ExpressionBlockStatement statement);
    
    T VisitModule(BlockStatement statement);
    
    T VisitEofStatement(EofStatement statement);
    
    T Visit(Statement statement);
}