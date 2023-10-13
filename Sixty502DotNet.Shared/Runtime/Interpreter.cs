//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents a runtime executive for the assembly process, overseeing parsing,
/// statement evaluation and code generation processes.
/// </summary> 
public sealed partial class Interpreter : SyntaxParserBaseVisitor<int>
{
    private readonly Dictionary<int, bool> _memoizedDefines;
    private readonly BinaryFileCollection _incBins;
    private readonly List<CodeAnalysisContext> _analysisContexts;
    private readonly Options _options;

    private IList<KeyValuePair<string, Macro>> _uninvokedMacros;
    private CpuEncoderBase _encoder;
    private const int MaxPasses = 6;

    /// <summary>
    /// Creates an interpreter instance with given options and binary file
    /// reader.
    /// </summary>
    /// <param name="options">The fully assembly options, including architecture,
    /// input, and output options.</param>
    /// <param name="binaryFileReader">The handler of binary file path resources.</param>
    public Interpreter(Options options, IBinaryFileReader binaryFileReader)
    {
        _uninvokedMacros = new List<KeyValuePair<string, Macro>>();
        _options = options;
        Services = new(this, options.GeneralOptions.CaseSensitive, options.ArchitectureOptions, options.DiagnosticOptions);
        _encoder = new M680xInstructionEncoder("6502", Services);
        _incBins = new(binaryFileReader);
        _analysisContexts = new();
        _memoizedDefines = new();
    }

    private static int GetGotoIndex(SyntaxParser.BlockContext context, Goto g)
    {
        if (g.Destination.expr().Length != 1)
        {
            throw new Error(g.Destination.expr()[1], "Unexpected expression");
        }
        SyntaxParser.ExprContext expr = g.Destination.expr()[0];
        if (expr is not SyntaxParser.ExpressionSimpleIdentifierContext)
        {
            throw g;
        }
        List<SyntaxParser.StatContext> stats = context.stat().ToList();
        int i = stats.FindIndex(stat =>
        {
            if (stat is SyntaxParser.StatLabelContext ||
                stat is SyntaxParser.StatInstructionContext ||
                stat is SyntaxParser.StatBlockContext)
            {
                return stat.Start.Text.Equals(expr.GetText());
            }
            return false;
        });
        if (i >= 0 && stats[i] is SyntaxParser.StatInstructionContext instr && instr.instruction().Start.Type != SyntaxParser.Label)
        {
            throw new Error(instr, "Only '.label' directive allowed here");
        }
        return i;
    }

    private void ExecBlock(SyntaxParser.BlockContext context)
    {
        var stats = context.stat();
        if (stats == null) return;
        var statCount = stats.Length;

        for (int i = 0; i < statCount; i++)
        {
            try
            {
                Services.State.StatementIndex++;
                Services.State.LogicalPCOnAssemble = Services.State.Output.LogicalPC;
                Services.State.LongLogicalPCOnAssemble = Services.State.Output.LongLogicalPC;
                _ = Visit(stats![i]);
            }
            catch (Warning warning)
            {
                AddWarning(warning.Context, warning.Message);
            }
            catch (Error err)
            {
                if (err is IllegalQuantityError && Services.State.PassNeeded)
                {
                    continue;
                }
                if (err.IsControlFlow)
                {
                    if (err is Goto g)
                    {
                        i = GetGotoIndex(context, g);
                        if (i >= 0)
                        {
                            continue;
                        }
                    }
                    throw err;
                }
                Services.State.Errors.Add(err);
            }
            catch (ProgramOverflowException overflow)
            {
                if (Services.State.PassNeeded)
                {
                    continue;
                }
                Services.State.Errors.Add(new Error(stats[i], overflow.Message));
            }
            catch (Exception exception)
            {
                Services.State.Errors.Add(new Error(stats[i], exception.Message));
            }
        }
    }

    public override int VisitProgram([NotNull] SyntaxParser.ProgramContext context)
    {
        if (context.block() == null)
        {
            return 0;
        }
        try
        {
            ExecBlock(context.block());
            return Services.State.Errors.Count == 0 ? 0 : -1;
        }
        catch (Error err)
        {
            Services.State.Errors.Add(err);
            return -1;
        }
    }

