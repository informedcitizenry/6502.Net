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
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;
using System.Collections.Frozen;
using System.Diagnostics;

namespace Sixty502DotNet.Shared.Parse;

public sealed partial class Parser
{
    private readonly bool _parseLegacyDelimiters;
    
    private int _statementIndex;
    
    private LabelStatement RefLabelStatement()
    {
        var reference = _current;
        Advance();
        var beginsStatement = BeginsStatement();
        if (!beginsStatement && !AtPreprocessor())
        {
            ConsumeEosOrColon();
        }
        return new LabelStatement(reference, beginsStatement)
        {
            StatementIndex = _statementIndex
        };
    }

    private SingleExpressionDirectiveStatement CpuDirectiveStatement()
    {
        var left = _current;
        Advance();
        var cpuNameExpr =  new PrimaryExpression(_current);
        var cpuName = ExpressionFolder.EvalStringLiteral(cpuNameExpr);
        var cpu = CpuLookup.ByName(cpuName);
        if (cpu == null)
        {
            throw new CompileException(CompileExceptionType.InvalidCpuSpecified, cpuNameExpr);
        }
        _lexer.Cpu = cpu.Value;
        Consume(TokenType.StringLiteral);
        ConsumeEosOrColon();
        return new SingleExpressionDirectiveStatement(left, cpuNameExpr);
    }

    private SimpleDirectiveStatement End()
    {
        var endKw = _current;
        Advance();
        ConsumeEos();
        _lexer.End();
        Ended = true;
        return new SimpleDirectiveStatement(endKw)
        {
            StatementIndex =  _statementIndex
        };
    }
    
    private Statement IdentFirst(ErrorLogger? logger)
    {
        var left = _current;
        Mark();
        Advance();
        var beginsStatement = BeginsStatement();
        if (CheckEosOrColon() || 
            _current.Type.IsExtraDirective() ||
            (beginsStatement && !Check(TokenType.MacroName)))
        {
            _ = Match(TokenType.Colon);
            DiscardNewlines();
            Release();
            return new LabelStatement(left, beginsStatement)
            {
                StatementIndex = _statementIndex
            };
        }
        switch (_current.Type)
        {
            case TokenType.BincludeKw:
            case TokenType.BlockKw:
            case TokenType.EnumKw:
            case TokenType.Eq:
            case TokenType.EquKw:
            case TokenType.FunctionKw:
            case TokenType.GlobalKw:
            case TokenType.IncludeKw:
            case TokenType.MacroKw:
            case TokenType.ProcKw:
                Release();
                return _current.Type switch
                {
                    TokenType.BincludeKw or
                    TokenType.IncludeKw => Include(left, logger),
                    TokenType.BlockKw or 
                    TokenType.ProcKw => LabeledBlockStatement(),
                    TokenType.EnumKw => Enum(),
                    TokenType.Eq or
                    TokenType.EquKw or 
                    TokenType.GlobalKw => ConstantAssign(left),
                    TokenType.FunctionKw => FunctionDefStatement(),
                    _ => Macro()
                };
        }
        _lexer.LexCommand = false;
        if (_current.Type == TokenType.MacroName)
        {
            var peek = Peek();
            if (peek.Type != TokenType.OpenBracket &&
                peek.Type != TokenType.Dot &&
                !peek.Type.IsAssignment())
            {
                Release();
                return MacroName(left, logger);
            }
        }
        Restore();
        return VarAssignment(true);
    }

    private EnumDeclaration Enum()
    {
        var name = _previous;
        Advance();
        TokenType delimiter;
        if (_parseLegacyDelimiters)
        {
            ConsumeEos();
            delimiter = TokenType.EndenumKw;
        }
        else
        {
            DiscardNewlines();
            Consume(TokenType.OpenBrace);
            delimiter = TokenType.CloseBrace;
        }
        SetRecoverDelimiter(delimiter);
        var enumeration = new EnumDeclaration
        {
            Enum = name,
            LeftToken = name,
            StatementIndex = _statementIndex
        };
        do
        {
            DiscardNewlines();
            Expression ? defaultValue = null;
            var enumerator = _current;
            ConsumeIdent();
            if (Match(TokenType.Eq))
            {
                DiscardNewlines();
                defaultValue = Expression();
                if (!defaultValue.IsConstant())
                {
                    throw new CompileException(CompileExceptionType.ValueNotConstant, defaultValue);
                }
            }
            ConsumeEosOrColon();
            enumeration.Enumerators.Add(new Enumerator
            {
                DefaultValue = defaultValue,
                LeftToken = enumerator,
                Name = enumerator,
                RightToken = defaultValue?.RightToken ?? enumerator,
                StatementIndex = _statementIndex
            });
            DiscardNewlines();
        } while (!Check(delimiter));
        enumeration.RightToken = _current;
        Match(delimiter);
        ResetRecoverDelimiter();
        ConsumeEos();
        return enumeration;
    }

