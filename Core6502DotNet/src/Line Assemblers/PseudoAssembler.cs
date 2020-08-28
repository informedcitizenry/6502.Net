//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for multi-byte assembly, including string types.
    /// </summary>
    public sealed class PseudoAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Members

        readonly Dictionary<string, BinaryFile> _includedBinaries;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="PseudoAssembler"/> line assembler.
        /// </summary>
        public PseudoAssembler()
        {
            _includedBinaries = new Dictionary<string, BinaryFile>();

            Reserved.DefineType("Types",
                    ".addr", ".align", ".binary", ".byte", ".sbyte",
                    ".char", ".dint", ".dword", ".fill", ".lint",
                    ".long", ".rta", ".short", ".sint", ".word",
                    ".cstring", ".lstring", ".nstring", ".pstring",
                    ".string"
                );

            Evaluator.AddFunctionEvaluator(this);
        }

        #endregion

        #region Methods

        static void AssembleFills(SourceLine line)
        {
            if (line.Operand.Children[0].Children.Count == 0)
            {
                Assembler.Log.LogEntry(line, $"Instruction \"{line.InstructionName}\" expects a value but none was given");
                return;
            }

            var alignval = (int)Evaluator.Evaluate(line.Operand.Children[0].Children, 1, ushort.MaxValue);
            if (line.Operand.Children.Count > 1 && !line.Operand.Children[1].ToString().Equals("?"))
            {
                if (line.Operand.Children.Count > 2)
                {
                    Assembler.Log.LogEntry(line, line.Operand, $"Too many arguments specified for instruction \"{line.InstructionName}\".");
                    return;
                }
                var fillval = (int)Evaluator.Evaluate(line.Operand.Children[1]);
                if (line.InstructionName.Equals(".align"))
                    Assembler.Output.Align(alignval, fillval);
                else
                    Assembler.Output.Fill(alignval, fillval);
            }
            else
            {
                if (line.InstructionName.Equals(".align"))
                    Assembler.Output.Align(alignval);
                else
                    Assembler.Output.Fill(alignval);
            }
        }

        static void AssembleValues(SourceLine line, long minValue, long maxValue, int setSize)
            => AssembleValues(line, minValue, maxValue, setSize, false);

        static void AssembleValues(SourceLine line, long minValue, long maxValue, int setSize, bool isRta)
        {
            if (line.Operand.Children.Count == 0)
            {
                Assembler.Log.LogEntry(line, line.Operand, $"Instruction \"{line.InstructionName}\" expects one or more arguments.");
                return;
            }
            foreach (Token child in line.Operand.Children)
            {
                if (child.Children.Count == 0)
                {
                    Assembler.Log.LogEntry(line, child.Position, "Expression expected.");
                    return;
                }
                var firstInExpression = child.Children[0];
                if (firstInExpression.Name.Equals("?"))
                {
                    if (child.Children.Count > 1)
                        Assembler.Log.LogEntry(line, child.Children[1].Position,
                            $"Unexpected expression \"{child.Children[1].Name}\".");
                    Assembler.Output.AddUninitialized(setSize);
                }
                else
                {
                    var val = Evaluator.Evaluate(child.Children, minValue, maxValue);
                    if (isRta)
                        val = ((int)(val - 1)) & 0xFFFF;
                    Assembler.Output.Add(val, setSize);
                }
            }
        }

        void AssembleBinaryFile(SourceLine line)
        {
            if (!line.OperandHasToken)
                throw new SyntaxException(line.Instruction.Position, "Filename not specified.");

            BinaryFile file;
            var filename = line.Operand.Children[0].Children[0].Name;
            if (_includedBinaries.ContainsKey(filename))
            {
                file = _includedBinaries[filename];
            }
            else
            {
                if (!filename.EnclosedInDoubleQuotes())
                    throw new SyntaxException(line.Operand.Position, "Filename not given in quotes.");
                file = new BinaryFile(filename.TrimOnce('"'));
                if (!file.Open())
                    throw new SyntaxException(line.Operand.Position, $"Unable to open file \"{filename}\".");
                _includedBinaries.Add(filename, file);
            }

            var offset = 0;
            var size = file.Data.Length;
            if (size > ushort.MaxValue)
                throw new ExpressionException(line.Operand.Position, "File size is too large.");
            if (line.Operand.Children.Count > 1)
            {
                if (line.Operand.Children.Count > 2)
                {
                    if (line.Operand.Children.Count > 3)
                        throw new SyntaxException(line.Operand.Children[3].Position, "Too many arguments specified for directive.");
                    size = (int)Evaluator.Evaluate(line.Operand.Children[2].Children, ushort.MinValue, ushort.MaxValue);
                }
                offset = (int)Evaluator.Evaluate(line.Operand.Children[1].Children, ushort.MinValue, ushort.MaxValue);

            }
            if (offset > size - 1)
                offset = size - 1;
            if (size > file.Data.Length - offset)
                size = file.Data.Length - offset;

            if (size > ushort.MaxValue)
                throw new ExpressionException(line.Operand.Position, $"Difference between specified offset and size is greater than the maximum allowed amount.");

            Assembler.Output.AddBytes(file.Data.Skip(offset), size);
        }

        static void AssembleStrings(SourceLine line)
        {
            if (!line.OperandHasToken)
            {
                throw new SyntaxException(line.Instruction.Position,
                    $"Instruction \"{line.InstructionName}\" expects one or more string arguments.");
            }
            var stringBytes = new List<byte>();
            foreach (Token child in line.Operand.Children)
            {
                if (child.Children.Count == 0)
                    throw new SyntaxException(child.Position, $"Expected value for instruction \"{line.InstructionName}\".");
                var element = child.Children[0];
                if (element.ToString().Equals("?"))
                {
                    if (child.Children.Count > 1)
                        Assembler.Log.LogEntry(line, child.Children[1].Position,
                            $"Unexpected expression \"{child.Children[1].Name}\".");
                    Assembler.Output.AddUninitialized(1);
                }
                else
                {
                    if (StringHelper.ExpressionIsAString(child))
                    {
                        stringBytes.AddRange(Assembler.Encoding.GetBytes(StringHelper.GetString(child)));
                    }
                    else 
                    {
                        if (Assembler.SymbolManager.SymbolExists(element.ToString()))
                        {
                            var symVal = Assembler.SymbolManager.GetStringValue(element);
                            if (!string.IsNullOrEmpty(symVal))
                                stringBytes.AddRange(Assembler.Encoding.GetBytes(StringHelper.GetString(child.Children[0])));
                            else
                                stringBytes.AddRange(BinaryOutput.ConvertToBytes(Assembler.SymbolManager.GetNumericValue(element)));
                        }
                        else
                        {
                            stringBytes.AddRange(BinaryOutput.ConvertToBytes(Evaluator.Evaluate(child)));
                        }   
                    }
                }
            }
            switch (line.InstructionName)
            {
                case ".cstring":
                    stringBytes.Add(0x00);
                    break;
                case ".pstring":
                    if (stringBytes.Count > 255)
                        throw new ExpressionException(line.Operand.Position, $"String expression exceeds the maximum length of \".pstring\" directive.");

                    stringBytes.Insert(0, Convert.ToByte(stringBytes.Count));
                    break;
                case ".lstring":
                case ".nstring":
                    if (stringBytes.Any(b => b > 0x7f))
                        throw new ExpressionException(line.Operand.Position, $"One or more elements in expression \"{line.Operand}\" exceeds maximum value.");
                    if (line.InstructionName.Equals(".lstring"))
                    {
                        stringBytes = stringBytes.Select(b => Convert.ToByte(b << 1)).ToList();
                        stringBytes[^1] |= 1;
                    }
                    else
                    {
                        stringBytes[^1] |= 0x80;
                    }
                    break;
                default:
                    break;
            }
            Assembler.Output.AddBytes(stringBytes);
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            switch (line.InstructionName)
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
                    AssembleBinaryFile(line);
                    break;
                case ".byte":
                    AssembleValues(line, byte.MinValue, byte.MaxValue, 1);
                    break;
                case ".sbyte":
                case ".char":
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
                case ".rta":
                    AssembleValues(line, short.MinValue, ushort.MaxValue, 2, true);
                    break;
                case ".sint":
                case ".short":
                    AssembleValues(line, short.MinValue, short.MaxValue, 2);
                    break;
                default:
                    AssembleStrings(line);
                    break;
            }
            if (Assembler.PassNeeded || string.IsNullOrEmpty(Assembler.Options.ListingFile))
                return string.Empty;
            return StringHelper.GetByteDisassembly(line, PCOnAssemble);
        }

        public bool EvaluatesFunction(Token function) => function.Name.Equals("format");

        public double EvaluateFunction(Token unused, Token parameters)
        {
            var str = StringHelper.GetStringFormat(parameters);
            return Assembler.Encoding.GetEncodedValue(str);
        }

        public void InvokeFunction(Token unused, Token parameters)
            => _ = EvaluateFunction(unused, parameters);

        public bool IsFunctionName(string symbol) => symbol.Equals("format");

        #endregion
    }
}
