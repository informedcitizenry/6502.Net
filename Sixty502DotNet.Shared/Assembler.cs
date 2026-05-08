// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Arch.Formats;
using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Encode;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse;
using Sixty502DotNet.Shared.Parse.Ast;
using System.Diagnostics;

namespace Sixty502DotNet.Shared;

public static class Assembler
{
    public static AssemblyResult Assemble(List<string> sourcePaths, ISourceFactory sourceFactory, AssemblyOptions assemblyOptions)
    {
        if (sourcePaths.Count == 0)
        {
            return new AssemblyResult(new ErrorLogger(false));
        }
        var state = Init(assemblyOptions, sourceFactory);
        if (state.Logger.ErrorCount > 0)
        {
            return new AssemblyResult(state.Logger);
        }
        state.Logger.CheckLogWarnings = () => !state.PassNeeded;

        var parseTimer = new Stopwatch();
        parseTimer.Start();
        var parsedSources = ParseSources
        (
            state, 
            sourcePaths, 
            assemblyOptions.BraFor6502,
            assemblyOptions.PseudoBranches6502,
            assemblyOptions.LexerBehavior
        );
        if (state.Logger.GetErrors().Count > 0)
        {
            return new AssemblyResult(state.Logger);
        }
        parseTimer.Stop();
        
        CompileAndAnalyzeCode(state, parsedSources);
        
        GenUnusedReferenceWarnings(state);
        
        var checksum = assemblyOptions.Checksum
            ? state.Output.GetOutputHash(assemblyOptions.OutputSection) 
            : string.Empty;
        
        try
        {
            return new AssemblyResult
            {
                Start = state.Output.Start,
                End = state.Output.End,
                PcStart = state.Output.ProgramCounterStart,
                PcEnd = state.Output.ProgramCounterEnd,
                Checksum = checksum,
                Errors = state.Logger.GetErrors(),
                Warnings = state.Logger.GetWarnings(),
                ObjectCode = GetObjectCode(sourcePaths, state.Output.Start, state, assemblyOptions).ToArray(),
                Labels = state.SymbolTable.Report(assemblyOptions.LabelsAddressesOnly, assemblyOptions.ViceLabels),
                Listing = state.Listings.ToString(),
                Passes = state.Passes,
                ParseTime = parseTimer.Elapsed.TotalSeconds
            };
        }
        catch (OutputFormatException)
        {
            state.Logger.LogFatalError(CompileExceptionType.InvalidFormat.Stringified());
            return new AssemblyResult(state.Logger);
        }
    }