    private NamespaceBlockStatement Namespace()
    {
        var directive = _current;
        Advance();
        SetRecoverDelimiter(TokenType.EndnamespaceKw);
        var block = NamespacePart(directive);
        block.LeftToken = directive;
        return block;
    }

    private NamespaceBlockStatement NamespacePart(Token directive)
    {
        var namespaceToken = _current;
        _lexer.LexCommand = false;
        ConsumeIdent();
        if (!Match(TokenType.Dot))
        {
            return new NamespaceBlockStatement
            {
                Statements = Block(directive, true),
                StatementIndex = _statementIndex,
                LeftToken = namespaceToken,
                Namespace = namespaceToken,
                RightToken = _previous2,
            };
        }
        DiscardNewlines();
        return new NamespaceBlockStatement
        {
            Statements = new List<Statement>
            {
                NamespacePart(directive)
            },
            StatementIndex = _statementIndex,
            LeftToken = namespaceToken,
            Namespace = namespaceToken,
            RightToken = _previous2
        };
    }

    private PageBlockStatement Page()
    {
        var directive = _current;
        Advance();
        SetRecoverDelimiter(TokenType.EndpageKw);
        return new PageBlockStatement
        {
            Statements = Block(directive, true),
            StatementIndex = _statementIndex,
            LeftToken = directive,
            RightToken = _previous2
        };
    }

    private ConstantAssignStatement ConstantAssign(Token identifier)
    {
        var lhs = new PrimaryExpression(identifier);
        var op = _current;
        Advance();
        var constStat = new ConstantAssignStatement(lhs, op, Expression())
        {
            StatementIndex = _statementIndex,
            RightToken = _previous
        };
        ConsumeEosOrColon();
        return constStat;
    }

    private Statement VarAssignment(bool checkEos) 
    {
        var lhs = ParsePrecedence(this, Precedence.Call);
        if (!lhs.IsLValue())
        {
            throw new CompileException
            (
                CompileExceptionType.UnexpectedExpression, 
                lhs
            );
        }
        var op = _current;
        var isNotPrimaryLvalue = lhs is not PrimaryExpression && 
                                 lhs is not ArrayInitExpression { IsTuple: true };
        if (!op.Type.IsAssignment() || 
            (isNotPrimaryLvalue &&
             checkEos && !op.Type.IsVarAssignment()))
        {
            throw new CompileException
            (
                isNotPrimaryLvalue && !op.Type.IsVarAssignment()
                    ? CompileExceptionType.NotVarAssignment
                    : CompileExceptionType.CommandRequired, 
                op
            );
        }
        Advance();
        DiscardNewlines();
        if (op.Type == TokenType.Eq && checkEos)
        {
            var constStat = new ConstantAssignStatement(lhs, op, Expression());
            ConsumeEosOrColon();
            return constStat;
        }
        var varStat = new VarAssignmentStatement(lhs, op, Expression());
        if (checkEos)
            ConsumeEosOrColon();
        return varStat;
    }
    
    private LabeledBlockStatement LabeledBlockStatement()
    {
        var label = _previous;
        var command = _current;
        SetRecoverDelimiter(command.Type.GetDelimiter());
        Advance();
        return new LabeledBlockStatement(label, command)
        {
            Statements = Block(command, true),
            StatementIndex = _statementIndex,
            RightToken = _previous2

        };
    }

