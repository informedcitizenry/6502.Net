//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
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

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that will output 6502.Net psuedo-ops into Controller.Output.
    /// </summary>
    public class PseudoOps6502 : AssemblerBase, ILineAssembler
    {
        #region Members

        private TextEncoding encoding_;

        private HashSet<BinaryFile> includedBinaries_;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a 6502.Net Pseudo-operation line assembler.
        /// </summary>
        /// <param name="controller">The assembly controller</param>
        public PseudoOps6502(IAssemblyController controller) :
            base(controller)
        {
            encoding_ = TextEncoding.None;

            includedBinaries_ = new HashSet<BinaryFile>();

            Reserved.Types.Add("PseudoOps", new HashSet<string>(new string[]
                {
                    ".addr", ".byte", ".char", ".dint", ".dword", ".enc", ".fill", ".lint", ".long", ".cstring", 
                    ".pstring", ".nstring", ".string", ".word", ".binary", 
                    ".align", 
                    ".repeat", ".rta", ".sint",
                    ".lsstring"
                }));
        }

        /// <summary>
        /// Callback method that handles text encoding for output.
        /// </summary>
        /// <param name="b">The byte to enocde.</param>
        /// <returns>The encoded byte.</returns>
        private byte EncodeString(byte b)
        {
            if (encoding_ == TextEncoding.Screen)
            {
                if (b < 0x20) b += 128;
                else if (b >= 0x40 && b < 0x60) b -= 0x40;
                else if (b >= 0x60 && b < 0x80) b -= 0x20;
                else if (b >= 0x80 && b < 0xA0) b += 0x40;
                else if (b >= 0xA0 && b < 0xC0) b -= 0x40;
                else if (b >= 0xC0 && b < 0xFF) b -= 0x80;
                else if (b == 0xFF) b = 0x94;
            }
            else if (encoding_ == TextEncoding.Petscii)
            {
                if (b >= Convert.ToByte('A') && b <= Convert.ToByte('Z'))
                    b += 32;
                else if (b >= Convert.ToByte('a') && b <= Convert.ToByte('z'))
                    b -= 32;
            }
            return b;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assemble strings to the output.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        private void AssembleStrings(SourceLine line)
        {
            string format = line.Instruction;

            if (format.Equals(".pstring", Controller.Options.StringComparison))
            {
                // we need to get the instruction size for the whole length, including all args
                int length = GetInstructionSize(line) - 1;
                if (length > 255)
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.PStringSizeTooLarge);
                    return;
                }
                else if (length < 0)
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments);
                    return;
                }
                else
                {
                    Controller.Output.Add(length, 1);
                }
            }
            else if (format.Equals(".lsstring", Controller.Options.StringComparison))
            {
                Controller.Output.Transforms.Push(delegate(byte b)
                {
                    if (b > 0x7F)
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.MSBShouldNotBeSet, b.ToString());
                        return 0;
                    }
                    return b <<= 1;
                });
            }
            string operand = line.Operand;

            Controller.Output.Transforms.Push(EncodeString);

            var args = line.CommaSeparateOperand();

            foreach (var arg in args)
            {

                if (arg.EnclosedInQuotes() == false)
                {
                    if (arg == "?")
                    {
                        Controller.Output.AddUninitialized(1);
                        continue;
                    }

                    int size = 1;
                    Int64 val;

                    var m = Regex.Match(arg, @"str(\((.+)\))", Controller.Options.RegexOption);
                    if (string.IsNullOrEmpty(m.Value) == false)
                    {
                        if (m.Groups[1].Value != arg.Substring(3))
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression, arg);
                            continue;
                        }
                        else
                        {
                            var param = m.Groups[2].Value;
                            val = Controller.Evaluator.Eval(param);
                            Controller.Output.Add(val.ToString());
                            continue;
                        }
                    }

                    val = Controller.Evaluator.Eval(arg);
                    size = val.Size();
                    var convbytes = BitConverter.GetBytes(val).ToList();

                    Controller.Output.AddBytes(convbytes, size, false);

                    if (format.Equals(".lsstring", Controller.Options.StringComparison))
                        Controller.Output.ChangeLast(convbytes.Last() + 1, 1);
                    else if (format.Equals(".nstring", Controller.Options.StringComparison))
                        Controller.Output.ChangeLast(convbytes.Last() | 0x80, 1);
                }
                else
                {
                    string noquotes = arg.Trim('"');
                    if (string.IsNullOrEmpty(noquotes))
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments);
                        return;
                    }
                    Controller.Output.Add(noquotes);
                    var lastbyte = Controller.Output.GetCompilation().Last();
                    if (format.Equals(".lsstring", Controller.Options.StringComparison))
                        Controller.Output.ChangeLast(lastbyte + 1, 1);
                    else if (format.Equals(".nstring", Controller.Options.StringComparison))
                        Controller.Output.ChangeLast(lastbyte | 0x80, 1);

                }
            }
            Controller.Output.Transforms.Pop(); // clean up

            if (format.Equals(".cstring", Controller.Options.StringComparison))
                Controller.Output.Add(0, 1);
            else if (format.Equals(".lsstring", Controller.Options.StringComparison))
                Controller.Output.Transforms.Pop(); // clean up again :)
        }

        /// <summary>
        /// Assemble multiple values to the output.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        private void AssembleFills(SourceLine line)
        {
            var csv = line.CommaSeparateOperand();
            if (csv.Count == 0 || (csv.Count == 1 && line.Instruction.ToLower().Equals(".repeat")))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments, line.Instruction);
                return;
            }
          
            Int64 alignval = Controller.Evaluator.Eval(csv.First());
            if (alignval < 1)
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression, alignval.ToString());
                return;
            }

            if (csv.Count > 1)
            {
                if (csv.Count > 2)
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.TooManyArguments, line.Instruction);
                    return;
                }
                Int64 fillval = Controller.Evaluator.Eval(csv.Last());

                if (line.Instruction.Equals(".align", Controller.Options.StringComparison))
                {
                    Controller.Output.Align(Convert.ToUInt16(alignval), fillval);
                }
                else
                {
                    bool repeat = line.Instruction.Equals(".repeat", Controller.Options.StringComparison);
                    Controller.Output.Fill(Convert.ToUInt16(alignval), fillval, repeat);
                }
            }
            else
            {
                if (line.Instruction.Equals(".align", Controller.Options.StringComparison))
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
        private void AssembleValues(SourceLine line, long minval, long maxval, int size)
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
                    Int64 val = Controller.Evaluator.Eval(t);
                    if (line.Instruction.Equals(".rta", Controller.Options.StringComparison))
                        val -= 1;
                    if (val < minval || val > maxval)
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.IllegalQuantity, val.ToString());
                        return;
                    }
                    Controller.Output.Add(val, size);
                }
            }
        }

        /// <summary>
        /// Process the .enc directive.
        /// </summary>
        /// <param name="line">The SourceLine to assemble</param>
        private void GetEncoding(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments, line.Instruction);
                return;
            }
            if (line.Operand.Equals("screen", Controller.Options.StringComparison))
            {
                encoding_ = TextEncoding.Screen;
            }
            else if (line.Operand.Equals("petscii", Controller.Options.StringComparison))
            {
                encoding_ = TextEncoding.Petscii;
            }
            else if (line.Operand.Equals("none", Controller.Options.StringComparison))
            {
                encoding_ = TextEncoding.None;
            }
            else
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.InvalidParamRef, line.Operand);
                return;
            }
            line.IsDefinition = true;
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
            if (size > ushort.MaxValue)
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.IllegalQuantity, size.ToString());
                return;
            }
            if (offs > binary.Data.Count - 1)
                offs = binary.Data.Count - 1;
            if (size > binary.Data.Count - offs)
                size = binary.Data.Count - offs;
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
            if (args.Count > 3)
                Controller.Log.LogEntry(line, Resources.ErrorStrings.TooManyArguments, line.Operand);
            if (args.First().EnclosedInQuotes() == false)
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.FilenameNotSpecified);
                return null;
            }

            BinaryFile binary = new BinaryFile(args.First());
            if (binary.Open() == false)
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.CouldNotProcessBinary, args.First());
                return null;
            }
            includedBinaries_.Add(binary);
            return binary;
        }

        #region ILineAssembler.Methods

        /// <summary>
        /// Asssembles the pseudo-op of a source line into Controller.Output.
        /// </summary>
        /// <param name="line">The source line to assemble.</param>
        public void AssembleLine(SourceLine line)
        {
            if (Controller.Output.PCOverflow)
            {
                Controller.Log.LogEntry(line, 
                                        Resources.ErrorStrings.PCOverflow, 
                                        Controller.Output.GetPC().ToString());
                return;
            }
            Controller.Evaluator.CharEncoding = EncodeString;

            switch (line.Instruction.ToLower())
            {
                case ".addr":
                case ".rta":
                case ".word":
                    AssembleValues(line, ushort.MinValue, ushort.MaxValue, 2);
                    break;
                case ".align":
                case ".fill":
                case ".repeat":
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
                case ".enc":
                    GetEncoding(line);
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
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.UnknownInstruction, line.Instruction);
                    break;
            }
        }

        /// <summary>
        /// Gets the size of the instruction in the source line.
        /// </summary>
        /// <param name="line">The source line to query.</param>
        /// <returns>Returns the size in bytes of the instruction or directive.</returns>
        public int GetInstructionSize(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments, line.Instruction);
                return 0;
            }

            var csv = line.CommaSeparateOperand();
            switch (line.Instruction.ToLower())
            {
                case ".align":
                    {
                        Int64 alignval = Controller.Evaluator.Eval(csv.First());
                        return Compilation.GetAlignmentSize(line.PC, Convert.ToUInt16(alignval));
                    }
                case ".binary":
                    {
                        Int64 boffset = 0;
                        Int64 bsize = 0;
                        var binary = IncludeBinary(line);
                        if (csv.Count > 1)
                        {
                            boffset = Controller.Evaluator.Eval(csv[1]);
                            if (boffset > ushort.MaxValue)
                            {
                                Controller.Log.LogEntry(line, Resources.ErrorStrings.PCOverflow, boffset.ToString());
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
                case ".repeat":
                    {
                        var fillamount = Controller.Evaluator.Eval(csv.First());
                        if (csv.Count < 2)
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments, line.Instruction);
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
                    {
                        int numbytes = 0;
                        foreach (var t in csv)
                        {
                            if (t.EnclosedInQuotes())
                            {
                                numbytes += t.Length - 2;
                            }
                            else
                            {
                                if (t == "?")
                                {
                                    numbytes++;
                                }
                                else
                                {
                                    Int64 v;
                                    var m = Regex.Match(t, @"str(\(.+\))", Controller.Options.RegexOption);
                                    if (string.IsNullOrEmpty(m.Value) == false)
                                    {
                                        string exp = m.Groups[1].Value;
                                        if (ExpressionEvaluator.FirstParenGroup(exp, true) !=
                                            exp)
                                        {
                                            Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression, exp);
                                            return 0;
                                        }
                                        v = Controller.Evaluator.Eval(m.Groups[1].Value);
                                        numbytes += v.ToString().Length;
                                        continue;
                                    }
                                    v = Controller.Evaluator.Eval(t);
                                    numbytes += v.Size();
                                }
                            }
                        }
                        if (line.Instruction.Equals(".cstring", Controller.Options.StringComparison) ||
                            line.Instruction.Equals(".pstring", Controller.Options.StringComparison))
                            numbytes++;
                        return numbytes;
                    }
                case ".addr":
                case ".rta":
                case ".sint":
                case ".word":
                    return csv.Count * 2;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Indicates whether this line assembler will assemble the 
        /// given instruction or directive.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <returns>True, if the line assembler can assemble the source, 
        /// otherwise false.</returns>
        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsReserved(instruction);
        }

        public bool HandleFirstPass(SourceLine line)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