    private void ProcessOptions(string cpu)
    {
        for (int i = 0; i < _options.GeneralOptions.Defines?.Count; i++)
        {
            SyntaxParser.DefineAssignContext ctx = ParserBase.ParseDefineAssign(_options.GeneralOptions.Defines[i]);
            if (ctx.exception != null)
            {
                throw new Exception($"Define argument '{_options.GeneralOptions.Defines[i]}' is not valid");
            }
            ValueBase val = new NumericValue(1);
            if (ctx.expr() != null)
            {
                val = Evaluator.EvalConstant(ctx.expr());
            }
            if (!val.IsDefined)
            {
                throw new Exception($"Define argument '{_options.GeneralOptions.Defines[i]}' must be a constant expression");
            }
            Environment.DefineConstant(ctx.Identifier().GetText(),
                                       val,
                                       Services.State.Symbols.GlobalScope,
                                       false);
        }
        for (int i = 0; i < _options.GeneralOptions.Sections?.Count; i++)
        {
            (string section, int start, int? parsedEnd) = ParserBase.ParseDefineSection(_options.GeneralOptions.Sections[i]);
            int end = parsedEnd ?? 0x10000;
            Services.State.Output.DefineSection(section, start, end);
        }
        if (!string.IsNullOrEmpty(_options.OutputOptions.Format))
        {
            Services.State.Output.OutputFormat = OutputFormatSelector.Select(_options.OutputOptions.Format, cpu);
        }
        else
        {
            Services.State.Output.OutputFormat = OutputFormatSelector.DefaultProvider;
        }
    }

    private void GenListing(IToken start, string listing)
    {
        if (AddListing())
        {
            if (_options.OutputOptions.VerboseList)
            {
                Services.State.StatementListings.Add($"{start.SourceInfo(),-30}{listing}");
                return;
            }
            Services.State.StatementListings.Add(listing);
        }
    }

    private void GenListing(SyntaxParser.StatContext stat, ValueBase value)
    {
        if (!AddListing() || value.ValueType == ValueType.Callable || stat.Start.Text.Equals("_", StringComparison.Ordinal))
        {
            return;
        }
        string valString = value.ToString()!;
        if (!_options.OutputOptions.NoSource)
        {
            string source = stat.GetSourceLine(_options.OutputOptions.VerboseList);
            if (_options.OutputOptions.VerboseList)
            {
                string[] sources = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                source = sources[0];
                for (int i = 1; i < sources.Length; i++)
                {
                    source += $"\n{" ",-86}{sources[i]}";
                }
                if (!_options.OutputOptions.NoAssembly)
                {
                    GenListing(stat.Start, $"={valString,-55}{source}");
                }
                else
                {
                    GenListing(stat.Start, $"{"",-55}{source}");
                }
                return;
            }
            valString = valString.Elliptical(38);
            if (!_options.OutputOptions.NoAssembly)
            {
                GenListing(stat.Start, $"={valString,-55}{source.Elliptical(90)}");
            }
            else
            {
                GenListing(stat.Start, $"{"",-55}{source.Elliptical(90)}");
            }
        }
        else if (!_options.OutputOptions.NoAssembly)
        {
            GenListing(stat.Start, $"={valString}");
        }
    }
   
