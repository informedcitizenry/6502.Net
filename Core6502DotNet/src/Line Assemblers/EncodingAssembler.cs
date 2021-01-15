//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

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
            : base(services)
        {
            Reserved.DefineType("Directives",
                ".encoding", ".map", ".unmap");
        }

        #endregion

        #region Methods

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            var instruction = line.Instruction.Name.ToLower();
            var iterator = line.Operands.GetIterator();
            if (!iterator.MoveNext())
                Services.Log.LogEntry(line.Instruction, "Expected expression.");
            if (instruction.Equals(".encoding"))
            {
                if (!iterator.Current.IsDoubleQuote() || !Token.IsEnd(iterator.PeekNext()))
                    Services.Log.LogEntry(iterator.Current, "Expected string expression for encoding definition.");
                else
                    Services.Encoding.SelectEncoding(iterator.Current.Name);
            }
            else
            {
                string mapping;
                if (!iterator.Current.IsDoubleQuote() || !Token.IsEnd(iterator.PeekNext()))
                    mapping = char.ConvertFromUtf32((int)Services.Evaluator.Evaluate(iterator, false, 0, 0x10ffff));
                else
                    mapping = StringHelper.GetString(iterator, Services);
                if (instruction.Equals(".map"))
                {
                    if (!iterator.MoveNext())
                    {
                        Services.Log.LogEntry(line.Operands[0],
                            "Missing one or more arguments for directive \".map\".");
                    }
                    else
                    {
                        int translation;
                        if (iterator.Current.IsDoubleQuote() && Token.IsEnd(iterator.PeekNext()))
                            translation = Services.Encoding.GetEncodedValue(StringHelper.GetString(iterator, Services));
                        else
                            translation = (int)Services.Evaluator.Evaluate(iterator, false, 0, 0x10ffff);
                        if (iterator.Current != null)
                        {
                            if (!iterator.MoveNext())
                            {
                                Services.Log.LogEntry(iterator.Current, "Expected expression.");
                            }
                            else
                            {
                                mapping += char.ConvertFromUtf32(translation);
                                if (StringHelper.IsStringLiteral(iterator))
                                    translation = Services.Encoding.GetEncodedValue(StringHelper.GetString(iterator, Services));
                                else
                                    translation = (int)Services.Evaluator.Evaluate(iterator, false, 0, 0x10ffff);
                                if (iterator.Current != null)
                                {
                                    Services.Log.LogEntry(iterator.Current,
                                    "Unexpected expression.");
                                    return string.Empty;
                                }
                            }
                        }
                        Services.Encoding.Map(mapping, translation);
                    }
                }
                else
                {
                    if (iterator.Current != null)
                        Services.Log.LogEntry(iterator.Current,
                            $"Unexpected argument \"{iterator.Current}\" given for directive \".unmap\".");
                    else
                        Services.Encoding.Unmap(mapping);
                }
            }
            return string.Empty;
        }
        #endregion
    }
}