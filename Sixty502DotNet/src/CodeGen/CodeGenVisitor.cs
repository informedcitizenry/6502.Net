//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;
using System.Linq;
using System.Text;

namespace Sixty502DotNet
{
    /// <summary>
    /// A visitor class for parsed statements whose methods return a
    /// <see cref="BlockState"/> value. Parsed source that is responsible for
    /// generating code is visited, while higher level statement blcok and
    /// assembly directive code is visited by the base
    /// <see cref="BlockVisitor"/> class.
    /// </summary>
    public sealed class CodeGenVisitor : BlockVisitor
    {
        private readonly PseudoOps _pseudoOps;
        private readonly InstructionSet _instructionSet;

        /// <summary>
        /// Construct a new instance of the <see cref="CodeGenVisitor"/>
        /// class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="instructionSet">The instruction set to dispatch
        /// CPU-specific code genertion upon visit to CPU-specific nodes
        /// in the parse tree.</param>
        public CodeGenVisitor(AssemblyServices services, InstructionSet instructionSet)
            : base(services)
        {
            _instructionSet = instructionSet;
            _pseudoOps = new PseudoOps(services);
        }

        private void GenDataListing()
        {
            if (Services.State.CurrentStatement != null && ListingReady(Services))
            {
                var sb = new StringBuilder();
                var assembly = Services.Output.GetBytesFrom(Services.State.LogicalPCOnAssemble);
                if (!Services.Options.NoAssembly)
                {
                    var firstBytes = assembly.Take(8).ToString(Services.State.LogicalPCOnAssemble);
                    if (assembly.Count > 8 && Services.Options.TruncateAssembly)
                        sb.Append(firstBytes).Append(" ...".PadRight(10, ' '));
                    else
                        sb.Append(assembly.Take(8).ToString(Services.State.LogicalPCOnAssemble).PadRight(43, ' '));
                    if (!Services.Options.NoSource)
                    {
                        sb.Append(Services.State.CurrentStatement.GetSourceLine(Services.Options.VerboseList));
                    }
                    if (assembly.Count > 8 && !Services.Options.TruncateAssembly)
                    {
                        sb.AppendLine();
                        sb.Append(assembly.Skip(8).ToString(Services.State.LogicalPCOnAssemble + 8));
                    }
                }
                else
                {
                    sb.Append($">{Services.State.LogicalPCOnAssemble:x4}");
                    if (!Services.Options.NoSource)
                    {
                        sb.Append(Services.State.CurrentStatement.GetSourceLine(Services.Options.VerboseList));
                    }
                }
                Services.StatementListings.Add(sb.ToString());
            }
        }

        public override BlockState VisitLabel([NotNull] Sixty502DotNetParser.LabelContext context)
        {
            // sync the label/line reference to the program counter
            var pc = new Value(Services.Output.LogicalPC);
            if (context.anonymousLabel() != null)
            {
                var lineRef = Services.Symbols.Scope.ResolveAnonymousLabel(context.Start.TokenIndex)!;
                Services.State.PassNeeded |= lineRef.LabelType == AnonymousLabel.Forward &&
                    !lineRef.Value.Equals(pc);
                lineRef.Value.SetAs(pc);
            }
            else if (context.Ident() != null && !Services.Symbols.Scope.InFunctionScope)
            {
                if (context.Parent is Sixty502DotNetParser.StatContext statContext &&
                    statContext.blockStat()?.enterBlock()?.directive.Type == Sixty502DotNetParser.Function)
                {
                    return BlockState.Evaluating();
                }
                if (UpdateLabelOrConstant(context.Start.Text, pc) is Label label)
                {
                    label.Bank = Services.Output.CurrentBank;
                }
            }
            return BlockState.Evaluating();
        }

        public override BlockState VisitCpuStat([NotNull] Sixty502DotNetParser.CpuStatContext context)
        {
            if (context.cpuDirectiveStat() != null)
            {
                CpuDirective(context.cpuDirectiveStat());
            }
            else if (!_instructionSet.GenCpuStatement(context))
            {
                Services.Log.LogEntry(context, Errors.ModeNotSupported);
            }
            return BlockState.Evaluating();
        }

        // '.cpu' StringLiteral ;
        private void SetInstructionSet(Sixty502DotNetParser.PseudoOpArgContext[] context)
        {
            if (context.Length > 1 || context[0].Start.Type != Sixty502DotNetParser.StringLiteral)
            {
                Services.Log.LogEntry(context[^1], "Expected single string literal argument for \".cpu\" directive.");
                return;
            }
            // set the mnemonics on the current instruction set (and discard the tokens since we're already
            // post-parse)
            _ = _instructionSet.SetMnemonics(context[0].Start.Text.TrimOnce('"'));
        }