    private int GenListing(SyntaxParser.InstructionContext context, char leadingChar, bool disassemble)
    {
        if (AddListing())
        {
            SyntaxParser.StatContext stat = (SyntaxParser.StatContext)context.Parent;
            var genBytes = Services.State.Output.GetBytesFrom(Services.State.LongLogicalPCOnAssemble);
            string hexBytes = genBytes.ToHexString(8);
            StringBuilder listing = new();
            if (_options.OutputOptions.VerboseList)
            {
                listing.Append(stat.Start.SourceInfo().PadRight(30));
            }
            if (!_options.OutputOptions.NoAssembly)
            {
                listing.Append($"{leadingChar}{Services.State.LogicalPCOnAssemble:x4}    {hexBytes}".PadRight(32));
            }
            else
            {
                listing.Append("".PadRight(32));
            }
            if (disassemble && !_options.OutputOptions.NoDisassembly)
            {
                listing.Append(_encoder.DecodeInstruction(genBytes.ToArray(), Services.State.LogicalPCOnAssemble).PadRight(24));
            }
            else
            {
                listing.Append("".PadRight(24));
            }
            if (!_options.OutputOptions.NoSource)
            {
                string source = stat.GetSourceLine(_options.OutputOptions.VerboseList);
                if (_options.OutputOptions.VerboseList)
                {
                    string[] sources = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                    listing.Append(sources[0]);
                    for (int i = 1; i < sources.Length; i++)
                    {
                        listing.Append($"\n{" ".PadRight(86)}{sources[i]}");
                    }
                }
                else
                {
                    listing.Append(source);
                }
            }
            int genByteCount = genBytes.Count - 8;
            int pc = Services.State.LogicalPCOnAssemble;
            while (genByteCount > 0 && !_options.OutputOptions.TruncateAssembly && !_options.OutputOptions.NoAssembly)
            {
                listing.AppendLine();
                pc += 8;
                if (_options.OutputOptions.VerboseList)
                {
                    listing.Append(" ".PadRight(30));
                }
                listing.Append($"{leadingChar}{pc:x4}    ");
                genBytes = Services.State.Output.GetBytesFrom(pc);
                listing.Append(genBytes.ToHexString(8));
                genByteCount -= 8;
            }
            Services.State.StatementListings.Add(listing.ToString());
        }
        return 0;
    }

    private void PushAnonymousScope(int scopeIndex)
    {
        if (Services.State.Symbols.LookupToScope($"::{scopeIndex}") is not AnonymousScope scope)
        {
            scope = new AnonymousScope(scopeIndex, Services.State.Symbols.ActiveScope);
            Services.State.Symbols.Define(scope);
        }
        Services.State.Symbols.PushScope(scope);
    }

    private void AddWarning(Warning w)
    {
        if (Services.DiagnosticOptions.WarningsAsErrors)
        {
            Services.State.Errors.Add(new Error(w));
        }
        else
        {
            Services.State.Warnings.Add(w);
        }
    }

    private void AddWarning(IToken? token, string message)
    {
        if (Services.DiagnosticOptions.WarningsAsErrors)
        {
            Services.State.Errors.Add(new Error(token, message));
        }
        else
        {
            Services.State.Warnings.Add(new Warning(token, message));
        }
    }

    private void AddWarning(ParserRuleContext? context, string message)
    {
        if (Services.DiagnosticOptions.WarningsAsErrors)
        {
            Services.State.Errors.Add(context != null ? new Error(context, message) : new Error(message));
        }
        else
        {
            Services.State.Warnings.Add(context != null ? new Warning(context, message) : new Warning((IToken?)null, message));
        }
    }

    /// <summary>
    /// Execute the interpreter responsible for generating code from source.
    /// </summary>
    /// <param name="source">The source. This can be source code itself or a resource
    /// pointing to source. How this parameter is understood is according to the
    /// <see cref="ICharStreamFactory"/> factory parameter.</param>
    /// <param name="charStreamFactory">The <see cref="ICharStreamFactory"/>
    /// that creates a <see cref="ICharStream"/> of the source.</param>
    /// <returns></returns>
    public AssemblyState Exec(string source, ICharStreamFactory charStreamFactory)
    {
        return Exec(new List<string> { source }, charStreamFactory);
    }

