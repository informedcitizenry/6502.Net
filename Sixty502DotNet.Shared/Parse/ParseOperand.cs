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
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Sixty502DotNet.Shared.Parse;

public sealed partial class Parser
{
    private Operand Operand()
    {
        if (CheckEosOrColon())
        {
            return new Operand(OperandType.Implied, _previous)
            {
                StatementIndex = _statementIndex
            };
        }
        if (_previous.Type == TokenType.Ld && 
            _lexer.Cpu == Cpu.Gb80 &&
            ((Check(TokenType.A) && Peek().Type == TokenType.Comma) ||
             (Check(TokenType.Hl) && Peek().Type == TokenType.Comma) ||
             Check(TokenType.OpenParen)))
        {
            return Gb80Operand();
        }
        return _current.Type switch
        {
            var type when type.IsI86Segment() && Peek().Type == TokenType.Colon => I86OperandSegmentFirst(),
            var type when _lexer.Cpu == Cpu.I86 && (type.IsRegister() || type.IsStParenReg())
                => I86OperandRegisterFirst(),
            var type when type.IsI86PtrType() && Peek().Type == TokenType.Ptr => I86OperandPtrTypeFirst(),
            var type when type.IsRegister() => RegisterFirst(),
            TokenType.OpenParen when _lexer.Cpu != Cpu.I86 => OpenParen(0),
            TokenType.OpenParen => ExpressionFirst(0),
            TokenType.OpenBracket when _lexer.Cpu == Cpu.I86 => I86IndirectOperand(),
            TokenType.OpenBracket => IsCoercing(),
            TokenType.Pound => Immediate(0),
            _ => Match(TokenType.Comma)
                ? AutoIncrement()
                : ExpressionFirst(0)
        };
    }
    
