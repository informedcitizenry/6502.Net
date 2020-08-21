//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A utility class for source lines.
    /// </summary>
    public static class SourceLineHelper
    {
        /// <summary>
        /// Returns the previous instruction line in the global Assembler iterator.
        /// </summary>
        /// <returns>Returns the previous instruction line in ther global Assembler iterator.</returns>
        public static SourceLine GetLastInstructionLine(RandomAccessIterator<SourceLine> iterator)
        {
            // make a copy
            iterator = new RandomAccessIterator<SourceLine>(iterator);
            var index = iterator.Index;
            if (index > -1)
            {
                var line = iterator.Current;
                while (line == null || line.Instruction == null)
                {
                    if (--index < 0)
                        break;
                    iterator.Rewind(index);
                    line = iterator.Current;
                }
                return line;
            }
            return null;
        }
    }
}
