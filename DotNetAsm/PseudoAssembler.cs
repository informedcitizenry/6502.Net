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
        private HashSet<BinaryFile> _includedBinaries;

        #region Constructors

        /// <summary>
        /// Constructs an instance of a 6502.Net Pseudo-operation line assembler.
        /// </summary>
        /// <param name="controller">The assembly controller</param>
        public PseudoAssembler(IAssemblyController controller) :
            base(controller)
        {
            _includedBinaries = new HashSet<BinaryFile>();

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

            int alignval = (int)Controller.Evaluator.Eval(csv.First(), ushort.MinValue, ushort.MaxValue);
            
            if (csv.Count > 1 && csv.Last().Equals("?") == false)
            {
                if (csv.Count > 2)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                    return;
                }
                Int64 fillval = Controller.Evaluator.Eval(csv.Last(), int.MinValue, uint.MaxValue);

                if (line.Instruction.Equals(".align", Controller.Options.StringComparison))
                    Controller.Output.Align(alignval, fillval);
                else
                    Controller.Output.Fill(alignval, fillval);
            }
            else
            {
                if (line.Instruction.ToLower().Equals(".align"))
                    Controller.Output.Align(alignval);
                else
                    Controller.Output.Fill(alignval);
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
        /// Get the offset and size of the operand of a .binary file
        /// </summary>
        /// <param name="args">The System.Collections.Generic.List&lt;string&gt; arguments</param>
        /// <param name="binarysize">The size of the binary file</param>
        /// <param name="offs">The offset</param>
        /// <param name="size">The size</param>
        private void GetBinaryOffsetSize(List<string> args, int binarysize, ref int offs, ref int size)
        {
            if (args.Count >= 2)
            {
                offs = (int)Controller.Evaluator.Eval(args[1], ushort.MinValue, ushort.MaxValue);
                if (args.Count == 3)
                    size = (int)Controller.Evaluator.Eval(args[2], ushort.MinValue, ushort.MaxValue);
            }
            if (offs > binarysize - 1)
                offs = binarysize - 1;
            if (size > binarysize - offs)
                size = binarysize - offs;
        }

        /// <summary>
        /// Assemble an included binary file's bytes.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        private void AssembleBinaryBytes(SourceLine line)
        {
            var args = line.CommaSeparateOperand();
            var binary = _includedBinaries.FirstOrDefault(b => b.Filename.Equals(args[0].Trim('"')));
            if (binary == null)
                throw new Exception("Unable to find binary file " + args[0]);
            int offs = 0, size = binary.Data.Count;
            GetBinaryOffsetSize(args, size, ref offs, ref size);
            if (size > ushort.MaxValue)
            {
                Controller.Log.LogEntry(line, ErrorStrings.IllegalQuantity, size.ToString());
                return;
            }
            Controller.Output.AddBytes(binary.Data.Skip(offs), size);
        }

        /// <summary>
        /// Process the .binary directive and cache the binary file's contents
        /// for later use.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        /// <returns>Returns a binary file.</returns>
        private BinaryFile IncludeBinary(SourceLine line, List<string> args)
        {
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
            BinaryFile binary = _includedBinaries.FirstOrDefault(b => b.Filename.Equals(filename));

            if (binary == null)
            {
                binary = new BinaryFile(args.First());

                if (binary.Open())
                    _includedBinaries.Add(binary);
                else
                    Controller.Log.LogEntry(line, ErrorStrings.CouldNotProcessBinary, args.First());
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
                        int boffset = 0;
                        var binary = IncludeBinary(line, csv);
                        int bsize = binary.Data.Count;
                        GetBinaryOffsetSize(csv, bsize, ref boffset, ref bsize);
                        return bsize;
                    }
                case ".byte":
                case ".char":
                    return csv.Count;
                case ".dword":
                case ".dint":
                    return csv.Count * 4;
                case ".fill":
                    return (int)Controller.Evaluator.Eval(csv.First(), ushort.MinValue, ushort.MaxValue);
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