    private FunctionDefinitionStatement FunctionDefStatement()
    {
        var funcName = _previous;
        var funcDirective = _current;
        Advance();
        SetRecoverDelimiter(TokenType.EndfunctionKw);
        var parameters = new List<PrimaryExpression>();
        var defaultValues = new List<Expression>();
        var eos = _parseLegacyDelimiters ? TokenType.Newline : TokenType.OpenBrace;
        while (!Check(eos))
        {
            if (Check(TokenType.Eof))
                throw new ParserException(TokenType.OpenBrace, _current);
            ConsumeIdent();
            parameters.Add(new PrimaryExpression(_previous)
            {
                StatementIndex = _statementIndex
            });
            var commaOrEq = _current;
            if (Match(TokenType.Eq))
            {
                DiscardNewlines();
                defaultValues.Add(Expression());
            }
            else if (defaultValues.Count > 0)
            {
                throw new CompileException(CompileExceptionType.DefaultValueNotSpecified, commaOrEq);
            }
            if (!Match(TokenType.Comma))
            {
                break;
            }
            DiscardNewlines();
            if (Check(eos))
                throw new CompileException
                (
                    CompileExceptionType.ExpectedExpression,
                    _current
                );
        }
        return new FunctionDefinitionStatement(funcName, parameters, defaultValues)
        {
            Body = Block(funcDirective, true),
            StatementIndex = _statementIndex,
            RightToken = _previous2

        };
    }

    private Statement Macro()
    {
        SetRecoverDelimiter(TokenType.EndmacroKw);
        var name = $".{_previous.Text}";
        if (Macros.ContainsKey(name))
            throw new CompileException
            (
                CompileExceptionType.MacroPreviousDefined,
                _previous
            );
        if (_lexer.CheckDirectiveType(name) != TokenType.MacroName)
        {
            throw new CompileException(CompileExceptionType.MacroNameNotPermitted, _previous);
        }
        var comparer = _lexer.Behavior is LexerBehavior.DefaultCaseInsensitive or LexerBehavior.LegacyDelimitersCaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var macro = new Macro(_previous, _lexer.Source, comparer);
        Advance();
        var column = _current.Location.Start;
        var index = 0;
        var groups = 0;
        var defaultValueColumn = 0;
        
        var delimiter = _parseLegacyDelimiters ? TokenType.Newline : TokenType.OpenBrace;
        while (!Check(delimiter))
        {
            if (Check(TokenType.Eof))
            {
                throw new ParserException(delimiter, _current);
            }
            ConsumeIdent();
            string? defaultValue = null;
            var paramName = _previous.Text.ToString();
            if (Match(TokenType.Eq))
            {
                defaultValueColumn = _current.Location.Start;
                while (!(Check(TokenType.Comma) && groups == 0) && 
                       !Check(delimiter))
                {
                    if (Check(TokenType.Eof))
                    {
                        throw new ParserException(TokenType.OpenBrace, _current);
                    }
                    if (Match(TokenType.OpenParen) || Match(TokenType.OpenBracket))
                    {
                        groups++;
                    }
                    else if (groups > 0 && (Match(TokenType.CloseParen) || Match(TokenType.CloseBracket)))
                    {
                        groups--;
                    }
                    else
                    {
                        Advance();
                    }
                }
                defaultValue = _lexer.Source.Text[defaultValueColumn.._previous.Location.End].ToString();
            }
            else if (defaultValueColumn > 0 && defaultValueColumn < column)
            {
                throw new CompileException
                (
                    CompileExceptionType.DefaultValueNotSpecified,
                    _previous
                );
            }
            if (!macro.AddParameter(paramName, index, defaultValue))
            {
                throw new CompileException(CompileExceptionType.SymbolRedefined, _previous);
            }
            index++;
            if (!Match(TokenType.Comma)) break;
            if (Check(delimiter))
                throw new CompileException
                (
                    CompileExceptionType.ParameterExpected,
                    new PrimaryExpression(_previous)
                );
        }
        if (!_parseLegacyDelimiters)
        {
            DiscardNewlines();
        }
        Consume(delimiter);
        delimiter = _parseLegacyDelimiters ? TokenType.EndmacroKw : TokenType.CloseBrace;
        macro.Start = _previous.Location.End;
        macro.StartLine = _current.Line;
        while (!Match(delimiter))
        {
            if (Check(TokenType.Eof))
            {
                throw new ParserException(TokenType.OpenBrace, _previous);
            }
            if (Check(TokenType.NamedArg))
            {
                if (!macro.AddNamedParameterLocation(_current.Text.ToString()[1..], _current.Location.Start))
                {
                    throw new CompileException(CompileExceptionType.SymbolNotFound, _current);
                }
            }
            else if (Check(TokenType.MacroArg) || Check(TokenType.VarArg))
            {
                var numberedIndex = Check(TokenType.VarArg) 
                    ? -1 
                    : int.Parse(_current.Text.ToString()[1..]);
                macro.AddNumberedParamLocation(numberedIndex, _current.Location.Start);
            }
            Advance();
        }
        macro.End = _previous.Location.Start;
        ResetRecoverDelimiter();
        ConsumeEos();
        Macros[name] = macro;
        return Statement();
    }
    
