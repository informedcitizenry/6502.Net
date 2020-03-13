//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    public static class SourceLineHelper
    {
        public static SourceLine GetLastInstructionLine()
        {
            var iterator = new RandomAccessIterator<SourceLine>(Assembler.LineIterator);
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
