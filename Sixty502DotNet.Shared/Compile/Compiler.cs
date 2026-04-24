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

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Arch.Formats;
using Sixty502DotNet.Shared.Encode;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Function;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;
using System.Text;

namespace Sixty502DotNet.Shared.Compile;

public class Compiler : IStatementVisitor<Jump>
{
    private readonly AssemblyState _assemblyState;
    private readonly Evaluator _evaluator;

    public Compiler(AssemblyState assemblyState)
    {
        _assemblyState = assemblyState;
        _evaluator = new Evaluator(_assemblyState);
    }

    public Jump VisitModule(BlockStatement statement)
    {
        var jump = CompileBlock(statement.Statements);
        if (jump.Type is JumpType.None or JumpType.Exit) return jump;
        var jumpError = jump.Type switch
        {
            JumpType.Break => CompileExceptionType.InvalidBreak,
            JumpType.Continue => CompileExceptionType.InvalidContinue,
            JumpType.Goto => CompileExceptionType.CannotFindGoto,
            _ => CompileExceptionType.InvalidReturn
        };
        Ast? ast;
        if (jump is { Type: JumpType.Goto, JumpStatement: SingleExpressionDirectiveStatement label })
        {
            ast = label.Expression;
        }
        else
        {
            ast = jump.JumpStatement;
        }
        _assemblyState.Logger.LogError(jumpError.Stringified(), ast ?? statement.Statements[^1]);
        return Jump.NoJump;
    }

    private Jump CompileConstantAssign
    (
        Expression left, 
        Token op, 
        Value constVal, 
        Expression valueExpr,
        bool genListing
    )
    {
        if (!constVal.IsRValue())
            throw new CompileException(CompileExceptionType.InvalidOperation, valueExpr);
        if (constVal.TypeTag == TypeTag.Function && 
            valueExpr is FunctionExpression && 
            _assemblyState.Passes > 0)
        {
            return Jump.NoJump;
        }
        if (left is ArrayInitExpression { IsTuple: true } arrayInitExpr )
        {
            return CompileTupleAssignment
            (
                arrayInitExpr, 
                op, 
                constVal, 
                valueExpr, 
                true, 
                genListing
            );
        }
        if (left is not PrimaryExpression)
        {
            throw new CompileException(CompileExceptionType.InvalidOperation, op);
        }
        var symbol = left.LeftToken.Text.ToString();
        if (symbol == "*")
        {
            return CompilePcAssignment(left, op, constVal, genListing, valueExpr);
        }
        if (symbol.Equals("this", _assemblyState.Comparison))
        {
            throw new CompileException(CompileExceptionType.SymbolThisReserved, left);
        }
        if (left.LeftToken.Column > 0 && _assemblyState.AssemblyOptions.WarnLeftSpaceOfLabel)
        {
            _assemblyState.Logger.LogWarning("Whitspace precedes label", left);
        }
        if (left.LeftToken.Type.IsRegister() && 
            _assemblyState.AssemblyOptions.WarnRegistersAsIdent &&
            !_assemblyState.SymbolTable.InFunction)
        {
            _assemblyState.Logger.LogWarning($"Symbol `{symbol}` is also a register name", left);
        }
        var canDefine = op.Type == TokenType.GlobalKw
            ? _assemblyState.SymbolTable.TryDefineGlobal(left.LeftToken, constVal, out var existing)
            : _assemblyState.SymbolTable.TryDefineConstant(left.LeftToken, constVal, out existing);
        if (!canDefine)
        {
            if (_assemblyState.Passes == 0)
            {
                throw new CompileException(CompileExceptionType.SymbolRedefined, left);
            }
            _assemblyState.PassNeeded |= 
                !_assemblyState.SymbolTable.InFunction && 
                existing?.Equals(constVal) == false;
        }
        if (genListing)
        {
            GenAssignmentListing(left, '=', constVal);
        }
        return Jump.NoJump;
    }
    
    public Jump VisitConstantAssignStatement(ConstantAssignStatement statement)
    {
        var constVal = _evaluator.Visit(statement.Value);
        return constVal == null 
            ? Jump.NoJump 
            : CompileConstantAssign
            (
                statement.ConstSymbol, 
                statement.Operator, 
                constVal, 
                statement.Value,
                true
            );
    }

    private Jump CompileVarAssignment
    (
        Expression left, 
        Token op, 
        Value rightValue, 
        Expression right,
        bool genListing
    )
    {
        if (!rightValue.IsRValue())
            throw new CompileException(CompileExceptionType.InvalidOperation, right);

        if (left is ArrayInitExpression { IsTuple: true } tupleExpr)
        {
            return CompileTupleAssignment
            (
                tupleExpr, 
                op, 
                rightValue, 
                right, 
                false, 
                genListing
            );
        }
        Value? symbol = null;
        var memberExpr = left as MemberExpression;
        var subscript = left as SubscriptExpression;

        if (IsLValueConstant(left))
        {
            if (memberExpr != null)
                throw new CompileException(CompileExceptionType.SymbolRedefined, memberExpr.Member);
            throw new CompileException
            (
                CompileExceptionType.SymbolRedefined, 
                subscript?.Left ?? left
            );
        }
        if (subscript != null ||
            memberExpr != null || 
            (op.Type != TokenType.Eq && op.Type != TokenType.ColonEq))
        {
            _ = _evaluator.TryFetchSymbolValue(left, out symbol);
            if (symbol == null)
            {
                var offender = left.RightToken;
                while (offender.Type != TokenType.Ident)
                {
                    if (left is SubscriptExpression se)
                    {
                        left = se.Left;
                    }
                    else
                    {
                        offender = left.LeftToken;
                        break;
                    }
                    offender = left.RightToken;
                }
                throw new CompileException
                (
                    CompileExceptionType.SymbolNotFound,
                    offender
                ); 
            }
            if (subscript != null)
            {
                return CompileSubscriptAssignment(subscript, symbol, op, rightValue, right, genListing);
            }
            if (memberExpr != null)
            {
                return CompileMemberAssignment(memberExpr, symbol, op, rightValue, right, genListing);
            }
        }
        var newValue = EvalValues.EvalAssign
        (
            symbol, 
            rightValue, 
            new BinaryOpExpression(left, op, right),
            _assemblyState.TextEncodingCollection
        );
        if (newValue == null) return Jump.NoJump;
        
        var symbolName = left.LeftToken.Text.ToString();
        if (symbolName.Equals("this", _assemblyState.Comparison))
        {
            throw new CompileException(CompileExceptionType.SymbolThisReserved, left);
        }
        if (symbolName == "*")
        {
            return CompilePcAssignment(left, op, rightValue, genListing, right);
        }
        if (genListing)
        {
            GenAssignmentListing(left, '=', newValue);
        }
        if (left.LeftToken.Type.IsRegister() && 
            _assemblyState.AssemblyOptions.WarnRegistersAsIdent &&
            !_assemblyState.SymbolTable.InFunction)
        {
            _assemblyState.Logger.LogWarning($"Symbol `{symbolName}` is also a register name", left);
        }
        _assemblyState.SymbolTable.DefineOrUpdateVariable(left.LeftToken, newValue);
        return Jump.NoJump;
    }

    private Jump CompileMemberAssignment
    (
        MemberExpression memberExpr,
        Value? symbol,
        Token op,
        Value rightValue,
        Expression right,
        bool genListing
    )
    {
        var newValue = EvalValues.EvalAssign
        (
            symbol, 
            rightValue, 
            new BinaryOpExpression(memberExpr, op, right),
            _assemblyState.TextEncodingCollection
        );
        if (newValue == null) return Jump.NoJump;
        var sym = memberExpr.Member.Text.ToString();
        if (symbol?.AsDictionary() is { } memberDict)
        {
            var dictMember = new Value(sym, TextEncodingType.Default);
            if (!memberDict.ContainsKey(dictMember))
                throw new CompileException(CompileExceptionType.KeyNotFound, memberExpr.Member);
            if (!memberDict[dictMember].IsCompatibleType(newValue))
                throw new TypeException(dictMember.TypeTag, newValue, right);
            memberDict[dictMember] = newValue;
        }
        else
        {
            var scope = _evaluator.Visit(memberExpr.Left);
            Environment environment;
            if (scope?.AsResolver() is ScopeLabel lbl)
            {
                environment = lbl.Env;
            }
            else
            {
                if (scope?.AsResolver() is not Namespace ns)
                {
                    throw new CompileException(CompileExceptionType.SymbolRedefined, memberExpr);
                }
                environment = ns.Env;
            }
            if (!environment.SymbolExists(sym))
                throw new CompileException(CompileExceptionType.HasNoMemberSymbol, memberExpr.Member);
            environment.DefineOrUpdateVariable(memberExpr.Member, newValue);
        }
        if (genListing)
        {
            GenAssignmentListing(memberExpr, '=', newValue);
        }
        return Jump.NoJump;
    }
    
