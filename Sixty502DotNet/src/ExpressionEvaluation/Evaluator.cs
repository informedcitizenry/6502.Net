//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
namespace Sixty502DotNet
{
    /// <summary>
    /// A static class that evaluates parsed complex expressions.
    /// </summary>
    public static class Evaluator
    {
        private static readonly BinaryConverter s_bin = new();
        private static readonly BinaryDoubleConverter s_binDouble = new();
        private static readonly CharConverter s_char = new();
        private static readonly HexConverter s_hex = new();
        private static readonly HexDoubleConverter s_hexDouble = new();
        private static readonly NumberConverter s_num = new();
        private static readonly OctalDoubleConverter s_octDouble = new();
        private static readonly StringConverter s_str = new();
        private static readonly BooleanConverter s_bool = new();
        private static readonly int[] s_compoundAssignments =
        {
            Sixty502DotNetParser.CaretEqual, Sixty502DotNetParser.AmpersandEqual,
            Sixty502DotNetParser.HyphenEqual, Sixty502DotNetParser.LeftShiftEqual,
            Sixty502DotNetParser.SolidusEqual, Sixty502DotNetParser.RightShiftEqual,
            Sixty502DotNetParser.PlusEqual,  Sixty502DotNetParser.PercentEqual,
            Sixty502DotNetParser.PipeEqual,  Sixty502DotNetParser.AsteriskEq,
            Sixty502DotNetParser.RightSignShiftEq
        };

        private static readonly Dictionary<int, int> s_compoundsToAssign =
            new()
        {
            { Sixty502DotNetParser.CaretEqual,      Sixty502DotNetParser.Caret },
            { Sixty502DotNetParser.HyphenEqual,      Sixty502DotNetParser.Hyphen },
            { Sixty502DotNetParser.SolidusEqual,      Sixty502DotNetParser.Solidus },
            { Sixty502DotNetParser.PlusEqual,       Sixty502DotNetParser.Plus },
            { Sixty502DotNetParser.PipeEqual,       Sixty502DotNetParser.Pipe },
            { Sixty502DotNetParser.AsteriskEq,      Sixty502DotNetParser.Asterisk },
            { Sixty502DotNetParser.AmpersandEqual,  Sixty502DotNetParser.Ampersand },
            { Sixty502DotNetParser.LeftShiftEqual,  Sixty502DotNetParser.LeftShift },
            { Sixty502DotNetParser.RightShiftEqual, Sixty502DotNetParser.RightShiftEqual },
            { Sixty502DotNetParser.RightSignShiftEq,Sixty502DotNetParser.RightSignShift },
            { Sixty502DotNetParser.PercentEqual,    Sixty502DotNetParser.Percent }

        };

        private static readonly HashSet<int> s_bitWiseOps = new()
        {
            Sixty502DotNetParser.LeftShift,
            Sixty502DotNetParser.RightShift,
            Sixty502DotNetParser.RightSignShift,
            Sixty502DotNetParser.Ampersand,
            Sixty502DotNetParser.Caret,
            Sixty502DotNetParser.Pipe
        };

        private static Value RightSignShift(Value lhs, Value rhs)
        {
            var val = lhs.ToInt();
            var valSign = val >= 0 ? 1 : -1;
            return new Value((Math.Abs(val) >> rhs.ToInt()) * valSign);
        }

        /// <summary>
        /// Determines if the type of operator is a compound assignment, for
        /// example <code>+=</code>.
        /// </summary>
        /// <param name="type">The operator type.</param>
        /// <returns><c>true</c> if the type is a compound assignment,
        /// <c>false</c> otherwise.</returns>
        public static bool IsCompoundAssignment(int type)
            => s_compoundAssignments.Contains(type);

        /// <summary>
        /// Converts a compound assignment type operator into its correspoding
        /// regular assignment type.
        /// </summary>
        /// <param name="type">The operator type.</param>
        /// <returns>The assignment operator type.</returns>
        public static int CompoundToAssign(int type)
            => s_compoundsToAssign[type];