    private Operand RegisterFirst()
    {
        var reg0 = _current;
        Mark();
        Advance();
        if (!Match(TokenType.Comma))
        {
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.Register, reg0)
                {
                    Registers = [reg0],
                    RightToken = reg0,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
            return ExpressionFirst(0);
        }
        DiscardNewlines();
        Release();
        if (Check(TokenType.OpenParen))
        {
            Mark();
            Advance();
            var index80 = Z80Indexed();
            if (index80 != null)
            {
                Release();
                return new Operand(OperandType.RegisterIndirectIndexed80, reg0)
                {
                    Expressions = [new UnaryOpExpression(index80.Operator, index80.Right, false)],
                    Registers = [reg0, index80.LeftToken],
                    RightToken = _previous,
                    StatementIndex =  _statementIndex
                };
            }
            if (_current.Type.IsRegister() && Peek().Type == TokenType.CloseParen)
            {
                var reg1 = _current;
                Advance();
                Advance();
                Release();
                return new Operand(OperandType.RegisterIndirectRegister, reg0)
                {
                    Registers = [reg0, reg1],
                    RightToken = _previous,
                    StatementIndex =  _statementIndex
                };
            }
            var expr =  Expression();
            Consume(TokenType.CloseParen);
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.RegisterIndirect, reg0)
                {
                    Expressions = [expr],
                    Registers = [reg0],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
            return new Operand(OperandType.Immediate80, reg0)
            {
                Expressions = [Expression()],
                Registers = [reg0],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        if (!_current.Type.IsRegister())
        {
            return new Operand(OperandType.Immediate80, reg0)
            {
                Registers = [reg0],
                Expressions = [Expression()],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        Mark();
        var regs = new List<Token>{ reg0, _current };
        Advance();
        if (CheckEosOrColon())
        {
            Release();
            return new Operand(OperandType.RegisterRegister, reg0)
            {
                Registers = regs,
                RightToken = regs[^1],
                StatementIndex =  _statementIndex
            };
        }
        if (Match(TokenType.Comma))
        {
            DiscardNewlines();
            while (_current.Type.IsRegister())
            {
                regs.Add(_current);
                Advance();
                if (!Match(TokenType.Comma))
                    break;
                DiscardNewlines();
            }
            Release();
            return new Operand(OperandType.RegisterList, reg0)
            {
                Registers = regs,
                RightToken = regs[^1],
                StatementIndex = _statementIndex
            };
        }
        Restore();
        return new Operand(OperandType.Immediate80, reg0)
        {
            Registers = [reg0],
            Expressions = [Expression()],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }

    private Operand OpenParen(int coercedSize, Token? coerceOpenBracket = null)
    {
        switch (coercedSize)
        {
            case 3:
                return ExpressionFirst(coercedSize, coerceOpenBracket);
            case 1 or 2:
                return OpenParenCoerced(coercedSize, coerceOpenBracket!.Value);
        }
        var openParen = _current;
        Mark();
        Advance();
        if (_current.Type.IsRegister())
        {
            var reg0 = _current;
            var index80 = Z80Indexed();
            if (index80 != null)
            {

                var indexOffsExpr = new UnaryOpExpression(index80.Operator, index80.Right, false);
                if (CheckEosOrColon())
                {
                    Release();
                    return new Operand(OperandType.Indexed80, openParen)
                    {
                        Registers = [reg0],
                        Expressions = [indexOffsExpr],
                        RightToken = _previous,
                        StatementIndex =  _statementIndex
                    };
                }
                if (Match(TokenType.Comma))
                {
                    DiscardNewlines();
                    if (_current.Type.IsRegister())
                    {
                        Mark();
                        var reg1 = _current;
                        Advance();
                        if (CheckEosOrColon())
                        {
                            Release();
                            Release();
                            return new Operand(OperandType.IndirectIndexed80, openParen)
                            {
                                Registers = [reg0, reg1],
                                Expressions = [indexOffsExpr],
                                RightToken = reg1,
                                StatementIndex = _statementIndex
                            };
                        }
                        Restore();
                    }
                    var exp = Expression();
                    Release();
                    return new Operand(OperandType.IndirectIndexed80Immediate, openParen)
                    {
                        Registers = [reg0],
                        Expressions = [indexOffsExpr, exp],
                        RightToken = exp.RightToken,
                        StatementIndex = _statementIndex
                    };   
                }
                Restore();
                return ExpressionFirst(coercedSize);
            }
            Advance();
            if (Match(TokenType.CloseParen))
            {
                if (CheckEosOrColon())
                {
                    Release();
                    return new Operand(OperandType.IndirectRegister, openParen)
                    {
                        Registers = [reg0],
                        RightToken =  _previous,
                        StatementIndex =  _statementIndex
                    };
                }
                if (Match(TokenType.Comma))
                {
                    DiscardNewlines();
                    var reg1 = _current;
                    if (reg1.Type.IsRegister())
                    {
                        Mark();
                        Advance();
                        if (CheckEosOrColon())
                        {
                            Release();
                            Release();
                            return new Operand(OperandType.IndirectRegisterRegister, openParen)
                            {
                                Registers = [reg0, reg1],
                                RightToken = reg1,
                                StatementIndex = _statementIndex
                            };
                        }
                        Restore();
                    }
                    else
                    {
                        Release();
                    }
                    return new Operand(OperandType.IndirectRegisterImmediate, openParen)
                    {
                        Registers =  [reg0],
                        Expressions = [Expression()],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
            }
            Restore();
            return ExpressionFirst(coercedSize);
        }
        var inner = Expression();
        if (Match(TokenType.CloseParen))
        {
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.Indirect, openParen)
                {
                    Expressions = [inner],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            if (Match(TokenType.Comma))
            {
                DiscardNewlines();
                if (!_current.Type.IsRegister())
                    throw new CompileException
                    (
                        CompileExceptionType.UnexpectedExpression,
                        _current
                    );
                var reg = _current;
                Advance();
                Release();
                return new Operand(OperandType.IndirectIndexed, openParen)
                {
                    Registers = [reg],
                    Expressions = [inner],
                    RightToken = reg,
                    StatementIndex = _statementIndex
                };
            }
        }
        else if (Match(TokenType.Comma) && 
                 _current.Type.IsRegister() && 
                 Peek().Type == TokenType.CloseParen)
        {
            var innerIndex = _current;
            Advance();
            Advance();
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.IndexedIndirect, openParen)
                {
                    Registers = [innerIndex],
                    Expressions = [inner],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            if (Match(TokenType.Comma))
            {          
                Release();
                DiscardNewlines();
                Consume(TokenType.Y);
                return new Operand(OperandType.IndexedIndirectIndexed, openParen)
                {
                    Registers = [innerIndex],
                    Expressions = [inner],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
        }
        Restore();
        return ExpressionFirst(coercedSize);
    }

    private Operand OpenParenCoerced(int coercedSize, Token coerceOpenBracket)
    {
        Mark();
        Advance();
        var expr = Expression();
        if (Match(TokenType.CloseParen))
        {
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.Indirect, coerceOpenBracket)
                {
                    CoercedSize = coercedSize,
                    Expressions = [expr],
                    RightToken = _previous,
                    StatementIndex =  _statementIndex
                };
            }
            if (Match(TokenType.Comma) && coercedSize == 1)
            {
                DiscardNewlines();
                if (Match(TokenType.Y) || Match(TokenType.Z))
                {
                    Release();
                    return new Operand(OperandType.IndirectIndexed, coerceOpenBracket)
                    {
                        Registers = [_previous],
                        Expressions = [expr],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
            }
        }
        else if (Match(TokenType.Comma) && Match(TokenType.X))
        {
            var x = _previous;
            if (Match(TokenType.CloseParen) &&
                CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.IndexedIndirect, coerceOpenBracket)
                {
                    CoercedSize = coercedSize,
                    Expressions = [expr],
                    Registers = [x],
                    RightToken = _previous,
                    StatementIndex =  _statementIndex
                };
            }
        }
        Restore();
        return ExpressionFirst(coercedSize);
    }
    
    private Operand IsCoercing()
    {
        Mark();
        var openBracket = _current;
        Advance();
        var coercedSize = 0;
        if (_current.Type == TokenType.IntLiteral && Peek().Type == TokenType.CloseBracket)
        {
            var sizeToken = _current;
            Advance();
            Advance();
            if (!CheckEosOrColon() && !Check(TokenType.Comma))
            {
                if (sizeToken.Text.Length == 1 && sizeToken.Text[0] == '8')
                {
                    coercedSize = 1;
                }
                else if (sizeToken.Text.Length == 2)
                {
                    switch (sizeToken.Text[0])
                    {
                        case '1' when sizeToken.Text[1] == '6':
                            coercedSize = 2;
                            break;
                        case '2' when sizeToken.Text[1] == '4':
                            coercedSize = 3;
                            break;
                    }
                }
            }
        }
        if (coercedSize > 0)
        {
            Release();
            return _current.Type switch
            {
                TokenType.OpenParen => OpenParen(coercedSize, openBracket),
                TokenType.OpenBracket => OpenBracket(coercedSize, openBracket),
                TokenType.Pound => Immediate(coercedSize, openBracket),
                _ => ExpressionFirst(coercedSize, openBracket)
            };
        }
        Restore();
        return OpenBracket(0);
    }

    private Operand OpenBracket(int coercedSize, Token? coerceOpenBracket = null)
    {
        if (coercedSize == 3) return ExpressionFirst(coercedSize, coerceOpenBracket);
        var openBracket = coerceOpenBracket ?? _current;
        Mark();
        Advance();
        if (coercedSize > 0)
        {
            var indirectLong = Expression();
            if (Match(TokenType.CloseBracket))
            {
                if (CheckEosOrColon())
                {
                    Release();
                    return new Operand(OperandType.IndirectLong, openBracket)
                    {
                        CoercedSize = coercedSize,
                        Expressions = [indirectLong],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
                if (coercedSize == 1 && Match(TokenType.Comma))
                {
                    Release();
                    DiscardNewlines();
                    var reg = _current;
                    var type = OperandType.IndirectLongZ;
                    if (!Match(TokenType.Z))
                    {
                        type = OperandType.IndirectLongIndexed;
                        Consume(TokenType.Y);
                    }
                    return new Operand(type, openBracket)
                    {
                        Registers = [reg],
                        CoercedSize = coercedSize,
                        Expressions = [indirectLong],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
            }
            Restore();
            return ExpressionFirst(coercedSize);
        }
        if (Match(TokenType.Comma))
        {
            Release();
            return IndirectAutoIncrement(openBracket);
        }
        if (_current.Type is TokenType.A or TokenType.B or TokenType.D && 
            Peek().Type == TokenType.Comma)
        {
            Mark();
            var accumulator = _current;
            Advance();
            Advance();
            var secondReg = _current;
            if (secondReg.Type.IsRegister())
            {
                Advance();
                if (Match(TokenType.CloseBracket) && CheckEosOrColon())
                {
                    Release();
                    Release();
                    return new Operand(OperandType.IndirectRegisterRegister6809, openBracket)
                    {
                        Registers = [accumulator, secondReg],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
            }
            Restore();
        }
        var expr = Expression();
        if (!Match(TokenType.CloseBracket))
        {
            if (Match(TokenType.Comma))
            {
                var r = _current;
                if (r.Type.IsRegister())
                {
                    Advance();
                    if (Match(TokenType.CloseBracket) && CheckEosOrColon())
                    {
                        Release();
                        return new Operand(OperandType.IndexedIndirect6809, openBracket)
                        {
                            Registers = [r],
                            Expressions = [expr],
                            RightToken = _previous,
                            StatementIndex = _statementIndex
                        };
                    }
                }
            }
            Restore();
            return ExpressionFirst(coercedSize);
        }
        if (!Match(TokenType.Comma))
        {
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.IndirectLong, openBracket)
                {
                    Expressions = [expr],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
            return ExpressionFirst(coercedSize);
        }
        Release();
        DiscardNewlines();
        if (Match(TokenType.Z))
        {
            return new Operand(OperandType.IndirectLongZ, openBracket)
            {   
                Registers = [_previous],
                Expressions = [expr],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        Consume(TokenType.Y);
        return new Operand(OperandType.IndirectLongIndexed, openBracket)
        {
            Registers = [_previous],
            Expressions = [expr],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }
    
    private Operand Immediate(int coercedSize, Token? coerceOpenBracket = null)
    {
        var pound = coerceOpenBracket ?? _current;
        Advance();
        if (coercedSize == 3)
            throw new CompileException
            (
                CompileExceptionType.ExpectedExpression,
                pound
            );
        var imm = Expression();
        if (CheckEosOrColon())
        {
            return new Operand(OperandType.Immediate, pound)
            {
                CoercedSize = coercedSize,
                Expressions = [imm],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        Consume(TokenType.Comma);
        DiscardNewlines();
        var expr =  Expression();
        if (CheckEosOrColon())
        {
            return new Operand(OperandType.ImmediateBranch, pound)
            {
                CoercedSize = coercedSize,
                Expressions = [imm, expr],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        Consume(TokenType.Comma);
        DiscardNewlines();
        Consume(TokenType.X);
        return new Operand(OperandType.ImmediateBranchIndexed, pound)
        {
            CoercedSize = coercedSize,
            Registers = [_previous],
            Expressions = [imm, expr],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }
    
    private Operand ExpressionFirst(int coercedSize, Token? coerceOpenBracket = null)
    {
        var expr = Expression();
        var leftToken = coerceOpenBracket ?? expr.LeftToken;
        if (coercedSize > 0)
        {
            return ExpressionFirstCoercedSize(expr, coercedSize, leftToken);
        } 
        if (!Match(TokenType.Comma))
        {
            if (Check(TokenType.Colon) && _lexer.Cpu == Cpu.I86)
            {
                Mark();
                Advance();
                if (!CheckEos() && _current.Type <= TokenType.Z )
                {
                    var addr = Expression();
                    if (CheckEos())
                    {
                        Release();
                        return new Operand(OperandType.SegmentAbsoluteDirect, leftToken)
                        {
                            Expressions = [expr, addr],
                            RightToken = _previous,
                            StatementIndex = _statementIndex
                        };
                    }
                }
                Restore();
            }
            return new Operand(OperandType.Address, leftToken)
            {
                Expressions = [expr],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
            
        }
        DiscardNewlines();
        if (_current.Type.IsRegister())
        {
            Mark();
            var reg = _current;
            Advance();
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.Indexed, leftToken)
                {
                    Expressions = [expr],
                    Registers = [reg],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
        }
        if (Check(TokenType.OpenParen) && Peek().Type is TokenType.Hl or TokenType.Ix or TokenType.Iy)
        {
            Mark();
            Advance();
            var index80 = Z80Indexed();
            if (index80 != null)
            {
                Release();
                var ix = index80.Left.LeftToken;
                var indexExpr = new UnaryOpExpression(index80.Operator, index80.Right, false);
                if (CheckEosOrColon())
                {
                    return new Operand(OperandType.Indexed80Bit, leftToken)
                    {
                        Registers = [ix],
                        Expressions = [expr, indexExpr],
                        RightToken = _previous,
                        StatementIndex =  _statementIndex
                    };
                }
                Consume(TokenType.Comma);
                DiscardNewlines();
                if (!_current.Type.IsRegister())
                {
                    throw new CompileException(CompileExceptionType.UnexpectedExpression, _current);
                }
                Advance();
                return new Operand(OperandType.IndirectIndexed80Bit, leftToken)
                {
                    Registers = [ix, _previous],
                    Expressions = [expr, indexExpr],
                    RightToken = _previous,
                    StatementIndex =  _statementIndex
                };
            }
            var hlReg = _current;
            Advance();
            if (hlReg.Type == TokenType.Hl && Match(TokenType.CloseParen))
            {
                Release();
                return new Operand(OperandType.IndirectHlBit, leftToken)
                {
                    Registers = [hlReg],
                    Expressions = [expr],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
        }
        var expr2 =  Expression();
        if (!Match(TokenType.Comma))
            return new Operand(OperandType.TwoExpression, leftToken)
            {
                Expressions = [expr, expr2],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        DiscardNewlines();
        var expr3 = Expression();
        return new Operand(OperandType.ThreeExpression, leftToken)
        {
            Expressions = [expr, expr2, expr3],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }

    private Operand ExpressionFirstCoercedSize
    (
        Expression expr, 
        int coercedSize, 
        Token coerceOpenBracket
    )
    {
        if (coercedSize == 3)
        {
            if (!Match(TokenType.Comma))
                return new Operand(OperandType.Address, coerceOpenBracket)
                {
                    CoercedSize =  coercedSize,
                    Expressions = [expr],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            DiscardNewlines();
            Consume(TokenType.X);
            return new Operand(OperandType.Indexed, coerceOpenBracket)
            {
                CoercedSize =  coercedSize,
                Expressions = [expr],
                RightToken = _previous,
                Registers = [_previous],
                StatementIndex = _statementIndex
            };
        }
        if (Match(TokenType.Comma))
        {
            DiscardNewlines();
            if (!_current.Type.IsRegister())
            {
                var secondExpr = Expression();
                if (coercedSize != 1)
                {
                    throw new CompileException(CompileExceptionType.UnexpectedExpression, secondExpr);
                }
                return new Operand(OperandType.TwoExpression, coerceOpenBracket)
                {
                    Expressions = [expr, secondExpr],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            var indexRegister = _current;
            Advance();
            return new Operand(OperandType.Indexed, coerceOpenBracket)
            {
                CoercedSize = coercedSize,
                Expressions = [expr],
                RightToken = _previous,
                Registers = [indexRegister],
                StatementIndex = _statementIndex
            };
        }
        return new Operand(OperandType.Address, coerceOpenBracket)
        {
            CoercedSize = coercedSize,
            Expressions = [expr],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }

    private Operand AutoIncrement()
    {
        var comma = _previous;
        DiscardNewlines();
        if (Match(TokenType.Minus))
        {
            var type = OperandType.AutoDecrement;
            DiscardNewlines();
            if (Match(TokenType.Minus))
            {
                DiscardNewlines();
                type = OperandType.AutoDecrement2;
            }
            var reg = _current;
            if (!reg.Type.IsRegister())
                throw new CompileException
                (
                    CompileExceptionType.UnexpectedExpression,
                    _current
                );
            Advance();
            return new Operand(type, comma)
            {
                Registers = [reg],
                RightToken = reg,
                StatementIndex = _statementIndex
            };
        }
        var register = _current;
        if (!register.Type.IsRegister())
            throw new CompileException
            (
                CompileExceptionType.UnexpectedExpression,
                _current
            );
        Advance();
        if (!Match(TokenType.Plus))
        {
            return new Operand(OperandType.RegisterOffset, comma)
            {
                Registers = [register],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }

        return new Operand
        (
            Match(TokenType.Plus) 
                ? OperandType.AutoIncrement2 
                : OperandType.AutoIncrement, 
            comma
        )
        {
            Registers = [register],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }

    private Operand IndirectAutoIncrement(Token openBracket)
    {
        if (Match(TokenType.Minus))
        {
            Consume(TokenType.Minus);
            if (!_current.Type.IsRegister())
                throw new CompileException
                (
                    CompileExceptionType.UnexpectedExpression,
                    _current
                );
            var reg = _current;
            Advance();
            Consume(TokenType.CloseBracket);
            return new Operand(OperandType.IndirectAutoDecrement, openBracket)
            {
                Registers = [reg],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        var incReg = _current;
        if (!incReg.Type.IsRegister())
            throw new CompileException
            (
                CompileExceptionType.UnexpectedExpression,
                _current
            );
        Advance();
        var type = OperandType.IndirectRegisterOffset;
        if (Match(TokenType.Plus))
        {
            Consume(TokenType.Plus);
            type = OperandType.IndirectAutoIncrement;
        }
        Consume(TokenType.CloseBracket);
        return new Operand(type, openBracket)
        {
            Registers = [incReg],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }
    
    private Operand Gb80Operand()
    {
        var reg0 = _current;
        if (Match(TokenType.Hl))
        {
            Consume(TokenType.Comma);
            DiscardNewlines();
            if (Check(TokenType.OpenParen))
            {
                Mark();
                Advance();
                if (_current.Type.IsRegister())
                {
                    var reg1 = _current;
                    Advance(); 
                    Mark();
                    if (Match(TokenType.CloseParen))
                    {
                        Release();
                        Release();
                        return new Operand(OperandType.RegisterIndirectRegister, reg0)
                        {
                            Registers = [reg0, reg1],
                            RightToken = _previous,
                            StatementIndex = _statementIndex
                        };
                    }
                    Restore();
                }
                var indirectExpr = Expression();
                Consume(TokenType.CloseParen);
                if (CheckEosOrColon())
                {
                    Release();
                    return new Operand(OperandType.RegisterIndirect, reg0)
                    {
                        Registers = [reg0],
                        Expressions = [indirectExpr],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
                Restore();
                return new Operand(OperandType.Immediate80, reg0)
                {
                    Registers = [reg0],
                    Expressions = [Expression()],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            if (Check(TokenType.Sp) && 
                Peek().Type is TokenType.Plus or TokenType.Minus)
            {
                var reg1 = _current;
                Advance();
                var op = _current;
                Advance();
                DiscardNewlines();
                var stackOffs = new BinaryOpExpression(new PrimaryExpression(reg1), op, ParsePrecedence(this, Precedence.Addition));
                return new Operand(OperandType.GbStackOffset, reg0)
                {
                    Registers = [reg0, reg1],
                    Expressions = [stackOffs],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            if (_current.Type.IsRegister())
            {
                var reg1 = _current;
                Mark();
                Advance();
                if (CheckEosOrColon())
                {
                    Release();
                    return new Operand(OperandType.RegisterRegister, reg0)
                    {
                        Registers = [reg0, reg1],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
                Restore();
            }
            return new Operand(OperandType.Immediate80, reg0)
            {
                Registers = [reg0],
                Expressions = [Expression()],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        if (Match(TokenType.OpenParen))
        {
            var openParen = _previous;
            reg0 = _current;
            if (reg0.Type.IsRegister() && Peek().Type == TokenType.CloseParen)
            {
                Advance();
                Advance();
                if (CheckEosOrColon())
                {
                    return new Operand(OperandType.IndirectRegister, openParen)
                    {
                        Registers = [reg0],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
                Consume(TokenType.Comma);
                DiscardNewlines();
                if (_current.Type.IsRegister())
                {
                    var reg1 = _current;
                    Mark();
                    Advance();
                    if (CheckEosOrColon())
                    {
                        Release();
                        return new Operand(OperandType.IndirectRegisterRegister, openParen)
                        {
                            Registers = [reg0, reg1],
                            RightToken = _previous,
                            StatementIndex = _statementIndex
                        };
                    }
                    Restore();
                }
                return new Operand(OperandType.IndirectRegisterImmediate, openParen)
                {
                    Registers = [reg0],
                    Expressions = [Expression()],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            if (Check(TokenType.Hl) && Peek().Type is TokenType.Plus or TokenType.Minus)
            {
                Mark();
                Advance();
                var op = _current;
                if (Match(TokenType.CloseParen))
                {
                    Release();
                    Consume(TokenType.Comma);
                    DiscardNewlines();
                    Consume(TokenType.A);
                    var operandType = op.Type switch
                    {
                        TokenType.Plus => OperandType.GbHlIncrementAccumulator,
                        _ => OperandType.GbHlDecrementAccumulator
                    };
                    return new Operand(operandType, openParen)
                    {
                        Registers = [reg0, _previous],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
                Restore();
            }
            Mark();
            var ff00 = ParsePrecedence(this, Precedence.Multiplication);
            var ff00Op = _current;
            if (Match(TokenType.Plus) || Match(TokenType.Minus))
            {
                if (Check(TokenType.C) && Peek().Type == TokenType.CloseParen)
                {
                    var cReg = _current;
                    ff00 = new BinaryOpExpression(ff00, ff00Op, new PrimaryExpression(cReg));
                    Advance();
                    Advance();
                    if (Match(TokenType.Comma))
                    {
                        DiscardNewlines();
                        if (Match(TokenType.A))
                        {
                            Release();
                            return new Operand(OperandType.GbIndirectIndexed, openParen)
                            {
                                Registers = [reg0, cReg, _previous],
                                Expressions = [ff00],
                                RightToken = _previous,
                                StatementIndex = _statementIndex
                            };
                        }
                    }
                }
                else
                {
                    ff00 = new BinaryOpExpression(ff00, ff00Op, ParsePrecedence(this, Precedence.Multiplication));
                    if (Match(TokenType.CloseParen) &&
                        Match(TokenType.Comma))
                    {
                        DiscardNewlines();
                        if (Match(TokenType.A))
                        {
                            Release();
                            return new Operand(OperandType.GbIndirect, openParen)
                            {
                                Registers = [_previous],
                                Expressions = [ff00],
                                RightToken = _previous,
                                StatementIndex = _statementIndex
                            };
                        }
                    }
                }
            }
            Restore();
            ff00 = Expression();
            Consume(TokenType.CloseParen);
            Consume(TokenType.Comma);
            DiscardNewlines();
            if (!_current.Type.IsRegister())
            {
                throw new CompileException(CompileExceptionType.UnexpectedToken, _current);
            }
            Advance();
            return new Operand(OperandType.IndirectIndexed, openParen)
            {
                Registers = [_previous],
                Expressions = [ff00],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        Advance(); // a
        Advance(); // ,
        DiscardNewlines();
        if (_current.Type.IsRegister())
        {
            Mark();
            var reg1 = _current;
            Advance();
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.RegisterRegister, reg0)
                {
                    Registers = [reg0, reg1],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
            return new Operand(OperandType.Immediate80, reg0)
            {
                Registers = [reg0],
                Expressions = [Expression()],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        if (!Match(TokenType.OpenParen))
            return new Operand(OperandType.Immediate80, reg0)
            {
                Registers = [reg0],
                Expressions = [Expression()],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        if (!_current.Type.IsRegister())
        {
            Mark();
            var indExpr = ParsePrecedence(this, Precedence.Multiplication);
            if (Check(TokenType.Plus) || Check(TokenType.Minus))
            {
                var op = _current;
                Advance();
                if (Match(TokenType.C))
                {
                    Mark();
                    var reg1 = _previous;
                    if (Match(TokenType.CloseParen))
                    {
                        Release();
                        Release();
                        return new Operand(OperandType.GbImmediateIndirectIndexed, reg0)
                        {
                            Registers = [reg0, reg1],
                            Expressions = [new BinaryOpExpression(indExpr, op, new PrimaryExpression(reg1))],
                            RightToken = _previous,
                            StatementIndex = _statementIndex
                        };
                    }
                    Restore();
                }
                indExpr = new BinaryOpExpression(indExpr, op, ParsePrecedence(this, Precedence.Multiplication));
                if (Match(TokenType.CloseParen) && CheckEosOrColon())
                {
                    Release();
                    return new Operand(OperandType.GbImmediateIndirect, reg0)
                    {
                        Registers = [reg0],
                        Expressions = [indExpr],
                        RightToken = _previous,
                        StatementIndex = _statementIndex
                    };
                }
            }
            Restore();
            Mark();
            indExpr = Expression();
            if (Match(TokenType.CloseParen) && CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.RegisterIndirect, reg0)
                {
                    Registers = [reg0],
                    Expressions = [indExpr],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
        }
        var secondReg = _current;
        if (Peek().Type == TokenType.CloseParen)
        {
            Mark();
            Advance();
            Advance();
            if (CheckEosOrColon())
            {
                Release();
                return new Operand(OperandType.RegisterIndirectRegister, reg0)
                {
                    Registers = [reg0, secondReg],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
        }
        if (Check(TokenType.Hl) && Peek().Type is TokenType.Plus or TokenType.Minus)
        {
            Mark();
            
            Advance();
            var op = _current;
            Advance();
            if (Match(TokenType.CloseParen))
            {
                Release();
                var operandType = op.Type switch
                {
                    TokenType.Plus => OperandType.GbAccumulatorHlIncrement,
                    _ => OperandType.GbAccumulatorHlDecrement
                };
                return new Operand(operandType, reg0)
                {
                    Registers = [reg0, secondReg],
                    RightToken = _previous,
                    StatementIndex = _statementIndex
                };
            }
            Restore();
        }
        return new Operand(OperandType.Immediate80, reg0)
        {
            Registers = [reg0],
            Expressions = [Expression()],
            RightToken = _previous,
            StatementIndex = _statementIndex
        };
    }
    
    private BinaryOpExpression? Z80Indexed()
    {
        var indexed = (Check(TokenType.Ix) || Check(TokenType.Iy)) &&
            (Peek().Type == TokenType.Plus || Peek().Type == TokenType.Minus);
        if (!indexed)
        {
            return null;
        }
        Mark();
        var reg0 = new PrimaryExpression(_current);
        Advance();
        var op = _current;
        Advance();
        var offs = ParsePrecedence(this, Precedence.Addition);
        if (Match(TokenType.CloseParen))
        {
            Release();
            return new BinaryOpExpression(reg0, op, offs)
            {
                StatementIndex = _statementIndex
            };
        }
        Restore();
        return null;
    }
}