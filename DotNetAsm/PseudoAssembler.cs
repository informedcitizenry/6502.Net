//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// An implementation of the DotNetAsm.ILineAssembler interface that assembles
    /// pseudo operations such as byte and string assembly.
    /// </summary>
    public class PseudoAssembler : StringAssemblerBase, ILineAssembler
    {
        private HashSet<BinaryFile> includedBinaries_;

        #region Constructors

        /// <summary>
        /// Constructs an instance of a 6502.Net Pseudo-operation line assembler.
        /// </summary>
        /// <param name="controller">The assembly controller</param>
        public PseudoAssembler(IAssemblyController controller) :
            base(controller)
        {
            includedBinaries_ = new HashSet<BinaryFile>();

            Reserved.DefineType("PseudoOps", new string[]
                {
                    ".addr", ".align", ".binary", ".byte", ".char",   
                    ".dint", ".dword", ".fill", ".lint", ".long", 
                    ".sint", ".word"
                });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assemble multiple values to the output.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        private void AssembleFills(SourceLine line)
        {
            var csv = line.CommaSeparateOperand();

            Int64 alignval = Controller.Evaluator.Eval(csv.First());
            if (alignval < 1)
            {
                Controller.Log.LogEntry(line, ErrorStrings.BadExpression, alignval.ToString());
                return;
            }

            if (csv.Count > 1 && csv.Last().Equals("?") == false)
            {
                if (csv.Count > 2)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                    return;
                }
                Int64 fillval = Controller.Evaluator.Eval(csv.Last());

                if (line.Instruction.Equals(".align", Controller.Options.StringComparison))
                {
                    Controller.Output.Align(Convert.ToUInt16(alignval), fillval);
                }
                else
                {
                    bool repeat = line.Instruction.Equals(".rep", Controller.Options.StringComparison);
                    Controller.Output.Fill(Convert.ToUInt16(alignval), fillval, repeat);
                }
            }
            else
            {
                if (line.Instruction.ToLower().Equals(".rep"))
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                else if (line.Instruction.ToLower().Equals(".align"))
                    Controller.Output.Align(Convert.ToUInt16(alignval));
                else
                    Controller.Output.Fill(Convert.ToUInt16(alignval));
            }
        }

        /// <summary>
        /// Assemble scalar values to the output.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        /// <param name="minval">The minimum value based on the type.</param>
        /// <param name="maxval">The maximum value based on the type.</param>
        /// <param name="size">The precise size in bytes of the assembled value.</param>
        protected void AssembleValues(SourceLine line, long minval, long maxval, int size)
        {
            var tokens = line.CommaSeparateOperand();
            foreach (var t in tokens)
            {
                if (t == "?")
                {
                    Controller.Output.AddUninitialized(size);
                }
                else
                {
                    Int64 val = Controller.Evaluator.Eval(t, minval, maxval);
                    Controller.Output.Add(val, size);
                }
            }
        }

        /// <summary>
        /// Assemble an included binary file's bytes.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        private void AssembleBinaryBytes(SourceLine line)
        {
            var args = line.CommaSeparateOperand();
            var binary = includedBinaries_.FirstOrDefault(b => b.Filename.Equals(args[0].Trim('"')));
            if (binary == null)
                throw new Exception("Unable to find binary file " + args[0]);
            Int64 offs = 0, size = binary.Data.Count;

            if (args.Count >= 2)
            {
                offs = Controller.Evaluator.Eval(args[1]);
                if (args.Count == 3)
                    size = Controller.Evaluator.Eval(args[2]);
            }
            if (offs > binary.Data.Count - 1)
                offs = binary.Data.Count - 1;
            if (size > binary.Data.Count - offs)
                size = binary.Data.Count - offs;
            if (size > ushort.MaxValue)
            {
                Controller.Log.LogEntry(line, ErrorStrings.IllegalQuantity, size.ToString());
                return;
            }
            Controller.Output.AddBytes(binary.Data.Skip(Convert.ToUInt16(offs)), Convert.ToUInt16(size));
        }

        /// <summary>
        /// Process the .binary directive and cache the binary file's contents
        /// for later use.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        /// <returns>Returns a binary file.</returns>
        private BinaryFile IncludeBinary(SourceLine line)
        {
            var args = line.CommaSeparateOperand();
            if (args.Count == 0 || args.First().EnclosedInQuotes() == false)
            {
                if (args.Count == 0)
                    Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                else
                    Controller.Log.LogEntry(line, ErrorStrings.FilenameNotSpecified);
                return null;
            }
            else if (args.Count > 3)
            {
                Controller.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Operand);
            }
            string filename = args.First().Trim('"');
            BinaryFile binary = includedBinaries_.FirstOrDefault(b => b.Filename.Equals(filename));

            if (binary == null)
            {
                binary = new BinaryFile(args.First());

                if (binary.Open() == false)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.CouldNotProcessBinary, args.First());
                    return null;
                }
                includedBinaries_.Add(binary);
            }
            return binary;
        }

        public virtual void AssembleLine(SourceLine line)
        {
            if (Controller.Output.PCOverflow)
            {
                Controller.Log.LogEntry(line, 
                                        ErrorStrings.PCOverflow, 
                                        Controller.Output.GetPC().ToString());
                return;
            }
            string instruction = Controller.Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();
            switch (instruction)
            {
                case ".addr":
                case ".word":
                    AssembleValues(line, ushort.MinValue, ushort.MaxValue, 2);
                    break;
                case ".align":
                case ".fill":
                case ".rep":
                    AssembleFills(line);
                    break;
                case ".binary":
                    AssembleBinaryBytes(line);
                    break;
                case ".byte":
                    AssembleValues(line, byte.MinValue, byte.MaxValue, 1);
                    break;
                case ".char":
                    AssembleValues(line, sbyte.MinValue, sbyte.MaxValue, 1);
                    break;
                case ".cstring":
                case ".lsstring":
                case ".nstring":
                case ".pstring":
                case ".string":
                    AssembleStrings(line);
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
                default:
                    Controller.Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
                    break;
            }
        }

        public virtual int GetInstructionSize(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                return 0;
            }

            var csv = line.CommaSeparateOperand();
            if (csv.Count == 0)
            {
                Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                return 0;
            }
            string instruction = Controller.Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();
            switch (instruction)
            {
                case ".align":
                    {
                        Int64 alignval = Controller.Evaluator.Eval(csv.First());
                        return Compilation.GetAlignmentSize(line.PC, Convert.ToUInt16(alignval));
                    }
                case ".binary":
                    {
                        Int64 boffset = 0;
                        var binary = IncludeBinary(line);
                        Int64 bsize = binary.Data.Count;
                        if (csv.Count > 1)
                        {
                            boffset = Controller.Evaluator.Eval(csv[1]);
                            if (boffset < 0)
                            {
                                Controller.Log.LogEntry(line, ErrorStrings.IllegalQuantity, boffset.ToString());
                                return 0;
                            }
                            if (csv.Count > 2)
                                bsize = Controller.Evaluator.Eval(csv.Last());
                            else
                                bsize = binary.Data.Count;
                            if (bsize > binary.Data.Count - boffset)
                                bsize = binary.Data.Count - boffset;
                        }
                        return Convert.ToUInt16(bsize);
                    }
                case ".byte":
                case ".char":
                    return csv.Count;
                case ".dword":
                case ".dint":
                    return csv.Count * 4;
                case ".fill":
                    return Convert.ToUInt16(Controller.Evaluator.Eval(csv.First()));
                case ".rep":
                    {
                        var fillamount = Controller.Evaluator.Eval(csv.First());
                        if (csv.Count < 2)
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                            return 0;
                        }
                        var fillval = Controller.Evaluator.Eval(csv[1]);
                        var size = fillval.Size() * fillamount;
                        return Convert.ToUInt16(size);
                    }
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

        public virtual bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsOneOf("PseudoOps", instruction) || base.IsReserved(instruction);
        }
        #endregion
    }
}