    private Statement MacroName(Token? label, ErrorLogger? logger)
    {
        var macroToken = _current;
        if (!Macros.TryGetValue(macroToken.Text.ToString(), out var macro))
        {
            throw new CompileException
            (
                CompileExceptionType.MacroNotDefined, 
                _current
            );
        }
        macro.Expanded = true;
        Advance();
        var args = new List<string>();
        var group = 0;
        var argIndex = _current.Location.Start;
        var argStart = argIndex;
        while (!CheckEos())
        {
            if (Match(TokenType.OpenParen) || Match(TokenType.OpenBracket))
            {
                group++;
            }
            else switch (group)
            {
                case > 0 when Match(TokenType.CloseParen) || Match(TokenType.CloseBracket):
                    group--;
                    if (CheckEos())
                    {
                        args.Add(_lexer.Source.Text[argIndex.._current.Location.Start].ToString());
                    }
                    break;
                case 0 when Match(TokenType.Comma):
                {
                    if (argIndex == _previous.Location.Start)
                    {
                        throw new CompileException(CompileExceptionType.ParameterExpected, _previous);
                    }
                    args.Add(_lexer.Source.Text[argIndex.._previous.Location.Start].ToString());
                    DiscardNewlines();
                    argIndex = _current.Location.Start;
                    break;
                }
                default:
                {
                    Advance();
                    if (CheckEos() && argIndex < _current.Location.Start)
                    {
                        args.Add(_lexer.Source.Text[argIndex.._previous.Location.End].ToString());
                    }
                    break;
                }
            }
        }
        var argList = _lexer.Source.Text.Slice(argStart, _current.Location.Start - argStart);
        ConsumeEos();
        Mark();
        var currentSource = _lexer.Source;
        try
        {
            SetSource
            (
                new Source(macro.Token.Source.Name, macro.Expand(args, argList)),
                macro.StartLine,
                new Inclusion
                (
                    macroToken.Source.Name,
                    macroToken.Line,
                    macroToken.Column,
                    true
                )
            );
            var block = ParseModule(logger);
            if (label != null)
            {
                return new LabeledBlockStatement(label.Value, macroToken.CopyWithType(TokenType.BlockKw))
                {
                    Statements = block.Statements,
                    StatementIndex = _statementIndex,
                    RightToken = block.RightToken

                };
            }
            return new AnonymousBlockStatement(macroToken)
            {
                Statements = block.Statements,
                StatementIndex = _statementIndex,
                RightToken = block.RightToken

            };
        }
        catch (ArgumentException)
        {
            throw new CompileException(CompileExceptionType.TooFewArguments, macroToken);
        }
        finally
        {
            ReturnToSource(currentSource);
        }
    }

