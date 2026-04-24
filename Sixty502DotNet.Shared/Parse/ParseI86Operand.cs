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

using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;
using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Parse;

public sealed partial class Parser
{
    private readonly struct I86ModRegMem
    (
        OperandType registerSegmentOverrideType,
        OperandType segmentOverrideType,
        OperandType registerType,
        OperandType segmentOverrideTypeImm,
        OperandType typeImm,
        OperandType segmentOverrideTypeRegister,
        OperandType typeRegister
    )
    {
        public OperandType RegisterSegmentOverrideType { get; } = registerSegmentOverrideType;

        public OperandType SegmentOverrideType { get; } = segmentOverrideType;

        public OperandType RegisterType { get; } = registerType;
        
        public OperandType SegmentOverrideTypeImm { get; } = segmentOverrideTypeImm;
        
        public OperandType TypeImm { get; } = typeImm;
        
        public OperandType SegmentOverrideTypeRegister { get; } = segmentOverrideTypeRegister;
        
        public OperandType TypeRegister { get; } = typeRegister;
    }

    private static readonly FrozenDictionary
    <
        OperandType, I86ModRegMem
    > s_modRegRmOperands = new Dictionary
    <
        OperandType, I86ModRegMem
    >
    {
        {
            OperandType.IndirectLong,
            new I86ModRegMem
            (
                OperandType.RegisterSegmentOverrideDirect,
                OperandType.SegmentOverrideDirect,
                OperandType.RegisterDirect,
                OperandType.SegmentOverrideDirectImm,
                OperandType.DirectImm,
                OperandType.SegmentOverrideDirectRegister,
                OperandType.DirectRegister
            )
        },
        {
            OperandType.IndirectRegister86,
            new I86ModRegMem
            (
                OperandType.RegisterSegmentOverrideRegister,
                OperandType.SegmentOverrideRegister,
                OperandType.RegisterIndirectRegister86,
                OperandType.SegmentOverrideRegisterImm,
                OperandType.IndirectRegister86Imm,
                OperandType.SegmentOverrideRegisterRegister,
                OperandType.IndirectRegister86Register
            )
        },
        {
            OperandType.BaseIndex,
            new I86ModRegMem
            (
                OperandType.RegisterSegmentOverrideBaseIndex,
                OperandType.SegmentOverrideBaseIndex,
                OperandType.RegisterBaseIndex,
                OperandType.SegmentOverrideBaseIndexImm,
                OperandType.BaseIndexImm,
                OperandType.SegmentOverrideBaseIndexRegister,
                OperandType.BaseIndexRegister
            )
        },
        {
            OperandType.BaseDisplacement,
            new I86ModRegMem
            (
                OperandType.RegisterSegmentOverrideBaseDisplacement,
                OperandType.SegmentOverrideBaseDisplacement,
                OperandType.RegisterBaseDisplacement,
                OperandType.SegmentOverrideBaseDisplacementImm,
                OperandType.BaseDisplacementImm,
                OperandType.SegmentOverrideBaseDisplacementRegister,
                OperandType.BaseDisplacementRegister
            )
        },
        {
            OperandType.BaseIndexDisplacement,
            new I86ModRegMem
            (
                OperandType.RegisterSegmentOverrideBaseIndexDisplacement,
                OperandType.SegmentOverrideBaseIndexDisplacement,
                OperandType.RegisterBaseIndexDisplacement,
                OperandType.SegmentOverrideBaseIndexDisplacementImm,
                OperandType.BaseIndexDisplacementImm,
                OperandType.SegmentOverrideBaseIndexDisplacementRegister,
                OperandType.BaseIndexDisplacementRegister
            )
        }
    }.ToFrozenDictionary();
    
