//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet.m680x
{
    /// <summary>
    /// A class responsible for Motorola 6800/6809 assembly.
    /// </summary>
    public sealed partial class M6809Asm : MotorolaBase
    {
        #region Subclasses

        class PushPullComparer : IComparer<string>
        {
            readonly IDictionary<string, byte> _lookup;

            public PushPullComparer(IDictionary<string, byte> lookup)
                => _lookup = lookup;

            public int Compare(string x, string y) =>
                _lookup == null ? 1 : _lookup[x].CompareTo(_lookup[y]);
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the M6800/6809 assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public M6809Asm(AssemblyServices services)
            : base(services)
        {
            Reserved.DefineType("Mnemonics",
                "aba", "abx", "adca", "adcb", "adda", "addb",
                "addd", "anda", "andb", "andcc", "asl", "asla",
                "aslb", "asr", "asra", "asrb", "bcc", "bcs",
                "beq", "bge", "bgt", "bhi", "bita", "bitb",
                "ble", "bls", "blt", "bmi", "bne", "bpl",
                "bra", "bsr", "bvc", "bvs", "cba", "clc",
                "cli", "clr", "clra", "clrb", "clv", "cmpa",
                "cmpb", "cmpd", "cmps", "cmpu", "cmpx", "cmpy",
                "com", "coma", "comb", "cpxa", "cwai", "daa",
                "dec", "deca", "decb", "des", "dex", "eora",
                "eorb", "inc", "inca", "incb", "ins", "inx",
                "jmp", "jsr", "lda", "ldaa", "ldab", "ldb",
                "ldd", "lds", "ldu", "ldx", "ldy", "leas",
                "leau", "leax", "leay", "lsl", "lsla", "lslb",
                "lsr", "lsra", "lsrb", "mul", "neg", "nega",
                "negb", "nop", "ora", "oraa", "orab", "orb",
                "orcc", "psha", "pshb", "pshs", "pshu", "pula",
                "pulb", "puls", "pulu", "rol", "rola", "rolb",
                "ror", "rora", "rorb", "rti", "rts", "sba",
                "sbca", "sbcb", "sec", "sei", "sev", "sex",
                "sta", "staa", "stab", "stb", "std", "sts",
                "stu", "stx", "sty", "suba", "subb", "subd",
                "swi", "swi2", "swi3", "sync", "tab", "tap",
                "tba", "tpa", "tst", "tsta", "tstb", "tsx",
                "txs", "wai");

            Reserved.DefineType("PushPullsExchanges",
                "pshs", "pshu", "puls", "pulu", "tfr", "exg");

            Reserved.DefineType("Exchanges",
                "tfr", "exg");

            Reserved.DefineType("Branches",
                "bcc", "bcs", "beq", "bge", "bgt", "bhi",
                "bhs", "ble", "bls", "blo", "bls", "blt",
                "bmi", "bne", "bpl", "bra", "brn", "bsr",
                "bvc", "bvs");

            Reserved.DefineType("LongBranches",
                "lbcc", "lbcs", "lbeq", "lbge", "lbgt", "lbhi",
                "lbhs", "lble", "lbls", "lblo", "lbls", "lblt",
                "lbmi", "lbne", "lbpl", "lbra", "lbrn", "lbsr",
                "lbvc", "lbvs");

            Reserved.DefineType("DirectPage",
                ".dp");

            // for 6809, parsing line terminations is tricky for auto-increment indexing (e.g, ,x++)
            // so we need to redefine the parser.
            if (CPU.Equals("m6809"))
                LexerParser.LineTerminationFunc = TerminatesLine;

            Services.Output.IsLittleEndian = false;
            _dp = 0;
        }
        #endregion

        #region Methods

        bool TerminatesLine(Token token)
        {
            if (token.Name.Equals("+") && token.Parent.Name.Equals(","))
            {
                // look at the instruction, is it a 6809?
                var parent = token.Parent.Parent;
                while (parent != null)
                {
                    var inst = parent.Children.FirstOrDefault(t => t.Type == TokenType.Instruction);
                    if (inst != null)
                        return Assembles(inst.Name);
                    parent = parent.Parent;
                }
            }
            return false;
        }

        protected override bool IsCpuValid(string cpu)
            => cpu.Equals("m6800") || cpu.Equals("m6809");

        protected override void OnReset() { _dp = 0; }

        protected override void OnSetCpu()
        {
            if (CPU.Equals("m6809"))
                ActiveInstructions = new Dictionary<(string Mnem, Modes mode), CpuInstruction>(s_opcodes6809);
            else
                ActiveInstructions = new Dictionary<(string Mnem, Modes mode), CpuInstruction>(s_opcodes6800);
        }

        protected override Modes ParseOperand(SourceLine line)
        {
            var mode = Modes.Implied;
            if (!string.IsNullOrEmpty(line.OperandExpression))
            {
                var expression = line.Operand.Children[0].Children;
                var firstToken = expression[0];

                var forcedMode = GetForcedModifier(firstToken, expression.Count);
                if (forcedMode != Modes.Implied)
                {
                    expression = expression.Skip(1).ToList();
                    firstToken = expression[0];
                    mode |= forcedMode;
                }
                if (firstToken.Name.Equals("#"))
                {
                    if (expression.Count < 2)
                        throw new SyntaxException(firstToken.Position,
                            "Missing expression.");
                    expression = expression.Skip(1).ToList();
                    mode |= Modes.Immediate;
                }
                var evalMode = Evaluate(expression, 0);
                if (!mode.HasFlag(Modes.ForceWidth))
                    mode |= evalMode;
                else if (evalMode > (mode & Modes.SizeMask))
                    throw new ExpressionException(firstToken.Position, "Width specifier does not match expression.");

                if (line.Operand.Children.Count > 1)
                {
                    if (line.Operand.Children[1].Children.Count != 1 ||
                        !line.Operand.Children[1].Children[0].Name.Equals("x", Services.StringComparison))
                        throw new SyntaxException(line.Operand.Children[1].Position,
                            "Unexpected expression.");
                    mode |= Modes.IndexedX;
                }
                if (Reserved.IsOneOf("Branches", line.InstructionName))
                    mode = (mode & ~Modes.Absolute) | Modes.Relative;
                else if (Reserved.IsOneOf("Longbranches", line.InstructionName))
                    mode |= Modes.Relative;
                if (_dp > 0 &&
                    (mode == Modes.ZeroPage ||
                    (mode == Modes.Absolute && (int)Evaluations[0] / 256 == _dp)))
                {
                    if (mode == Modes.Absolute)
                    {
                        Evaluations[0] = (int)Evaluations[0] & 0xFF;
                        mode = Modes.ZeroPage;
                    }
                    else
                    {
                        mode = Modes.Absolute;
                    }
                }
            }
            return mode;
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            Evaluations[0] = Evaluations[1] = Evaluations[2] = double.NaN;
            if (line.InstructionName.Equals(".dp"))
                return AssembleDpDirective(line);
            if (Reserved.IsOneOf("PushPullsExchanges", line.InstructionName))
            {
                try
                {
                    return AssemblePushPullExchange(line);
                }
                catch (SyntaxException synEx)
                {
                    Services.Log.LogEntry(line, synEx.Position, synEx.Message);
                }
            }
            if (CPU.Equals("m6809") &&
                line.Operand.Children.Count > 0 &&
                (
                 line.Operand.Children.Count >= 2 ||
                 (
                  line.Operand.Children[0].Children.Count == 1 &&
                  line.Operand.Children[0].Children[0].Name.Equals("[")
                 )
                )
               )
            {
                try
                {
                    return AssembleIndexed(line);
                }
                catch (SyntaxException synEx)
                {
                    Services.Log.LogEntry(line, synEx.Position, synEx.Message);
                    return string.Empty;
                }
            }
            return base.OnAssembleLine(line);
        }

        string AssembleDpDirective(SourceLine line)
        {
            if (!line.OperandHasToken)
                Services.Log.LogEntry(line, line.Instruction,
                   "Directive \".dp\" requires a page.");
            else if (CPU.Equals("m6809"))
                _dp = (int)Services.Evaluator.Evaluate(line.Operand, byte.MinValue, byte.MaxValue);
            else
                Services.Log.LogEntry(line, line.Instruction,
                    "Directive \".dp\" ignored for selected CPU.", false);
            return string.Empty;
        }

        string AssembleIndexed(SourceLine line)
        {
            if (!ActiveInstructions.TryGetValue((line.InstructionName, Modes.IndexedX), out var instruction))
                throw new SyntaxException(line.Operand.Position,
                    $"Addressing mode not supported for instruction \"{line.InstructionName}\".");
            var modes = IndexModes.None;
            var firstparam = line.Operand;
            var disSb = new StringBuilder($"{line.InstructionName} ");
            IndexModes indexMode = IndexModes.None, offsRegMode = IndexModes.None;
            if (firstparam.Children[0].Children.Count > 0 &&
                firstparam.Children[0].Children[0].Name.Equals("["))
            {
                // check for indirect modes
                firstparam = firstparam.Children[0];
                var indirect = firstparam.Children[0];
                if (firstparam.Children.Count > 1)
                    throw new SyntaxException(firstparam.Children[1].Position,
                        $"Unexpected expression \"{firstparam.Children[1]}\".");
                disSb.Append('[');
                if (indirect.Children.Count < 2)
                {
                    try
                    {
                        var value = Services.Evaluator.Evaluate(indirect.Children[0], short.MinValue, ushort.MaxValue);
                        disSb.Append($"${(int)value & 0xFFFF:x4}");
                        Evaluations[1] = (int)value / 256;
                        Evaluations[2] = (int)value & 0xFF;
                    }
                    catch (IllegalQuantityException ex)
                    {
                        if (!Services.PassNeeded)
                            throw ex;
                        Evaluations[1] = Evaluations[2] = 0xFF;
                    }
                    modes = IndexModes.ExtInd;
                }
                else
                {
                    modes |= IndexModes.Indir;
                    firstparam = indirect;
                }
            }
            if (firstparam.Children.Count > 1)
            {
                if (firstparam.Children.Count != 2)
                    throw new SyntaxException(firstparam.Children[2].Position,
                        $"Unexpected expression \"{firstparam.Children[2].ToString().Trim()}\".");

                var firstOp = firstparam.Children[0];

                if (firstOp.Children.Count == 0)
                {
                    // auto-increment/decrement, e.g. "leax ,x++"
                    disSb.Append(',');
                    var secondOp = firstparam.Children[1];
                    if (secondOp.Children.Count == 0)
                        throw new SyntaxException(secondOp.Position,
                            "Missing expression.");
                    if (secondOp.Children.Count > 3)
                        throw new SyntaxException(secondOp.Children[3].Position,
                            $"Unexpected expression \"{secondOp.Children[3].ToString().Trim()}\".");
                    Token indexToken;
                    Token incToken = null;
                    int amount = 1;
                    // examine first token post comma
                    var secondOpChild = secondOp.Children[0];
                    if (s_ixRegisterModes.ContainsKey(secondOpChild.Name))
                    {
                        // auto-increment
                        indexToken = secondOpChild;
                        disSb.Append(indexToken.Name);
                        if (secondOp.Children.Count > 1)
                        {
                            incToken = secondOp.Children[1];
                            if (!incToken.Name.Equals("+"))
                                throw new SyntaxException(incToken.Position,
                                    $"Invalid expression \"{secondOp.Name}\".");
                            disSb.Append('+');
                            if (secondOp.Children.Count == 3)
                            {
                                if (!secondOp.Children[2].Name.Equals(incToken.Name))
                                    throw new SyntaxException(secondOp.Children[2].Position,
                                        $"Invalid expression \"{secondOp.Name}\".");
                                disSb.Append('+');
                                amount++;
                            }
                        }
                    }
                    else
                    {
                        if (secondOp.Children.Count < 2)
                            throw new SyntaxException(secondOp.Children[0].Position,
                                "Missing expression.");
                        incToken = secondOpChild;
                        if (!incToken.Name.Equals("-"))
                            throw new SyntaxException(incToken.Position,
                                $"Invalid expression \"{secondOp.Name}\".");
                        disSb.Append('-');
                        if (secondOp.Children[1].Name.Equals("-"))
                        {
                            if (secondOp.Children.Count != 3)
                                throw new SyntaxException(secondOp.Position,
                                    "Missing expression.");
                            disSb.Append('-');
                            indexToken = secondOp.Children[2];
                            amount++;
                        }
                        else
                        {
                            if (secondOp.Children.Count > 2)
                                throw new SyntaxException(secondOp.Children[2].Position,
                                    $"Invalid expression \"{secondOp.Name}\".");
                            indexToken = secondOp.Children[1];
                        }
                        disSb.Append(indexToken.Name);
                    }
                    if (!s_ixRegisterModes.TryGetValue(indexToken.Name, out indexMode))
                        throw new SyntaxException(indexToken.Position,
                            $"\"{indexToken.Name}\" is not a valid register.");
                    modes |= indexMode | IndexModes.Inc1;
                    if (incToken == null)
                        modes |= IndexModes.ZeroOffs;
                    else if (incToken.Name.Equals("-"))
                        modes |= IndexModes.Dec1;
                    if (amount > 1)
                        modes |= IndexModes.Inc2;
                    if (modes.HasFlag(IndexModes.Indir) &&
                        !modes.HasFlag(IndexModes.By2Bit) &&
                        !modes.HasFlag(IndexModes.Offsbit))
                        throw new SyntaxException(firstOp.Position,
                            "Addressing mode not supported for selected CPU.");
                }
                else
                {
                    var secondOp = firstparam.Children[1];
                    if (secondOp.Children.Count == 0)
                        throw new SyntaxException(secondOp.Position,
                            "Missing Expression.");
                    if (secondOp.Children.Count > 1)
                        throw new SyntaxException(secondOp.Children[1].Position,
                            "Unexpected expression.");

                    var ixRegName = secondOp.Children[0].Name;

                    if (firstOp.Children.Count == 1 &&
                        s_offsRegisterModes.TryGetValue(firstOp.Children[0].Name, out offsRegMode))
                    {
                        modes |= offsRegMode;
                        disSb.Append(firstOp.Children[0].Name);
                    }
                    else
                    {
                        if (ixRegName.Equals("pc", Services.StringComparison))
                        {
                            Evaluations[0] = Services.Evaluator.Evaluate(firstOp, short.MinValue, ushort.MaxValue);
                            int relOffIx = instruction.Size + 1;
                            var relative = Services.Output.GetRelativeOffset((int)Evaluations[0], relOffIx);
                            if (relative > sbyte.MaxValue || relative < sbyte.MinValue)
                            {
                                relative = Services.Output.GetRelativeOffset((int)Evaluations[0], relOffIx + 1);
                                if (relative < short.MinValue || relative > short.MaxValue)
                                {
                                    if (!Services.PassNeeded)
                                        throw new ExpressionException(line.Operand.Position, "Offset is too far.");
                                    relative = short.MaxValue;
                                }
                                modes |= IndexModes.PC16;
                            }
                            else
                            {
                                modes |= IndexModes.PC8;
                            }
                            disSb.Append($"${(int)Evaluations[0] & 0xFFFF:x4}");
                            Evaluations[0] = relative;
                        }
                        else
                        {
                            Evaluations[0] = Services.Evaluator.Evaluate(firstOp, short.MinValue, short.MaxValue);
                            if (Evaluations[0] < 0)
                                disSb.Append('-');
                            if (Evaluations[0] >= sbyte.MinValue && Evaluations[0] <= sbyte.MaxValue)
                            {
                                disSb.Append($"${(int)Math.Abs(Evaluations[0]):x2}");
                                if (!modes.HasFlag(IndexModes.Indir) && Evaluations[0] >= -16 && Evaluations[0] <= 15)
                                {
                                    Evaluations[0] = (int)Evaluations[0] & 0x1F;
                                }
                                else
                                {
                                    modes |= IndexModes.Offset8;
                                    Evaluations[0] = (int)Evaluations[0] & 0xFF;
                                }
                            }
                            else
                            {
                                disSb.Append($"${(int)Math.Abs(Evaluations[0]):x4}");
                                modes |= IndexModes.Offset16;
                                Evaluations[0] = (int)Evaluations[0] & 0xFFFF;
                            }
                        }
                    }
                    if (s_ixRegisterModes.TryGetValue(ixRegName, out indexMode))
                        modes |= indexMode;
                    else if (!ixRegName.Equals("pc", Services.StringComparison) ||
                            offsRegMode != IndexModes.None)
                        throw new SyntaxException(secondOp.Children[0].Position,
                           $"\"{ixRegName}\" is not a valid register.");

                    disSb.Append($",{ixRegName}");
                }
            }
            if (modes.HasFlag(IndexModes.Indir))
                disSb.Append(']');

            if (offsRegMode == IndexModes.None)
            {
                if (modes.HasFlag(IndexModes.Inc1))
                {
                    if (Evaluations[0] < sbyte.MinValue ||
                    Evaluations[0] > byte.MaxValue ||
                    (modes & IndexModes.Offset16) == IndexModes.Offset16)
                    {
                        Evaluations[1] = ((int)Evaluations[0] & 0xFFFF) / 256;
                        Evaluations[2] = (int)Evaluations[0] & 0xFF;
                    }
                    else if (modes != IndexModes.ExtInd)
                    {
                        Evaluations[1] = Evaluations[0];
                    }
                    Evaluations[0] = (int)modes;
                }
                else
                {
                    Evaluations[0] = (int)modes | (int)Evaluations[0];
                }
            }
            else
            {
                Evaluations[0] = (int)modes;
            }
            Services.Output.Add(instruction.Opcode, instruction.Opcode.Size());

            foreach (var eval in Evaluations.Where(e => !double.IsNaN(e)))
                Services.Output.Add(eval, 1);
            if (!Services.PassNeeded)
            {
                var sb = new StringBuilder();
                if (!Services.Options.NoAssembly)
                {
                    var byteString = Services.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.');
                    sb.Append(byteString.PadRight(Padding));
                }
                else
                {
                    sb.Append($".{PCOnAssemble:x4}                        ");
                }
                if (!Services.Options.NoDisassembly)
                    sb.Append(disSb.ToString().PadRight(18));
                else
                    sb.Append("                  ");
                if (!Services.Options.NoSource)
                    sb.Append(line.UnparsedSource);
                return sb.ToString();
            }
            return string.Empty;
        }

        string AssemblePushPullExchange(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.OperandExpression))
                throw new SyntaxException(line.Instruction.Position,
                    "Missing register.");

            var isExchange = Reserved.IsOneOf("Exchanges",
                                    line.InstructionName);

            var lookup = isExchange ? s_exchangeModes : s_pushPullModes;

            byte registers = byte.MinValue;
            var registersEvaled = new SortedSet<string>(new PushPullComparer(!isExchange ? lookup : null));
            foreach (var child in line.Operand.Children)
            {
                if (child.Children.Count == 0)
                    throw new SyntaxException(child.Position,
                        "Missing register.");
                if (child.Children.Count > 1)
                    throw new SyntaxException(child.Position,
                        $"Invalid expression \"{child.ToString().Trim()}\".");

                var registerName = child.Children[0].Name;
                if (!lookup.TryGetValue(registerName, out var postbyte))
                    throw new SyntaxException(child.Position,
                        $"Invalid expression \"{child.ToString().Trim()}\".");
                if (registersEvaled.Contains(registerName))
                    throw new SyntaxException(child.Position,
                        $"Duplicate register \"{registerName}\".");
                if (isExchange && registersEvaled.Count > 1)
                    throw new SyntaxException(child.Position,
                        $"Unexpected expression \"{child.ToString().Trim()}\".");
                if (!isExchange && registerName[0] == line.InstructionName[^1])
                    throw new SyntaxException(child.Children[0].Position,
                        $"Cannot use \"{line.InstructionName}\" with register \"{registerName}\".");
                if (isExchange && registersEvaled.Count == 1)
                {
                    registers <<= 4;
                    registers |= postbyte;
                }
                else
                {
                    registers |= postbyte;
                }
                registersEvaled.Add(registerName);
            }
            var instr = ActiveInstructions[(line.InstructionName, Modes.ZeroPage)];
            Services.Output.Add(instr.Opcode, 1);
            Services.Output.Add(registers);
            if (!Services.PassNeeded)
            {
                var sb = new StringBuilder();
                if (!Services.Options.NoAssembly)
                {
                    var byteString = Services.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.');
                    sb.Append(byteString.PadRight(Padding));
                }
                else
                {
                    sb.Append($".{PCOnAssemble:x4}                        ");
                }
                if (!Services.Options.NoDisassembly)
                    sb.Append($"{line.InstructionName} {string.Join(',', registersEvaled)}".PadRight(18));
                if (!Services.Options.NoSource)
                    sb.Append(line.UnparsedSource);
                return sb.ToString();
            }
            return string.Empty;
        }

        public override bool Assembles(string s) => Reserved.IsReserved(s);

        public override bool IsReserved(string token)
            => base.IsReserved(token) || s_exchangeModes.ContainsKey(token);

        #endregion

        #region Properties

        protected override bool PseudoBranchSupported => false;

        #endregion
    }
}