    /// <summary>
    /// Execute the interpreter responsible for generating code from source.
    /// </summary>
    /// <param name="sources">The source or sources. These can be source code, files,
    /// or other named resources. How this parameter is understood is according to
    /// the <see cref="ICharStreamFactory"/> factory parameter.</param>
    /// <param name="charStreamFactory">The <see cref="ICharStreamFactory"/> that creates
    /// a <see cref="ICharStream"/> of the source.</param>
    /// <returns>The result of execution as an <see cref="AssemblyState"/>.</returns>
    /// <exception cref="Exception"></exception>
    public AssemblyState Exec(IList<string> sources, ICharStreamFactory charStreamFactory)
    {
        if (sources.Count == 0)
        {
            return Services.State;
        }

        string cpuid = _options.ArchitectureOptions.Cpuid ??
            Preprocessor.DetectCpuidFromSource(sources[0], charStreamFactory, _options.GeneralOptions.CaseSensitive) ??
            "6502";

        Architecture architecture = Architecture.FromCpuid(cpuid, Services);
        _encoder = architecture.Encoder;
        Services.State.Output.IsLittleEndian = architecture.IsLittleEndian;
        Services.State.Output.AllowLongOutput = _options.ArchitectureOptions.LongAddressing;

        ParseOptions parseOptions = new()
        {
            CaseSensitive = _options.GeneralOptions.CaseSensitive,
            StreamFactory = charStreamFactory,
            InstructionSet = architecture.InstructionSet
        };
        ParseResult parseResult = ProgramParser.Parse(sources,
                                                      parseOptions);
        _uninvokedMacros = parseResult.UninvokeMacros;
        Services.State.Errors.AddRange(parseResult.Errors);

        Environment.Init(Services);
        ProcessOptions(cpuid);
        Services.State.Output.AllowLongOutput = _options.ArchitectureOptions.LongAddressing;

        Constant binaryFunctionConst = (Constant)Services.State.Symbols.GlobalScope.Lookup("binary")!;
        BinaryFunction binaryFunction = (BinaryFunction)binaryFunctionConst.Value!;
        binaryFunction.BinaryCollection = _incBins;

        Constant currentPass = (Constant)Services.State.Symbols.GlobalScope.Lookup("CURRENT_PASS")!;
        Services.State.PassNeeded = true;

        while (Services.State.Errors.Count == 0 && Services.State.PassNeeded)
        {
            _encoder.Reset();
            Services.State.Reset();
            currentPass.Value.SetAs(new NumericValue(Services.State.CurrentPass));
            if (Services.State.CurrentPass > MaxPasses)
            {
                throw new Exception("Too many passes attempted");
            }
            if (Visit(parseResult.Program) < 0) break;
        }
        if (Services.State.Errors.Count > 0)
        {
            return Services.State;
        }
        _encoder.Analyze(_analysisContexts);
        for (int i = 0; i < _analysisContexts.Count; i++)
        {
            if (!string.IsNullOrEmpty(_analysisContexts[i].Report))
            {
                AddWarning(_analysisContexts[i].Context, _analysisContexts[i].Report!);
            }
        }
        if (!Services.DiagnosticOptions.DoNotWarnAboutUnusedSections)
        {
            IEnumerable<string> unusedSections = Services.State.Output.UnusedSections;
            foreach (string sectionName in unusedSections)
            {
                AddWarning((IToken?)null, $"Unused section '{sectionName}'");
            }
        }
        if (Services.DiagnosticOptions.WarnOfUnreferencedSymbols)
        {
            IEnumerable<SymbolBase> unusedSymbols = Services.State.Symbols.GetUnreferencedSymbols();
            foreach (SymbolBase unusedSymbol in unusedSymbols)
            {
                AddWarning(unusedSymbol.Token, $"Symbol '{unusedSymbol.Name}' was never referenced");
            }
            foreach (KeyValuePair<string, Macro> macroName in _uninvokedMacros)
            {
                AddWarning(macroName.Value.MacroDeclaration, $"Macro '{macroName.Key}' was never invoked");
            }
        }
        return Services.State;
    }

    private bool AddListing() => !Services.State.PrintOff &&
                                 !Services.State.PassNeeded &&
                                 !Services.State.Symbols.InFunctionScope;

    /// <summary>
    /// The shared <see cref="AssemblyServices"/> for the runtime assembly
    /// process.
    /// </summary>
    public AssemblyServices Services { get; init; }
}