    private Operand I86OperandRegisterFirst()
    {
        List<Token> regs = [_current];
        if (Peek().Type != TokenType.Comma)
        {
            Mark();
            Advance();
            if (CheckEosOrColon() || regs[0].Type.IsStParenReg())
            {
                Release();
                return new Operand(OperandType.Register, regs[0])
                {
                    Registers = regs,
                    StatementIndex = _statementIndex,
                    RightToken = _current
                };
            }
            Restore();
            return new Operand(OperandType.Address, regs[0])
            {
                Expressions = [Expression()],
                RightToken = _previous,
                StatementIndex = _statementIndex
            };
        }
        var exprs = new List<Expression>();
        Advance(); // consume register
        Advance(); // consume comma
        DiscardNewlines();
        var ptrSize = 0;
        if (IsPtr())
        {
            ptrSize = _current.Type.PtrSize();
            Advance();
            Consume(TokenType.Ptr);
        }
        var segmentOverride = _current.Type.IsI86Segment() && Peek().Type == TokenType.Colon;
        var seg = _current;
        if (_current.Type.IsRegister() || _current.Type.IsStParenReg())
        {
            Mark();
            Advance();
            if (segmentOverride && Peek().Type == TokenType.OpenBracket)
            {
                Advance();
            }
            else if (CheckEosOrColon())
            {
                regs.Add(_previous);
                Release();
                return new Operand(OperandType.RegisterRegister, regs[0])
                {
                    Registers = regs,
                    StatementIndex = _statementIndex,
                    RightToken = _previous
                };
            }
            if (!segmentOverride && !Check(TokenType.OpenBracket))
            {
                Restore();
                return new Operand(OperandType.Immediate80, regs[0])
                {
                    Registers = regs,
                    Expressions = [Expression()],
                    StatementIndex = _statementIndex,
                    RightToken = _previous
                };
            }
            Release();
        }
        if (!Check(TokenType.OpenBracket))
        {
            return new Operand(OperandType.Immediate80, regs[0])
            {
                Expressions = [Expression()],
                Registers = regs,
                StatementIndex = _statementIndex,
                RightToken = _previous
            };
        }
        Mark();
        Advance();
        OperandType operandType;
        var i86Indexed = I86Mem();
        if (i86Indexed != null)
        {
            if (!CheckEosOrColon())
            {
                Restore();
                return new Operand(OperandType.Immediate80, regs[0])
                {
                    Expressions = [Expression()],
                    Registers = regs,
                    StatementIndex = _statementIndex,
                    RightToken = _previous
                };
            }
            if (segmentOverride)
            {
                regs.Add(seg);
            }
            var types = s_modRegRmOperands[i86Indexed.Type];
            operandType = segmentOverride ? types.RegisterSegmentOverrideType : types.RegisterType;
            regs.AddRange(i86Indexed.Registers);
            Release();
            return new Operand(operandType, regs[0])
            {
                Registers = regs,
                Expressions = i86Indexed.Expressions.ToList(),
                StatementIndex = _statementIndex,
                RightToken = _previous,
                CoercedSize = ptrSize
            };
        }
        exprs.Add(Expression());
        if (Match(TokenType.CloseBracket) && CheckEosOrColon())
        {
            var possibleTypes = s_modRegRmOperands[OperandType.IndirectLong];
            operandType = segmentOverride ? possibleTypes.RegisterSegmentOverrideType : possibleTypes.RegisterType;
            Release();
            return new Operand(operandType, regs[0])
            {
                CoercedSize = ptrSize,
                Registers = regs,
                Expressions = exprs,
                StatementIndex = _statementIndex,
                RightToken = _previous
            };
        }
        Restore();
        return new Operand(OperandType.Immediate80, regs[0])
        {
            Expressions = [Expression()],
            Registers = regs,
            StatementIndex = _statementIndex,
            RightToken = _previous
        };
    }

    private Operand I86OperandPtrTypeFirst()
    {
        var operandType = OperandType.IndirectLong;
        List<Token> regs = [];
        List<Expression> exprs = [];
        var ptrType = _current;
        var ptrSize = _current.Type.PtrSize();
        Advance();
        Consume(TokenType.Ptr);
        var isSegment = _current.Type.IsI86Segment() && Peek().Type == TokenType.Colon;
        if (isSegment)
        {
            regs.Add(_current);
            Advance();
            Advance();
        }
        if (Check(TokenType.OpenBracket))
        {
            Mark();
            Advance();
            var i86Mod = I86Mem();
            if (i86Mod != null)
            {
                Release();
                operandType = i86Mod.Type;
                regs.AddRange(i86Mod.Registers);
                exprs.AddRange(i86Mod.Expressions);
            }
            else
            {
                var expr = Expression();
                Consume(TokenType.CloseBracket);
                if (CheckEosOrColon() || Check(TokenType.Comma))
                {
                    Release();
                    exprs.Add(expr);
                }
                else
                {
                    Restore();
                    exprs.Add(Expression());
                }
            }
        }
        else
        {
            if (!isSegment)
            {
                // we can't do something like jmp BYTE PTR 0ffd2h
                throw new CompileException(CompileExceptionType.UnexpectedExpression, _current);
            }
            exprs.Add(Expression());
        }
        if (Match(TokenType.Comma))
        {
            DiscardNewlines();
            return GeneralRegOrImmSource(ptrType, isSegment, operandType, regs, exprs);
        }
        // we got here: jmp WORD PTR [bx+si], which is legal
        var types = s_modRegRmOperands[operandType];
        if (isSegment)
        {
            operandType = types.SegmentOverrideType;
        }
        return new Operand(operandType, ptrType)
        {
            CoercedSize = ptrSize,
            Expressions = exprs,
            Registers = regs,
            StatementIndex = _statementIndex,
            RightToken = _previous
        };
    }

