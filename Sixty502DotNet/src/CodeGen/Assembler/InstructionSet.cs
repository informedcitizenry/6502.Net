//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class contains <see cref="Instruction"/> records of a CPU and their
    /// associated <see cref="Opcode"/> values. This class must be inherited.
    /// </summary>
    public abstract class InstructionSet
    {
        private readonly string _initialArchitecture;

        /// <summary>
        /// Construct a new instance of an <see cref="InstructionSet"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>.</param>
        /// <param name="initialArchitecture">The set's initial architecture.</param>
        public InstructionSet(AssemblyServices services, string initialArchitecture)
        {
            Services = services;
            _initialArchitecture = initialArchitecture;
        }

        /// <summary>
        /// Determines if the given instruction mnemonic and addressing mode
        /// are valid for this instruction set.
        /// </summary>
        /// <param name="mnemonic">The mnemonic type.</param>
        /// <param name="mode">The addressing mode.</param>
        /// <returns><c>true</c> if the mnemonic and mode pair represent a
        /// valid instruction, <c>false</c> otherwise.</returns>
        public bool IsValid(int mnemonic, int mode)
            => Set.ContainsKey(new Instruction(mnemonic, mode));

        /// <summary>
        /// Get the associated <see cref="Opcode"/> for the given mnemonic and
        /// addressing mode pair.
        /// </summary>
        /// <param name="mnemonic">The mnemonic type.</param>
        /// <param name="mode">The addressing mode.</param>
        /// <returns>An <see cref="Opcode"/>.</returns>
        protected Opcode Get(int mnemonic, int mode)
            => Set[new Instruction(mnemonic, mode)];

        /// <summary>
        /// Attempt code generation from a parsed CPU instruction statement.
        /// </summary>
        /// <param name="context">A <see cref="Sixty502DotNetParser.CpuStatContext"/>
        /// context.</param>
        /// <returns><c>true</c> if code generation was successful, <c>false</c>
        /// otherwise.</returns>
        public virtual bool GenCpuStatement(Sixty502DotNetParser.CpuStatContext context)
        {
            return false;
        }

        private IDictionary<string, int> GetDirectives()
            => new Dictionary<string, int>(Services.StringComparer)
            {
                { ".addr", Sixty502DotNetLexer.Addr },
                { ".align", Sixty502DotNetLexer.Align },
                { ".auto", Sixty502DotNetLexer.Auto },
                { ".assert", Sixty502DotNetLexer.Assert },
                { ".bank", Sixty502DotNetLexer.Bank },
                { ".bankbytes", Sixty502DotNetLexer.Bankbytes },
                { ".binary", Sixty502DotNetLexer.Binary },
                { ".block", Sixty502DotNetLexer.Block },
                { ".break", Sixty502DotNetLexer.Break },
                { ".bstring", Sixty502DotNetLexer.Bstring },
                { ".byte", Sixty502DotNetLexer.Byte },
                { ".case", Sixty502DotNetLexer.Case },
                { ".char", Sixty502DotNetLexer.Char },
                { ".cbmflt", Sixty502DotNetLexer.Cbmflt },
                { ".cbmfltp", Sixty502DotNetLexer.Cbmfltp },
                { ".continue", Sixty502DotNetLexer.Continue },
                { ".cstring", Sixty502DotNetLexer.Cstring },
                { ".default", Sixty502DotNetLexer.Default },
                { ".dint", Sixty502DotNetLexer.Dint },
                { ".do", Sixty502DotNetLexer.Do },
                { ".dsection", Sixty502DotNetLexer.Dsection },
                { ".dword", Sixty502DotNetLexer.Dword },
                { ".echo", Sixty502DotNetLexer.Echo },
                { ".else", Sixty502DotNetLexer.Else },
                { ".elseif", Sixty502DotNetLexer.Elseif },
                { ".elseifconst", Sixty502DotNetLexer.Elseifconst },
                { ".elseifdef", Sixty502DotNetLexer.Elseifdef },
                { ".elseifnconst", Sixty502DotNetLexer.Elseifnconst },
                { ".elseifndef", Sixty502DotNetLexer.Elseifndef },
                { ".encoding", Sixty502DotNetLexer.Encoding },
                { ".endblock", Sixty502DotNetLexer.Endblock },
                { ".endenum", Sixty502DotNetLexer.Endenum },
                { ".endfunction", Sixty502DotNetLexer.Endfunction },
                { ".endif", Sixty502DotNetLexer.Endif },
                { ".endnamespace", Sixty502DotNetLexer.Endnamespace },
                { ".endpage", Sixty502DotNetLexer.Endpage },
                { ".endrelocate", Sixty502DotNetLexer.Endrelocate },
                { ".endrepeat", Sixty502DotNetLexer.Endrepeat },
                { ".endswitch", Sixty502DotNetLexer.Endswitch },
                { ".endwhile", Sixty502DotNetLexer.Endwhile },
                { ".enum", Sixty502DotNetLexer.Enum },
                { ".eor", Sixty502DotNetLexer.DotEor },
                { ".equ", Sixty502DotNetLexer.Equ },
                { ".error", Sixty502DotNetLexer.Error },
                { ".errorif", Sixty502DotNetLexer.Errorif },
                { ".fill", Sixty502DotNetLexer.Fill },
                { ".for", Sixty502DotNetLexer.For },
                { ".forcepass", Sixty502DotNetLexer.Forcepass },
                { ".foreach", Sixty502DotNetLexer.Foreach },
                { ".format", Sixty502DotNetLexer.Format },
                { ".function", Sixty502DotNetLexer.Function },
                { ".global", Sixty502DotNetLexer.Global },
                { ".goto", Sixty502DotNetLexer.Goto },
                { ".hibytes", Sixty502DotNetLexer.Hibytes },
                { ".hstring", Sixty502DotNetLexer.Hstring },
                { ".hiwords", Sixty502DotNetLexer.Hiwords },
                { ".if", Sixty502DotNetLexer.If },
                { ".ifconst", Sixty502DotNetLexer.Ifconst },
                { ".ifdef", Sixty502DotNetLexer.Ifdef },
                { ".ifnconst", Sixty502DotNetLexer.Ifnconst },
                { ".ifndef", Sixty502DotNetLexer.Ifndef },
                { ".import", Sixty502DotNetLexer.Import },
                { ".initmem", Sixty502DotNetLexer.Initmem },
                { ".invoke", Sixty502DotNetLexer.Invoke },
                { ".label", Sixty502DotNetLexer.Label },
                { ".lobytes", Sixty502DotNetLexer.Lobytes },
                { ".let", Sixty502DotNetLexer.Let },
                { ".lint", Sixty502DotNetLexer.Lint },
                { ".long", Sixty502DotNetLexer.Long },
                { ".lstring", Sixty502DotNetLexer.Lstring },
                { ".lowords", Sixty502DotNetLexer.Lowords },
                { ".m8", Sixty502DotNetLexer.M8 },
                { ".m16", Sixty502DotNetLexer.M16 },
                { ".manual", Sixty502DotNetLexer.Manual },
                { ".map", Sixty502DotNetLexer.Map },
                { ".mx8", Sixty502DotNetLexer.MX8 },
                { ".mx16", Sixty502DotNetLexer.MX16 },
                { ".namespace", Sixty502DotNetLexer.Namespace },
                { ".next", Sixty502DotNetLexer.Next },
                { ".nstring", Sixty502DotNetLexer.Nstring },
                { ".org", Sixty502DotNetLexer.Org },
                { ".page", Sixty502DotNetLexer.Page },
                { ".proff", Sixty502DotNetLexer.Proff },
                { ".pron", Sixty502DotNetLexer.Pron },
                { ".pseudopc", Sixty502DotNetLexer.Pseudopc },
                { ".pstring", Sixty502DotNetLexer.Pstring },
                { ".realpc", Sixty502DotNetLexer.Realpc },
                { ".relocate", Sixty502DotNetLexer.Relocate },
                { ".repeat", Sixty502DotNetLexer.Repeat },
                { ".return", Sixty502DotNetLexer.Return },
                { ".rta", Sixty502DotNetLexer.Rta },
                { ".sbyte", Sixty502DotNetLexer.Sbyte },
                { ".section", Sixty502DotNetLexer.Section },
                { ".short", Sixty502DotNetLexer.Short },
                { ".sint", Sixty502DotNetLexer.Sint },
                { ".string", Sixty502DotNetLexer.String },
                { ".switch", Sixty502DotNetLexer.Switch },
                { ".tfradp", Sixty502DotNetLexer.Tfradp },
                { ".tfrbdp", Sixty502DotNetLexer.Tfrbdp },
                { ".unmap", Sixty502DotNetLexer.Unmap },
                { ".warn", Sixty502DotNetLexer.Warn },
                { ".warnif", Sixty502DotNetLexer.Warnif },
                { ".while", Sixty502DotNetLexer.While },
                { ".whiletrue", Sixty502DotNetLexer.Whiletrue },
                { ".word", Sixty502DotNetLexer.Word },
                { ".x8", Sixty502DotNetLexer.X8 },
                { ".x16", Sixty502DotNetLexer.X16 },
                { "true", Sixty502DotNetLexer.True },
                { "false", Sixty502DotNetLexer.False }
            };

        /// <summary>
        /// For a given architecture, set the mnemonic names and their associated
        /// types.
        /// </summary>
        /// <param name="architecture">The CPU architecture.</param>
        /// <returns>A dictionary of mnemonic names and their corresponding
        /// token types, useful during the parse phase.</returns>
        public IDictionary<string, int> SetMnemonics(string architecture)
        {
            return GetDirectives()
                .Concat(OnGetMnemonics(architecture))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, Services.StringComparer);
        }

        /// <summary>
        /// Check if a call to a subroutine is immediately followed by a return
        /// instruction.
        /// </summary>
        /// <param name="context">The parser context of the return statement.</param>
        /// <param name="callOpcode">The generated opcode representing the call.</param>
        /// <param name="callLength">The length of the call instruction.</param>
        /// <param name="retOpcode">The generated opcode representing the return.</param>
        public void CheckRedundantCallReturn(ParserRuleContext context, byte callOpcode, int callLength, byte retOpcode)
        {
            if (Services.Options.WarnSimplifyCallReturn)
            {
                var previousCallAddress = Services.State.LongLogicalPCOnAssemble - callLength;
                if (previousCallAddress >= 0)
                {
                    var lastBytes = Services.Output.GetBytesFrom(previousCallAddress);
                    if (lastBytes.Count > 1 && lastBytes[0] == callOpcode && lastBytes[^1] == retOpcode)
                    {
                        Services.Log.LogEntry(context, "Call and return statements can be simplified to a jump.", false);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the mnemonics are retrieved during the SetMnemonic call.
        /// </summary>
        /// <param name="architecture">The CPU architecture.</param>
        /// <returns></returns>
        protected abstract IDictionary<string, int> OnGetMnemonics(string architecture);

        /// <summary>
        /// Reset the state of the instruction set.
        /// </summary>
        public virtual void Reset()
        {
            Page = 0;
            _ = SetMnemonics(_initialArchitecture);
        }

        /// <summary>
        /// Get or set the instruction set's <see cref="Instruction"/>s and their
        /// associated <see cref="Opcode"/>s.
        /// </summary>
        protected abstract IDictionary<Instruction, Opcode> Set { get; set; }

        /// <summary>
        /// Gets or sets the current logical page in memory to reduce the size
        /// of certain generated instructions.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets the instruction set's referenced to the shared
        /// <see cref="AssemblyServices"/>.
        /// </summary>
        protected AssemblyServices Services { get; init; }
    }
}