    private Jump CompileSubscriptAssignment
    (
        SubscriptExpression subscript, 
        Value? symbol,
        Token op, 
        Value rightValue, 
        Expression right,
        bool genListing
    )
    {
        if (subscript.Index.Start == null ||
            subscript.Index.Type != RangeType.IsIndex)
        {
            throw new CompileException(CompileExceptionType.InvalidOperation, subscript);
        }
        var existing = _evaluator.Visit(subscript);
        if (existing == null) return Jump.NoJump;

        var index = _evaluator.Visit(subscript.Index.Start);
        if (index == null) return Jump.NoJump;
        var binExpr = new BinaryOpExpression(subscript, op, right);
        var newValue = EvalValues.EvalAssign
        (
            existing,
            rightValue,
            binExpr,
            _assemblyState.TextEncodingCollection
        );
        if (newValue == null) return Jump.NoJump;
        if (symbol?.TypeTag is TypeTag.Array || symbol?.AsAddress() != null)
        {
            if (!index.IsNumber())
                throw new TypeException(TypeTag.Float, index, subscript.Index.Start);
            var i = index.AsInt(_assemblyState.TextEncodingCollection);
            if (symbol.TypeTag == TypeTag.Array)
            {
                if (i < 0) i = symbol.Length + 1;
                if (i >= symbol.Length)
                {
                    return _assemblyState.PassNeeded 
                        ? Jump.NoJump 
                        : throw new CompileException(CompileExceptionType.IndexOutOfRange, subscript.Index.Start);
                }
                if (!symbol.UpdateIndex(i, newValue, _assemblyState.TextEncodingCollection))
                {
                    throw new TypeException(existing.TypeTag, newValue, right);
                }
            }
            else
            {
                if (newValue.AsInt().Size() > 1)
                {
                    return _assemblyState.PassNeeded 
                        ? Jump.NoJump 
                        : throw new IntegerOverflowException(1, sbyte.MinValue, byte.MaxValue, right);
                }
                _assemblyState.Output.Poke
                (
                    (int)(symbol.AsInt() + i), 
                    (byte)newValue.AsInt().AsPositive()
                );
            }
            if (genListing)
            {
                GenAssignmentListing(subscript, '=', newValue);
            }
            return Jump.NoJump;
        }
        var dict = symbol?.AsDictionary() ?? 
        throw new CompileException
        (
            CompileExceptionType.CannotSubscript, 
            subscript
        );
        if (!dict.ContainsKey(index))
        {
            throw new CompileException(CompileExceptionType.KeyNotFound, subscript.Index.Start);
        }
        if (!dict[index].IsCompatibleType(newValue))
        {
            throw new TypeException(dict[index].TypeTag, newValue, right);
        }
        dict[index] = newValue;
        if (genListing)
        {
            GenAssignmentListing(subscript, '=', newValue);
        }
        return Jump.NoJump;
    }
    
    private bool IsLValueConstant(Expression left)
    {
        var varName = left.LeftToken.Text.ToString();
        while (true)
        {
            if (left is not MemberExpression member)
            {
                if (left is not SubscriptExpression subscript)
                    return _assemblyState.SymbolTable.IsConstant(varName);
                left = subscript.Left;
                continue;
            }
            varName = member.Member.Text.ToString();
            var rootValue = _evaluator.Visit(member.Left);
            Environment env;
            if (rootValue?.AsResolver() is ScopeLabel label)
            {
                env = label.Env;
            }
            else if (rootValue?.AsResolver() is Namespace ns)
            {
                env = ns.Env;
            }
            else
            {
                return _assemblyState.SymbolTable.IsConstant(varName);
            }
            return env.IsConstant(varName);
        }
    }

    private Jump CompilePcAssignment
    (
        Expression left, 
        Token op, 
        Value rightValue, 
        bool genListing,
        Expression right
    )
    {
        if (!rightValue.IsNumber() || op.Type == TokenType.GlobalKw)
            throw new CompileException(CompileExceptionType.TypeMismatch, right);
        var num = rightValue.AsInt(_assemblyState.TextEncodingCollection).AsPositive();
        if (num is < short.MinValue or > ushort.MaxValue)
            throw new IntegerOverflowException(2, short.MinValue, ushort.MaxValue, right);
        if (_assemblyState.Output.Synched)
        {
            _assemblyState.Output.Offset = (int)num;
        }
        _assemblyState.Output.ProgramCounter = (int)num;
        if (genListing)
        {
            GenPcListing(left);
        }
        return Jump.NoJump;
    }
    
    public Jump VisitVarAssignmentStatement(VarAssignmentStatement statement)
    {
        var right = _evaluator.Visit(statement.Right);
        return right == null 
            ? Jump.NoJump :
            CompileVarAssignment
            (
                statement.Left, 
                statement.Operator, 
                right, 
                statement.Right,
                true
            );
    }

