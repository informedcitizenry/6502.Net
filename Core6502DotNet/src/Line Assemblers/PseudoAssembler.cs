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

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for multi-byte assembly, including string types.
    /// </summary>
    public sealed class PseudoAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Constants

        const int IeeeBias = 1023;
        
        const int CbmBias = 129;

        #endregion

        #region Members

        readonly Dictionary<string, BinaryFile> _includedBinaries;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="PseudoAssembler"/> line assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public PseudoAssembler(AssemblyServices services)
            :base(services)
        {
            _includedBinaries = new Dictionary<string, BinaryFile>();

            Reserved.DefineType("Types",
                    ".addr", ".align", ".binary", ".byte", ".sbyte",
                    ".char", ".dint", ".dword", ".fill", ".lint",
                    ".long", ".rta", ".short", ".sint", ".word",
                    ".cstring", ".lstring", ".nstring", ".pstring",
                    ".string", ".cbmflt", ".cbmfltp"
                );

            Reserved.DefineType("Functions",
                "cbmflt", "cbmfltp", "format", "peek", "poke", "section");

            Services.Evaluator.AddFunctionEvaluator(this);
        }

        #endregion

        #region Methods

        void AssembleFills(SourceLine line)
        {
            var alignval = (int)Services.Evaluator.Evaluate(line.Operand.Children[0].Children, 1, ushort.MaxValue);
            if (line.Operand.Children.Count > 1 && !line.Operand.Children[1].ToString().Trim().Equals("?"))
            {
                if (line.Operand.Children.Count > 2)
                {
                    Services.Log.LogEntry(line, line.Operand, $"Too many arguments specified for instruction \"{line.InstructionName}\".");
                    return;
                }
                var fillval = (int)Services.Evaluator.Evaluate(line.Operand.Children[1]);
                if (line.InstructionName.Equals(".align"))
                    Services.Output.Align(alignval, fillval);
                else
                    Services.Output.Fill(alignval, fillval);
            }
            else
            {
                if (line.InstructionName.Equals(".align"))
                    Services.Output.Align(alignval);
                else
                    Services.Output.Fill(alignval);
            }
        }

        void AssembleValues(SourceLine line, long minValue, long maxValue, int setSize)
            => AssembleValues(line, minValue, maxValue, setSize, false);

        void AssembleValues(SourceLine line, long minValue, long maxValue, int setSize, bool isRta)
        {
            foreach (var child in line.Operand.Children)
            {
                if (child.Children.Count == 0)
                {
                    Services.Log.LogEntry(line, child.Position, "Expression expected.");
                    return;
                }
                var firstInExpression = child.Children[0];
                if (firstInExpression.Name.Equals("?"))
                {
                    if (child.Children.Count > 1)
                        Services.Log.LogEntry(line, child.Children[1].Position,
                            $"Unexpected expression \"{child.Children[1].Name}\".");
                    Services.Output.AddUninitialized(setSize);
                }
                else
                {
                    var val = Services.Evaluator.Evaluate(child.Children, minValue, maxValue);
                    if (isRta)
                        val = ((int)(val - 1)) & 0xFFFF;
                    Services.Output.Add(val, setSize);
                }
            }
        }

        void AssembleBinaryFile(SourceLine line)
        {
            BinaryFile file;
            var filename = line.Operand.Children[0].Children[0].Name;
            if (_includedBinaries.ContainsKey(filename))
            {
                file = _includedBinaries[filename];
            }
            else
            {
                if (!filename.EnclosedInDoubleQuotes())
                    throw new ExpressionException(line.Operand.Position, "Filename not given in quotes.");
                file = new BinaryFile(filename.TrimOnce('"'));
                if (!file.Open())
                    throw new ExpressionException(line.Operand.Position, $"Unable to open file \"{filename}\".");
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
                        throw new ExpressionException(line.Operand.Children[3].Position, "Too many arguments specified for directive.");
                    size = (int)Services.Evaluator.Evaluate(line.Operand.Children[2].Children, ushort.MinValue, ushort.MaxValue);
                }
                offset = (int)Services.Evaluator.Evaluate(line.Operand.Children[1].Children, ushort.MinValue, ushort.MaxValue);

            }
            if (offset > size - 1)
                offset = size - 1;
            if (size > file.Data.Length - offset)
                size = file.Data.Length - offset;

            if (size > ushort.MaxValue)
                throw new ExpressionException(line.Operand.Position, $"Difference between specified offset and size is greater than the maximum allowed amount.");

            Services.Output.AddBytes(file.Data.Skip(offset), size);
        }

        void AssembleStrings(SourceLine line)
        {
            var stringBytes = new List<byte>();
            foreach (Token child in line.Operand.Children)
            {
                var element = child.Children[0];
                if (element.ToString().Trim().Equals("?"))
                {
                    if (child.Children.Count > 1)
                        Services.Log.LogEntry(line, child.Children[1].Position,
                            $"Unexpected expression \"{child.Children[1].Name}\".");
                    Services.Output.AddUninitialized(1);
                }
                else
                {
                    if (StringHelper.ExpressionIsAString(child, Services.SymbolManager))
                    {
                        stringBytes.AddRange(Services.Encoding.GetBytes(StringHelper.GetString(child, Services.SymbolManager, Services.Evaluator)));
                    }
                    else 
                    {
                        if (Services.SymbolManager.SymbolExists(element.ToString().Trim()))
                        {
                            var symVal = Services.SymbolManager.GetStringValue(element);
                            if (!string.IsNullOrEmpty(symVal))
                                stringBytes.AddRange(Services.Encoding.GetBytes(StringHelper.GetString(child.Children[0], Services.SymbolManager, Services.Evaluator)));
                            else
                                stringBytes.AddRange(Services.Output.ConvertToBytes(Services.SymbolManager.GetNumericValue(element)));
                        }
                        else
                        {
                            stringBytes.AddRange(Services.Output.ConvertToBytes(Services.Evaluator.Evaluate(child)));
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
            Services.Output.AddBytes(stringBytes, true);
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            if (line.Operand.Children.Count == 0)
            {
                Services.Log.LogEntry(line, line.Operand,
                    $"Instruction \"{line.InstructionName}\" expects one or more arguments.");
                return string.Empty;
            }
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
                case ".cbmflt":
                case ".cbmfltp":
                    AssembleCbmFloat(line);
                    break;
                default:
                    AssembleStrings(line);
                    break;
            }
            if (Services.PassNeeded || string.IsNullOrEmpty(Services.Options.ListingFile))
                return string.Empty;
            var sb = new StringBuilder();
            var assembly = Services.Output.GetBytesFrom(PCOnAssemble);
            if (!Services.Options.NoAssembly)
            {
                sb.Append(assembly.Take(8).ToString(PCOnAssemble).PadRight(43, ' '));
                if (!Services.Options.NoSource)
                    sb.Append(line.UnparsedSource);
                if (assembly.Count > 8)
                {
                    sb.AppendLine();
                    sb.Append(assembly.Skip(8).ToString(PCOnAssemble + 8));
                }
            }
            else
            {
                sb.Append($">{PCOnAssemble:x4}");
                if (!Services.Options.NoSource)
                    sb.Append($"{line.UnparsedSource,43}");
            }
            return sb.ToString();
        }

        void AssembleCbmFloat(SourceLine line)
        {
            var packed = line.InstructionName.EndsWith('p');
            var bytes = packed ? new byte[5] : new byte[6];
            foreach (var operand in line.Operand.Children)
            {
                if (operand.ToString().Trim().Equals('?'))
                {
                    Services.Output.AddUninitialized(packed ? 5 : 6);
                    continue;
                }
                var val = Services.Evaluator.Evaluate(operand.Children, Evaluator.CbmFloatMinValue, Evaluator.CbmFloatMaxValue);
                if (val != 0 && double.IsNormal(val))
                {
                    // Convert float to binary.
                    var ieee = BitConverter.GetBytes(val);

                    // Calculate exponent
                    var exp = (((ieee[7] << 4) + (ieee[6] >> 4)) & 0x7ff) - IeeeBias;
                    exp += CbmBias;
                    bytes[0] = Convert.ToByte(exp);
                
                    // Calculate mantissa
                    var mantissa = (       ieee[2]                | 
                                   ( (long)ieee[3]         << 8)  | 
                                   ( (long)ieee[4]         << 16) | 
                                   ( (long)ieee[5]         << 24) | 
                                   (((long)ieee[6] & 0xf)  << 32)) 
                                   << 3;

                    var manix = packed ? 1 : 2;
                    bytes[manix    ] = (byte)((mantissa >> 32) & 0xff);
                    bytes[manix + 1] = (byte)((mantissa >> 24) & 0xff);
                    bytes[manix + 2] = (byte)((mantissa >> 16) & 0xff);
                    bytes[manix + 3] = (byte)((mantissa >>  8) & 0xff);

                    if (bytes[manix] >= 0x80 && packed)
                        bytes[manix] &= 0x7f;

                    // Calculate sign
                    if ((ieee[7] & 0x80) != 0)
                    {
                        if (packed)
                            bytes[1] |= 0x80;
                        else
                            bytes[1] = 1;
                    }
                }
                Services.Output.AddBytes(bytes, true);
            }
        }

        double GetFloatFromMemory(int address, bool packed)
        {
            var size = packed ? 5 : 6;
            if (address + size > BinaryOutput.MaxAddress)
                return double.NaN;

            var bytes = Services.Output.GetRange(address, size).ToList();
            
            var ieeebytes = new byte[8];
            var exp = bytes[0] - CbmBias + IeeeBias;
            int sign;
            if (packed)
            {
                sign = bytes[1] & 0x80;
            }
            else
            {
                if (bytes[1] != 0 && bytes[1] != 1)
                    return double.NaN;
                sign = bytes[1] * 0x80;
                bytes[1] = (byte)((bytes[1] << 7) | bytes[2]);
                for(var i = 2; i < 5; i++)
                    bytes[i] = bytes[i + 1];
            }
            exp |= sign << 4;

            ieeebytes[7] = (byte)(exp >> 4);
            ieeebytes[6] = (byte)(((exp & 0x0F) << 4) | ((bytes[1] & 0x78) >> 3));

            for (var i = 1; i < 4; i++)
                ieeebytes[6 - i] = (byte)(((bytes[i] & 0x7) << 5) | ((bytes[i + 1] & 0xf8) >> 3));

            ieeebytes[2] = (byte)((bytes[4] & 0x7) << 5);
            return BitConverter.ToDouble(ieeebytes);
        }

        public bool EvaluatesFunction(Token function) => IsFunctionName(function.Name);

        public double EvaluateFunction(Token function, Token parameters)
        {
            if (function.Name.Equals("format"))
            {   
                var str = StringHelper.GetStringFormat(parameters, Services.SymbolManager, Services.Evaluator);
                return Services.Encoding.GetEncodedValue(str);
            }
            if (parameters.Children.Count == 0 || parameters.Children[0].Children.Count == 0)
                throw new ExpressionException(parameters.Position,
                $"Too few arguments passed for function \"{function.Name}\".");

            if (function.Name.Equals("section"))
                return Services.Output.GetSectionStart(parameters.Children[0].Children[0].Name);

            var address = (int)Services.Evaluator.Evaluate(parameters.Children[0], ushort.MinValue, ushort.MaxValue);

            if (function.Name.Equals("peek"))
            {
                if (parameters.Children.Count != 1)
                    throw new ExpressionException(parameters.Position,
                        "Too many arguments passed for function \"peek\".");

                return Services.Output.Peek(address);
            }
            else if (function.Name.Equals("cbmflt") || function.Name.Equals("cbmfltp"))
            {
                var doubleVal = GetFloatFromMemory(address, function.Name[^1] == 'p');
                if (double.IsNaN(doubleVal))
                    throw new ExpressionException(parameters.Position, 
                        $"Content at address ${address:x4} is not in the proper format.");
                return doubleVal;
            }
            else
            {
                if (parameters.Children.Count != 2)
                    throw new ExpressionException(parameters.Position,
                        "Too many arguments passed for function \"poke\".");
                var value = (byte)Services.Evaluator.Evaluate(parameters.Children[1], sbyte.MinValue, byte.MaxValue);

                Services.Output.Poke(address, value);
                return double.NaN;
            }
        }

        public override bool Assembles(string s) => Reserved.IsOneOf("Types", s);

        public void InvokeFunction(Token unused, Token parameters)
            => _ = EvaluateFunction(unused, parameters);

        public bool IsFunctionName(string symbol) => Reserved.IsOneOf("Functions", symbol);

        #endregion
    }
}