    private static BlockStatement ParseSources
    (
        AssemblyState state, 
        List<string> sourcePaths,  
        bool bra6502,
        bool pseudoBra6502,
        LexerBehavior lexerBehavior
    )
    {
        if (state.InitialCpu == null &&
            Parser.TryParseTopLevelCpuStatement(state.SourceFactory, sourcePaths[0], lexerBehavior, out var cpuStatement))
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            var cpuName = ExpressionFolder.EvalStringLiteral(cpuStatement!.Expression);
            var cpu = CpuLookup.ByName(cpuName);
            if (cpu == null)
            {
                state.Logger.LogError
                (
                    new CompileException(
                        CompileExceptionType.InvalidCpuSpecified, 
                        cpuStatement.Expression)
                );
                return new BlockStatement();
            }
            state.InitialCpu = state.Cpu = cpu.Value;
        }
        else
        {
            state.InitialCpu ??= Cpu.M6502;
        }
        var parsedSources = new BlockStatement();
        var sourceIndex = 0;
        try
        {
            var parser = new Parser
            (
                state.SourceFactory, 
                sourcePaths[0], 
                null, 
                state.Cpu, 
                bra6502,
                pseudoBra6502,
                lexerBehavior
            );
            parsedSources.Statements.Add(parser.ParseModule(state.Logger));
            for (sourceIndex = 1; sourceIndex < sourcePaths.Count && !parser.Ended; sourceIndex++)
            {
                parser.ChangeSource(sourcePaths[sourceIndex]);
                parsedSources.Statements.Add(parser.ParseModule(state.Logger));
            }
            state.UnexpandedMacros.AddRange(parser.UnexpandedMacros);
        }
        catch
        {
            state.Logger.LogFatalError($"Could not read source at path `{sourcePaths[sourceIndex]}`");
            return new BlockStatement();
        }
        return parsedSources;
    }
    
    private static void CompileAndAnalyzeCode(AssemblyState state, BlockStatement parsedSources)
    {
        var compiler = new Compiler(state);
        do
        {
            state.Reset();
            state.SymbolTable.Reset();
            state.SymbolTable.DefineBuiltIn("CURRENT_PASS", new Value(state.Passes + 1));
            _ = compiler.Visit(parsedSources);
            state.Passes++;
            if (state.Passes <= 5) continue;
            state.Logger.LogFatalError("Too many passes attempted");
            break;
        } 
        while (state is { PassNeeded: true, Logger.ErrorCount: 0 });
        
        for (var i = 0; i < state.AnalysisContexts.Count; i++)
        {
            var context = state.AnalysisContexts[i];
            var context2 = i < state.AnalysisContexts.Count - 1 ?  state.AnalysisContexts[i + 1] : null;
            var report = context.Cpuid switch
            {
                Cpu.M6800 or Cpu.M6809 => MotorolaEncoder.Analyze(state.AssemblyOptions, context, context2),
                Cpu.Gb80 or Cpu.I8080 or Cpu.Z80 => ZilogIntelEncoder.Analyze(state.AssemblyOptions, context, context2),
                Cpu.I86 => I86Encoder.Analyze(state.AssemblyOptions, context, context2),
                _ => M65xxEncoder.Analyze(state.AssemblyOptions, context, context2)
            };
            if (!string.IsNullOrEmpty(report))
            {
                state.Logger.LogWarning(report, context.Statement);
            }
        }
    }

    private static void GenUnusedReferenceWarnings(AssemblyState state)
    {
        if (state.Logger.ErrorCount > 0) return;
        if (!state.AssemblyOptions.DoNotWarnOnUnusedSections)
        {
            var unused = state.Output.GetUnusedSections();
            for (var i = 0; i < unused.Count; i++)
            {
                state.Logger.LogGeneralWarning($"Section `{unused[i]}` was never selected");
            }
        }
        if (state.AssemblyOptions.WarnUnreferencedSymbols)
        {
            var unreferenced = state.SymbolTable.GetUnreferencedSymbols();
            for (var i = 0; i < unreferenced.Count; i++)
            {
                state.Logger.LogWarning
                (
                    "Symbol was never referenced", 
                    new PrimaryExpression(unreferenced[i].Value)
                );
            }
            for (var i = 0; i < state.UnexpandedMacros.Count; i++)
            {
                var macro = state.UnexpandedMacros[i];
                state.Logger.LogWarning
                (
                    "Macro was never invoked", 
                    new PrimaryExpression(macro)
                );
            }
        }
    }
    
    private static IReadOnlyCollection<byte> GetObjectCode
    (
        List<string> sourcePaths,
        int startAddress,
        AssemblyState state, 
        AssemblyOptions options
    )
    {
        ReadOnlySpan<byte> objectCode ;
        if (string.IsNullOrEmpty(options.OutputSection))
        {
            objectCode = state.Output.GetCompilation();
        }
        else
        {
            try
            {
                objectCode = state.Output.GetCompilation(options.OutputSection);
                startAddress = state.Output.SectionStart(options.OutputSection);
            }
            catch
            {
                state.Logger.LogFatalError($"Invalid output section `{options.OutputSection}` specified");
                return [];
            }
        }

        var cpu = state.Cpu;
        IOutputFormatProvider formatProvider = state.Format switch
        {
            OutputFormat.ByteSource => new ByteSourceFormatProvider(),
            OutputFormat.Flat => new FlatFormatProvider(),
            OutputFormat.Hex => new HexFormatProvider(),
            OutputFormat.Hex86 => new Hex86FormatProvider(),
            OutputFormat.SRecMos or 
            OutputFormat.SRecord => new SRecordFormatProvider(state.Format),
            _ => cpu switch
            {
                Cpu.M6800 or Cpu.M6809 => state.Format switch
                {
                    OutputFormat.None => new FlatFormatProvider(),
                    _ => throw new OutputFormatException()
                },
                Cpu.Gb80 or Cpu.I8080 or Cpu.I86 or Cpu.Z80 => state.Format switch
                {
                    OutputFormat.None => new FlatFormatProvider(),
                    OutputFormat.Cpm => new CpmFormatProvider(),
                    OutputFormat.Mz => new MzFormatProvider(),
                    _ => cpu != Cpu.I86 
                        ? new Z80FormatProvider(state.Format) 
                        : throw new OutputFormatException()
                },
                _ => state.Format switch
                {
                    OutputFormat.Cart => new C64CartFormatProvider(),
                    OutputFormat.D64 => new D64FormatProvider(),
                    OutputFormat.T64 => new T64FormatProvider(),
                    _ => new M65xxFormatProvider(state.Format)
                }
            }
        };
        return formatProvider.GetFormat(sourcePaths[0], startAddress, objectCode);
    }

    private static AssemblyState Init(AssemblyOptions assemblyOptions, ISourceFactory sourceFactory)
    {
        var logger = new ErrorLogger(assemblyOptions.WarningsAsErrors);
        Cpu? initialCpu = null;
        if (!string.IsNullOrEmpty(assemblyOptions.Cpu))
        {
            initialCpu = CpuLookup.ByName(assemblyOptions.Cpu);
            if (initialCpu == null)
            {
                logger.LogFatalError($"Invalid CPU `{assemblyOptions.Cpu}` specified");
            }
        }
        OutputFormat? initialFormat = null;
        if (!string.IsNullOrEmpty(assemblyOptions.Format))
        {
            initialFormat = FormatLookup.ByName(assemblyOptions.Format);
            if (initialFormat == null)
            {
                logger.LogFatalError($"Invalid format `{assemblyOptions.Format}` specified");
            }
        }
        var comparer = assemblyOptions.CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;
        
        var symTable = new SymbolTable(comparer);
        if (logger.ErrorCount > 0)
        {
            // we encountered errors in the CPU or format option so just return
            return new AssemblyState(assemblyOptions, sourceFactory, symTable, logger, comparer);
        }
        var state = new AssemblyState(assemblyOptions, sourceFactory, symTable, logger, comparer)
        {
            InitialCpu = initialCpu,
            Format = initialFormat ?? OutputFormat.Cbm
        };
        
        BuiltIn.Define(state);
        var eval = new Evaluator(state);
        foreach (var define in assemblyOptions.Defines)
        {
            var parsedDefine = Parser.ParseDefine(define);
            var value = parsedDefine.Value != null
                ? eval.Visit(parsedDefine.Value)
                : new Value(1);
            if (value == null)
            {
                state.Logger.LogError(new CompileException(CompileExceptionType.ValueNotConstant, parsedDefine));
                break;
            }
            if (symTable.TryDefineConstant(parsedDefine.Label, value, out _)) continue;
            state.Logger.LogError(
                new CompileException(CompileExceptionType.SymbolRedefined, parsedDefine));
            break;
        }
        foreach (var section in assemblyOptions.Sections)
        {
            var sectionParts = section.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var ends = Address.BadAddress;
            if (sectionParts.Length is < 2 or > 3 ||
                !int.TryParse(sectionParts[1], out var starts) ||
                (sectionParts.Length == 3 &&  !int.TryParse(sectionParts[2], out ends)))
            {
                state.Logger.LogFatalError($"Section definition `{sectionParts[0]}` is invalid");
                break;
            }
            try
            {
                state.Output.DefineSection(sectionParts[0], starts, ends);
            }
            catch (SectionException sectionException)
            {
                state.Logger.LogFatalError(
                    $"Section definition `{sectionParts[0]}` is invalid because of error:\n{sectionException.Message}");
                break;
            }
        }
        state.Reset();
        return state;
    }
}