        private void CpuDirective(Sixty502DotNetParser.CpuDirectiveStatContext context)
        {
            if (_instructionSet is MotorolaBase motorola)
            {
                motorola.CpuDirectiveStatement(context);
                return;
            }
            Services.Log.LogEntry(context, "Directive ignored for CPU.", false);
        }

        // pseudoOpArg : expr | '?' ;
        // pseudoOpList: pseudoOpArg (',' pseudoOpArg)* ;
        // pseudoOpStat: pseudoOp pseudoOpList ;
        public override BlockState VisitPseudoOpStat([NotNull] Sixty502DotNetParser.PseudoOpStatContext context)
        {
            var args = context.pseudoOpList().pseudoOpArg();
            var directive = context.pseudoOp().directive.Type;
            switch (directive)
            {
                case Sixty502DotNetParser.Addr:
                    _pseudoOps.GenValues(args, short.MinValue, ushort.MaxValue, 2, true, false);
                    break;
                case Sixty502DotNetParser.Align:
                case Sixty502DotNetParser.Fill:
                    _pseudoOps.GenFills(directive, args);
                    break;
                case Sixty502DotNetParser.Bankbytes:
                    _pseudoOps.GenValues(args, int.MinValue, uint.MaxValue, 1, false, false, 0xff0000);
                    break;
                case Sixty502DotNetParser.Binary:
                    _pseudoOps.GenFromFile(args);
                    break;
                case Sixty502DotNetParser.Bstring:
                case Sixty502DotNetParser.Hstring:
                    _pseudoOps.GenBinHexStrings(directive, context.pseudoOpList());
                    break;
                case Sixty502DotNetParser.Byte:
                    _pseudoOps.GenValues(args, byte.MinValue, byte.MaxValue, 1, false, false);
                    break;
                case Sixty502DotNetParser.Cbmflt:
                case Sixty502DotNetParser.Cbmfltp:
                    _pseudoOps.GenFloats(directive, context.pseudoOpList());
                    break;
                case Sixty502DotNetParser.Char:
                case Sixty502DotNetParser.Sbyte:
                    _pseudoOps.GenValues(args, sbyte.MinValue, sbyte.MaxValue, 1, false, false);
                    break;
                case Sixty502DotNetParser.Cpu:
                    SetInstructionSet(args);
                    break;
                case Sixty502DotNetParser.Dint:
                    _pseudoOps.GenValues(args, int.MinValue, int.MaxValue, 4, false, false);
                    break;
                case Sixty502DotNetParser.Dword:
                    _pseudoOps.GenValues(args, uint.MinValue, uint.MaxValue, 4, false, false);
                    break;
                case Sixty502DotNetParser.Hibytes:
                    _pseudoOps.GenValues(args, int.MinValue, uint.MaxValue, 1, false, false, 0xff00);
                    break;
                case Sixty502DotNetParser.Hiwords:
                    _pseudoOps.GenValues(args, int.MinValue, uint.MaxValue, 2, false, false, 0xffff0000);
                    break;
                case Sixty502DotNetParser.Initmem:
                    _pseudoOps.InitMem(args);
                    break;
                case Sixty502DotNetParser.Lobytes:
                    _pseudoOps.GenValues(args, int.MinValue, uint.MaxValue, 1, false, false, 0xff);
                    break;
                case Sixty502DotNetParser.Lint:
                    _pseudoOps.GenValues(args, Int24.MinValue, Int24.MaxValue, 3, false, false);
                    break;
                case Sixty502DotNetParser.Long:
                    _pseudoOps.GenValues(args, UInt24.MinValue, UInt24.MaxValue, 3, false, false);
                    break;
                case Sixty502DotNetParser.Lowords:
                    _pseudoOps.GenValues(args, int.MinValue, uint.MaxValue, 2, false, false, 0xffff);
                    break;
                case Sixty502DotNetParser.Rta:
                    _pseudoOps.GenValues(args, short.MinValue, ushort.MaxValue, 2, true, true);
                    break;
                case Sixty502DotNetParser.Short:
                case Sixty502DotNetParser.Sint:
                    _pseudoOps.GenValues(args, short.MinValue, short.MaxValue, 2, false, false);
                    break;
                case Sixty502DotNetParser.Word:
                    _pseudoOps.GenValues(args, ushort.MinValue, ushort.MaxValue, 2, false, false);
                    break;
                default:
                    _pseudoOps.GenStrings(directive, context.pseudoOpList());
                    break;
            }
            if (directive != Sixty502DotNetParser.Cpu)
            {
                GenDataListing();
            }
            return BlockState.Evaluating();
        }

        /// <summary>
        /// Resets the state of the object, for example if assembly needs to
        /// perform anew pass.
        /// </summary>
        public void Reset() => _instructionSet.Reset();
    }
}
