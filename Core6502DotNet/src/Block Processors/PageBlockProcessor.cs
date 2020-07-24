//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    public class PageBlockProcessor : BlockProcessorBase
    {
        int _page;

        public PageBlockProcessor(SourceLine line, BlockType type)
            : base(line, type) => _page = GetPage();

        int GetPage() => Assembler.Output.LogicalPC & 0xF00;

        int GetPage(int address) => address & 0xF00;

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override bool ExecuteDirective()
        {
            var line = Assembler.LineIterator.Current;
            if (line.InstructionName.Equals(".endpage"))
            {
                if (!Assembler.PassNeeded && GetPage(Assembler.Output.LogicalPC - 1) != _page)
                    Assembler.Log.LogEntry(line, "Page boundary crossed.");
                return true;
            }
            return line.InstructionName.Equals(".page");
        }
    }
}