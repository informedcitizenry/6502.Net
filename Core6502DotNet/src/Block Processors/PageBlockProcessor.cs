//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .page/.endpage blocks.
    /// </summary>
    public sealed class PageBlockProcessor : BlockProcessorBase
    {
        readonly int _page;

        /// <summary>
        /// Creates a new instance of a page block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator to traverse when
        /// processing the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public PageBlockProcessor(AssemblyServices services,
                                  RandomAccessIterator<SourceLine> iterator,
                                  BlockType type)
            : base(services, iterator, type, false) => _page = GetPage();

        int GetPage() => Services.Output.LogicalPC & 0xF00;

        int GetPage(int address) => address & 0xF00;

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override bool ExecuteDirective()
        {
            var line = LineIterator.Current;
            if (line.InstructionName.Equals(".endpage"))
            {
                if (!Services.PassNeeded && GetPage(Services.Output.LogicalPC - 1) != _page)
                    Services.Log.LogEntry(line, "Page boundary crossed.");
                return true;
            }
            return line.InstructionName.Equals(".page");
        }

        public override void PopScope() { }
    }
}