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
    public class PseudoAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Constants

        const int IeeeBias = 1023;

        const int CbmBias = 129;

        #endregion

        #region Members

        readonly Dictionary<StringView, BinaryFile> _includedBinaries;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="PseudoAssembler"/> line assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public PseudoAssembler(AssemblyServices services) 
            : base(services)
        {
            Reserved.DefineType("Types",
                    ".addr", ".align", ".binary", ".bstring", ".byte", ".sbyte",
                    ".char", ".dint", ".dword", ".fill", ".hstring", ".lint",
                    ".long", ".rta", ".short", ".sint", ".word",
                    ".cstring", ".lstring", ".nstring", ".pstring",
                    ".string", ".cbmflt", ".cbmfltp"
                );

            Reserved.DefineType("Functions",
                    "cbmflt", "cbmfltp", "format", "peek", "poke", "section"
                );
            services.Evaluator.AddFunctionEvaluator(this);
            _includedBinaries = new Dictionary<StringView, BinaryFile>(services.StringViewComparer);
        }

        #endregion

        #region Methods

        void AssembleHexBinStrings(SourceLine line)
        {
            var radix = line.Instruction.Name[1] == 'b' || line.Instruction.Name[1] == 'B' ? 2 : 16;
            var size = radix == 16 ? 2 : 8;
            var len = size - 1;
            var iterator = line.Operands.GetIterator();
            Token token;
            while ((token = iterator.GetNext()) != null)
            {
                if (!token.IsDoubleQuote())
                {
                    if (!token.Name.Equals("?"))
                        throw new SyntaxException(token.Position, "String literal expected.");
                    Services.Output.AddUninitialized(1);
                }
                else
                {
                    var hexBinDigits = new List<string>();
                    var hexBinString = Evaluator.GetBinaryString(token.Name.TrimOnce('"').ToString());
                    var startIndex = 0;
                    while (startIndex < hexBinString.Length)
                    {
                        if (startIndex + len >= hexBinString.Length)
                            hexBinDigits.Add(hexBinString[startIndex..]);
                        else
                            hexBinDigits.Add(hexBinString.Substring(startIndex, size));
                        startIndex += size;
                    }
                    try
                    {
                        foreach (var digit in hexBinDigits)
                            Services.Output.Add(Convert.ToByte(digit, radix));
                    }
                    catch (FormatException)
                    {
                        Services.Log.LogEntry(token, "String is not in the correct format.");
                    }
                }
                if (!Token.IsEnd(iterator.GetNext()))
                    throw new SyntaxException(iterator.Current, "Unexpected expression.");
            }
        }

        void AssembleValues(SourceLine line, double minValue, double maxValue, int setSize, bool isRta = false)
        {
            Token token;
            var iterator = line.Operands.GetIterator();
            while ((token = iterator.GetNext()) != null)
            {
                if (token.Name.Equals("?"))
                {
                    if (!Token.IsEnd(token = iterator.GetNext()))
                        throw new SyntaxException(token.Position,
                            "Unexpected expression.");
                    Services.Output.AddUninitialized(setSize);
                    
                }
                else
                {
                    var val = Services.Evaluator.Evaluate(iterator, false, minValue, maxValue);
                    if (isRta)
                        val = ((int)(val - 1)) & 0xFFFF;
                    Services.Output.Add(val, setSize);
                }
            }
        }

        void AssembleBinaryFile(SourceLine line)
        {
            var iterator = line.Operands.GetIterator();
            var filename = iterator.GetNext();
            if (Token.IsEnd(filename))
                throw new SyntaxException(line.Instruction, "Filename not specified.");
            if (!filename.IsDoubleQuote())
                throw new SyntaxException(line.Instruction, "Specified filename not valid.");
            var fileName = filename.Name;
            if (!_includedBinaries.TryGetValue(fileName, out var file))
            {
                file = new BinaryFile(fileName.ToString().TrimOnce('"'), Services.Options.IncludePath);
                if (!file.Open())
                    throw new SyntaxException(line.Operands[0], $"Unable to open file {fileName}.");
                _includedBinaries[fileName] = file;
            }
            var offset = 0;
            var size = file.Data.Length;
            if (iterator.MoveNext())
            {
                offset = (int)Services.Evaluator.Evaluate(iterator, ushort.MinValue, ushort.MaxValue);
                if (iterator.Current != null)
                {
                    size = (int)Services.Evaluator.Evaluate(iterator, ushort.MinValue, ushort.MaxValue);
                    if (iterator.Current != null)
                        throw new SyntaxException(iterator.Current, "Too many arguments specified for directive.");
                }
            }
            if (offset > size - 1)
                offset = size - 1;
            if (size > file.Data.Length - offset)
                size = file.Data.Length - offset;

            if (size > ushort.MaxValue)
                throw new SyntaxException(line.Operands[0], $"Binary file data is too great.");
            if (!Services.PassNeeded)
                Services.Output.AddBytes(file.Data.Skip(offset), size);
            else
                Services.Output.AddUninitialized(size);
        }

        void AssembleCbmFloat(SourceLine line)
        {
            var packed = line.Instruction.Name[^1] == 'p' || line.Instruction.Name[^1] == 'P';
            var bytes = packed ? new byte[5] : new byte[6];
            var iterator = line.Operands.GetIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Name.Equals("?"))
                {
                    if (!Token.IsEnd(iterator.GetNext()))
                        throw new SyntaxException(iterator.Current.Position,
                            "Unexpected expression.");
                    Services.Output.AddUninitialized(packed ? 5 : 6);
                    continue;
                }
                var val = Services.Evaluator.Evaluate(iterator, false, Evaluator.CbmFloatMinValue, Evaluator.CbmFloatMaxValue);
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
                if (iterator.Current == null)
                    break;
            }
        }

        void AssembleFills(SourceLine line)
        {
            var iterator = line.Operands.GetIterator();
            var alignval = (int)Services.Evaluator.Evaluate(iterator, 1, ushort.MaxValue);
            if (iterator.Current != null)
            {
                var fillval = (int)Services.Evaluator.Evaluate(iterator);
                if (iterator.Current != null)
                    throw new SyntaxException(iterator.Current, "Unexpected expression.");
                if (line.Instruction.Name.ToLower().Equals(".align"))
                    Services.Output.Align(alignval, fillval);
                else
                    Services.Output.Fill(alignval, fillval);
            }
            else
            {
                if (line.Instruction.Name.ToLower().Equals(".align"))
                    Services.Output.Align(alignval);
                else
                    Services.Output.Fill(alignval);
            }
        }
        
        void AssembleStrings(SourceLine line)
        {
            var stringBytes = new List<byte>();
            var iterator = line.Operands.GetIterator();
            Token token;
            while ((token = iterator.GetNext()) != null)
            {
                if (StringHelper.IsStringLiteral(iterator) || token.Name.Equals("?"))
                {
                    if (token.IsDoubleQuote())
                    {
                        stringBytes.AddRange(Services.Encoding.GetBytes(StringHelper.GetString(iterator, Services)));
                    }
                    else
                    {
                        if (stringBytes.Count == 0)
                            Services.Output.AddUninitialized(1);
                        else
                            stringBytes.Add(0);
                        iterator.MoveNext();
                    }
                    if (!Token.IsEnd(iterator.Current))
                        throw new SyntaxException(iterator.Current, "Unexpected expression.");
                }
                else if (StringHelper.ExpressionIsAString(iterator, Services))
                {
                    stringBytes.AddRange(Services.Encoding.GetBytes(StringHelper.GetString(iterator, Services)));
                }
                else 
                { 
                    stringBytes.AddRange(Services.Output.ConvertToBytes(Services.Evaluator.Evaluate(iterator, false)));
                }
                if (iterator.Current == null)
                    break;
            }
            var instructionName = line.Instruction.Name.ToLower();
            switch (instructionName)
            {
                case ".cstring":
                    stringBytes.Add(0x00);
                    break;
                case ".pstring":
                    if (stringBytes.Count > 255)
                        throw new ExpressionException(line.Operands[0].Position, $"String expression exceeds the maximum length of \".pstring\" directive.");

                    stringBytes.Insert(0, Convert.ToByte(stringBytes.Count));
                    break;
                case ".lstring":
                case ".nstring":
                    if (stringBytes.Any(b => b > 0x7f))
                        throw new ExpressionException(line.Operands[0].Position, $"One or more elements in expression exceeds maximum value.");
                    if (instructionName.Equals(".lstring"))
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

        public override bool Assembles(StringView s) => Reserved.IsOneOf("Types", s);

        internal override int GetInstructionSize(SourceLine line)
        {
            var instruction = line.Instruction.Name.ToLower();
            var iterator = line.Operands.GetIterator();
            Token token;
            if (instruction.EndsWith("string"))
            {
                int len = instruction.Equals(".cstring") || instruction.Equals(".pstring") ? 1 : 0;
                while ((token = iterator.GetNext()) != null)
                {
                    if (token.IsDoubleQuote())
                    {
                        len += Services.Encoding.GetByteCount(token.Name.TrimOnce('"').ToString());
                        if (!iterator.MoveNext())
                            break;
                    }
                    else if (token.Name.Equals("format", Services.StringComparison))
                    {
                        len += Services.Encoding.GetByteCount(StringHelper.GetString(iterator, Services));
                        if (iterator.Current == null)
                            break;
                    }
                    else if (token.Name.Equals("?"))
                    {
                        len++;
                        if (!iterator.MoveNext())
                            break;
                    }
                    else
                    {
                        len += Services.Evaluator.Evaluate(iterator, false).Size();
                        if (iterator.Current == null)
                            break;
                    }
                }
                return len;
            }
            else if (instruction.Equals(".fill"))
            {
                var times = (int)Services.Evaluator.Evaluate(iterator);
                if (iterator.Current != null)
                {
                    var size = Services.Evaluator.Evaluate(iterator).Size();
                    return times * size;
                }
                return times;
            }
            else if (instruction.Equals(".align"))
            {
                var align = (int)Services.Evaluator.Evaluate(iterator);
                return BinaryOutput.GetAlignmentSize(Services.Output.LogicalPC, align);
            }
            else
            {
                var operandCount = 1;
                int opens = 0;
                while ((token = iterator.GetNext()) != null)
                {
                    if (token.Type == TokenType.Open)
                        opens++;
                    else if (token.Type == TokenType.Closed)
                        opens--;
                    else if (token.Type == TokenType.Separator && opens == 0)
                        operandCount++;
                }
                switch (instruction)
                {
                    case ".bstring":
                    case ".byte":
                    case ".char":
                    case ".hstring":
                    case ".sbyte":
                        return operandCount;
                    case ".addr":
                    case ".word":
                    case ".rta":
                    case ".short":
                    case ".sint":
                        return operandCount * 2;
                    case ".long":
                    case ".lint":
                        return operandCount * 3;
                    case ".dword":
                    case ".dint":
                        return operandCount * 4;
                    case ".cbmfltp":
                        return operandCount * 5;
                    default:
                        return operandCount * 6;
                }
            }
        }

        protected override string OnAssemble(RandomAccessIterator<SourceLine> iterator)
        {
            var line = iterator.Current;
            if (line.Operands.Count == 0)
            {
                Services.Log.LogEntry(line.Instruction, "Expression expected.");
                return string.Empty;
            }
            var instruction = line.Instruction.Name.ToLower();
            switch (instruction)
            {
                case ".byte":
                    AssembleValues(line, byte.MinValue, byte.MaxValue, 1);
                    break;
                case ".char":
                case ".sbyte":
                    AssembleValues(line, sbyte.MinValue, sbyte.MaxValue, 1);
                    break;
                case ".addr":
                case ".word":
                    AssembleValues(line, ushort.MinValue, ushort.MaxValue, 2);
                    break;
                case ".rta":
                    AssembleValues(line, ushort.MinValue, ushort.MaxValue, 2, true);
                    break;
                case ".short":
                case ".sint":
                    AssembleValues(line, short.MinValue, short.MaxValue, 2);
                    break;
                case ".bstring":
                case ".hstring":
                    AssembleHexBinStrings(line);
                    break;
                case ".long":
                    AssembleValues(line, UInt24.MinValue, UInt24.MaxValue, 3);
                    break;
                case ".lint":
                    AssembleValues(line, Int24.MinValue, Int24.MaxValue, 3);
                    break;
                case ".dword":
                    AssembleValues(line, uint.MinValue, uint.MaxValue, 4);
                    break;
                case ".dint":
                    AssembleValues(line, int.MinValue, int.MaxValue, 4);
                    break;
                case ".align":
                case ".fill":
                    AssembleFills(line);
                    break;
                case ".binary":
                    AssembleBinaryFile(line);
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
                var firstBytes = assembly.Take(8).ToString(PCOnAssemble);
                if (assembly.Count > 8 && Services.Options.TruncateAssembly)
                    sb.Append(firstBytes).Append(" ...".PadRight(10, ' '));
                else
                    sb.Append(assembly.Take(8).ToString(PCOnAssemble).PadRight(43, ' '));
                if (!Services.Options.NoSource)
                {
                    if (Services.Options.VerboseList)
                        sb.Append($"{line.FullSource}");
                    else
                        sb.Append($"{line.Source}");
                }
                if (assembly.Count > 8 && !Services.Options.TruncateAssembly)
                {
                    sb.AppendLine();
                    sb.Append(assembly.Skip(8).ToString(PCOnAssemble + 8));
                }
            }
            else
            {
                sb.Append($">{PCOnAssemble:x4}");
                if (!Services.Options.NoSource)
                {
                    if (Services.Options.VerboseList)
                        sb.Append($"{line.FullSource}");
                    else
                        sb.Append($"{line.Source}");
                }
            }
            return sb.ToString();
        }

        double GetFloatFromMemory(int address, bool packed)
        {
            var size = packed ? 5 : 6;
            if (address + size > Services.Output.ProgramEnd)
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
                for (var i = 2; i < 5; i++)
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

        public bool EvaluatesFunction(Token function) => Reserved.IsOneOf("Functions", function.Name);

        public double EvaluateFunction(RandomAccessIterator<Token> tokens)
        {
            var function = tokens.Current;
            var functionName = function.Name.ToLower();
            if (functionName.Equals("format"))
            {
                var str = StringHelper.GetFormatted(tokens, Services);
                return Services.Encoding.GetEncodedValue(str);
            }
            tokens.MoveNext();
            if (Token.IsEnd(tokens.GetNext()))
                throw new SyntaxException(function, "Expression expected.");
            if (functionName.Equals("section"))
            {
                var section = tokens.Current;
                if (!section.IsDoubleQuote() || !Token.IsEnd(tokens.GetNext()))
                    throw new SyntaxException(function, "String expression expected.");
                return Services.Output.GetSectionStart(section.Name);
            }
            var address = (int)Services.Evaluator.Evaluate(tokens, false, ushort.MinValue, ushort.MaxValue);
            if (functionName.Equals("peek"))
            {
                if (!tokens.Current.Name.Equals(")"))
                    throw new SyntaxException(tokens.Current, "Unexpected expression.");
                return Services.Output.Peek(address);
            }
            if (functionName.Equals("cbmflt") || functionName.Equals("cbmfltp"))
            {
                if (!tokens.Current.Name.Equals(")"))
                    throw new SyntaxException(tokens.Current, "Unexpected expression.");
                var doubleVal = GetFloatFromMemory(address, functionName[^1] == 'p');
                if (double.IsNaN(doubleVal))
                    throw new SyntaxException(function, $"Content at address ${address:x4} is not in the proper format.");
                return doubleVal;
            }
            if (!tokens.Current.IsSeparator())
                throw new SyntaxException(tokens.Current, "Unexpected expression.");
            var value = (byte)Services.Evaluator.Evaluate(tokens, sbyte.MinValue, byte.MaxValue);
            if (!tokens.Current.Name.Equals(")"))
                throw new SyntaxException(tokens.Current, "Unexpected expression.");
            Services.Output.Poke(address, value);
            return double.NaN;
        }

        public void InvokeFunction(RandomAccessIterator<Token> tokens) => _ = EvaluateFunction(tokens);

        public bool IsFunctionName(StringView symbol) => Reserved.IsOneOf("Functions", symbol);

        #endregion
    }
}