    private Statement Include(Token? label, ErrorLogger? logger)
    {
        var directive = _current;
        Advance();
        var fileToken = _current;
        var fileName = ExpressionFolder.EvalStringLiteral(Expression());
        ConsumeEosOrColon();
        Mark(); // to get back
        var currentSource = _lexer.Source;
        try
        {
            _lexer.Inclusions.Add
            (
                new Inclusion
                (
                    fileToken.Source.Name,
                    fileToken.Line,
                    fileToken.Column
                )
            );
            ChangeSource(fileName);
            var block = ParseModule(logger);
            if (directive.Type == TokenType.IncludeKw)
            {
                if (label != null)
                {
                    block.Statements.Insert(0, new LabelStatement(label.Value, false)
                    {
                        StatementIndex = _statementIndex
                    });
                }
                return block;
            }
            if (label != null)
            {
                return new LabeledBlockStatement(label.Value, directive)
                {
                    Statements = block.Statements,
                    StatementIndex = _statementIndex,
                    RightToken = block.RightToken

                };
            }
            return new AnonymousBlockStatement(directive)
            {
                Statements = block.Statements,
                StatementIndex = _statementIndex,
                RightToken = block.RightToken
            };
        }
        catch (FileNotFoundException)
        {
            throw new CompileException(CompileExceptionType.FileNotFound, fileToken);
        }
        finally
        {
            ReturnToSource(currentSource);
        }
    }

    private static readonly FrozenSet<TokenType> s_86Prefixes =
    [
        TokenType.Rep,
        TokenType.Repe, TokenType.Repne, TokenType.Repnz,
        TokenType.Repz
    ];

    private static readonly FrozenSet<TokenType> s_strings86 =
    [
        TokenType.Cmps, TokenType.Cmpsb, TokenType.Cmpsw,
        TokenType.Ins, TokenType.Insb, TokenType.Insw,
        TokenType.Lods, TokenType.Lodsb, TokenType.Lodsw,
        TokenType.Movs, TokenType.Movsb, TokenType.Movsw,
        TokenType.Scas, TokenType.Scasb, TokenType.Scasw, 
        TokenType.Stos, TokenType.Stosb, TokenType.Stosw
    ];
    
