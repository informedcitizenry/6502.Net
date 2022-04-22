//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that creates and helps a <see cref="Sixty502DotNetLexer"/> 
    /// work with sourceto be preprocessed before main parsing.
    /// </summary>
    public class Preprocessor
    {
        private static readonly Regex s_macroDefinition =
            new(@"(\p{L}(\p{L}|[0-9_])*)(\\[\r\n]|[ \t]|(/\*.*?\*/))*\.macro",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex s_macroInvocation =
            new(@"(\.\p{L}(\p{L}|[0-9_])*)", RegexOptions.Compiled);

        private readonly TokenFactory _tokenFactory;
        private readonly Dictionary<string, Macro> _macros;
        private readonly Stack<IDictionary<string, string>> _macroInvokeArgStack;
        private readonly AssemblyServices _services;

        /// <summary>
        /// Construct a new instance of the <see cref="Preprocessor"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="fileName">The filename of the source.</param>
        public Preprocessor(AssemblyServices services, string fileName)
            : this(services, CharStreams.fromPath(fileName))
        {
            _tokenFactory.Filenames.Pop();
            _tokenFactory.Filenames.Push(fileName);
            _tokenFactory.Included.Push(false);
        }

        /// <summary>
        /// Construct a new instance of the <see cref="Preprocessor"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="input">The input stream to the source.</param>
        public Preprocessor(AssemblyServices services, ICharStream input)
        {
            _tokenFactory = new();
            InputFilesProcessed = new SortedSet<string>();
            _macros = new(services.StringComparer);
            _macroInvokeArgStack = new();
            _services = services;
            _macroInvokeArgStack.Push(new Dictionary<string, string>());
            _tokenFactory.Filenames.Push("<unknown>");
            _tokenFactory.Included.Push(false);
            Lexer = InitializeLexer(input);
        }

        //// <summary>
        /// Construct a new instance of the <see cref="Preprocessor"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public Preprocessor(AssemblyServices services)
        {
            _tokenFactory = new();
            InputFilesProcessed = new SortedSet<string>();
            _macros = new(services.StringComparer);
            _macroInvokeArgStack = new();
            _services = services;
            _macroInvokeArgStack.Push(new Dictionary<string, string>());
            _tokenFactory.Included.Push(false);
            var inputFiles = services.Options.InputFiles;
            if (inputFiles?.Count > 0)
            {
                var source = FileHelper.GetPath(inputFiles[0], services.Options.IncludePath);
                if (string.IsNullOrEmpty(source))
                {
                    services.Log.LogEntrySimple($"Source file \"{inputFiles[0]}\" could not be opened.");
                    return;
                }
                _tokenFactory.Filenames.Push(source);
                InputFilesProcessed.Add(source);
                Lexer = InitializeLexer(CharStreams.fromPath(source));
                for (var i = inputFiles.Count - 1; i > 0; i--)
                {
                    source = FileHelper.GetPath(inputFiles[i], services.Options.IncludePath);
                    if (string.IsNullOrEmpty(source))
                    {
                        services.Log.LogEntrySimple($"Source file \"{inputFiles[i]}\" could not be opened.");
                        return;
                    }
                    Lexer.Sources.Push(source);
                }
            }
            else
            {
                throw new Error("No input file specified.");
            }
        }

        private Sixty502DotNetLexer InitializeLexer(ICharStream input)
        {
            var lexer = new Sixty502DotNetLexer(input)
            {
                Preprocessor = this,
                TokenFactory = _tokenFactory,
            };
            if (string.IsNullOrEmpty(_services.Options?.CPU) &&
                string.IsNullOrEmpty(_services.CPU))
            {
                lexer.InstructionSet = new M65xx(_services, "6502");
                lexer.SetMnemonics("6502");
            }
            else
            {
                var cpu = _services.Options!.CPU ?? _services.CPU;
                _services.CPU = cpu;
                lexer.InstructionSet = InstructionSetSelector.Select(_services, cpu);
                lexer.SetMnemonics(cpu);
            }
            return lexer;
        }

        private IList<IToken> StreamToTokens(Stream stream)
        {
            var input = CharStreams.fromStream(stream);
            Sixty502DotNetLexer lexer = InitializeLexer(input);
            return lexer.GetAllTokens();
        }

        private static Stream StringToStream(string s)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write(s);
            sw.Flush();
            ms.Position = 0;
            return ms;
        }

        private PreprocessorParser ParseFromTokenList(IList<IToken> tokens)
        {
            var tokenSource = new ListTokenSource(tokens);
            var parser = new PreprocessorParser(new CommonTokenStream(tokenSource, Sixty502DotNetLexer.PREPROCESSOR));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(_services.Log);
            return parser;
        }

        /// <summary>
        /// Include a filename in the processed token stream and return
        /// to the lexer as a list of tokens.
        /// </summary>
        /// <param name="includeName">The include instruction name.</param>
        /// <param name="includeArg">The include argument token, which is the
        /// filename/filepath.</param>
        /// <returns></returns>
        public IList<IToken> Include(string includeName, IToken includeArg)
        {
            var scope = includeName[1] == 'b' || includeName[1] == 'B';
            if (includeName.Contains('\\') && Path.DirectorySeparatorChar != '\\')
            {
                _services.Log.LogEntry((Token)includeArg,
                    "Directory separator '\\' might not be supported in host operating system.", false);
            }
            var filename = FileHelper.GetPath(includeArg.Text.TrimOnce('"'), _services.Options.IncludePath);
            if (!string.IsNullOrEmpty(filename))
            {
                InputFilesProcessed.Add(filename);
                _tokenFactory.Filenames.Push(filename);
                _tokenFactory.IncludeLineNumbers.Push(includeArg.Line);
                _tokenFactory.Included.Push(true);
                try
                {
                    var tokens = Load(filename, scope);
                    return tokens;
                }
                finally
                {
                    _tokenFactory.IncludeLineNumbers.Pop();
                    _tokenFactory.Filenames.Pop();
                    _tokenFactory.Included.Pop();

                }
            }
            _services.Log.LogEntry((Token)includeArg, $"file {includeArg.Text} not found.");
            return new List<IToken>();
        }

        /// <summary>
        /// Set the CPU instruction set for the passed lexer.
        /// </summary>
        /// <param name="lexer">The <see cref="LexerBase"/>
        /// to set its instruction set.</param>
        /// <param name="cpuToken">The token declaring the <c>.cpu</c>
        /// directive.</param>
        /// <param name="cpuName">The token containing the CPU name.</param>
        public void SetCpu(LexerBase lexer, IToken cpuToken, IToken cpuName)
        {
            // was CPU set in options?
            if (!string.IsNullOrEmpty(_services.Options.CPU))
            {
                _services.Log.LogEntry((Token)cpuToken, "\".cpu\" directive ignored. Option set in command line.", false);
                return;

            }
            if (cpuName.Type != Sixty502DotNetLexer.StringLiteral)
            {
                _services.Log.LogEntry((Token)cpuName, "\".cpu\" directive requires string literal argument.");
                return;
            }
            _services.CPU = cpuName.Text.TrimOnce('"');
            // is this the first token we've seen?
            if (ReferenceEquals(Lexer?.FirstSeen, cpuToken))
            {
                try
                {
                    lexer.InstructionSet = InstructionSetSelector.Select(_services, _services.CPU);
                }
                catch (Error ex)
                {
                    _services.Log.LogEntry((Token)cpuName, ex.Message);
                    return;
                }
            }
            // set the lexer mnemonics from its current instruction set based on the cpu type.
            try
            {
                lexer.SetMnemonics(_services.CPU);
            }
            catch
            {
                _services.Log.LogEntry((Token)cpuToken, $"Invalid cpu type \"{_services.CPU}\" specified.");
            }
        }

        /// <summary>
        /// Get the macro name from the macro declaration, which is typically
        /// <c>macro_name .macro</c>
        /// </summary>
        /// <param name="decl">The declaration text.</param>
        /// <returns></returns>
        public static string GetMacroName(string decl)
        {
            var match = s_macroDefinition.Match(decl);
            if (match.Success)
            {
                return $".{match.Groups[1].Value}";
            }
            return string.Empty;
        }

        /// <summary>
        /// Determines if the given text is a defined macro.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool IsAMacro(string text)
            => _macros.ContainsKey(text);

        /// <summary>
        /// Define a macro from a list of tokens.
        /// </summary>
        /// <param name="macroToken">The declaration token.</param>
        /// <param name="macroName">The macro name.</param>
        /// <param name="defTokens">The macro definition tokens to be
        /// parsed.</param>
        public void DefineMacro(IToken macroToken, string macroName, IList<IToken> defTokens)
        {
            var macroBlock = ParseFromTokenList(defTokens).macroBlock();
            var macroDeclaration = macroBlock.macroDeclaration();
            try
            {
                _macros[macroName] = new Macro((Token)macroToken, macroDeclaration.macroArgList(), macroBlock);
            }
            catch (Error e)
            {
                if (e.Context != null)
                {
                    _services.Log.LogEntry(e.Context, e.Message);
                }
                else
                {
                    _services.Log.LogEntrySimple(e.Message);
                }
            }
        }

        /// <summary>
        /// Expand a macro with given parameters into source, including all
        /// substitutions.s
        /// </summary>
        /// <param name="invokeLine">The line number of the macro invocation.
        /// This is tracked for diagnostic purposes.</param>
        /// <param name="invokeText">The invocation text (macro name).</param>
        /// <param name="invokeArgs">The arguments to pass to expand the
        /// defined macro at the corresponding substitution points.</param>
        /// <returns></returns>
        public IList<IToken> ExpandMacro(int invokeLine, string invokeText, IList<IToken> invokeArgs)
        {
            var name = s_macroInvocation.Match(invokeText).Groups[1].Value;
            if (_macros.TryGetValue(name, out var macro))
            {
                var invokeAst = ParseFromTokenList(invokeArgs).macroInvocation();
                var args = invokeAst.macroInvocationArgList();
                var newArgs = macro.GetArgList(args, _macroInvokeArgStack.Peek());
                var expander = new MacroExpander(newArgs, _services);
                _macroInvokeArgStack.Push(newArgs);
                var macroBlockStr = new StringBuilder(".block\n");
                _ = macroBlockStr.Append(expander.Visit(macro.Definition))
                                 .AppendLine(".endblock");
                _macroInvokeArgStack.Pop();
                if (!_services.Log.HasErrors)
                {
                    try
                    {
                        _tokenFactory.MacroInvokeLines.Push(new Tuple<string, int>(_tokenFactory.Filenames.Peek(), invokeLine));
                        var expandedTokens = StreamToTokens(StringToStream(macroBlockStr.ToString()));
                        macro.IsReferenced = true;
                        return expandedTokens;
                    }
                    finally
                    {
                        _tokenFactory.MacroInvokeLines.Pop();
                    }
                }
            }
            return new List<IToken>();
        }

        /// <summary>
        /// Load a list of tokens from a file specified by the filename and
        /// optionally include them in block scope.
        /// </summary>
        /// <param name="filename">The source filename.</param>
        /// <param name="scope">Indicate if the source should be in
        /// its own scope.</param>
        /// <returns></returns>
        public IList<IToken> Load(string filename, bool scope)
        {
            using var fs = File.OpenRead(filename);
            if (scope)
            {
                using var sr = new StreamReader(fs);
                var ms = StringToStream($".block\n{sr.ReadToEnd()}\n.endblock\n");
                return StreamToTokens(ms);
            }
            return StreamToTokens(fs);
        }

        /// <summary>
        /// Get a list of macros as parsed <see cref="Token"/> objects that
        /// were defined in the source but never referenced elsewhere.
        /// </summary>
        /// <returns>The list of macros.</returns>
        public IReadOnlyCollection<Token> GetUnreferencedMacroTokens()
        {
            var unreferencedMacros = _macros.Values.Where(m => !m.IsReferenced).Select(m => m.DefinitionToken);
            return unreferencedMacros.ToList().AsReadOnly();
        }

        /// <summary>
        /// Get the preprocessor's lexer.
        /// </summary>
        public Sixty502DotNetLexer? Lexer { get; init; }

        /// <summary>
        /// Get the input files processed, based on the order they were specified
        /// in the command-line options.
        /// </summary>
        public SortedSet<string> InputFilesProcessed { get; init; }
    }
}