    private Operand GeneralRegOrImmSource
    (
        Token ptrType, 
        bool isSegment, 
        OperandType operandType, 
        List<Token> regs, 
        List<Expression> exprs
    )
    {
        var ptrSize = ptrType.Type.PtrSize();
        var possibleTypes = s_modRegRmOperands[operandType];
        if (IsPtr() && _current.Type != TokenType.Dword)
        {
            var ptr2Type = _current;
            if (ptrSize == 0)
            {
                ptrSize = _current.Type.PtrSize();
                Advance();
            }
            else
            {
                Consume(ptrSize == 1 ? TokenType.Byte : TokenType.Word);
            }
            Consume(TokenType.Ptr);
            if (_current.Type.IsI86Segment() && Peek().Type == TokenType.Colon)
            {
                regs.Add(_current);
                Advance();
                Advance();
                if (operandType is OperandType.IndirectRegister86 && Check(TokenType.OpenBracket))
                {
                    Consume(TokenType.OpenBracket);
                    var i86Mod = I86Mem();
                    if (i86Mod?.Type != OperandType.IndirectRegister86)
                    {
                        throw new CompileException(CompileExceptionType.ExpectedExpression, ptr2Type);
                    }
                    operandType = OperandType.SegmentOverrideRegisterSegmentOverrideRegister;
                    regs.AddRange(i86Mod.Registers);
                }
                else
                {
                    // allow constructs like mov [bx+si], WORD PTR 0xffd2
                    exprs.Add(Expression());
                    operandType = possibleTypes.SegmentOverrideTypeImm;
                }
            }
            else
            {
                exprs.Add(Expression());
                operandType = possibleTypes.SegmentOverrideTypeImm;
            }
        }
        else if (ptrSize > 0 && 
                 operandType == OperandType.IndirectRegister86 && 
                 _current.Type.IsI86Segment() && 
                 Peek().Type == TokenType.Colon)
        {
            regs.Add(_current);
            Advance();
            Advance();
            var i86Mod = I86Mem();
            if (i86Mod?.Type != OperandType.IndirectRegister86)
            {
                throw new CompileException(CompileExceptionType.ExpectedExpression, ptrType);
            }
            operandType = OperandType.SegmentOverrideRegisterSegmentOverrideRegister;
            regs.AddRange(i86Mod.Registers);
        }
        else if (_current.Type.IsRegister())
        {
            regs.Add(_current);
            Advance();
            operandType = isSegment ? possibleTypes.SegmentOverrideTypeRegister : possibleTypes.TypeRegister;
        }
        else
        {
            if (ptrSize == 0)
            {
                throw new CompileException(CompileExceptionType.UnexpectedExpression, _current);
            }
            exprs.Add(Expression());
            operandType = isSegment 
                ? possibleTypes.SegmentOverrideTypeImm 
                : possibleTypes.TypeImm;
        }
        return new Operand(operandType, ptrType)
        {
            CoercedSize = ptrSize,
            Registers = regs,
            Expressions = exprs,
            StatementIndex = _statementIndex,
            RightToken = _previous
        };
    }
    