    private Statement CpuInstructionStatement()
    {
        var mnemonic = _current;
        Advance();
        if (_lexer.Cpu == Cpu.I86 && s_86Prefixes.Contains(mnemonic.Type))
        {
            if (!s_strings86.Contains(_current.Type))
            {
                ConsumeEosOrColon();
                return new CpuInstructionStatement(mnemonic, new Operand(OperandType.Implied, mnemonic))
                {
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            return new I86RepInstructionStatement
            (
                mnemonic, 
                (CpuInstructionStatement)CpuInstructionStatement()
            )
            {
                StatementIndex = _statementIndex
            };
        }
        _lexer.LexCommand = false;
        var assertCount = _states.Count;
        var operand = Operand();
        Debug.Assert(_states.Count == assertCount, 
            $"In call to Operand(), one or more calls to Mark() did not have a matching call to Release()/Restore() parsing line {_previous.Line}");
        var rightToken = _previous;
        ConsumeEosOrColon();
        return new CpuInstructionStatement(mnemonic, operand)
        {
            RightToken = rightToken,
            StatementIndex = _statementIndex
        };
    }

    private PseudoOpStatement PseudoOpStatement()
    {
        var pseudoOp = new PseudoOpStatement(_current)
        {
            StatementIndex = _statementIndex
        };
        Advance();
        while (true)
        {  
            pseudoOp.Expressions.Add(Match(TokenType.QuestionMark) ? null : Expression());
            if (!Match(TokenType.Comma)) break;
            DiscardNewlines();
        }
        pseudoOp.RightToken = _previous;
        ConsumeEosOrColon();
        return pseudoOp;
    }

    private Statement DirectiveStatement()
    {
        var directive = _current;
        Advance();
        if (CheckEosOrColon() ||
            (directive.Type is TokenType.BankKw or TokenType.DpKw && Match(TokenType.QuestionMark)))
        {
            ConsumeEosOrColon();
            return new SimpleDirectiveStatement(directive)
            {
                StatementIndex = _statementIndex
            };
        }
        List<Expression> operands = [];
        do
        {
            operands.Add(Expression());
            if (!Match(TokenType.Comma)) break;
            DiscardNewlines();
        }
        while (true);
        ConsumeEosOrColon();
        if (operands.Count == 1)
        {
            return new SingleExpressionDirectiveStatement(directive, operands[0])
            {
                StatementIndex = _statementIndex,
                RightToken = operands[0].RightToken
            };
        }
        return new MultiExpressionDirectiveStatement(directive)
        {
            Expressions = operands,
            StatementIndex = _statementIndex,
            RightToken = operands[^1].RightToken
        };
    }
    
    private AnonymousBlockStatement AnonymousBlockStatement()
    {
        var directive = _current;
        Advance();
        return new AnonymousBlockStatement(directive)
        {
            Statements = Block(directive, true),
            StatementIndex = _statementIndex,
            RightToken = _previous
        };
    }

    private ForStatement ForStatement()
    {
        var forDirective = _current;
        SetRecoverDelimiter(TokenType.NextKw);
        Advance();
        VarAssignmentStatement? init = null;
        if (!Check(TokenType.Comma))
        {
            init = VarAssignment(false) as VarAssignmentStatement;
        }
        Consume(TokenType.Comma);
        DiscardNewlines();
        var condition = !Check(TokenType.Comma) ? Expression() : null;
        Consume(TokenType.Comma);
        DiscardNewlines();
        var iterators = new List<Statement>();
        while (true)
        {
            iterators.Add(VarAssignment(false));
            if (!Match(TokenType.Comma)) break;
            DiscardNewlines();
        }
        return new ForStatement(forDirective, init, condition, iterators)
        {  
            Block = Block(forDirective, true),
            StatementIndex = _statementIndex,
            RightToken = _previous2
        };
    }

    private ForeachStatement ForeachStatement()
    {
        var forDirective = _current;
        SetRecoverDelimiter(TokenType.NextKw);
        Advance();
        var enumerator = _current;
        ConsumeIdent();
        Consume(TokenType.Comma);
        DiscardNewlines();
        var enumerable = Expression();
        return new ForeachStatement(forDirective, enumerator, enumerable)
        {
            Block = Block(forDirective, true),
            StatementIndex = _statementIndex,
            RightToken = _previous2
        };
    }

    private IfStatement IfStatement()
    {
        var ifDirective = _current;
        var ifBlock = IfBlock(ifDirective);
        var elseIfBlocks = new List<IfBlock>();
        while (Check(TokenType.ElseifKw) || 
               Check(TokenType.ElseifdefKw) ||
               Check(TokenType.ElseifndefKw))
        {
            elseIfBlocks.Add(IfBlock(ifDirective));
        }
        IList<Statement> elseBlock;
        if (Match(TokenType.ElseKw))
        {
            SetRecoverDelimiter(TokenType.EndifKw);
            elseBlock = Block(_previous, true);
        }
        else
        {
            elseBlock = [];
        }
        return new IfStatement(ifBlock)
        {
            ElseIfBlocks = elseIfBlocks,
            ElseBlock = elseBlock,
            RightToken = _previous2,
            StatementIndex = _statementIndex
        };
    }
    
    private BlockStatement LetStatement()
    {
        BlockStatement assigns = new()
        {
            StatementIndex = _statementIndex
        };
        Advance();
        do
        {
            assigns.Statements.Add(VarAssignment(false));
            _lexer.SetLogicalNewline(_current.Location.End);
            if (!Match(TokenType.Comma)) break;
            DiscardNewlines();
        } 
        while (!CheckEosOrColon());
        ConsumeEosOrColon();
        return assigns;
    }
    
    private IfBlock IfBlock(Token ifDirective)
    {
        SetRecoverDelimiter(TokenType.EndifKw);
        Advance();
        var condition = Expression();
        return new IfBlock(ifDirective, condition)
        {
            Block = Block(ifDirective, _parseLegacyDelimiters),
            StatementIndex = _statementIndex,
            RightToken = _previous2
        };
    }

    private ExpressionBlockStatement ExpressionBlock()
    {
        if (Match(TokenType.DoKw))
        {
            SetRecoverDelimiter(TokenType.WhiletrueKw);
            var doDirective = _previous;
            var block = Block(doDirective, false);
            if (!_parseLegacyDelimiters)
            {
                Consume(TokenType.WhileKw);
            }
            var condition = Expression();
            ConsumeEos();
            return new ExpressionBlockStatement(doDirective, condition)
            {
                Block = block,
                StatementIndex = _statementIndex,
                RightToken = condition.RightToken

            };
        }
        var directive = _current;
        SetRecoverDelimiter(_current.Type.GetDelimiter());
        Advance();
        var expression = Expression();
        return new ExpressionBlockStatement(directive, expression)
        {
            Block = Block(directive, true),
            StatementIndex = _statementIndex,
            RightToken = _previous2
        };
    }

    private SwitchStatement SwitchStatement()
    {
        var switchDirective = _current;
        SetRecoverDelimiter(TokenType.EndswitchKw);
        Advance();
        var expression = Expression();
        if (_parseLegacyDelimiters)
        {
            ConsumeEos();
        }
        else
        {
            DiscardNewlines();
            _lexer.LexCommand = true;
            Consume(TokenType.OpenBrace);
        }
        var caseBlocks = new List<SwitchCaseBlock>();
        var switchBlockDelim = _parseLegacyDelimiters 
            ? TokenType.EndswitchKw 
            : TokenType.CloseBrace;
        DiscardNewlines();
        while (!Check(switchBlockDelim))
        {
            caseBlocks.Add(CaseBlocks());
        }
        Consume(switchBlockDelim);
        ConsumeEos();
        ResetRecoverDelimiter();
        return new SwitchStatement(switchDirective, expression)
        {
            Cases = caseBlocks,
            StatementIndex = _statementIndex,
            RightToken = _previous2
        };
    }

    private SwitchCaseBlock CaseBlocks()
    {
        var isDefault = false;
        var cases = new List<Expression>();
        var firstCase = _current;
        while (Check(TokenType.CaseKw) || Check(TokenType.DefaultKw))
        {
            if (Match(TokenType.DefaultKw))
            {
                isDefault = true;
            }
            else
            {
                Advance();
                var caseLabel = Expression();
                if (!caseLabel.IsConstant())
                {
                    throw new CompileException(CompileExceptionType.CaseLabelNotConstant, caseLabel);
                }
                cases.Add(caseLabel);
            }
            ConsumeEosOrColon();
            DiscardNewlines();
        }
        var caseStatements = new List<Statement>();
        var switchBlockDelim = _parseLegacyDelimiters ? TokenType.EndswitchKw : TokenType.CloseBrace;
        while (!Check(TokenType.CaseKw) && !Check(TokenType.DefaultKw) && !Check(switchBlockDelim))
        {
            caseStatements.Add(Statement());
            DiscardNewlines();
        }
        return new SwitchCaseBlock
        {
            LeftToken = firstCase,
            Block = caseStatements,
            Cases = cases,
            IsDefault = isDefault,
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }
    
    private List<Statement> Block(Token declToken, bool eos)
    {
        var openType = declToken.Type;
        var list = new List<Statement>();
        var inElseIfBock = false;
        var declEndToken = _previous;
        if (_parseLegacyDelimiters)
        {
            ConsumeEos();
            while (!Match(openType.GetDelimiter()))
            {
                if (openType is TokenType.IfKw or TokenType.IfdefKw or TokenType.IfndefKw && 
                    (Check(TokenType.ElseKw) || 
                     Check(TokenType.ElseifKw) ||
                     Check(TokenType.ElseifdefKw) ||
                     Check(TokenType.ElseifndefKw)))
                {
                    inElseIfBock = true;
                    break;
                }
                if (Check(TokenType.Eof))
                {
                    _recoveryDelimiters.Clear();
                    throw new UnresolvedDeclException
                    (
                        CompileExceptionType.ExpectedTokenException, 
                        openType.GetDelimiter(), 
                        declToken,
                        declEndToken, 
                        _previous
                    );
                }
                list.Add(Statement());
                DiscardNewlines();
            }
            ResetRecoverDelimiter();
            if (eos && !inElseIfBock)
                ConsumeEos();
            return list;
        }
        DiscardNewlines();
        _lexer.LexCommand = true;
        if (!Match(TokenType.OpenBrace))
        {
            throw new CompileException(CompileExceptionType.ExpectedOpenBrace, _current);
        }
        DiscardNewlines();
        while (!Match(TokenType.CloseBrace))
        {
            if (Check(TokenType.Eof))
                throw new UnresolvedDeclException
                (
                    CompileExceptionType.ExpectedTokenException,
                    TokenType.CloseBrace, 
                    declToken, 
                    declEndToken, 
                    _previous
                );
            list.Add(Statement());
            _lexer.LexCommand = true;
            DiscardNewlines();
        }
        ResetRecoverDelimiter();
        if (eos)
            ConsumeEos();
        return list;
    }
}