//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing source text before proper assembly, such as
    /// macro expansion and comment scrubbing.
    /// </summary>
    public class Preprocessor : AssemblerBase
    {
        #region Members

        readonly Dictionary<string, Macro> _macros;
        readonly HashSet<string> _includedFiles;
        bool _processingStarted;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the preprocessor class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public Preprocessor(AssemblyServices services)
            : base(services)
        {
            Reserved.DefineType("Directives",
                ".macro",".endmacro", ".include", ".binclude");

            Reserved.DefineType("MacroNames");

            _processingStarted = false;
            _includedFiles = new HashSet<string>();
            _macros = new Dictionary<string, Macro>();
        }

        #endregion

        #region Methods

        IEnumerable<SourceLine> ExpandInclude(SourceLine line)
        {
            var expanded = new List<SourceLine>();
            if (!line.OperandHasToken)
                throw new Exception();
            var include = line.OperandExpression;
            if (!include.EnclosedInDoubleQuotes())
                throw new Exception();

            include = include.TrimOnce('"');

            if (line.InstructionName[1] == 'b')
                expanded.Add(GetBlockDirectiveLine(include, line.LineNumber, line.LabelName, ".block"));

            expanded.AddRange(PreprocessFile(include));

            if (line.InstructionName[1] == 'b')
                expanded.Add(GetBlockDirectiveLine(include, line.LineNumber, ".endblock"));

            return expanded;
        }

        static string ProcessComments(string fileName, string source)
        {
            char c;
            int lineNumber = 1;
            var iterator = source.GetIterator();
            var uncommented = new StringBuilder();
            while ((c = iterator.GetNext()) != char.MinValue)
            {
                var peekNext = iterator.PeekNext();
                if (c == '/' && peekNext == '*')
                {
                    var endBlock = false;
                    iterator.MoveNext();
                    while ((c = iterator.FirstOrDefault(c => c == '\n' || c == '*')) != char.MinValue)
                    {
                        if (c == '\n')
                        {
                            lineNumber++;
                            uncommented.Append(c);
                        }
                        else if ((peekNext = iterator.PeekNext()) == '/')
                        {
                            endBlock = true;
                            break;
                        }
                    }
                    if (!endBlock)
                        throw new Exception($"{fileName}({lineNumber}): End of file reached before \"*/\" found.");
                    if (!iterator.MoveNext())
                        break;
                }
                else if (c == '*' && peekNext == '/')
                {
                    throw new Exception($"{fileName}({lineNumber}): \"*/\" does not close a comment block.");
                }
                else
                {
                    var isSemi = c == ';';
                    if (isSemi || (c == '/' && peekNext == '/'))
                    {
                        while (c != '\n' && c != char.MinValue)
                        {
                            if (isSemi)
                                uncommented.Append(c);
                            c = iterator.GetNext();
                        }
                        if (c == char.MinValue)
                            break;
                    }
                    uncommented.Append(c);
                    if (c == '"' || c == '\'')
                    {
                        var close = c;
                        while ((c = iterator.GetNext()) != char.MinValue)
                        {
                            uncommented.Append(c);
                            if (c == '\\')
                            {
                                if ((c = iterator.GetNext()) == char.MinValue)
                                    break;
                                uncommented.Append(c);
                            }
                            else if (c == close)
                            {
                                break;
                            }
                        }
                    }
                    else if (c == '\n')
                        lineNumber++;
                }
            }
            return uncommented.ToString(); 
        }

        IEnumerable<SourceLine> ProcessDirectives(IEnumerable<SourceLine> uncommented)
        {
            var macroProcessed = new List<SourceLine>();
            RandomAccessIterator<SourceLine> lineIterator = uncommented.GetIterator();
            SourceLine line = null;
            while ((line = lineIterator.GetNext()) != null)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(line.ParsedSource))
                    {
                        macroProcessed.Add(line);
                        continue;
                    }
                    if (line.InstructionName.Equals(".cpu"))
                        throw new SyntaxException(line.Instruction.Position,
                                "\".cpu\" directive must be precede all others in sources.");

                    _processingStarted = true;

                    if (line.InstructionName.Equals(".macro"))
                    {
                        if (string.IsNullOrEmpty(line.LabelName))
                            throw new SyntaxException(line.Instruction.Position,
                                "Macro name not specified.");

                        var macroName = "." + line.LabelName;

                        if (_macros.ContainsKey(macroName))
                            throw new SyntaxException(line.Label.Position, 
                                $"Macro named \"{line.LabelName}\" already defined.");
                        if (Services.IsReserved.Any(i => i.Invoke(macroName)) ||
                            !(char.IsLetter(line.LabelName[0]) && 
                              (char.IsLetterOrDigit(macroName[^1]) || macroName[^1] == '_')))
                            throw new SyntaxException(line.Label.Position, 
                                $"Macro name \"{line.LabelName}\" is not valid.");
                        
                        Reserved.AddWord("MacroNames", macroName);

                        var macro = new Macro(Services, 
                                              line.Operand, 
                                              line.ParsedSource);
                        _macros[macroName] = macro;
                        macro.AddSource(GetBlockDirectiveLine(line.Filename, line.LineNumber, ".block"));
                        var instr = line;
                        while ((line = lineIterator.GetNext()) != null && !line.InstructionName.Equals(".endmacro"))
                        {
                            if (macroName.Equals(line.InstructionName))
                                throw new SyntaxException(line.Instruction.Position,
                                    "Recursive macro call not allowed.");
                            if (line.InstructionName.Equals(".macro"))
                                throw new SyntaxException(line.Instruction.Position,
                                    "Nested macro definitions not allowed.");
                            
                            if (line.InstructionName.Equals(".include") || line.InstructionName.Equals(".binclude"))
                            {
                                var includes = ExpandInclude(line);
                                foreach (var incl in includes)
                                {
                                    if (macroName.Equals(incl.InstructionName))
                                        throw new SyntaxException(incl.Instruction.Position,
                                            "Recursive macro call not allowed.");
                                    
                                    macro.AddSource(incl);
                                }
                            }
                            else
                            {
                                macro.AddSource(line);
                            }
                        }
                        if (!string.IsNullOrEmpty(line.LabelName))
                        {
                            if (line.OperandHasToken)
                                throw new SyntaxException(line.Operand.Position,
                                    "Unexpected argument found for macro definition closure.");
                            macro.AddSource(LexerParser.Parse(line.Filename, line.LabelName, Services, true)
                                                        .First()
                                                        .WithLineNumber(line.LineNumber));
                        }
                        else if (line == null)
                        {
                            line = instr;
                            throw new SyntaxException(instr.Instruction.Position,
                                "Missing closure for macro definition.");
                        }
                        macro.AddSource(GetBlockDirectiveLine(line.Filename, line.LineNumber, ".endblock"));
                    }
                    else if (line.InstructionName.Equals(".include") || line.InstructionName.Equals(".binclude"))
                    {
                        macroProcessed.AddRange(ExpandInclude(line));
                    }
                    else if (_macros.ContainsKey(line.InstructionName))
                    {
                        if (!string.IsNullOrEmpty(line.LabelName))
                            macroProcessed.AddRange(LexerParser.Parse(line.Filename, line.LabelName, Services));
                        Macro macro = _macros[line.InstructionName];                        
                        macroProcessed.AddRange(ProcessExpansion(macro.Expand(line.Operand)));
                    }
                    else if (line.InstructionName.Equals(".endmacro"))
                    {
                        throw new SyntaxException(line.Instruction.Position,
                            "Directive \".endmacro\" does not close a macro definition.");                    }
                    else
                    {
                        macroProcessed.Add(line);
                    }
                }
                catch (SyntaxException ex)
                {
                    Services.Log.LogEntry(line, ex.Position, ex.Message);
                }
            }
            return macroProcessed;
        }

        IEnumerable<SourceLine> ProcessExpansion(IEnumerable<SourceLine> sources)
        {
            var processed = new List<SourceLine>();
            foreach (SourceLine line in sources)
            {
                if (line.InstructionName.Equals(".include") || line.InstructionName.Equals(".binclude"))
                {
                    processed.AddRange(ExpandInclude(line));
                }
                else if (_macros.ContainsKey(line.InstructionName))
                {
                    Macro macro = _macros[line.InstructionName];
                    processed.AddRange(ProcessExpansion(macro.Expand(line.Operand)));
                }
                else if (line.InstructionName.Equals(".endmacro"))
                {
                    Services.Log.LogEntry(line, line.Instruction,
                        "Directive \".endmacro\" does not close a macro definition.");
                    break;
                }
                else
                {
                    processed.Add(line);
                }
            }
            return processed;
        }

        /// <summary>
        /// Perforsm preprocessing of the input string as a label define expression.
        /// </summary>
        /// <param name="defineExpression">The define</param>
        /// <returns>A <see cref="SourceLine"/> representing the parsed label define.</returns>
        /// <exception cref="Exception"/>
        public SourceLine PreprocessDefine(string defineExpression)
        {
            if (!defineExpression.Contains('='))
                defineExpression += "=1";

            var defines = LexerParser.Parse(string.Empty, defineExpression, Services);
            if (defines.Count() != 1)
                throw new Exception($"Define expression \"{defineExpression}\" is not valid.");
            var line = defines.First();
            if (line.Label == null || line.Instruction == null || !line.InstructionName.Equals("=") || line.Operand == null)
                throw new Exception($"Define expression \"{defineExpression}\" is not valid.");
            if (!line.OperandExpression.EnclosedInDoubleQuotes() && 
                !Services.Evaluator.ExpressionIsConstant(line.Operand))
                throw new Exception($"Define expression \"{line.Operand}\" is not a constant.");
            return line;
        }

        /// <summary>
        /// Returns a new source line representing a block or endblock directive.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <param name="lineNumber">The source's line number.</param>
        /// <param name="directive">The directive name.</param>
        /// <returns></returns>
        public SourceLine GetBlockDirectiveLine(string fileName, int lineNumber, string directive)
            => GetBlockDirectiveLine(fileName, lineNumber, string.Empty, directive);

        /// <summary>
        /// Returns a new source line representing a block or endblock directive.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <param name="lineNumber">The source's line number.</param>
        /// <param name="label">The label to attach the block line.</param>
        /// <param name="directive">The directive name.</param>
        /// <returns></returns>
        public SourceLine GetBlockDirectiveLine(string fileName, int lineNumber, string label, string directive)
        {
            var source = string.IsNullOrEmpty(label) ? directive : $"{label} {directive}";
            var parsed = LexerParser.Parse(fileName, source, Services, true).First();
            return parsed.WithLineNumber(lineNumber);
        }

        /// <summary>
        /// Perform preprocessing of the source text within the source file, 
        /// including comment scrubbing and macro creation and expansion.
        /// </summary>
        /// <param name="fileName">The source file.</param>
        /// <returns>A collection of parsed <see cref="SourceLine"/>s.</returns>
        public IEnumerable<SourceLine> PreprocessFile(string fileName)
        {
            string source = string.Empty;
            string fullPath = string.Empty;
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists)
            {
                using var fs = fileInfo.OpenText();
                fullPath = fileInfo.FullName;
                source = fs.ReadToEnd();
            }
            else if (!string.IsNullOrEmpty(Services.Options.IncludePath))
            {
                fullPath = Path.Combine(Services.Options.IncludePath, fileName);
                if (File.Exists(fullPath))
                    source = File.ReadAllText(fullPath);
                else
                    throw new FileNotFoundException($"Source \"{fileInfo.FullName}\" not found.");
            }
            else
            {
                throw new FileNotFoundException($"Source \"{fileInfo.FullName}\" not found.");
            }
            var sourceInvalid = string.IsNullOrEmpty(source) ||
                                source.Take(5).Any(c => char.IsControl(c) && !char.IsWhiteSpace(c));
            
            if (sourceInvalid)
                throw new Exception($"File \"{fileName}\" may be empty or in an unrecognized file format.");

            var location = new Uri(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            var dirInfo = new DirectoryInfo(location.AbsolutePath);
            if (Path.GetDirectoryName(dirInfo.FullName).Equals(Path.GetDirectoryName(fullPath)))
                fullPath = fileName;
            if (_includedFiles.Contains(fullPath))
                throw new FileLoadException($"File \"{fullPath}\" already included in source.");
            _includedFiles.Add(fullPath);
            return Preprocess(fileName, source);
        }

        IEnumerable<SourceLine> Preprocess(string fileName, string source)
        {
            source = source.Replace("\r", string.Empty); // remove Windows CR
            source = ProcessComments(fileName, source);

            // we cannot do proper parsing until the cpu assembler is properly configured first.
            string cpu = Services.Options.CPU;
            var firstLine = LexerParser.Parse(fileName, source, Services, true)
                                       .FirstOrDefault(l => !string.IsNullOrEmpty(l.ParsedSource));
            if (firstLine != null && firstLine.InstructionName.Equals(".cpu"))
            {
                if (_processingStarted)
                {
                    Services.Log.LogEntry(firstLine, firstLine.Instruction,
                          "\".cpu\" directive must precede all others in sources.");
                }
                else
                {
                    if (!string.IsNullOrEmpty(firstLine.LabelName))
                        Services.Log.LogEntry(firstLine, firstLine.Label, 
                            "Label definition ignored for directive \".cpu\".", false);
                    if (!string.IsNullOrEmpty(Services.Options.CPU))
                        Services.Log.LogEntry(firstLine, firstLine.Instruction,
                            "\".cpu\" directive ignores option '--cpu'.", false);
                    cpu = firstLine.OperandExpression.Trim();
                    if (!cpu.EnclosedInDoubleQuotes())
                    {
                        Services.Log.LogEntry(firstLine, firstLine.Operand,
                           "Expression must be a string.");
                    }
                    else
                    {
                        cpu = cpu.TrimOnce('"');

                        // scrub out the .cpu directive line since it's been evaluated.
                        var postIndex = firstLine.IndexInSource + firstLine.UnparsedSource.Length;
                        var preSource = string.Empty;
                        if (firstLine.IndexInSource != 0)
                            preSource = source.Substring(0, firstLine.IndexInSource);
                        if (source.Length > postIndex)
                            source = preSource + source.Substring(postIndex);
                        else
                            source = preSource;
                    }
                }
            }
            if (!_processingStarted)
                Services.SelectCPU(cpu);
            return ProcessDirectives(LexerParser.Parse(fileName, source, Services));
        }

        /// <summary>
        /// Gets the input filenames that were processed by the preprocessor.
        /// </summary>
        /// <returns>A collection of input files.</returns>
        public ReadOnlyCollection<string> GetInputFiles()
            => new ReadOnlyCollection<string>(_includedFiles.ToList());

        protected override string OnAssembleLine(SourceLine line) => throw new NotImplementedException();

        public override bool Assembles(string keyword)
            => Reserved.IsReserved(keyword) || (!string.IsNullOrEmpty(keyword) && keyword[0] == '.');

        #endregion
    }
}