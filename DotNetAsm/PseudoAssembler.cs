//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// An implementation of the <see cref="T:DotNetAsm.ILineAssembler"/> interface that assembles
    /// pseudo operations such as byte and string assembly.
    /// </summary>
    public sealed class PseudoAssembler : StringAssemblerBase, ILineAssembler
    {
        #region Members

        readonly HashSet<BinaryFile> _includedBinaries;
        readonly Dictionary<string, string> _typeDefs;
        readonly Func<string, bool> _reservedSymbol;
        readonly Func<string, bool> _instruction;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="T:DotNetAsm.PseudoAssembler"/> line assembler.
        /// </summary>
        /// <param name="isInstructionFunc">A function callback to determine if the given token is 
        /// an instruction.</param>
        /// <param name="reservedSymbolFunc">A function callback to determine if the given token 
        /// is a symbol name.</param>
        public PseudoAssembler(Func<string, bool> isInstructionFunc, Func<string, bool> reservedSymbolFunc)
        {
            _includedBinaries = new HashSet<BinaryFile>();

            Reserved.DefineType("PseudoOps",
                    ".addr", ".align", ".binary", ".byte", ".sbyte",
                    ".dint", ".dword", ".fill", ".lint", ".long",
                    ".sint", ".typedef", ".word"
                );
            _typeDefs = new Dictionary<string, string>(Assembler.Options.StringComparar);
            _instruction = isInstructionFunc;
            _reservedSymbol = reservedSymbolFunc;
        }

        #endregion

        #region Methods

        void AssembleFills(SourceLine line)
        {
            var csv = line.Operand.CommaSeparate();

            var alignval = (int)Assembler.Evaluator.Eval(csv.First(), ushort.MinValue, ushort.MaxValue);

            if (csv.Count > 1 && csv.Last().Equals("?") == false)
            {
                if (csv.Count > 2)
                {
                    Assembler.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                    return;
                }
                var fillval = Assembler.Evaluator.Eval(csv.Last(), int.MinValue, uint.MaxValue);

                if (line.Instruction.Equals(".align", Assembler.Options.StringComparison))
                    line.Assembly = Assembler.Output.Align(alignval, fillval);
                else
                    line.Assembly = Assembler.Output.Fill(alignval, fillval);
            }
            else
            {
                if (line.Instruction.ToLower().Equals(".align"))
                    Assembler.Output.Align(alignval);
                else
                    Assembler.Output.Fill(alignval);
            }
        }

        void AssembleValues(SourceLine line, long minval, long maxval, int size)
        {
            var tokens = line.Operand.CommaSeparate();
            if (line.Assembly.Count > 0)
                line.Assembly.Clear();
            foreach (var t in tokens)
            {
                if (t == "?")
                {
                    Assembler.Output.AddUninitialized(size);
                }
                else
                {
                    var val = Assembler.Evaluator.Eval(t, minval, maxval);
                    line.Assembly.AddRange(Assembler.Output.Add(val, size));
                }
            }
        }

        void GetBinaryOffsetSize(List<string> args, int binarysize, ref int offs, ref int size)
        {
            if (args.Count >= 2)
            {
                offs = (int)Assembler.Evaluator.Eval(args[1], ushort.MinValue, ushort.MaxValue);
                if (args.Count == 3)
                    size = (int)Assembler.Evaluator.Eval(args[2], ushort.MinValue, ushort.MaxValue);
            }
            if (offs > binarysize - 1)
                offs = binarysize - 1;
            if (size > binarysize - offs)
                size = binarysize - offs;
        }

        void AssembleBinaryBytes(SourceLine line)
        {
            var args = line.Operand.CommaSeparate();
            var binary = IncludeBinary(line, args);
            if (binary != null)
            {
                int offs = 0, size = binary.Data.Length;

                GetBinaryOffsetSize(args, size, ref offs, ref size);

                if (size > ushort.MaxValue)
                    Assembler.Log.LogEntry(line, ErrorStrings.IllegalQuantity, size);
                else
                    line.Assembly = Assembler.Output.AddBytes(binary.Data.Skip(offs), size);
            }
        }

        BinaryFile IncludeBinary(SourceLine line, List<string> args)
        {
            if (args.Count < 1 || args.Count > 3 || !args.First().EnclosedInQuotes(out string filename))
            {
                if (args.Count == 0)
                    Assembler.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                else if (args.Count > 3)
                    Assembler.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                else
                    Assembler.Log.LogEntry(line, ErrorStrings.FilenameNotSpecified);
                return null;
            }
            var binary = _includedBinaries.FirstOrDefault(b => b.Filename.Equals(filename));

            if (binary == null)
            {
                binary = new BinaryFile(args.First());

                if (binary.Open())
                {
                    _includedBinaries.Add(binary);
                }
                else
                {
                    Assembler.Log.LogEntry(line, ErrorStrings.CouldNotProcessBinary, args.First());
                    return null;
                }
            }
            return binary;
        }

        void DefineType(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Label) == false)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.None);
                return;
            }
            var csvs = line.Operand.CommaSeparate();
            if (csvs.Count != 2)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.None);
                return;
            }
            string currtype = csvs.First();
            if (!Reserved.IsOneOf("PseudoOps", currtype) &&
                !base.IsReserved(currtype))
            {
                Assembler.Log.LogEntry(line, ErrorStrings.DefininingUnknownType, currtype);
                return;
            }

            string newtype = csvs.Last();
            if (!Regex.IsMatch(newtype, @"^\.?" + Patterns.SymbolUnicode + "$"))
            {
                Assembler.Log.LogEntry(line, ErrorStrings.None);
            }
            else if (_instruction(newtype))
            {
                Assembler.Log.LogEntry(line, ErrorStrings.TypeDefinitionError, newtype);
            }
            else if (_reservedSymbol(newtype))
            {
                Assembler.Log.LogEntry(line, ErrorStrings.TypeNameReserved, newtype);
            }
            else
            {
                _typeDefs.Add(newtype, currtype);
                line.DoNotAssemble = true;
            }
        }

        public void AssembleLine(SourceLine line)
        {
            if (Assembler.Output.PCOverflow)
            {
                Assembler.Log.LogEntry(line,
                                        ErrorStrings.PCOverflow,
                                        Assembler.Output.LogicalPC);
                return;
            }
            var instruction = line.Instruction.ToLower();
            if (_typeDefs.ContainsKey(instruction))
                instruction = _typeDefs[instruction].ToLower();

            switch (instruction)
            {
                case ".addr":
                case ".word":
                    AssembleValues(line, ushort.MinValue, ushort.MaxValue, 2);
                    break;
                case ".align":
                case ".fill":
                    AssembleFills(line);
                    break;
                case ".binary":
                    AssembleBinaryBytes(line);
                    break;
                case ".byte":
                    AssembleValues(line, byte.MinValue, byte.MaxValue, 1);
                    break;
                case ".sbyte":
                    AssembleValues(line, sbyte.MinValue, sbyte.MaxValue, 1);
                    break;
                case ".dint":
                    AssembleValues(line, int.MinValue, int.MaxValue, 4);
                    break;
                case ".dword":
                    AssembleValues(line, uint.MinValue, uint.MaxValue, 4);
                    break;
                case ".lint":
                    AssembleValues(line, Int24.MinValue, Int24.MaxValue, 3);
                    break;
                case ".long":
                    AssembleValues(line, UInt24.MinValue, UInt24.MaxValue, 3);
                    break;
                case ".sint":
                    AssembleValues(line, short.MinValue, short.MaxValue, 2);
                    break;
                case ".typedef":
                    DefineType(line);
                    break;
                default:
                    AssembleStrings(line);
                    break;
            }
        }

        public int GetInstructionSize(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Assembler.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                return 0;
            }

            var csv = line.Operand.CommaSeparate();
            if (csv.Count == 0)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                return 0;
            }
            var instruction = line.Instruction.ToLower();
            if (_typeDefs.ContainsKey(instruction))
                instruction = _typeDefs[instruction].ToLower();

            switch (instruction)
            {
                case ".align":
                    {
                        var alignval = Assembler.Evaluator.Eval(csv.First());
                        return Compilation.GetAlignmentSize(Convert.ToUInt16(line.PC), Convert.ToUInt16(alignval));
                    }
                case ".binary":
                    {
                        int boffset = 0;
                        var binary = IncludeBinary(line, csv);
                        if (binary != null)
                        {
                            int bsize = binary.Data.Length;
                            GetBinaryOffsetSize(csv, bsize, ref boffset, ref bsize);
                            return bsize;
                        }
                        return 0;
                    }
                case ".byte":
                case ".sbyte":
                    return csv.Count;
                case ".dword":
                case ".dint":
                    return csv.Count * 4;
                case ".fill":
                    return (int)Assembler.Evaluator.Eval(csv.First(), ushort.MinValue, ushort.MaxValue);
                case ".long":
                case ".lint":
                    return csv.Count * 3;
                case ".cstring":
                case ".pstring":
                case ".nstring":
                case ".lsstring":
                case ".string":
                    return GetExpressionSize(line);
                case ".addr":
                case ".sint":
                case ".word":
                    return csv.Count * 2;
                default:
                    return 0;
            }
        }

        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsOneOf("PseudoOps", instruction) ||
                base.IsReserved(instruction) ||
                _typeDefs.ContainsKey(instruction);
        }
        #endregion
    }
}