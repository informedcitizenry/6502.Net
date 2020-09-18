//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for handling encoding-related directives.
    /// </summary>
    public sealed class EncodingAssembler : AssemblerBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the encoding assembler class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public EncodingAssembler(AssemblyServices services)
            :base(services)
        {
            Reserved.DefineType("Directives",
                ".encoding", ".map", ".unmap");
        }

        #endregion

        #region Methods

        string EvalEncodingParam(Token p)
        {
            if (!p.ToString().Trim().EnclosedInDoubleQuotes())
            {
                var result = (int)Services.Evaluator.Evaluate(p, 0, 0x10FFFF);
                try
                {
                    return char.ConvertFromUtf32(result);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ExpressionException(p.Position, $"\"{p}\" is not a valid UTF-32 value.");
                }
            }
            return p.ToString().Trim()[1..^1];
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            if (line.InstructionName.Equals(".encoding"))
            {
                if (!line.OperandHasToken)
                    Services.Log.LogEntry(line, line.Operand, "Encoding definition not specified.");
                else if (!line.OperandExpression.EnclosedInDoubleQuotes())
                    Services.Log.LogEntry(line, line.Operand, "Expected string expression for encoding definition.");
                else
                    Services.Encoding.SelectEncoding(line.OperandExpression.TrimOnce('"'));
            }
            else if (line.InstructionName.Equals(".map"))
            {
                if (line.Operand.Children.Count < 2 || 
                    line.Operand.Children[0].Children.Count == 0 || 
                    line.Operand.Children[1].Children.Count == 0)
                {
                    Services.Log.LogEntry(line, line.Operand.LastChild, "Missing one or more arguments for directive \".map\".");
                }
                else if (line.Operand.Children.Count > 3)
                {
                    Services.Log.LogEntry(line, line.Operand.LastChild, $"Unexpected argument \"{line.Operand.LastChild}\" for directive \".map\".");
                }
                else
                {
                    Token firstParam = line.Operand.Children[0].Children[0];
                    var remainingParms = line.Operand.Children[1];
                    var secondParam = remainingParms.Children[0];

                    int translation;
                    if (secondParam.Name.EnclosedInQuotes())
                    {
                        if (secondParam.Name.EnclosedInDoubleQuotes())
                        {
                            var transString = EvalEncodingParam(secondParam);
                            var translationBytes = Encoding.UTF8.GetBytes(transString);
                            if (translationBytes.Length < 4)
                                Array.Resize(ref translationBytes, 4);
                            translation = BitConverter.ToInt32(translationBytes, 0);
                        }
                        else if (secondParam.Name.EnclosedInSingleQuotes())
                        {  
                            translation = char.ConvertToUtf32(EvalEncodingParam(secondParam), 0);
                        }
                        else
                        {
                            Services.Log.LogEntry(line, secondParam, $"Argument \"{secondParam.Name}\" is not a char literal.");
                            return string.Empty;
                        }
                    }
                    else
                    {
                        translation = (int)Services.Evaluator.Evaluate(remainingParms, int.MinValue, int.MaxValue);
                    }
                    if (line.Operand.Children.Count == 2)
                    {
                        var mapchar = EvalEncodingParam(firstParam);
                        Services.Encoding.Map(mapchar, translation);
                    }
                    else
                    {
                        var firstRange = EvalEncodingParam(firstParam);
                        var lastRange = EvalEncodingParam(line.Operand.Children[1]);
                        Services.Encoding.Map(string.Concat(firstRange, lastRange), translation);
                    }
                }
            }
            else
            {
                if (line.Operand.Children.Count > 2)
                {
                    Services.Log.LogEntry(line, line.Operand.LastChild,
                        $"Unexpected argument \"{line.Operand.LastChild}\" given for directive \".unmap\".");
                }
                else
                {
                    if (line.Operand.Children.Count == 1)
                    {
                        var unmap = EvalEncodingParam(line.Operand.Children[0]);
                        Services.Encoding.Unmap(unmap);
                    }
                    else
                    {
                        var firstunmap = EvalEncodingParam(line.Operand.Children[0]);
                        var lastunmap = EvalEncodingParam(line.Operand.Children[1]);
                        Services.Encoding.Unmap(string.Concat(firstunmap, lastunmap));
                    }
                }
            }
            return string.Empty;
        }
        #endregion
    }
}