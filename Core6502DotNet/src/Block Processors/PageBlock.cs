//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .page/.endpage blocks.
    /// </summary>
    public sealed class PageBlock : BlockProcessorBase
    {
        readonly int _page;

        /// <summary>
        /// Creates a new instance of a page block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public PageBlock(AssemblyServices services,
                                  int index)
            : base(services, index, false)
        {
            Reserved.DefineType("Directives", ".page", ".endpage");
            _page = GetPage();
        }

        static int GetPage(int address) => address & 0xF00;

        int GetPage() => GetPage(Services.Output.LogicalPC);


        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (line.Operands.Count > 0)
                throw new SyntaxException(line.Operands[0], "Unexpected expression.");
            if (line.Instruction.Name.Equals(".endpage", Services.StringComparison))
            {
                if (!Services.PassNeeded && GetPage(Services.Output.LogicalPC - 1) != _page)
                    Services.Log.LogEntry(line.Instruction, "Page boundary crossed.");
            }
        }

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override IEnumerable<string> BlockOpens => new string[] { ".page" };

        public override string BlockClosure => ".endpage";

    }
}