        /// <summary>
        /// Determines if the <see cref="IValue"/> is a condition.
        /// </summary>
        /// <param name="cond">The <see cref="IValue"/> to test.</param>
        /// <returns><c>true</c> if the value is a condition or is
        /// undefined, <c>false</c> otherwise.</returns>
        public static bool IsCondition(Value cond)
        {
            if (cond.DotNetType != TypeCode.Boolean)
            {
                if (cond.IsDefined)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resolve a symbol in a given scope from the parsed
        /// <see cref="Sixty502DotNetParser.IdentifierContext"/>.
        /// </summary>
        /// <param name="primaryScope">The primary <see cref="IScope"/> in which to
        /// resolve the symbol.</param>
        /// <param name="alternateScopes">The alternate scopes to resolve the
        /// symbol if not found in the primary scope..</param>
        /// <param name="context">The parsed identifier representing the
        /// symbol name.</param>
        /// <returns>A <see cref="SymbolBase"/> object, if the identifier was able
        /// to be resolved, otherwise <c>null</c>.</returns>
        public static SymbolBase? ResolveIdentifierSymbol(IScope primaryScope, 
                                                          IEnumerable<IScope>? alternateScopes,
                                                          Sixty502DotNetParser.IdentifierContext context)
        {
            if (context.name != null)
            {
                var resolved = primaryScope.Resolve(context.name.Text);
                if (resolved == null && alternateScopes != null)
                {
                    foreach (var scope in alternateScopes)
                    {
                        if ((resolved = ResolveIdentifierSymbol(scope, null, context)) != null)
                        {
                            break;
                        }
                    }
                }
                return resolved;
            }
            if (context.lhs != null)
            {
                var lhs = ResolveIdentifierSymbol(primaryScope, alternateScopes, context.lhs);
                if (lhs is NamedMemberSymbol namedMemberSymbol)
                {
                    return namedMemberSymbol.ResolveMember(context.rhs.Start.Text);
                }
            }
            return ResolveIdentifierSymbol(primaryScope, alternateScopes, context.identifier()[0]);
        }

        /// <summary>
        /// Convert a <see cref="Sixty502DotNetParser.ExprContext"/> to a
        /// binary number value.
        /// </summary>
        /// <param name="context">The <see cref="Sixty502DotNetParser.ExprContext"/>.</param>
        /// <returns>An <see cref="IValue"/> representing the binary number
        /// value.</returns>
        public static Value BinaryNumber(Sixty502DotNetParser.ExprContext context)
        {
            if (context.BinaryDigitsDouble() != null)
            {
                var binDouble = context.BinaryDigitsDouble().GetText();
                return s_binDouble.Convert($"%{binDouble}");
            }
            var bin = context.BinaryDigits().GetText();
            return s_bin.Convert($"%{bin}");
        }

        /// <summary>
        /// Perform a prefix unary operation.
        /// </summary>
        /// <param name="op">The unary operator type.</param>
        /// <param name="rhs">The right-hand side value.</param>
        /// <returns>The evaluation as a <see cref="IValue"/>.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Value UnaryOp(int op, Value rhs)
        {
            if (rhs.IsNumeric)
            {
                if (op == Sixty502DotNetParser.Tilde || op == Sixty502DotNetParser.Hyphen)
                {
                    if (rhs.IsIntegral)
                    {
                        return op == Sixty502DotNetParser.Tilde ? new Value(~rhs.ToInt()) : new Value(-rhs.ToInt());
                    }
                    return op == Sixty502DotNetParser.Tilde ?
                        new Value(Math.Floor(Math.Abs(rhs.ToDouble()))) :
                        new Value(-rhs.ToDouble());
                }
                return op switch
                {
                    Sixty502DotNetParser.Plus       => rhs,
                    Sixty502DotNetParser.LeftAngle  => new Value(rhs.ToInt()  & 0xFF),
                    Sixty502DotNetParser.RightAngle => new Value((rhs.ToInt() / 0x100) & 0xFF),
                    Sixty502DotNetParser.Ampersand  => new Value(rhs.ToInt()  & 0xFFFF),
                    Sixty502DotNetParser.Caret      => new Value(rhs.ToInt()  / 0x10000 & 0xFF),
                    _                               => throw new InvalidOperationException(Errors.InvalidOperation)
                };
            }
            if (rhs.DotNetType == TypeCode.Boolean && op == Sixty502DotNetParser.Bang)
            {
                return new Value(!rhs.ToBool());
            }
            throw new InvalidOperationException(Errors.InvalidOperation);
        }

        /// <summary>
        /// Perform an identity equality expression.
        /// </summary>
        /// <param name="primaryScope">The scope from which to resolve the identifiers
        /// in the left-hand and right-hand sides of the expression.</param>
        /// <param name="lhs">The left-hand side expression.</param>
        /// <param name="rhs">The right-hand side expression.</param>
        /// <returns><c>true</c> if both the left-hand and right-hand side
        /// expressions are both identifiers of the type
        /// <see cref="IValueResolver"/> and one of the value resolvers is a
        /// reference to the other, otherwise <c>false</c>.</returns>
        public static Value IsIdentical(IScope primaryScope,
                                        IEnumerable<IScope> alternateScopes,
                                        Sixty502DotNetParser.ExprContext lhs,
                                        Sixty502DotNetParser.ExprContext rhs)
        {
            if (lhs.refExpr()?.identifier() != null && rhs.refExpr()?.identifier() != null)
            {
                var lhsResolver = ResolveIdentifierSymbol(primaryScope, alternateScopes, lhs.refExpr().identifier()) as IValueResolver;
                var rhsResolver = ResolveIdentifierSymbol(primaryScope, alternateScopes, rhs.refExpr().identifier()) as IValueResolver;
                return new Value(lhsResolver != null && rhsResolver != null &&
                    (ReferenceEquals(lhsResolver.IsAReferenceTo, rhsResolver) ||
                    ReferenceEquals(rhsResolver.IsAReferenceTo, lhsResolver)));
            }
            return new Value(false);
        }

        /// <summary>
        /// Perform an infix operation.
        /// </summary>
        /// <param name="lhs">The left-hand side value.</param>
        /// <param name="op">The operator type.</param>
        /// <param name="rhs">The right-hand side value.</param>
        /// <returns>The evaluation as a <see cref="IValue"/>.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Value BinaryOp(Value lhs, int op, Value rhs)
        {
            var bothNumeric = (lhs.IsNumeric && rhs.IsNumeric) ||
                              (lhs.DotNetType == TypeCode.Char && rhs.DotNetType == TypeCode.Char);
            if (bothNumeric)
            {
                if (s_bitWiseOps.Contains(op))
                {
                    return op switch
                    {
                        Sixty502DotNetParser.LeftShift      => new Value(lhs.ToInt() << rhs.ToInt()),
                        Sixty502DotNetParser.RightShift     => new Value(lhs.ToInt() >> rhs.ToInt()),
                        Sixty502DotNetParser.RightSignShift => RightSignShift(lhs, rhs),
                        Sixty502DotNetParser.Ampersand      => new Value(lhs.ToInt() & rhs.ToInt()),
                        Sixty502DotNetParser.Caret          => new Value(lhs.ToInt() ^ rhs.ToInt()),
                        _                                   => new Value(lhs.ToInt() | rhs.ToInt())
                    };
                }
                if ((lhs.IsIntegral && rhs.IsIntegral) || lhs.DotNetType == TypeCode.Char)
                {
                    var lhsInt = lhs.DotNetType == TypeCode.Char ? lhs.ToString(true)[0] : lhs.ToInt();
                    var rhsInt = lhs.DotNetType == TypeCode.Char ? rhs.ToString(true)[0] : rhs.ToInt();
                    return op switch
                    {
                        Sixty502DotNetParser.Asterisk       => new Value(lhsInt * rhsInt),
                        Sixty502DotNetParser.Solidus          => new Value(lhsInt / rhsInt),
                        Sixty502DotNetParser.Percent        => new Value(lhsInt % rhsInt),
                        Sixty502DotNetParser.Plus           => new Value(lhsInt + rhsInt),
                        Sixty502DotNetParser.Hyphen          => new Value(lhsInt - rhsInt),  
                        Sixty502DotNetParser.LeftAngle      => new Value(lhsInt < rhsInt),
                        Sixty502DotNetParser.LTE            => new Value(lhsInt <= rhsInt),
                        Sixty502DotNetParser.GTE            => new Value(lhsInt >= rhsInt),
                        Sixty502DotNetParser.Spaceship      => new Value(Math.Sign(lhsInt - rhsInt)),
                        Sixty502DotNetParser.DoubleEqual    => new Value(lhsInt == rhsInt),
                        Sixty502DotNetParser.BangEqual      => new Value(lhsInt != rhsInt),
                        Sixty502DotNetParser.RightAngle     => new Value(lhsInt > rhsInt),
                        Sixty502DotNetParser.DoubleCaret    => new Value((int)Math.Pow(lhsInt, rhsInt)),
                        _                                   => throw new InvalidOperationException(Errors.InvalidOperation)
                    };
                }
                var lhsDouble = lhs.DotNetType == TypeCode.Char ? lhs.ToString(true)[0] * 1.0 : lhs.ToDouble();
                var rhsDouble = rhs.DotNetType == TypeCode.Char ? rhs.ToString(true)[0] * 1.0 : rhs.ToDouble();
                return op switch
                {
                    Sixty502DotNetParser.Asterisk           => new Value(lhsDouble * rhsDouble),
                    Sixty502DotNetParser.Solidus              => new Value(lhsDouble / rhsDouble),
                    Sixty502DotNetParser.Percent            => new Value(lhsDouble % rhsDouble),
                    Sixty502DotNetParser.Plus               => new Value(lhsDouble + rhsDouble),
                    Sixty502DotNetParser.Hyphen              => new Value(lhsDouble - rhsDouble),
                    Sixty502DotNetParser.LeftAngle          => new Value(lhsDouble < rhsDouble),
                    Sixty502DotNetParser.LTE                => new Value(lhsDouble <= rhsDouble),
                    Sixty502DotNetParser.GTE                => new Value(lhsDouble >= rhsDouble),
                    Sixty502DotNetParser.Spaceship          => new Value(Math.Sign(lhsDouble - rhsDouble)),
                    Sixty502DotNetParser.DoubleEqual        => new Value(lhsDouble == rhsDouble),
                    Sixty502DotNetParser.BangEqual          => new Value(lhsDouble != rhsDouble),
                    Sixty502DotNetParser.RightAngle         => new Value(lhsDouble > rhsDouble),
                    Sixty502DotNetParser.DoubleCaret        => new Value(Math.Pow(lhsDouble, rhsDouble)),
                    _                                       => throw new InvalidOperationException(Errors.InvalidOperation)
                };
            }
            else if (lhs.IsString && rhs.IsString)
            {
                return op switch
                {
                    Sixty502DotNetParser.Plus               => new Value($"\"{lhs.ToString(true)}{rhs.ToString(true)}\""),
                    Sixty502DotNetParser.DoubleEqual        => new Value(lhs.ToString(false).Equals(rhs.ToString(false))),
                    Sixty502DotNetParser.BangEqual          => new Value(!lhs.ToString(false).Equals(rhs.ToString(false))),
                    _                                       => throw new InvalidOperationException(Errors.InvalidOperation)
                };
            }
            else if (op == Sixty502DotNetParser.Plus &&
                    (lhs.DotNetType == TypeCode.Char || lhs.IsString) &&
                    (rhs.DotNetType == TypeCode.Char || rhs.IsString))
            {
                return new Value($"\"{lhs.ToString(true)}{rhs.ToString(true)}\"");
            }
            else if (lhs.DotNetType == TypeCode.Boolean && rhs.DotNetType == TypeCode.Boolean)
            {
                return op switch
                {
                    Sixty502DotNetParser.DoubleEqual        => new Value(lhs.ToBool() == rhs.ToBool()),
                    Sixty502DotNetParser.BangEqual          => new Value(lhs.ToBool() != rhs.ToBool()),
                    Sixty502DotNetParser.DoubleAmpersand    => new Value(lhs.ToBool() && rhs.ToBool()),
                    Sixty502DotNetParser.DoublePipe         => new Value(lhs.ToBool() || rhs.ToBool()),
                    _                                       => throw new InvalidOperationException(Errors.InvalidOperation)
                };
            }
            throw new InvalidOperationException(Errors.InvalidOperation);
        }

        /// <summary>
        /// Perform a conditional (ternary) operation.
        /// </summary>
        /// <param name="cond">The condition.</param>
        /// <param name="then">The result if the condition is <c>true</c>.</param>
        /// <param name="els">The result if the condition is <c>false</c>.</param>
        /// <returns>Either the <paramref name="then"/> or <paramref name="els"/>
        /// parameter.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Value CondOp(Value cond, Value then, Value els)
        {
            if (cond.DotNetType == TypeCode.Boolean)
            {
                return cond.ToBool() ? then : els;
            }
            throw new InvalidOperationException(Errors.TypeMismatchError);
        }

        /// <summary>
        /// Convert a <see cref="double"/> to an <see cref="int"/> or
        /// <see cref="uint"/> if the converted value is able to be converted.
        /// Otherwise, the returned value is the original value itself.
        /// </summary>
        /// <param name="value">The <see cref="double"/> as an <see cref="IValue"/>.
        /// </param>
        /// <returns>The converted <see cref="int"/> or <see cref="uint"/> as an
        /// <see cref="Value"/> if conversion was successful, otherwise the
        /// original value itself.</returns>
        public static Value ConvertToIntegral(Value value)
        {
            if (value.ToDouble() >= int.MinValue && value.ToDouble() <= uint.MaxValue)
            {
                if (value.ToDouble() <= int.MaxValue)
                {
                    return new Value(unchecked((int)(value.ToLong() & 0xFFFF_FFFF)));
                }
                return new Value((uint)(value.ToLong() & 0xFFFF_FFFF));
            }
            return value;
        }

        private static bool ExpressionContainsPC(Sixty502DotNetParser.ExprContext context)
        {
            if (context.refExpr()?.programCounter() != null)
            {
                return true;
            }
            var exprs = context.expr();
            for(var i = 0; i < exprs.Length; ++i)
            {
                if (exprs[i].refExpr()?.programCounter() != null ||
                    ExpressionContainsPC(exprs[i]))
                {
                    return true;
                }
            }
            if (context.assignExpr() != null)
            {
                return ExpressionContainsPC(context.assignExpr().expr());
            }
            return false;
        }

        // $hex_val = hex
        // $hex_val + $hex_val = hex
        // $hex_val + int = hex
        // $hex_val + double != hex
        // int + int != hex
        // etc.
        private static bool OperandsAreBinHexAndIntegral(Sixty502DotNetParser.ExprContext context)
        {
            if (context.rhs != null)
            {
                var rhsIsBinHex = IsBinHexValue(context.rhs);
                var lhsIsBinHex = true;
                if (context.lhs != null)
                {
                    lhsIsBinHex = IsBinHexValue(context.lhs);
                }
                if (lhsIsBinHex)
                {
                    return rhsIsBinHex || GetPrimaryExpression(context.rhs).IsIntegral;
                }
                return rhsIsBinHex && (context.lhs == null || GetPrimaryExpression(context.lhs).IsIntegral);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the parsed expression represents a binary/hexadecimal
        /// value.
        /// </summary>
        /// <param name="context">The <see cref="Sixty502DotNetParser.ExprContext"/>.</param>
        /// <returns><c>true</c> if the expression represents a binary/hexadecimal
        /// value, <c>false</c> otherwise.</returns>
        public static bool IsBinHexValue(Sixty502DotNetParser.ExprContext context)
        {
            if (context.primaryExpr() == null)
            {
                if (context.op?.Type == Sixty502DotNetParser.Percent && context.BinaryDigits() != null)
                {
                    return true;
                }
                return ExpressionContainsPC(context) || OperandsAreBinHexAndIntegral(context);   
            }
            return context.primaryExpr().Hexadecimal() != null ||
                   context.primaryExpr().BinaryLiteral() != null ||
                   context.primaryExpr().Octal() != null ||
                   context.primaryExpr().AltBinary() != null;
        }

        /// <summary>
        /// Evaluate the parsed expression as a primary expression.
        /// </summary>
        /// <param name="context">The <see cref="Sixty502DotNetParser.PrimaryExprContext"/>.</param>
        /// <returns>An <see cref="IValue"/> representing the evaluated
        /// primary expression.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Value GetPrimaryExpression(Sixty502DotNetParser.PrimaryExprContext context)
            => GetPrimaryExpression(context.constExpr);

        /// <summary>
        /// Evaluate the parsed expression as a primary expression.
        /// </summary>
        /// <param name="context">The <see cref="Sixty502DotNetParser.ExprContext"/>.</param>
        /// <returns>An <see cref="IValue"/> representing the evaluated
        /// primary expression.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Value GetPrimaryExpression(Sixty502DotNetParser.ExprContext context)
        {
            if (context.op != null)
            {
                if (context.rhs != null)
                {
                    var rhs = GetPrimaryExpression(context.rhs);
                    if (rhs.IsDefined)
                    {
                        if (context.lhs != null)
                        {
                            var lhs = GetPrimaryExpression(context.lhs);
                            if (lhs.IsDefined)
                            {
                                return BinaryOp(lhs, context.op.Type, rhs);
                            }
                        }
                        return UnaryOp(context.op.Type, rhs);
                    }
                    return Value.Undefined();
                }
                if (context.cond != null)
                {
                    var cond = GetPrimaryExpression(context.cond);
                    var then = GetPrimaryExpression(context.then);
                    var els = GetPrimaryExpression(context.els);
                    if (cond.IsDefined && then.IsDefined && els.IsDefined)
                    {
                        return CondOp(cond, then, els);
                    }
                }
                if (context.op.Type == Sixty502DotNetParser.Percent)
                {
                    return BinaryNumber(context);
                }
            }
            if (context.lparen != null)
            {
                return GetPrimaryExpression(context.expr()[0]);
            }
            if (context.primaryExpr() != null)
            {
                return GetPrimaryExpression(context.primaryExpr());
            }
            return Value.Undefined();
        }

        /// <summary>
        /// Evaluate the <see cref="IToken"/> as a primary expression (a term).
        /// </summary>
        /// <param name="token">The <see cref="IToken"/> to parse.</param>
        /// <returns>The primary expression as a <see cref="IValue"/>.</returns>
        public static Value GetPrimaryExpression(IToken token)
        {
            ICustomConverter converter = token.Type switch
            {
                Sixty502DotNetParser.Hexadecimal        => s_hex,
                Sixty502DotNetParser.HexadecimalDouble  => s_hexDouble,
                Sixty502DotNetParser.Double        or
                Sixty502DotNetParser.Integer       or
                Sixty502DotNetParser.BinaryDigits  or
                Sixty502DotNetParser.Octal              => s_num,
                Sixty502DotNetParser.OctalDouble        => s_octDouble,
                Sixty502DotNetParser.BinaryLiteral or
                Sixty502DotNetParser.AltBinary          => s_bin,
                Sixty502DotNetParser.BinaryLiteralDouble=> s_binDouble,
                Sixty502DotNetParser.StringLiteral      => s_str,
                Sixty502DotNetParser.CharLiteral        => s_char,
                _                                       => s_bool,
            };
            return converter.Convert(token.Text);
        }
    }
}
