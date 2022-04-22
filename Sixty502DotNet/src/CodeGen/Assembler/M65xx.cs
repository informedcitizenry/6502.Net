//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class responsible for assembly of 65xx source.
    /// </summary>
    public partial class M65xx : MotorolaBase
    {
        /// <summary>
        /// Construct a new instance of a <see cref="M65xx"/> object.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="initialArchitecture">The initial CPU/architecture
        /// to initialize the instruction set mnemonics.</param>
        public M65xx(AssemblyServices services, string initialArchitecture)
            : base(services, initialArchitecture)
        {
            // set architecture specific encodings
            Services.Encoding.SelectEncoding("\"petscii\"");
            Services.Encoding.Map("az", 'A');
            Services.Encoding.Map("AZ", 0xc1);
            Services.Encoding.Map('£', '\\');
            Services.Encoding.Map('↑', '^');
            Services.Encoding.Map('←', '_');
            Services.Encoding.Map('▌', 0xa1);
            Services.Encoding.Map('▄', 0xa2);
            Services.Encoding.Map('▔', 0xa3);
            Services.Encoding.Map('▁', 0xa4);
            Services.Encoding.Map('▏', 0xa5);
            Services.Encoding.Map('▒', 0xa6);
            Services.Encoding.Map('▕', 0xa7);
            Services.Encoding.Map('◤', 0xa9);
            Services.Encoding.Map('├', 0xab);
            Services.Encoding.Map('└', 0xad);
            Services.Encoding.Map('┐', 0xae);
            Services.Encoding.Map('▂', 0xaf);
            Services.Encoding.Map('┌', 0xb0);
            Services.Encoding.Map('┴', 0xb1);
            Services.Encoding.Map('┬', 0xb2);
            Services.Encoding.Map('┤', 0xb3);
            Services.Encoding.Map('▎', 0xb4);
            Services.Encoding.Map('▍', 0xb5);
            Services.Encoding.Map('▃', 0xb9);
            Services.Encoding.Map('✓', 0xba);
            Services.Encoding.Map('┘', 0xbd);
            Services.Encoding.Map('━', 0xc0);
            Services.Encoding.Map('♠', 0xc1);
            Services.Encoding.Map('│', 0xc2);
            Services.Encoding.Map('╮', 0xc9);
            Services.Encoding.Map('╰', 0xca);
            Services.Encoding.Map('╯', 0xcb);
            Services.Encoding.Map('╲', 0xcd);
            Services.Encoding.Map('╱', 0xce);
            Services.Encoding.Map('●', 0xd1);
            Services.Encoding.Map('♥', 0xd3);
            Services.Encoding.Map('╭', 0xd5);
            Services.Encoding.Map('╳', 0xd6);
            Services.Encoding.Map('○', 0xd7);
            Services.Encoding.Map('♣', 0xd8);
            Services.Encoding.Map('♦', 0xda);
            Services.Encoding.Map('┼', 0xdb);
            Services.Encoding.Map('π', 0xde);
            Services.Encoding.Map('◥', 0xdf);

            Services.Encoding.SelectEncoding("\"cbmscreen\"");
            Services.Encoding.Map("@Z", '\0');
            Services.Encoding.Map("az", 'A');
            Services.Encoding.Map('£', '\\');
            Services.Encoding.Map('π', '^'); // π is $5e in unshifted
            Services.Encoding.Map('↑', '^'); // ↑ is $5e in shifted
            Services.Encoding.Map('←', '_');
            Services.Encoding.Map('▌', '`');
            Services.Encoding.Map('▄', 'a');
            Services.Encoding.Map('▔', 'b');
            Services.Encoding.Map('▁', 'c');
            Services.Encoding.Map('▏', 'd');
            Services.Encoding.Map('▒', 'e');
            Services.Encoding.Map('▕', 'f');
            Services.Encoding.Map('◤', 'i');
            Services.Encoding.Map('├', 'k');
            Services.Encoding.Map('└', 'm');
            Services.Encoding.Map('┐', 'n');
            Services.Encoding.Map('▂', 'o');
            Services.Encoding.Map('┌', 'p');
            Services.Encoding.Map('┴', 'q');
            Services.Encoding.Map('┬', 'r');
            Services.Encoding.Map('┤', 's');
            Services.Encoding.Map('▎', 't');
            Services.Encoding.Map('▍', 'u');
            Services.Encoding.Map('▃', 'y');
            Services.Encoding.Map('✓', 'z');
            Services.Encoding.Map('┘', '}');
            Services.Encoding.Map('━', '@');
            Services.Encoding.Map('♠', 'A');
            Services.Encoding.Map('│', 'B');
            Services.Encoding.Map('╮', 'I');
            Services.Encoding.Map('╰', 'J');
            Services.Encoding.Map('╯', 'K');
            Services.Encoding.Map('╲', 'M');
            Services.Encoding.Map('╱', 'N');
            Services.Encoding.Map('●', 'Q');
            Services.Encoding.Map('♥', 'S');
            Services.Encoding.Map('╭', 'U');
            Services.Encoding.Map('╳', 'V');
            Services.Encoding.Map('○', 'W');
            Services.Encoding.Map('♣', 'X');
            Services.Encoding.Map('♦', 'Z');
            Services.Encoding.Map('┼', '[');
            Services.Encoding.Map('◥', '_');

            Services.Encoding.SelectEncoding("\"atascreen\"");
            Services.Encoding.Map(" _", '\0');

            Services.Encoding.SelectDefaultEncoding();
            _cpu = "";
            Set = s_6502;
            _ = OnGetMnemonics(Services.CPU);
            _m16 = _x16 = false;
        }

        public override void Reset()
        {
            base.Reset();
            _m16 = _x16 = false;
        }

        protected override IDictionary<string, int> OnGetMnemonics(string architecture)
        {
            IDictionary<string, int> mnemonics;
            if (string.IsNullOrEmpty(architecture))
            {
                _cpu = "6502";
                Set = new Dictionary<Instruction, Opcode>(s_6502);
                mnemonics = new Dictionary<string, int>(s_6502Mnemonics, Services.StringComparer);
            }
            else
            {
                switch (architecture)
                {
                    case "6502":
                        Set = new Dictionary<Instruction, Opcode>(s_6502);
                        mnemonics = new Dictionary<string, int>(s_6502Mnemonics, Services.StringComparer);
                        break;
                    case "6502i":
                        Set = s_6502.Concat(s_6502i).ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics.Concat(s_6502iMnemonics).ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        break;
                    case "65816":
                        Set = s_6502.Concat(s_65c02)
                                    .Concat(s_65816)
                                    .Concat(s_w65c02)
                                    .ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics
                                    .Concat(s_65816Mnemonics)
                                    .Concat(s_65c02Mnemonics)
                                    .ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        mnemonics.Add("wai", Sixty502DotNetLexer.WAI);
                        break;
                    case "65C02":
                        Set = s_6502.Concat(s_65c02).ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics.Concat(s_65c02Mnemonics).ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        break;
                    case "65CS02":
                        Set = s_6502.Concat(s_65c02).Concat(s_w65c02).ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics.Concat(s_65c02Mnemonics).ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        mnemonics.Add("stp", Sixty502DotNetLexer.STP);
                        mnemonics.Add("wai", Sixty502DotNetLexer.WAI);
                        break;
                    case "45GS02":
                    case "m65":
                    case "65CE02":
                        Set = s_6502.Where(o => (o.Value.code & 0x1f) != 0x10) // exclude 6502 branch instructions
                                    .Concat(s_65c02.Where(o => o.Value.code != 0x80 && (o.Value.code & 0x0f) != 0x02))
                                    .Concat(s_r65c02)
                                    .Concat(s_65ce02)
                                    .ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics.Concat(s_65c02Mnemonics)
                                                   .Concat(s_r65c02Mnemonics)
                                                   .Concat(s_65ce02Mnemonics)
                                                   .ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        if (architecture[0] != '6')
                        {
                            if (architecture[0] == 'm')
                            {
                                Set = Set.Where(o => o.Key.mnemonic != Sixty502DotNetParser.NOP)
                                         .Concat(s_m65)
                                         .ToDictionary(k => k.Key, k => k.Value);
                            }
                            Set.Add(new Instruction(Sixty502DotNetParser.MAP, Implied), new Opcode("45GS02", 0x5c));
                            Set.Add(new Instruction(Sixty502DotNetParser.EOM, Implied), new Opcode("45GS02", 0xea));
                            mnemonics = mnemonics.Where(o => !o.Key.Equals("nop"))
                                                 .Concat(s_m65Mnemonics)
                                                 .ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                            mnemonics.Add("map", Sixty502DotNetLexer.MAP);
                            mnemonics.Add("eom", Sixty502DotNetLexer.EOM);
                        }
                        break;
                    case "c64dtv2":
                        Set = s_6502.Concat(s_c64dtv2).ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = new Dictionary<string, int>(s_6502Mnemonics, Services.StringComparer)
                        {
                            { "sac", Sixty502DotNetLexer.SAC },
                            { "sir", Sixty502DotNetLexer.SIR }
                        };
                        break;
                    case "HuC6280":
                        Set = s_6502.Concat(s_65c02.Where(o => (o.Value.code & 0x0f) != 0x02))
                                    .Concat(s_r65c02)
                                    .Concat(s_w65c02)
                                    .Concat(s_huC6280)
                                    .ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics.Concat(s_65c02Mnemonics)
                                                    .Concat(s_r65c02Mnemonics)
                                                    .Concat(s_huc6280Mnemonics)
                                                    .ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        mnemonics.Add("stp", Sixty502DotNetLexer.STP);
                        mnemonics.Add("wai", Sixty502DotNetLexer.WAI);
                        break;
                    case "R65C02":
                        Set = s_6502.Concat(s_65c02)
                                    .Concat(s_r65c02)
                                    .ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics.Concat(s_65c02Mnemonics)
                                                   .Concat(s_r65c02Mnemonics)
                                                   .ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        break;
                    case "W65C02":
                        Set = s_6502.Concat(s_65c02)
                                    .Concat(s_r65c02)
                                    .Concat(s_w65c02)
                                    .ToDictionary(k => k.Key, k => k.Value);
                        mnemonics = s_6502Mnemonics
                                    .Concat(s_r65c02Mnemonics)
                                    .ToDictionary(k => k.Key, k => k.Value, Services.StringComparer);
                        mnemonics.Add("stp", Sixty502DotNetLexer.STP);
                        mnemonics.Add("wai", Sixty502DotNetLexer.WAI);
                        break;
                    default:
                        throw new Exception();
                }
                _cpu = architecture;
            }
            if (Services.Options.BranchAlways && (architecture.StartsWith("6502") || string.IsNullOrEmpty(architecture)))
            {
                mnemonics.Add("bra", Sixty502DotNetParser.BRA);
                Set.Add(new Instruction(Sixty502DotNetParser.BRA, Relative), new Opcode(architecture, 0x50));
            }
            SupportsDirectPage = _cpu.Equals("65816");
            _auto = _cpu.Equals("65816") && Services.Options.Autosize;
            return mnemonics;
        }

        /// <summary>
        /// Set the immediate mode size based on the parsed directive.
        /// </summary>
        /// <param name="context">The parse tree context containing the
        /// directive.</param>
        public void SetImmediate(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            if (_cpu.Equals("65816"))
            {
                int directive = context.Start.Type;
                switch (directive)
                {
                    case Sixty502DotNetParser.M8:
                    case Sixty502DotNetParser.M16:
                        _m16 = directive == Sixty502DotNetParser.M16;
                        SetImmediate(_m16 ? 3 : 2, 'a');
                        break;
                    case Sixty502DotNetParser.MX8:
                    case Sixty502DotNetParser.MX16:
                        _m16 = _x16 = directive == Sixty502DotNetParser.MX16;
                        SetImmediate(_m16 ? 3 : 2, 'a');
                        SetImmediate(_x16 ? 3 : 2, 'x');
                        break;
                    case Sixty502DotNetParser.X8:
                    case Sixty502DotNetParser.X16:
                        _x16 = directive == Sixty502DotNetParser.X16;
                        SetImmediate(_x16 ? 3 : 2, 'x');
                        break;
                }
            }
            else
            {
                Services.Log.LogEntry(context, "Directive ignored for non-65816 mode.", false);
            }
        }

        /// <summary>
        /// For 65816 mode, track size of operands when encountering <c>rep</c>
        /// and <c>sep</c> commands.
        /// </summary>
        /// <param name="context"></param>
        public void SetAuto(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            if (_cpu.Equals("65816"))
            {
                _auto = context.Start.Type == Sixty502DotNetParser.Auto;
            }
            else
            {
                Services.Log.LogEntry(context, "Directive ignored for non-65816 mode.", false);
            }
        }

        private void SetImmediate(int size, char register)
        {
            int currmode, newmode;
            if (size == 2)
            {
                newmode = Immediate;
                currmode = newmode | Absolute;
            }
            else
            {
                currmode = Immediate;
                newmode = currmode | Absolute;
            }
            var immediateSet = register == 'a' ? s_immediateA : s_immediateX;
            var immediates = Set.Where(kvp =>
            {
                if (kvp.Key.mode == currmode)
                {
                    if (immediateSet.Contains(kvp.Key.mnemonic))
                        return true;
                    if (register == 'a' && (kvp.Value.cpu.Equals("6502") || kvp.Value.cpu.Equals("65C02")))
                        return !s_immediateX.Contains(kvp.Key.mnemonic);
                }
                return false;
            });
            var nonImmediates = Set.Except(immediates);

            Set = nonImmediates.Concat(immediates.ToDictionary(kvp => new Instruction(kvp.Key.mnemonic, newmode),
                                         kvp => new Opcode(kvp.Value.cpu, kvp.Value.code, size)))
                                                .ToDictionary(k => k.Key, k => k.Value);
        }

        private bool GenImmediate(IToken mnemonic, Sixty502DotNetParser.ExprContext context)
        {
            var result = GenOperand(mnemonic, Immediate, context);
            if (result && (mnemonic.Type == Sixty502DotNetParser.REP || mnemonic.Type == Sixty502DotNetParser.SEP) && _auto)
            {
                var size = mnemonic.Type == Sixty502DotNetParser.REP ? 3 : 2;
                var immediate = Services.ExpressionVisitor.Visit(context);
                if (immediate.IsDefined && immediate.IsNumeric)
                {
                    var p = immediate.ToInt();
                    if ((p & 0x20) != 0)
                    {
                        SetImmediate(size, 'a');
                    }
                    if ((p & 0x10) != 0)
                    {
                        SetImmediate(size, 'x');
                    }
                }
            }
            return result;
        }

        private void GenRelative(IToken mnemonic, Sixty502DotNetParser.ExprContext context, bool allowPseudo)
        {
            var mnemType = mnemonic.Type;
            var rel = Services.ExpressionVisitor.Visit(context);
            if (rel.IsNumeric)
            {
                var offset = rel.ToInt();
                int addrOffs;
                int minValue, maxValue;
                var mode = Relative;
                if (_cpu.Equals("65CE02") || s_branches16.Contains(mnemType))
                {
                    addrOffs = 3;
                    minValue = short.MinValue;
                    maxValue = short.MaxValue;
                    mode |= Absolute;
                }
                else
                {
                    addrOffs = 2;
                    minValue = sbyte.MinValue;
                    maxValue = sbyte.MaxValue;
                    mode |= ZeroPage;
                }
                var relOffs = Services.Output.GetRelativeOffset(offset, addrOffs);
                if (relOffs < minValue || relOffs > maxValue)
                {
                    if (allowPseudo)
                    {
                        if (offset >= short.MinValue && offset <= ushort.MaxValue)
                        {
                            Services.Output.Add(Get(s_pseudoConv[mnemType], Relative).code, 1);
                            Services.Output.Add(3, 1);
                            Services.Output.Add((byte)0x4c);
                            Services.Output.Add(offset, 2);
                            BlockVisitor.GenLineListing(Services,
                                $"{s_pseudoConvText[mnemType]} ${Services.Output.LogicalPC:x4}:jmp ${offset:x4}");
                            return;
                        }
                    }
                    if (!Services.State.PassNeeded)
                    {
                        Services.Log.LogEntry(context,
                            $"Relative branch too far ({Math.Abs(relOffs)} bytes). Consider using a pseudo branch directive.");
                    }
                    else
                    {
                        Services.Output.AddUninitialized(allowPseudo ? 3 : 2);
                    }
                }
                else
                {
                    var mnemText = mnemonic.Text.ToLower();
                    if (allowPseudo)
                    {
                        mnemText = s_pseudoRelText[mnemType];
                        mnemType = s_pseudoToRel[mnemType];
                    }
                    if (IsValid(mnemType, mode))
                    {
                        Services.Output.Add(Get(mnemType, mode).code, 1);
                        Services.Output.Add(relOffs, addrOffs - 1);
                        BlockVisitor.GenLineListing(Services,
                            $"{mnemText} ${offset & 0xffff:x4}");
                    }
                }
            }
            else if (rel.IsDefined)
            {
                Services.Log.LogEntry(context, Errors.TypeMismatchError);
            }
            else
            {
                Services.Output.AddUninitialized(2);
            }
        }

        private bool GenBlockMove(IToken mnemonic, Sixty502DotNetParser.BlockMoveStatContext context)
        {
            if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[0], sbyte.MinValue, byte.MaxValue, out var src))
            {
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[1], sbyte.MinValue, byte.MaxValue, out var dst))
                {
                    var opcode = mnemonic.Type == Sixty502DotNetParser.MVN ? 0x54 : 0x44;
                    Services.Output.Add(opcode, 1);
                    Services.Output.Add(src, 1);
                    Services.Output.Add(dst, 1);
                    BlockVisitor.GenLineListing(Services,
                        $"{mnemonic.Text.ToLower()} ${(int)src & 0xff:x2},${(int)dst & 0xff:x2}");
                    return true;
                }
            }
            if (Services.State.PassNeeded)
            {
                Services.Output.AddUninitialized(3);
                return true;
            }
            return false;
        }

        private bool GenBitMemoryStat(IToken mnemonic, Sixty502DotNetParser.BitMemoryStatContext context)
        {
            if (!Services.ExpressionVisitor.TryGetPrimaryExpression(context.bitExpr, out var bit))
            {
                return false;
            }
            if (bit.IsIntegral && bit.ToInt() >= 0 && bit.ToInt() <= 7)
            {
                var bitMode = (bit.ToInt() << 14) | Bit0;
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[1], sbyte.MinValue, byte.MaxValue, out var e1))
                {
                    if (context.expr().Length > 2)
                    {
                        if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[2], short.MinValue, ushort.MaxValue, out var e2)
                            && e2.IsInteger())
                        {
                            var relative = Services.Output.GetRelativeOffset((int)e2, 3);
                            if (relative >= sbyte.MinValue && relative <= sbyte.MaxValue &&
                                IsValid(mnemonic.Type, bitMode | ThreeOpRel0))
                            {
                                var code = Get(mnemonic.Type, bitMode | ThreeOpRel0);
                                Services.Output.Add(code.code, 1);
                                Services.Output.Add(e1);
                                Services.Output.Add(relative, 1);
                                BlockVisitor.GenLineListing(Services,
                                    $"{mnemonic.Text.ToLower()} {bit.ToInt()},{(int)e1 & 0xff:x2},{(int)e2 & 0xffff:x4}");
                                return true;
                            }
                            if (Services.State.PassNeeded)
                            {
                                Services.Output.AddUninitialized(3);
                                return true;
                            }
                            return false;
                        }
                        if (Services.State.PassNeeded)
                        {
                            Services.Output.AddUninitialized(3);
                            return true;
                        }
                    }
                    else if (IsValid(mnemonic.Type, bitMode | Zp0))
                    {
                        var code = Get(mnemonic.Type, bitMode | Zp0);
                        Services.Output.Add(code.code, 1);
                        Services.Output.Add(e1, 1);
                        BlockVisitor.GenLineListing(Services,
                            $"{mnemonic.Text.ToLower()} {bit.ToInt()},{(int)e1 & 0xff:x2}");
                        return true;
                    }
                }
                else if (Services.State.PassNeeded)
                {
                    Services.Output.AddUninitialized(2);
                    return true;
                }
            }
            return false;
        }

        private bool GenTstMemoryStat(IToken mnemonic, Sixty502DotNetParser.TstMemoryStatContext context)
        {
            double e1;
            double e2;
            var sb = new StringBuilder($"{context.Start.Text.ToLower()} ");
            if (context.immStat() != null)
            {
                sb.Append('#');
                int mode;
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.immStat().expr(), sbyte.MinValue, byte.MaxValue, out e1))
                {
                    if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[0], short.MinValue, ushort.MaxValue, out e2))
                    {
                        mode = TestBitZp;
                        if (e2.Size() == 2)
                        {
                            mode = TestBitAbs;
                        }
                        if (context.X() != null)
                        {
                            mode |= IndexedX;
                        }
                    }
                    else
                    {
                        return false;
                    }
                    if (IsValid(mnemonic.Type, mode))
                    {
                        var code = Get(mnemonic.Type, mode);
                        Services.Output.Add(code.code, 1);
                        Services.Output.Add(e1, 1);
                        if (!double.IsNaN(e2))
                        {
                            Services.Output.Add(e2);
                            sb.Append($"${(int)e1 & 0xff:x2},${(int)e2 & 0xff}");
                            if (context.X() != null)
                            {
                                sb.Append(",x");
                            }
                            BlockVisitor.GenLineListing(Services, sb.ToString());
                        }
                        return true;
                    }
                }
                return false;
            }
            else if (Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[0], short.MinValue, ushort.MaxValue, out e1) &&
                    Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[1], short.MinValue, ushort.MaxValue, out e2) &&
                    Services.ExpressionVisitor.TryGetArithmeticExpr(context.expr()[2], short.MinValue, ushort.MaxValue, out var e3))
            {
                if (IsValid(mnemonic.Type, ThreeOpAbs))
                {
                    var code = Get(mnemonic.Type, ThreeOpAbs);
                    Services.Output.Add(code.code, 1);
                    Services.Output.Add(e1, 2);
                    Services.Output.Add(e2, 2);
                    Services.Output.Add(e3, 2);
                    sb.Append($"${(int)e1 & 0xffff:x4},${(int)e2 & 0xffff:x4},${(int)e3 & 0xffff:x4}");
                    BlockVisitor.GenLineListing(Services, sb.ToString());
                    return true;
                }
            }
            return false;
        }

        public override void CpuDirectiveStatement(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            int directive = context.Start.Type;
            if (directive == Sixty502DotNetParser.Dp)
            {
                SetPage(context);
                return;
            }
            switch (directive)
            {
                case Sixty502DotNetParser.Auto:
                case Sixty502DotNetParser.Manual:
                    SetAuto(context);
                    break;
                case Sixty502DotNetParser.M8:
                case Sixty502DotNetParser.M16:
                case Sixty502DotNetParser.MX8:
                case Sixty502DotNetParser.MX16:
                case Sixty502DotNetParser.X8:
                case Sixty502DotNetParser.X16:
                    SetImmediate(context);
                    break;
                default:
                    Services.Log.LogEntry(context, "Directive ignored for CPU.", false);
                    break;
            }
        }

        public override bool GenCpuStatement(Sixty502DotNetParser.CpuStatContext context)
        {
            var mnemonic = context.Start;
            // implStat: mnemonic ;
            if (context.implStat() != null)
            {
                var mnemType = context.Start.Type;
                if (IsValid(mnemType, Implied) &&
                    (context.implStat().A() == null || s_impliedAccumulator.Contains(mnemType)))
                {
                    Services.Output.Add(Get(mnemType, Implied).code, 1);
                    BlockVisitor.GenLineListing(Services, context.Start.Text.ToLower());
                    CheckRedundantCallReturn(context, 0x20, 3, 0x60); // rts
                    if (Services.CPU?.Equals("65816") == true)
                    {
                        CheckRedundantCallReturn(context, 0x22, 4, 0x60); // long jsr/rts
                    }
                    return true;
                }
                return false;
            }
            // bitMemoryStat
            //          :   ('bbr'|'bbs'|'rmb'|'smb') expr ',' expr (',' expr)? ;
            if (context.bitMemoryStat() != null)
            {
                return GenBitMemoryStat(mnemonic, context.bitMemoryStat());
            }
            // tstMemoryStat
            //          :   mnemonic '#' expr ',' expr (',' 'x')?
            //          |   mnemonic expr ',' expr ',' expr
            //          ;
            if (context.tstMemoryStat() != null)
            {
                return GenTstMemoryStat(mnemonic, context.tstMemoryStat());
            }
            // blockMoveStat: ('mvn'|'mvp') expr ',' expr
            if (context.blockMoveStat() != null)
            {
                return GenBlockMove(mnemonic, context.blockMoveStat());
            }
            // pseudoRelStat: pseudoRelStat expr ;
            if (context.pseudoRelStat() != null)
            {
                GenRelative(mnemonic, context.pseudoRelStat().expr(), true);
                return true;
            }
            // relStat
            //      :   relative expr 
            //      |   relative16 expr
            //      ;
            if (context.relStat() != null)
            {
                GenRelative(mnemonic, context.relStat().expr(), false);
                return true;
            }
            // indStat: mnemonic '(' expr ')' ;
            if (context.indStat() != null)
            {
                return GenOperand(mnemonic, IndZp, context.indStat().expr());
            }
            // indXStat: mnemonic '(' expr ',' 'x' ')' ;
            if (context.indXStat() != null)
            {
                return GenOperand(mnemonic, IndX, context.indXStat().expr());
            }
            // indYStat: mnemonic '(' expr (',' ('s'|'sp'))? ')' ',' ('y'|'z') ;
            if (context.indYStat() != null)
            {
                var mode = context.indYStat().outerIndex.Type == Sixty502DotNetParser.Y ? IndY : IndZ;
                if (context.indYStat().innerIndex?.Type == Sixty502DotNetParser.S)
                {
                    mode |= IndexedS;
                }
                if (context.indYStat().innerIndex?.Type == Sixty502DotNetParser.SP)
                {
                    mode |= IndexedSp;
                }
                return GenOperand(mnemonic, mode, context.indYStat().expr());
            }
            // dirStat: mnemonic '[' expr ']' ;
            if (context.dirStat() != null)
            {
                return GenOperand(mnemonic, Dir, context.dirStat().expr());
            }
            // dirYStat: mnemonic '[' expr ']' ',' ('y'|'z')
            if (context.dirYStat() != null)
            {
                var mode = context.dirYStat().index.Type == Sixty502DotNetParser.Y ? DirY : DirZ;
                return GenOperand(mnemonic, mode, context.dirYStat().expr());
            }
            // ixStat: mnemonic bitwidth_modifier? expr ',' ('s' | 'x' | 'y');
            if (context.ixStat() != null)
            {
                var mode = context.ixStat().index.Type switch
                {
                    Sixty502DotNetParser.S => IndexedS,
                    Sixty502DotNetParser.X => IndexedX,
                    Sixty502DotNetParser.Y => IndexedY,
                    _ => -1
                };
                if (mode < 0)
                {
                    Services.Log.LogEntry(context.ixStat(), context.ixStat().index, "Invalid or unknown index register.");
                    return false;
                }
                int size = mode == IndexedS ? 2 : 3;
                mode |= ZeroPage;
                return GenOperand(mnemonic, mode, context.ixStat().expr(), size, context.ixStat().bitwidth());
            }
            // immStat: mnemonic '#' expr ;
            if (context.immStat() != null)
            {
                return GenImmediate(mnemonic, context.immStat().expr());
            }
            return base.GenCpuStatement(context);
        }
    }
}
