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

using DotNetAsm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that adds pseudo-ops for 6502-specific architectures, such 
    /// as CBM and Apple ][ computers.
    /// </summary>
    public class Pseudo6502 : PseudoAssembler, ILineAssembler
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of Asm6502.Net.Pseudo6502 line assembler.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssembly controller associated 
        /// to this assembler.</param>
        public Pseudo6502(IAssemblyController controller) :
            base(controller)
        {
            Reserved.DefineType("TargetFunctions", new string[]
                {
                    "cbmscreen", "petscii", "atascreen"
                });

            Reserved.DefineType("TargetStrings", new string[]
                {
                    ".cbmscreen", ".petscii", ".atascreen"
                });

            Reserved.DefineType("ReturnAddress", new string[]
                {
                    ".rta"
                });

            controller.Evaluator.DefineSymbolLookup(@"(atascreen|cbmscreen|petscii)\(.+\)", EncodeStringFunc);
        }
        #endregion

        #region Members

        /// <summary>
        /// A callback to implement string functions to specific target architecture.
        /// </summary>
        /// <param name="function">The function name</param>
        /// <returns>Returns the value representation of the string or character.</returns>
        /// <exception cref="DotNetAsm.ExpressionEvaluator.ExpressionException">
        /// DotNetAsm.ExpressionEvaluator.ExpressionException
        /// </exception>
        private string EncodeStringFunc(string function)
        {
            int opix = function.IndexOf('(');
            string expression = new string(function.Skip(opix + 1).ToArray()).TrimEnd(')');
            Int64 val = Convert.ToInt64(expression);
            if (val < byte.MinValue || val > byte.MaxValue)
            {
                throw new OverflowException(expression);
            }
            if (opix == 9)
            {
                if (function.StartsWith("a", Controller.Options.StringComparison))
                    return EncodeAtari((byte)val).ToString();

                return EncodeCbm((byte)val).ToString();
            }
            return EncodePetscii((byte)val).ToString();
        }

        /// <summary>
        /// Encode an ASCII/UTF-8 byte into Atari screen code.
        /// </summary>
        /// <param name="b">The byte to convert</param>
        /// <returns>The converted value</returns>
        private byte EncodeAtari(byte b)
        {
            if (b < 96)
                return Convert.ToByte(b - 32);
            return b;
        }

        /// <summary>
        /// Encode an ASCII/UTF-8 byte into CBM screen code.
        /// </summary>
        /// <param name="b">The byte to convert</param>
        /// <returns>The converted value</returns>
        private byte EncodeCbm(byte b)
        {
                 if (b < 0x20) b += 128;
            else if (b >= 0x40 && b < 0x60) b -= 0x40;
            else if (b >= 0x60 && b < 0x80) b -= 0x20;
            else if (b >= 0x80 && b < 0xA0) b += 0x40;
            else if (b >= 0xA0 && b < 0xC0) b -= 0x40;
            else if (b >= 0xC0 && b < 0xFF) b -= 0x80;
            else if (b == 0xFF) b = 0x94;
            return b;
        }

        /// <summary>
        /// Encode an ASCII/UTF-8 byte into Commodore PETSCII code.
        /// </summary>
        /// <param name="b">The byte to convert</param>
        /// <returns>The converted value</returns>
        private byte EncodePetscii(byte b)
        {
            if (b >= Convert.ToByte('A') && b <= Convert.ToByte('Z'))
                b += 32;
            else if (b >= Convert.ToByte('a') && b <= Convert.ToByte('z'))
                b -= 32;
            return b;
        }

        public override void AssembleLine(SourceLine line)
        {
            string instruction = Controller.Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();

            if (instruction.Equals(".rta"))
            {
                var csv = line.CommaSeparateOperand();
                foreach(string rta in csv)
                {
                    if (rta.Equals("?"))
                    { 
                        Controller.Output.AddUninitialized(2); 
                    }
                    else
                    {
                        long val = Controller.Evaluator.Eval(rta, ushort.MinValue, ushort.MaxValue + 1);
                        Controller.Output.Add(val - 1, 2);
                    }
                }
                return;
            }

            if (instruction.Equals(".atascreen"))
            {
                Controller.Output.Transforms.Push(EncodeAtari);
            }
            else if (instruction.Equals(".cbmscreen"))
            {
                Controller.Output.Transforms.Push(EncodeCbm);
            }
            else if (instruction.Equals(".petscii"))
            {
                Controller.Output.Transforms.Push(EncodePetscii);
            }
            else
            {
                Controller.Log.LogEntry(line, ErrorStrings.UnknownInstruction, instruction);
                return;
            }
            AssembleStrings(line);

            Controller.Output.Transforms.Pop();
        }

        public override int GetInstructionSize(SourceLine line)
        {
            if (line.Instruction.Equals(".rta", Controller.Options.StringComparison))
                return 2;
            return GetExpressionSize(line);
        }

        public override bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsOneOf("TargetStrings", instruction) ||
                   Reserved.IsOneOf("ReturnAddress", instruction);
        }
        #endregion
    }
}