    private Operand I86OperandSegmentFirst()
    {
        var segment = _current;
        List<Token> regs = [_current];
        var exprs = new List<Expression>();
        Mark();
        Advance();
        Advance();
        Operand? i86Mod = null;
        if (Match(TokenType.OpenBracket))
        {
            i86Mod = I86Mem();
            if (i86Mod == null)
            {
                Restore();
            }
            else
            {
                Release();
                regs.AddRange(i86Mod.Registers);
                exprs.AddRange(i86Mod.Expressions);
            }
        }
        else
        {
            Release();
        }
        if (i86Mod == null)
        {
            exprs.Add(Expression());
            if (CheckEos())
            {
                return new Operand(OperandType.SegmentOverrideDirect, segment)
                {
                    Expressions = exprs,
                    StatementIndex = _statementIndex,
                    RightToken = _previous
                };
            }
        }
        if (CheckEosOrColon())
        {
            var operType = s_modRegRmOperands[i86Mod?.Type ?? OperandType.IndirectLong];
            return new Operand(operType.SegmentOverrideType, segment)
            {
                Expressions = exprs,
                Registers = regs,
                StatementIndex = _statementIndex,
                RightToken = _previous
            };
        }
        Consume(TokenType.Comma);
        DiscardNewlines();
        return GeneralRegOrImmSource(segment, true, i86Mod?.Type ?? OperandType.IndirectLong, regs, exprs);
    }
    
    private Operand I86IndirectOperand()
    {
        var leftBracket = _current;
        Advance();
        List<Token> regs = [];
        List<Expression> exprs = [];
        OperandType operandType;

        var i86Index = I86Mem();
        if (i86Index != null)
        {
            regs = i86Index.Registers.ToList();
            exprs = i86Index.Expressions.ToList();
            operandType = i86Index.Type;
        }
        else
        {
            operandType = OperandType.IndirectLong;
            exprs.Add(Expression());
            Consume(TokenType.CloseBracket);
        }
        if (!Match(TokenType.Comma))
        {
            return new Operand(operandType, leftBracket)
            {
                Expressions = exprs,
                Registers = regs,
                StatementIndex = _statementIndex,
                RightToken = _previous
            };
        }
        DiscardNewlines();
        return GeneralRegOrImmSource(leftBracket, false, operandType, regs, exprs);
    }

    private Operand? I86Mem()
    {
        Token? baseReg = null, indexReg = null;
        Expression? displ = null;
        var firstToken = _current;
        Mark();
        var isEa = true;
        while (isEa)
        {
            if (_current.Type.IsI86Base())
            {
                isEa &= baseReg == null;
                baseReg = _current;
                Advance();
            }
            else if (_current.Type.IsI86Index())
            {
                isEa &= indexReg == null;
                indexReg = _current;
                Advance();
            }
            else
            {
                isEa &= displ == null;
                displ = ParsePrecedence(this, Precedence.Multiplication);
            }
            if (Check(TokenType.Plus))
            {
                isEa &= baseReg == null || indexReg == null || displ == null;
                var peekType = Peek().Type;
                if (peekType.IsI86Base() || peekType.IsI86Index())
                {
                    Advance();
                }
            }
            else if (Check(TokenType.Minus))
            {
                isEa &= displ == null;
            }
            else
            {
                break;
            }
        }
        isEa &= Match(TokenType.CloseBracket) && (Check(TokenType.Comma) || CheckEosOrColon());
        if (!isEa)
        {
            Restore();
            return null;
        }
        Release();
        var exprs = new List<Expression>();
        List<Token> regs = [];

        if (baseReg != null)
        {
            regs.Add(baseReg.Value);
        }
        if (indexReg != null)
        {
            regs.Add(indexReg.Value);
        }
        if (displ != null)
        {
            exprs.Add(displ);
        }
        var operandType = regs.Count switch
        {
            0 => OperandType.IndirectLong,
            1 => exprs.Count switch
            {
                0 => OperandType.IndirectRegister86,
                _ => OperandType.BaseDisplacement
            },
            _ => exprs.Count switch
            {
                0 => OperandType.BaseIndex,
                _ => OperandType.BaseIndexDisplacement
            }
        };
        return new Operand(operandType, firstToken)
        {
            Registers = regs,
            Expressions = exprs,
            StatementIndex = _statementIndex,
            RightToken = _previous
        };
    }

    private bool IsPtr()
        => _current.Type.IsI86PtrType() && Peek().Type == TokenType.Ptr;
}