    public Jump VisitCpuInstructionStatement(CpuInstructionStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement.Operand);
        var startOffset = _assemblyState.Output.Offset;
        var programCounter = _assemblyState.Output.ProgramCounter;
        try
        {
            var success = _assemblyState.Cpu switch
            {
                Cpu.Gb80 or Cpu.I8080 or Cpu.Z80 => ZilogIntelEncoder.Encode(statement, _assemblyState),
                Cpu.M6800 or Cpu.M6809 => MotorolaEncoder.Encode(statement, _assemblyState),
                Cpu.I86 => I86Encoder.Encode(statement, _assemblyState),
                _ => M65xxEncoder.Encode(statement, _assemblyState)
            };
            if (!success)
            {
                Ast ast = statement.Operand.Type == OperandType.Implied 
                    ? statement 
                    : statement.Operand;
                throw new CompileException(CompileExceptionType.AddressingModeNotSupported, ast);
            }
            _assemblyState.AnalysisContexts.Add
            (
                new CodeAnalysisContext
                (
                    _assemblyState.Cpu,  
                    _assemblyState.Output.BytesFrom(startOffset).ToArray(),
                    startOffset,
                    statement
                )
            );
            var largePadding = _assemblyState.Cpu == Cpu.I86 &&
                               (statement.Operand.CoercedSize > 0 ? 1 : 0) + 
                               statement.Operand.Registers.Count +
                               statement.Operand.Expressions.Count > 3;
            BankCrossed(statement, programCounter);
            GenCpuListing(statement.Mnemonic, largePadding, startOffset, programCounter);
            return Jump.NoJump;
        }
        catch (CompileException e) when (e is { Type: CompileExceptionType.SymbolNotFound, Offender: PrimaryExpression primary } 
                                         && primary.Expr.Type.IsRegister())
        {
            // recast errors involving registers being used in expression evaluation 
            // rather than as operand arguments in various addressing modes
            throw new CompileException(CompileExceptionType.RegisterCannotBeEvaluated, primary);
        }
    }

    public Jump VisitI86RepInstructionStatement(I86RepInstructionStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
        
        try
        {
            var startOffset = _assemblyState.Output.Offset;
            var programCounter = _assemblyState.Output.ProgramCounter;
            if (_assemblyState.Cpu != Cpu.I86 || 
                !I86Encoder.Encode(statement.RepOpcode, _assemblyState) ||
                !I86Encoder.Encode(statement.Repetition, _assemblyState))
            {
                throw new CompileException(CompileExceptionType.AddressingModeNotSupported, statement.RepOpcode);
            }
            _assemblyState.AnalysisContexts.Add
            (
                new CodeAnalysisContext
                (
                    _assemblyState.Cpu,  
                    _assemblyState.Output.BytesFrom(startOffset).ToArray(),
                    startOffset,
                    statement.Repetition
                )
            );
            BankCrossed(statement, programCounter);
            GenCpuListing(statement.RepOpcode.Mnemonic, true, startOffset, programCounter);
            return Jump.NoJump;
        }
        catch (CompileException e) when (e is { Type: CompileExceptionType.SymbolNotFound, Offender: PrimaryExpression primary } 
                                         && primary.Expr.Type.IsRegister())
        {
            throw new CompileException(CompileExceptionType.RegisterCannotBeEvaluated, primary);
        }
    }

    public Jump VisitSimpleDirectiveStatement(SimpleDirectiveStatement statement)
    {
        switch (statement.Directive.Type)
        {
            case TokenType.AutoKw:
            case TokenType.M8Kw:
            case TokenType.M16Kw:
            case TokenType.ManualKw:
            case TokenType.Mx8Kw:
            case TokenType.Mx16Kw:
            case TokenType.X8Kw:
            case TokenType.X16Kw:
                if (_assemblyState.Cpu != Cpu.M65816)
                {
                    _assemblyState.Logger.LogWarning
                    (
                        $"Directive {statement.Directive.Type.Stringified()} ignored for CPU", 
                        statement
                    );
                    return Jump.NoJump;
                }
                switch (statement.Directive.Type)
                {
                    case TokenType.AutoKw:
                    case TokenType.ManualKw:
                        _assemblyState.AutosizeRegisters = statement.Directive.Type == TokenType.AutoKw;
                        return Jump.NoJump;
                    case TokenType.M8Kw:
                    case TokenType.M16Kw:
                        _assemblyState.M16 = statement.Directive.Type == TokenType.M16Kw;
                        return Jump.NoJump;
                    case TokenType.Mx8Kw:
                    case TokenType.Mx16Kw:
                        _assemblyState.M16 = statement.Directive.Type == TokenType.Mx16Kw;
                        _assemblyState.X16 = statement.Directive.Type == TokenType.Mx16Kw;
                        return Jump.NoJump;
                    default:
                        _assemblyState.X16 = statement.Directive.Type == TokenType.X16Kw;
                        return Jump.NoJump;
                }
            case TokenType.BankKw: 
                _assemblyState.BankOff = true;
                return Jump.NoJump;
            case TokenType.BreakKw: return new Jump(JumpType.Break, statement);
            case TokenType.ContinueKw: return new Jump(JumpType.Continue, statement);
            case TokenType.DpKw:
                if (_assemblyState.Cpu != Cpu.M65816 && _assemblyState.Cpu != Cpu.M6809)
                {
                    _assemblyState.Logger.LogWarning("Directive `.dp` ignored for CPU", statement);
                    return Jump.NoJump;
                }
                _assemblyState.DirectPageOff = true;
                return Jump.NoJump;
            case TokenType.EndKw: return new Jump(JumpType.Exit);
            case TokenType.EndrelocateKw:
            case TokenType.RealPcKw:
                _assemblyState.Output.Synch();
                GenPcListing(statement);
                return Jump.NoJump;
            case TokenType.ForcepassKw:
                _assemblyState.PassNeeded |= _assemblyState.Passes == 0;
                break;
            case TokenType.ProffKw:
            case TokenType.PronKw:
                _assemblyState.PrintOn = statement.Directive.Type == TokenType.PronKw;
                return Jump.NoJump;
            case TokenType.ReturnKw: 
                return new Jump(JumpType.Return, statement);
        }
        throw new CompileException(CompileExceptionType.ExpectedExpression, statement);
    }

    public Jump VisitMultiExpressionDirectiveStatement(MultiExpressionDirectiveStatement statement)
    {
        switch (statement.Directive.Type)
        {
            case TokenType.AlignKw when statement.Expressions.Count == 2:
            case TokenType.FillKw when statement.Expressions.Count == 2: 
                return CompileAlignOrFill(statement, statement.Expressions);
            case TokenType.AssertKw when statement.Expressions.Count == 2:
                return CompileAssert(statement.Expressions);
            case TokenType.DsectionKw when statement.Expressions.Count <= 3:
                return CompileDefineSection(statement);
            case TokenType.ErrorifKw when statement.Expressions.Count == 2:
            case TokenType.WarnifKw when statement.Expressions.Count == 2:
                return CompileConditionalErrorWarning(statement);
            case TokenType.MapKw when statement.Expressions.Count <= 3:
                return CompileMap(statement.Expressions);
            case TokenType.ImportKw:
                return CompileImport(statement, statement.Expressions);
            case TokenType.StringifyKw:
                return CompileStringify(statement, statement.Expressions);
            case TokenType.UnmapKw when statement.Expressions.Count == 2:
                return CompileUnmap(statement.Expressions);
            case TokenType.BinaryKw when statement.Expressions.Count <= 3: 
                return CompileBinaryStatement(statement, statement.Expressions);
        }
        throw new CompileException(CompileExceptionType.UnexpectedExpression, statement.Expressions[^1]);
    }

    public Jump VisitSingleExpressionDirectiveStatement(SingleExpressionDirectiveStatement statement)
    {
        switch (statement.Directive.Type)
        {
            case TokenType.AlignKw:
            case TokenType.FillKw:
                return CompileAlignOrFill(statement, new List<Expression>{statement.Expression});
            case TokenType.AssertKw:
                return CompileAssert(new List<Expression>{statement.Expression});
            case TokenType.DpKw:
            case TokenType.BankKw:
                return CompileDirectPageOrBank(statement);
            case TokenType.BinaryKw:
                return CompileBinaryStatement(statement, new List<Expression>{statement.Expression});
            case TokenType.CpuKw:
                return CompileSetCpuStatement(statement.Expression);
            case TokenType.EchoKw:
                _assemblyState.Logger.LogWarning("Directive `.echo` is deprecated and has no effect", statement);
                return Jump.NoJump;
            case TokenType.EorKw:
                return CompileXorStatement(statement.Expression);
            case TokenType.EncodingKw:
                return CompileEncoding(statement.Expression);
            case TokenType.ErrorKw:
                return CompileErrorWarning(true, statement, statement.Expression);
            case TokenType.FormatKw:
                return CompileFormatStatement(statement.Expression);
            case TokenType.GotoKw:
                return CompileGotoStatement(statement);
            case TokenType.ImportKw:
                return CompileImport(statement, [statement.Expression]);
            case TokenType.InitmemKw:
                return CompileInitMem(statement);
            case TokenType.InvokeKw:
                return CompileInvoke(statement.Expression);
            case TokenType.OrgKw:
            {
                var value = (int)_evaluator.EvalPagedBanked(statement.Expression, short.MinValue, ushort.MaxValue);
                if (_assemblyState.Output.Synched)
                {
                    _assemblyState.Output.Offset = value;
                }
                _assemblyState.Output.ProgramCounter = value;
                _assemblyState.Logger.LogWarning($"Directive `.org` is deprecated. Use `* = {statement.Expression.GetText()}` to set the program counter",  statement);
                return Jump.NoJump;
            }
            case TokenType.PseudoPcKw:
            case TokenType.RelocateKw:
                return CompileRelocate(statement.Expression);
            case TokenType.ReturnKw:
            {
                var value = _evaluator.Visit(statement.Expression);
                return value != null ? new Jump(value, statement) : new Jump(JumpType.Return, statement);
            }
            case TokenType.SectionKw:
                return CompileSection(statement.Expression);
            case TokenType.StringifyKw :
                return CompileStringify(statement, new List<Expression>{statement.Expression});
            case TokenType.UnmapKw:
                return CompileUnmap(new List<Expression>{statement.Expression});
            case TokenType.WarnKw:
                return CompileErrorWarning(false, statement, statement.Expression);
            case TokenType.ErrorifKw:
            case TokenType.WarnifKw:
                throw new CompileException(CompileExceptionType.ExpectedExpression, statement);
        }
        throw new CompileException(CompileExceptionType.UnexpectedExpression, statement.Expression);
    }

    public Jump VisitPseudoOpStatement(PseudoOpStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
        var startOffset = _assemblyState.Output.Offset;
        var programCounter = _assemblyState.Output.ProgramCounter;
        EmitData.Encode(_assemblyState, statement);
        BankCrossed(statement, programCounter);
        GenDataListing(statement.PseudoOp, startOffset, programCounter);
        return Jump.NoJump;
    }

    public Jump VisitLabelStatement(LabelStatement statement)
    {
        var pc = _assemblyState.Output.ProgramCounter;
        if (_assemblyState.AssemblyOptions.WarnLeftSpaceOfLabel &&
            statement.Label.Location.Start > statement.Label.LineTextStart)
        {
            _assemblyState.Logger.LogWarning("Whitespace precedes the label", statement);
        }
        var symbol = statement.Label.Text.ToString();
        if (symbol is "+" or "-")
        {
            if (_assemblyState.SymbolTable.InFunction)
                throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
            if (_assemblyState.SymbolTable.LookupAnonymousAtIndex(symbol[0], statement.StatementIndex, out var currentPc))
            {
                if (currentPc == Address.BadAddress)
                {
                    throw new CompileException(CompileExceptionType.AnonymousReferenceNotFound, statement.Label);
                }
                _assemblyState.PassNeeded |= !_assemblyState.SymbolTable.InFunction && 
                                             currentPc != pc;
            }
            _assemblyState.SymbolTable.DefineAnonymous
            (
                symbol, 
                pc, 
                statement.StatementIndex
            );
            GenLabeledStatementListing(statement);
            return Jump.NoJump;
        }
        if (_assemblyState.AssemblyOptions.WarnRegistersAsIdent &&
            statement.Label.Type.IsRegister())
        {
            _assemblyState.Logger.LogWarning($"Label `{symbol}` matches a register", statement);
        }
        if (!_assemblyState.SymbolTable.TryDefineLabel
            (
                statement.Label, 
                EnvironmentType.Local, 
                pc, 
                statement.StatementIndex,
                out var address
            )
        )
        {
            if (_assemblyState.Passes == 0) 
                throw new CompileException(CompileExceptionType.SymbolRedefined, statement);
            _assemblyState.PassNeeded |= !_assemblyState.SymbolTable.InFunction && 
                                         address != pc;
        }
        GenLabeledStatementListing(statement);
        return Jump.NoJump;
    }
    
    public Jump VisitLabeledBlockStatement(LabeledBlockStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);

        var type = statement.Directive.Type == TokenType.BlockKw 
            ? EnvironmentType.Block 
            : EnvironmentType.Proc;
        if (_assemblyState.AssemblyOptions.WarnLeftSpaceOfLabel &&
            statement.Label.Location.Start > statement.Label.LineTextStart)
        {
            _assemblyState.Logger.LogWarning("Whitespace precedes the label", statement);
        }
        if (_assemblyState.AssemblyOptions.WarnRegistersAsIdent &&
            statement.Label.Type.IsRegister())
        {
            _assemblyState.Logger.LogWarning($"Label `{statement.Label.Text}` matches a register", statement);
        }
        if (!_assemblyState.SymbolTable.TryDefineLabel
            (
                statement.Label, 
                type, 
                _assemblyState.Output.ProgramCounter,
                statement.StatementIndex,
                out var address
            )
           )
        {
            if (_assemblyState.Passes == 0) 
                throw new CompileException(CompileExceptionType.SymbolRedefined, statement);
            _assemblyState.PassNeeded |= !_assemblyState.SymbolTable.InFunction
                                         && address != _assemblyState.Output.ProgramCounter;
        }
        var jump = Jump.NoJump;
        if (statement.Directive.Type == TokenType.ProcKw && 
            (_assemblyState.Passes == 0 || !_assemblyState.SymbolTable.CurrentScopeIsReferenced) )
        {
            _assemblyState.PassNeeded |= _assemblyState.Passes == 0;
        }
        else
        {
            GenLabeledStatementListing(statement);
            jump = CompileBlock(statement.Statements);
        }
        _assemblyState.SymbolTable.Pop();
        return jump;
    }
    
    public Jump VisitEnumDeclaration(EnumDeclaration statement)
    {
        var enumName = statement.Enum.Text.ToString();
        if (_assemblyState.Passes > 0)
        {
            var enumVal =
                _assemblyState.SymbolTable.Lookup(enumName)?.AsResolver()
                    as Enumeration;
            GenEnumStatementListing(statement.Enum, enumVal);
            return Jump.NoJump;
        }
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);

        var enumeration = new Enumeration(_assemblyState.Comparer, false);
        long startValue = 0;
        for (var i = 0; i < statement.Enumerators.Count; i++)
        {
            long defValue;
            var defaultValExpr = statement.Enumerators[i].DefaultValue;
            if (defaultValExpr != null)
            {
                long minVal = i == 0 ? int.MinValue : startValue;
                defValue = _evaluator.EvalInteger(defaultValExpr, minVal, int.MaxValue);
                startValue = defValue + 1;
            }
            else
            {
                defValue = startValue++;
            }
            if (!enumeration.Define(statement.Enumerators[i].Name, new Value(defValue)))
            {
                throw new CompileException(CompileExceptionType.SymbolRedefined, statement.Enumerators[i].Name);
            }
        }
        GenEnumStatementListing(statement.Enum, enumeration);
        return !_assemblyState.SymbolTable.TryDefineConstant(statement.Enum, new Value(enumeration), out _)
            ? throw new CompileException(CompileExceptionType.SymbolRedefined, statement)
            : Jump.NoJump;
    }

    public Jump VisitNamespaceBlockStatement(NamespaceBlockStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
        if (!_assemblyState.SymbolTable.ActivateNamespace(statement.Namespace))
        {
            throw new CompileException(CompileExceptionType.SymbolRedefined, statement.Namespace);
        }
        var jump = CompileBlock(statement.Statements);
        _assemblyState.SymbolTable.Pop();
        return jump;
    }
    
    public Jump VisitPageBlockStatement(PageBlockStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
        var startPc = _assemblyState.Output.ProgramCounter;
        var startPcPage = startPc & 0xffffff00;
        var jump = CompileBlock(statement.Statements);
        var pc = _assemblyState.Output.ProgramCounter;
        var pcPage = pc & 0xffffff00;
        if (pcPage != startPcPage && pc - pcPage > 0)
        {
            if (_assemblyState is { PassNeeded: false, Passes: > 3 })
            {
                throw new CompileException
                (
                    CompileExceptionType.PageBoundaryCrossed, 
                    statement.Statements.Count > 0
                    ? statement.Statements[^1].RightToken
                    : statement.RightToken
                );
            }
            _assemblyState.PassNeeded = true;
        }
        return jump;
    }

    public Jump VisitAnonymousBlockStatement(AnonymousBlockStatement statement)
    {
        if (statement.Directive.Type == TokenType.ProcKw)
        {
            return Jump.NoJump;
        }
        _assemblyState.SymbolTable.PushAnonymous
        (
            statement.Directive.Type ==  TokenType.ProcKw,
            statement.StatementIndex, 
            _assemblyState.Output.ProgramCounter
        );
        var jump = CompileBlock(statement.Statements);
        _assemblyState.SymbolTable.Pop();
        return jump;
    }

    public Jump VisitFunctionDefinitionStatement(FunctionDefinitionStatement statement)
    {
        if (_assemblyState.SymbolTable.InFunction)
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
        if (_assemblyState.Passes > 0)
            return Jump.NoJump;
        var defaultValues = new List<Value>();
        for (var i = 0; i < statement.DefaultValues.Count; i++)
        {
            var val = _evaluator.Visit(statement.DefaultValues[i]);
            if (val == null) return Jump.NoJump;
            defaultValues.Add(val);
        }
        var userFunc = new UserFunction
        (
            _assemblyState, 
            statement.Parameters, 
            defaultValues, 
            statement.Body
        );
        if (statement.Name.Text.Equals("this", _assemblyState.Comparison))
        {
            throw new CompileException(CompileExceptionType.SymbolThisReserved, statement);
        }
        return !_assemblyState.SymbolTable.TryDefineConstant(statement.Name, new Value(userFunc), out _) 
            ? throw new  CompileException(CompileExceptionType.SymbolRedefined, statement) 
            : Jump.NoJump;
    }

    public Jump VisitIfStatement(IfStatement statement)
    {
        var evaledIfBlock = false;
        var jump = Jump.NoJump;
        if (ShouldCompileIfBlock(statement.IfBlock))
        {
            jump = CompileBlock(statement.IfBlock.Block);
            evaledIfBlock = true;
        }
        else
        {
            ScanUncompiledIfBlockForAnonRefs(statement.IfBlock.Block);
        }
        for (var i = 0; i < statement.ElseIfBlocks.Count; i++)
        {
            if (!evaledIfBlock && ShouldCompileIfBlock(statement.ElseIfBlocks[i]))
            {
                evaledIfBlock = true;
                jump = CompileBlock(statement.ElseIfBlocks[i].Block);
            }
            else
            {
                ScanUncompiledIfBlockForAnonRefs(statement.IfBlock.Block);
            }
        }
        if (!evaledIfBlock)
        {
            jump = CompileBlock(statement.ElseBlock);
        }
        return jump;
    }

    public Jump VisitForStatement(ForStatement statement)
    {
        if (statement.Init != null)
        {
            var printOn = _assemblyState.PrintOn;
            _assemblyState.PrintOn = false;
            VisitVarAssignmentStatement(statement.Init);
            _assemblyState.PrintOn = printOn;
        }
        var condValue = new Value(true);
        if (statement.Condition != null)
        {
            var evaledCondValue = _evaluator.Visit(statement.Condition);
            if (evaledCondValue == null) return Jump.NoJump;
            if (evaledCondValue.TypeTag != TypeTag.Boolean)
            {
                if (!_assemblyState.PassNeeded)
                    throw new TypeException
                    (
                        TypeTag.Boolean,
                        evaledCondValue,
                        statement.Condition
                    );
            }

            if (statement.Condition.IsConstant())
            {
                _assemblyState.Logger.LogWarning(
                    $"Condition is a constant expression that will always evaluate to {statement.Condition.Value}", 
                    statement.Condition);
            }
            condValue = evaledCondValue;
        }
        var repetition = 65535;
        while (condValue.AsBoolean() && repetition-- > 0)
        {
            if (repetition == 0)
            {
                _assemblyState.Logger.LogWarning
                (
                    "Condition in `.for` statement leads to infinite loop", 
                    (Ast?)statement.Condition ?? statement
                );
                break;
            }
            var jump = CompileBlock(statement.Block);
            if (jump.Type is not (JumpType.None or JumpType.Continue))
            {
                return jump.Type == JumpType.Break ? Jump.NoJump : jump;
            }
            var printOn = _assemblyState.PrintOn;
            _assemblyState.PrintOn = false;
            for (var i = 0; i < statement.Iterators.Count; i++)
            {
                _ = Visit(statement.Iterators[i]);
            }
            _assemblyState.PrintOn = printOn;
            if (statement.Condition != null)
                condValue = _evaluator.Visit(statement.Condition) ?? new Value(false);
        }
        return Jump.NoJump;
    }
    
    public Jump VisitForeachStatement(ForeachStatement statement)
    {
        var enumerable = _evaluator.Visit(statement.Enumerable);
        if (enumerable == null) return Jump.NoJump;
        _assemblyState.SymbolTable.PushAnonymous(false, statement.StatementIndex, _assemblyState.Output.ProgramCounter);
        var asDictionary = enumerable.AsDictionary();
        var blockJump = Jump.NoJump;
        if (asDictionary != null)
        {
            var keyToken = new Token(statement.Enumerator.Source.Name, TokenType.Ident, "key");
            var valToken = new Token(statement.Enumerator.Source.Name, TokenType.Ident, "value");
            foreach (var members in asDictionary)
            {
                var kvp = new Enumeration(_assemblyState.Comparer, true);
                kvp.Define(keyToken, members.Key);
                kvp.Define(valToken, members.Value);
                _assemblyState.SymbolTable.TryDefineConstant(statement.Enumerator,
                    new Value(kvp),
                    out _);
                
                var jump = CompileBlock(statement.Block);
                if (jump.Type is JumpType.None or JumpType.Continue) continue;
                blockJump = jump.Type == JumpType.Break ? Jump.NoJump : jump;
                break;
            }
        }
        else
        {
            if (enumerable.TypeTag != TypeTag.String && enumerable.TypeTag != TypeTag.Array)
            {
                throw new TypeException(TypeTag.Enumerable, enumerable, statement.Enumerable);
            }
            for (var i = 0; i < enumerable.Length; i++)
            {
                var enumeration = enumerable.TypeTag == TypeTag.String 
                    ? new Value(enumerable.AsString()[i]) 
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    : enumerable.AsArray()![i];
                _assemblyState.SymbolTable.TryDefineConstant(statement.Enumerator, enumeration, out _);
                var jump = CompileBlock(statement.Block);
                if (jump.Type is JumpType.None or JumpType.Continue) continue;
                blockJump = jump.Type == JumpType.Break ? Jump.NoJump : jump;
                break;
            }
        }
        _assemblyState.SymbolTable.Pop();
        return blockJump;
    }

    public Jump VisitSwitchStatement(SwitchStatement statement)
    {
        var cases = new Dictionary<Value, int>();
        var switchCondVal = _evaluator.Visit(statement.Condition);
        if (switchCondVal == null) return Jump.NoJump;
        var defaultCase = -1;
        for (var i = 0; i < statement.Cases.Count; i++)
        {
            var caseBlock =  statement.Cases[i];
            if (caseBlock.IsDefault)
            {
                if (defaultCase >= 0)
                {
                    throw new CompileException(CompileExceptionType.DefaultSpecified, caseBlock);
                }
                defaultCase = i;
            }
            for (var c = 0; c < caseBlock.Cases.Count; c++)
            {
                var caseLabel = caseBlock.Cases[c];
                if (!caseLabel.IsConstant())
                {
                    throw new CompileException(CompileExceptionType.ValueNotConstant, caseLabel);
                }
                var caseVal =  _evaluator.Visit(caseLabel);
                if (caseVal == null) return Jump.NoJump;
                if (cases.TryAdd(caseVal, i)) continue;
                if (!_assemblyState.PassNeeded)
                    throw new CompileException(CompileExceptionType.CaseSpecified,
                        caseLabel);
                break;
            }
        }
        if (!cases.TryGetValue(switchCondVal, out var switchCaseBlockIndex))
        {
            if (defaultCase >= 0)
                switchCaseBlockIndex = defaultCase;
            else 
                return Jump.NoJump;
        }
        for (var i = switchCaseBlockIndex; i < statement.Cases.Count; i++)
        {
            var jump = CompileBlock(statement.Cases[i].Block);
            if (jump.Type == JumpType.None) continue;
            return jump.Type == JumpType.Break ? Jump.NoJump : jump;
        }
        return Jump.NoJump;
    }

    public Jump VisitExpressionBlockStatement(ExpressionBlockStatement statement)
    {
        long repetition = 65535;
        Value? condVal;
        if (statement.Directive.Type == TokenType.RepeatKw)
        {
            repetition = _evaluator.EvalInteger(statement.Expression, 0, 65534);
            if (repetition == 65534 && _assemblyState.PassNeeded)
            {
                repetition = 0;
            }
            condVal = new Value(repetition > 0);
        }
        else
        {
            if (statement.Directive.Type == TokenType.DoKw)
            {
                var jump = CompileBlock(statement.Block);
                if (jump.Type != JumpType.None) return jump;
            }
            condVal = _evaluator.Visit(statement.Expression);
            if (condVal == null) return Jump.NoJump;
            if (condVal.TypeTag != TypeTag.Boolean)
                throw new TypeException(TypeTag.Boolean, condVal, statement.Expression);
        }
        while (condVal.AsBoolean() && repetition-- != 0)
        {
            var jump = CompileBlock(statement.Block);
            if (jump.Type != JumpType.None && jump.Type != JumpType.Continue)
            {
                return jump.Type == JumpType.Break ? Jump.NoJump : jump;
            }
            if (statement.Directive.Type == TokenType.RepeatKw)
            {
                condVal = new Value(repetition > 0);
            }
            else
            {
                condVal = _evaluator.Visit(statement.Expression) ?? new Value(false);
                if (condVal.AsBoolean() && repetition == 0)
                {
                    _assemblyState.Logger.LogWarning
                    (
                        $"Condition in {statement.Directive.Type.Stringified()} leads to infinite loop",
                        statement.Expression
                    );
                    break;
                }
            }
        }
        return Jump.NoJump;
    }

    public Jump VisitEofStatement(EofStatement statement)
    {
        // don't exit, since it's the end of a source but not necessarily all sources
        return Jump.NoJump; 
    }

    public Jump Visit(Statement statement)
    {
        try
        {
            return statement.Accept(this);
        }
        catch (IntegerOverflowException overflowEx)
        {
            _assemblyState.Logger.LogError(overflowEx);
        }
        catch (InvalidUnaryOperationException unaryEx)
        {
            _assemblyState.Logger.LogError(unaryEx);
        }
        catch (InvalidBinaryOperationException binaryEx)
        {
            _assemblyState.Logger.LogError(binaryEx);
        }        
        catch (TypeException typeEx)
        {
            _assemblyState.Logger.LogError(typeEx);
        }
        catch (CompileException compileEx)
        {
            _assemblyState.Logger.LogError(compileEx.Type.Stringified(), compileEx.Offender);
        }
        catch (SectionException sectionEx)
        {
            _assemblyState.Logger.LogError(sectionEx.Message, statement);
        }
        catch (OutputException outputEx)
        {
            if (_assemblyState.PassNeeded) return Jump.NoJump;
            var compileExceptionType = outputEx.Type switch
            {
                OutputExceptionType.AddressOverflow => CompileExceptionType.AddressOverflow,
                OutputExceptionType.InvalidAlignAmount => CompileExceptionType.InvalidAlignAmount,
                OutputExceptionType.InvalidFillAmount => CompileExceptionType.InvalidFillAmount,
                OutputExceptionType.InvalidPeekAddress => CompileExceptionType.InvalidPeekAddress,
                OutputExceptionType.InvalidPokeAddress => CompileExceptionType.InvalidPokeAddress,
                OutputExceptionType.SectionNotFound => CompileExceptionType.SectionNotFound,
                OutputExceptionType.NoObjectBytesForSection => CompileExceptionType.NoObjectBytesForSection,
                _ => CompileExceptionType.InvalidProgramCounter
            };
            _assemblyState.Logger.LogError(compileExceptionType.Stringified(), statement);
        }
        return Jump.NoJump;
    }

    private Jump CompileBinaryStatement(Statement statement, IList<Expression> expressions)
    {
        var file = ExpressionFolder.EvalStringLiteral(expressions[0]);
        try
        {
            var reader = _assemblyState.SourceFactory.CreateReader();
            var fileBytes = reader.GetBytes(file) 
                            ?? throw new CompileException(CompileExceptionType.FileNotFound, expressions[0]);
            var offset = 0;
            var length = fileBytes.Length;
            if (expressions.Count > 1)
            {
                offset = (int)_evaluator.EvalInteger(expressions[1], 0, int.MaxValue);
                if (expressions.Count == 3)
                {
                    length = (int)_evaluator.EvalInteger(expressions[2], 0, int.MaxValue);   
                }
            }
            if (offset + length > fileBytes.Length)
            {
                if (!_assemblyState.PassNeeded)
                    throw new CompileException
                    (
                        CompileExceptionType.OffsetAndLengthOutOfRange, 
                        expressions[1]
                    );
                _assemblyState.Output.EmitBytes(fileBytes, ByteOrder.LittleEndian);
                return Jump.NoJump;
            }

            var programCounter = _assemblyState.Output.ProgramCounter;
            var startOffset = _assemblyState.Output.Offset;
            _assemblyState.Output.EmitBytes
            (
                fileBytes
                    .Skip(offset)
                    .Take(length)
                    .ToArray(), 
                _assemblyState.Cpu.ByteOrder()
            );
            BankCrossed(statement, programCounter);
            GenDataListing(statement.LeftToken, startOffset, programCounter);
            return Jump.NoJump;
        }
        catch 
        {
            throw new CompileException(CompileExceptionType.FileNotFound, expressions[0]);
        }
    }

    private Jump CompileAlignOrFill(Statement statement, IList<Expression> expressions)
    {
        var directive = statement.LeftToken;
        var amount = (int)_evaluator.EvalInteger
        (
            expressions[0], 
            int.MinValue, 
            int.MaxValue
        );
        var startOffset = _assemblyState.Output.Offset;
        var programCounter = _assemblyState.Output.ProgramCounter;
        if (startOffset + amount > 0xffff)
        {
            return _assemblyState.PassNeeded 
                ? Jump.NoJump 
                : throw new CompileException(CompileExceptionType.ProgramOverflow, directive);
        }
        if (expressions.Count == 1)
        {
            if (directive.Type == TokenType.AlignKw)
            {
                _assemblyState.Output.Align(amount);
            }
            else
            {
                _assemblyState.Output.Fill(amount);
            }
            GenDataListing(directive, startOffset, programCounter);
            return Jump.NoJump;
        }
        var value = _evaluator.EvalInteger(expressions[1]);
        if (directive.Type == TokenType.AlignKw)
        {
            _assemblyState.Output.Align(amount, value, _assemblyState.Cpu.ByteOrder());
        }
        else
        {
            _assemblyState.Output.Fill(amount, value, _assemblyState.Cpu.ByteOrder());
        }
        BankCrossed(statement, programCounter);
        GenDataListing(directive, startOffset, programCounter);
        return Jump.NoJump;
    }

    private Jump CompileAssert(IList<Expression> expressions)
    {
        var cond = _evaluator.Visit(expressions[0]);
        if (cond?.TypeTag != TypeTag.Boolean)
        {
            return cond == null
                ? Jump.NoJump
                : throw new TypeException(TypeTag.Boolean, cond, expressions[0]);
        }
        if (cond.AsBoolean())
        {
            return Jump.NoJump;
        }
        var error = "Assertion failed.";
        if (expressions.Count > 1)
        {
            var errorVal = _evaluator.Visit(expressions[1]);
            if (errorVal != null)
            {
                if (!errorVal.IsCharOrString())
                {
                    throw new TypeException(TypeTag.String, errorVal, expressions[1]);
                }
                error = errorVal.AsString();
            }
        }
        _assemblyState.Logger.LogError(error, expressions[0]);
        return Jump.NoJump;
    }
    
    private Jump CompileConditionalErrorWarning(MultiExpressionDirectiveStatement statement)
    {
        var cond = _evaluator.Visit(statement.Expressions[0]);
        if (cond == null)
        {
            return Jump.NoJump;
        }
        if (cond.TypeTag != TypeTag.Boolean)
        {
            return _assemblyState.PassNeeded
                ? Jump.NoJump
                : throw new TypeException(TypeTag.Boolean, cond, statement.Expressions[0]);
        }
        return cond.AsBoolean() ? 
            CompileErrorWarning(statement.Directive.Type == TokenType.ErrorifKw, statement, statement.Expressions[1]) 
            : Jump.NoJump;
    }

    private Jump CompileErrorWarning(bool isError, Statement statement, Expression message)
    {
        var messageVal = _evaluator.Visit(message);
        if (messageVal == null) return Jump.NoJump;
        if (!messageVal.IsCharOrString())
        {
            return _assemblyState.PassNeeded
                ? Jump.NoJump
                : throw new TypeException(TypeTag.String, messageVal, message);
        }
        if (isError)
        {
            _assemblyState.Logger.LogError(messageVal.AsString(), statement);
        }
        else
        {
            _assemblyState.Logger.LogWarning(messageVal.AsString(), statement);
        }
        return Jump.NoJump;
    }

    private Jump CompileInitMem(SingleExpressionDirectiveStatement statement)
    {
        if (_assemblyState.Output.Started)
        {
            throw new CompileException(CompileExceptionType.CannotInitMem, statement);
        }
        _assemblyState.Output.InitMem = _evaluator.EvalByte(statement.Expression);
        return Jump.NoJump;
    }

    private Jump CompileInvoke(Expression expression)
    {
        if (expression is not CallExpression call)
        {
            throw new CompileException(CompileExceptionType.NotCallable, expression);
        }
        _ = _evaluator.Invoke(call);
        return Jump.NoJump;
    }
    
    private Jump CompileRelocate(Expression expression)
    {
        var relocate = (int)_evaluator.EvalInteger(expression,  int.MinValue, int.MaxValue);
        _assemblyState.Output.ProgramCounter = relocate;
        _assemblyState.Output.Synched = false;
        _assemblyState.PassNeeded |= _assemblyState.Passes == 0;
        GenPcListing(expression);
        return Jump.NoJump;
    }
    
    
    
    private bool ShouldCompileIfBlock(IfBlock block)
    {
        switch (block.IfDirective.Type)
        {
            case TokenType.IfdefKw or 
                TokenType.ElseifdefKw or 
                TokenType.IfndefKw or 
                TokenType.ElseifndefKw:
            {
                if (block.Condition is not PrimaryExpression primary || !primary.LeftToken.Type.IsIdent())
                {
                    throw new CompileException
                    (
                        CompileExceptionType.IdentifierExpected,
                        block.Condition
                    );
                }
                if (_assemblyState.SymbolTable.Lookup(primary.Expr.Text.ToString()) != null)
                {
                    return block.IfDirective.Type is TokenType.IfdefKw or TokenType.ElseifdefKw;
                }
                if (_assemblyState.Passes == 0)
                {
                    _assemblyState.PassNeeded = true;
                }
                return block.IfDirective.Type is TokenType.IfndefKw or TokenType.ElseifndefKw;
            }
        }
        if (block.Condition.IsConstant() && block.Condition.Value.TypeTag == TypeTag.Boolean)
        {
            var condStr = block.Condition.Value.ToString();
            _assemblyState.Logger.LogWarning(
                $"Condition is a constant expression that will always evaluate to {condStr}", 
                block.Condition);
        }
        var condVal = _evaluator.Visit(block.Condition);
        if (condVal == null) return false;
        return condVal.TypeTag != TypeTag.Boolean 
            ? throw new TypeException(TypeTag.Boolean, condVal, block.Condition) 
            : condVal.AsBoolean();
    }

    private void ScanUncompiledIfBlockForAnonRefs(IList<Statement> statements)
    {
        var anonRefs = statements.Select(s => s as LabelStatement);
        foreach (var statement in anonRefs)
        {
            var symbol = statement?.Label.Text.ToString();
            if (statement == null || symbol is not ("+" or "-") ||
               _assemblyState.SymbolTable.LookupAnonymousAtIndex(symbol[0], statement.StatementIndex, out _))
            {
               continue;
            }
            _assemblyState.SymbolTable.DefineAnonymous
            (
                symbol, 
                Address.BadAddress, 
                statement.StatementIndex
            );
        }
    }
    
    public Jump CompileBlock(IList<Statement> statements)
    {
        for (var i = 0; i < statements.Count; i++)
        {
            try
            {
                var jump = Visit(statements[i]);
                switch (jump.Type)
                {
                    case JumpType.None:
                        continue;
                    case JumpType.Goto:
                    {
                        i = statements
                            .ToList()
                            .FindIndex(s => s.StatementIndex == jump.GotoIndex) - 1;
                        if (i < -1) return jump;
                        continue;
                    }
                    case JumpType.Break:
                    case JumpType.Continue:
                    case JumpType.Exit:
                    case JumpType.Return:
                    default:
                        return jump;
                }
            }
            catch (CompileException e)
            {
                _assemblyState.Logger.LogError(e);
            }
        }    
        return Jump.NoJump;
    }

    private Jump CompileTupleAssignment
    (
        ArrayInitExpression tupleExpr,
        Token op,
        Value rightValue,
        Expression right,
        bool isConst,
        bool genListing
    )
    {
        var expressions = tupleExpr.Expressions;
        if (rightValue.AsArray() is not { } rightTuple || 
            rightValue.TypeTag != TypeTag.Tuple ||
            rightValue.Length != expressions.Count)
        {
            throw new CompileException(CompileExceptionType.TypeMismatch, right);
        }
        for (var i = 0; i < expressions.Count; i++)
        {
            _ = isConst 
                ? CompileConstantAssign(expressions[i], op, rightTuple[i], right, false) 
                : CompileVarAssignment(expressions[i], op, rightTuple[i], right, false);
        }
        if (genListing)
        {
            GenAssignmentListing(tupleExpr, '=', rightValue);
        }
        return Jump.NoJump;
    }

    private Jump CompileSetCpuStatement(Expression expression)
    {
        var cpuName = ExpressionFolder.EvalStringLiteral(expression);
        var cpu = CpuLookup.ByName(cpuName);
        if (cpu == null || !_assemblyState.Cpu.IsInFamilyWith(cpu.Value))
            throw new CompileException(CompileExceptionType.InvalidCpuSpecified, expression);
        _assemblyState.Cpu = cpu.Value;
        return Jump.NoJump;
    }

    private Jump CompileDefineSection(MultiExpressionDirectiveStatement statement)
    {
        if (_assemblyState.Passes > 0) return Jump.NoJump;
        var sectionName = ExpressionFolder.EvalStringLiteral(statement.Expressions[0]);
        var start = (int)_evaluator.EvalInteger(statement.Expressions[1],  short.MinValue, ushort.MaxValue);
        var end = Address.BadAddress;
        if (statement.Expressions.Count > 2)
        {
            end = (int)_evaluator.EvalInteger(statement.Expressions[2], short.MinValue + 1, Address.BadAddress);
        }

        if (_assemblyState.Output.Started)
        {
            throw new CompileException
            (
                CompileExceptionType.SectionDefinedAfterCompilation, 
                statement
            );
        }
        try
        {
            _assemblyState.Output.DefineSection(sectionName, start, end);
        }
        catch (SectionException e)
        {
            _assemblyState.Logger.LogError(e.Message, statement);
        }
        return Jump.NoJump;
    }

    private Jump CompileSection(Expression expression)
    {
        var sectionName = ExpressionFolder.EvalStringLiteral(expression);
        _assemblyState.Output.SetSection(sectionName);
        return Jump.NoJump;
    }
    
    private Jump CompileEncoding(Expression expression)
    {
        var encoding = ExpressionFolder.EvalStringLiteral(expression);
        _assemblyState.TextEncodingCollection.SelectEncoding(encoding);
        return Jump.NoJump;
    }

    private Jump CompileMap(IList<Expression> expressions)
    {
        var mapStart = _evaluator.Visit(expressions[0]);
        if (mapStart == null) return Jump.NoJump;
        if (!mapStart.IsCharOrString())
        {
            if (!mapStart.IsNumber())
            {
                throw new TypeException(TypeTag.Float, mapStart, expressions[0]);
            }
        }
        var mapStartIsRange = mapStart.IsCharOrString() && mapStart.Length > 1;
        if (mapStartIsRange && mapStart.Length > 2)
        {
            throw new CompileException(CompileExceptionType.ValueOverflow, expressions[0]);
        }
        if (expressions.Count > 2)
        {
            if (mapStartIsRange)
            {
                throw new CompileException
                (
                    CompileExceptionType.UnexpectedExpression,  
                    expressions[^1]
                );
            }
            var mapEnd = _evaluator.Visit(expressions[1]);
            if (mapEnd == null) return Jump.NoJump;
            if (!mapEnd.IsCharOrString())
            {
                if (!mapEnd.IsNumber())
                {
                    throw new TypeException
                    (
                        TypeTag.Float,
                        mapEnd,
                        expressions[1]
                    );
                }
                try
                {
                    mapEnd = new Value(char.ConvertFromUtf32((int)mapEnd.AsInt()));
                }
                catch
                {
                    return _assemblyState.PassNeeded 
                        ? Jump.NoJump 
                        : throw new CompileException
                        (
                            CompileExceptionType.ValueOverflow, 
                            expressions[1]
                        );
                }
            }
            var startMappingVal = _evaluator.EvalInteger(expressions[^1], char.MinValue, char.MaxValue);
            _assemblyState.TextEncodingCollection.Map(mapStart.AsString(), mapEnd.AsString(), (char)startMappingVal);
            return Jump.NoJump;
        }
        var mappingVal = _evaluator.EvalInteger(expressions[1], char.MinValue, char.MaxValue);
        if (mapStartIsRange)
        {
            var start = mapStart.AsString()[0];
            var end = mapStart.AsString()[1];
            _assemblyState.TextEncodingCollection.Map(start.ToString(), end.ToString(), (char)mappingVal);
        }
        else
        {
            _assemblyState.TextEncodingCollection.Map(mapStart.AsString(), (char)mappingVal);
        }
        return Jump.NoJump;
    }

    private Jump CompileStringify(Statement statement, IList<Expression> expressions)
    {
        if (_assemblyState.SymbolTable.InFunction)
        {
            throw new CompileException(CompileExceptionType.NotValidInFunction, statement);
        }

        var offs = _assemblyState.Output.Offset;
        var pc = _assemblyState.Output.ProgramCounter;
        EmitData.EncodeStringify(_assemblyState, expressions);
        BankCrossed(statement, pc);
        GenDataListing(statement.LeftToken, offs, pc);
        return Jump.NoJump;
    }
    
    private Jump CompileUnmap(IList<Expression> expressions)
    {
        var unmapStart = _evaluator.Visit(expressions[0]);
        if (unmapStart == null) return Jump.NoJump;
        if (!unmapStart.IsCharOrString())
        {
            if (!unmapStart.IsNumber())
            {
                throw new TypeException(TypeTag.Float, unmapStart, expressions[0]);
            }
        }
        if (expressions.Count > 1)
        {
            var unmapEnd = _evaluator.Visit(expressions[1]);
            if (unmapEnd == null) return Jump.NoJump;
            if (!unmapEnd.IsCharOrString())
            {
                if (!unmapEnd.IsNumber())
                {
                    throw new TypeException(TypeTag.Float, unmapEnd, expressions[1]);
                }
                try
                {
                    unmapEnd = new Value(char.ConvertFromUtf32((int)unmapEnd.AsInt()));
                }
                catch
                {
                    return _assemblyState.PassNeeded 
                        ? Jump.NoJump 
                        : throw new CompileException
                        (
                            CompileExceptionType.ValueOverflow, 
                            expressions[1]
                        );
                }
            }
            try
            {
                _assemblyState.TextEncodingCollection.Unmap(unmapStart.AsString(), unmapEnd.AsString());
                return Jump.NoJump;
            }
            catch
            {
                throw new CompileException(CompileExceptionType.ValueOverflow, expressions[0]);
            }
        }
        if (unmapStart.AsString().Length > 1)
        {
            if (unmapStart.AsString().Length > 2)
            {
                throw new CompileException(CompileExceptionType.ValueOverflow, expressions[0]);
            }

            var start = unmapStart.AsString()[0].ToString();
            var end = unmapStart.AsString()[1].ToString();
            _assemblyState.TextEncodingCollection.Unmap(start, end);
            return Jump.NoJump;
        }
        try
        {
            _assemblyState.TextEncodingCollection.Unmap(unmapStart.AsString());
            return Jump.NoJump;
        }
        catch 
        {
            throw new CompileException(CompileExceptionType.ValueOverflow, expressions[0]);
        }
    }

    private Jump CompileImport(Statement statement, IList<Expression> expressions)
    {
        if (!_assemblyState.SymbolTable.InRootScope)
        {
            throw new CompileException(CompileExceptionType.NotInRootScope, statement);
        }
        for (var i = 0; i < expressions.Count; i++)
        {
            var path = GetImportPath(expressions[i]);
            if (_assemblyState.SymbolTable.IsImported(path))
            {
                _assemblyState.Logger.LogWarning("Scope is already imported", statement);
                return Jump.NoJump;
            }
            try
            {
                _assemblyState.SymbolTable.Import(path);
            }
            catch (KeyNotFoundException knfExc)
            {
                if (_assemblyState.PassNeeded) continue;
                _assemblyState.Logger.LogError(knfExc.Message,expressions[i]);
                return Jump.NoJump;
            }
            catch (Exception e)
            {
                _assemblyState.Logger.LogError(e.Message, expressions[i]);
                return Jump.NoJump;
            }
        }
        return Jump.NoJump;
    }

    private static List<string> GetImportPath(Expression expression)
    {
        if (expression is MemberExpression member)
        {
            var path = new List<string>();
            path.AddRange(GetImportPath(member.Left));
            path.Add(member.Member.Text.ToString());
            return path;
        }
        if (expression is not PrimaryExpression primary || !primary.Expr.Type.IsIdent())
        {
            throw new CompileException(CompileExceptionType.IdentifierExpected, expression);
        }
        return [primary.Expr.Text.ToString()];
    }
    
    private Jump CompileDirectPageOrBank(SingleExpressionDirectiveStatement statement)
    {
        if (_assemblyState.Cpu != Cpu.M65816 && _assemblyState.Cpu != Cpu.M6809)
        {
            _assemblyState.Logger.LogWarning
            (
                $"Directive {statement.Directive.Type.Stringified()} ignored for CPU", 
                statement
            );
            return Jump.NoJump;
        }
        var operand = _evaluator.EvalByte(statement.Expression);
        if (statement.Directive.Type == TokenType.DpKw)
        {
            _assemblyState.DirectPageOff = false;
            _assemblyState.DirectPage = operand;
        }
        else
        {
            if (_assemblyState.Cpu != Cpu.M65816)
            {
                _assemblyState.Logger.LogWarning
                   (
                       $"Directive {statement.Directive.Type.Stringified()} ignored for CPU", 
                       statement
                   );
                return Jump.NoJump;
            }
            _assemblyState.BankOff = false;
            _assemblyState.Bank = operand;
        }
        return Jump.NoJump;
    }
    
    private Jump CompileXorStatement(Expression expression)
    {
        _assemblyState.Output.Xor = _evaluator.EvalByte(expression);
        return Jump.NoJump;
    }
    
    private Jump CompileFormatStatement(Expression expression)
    {
        if (_assemblyState.Format != OutputFormat.None)
        {
            throw new CompileException
            (
                CompileExceptionType.FormatPreviouslySpecified, 
                expression
            );
        }
        if (_assemblyState.Output.Started)
        {
            throw new CompileException
            (
                CompileExceptionType.FormatChangedAfterCompilation, 
                expression
            );
        }
        var format = FormatLookup.ByName(ExpressionFolder.EvalStringLiteral(expression));
        if (format == null)
        {
            throw new CompileException(CompileExceptionType.InvalidFormat, expression);
        }
        _assemblyState.Format = format.Value;
        return Jump.NoJump;
    }
    
    private Jump CompileGotoStatement(SingleExpressionDirectiveStatement statement)
    {
        if (statement.Expression is not PrimaryExpression expr || !expr.Expr.Type.IsIdent())
        {
            throw new CompileException
            (
                CompileExceptionType.LabelExpectedForGoto, 
                statement.Expression
            );
        }
        var labelName = expr.Expr.Text.ToString();
        var label = _assemblyState.SymbolTable.Lookup(labelName);
        
        if (label?.AsAddress() is Label l)
        {
            return new Jump(l.GotoIndex, statement);
        }
        if (label != null || _assemblyState.Passes != 0)
            throw new CompileException
            (
                label != null 
                    ? CompileExceptionType.LabelExpectedForGoto 
                    : CompileExceptionType.SymbolNotFound, 
                statement.Expression
            );
        _assemblyState.PassNeeded = true;
        return Jump.NoJump;
    }
    
    private void GenCpuListing(Token mnemonic, bool largePadding, int startOffset, int programCounter)
    {
        if (!_assemblyState.PrintListing) return;
        if (_assemblyState.AssemblyOptions.VerboseList)
        {
            _assemblyState.Listings.Append(mnemonic.LocationInfo);
        }
        else if (_assemblyState.AssemblyOptions.ListLineNumber)
        {
            _assemblyState.Listings.Append($"{mnemonic.Line,-11}");
        }
        _assemblyState.Listings.Append('.');
        if (!_assemblyState.Output.Synched)
        {
            _assemblyState.Listings.Append($"{startOffset,-7:x4}");
        }
        _assemblyState.Listings.Append($"{programCounter,-7:x4}");
        var bytes = _assemblyState.Output.BytesFrom(startOffset);
        if (_assemblyState.AssemblyOptions.ListMonitorCode)
        {
            _assemblyState.Listings.Append(GetDataBytesString(bytes, startOffset, programCounter).PadRight(24));
        }
        if (_assemblyState.AssemblyOptions.ListDisassembly)
        {
            var disassembly = _assemblyState.Cpu switch
            {
                Cpu.Gb80 or Cpu.I8080 or Cpu.Z80
                    => ZilogIntelEncoder.Decode(bytes, _assemblyState.Cpu, ref programCounter),
                Cpu.M6800 or Cpu.M6809 
                    => MotorolaEncoder.Decode(bytes, _assemblyState.Cpu, ref programCounter),
                Cpu.I86 => I86Encoder.Decode(bytes, ref programCounter),
                _ => M65xxEncoder.Decode
                (
                    bytes, 
                    _assemblyState.Cpu, 
                    _assemblyState.M16, 
                    _assemblyState.X16,  
                    ref programCounter
                )
            };
            var padding = largePadding ? 40 : 24;
            _assemblyState.Listings.Append(
                $"{disassembly}".PadRight(padding));
        }
        if (_assemblyState.AssemblyOptions.ListSourceCode)
        {
            var line = mnemonic.GetLineText();
            _assemblyState.Listings.Append(line);
        }
        _assemblyState.Listings.AppendLine();
    }

    private void GenDataListing(Token pseudoOp, int startOffset, int programCounter)
    {
        if (!_assemblyState.PrintListing) return;
        if (_assemblyState.AssemblyOptions.VerboseList)
        {
            _assemblyState.Listings.Append(pseudoOp.LocationInfo);
        }
        else if (_assemblyState.AssemblyOptions.ListLineNumber)
        {
            _assemblyState.Listings.Append($"{pseudoOp.Line,-11}");
        }
        _assemblyState.Listings.Append('>');
        if (!_assemblyState.Output.Synched)
        {
            _assemblyState.Listings.Append($"{startOffset,-7:x4}");
        }
        _assemblyState.Listings.Append($"{programCounter,-7:x4}");
        var bytes = _assemblyState.Output.BytesFrom(startOffset);
        var firstEight = bytes.Length > 8 ? bytes[..8] : bytes;
        if (!_assemblyState.AssemblyOptions.ListMonitorCode)
        {
            if (_assemblyState.AssemblyOptions.ListSourceCode)
            {
                var line = pseudoOp.GetLineText();
                _assemblyState.Listings.Append(line);
            }
        }
        else
        {
            _assemblyState.Listings.Append(GetDataBytesString(firstEight, startOffset, programCounter).PadRight(48));
            if (_assemblyState.AssemblyOptions.ListSourceCode)
            {
                var line = pseudoOp.GetLineText();
                _assemblyState.Listings.Append(line);
            }
            if (!_assemblyState.AssemblyOptions.TruncateListing && bytes.Length > 8)
            {
                _assemblyState.Listings.AppendLine();
                _assemblyState.Listings.Append('>');
                if (!_assemblyState.Output.Synched)
                {
                    _assemblyState.Listings.Append($"{startOffset + 8,-7:x4}");
                }
                _assemblyState.Listings.Append($"{programCounter + 8,-7:x4}");
                var remaining = bytes[8..];
                _assemblyState.Listings.Append(GetDataBytesString(remaining, startOffset + 8, programCounter + 8));
            }
        }
        _assemblyState.Listings.AppendLine();
    }
    
    private static string GetDataBytesString(ReadOnlySpan<byte> bytes, int offset, int programCounter)
    {
        var synched = programCounter == offset;
        var sb = new StringBuilder();
        for (var y = 0; y < bytes.Length; y += 8)
        {
            for (var x = 0; x < 8 && x + y < bytes.Length; x++)
            {
                sb.Append($" {bytes[y + x]:x2}");
            }
            if (bytes.Length <= y + 8)
            {
                break;
            }
            programCounter += 8;
            sb.AppendLine();
            sb.Append('>');
            if (!synched)
            {
                sb.Append($"{offset,-7:x4}");
            }
            sb.Append($"{programCounter,-7:x4}");
        }
        return sb.ToString();
    }

    private void GenAssignmentListing(Expression expression, char startChar, Value value)
    {
        if (!_assemblyState.PrintListing) return;
        if (_assemblyState.AssemblyOptions.VerboseList)
        {
            _assemblyState.Listings.Append(expression.LeftToken.LocationInfo);
        }
        else if (_assemblyState.AssemblyOptions.ListLineNumber)
        {
            _assemblyState.Listings.Append($"{expression.LeftToken.Line,-11}");
        }
        var valString = value.AsAddress() is not null
            ? $"${value.AsInt():x}"
            : value.ToString();
        if (valString.Length > 53)
        {
            valString = $"{valString[..50]}...";
        }

        var padding = _assemblyState.Output.Synched ? 56 : 63;
        _assemblyState.Listings.Append($"{startChar}{valString}".PadRight(padding));
        if (!_assemblyState.AssemblyOptions.ListSourceCode)
        {
            _assemblyState.Listings.AppendLine();
            return;
        }
        var line = expression.LeftToken.GetLineText();
        _assemblyState.Listings.AppendLine(line.ToString());
    }

    private void GenPcListing(Ast ast)
    {
        if (!_assemblyState.PrintListing) return;
        if (_assemblyState.AssemblyOptions.VerboseList)
        {
            _assemblyState.Listings.Append(ast.LeftToken.LocationInfo);
        }
        else if (_assemblyState.AssemblyOptions.ListLineNumber)
        {
            _assemblyState.Listings.Append($"{ast.LeftToken.Line,-11}");
        }
        _assemblyState.Listings.Append('.');
        if (!_assemblyState.Output.Synched)
        {
            _assemblyState.Listings.Append($"{_assemblyState.Output.Offset,-7:x4}");
        }
        _assemblyState.Listings.Append($"{_assemblyState.Output.ProgramCounter,-55:x4}");
        if (!_assemblyState.AssemblyOptions.ListSourceCode) return;
        var line = ast.LeftToken.GetLineText();
        _assemblyState.Listings.AppendLine(line.ToString());
    }

    private void GenEnumStatementListing(Token enumToken, Enumeration? enumValue)
    {
        if (!_assemblyState.PrintListing || enumValue == null) return;
        var enumName = _assemblyState.AssemblyOptions.VerboseList
            ? enumToken.LocationInfo + enumToken.Text.ToString()
            : enumToken.Text.ToString();
        _assemblyState.Listings.Append(enumValue.Report(enumName, false, false));
    }
    
    private void GenLabeledStatementListing(Statement statement)
    {
        if (!_assemblyState.PrintListing || 
            statement is LabelStatement { BeginsStatement: true })
        {
            return;
        }

        if (_assemblyState.AssemblyOptions.VerboseList)
        {
            _assemblyState.Listings.Append(statement.LeftToken.LocationInfo);
        }
        else if (_assemblyState.AssemblyOptions.ListLineNumber)
        {
            _assemblyState.Listings.Append($"{statement.LeftToken.Line,-11}");
        }
        _assemblyState.Listings.Append('.');
        if (!_assemblyState.Output.Synched)
        {
            _assemblyState.Listings.Append($"{_assemblyState.Output.Offset,-7:x4}");
        }
        _assemblyState.Listings.Append($"{_assemblyState.Output.ProgramCounter:x4}".PadRight(55));
        if (!_assemblyState.AssemblyOptions.ListSourceCode)
        {
            return;
        }
        switch (statement)
        {
            case LabeledBlockStatement labeledBlockStmt:
                _assemblyState.Listings.AppendLine(labeledBlockStmt.Label.Text.ToString());
                break;
            case LabelStatement { BeginsStatement: false } labeled:
            {
                var lineText = _assemblyState.AssemblyOptions.VerboseList
                    ? labeled.Label.GetLineText().ToString()
                    : labeled.Label.Text.ToString();
                if (_assemblyState.AssemblyOptions.ListLineNumber)
                {
                    _assemblyState.Listings.Append($"{labeled.Label.Line,-11}");
                }
                _assemblyState.Listings.AppendLine(lineText);
                break;
            }
        }
    }

    private void BankCrossed(Statement statement, int startAddress)
    {
        if (_assemblyState is { PassNeeded: false, AssemblyOptions.DoNotWarnBankCrossed: false } && 
            (startAddress & 0xff0000) != (_assemblyState.Output.ProgramCounter & 0xff0000))
        {
            _assemblyState.Logger.LogWarning("Bank boundary crossed here", statement);
        }
    }

}