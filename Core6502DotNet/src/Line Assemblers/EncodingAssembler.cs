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
                return Services.Log.LogEntry<string>(line.Instruction, "Argument missing.");
            
            if (instruction.Equals(".encoding"))
            {
                if (!StringHelper.IsStringLiteral(iterator))
                    return Services.Log.LogEntry<string>(iterator.Current, "Expected string expression for encoding definition.", false, true);
                Services.Encoding.SelectEncoding(iterator.Current.Name);
            }
            else
            {
                string mapping;
                if (StringHelper.IsStringLiteral(iterator))
                    mapping = StringHelper.GetString(iterator, Services); 
                else
                    mapping = char.ConvertFromUtf32((int)Services.Evaluator.Evaluate(iterator, false, 0, 0x10ffff));
                if (instruction.Equals(".map"))
                {
                    if (iterator.Current == null)
                    {
                        return Services.Log.LogEntry<string>(line.Operands[0],
                            "Missing argument for directive \".map\".",false, true);
                    }
                    int translation = 0;
                    iterator.MoveNext();
                    var nextString = string.Empty;
                    if (StringHelper.IsStringLiteral(iterator))
                    {
                        var stringToken = iterator.Current;
                        nextString = StringHelper.GetString(iterator, Services);
                        if (nextString.Length > 1)
                            return Services.Log.LogEntry<string>(stringToken, "String literal argument can only be a single character.");
                    }
                    else
                    {
                        translation = (int)Services.Evaluator.Evaluate(iterator, false, 0, 0x10ffff);
                    }
                    if (iterator.Current != null)
                    {
                        if (!string.IsNullOrEmpty(nextString))
                        {
                            mapping += nextString;
                            nextString = null;
                        }
                        else
                        { 
                            mapping += char.ConvertFromUtf32(translation);
                        } 
                        iterator.MoveNext();
                        if (StringHelper.IsStringLiteral(iterator))
                        {
                            var stringToken = iterator.Current;
                            nextString = StringHelper.GetString(iterator, Services);
                            if (nextString.Length > 1)
                                return Services.Log.LogEntry<string>(stringToken, "String literal argument can only be a single character.");
                        }
                        else
                        {
                            translation = (int)Services.Evaluator.Evaluate(iterator, false, 0, 0x10ffff);
                        }
                    }
                    if (!string.IsNullOrEmpty(nextString))
                    {
                        translation = char.ConvertToUtf32(nextString, 0);
                    }
                    Services.Encoding.Map(mapping, translation);
                }
                else
                {
                    if (iterator.Current != null)
                        return Services.Log.LogEntry<string>(iterator.PeekNext() ?? iterator.Current,
                            $"Unexpected argument given for directive \".unmap\".", false, true);
                    Services.Encoding.Unmap(mapping);
                }
            }
            return string.Empty;
        }
        #endregion
    }
}