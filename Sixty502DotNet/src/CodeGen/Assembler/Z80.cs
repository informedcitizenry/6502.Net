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
using Antlr4.Runtime.Tree;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class responsible for assembly of i8080 and z80 source.
    /// </summary>
    public partial class Z80 : InstructionSet
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Z80"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="initialArchitecture">The initial CPU/architecture
        /// to initialize the instruction set mnemonics.</param>
        public Z80(AssemblyServices services, string initialArchitecture)
            : base(services, initialArchitecture)
        {
            Set = initialArchitecture.StartsWith('z') ?
                new Dictionary<Instruction, Opcode>(s_z80) :
                new Dictionary<Instruction, Opcode>(s_i8080);
        }

        private double GetIxOffs(Sixty502DotNetParser.Z80IxOffsetContext context)
        {
            if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr(), -byte.MaxValue, byte.MaxValue, out var offs)
                && offs.IsInteger())
            {
                var intOffs = context.sign.Type == Sixty502DotNetParser.Hyphen ? -offs : offs;
                if (intOffs >= sbyte.MinValue && intOffs <= byte.MaxValue)
                {
                    return (int)intOffs & 0xFF;
                }
            }
            Services.Log.LogEntry(context.expr(), "Offset value was too far.");
            return int.MinValue;
        }

        // z80Rel
        //    :   'jr' ('c'|'nc'|'nz'|'z' ',')? expr
        //    |   'djnz' expr
        //    ;
        private bool GenRel(Sixty502DotNetParser.Z80RelContext context)
        {
            Instruction instr;
            if (context.JR() != null)
            {
                if (context.flag != null)
                {
                    instr = new Instruction(Sixty502DotNetParser.JR, GetFlag(context.flag) | N161);
                }
                else
                {
                    instr = new Instruction(Sixty502DotNetParser.JR, N160);
                }
            }
            else
            {
                instr = new Instruction(Sixty502DotNetParser.DJNZ, N160);
            }
            var opc = Set[instr];
            if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr(), short.MinValue, ushort.MaxValue, out var rel))
            {
                var offs = Services.Output.GetRelativeOffset((int)rel, 1);
                if (offs >= sbyte.MinValue && offs <= sbyte.MaxValue)
                {
                    Services.Output.Add(opc.code, opc.code.Size());
                    Services.Output.Add(offs & 0xFF, 1);
                    if (context.flag != null)
                    {
                        BlockVisitor.GenLineListing(Services,
                        $"{context.Start.Text.ToLower()} {context.flag.Text.ToLower()},${(int)rel & 0xffff:x4}");
                    }
                    else
                    {
                        BlockVisitor.GenLineListing(Services,
                        $"{context.Start.Text.ToLower()} ${(int)rel & 0xffff:x4}");
                    }
                }
                else if (Services.State.PassNeeded)
                {
                    Services.Output.AddUninitialized(2);
                }
                else
                {
                    Services.Log.LogEntry(context.expr(), $"Relative branch too far ({Math.Abs(offs)} bytes).");
                }
                return true;
            }
            if (Services.State.PassNeeded)
            {
                Services.Output.AddUninitialized(2);
            }
            return Services.State.PassNeeded;
        }

        //z80Bit  
        //   :   z80BitMnemonic expr ',' z80IxOffset (',' z80Reg)?
        //   |   z80BitMnemonic expr ',' (z80Reg | (LeftParen HL RightParen))
        //   ;
        private bool GenBit(Sixty502DotNetParser.CpuStatContext context)
        {
            var mnemonic = context.z80Bit().z80BitMnemonic();
            var bit = context.z80Bit().expr().GetText();
            var reg = context.z80Bit().z80Reg();
            var ix = context.z80Bit().z80IxOffset();
            if (bit.Length != 1 || bit[0] < '0' || bit[0] > '7')
            {
                Services.Log.LogEntry(context.z80Bit().expr(), "Bit value must be an integer between 0 and 7.");
                return false;
            }
            var disasm = $"{context.Start.Text.ToLower()} {bit}";
            var opcmsb = Convert.ToInt32(bit) * 8;
            if (reg != null)
            {
                try
                {
                    opcmsb += reg.Start.Type switch
                    {
                        Sixty502DotNetParser.A => 7,
                        Sixty502DotNetParser.B => 0,
                        Sixty502DotNetParser.C => 1,
                        Sixty502DotNetParser.D => 2,
                        Sixty502DotNetParser.E => 3,
                        Sixty502DotNetParser.H => 4,
                        Sixty502DotNetParser.L => 5,
                        _ => throw new Error(Errors.ModeNotSupported)
                    };
                }
                catch (Error)
                {
                    return false;
                }
            }
            else
            {
                opcmsb += 6;
            }
            opcmsb = mnemonic.Start.Type switch
            {
                Sixty502DotNetParser.BIT => (0x40 + opcmsb) * 0x100,
                Sixty502DotNetParser.RES => (0x80 + opcmsb) * 0x100,
                _ => (0xc0 + opcmsb) * 0x100
            };
            var opcode = opcmsb;
            if (ix != null)
            {
                var offs = (int)GetIxOffs(ix);
                if (offs == int.MinValue)
                {
                    return false;
                }
                opcode = ((((opcmsb + offs) * 0x100) | 0xcb) * 0x100) | (ix.IX() != null ? 0xdd : 0xfd);
                disasm += $",({ix.ixReg.Text.ToLower()}+${offs & 0xff:x2})";
            }
            else
            {
                opcode |= 0xcb;
            }
            if (reg != null)
            {
                disasm += $",{reg.GetText().ToLower()}";
            }
            else if (context.z80Bit().LeftParen() != null)
            {
                disasm += ",(hl)";
            }
            Services.Output.Add(opcode, opcode.Size());
            BlockVisitor.GenLineListing(Services, disasm);
            return true;
        }

        // z80IMRST: ('im'|'rst') expr ;
        private bool GenImRst(Sixty502DotNetParser.Z80ImRstContext context)
        {
            if (!Services.ExpressionVisitor.TryGetPrimaryExpression(context.expr(), out var rst))
            {
                return false;
            }
            if (rst.IsIntegral && rst.ToInt() >= 0x00 && rst.ToInt() <= 0x38)
            {
                if (context.mnem.Type == Sixty502DotNetParser.IM && rst.ToInt() <= 2)
                {
                    var opcode = rst.ToInt() switch
                    {
                        0 => 0x46ed,
                        1 => 0x4eed,
                        _ => 0x5eed
                    };
                    Services.Output.Add(opcode, 2);
                    BlockVisitor.GenLineListing(Services, $"im {rst.ToInt()}");
                    return true;
                }
                else if (context.mnem.Type == Sixty502DotNetParser.RST && rst.ToInt() % 8 == 0)
                {
                    Services.Output.Add(0xc7 + rst.ToInt(), 1);
                    BlockVisitor.GenLineListing(Services, $"rst ${rst.ToInt():x2}");
                    return true;
                }
            }
            if (rst.IsIntegral)
            {
                Services.Log.LogEntry(context, Errors.IllegalQuantity);
            }
            else
            {
                Services.Log.LogEntry(context, Errors.TypeMismatchError);
            }
            return false;
        }

        private static int N8ToN16(int mode)
        {
            if (mode < 128)
            {
                return (mode & ~N80) | N160;
            }
            return (mode & ~N81) | N161;
        }

        private static bool GetModSize(int mode)
            => mode == N80 || mode == IndN80 ||
               mode == N81 || mode == IndN81;

        private int GetExprMode(Sixty502DotNetParser.ExprContext context,
                                ITerminalNode? leftParen,
                                int placement,
                                out double exprVal)
        {
            int mode = N80;
            var val = Services.ExpressionVisitor.Visit(context);
            if (!val.IsDefined)
            {
                if (Services.State.PassNeeded)
                {
                    exprVal = int.MaxValue;
                    return mode;
                }
            };
            if (val.IsString || val.DotNetType == TypeCode.Char)
            {
                exprVal = Services.Encoding.GetEncodedValue(val.ToString(true));
            }
            else if (!val.IsNumeric)
            {
                exprVal = double.NaN;
                Services.Log.LogEntry(context, Errors.TypeMismatchError);
                return int.MinValue;
            }
            else
            {
                exprVal = val.ToDouble();
            }
            if (exprVal < sbyte.MinValue || exprVal > byte.MaxValue)
            {
                if (exprVal < short.MinValue || exprVal > ushort.MaxValue)
                {
                    return int.MaxValue;
                }
                mode = N160;
            }
            if (leftParen != null)
            {
                mode |= 64;
            }
            if (placement == 1)
            {
                return mode * 0b10000000;
            }
            return mode;
        }

        private static int GetRegMode(Sixty502DotNetParser.Z80RegContext? context, IToken? leftParen, int placement)
        {
            if (context == null)
            {
                return 0;
            }
            var regBase = s_parserRegToReg[context.Start.Type];
            if (leftParen != null)
            {
                if (s_reg_to_ind_reg.TryGetValue(regBase, out var indReg))
                {
                    regBase = indReg;
                }
                else
                {
                    regBase |= 64;
                }
            }
            if (placement == 1)
            {
                return regBase * 0b10000000;
            }
            return regBase;
        }

        private static int GetFlag(IToken token)
        {
            return token.Type switch
            {
                Sixty502DotNetParser.C  => C0,
                Sixty502DotNetParser.M  => M0,
                Sixty502DotNetParser.N  => N,
                Sixty502DotNetParser.NC => NC,
                Sixty502DotNetParser.NZ => NZ,
                Sixty502DotNetParser.P  => P,
                Sixty502DotNetParser.PE => PE,
                _                       => Z
            };
        }

        private bool GenOutC0(Sixty502DotNetParser.Z80IndRegExprContext context)
        {
            var expr = context.expr();
            if (expr?.primaryExpr()?.GetText().Equals("0") == true &&
                context.Start.Type == Sixty502DotNetParser.OUT)
            {
                var instr = Get(Sixty502DotNetParser.OUT, IndC0N81);
                Services.Output.Add(instr.code, instr.code.Size());
                BlockVisitor.GenLineListing(Services, "out (c),0");
                return true;
            }
            return false;
        }

        public override bool GenCpuStatement(Sixty502DotNetParser.CpuStatContext context)
        {
            if (context.z80Bit() != null)
            {
                return GenBit(context);
            }
            if (context.z80ImRst() != null)
            {
                return GenImRst(context.z80ImRst());
            }
            if (context.z80Rel() != null)
            {
                return GenRel(context.z80Rel());
            }
            double expr = double.NaN, expr2 = double.NaN;
            int mnemonic;
            int exprMode = 0, regMode = 0;
            string format = string.Empty;
            // z80RegIx: mnemonic z80Reg ',' '(' ('ix'|'iy') ('+'|'-') expr ')' ;
            if (context.z80RegIx() != null)
            {
                mnemonic = context.z80RegIx().z80Mnemonic().Start.Type;
                regMode |= GetRegMode(context.z80RegIx().z80Reg(), null, 0);
                expr = GetIxOffs(context.z80RegIx().z80IxOffset());
                if (expr == int.MinValue)
                {
                    return false;
                }
                if (context.z80RegIx().z80IxOffset().IX() != null)
                {
                    regMode |= IndIX1Offs;
                    format = $" {context.z80RegIx().z80Reg().GetText().ToLower()},(ix+${(int)expr & 0xff:x2})";
                }
                else
                {
                    regMode |= IndIY1Offs;
                    format = $" {context.z80RegIx().z80Reg().GetText().ToLower()},(iy+${(int)expr & 0xff:x2})";
                }
            }
            // z80IxReg: mnemonic '(' ('ix'|'iy') ('+'|'-') expr ')' (',' z80Reg)? ;
            else if (context.z80IxReg() != null)
            {
                mnemonic = context.z80IxReg().z80Mnemonic().Start.Type;
                expr = GetIxOffs(context.z80IxReg().z80IxOffset());
                if (expr == int.MinValue)
                {
                    return false;
                }
                if (context.z80IxReg().z80IxOffset().IX() != null)
                {
                    regMode |= IndIX0Offs;
                    format = $" (ix+${(int)expr & 0xff:x2})";
                }
                else
                {
                    regMode |= IndIY0Offs;
                    format = $" (ix+${(int)expr & 0xff:x2})";
                }
                if (context.z80IxReg().z80Reg() != null)
                {
                    regMode |= GetRegMode(context.z80IxReg().z80Reg(), null, 1);
                    format += $",{context.z80IxReg().z80Reg().GetText().ToLower()}";
                }
            }
            // z80IxExpr: mnemonic '(' ('ix'|'iy') ('+'|'-') expr ')' ',' expr ;
            else if (context.z80IxExpr() != null)
            {
                mnemonic = context.z80IxExpr().z80Mnemonic().Start.Type;
                expr = GetIxOffs(context.z80IxExpr().z80IxOffset());
                if (expr == int.MinValue)
                {
                    return false;
                }
                if (context.z80IxExpr().z80IxOffset().IX() != null)
                {
                    regMode |= IndIX0Offs;
                    format = $" (ix+${(int)expr & 0xff:x2})";
                }
                else
                {
                    regMode |= IndIY0Offs;
                    format = $" (ix+${(int)expr & 0xff:x2})";
                }
                exprMode |= GetExprMode(context.z80IxExpr().expr(), null, 1, out expr2);
                if (expr2 == int.MinValue)
                {
                    return false;
                }
                format += $",{(int)expr2 & 0xff:x2}";
            }
            // z80RegIndExpr: mnemonic z80Reg ',' '(' expr ')' ;
            else if (context.z80RegIndExpr() != null)
            {
                mnemonic = context.z80RegIndExpr().z80Mnemonic().Start.Type;
                exprMode = GetExprMode(context.z80RegIndExpr().expr(), context.z80RegIndExpr().LeftParen(), 1, out expr);
                if (exprMode == int.MinValue)
                {
                    return false;
                }
                regMode |= GetRegMode(context.z80RegIndExpr().z80Reg(), null, 0);
                format = $" {context.z80RegIndExpr().z80Reg().GetText().ToLower()},";
                if (GetModSize(exprMode))
                {
                    format += $"(${(int)expr & 0xff:x2})";
                }
                else
                {
                    format += $"(${(int)expr & 0xffff:x4})";
                }
            }
            // z80IndExpr: mnemonic '(' expr ')' (',' z80Reg)? ;
            else if (context.z80IndExpr() != null)
            {
                mnemonic = context.z80IndExpr().z80Mnemonic().Start.Type;
                exprMode = GetExprMode(context.z80IndExpr().expr(), context.z80IndExpr().LeftParen(), 0, out expr);
                if (exprMode == int.MinValue)
                {
                    return false;
                }
                if (GetModSize(exprMode))
                {
                    format = $" (${(int)expr & 0xff:x2})";
                }
                else
                {
                    format = $" (${(int)expr & 0xffff:x4})";
                }
                if (context.z80IndExpr().z80Reg() != null)
                {
                    regMode |= GetRegMode(context.z80IndExpr().z80Reg(), null, 1);
                    format += $",{context.z80IndExpr().z80Reg().GetText().ToLower()}";
                }
            }
            // z80IndRegExpr: mnemonic '(' z80Reg ')' (',' expr)? ;
            else if (context.z80IndRegExpr() != null)
            {
                mnemonic = context.z80IndRegExpr().z80Mnemonic().Start.Type;
                regMode |= GetRegMode(context.z80IndRegExpr().z80Reg(), context.z80IndRegExpr().LeftParen().Symbol, 0);
                if (context.z80IndRegExpr().expr() != null)
                {
                    if (regMode == IndC0)
                    {
                        return GenOutC0(context.z80IndRegExpr());
                    }
                    exprMode = GetExprMode(context.z80IndRegExpr().expr(), null, 1, out expr);
                    if (exprMode == int.MaxValue)
                    {
                        return false;
                    }
                    if (expr.Size() == 1)
                    {
                        format += $"${(int)expr & 0xff:x2}";
                    }
                    else
                    {
                        format += $"${(int)expr & 0xffff:x4}";
                    }
                }
                if (!string.IsNullOrEmpty(format))
                {
                    format = $" ({context.z80IndRegExpr().z80Reg().GetText().ToLower()}),{format}";
                }
                else
                {
                    format = $" ({context.z80IndRegExpr().z80Reg().GetText().ToLower()})";
                }
            }
            // z80RegReg
            //      :   mnemonic z80Reg ',' z80Reg
            //      |   mnemonic '(' z80Reg ')' ',' z80Reg
            //      |   mnemonic z80Reg ',' '(' z80Reg ')'
            //      ;
            else if (context.z80RegReg() != null)
            {
                var regRegCtx = context.z80RegReg();
                mnemonic = regRegCtx.z80Mnemonic().Start.Type;
                regMode |= GetRegMode(regRegCtx.reg0, regRegCtx.reg0LParen, 0)
                        | GetRegMode(regRegCtx.reg1, regRegCtx.reg1LParen, 1);
                var lreg = regRegCtx.reg0LParen != null ?
                    $"({regRegCtx.reg0.GetText().ToLower()})" :
                    $"{regRegCtx.reg0.GetText().ToLower()}";
                var rreg = regRegCtx.reg1LParen != null ?
                    $"({regRegCtx.reg1.GetText().ToLower()})" :
                    $"{regRegCtx.reg1.GetText().ToLower()}";
                format = $" {lreg},{rreg}";
            }
            // z80RegExpr: mnemonic z80Reg (',' expr)?;
            else if (context.z80RegExpr() != null)
            {
                mnemonic = context.z80RegExpr().z80Mnemonic().Start.Type;
                format = $" {context.z80RegExpr().z80Reg().GetText().ToLower()}";
                if (context.z80RegExpr().expr() != null)
                {
                    exprMode = GetExprMode(context.z80RegExpr().expr(), null, 1, out expr);
                    if (exprMode == int.MaxValue)
                    {
                        return false;
                    }
                    if (GetModSize(exprMode))
                    {
                        format += $",${(int)expr & 0xff:x2}";
                    }
                    else
                    {
                        format += $",${(int)expr & 0xffff:x4}";
                    }
                }
                regMode |= GetRegMode(context.z80RegExpr().z80Reg(), null, 0);
            }
            // z80Expr: mnemonic expr ;
            else if (context.z80Expr() != null)
            {
                mnemonic = context.z80Expr().z80Mnemonic().Start.Type;
                exprMode = GetExprMode(context.z80Expr().expr(), null, 0, out expr);
                if (GetModSize(exprMode))
                {
                    format = $" ${(int)expr & 0xff:x2}";
                }
                else
                {
                    format = $" ${(int)expr & 0xffff:x4}";
                }
            }
            // z80FlagsInstr: mnemonic flag (',' expr)? ;
            else if (context.z80FlagsInstr() != null)
            {
                mnemonic = context.z80FlagsInstr().z80Mnemonic().Start.Type;
                regMode |= GetFlag(context.z80FlagsInstr().flag);
                format = $" {context.z80FlagsInstr().flag.Text.ToLower()}";
                if (context.z80FlagsInstr().expr() != null)
                {
                    // we already know the mode, now just eval the expression to see if it's legal.
                    exprMode = GetExprMode(context.z80FlagsInstr().expr(), null, 1, out expr);
                    if (expr == int.MinValue)
                    {
                        return false;
                    }
                    format += $",{(int)expr & 0xffff:x4}";
                }
            }
            else if (context.z80Implied() == null)
            {
                // zpAbsStat: 'and' expr ;
                if (context.zpAbsStat() != null &&
                    context.zpAbsStat().mnemonic().AND() != null)
                {
                    mnemonic = Sixty502DotNetParser.AND;
                    exprMode = GetExprMode(context.zpAbsStat().expr(), null, 0, out expr);
                    if (GetModSize(exprMode))
                    {
                        format = $" ${(int)expr & 0xff:x2}";
                    }
                    else
                    {
                        format = $" ${(int)expr & 0xffff:x4}";
                    }
                }
                // implStat: mnemonic 'a'? ;
                else if (context.implStat() != null)
                {
                    mnemonic = context.implStat().Start.Type;
                    if (context.implStat().A() != null)
                    {
                        regMode = A0;
                        format = " a";
                    }
                }
                // regRegStat: mnemonic ('b'|'c'|'d') ;
                else if (context.regRegStat() != null)
                {
                    mnemonic = context.regRegStat().mnemonic().Start.Type;
                    regMode |= s_parserRegToReg[context.regRegStat().src.Type];
                    format = $" {context.regRegStat().src.Text.ToLower()}";
                    if (context.regRegStat().dst != null)
                    {
                        regMode |= s_parserRegToReg[context.regRegStat().dst.Type] * 0b10000000;
                        format += $",{context.regRegStat().dst.Text.ToLower()}";
                    }
                }
                // regListStat: mnemonic ('a'|'b'|'c'|'d') (',' 'a'|'b'|'c'|'d')+ ;
                else if (context.regListStat() != null)
                {
                    var regs = context.regListStat().m6809Reg().Select(ctx => ctx.Start).ToList();
                    if (regs.Count > 2)
                    {
                        return false;
                    }
                    mnemonic = context.regListStat().mnemonic().Start.Type;
                    regMode |= s_parserRegToReg[regs[0].Type] | s_parserRegToReg[regs[1].Type] * 0b10000000;
                    format = $" {regs[0].Text.ToLower()},{regs[1].Text.ToLower()}";
                }
                else
                {
                    return false;
                }
            }
            // z80Implied: mnemonic ;
            else
            {
                mnemonic = context.z80Implied().z80Mnemonic().Start.Type;
            }
            if (expr == int.MaxValue && exprMode == N80)
            {
                Services.Output.AddUninitialized(3);
                return true;
            }
            var instr = new Instruction(mnemonic, regMode | exprMode);
            if (!Set.TryGetValue(instr, out var opcode))
            {
                if (expr == int.MaxValue)
                {
                    expr = 0xffff;
                }
                instr = new Instruction(mnemonic, regMode | N8ToN16(exprMode));
                if (!Set.TryGetValue(instr, out opcode))
                {
                    return false;
                }
                else
                {
                    format = format.Replace($"${(int)expr & 0xff:x2}", $"${(int)expr & 0xffff:x4}");
                }
            }
            Services.Output.Add(opcode.code, opcode.code.Size());
            if (!double.IsNaN(expr))
            {
                var exprSize = opcode.size - opcode.code.Size();
                if (expr.Size() > exprSize)
                {
                    return false;
                }
                if (!double.IsNaN(expr2))
                {
                    if (expr.Size() + expr2.Size() > exprSize)
                    {
                        return false;
                    }
                    Services.Output.Add(expr, 1);
                    Services.Output.Add(expr2, 1);
                }
                else
                {
                    Services.Output.Add(expr, exprSize);
                }
            }
            if (context.implStat() != null)
            {
                CheckRedundantCallReturn(context, 0xcd, 3, 0xc9);
            }
            BlockVisitor.GenLineListing(Services, $"{context.Start.Text.ToLower()}{format}");
            return true;
        }

        protected override IDictionary<string, int> OnGetMnemonics(string architecture)
        {
            if (architecture.StartsWith('i'))
            {
                Set = new Dictionary<Instruction, Opcode>(s_i8080);
                return new Dictionary<string, int>(s_i8080Mnemonics, Services.StringComparer);
            }
            else
            {
                Set = new Dictionary<Instruction, Opcode>(s_z80);
                return new Dictionary<string, int>(s_z80Mnemonics, Services.StringComparer);
            }
        }
    }
}
