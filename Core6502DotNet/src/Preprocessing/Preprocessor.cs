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

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the preprocessor class.
        /// </summary>
        public Preprocessor()
        {
            Reserved.DefineType("Directives",
                ".comment", ".endcomment", ".macro",
                ".endmacro", ".include", ".binclude");

            Reserved.DefineType("MacroNames");

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
            var include = line.Operand.ToString();
            if (!include.EnclosedInDoubleQuotes())
                throw new Exception();

            include = include.TrimOnce('"');

            if (line.InstructionName[1] == 'b')
                expanded.Add(Macro.GetBlockDirectiveLine(include, line.LineNumber, line.LabelName, ".block"));

            expanded.AddRange(PreprocessFile(include));

            if (line.InstructionName[1] == 'b')
                expanded.Add(Macro.GetBlockDirectiveLine(include, line.LineNumber, ".endblock"));

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
                    iterator.MoveNext();
                    while ((c = iterator.Skip(c => c != '\n' && c != '*')) != char.MinValue)
                    {
                        if (c == '\n')
                        {
                            lineNumber++;
                            uncommented.Append(c);
                        }
                        if ((peekNext = iterator.PeekNext()) == '/')
                        {
                            break;
                        }
                    }
                    if (!iterator.MoveNext())
                        break;
                }
                else
                {
                    if (c == ';' || (c == '/' && peekNext == '/'))
                    {
                        var isSemi = c == ';';

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
            return uncommented.ToString(); ;
        }

        IEnumerable<SourceLine> ProcessMacros(IEnumerable<SourceLine> uncommented)
        {
            var macroProcessed = new List<SourceLine>();
            RandomAccessIterator<SourceLine> lineIterator = uncommented.GetIterator();
            SourceLine line = null;
            while ((line = lineIterator.GetNext()) != null)
            {
                if (string.IsNullOrWhiteSpace(line.ParsedSource))
                {
                    macroProcessed.Add(line);
                    continue;
                }
                if (line.InstructionName.Equals(".macro"))
                {
                    if (string.IsNullOrEmpty(line.LabelName))
                    {
                        Assembler.Log.LogEntry(line, line.Instruction, "Macro name not specified.");
                        continue;
                    }
                    var macroName = "." + line.LabelName;
                    if (_macros.ContainsKey(macroName))
                    {
                        Assembler.Log.LogEntry(line, line.Label, $"Macro named \"{line.LabelName}\" already defined.");
                        continue;
                    }
                    if (Assembler.IsReserved.Any(i => i.Invoke(macroName)) ||
                        !char.IsLetter(line.LabelName[0]))
                    {
                        Assembler.Log.LogEntry(line, line.Label, $"Macro name \"{line.LabelName}\" is not valid.");
                        continue;
                    }
                    Reserved.AddWord("MacroNames", macroName);

                    var macro = new Macro(line.Operand, line.ParsedSource);
                    _macros[macroName] = macro;
                    SourceLine instr = line;
                    while ((line = lineIterator.GetNext()) != null && !line.InstructionName.Equals(".endmacro"))
                    {
                        if (macroName.Equals(line.InstructionName))
                        {
                            Assembler.Log.LogEntry(line, line.Instruction, "Recursive macro call not allowed.");
                            continue;
                        }
                        if (line.InstructionName.Equals(".macro"))
                        {
                            Assembler.Log.LogEntry(line, line.Instruction, "Nested macro definitions not allowed.");
                            continue;
                        }
                        if (line.InstructionName.Equals(".include") || line.InstructionName.Equals(".binclude"))
                        {
                            IEnumerable<SourceLine> includes = ExpandInclude(line);
                            foreach (SourceLine incl in includes)
                            {
                                if (macroName.Equals(incl.InstructionName))
                                {
                                    Assembler.Log.LogEntry(incl, incl.Instruction, "Recursive macro call not allowed.");
                                    continue;
                                }
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
                        {
                            Assembler.Log.LogEntry(line, line.Operand, "Unexpected argument found for macro definition closure.");
                            continue;
                        }
                        line.Instruction = null;
                        line.ParsedSource = line.ParsedSource.Replace(".endmacro", string.Empty);
                        macro.AddSource(line);
                    }
                    else if (line == null)
                    {
                        line = instr;
                        Assembler.Log.LogEntry(instr, instr.Instruction, "Missing closure for macro definition.");
                        continue;
                    }
                }
                else if (line.InstructionName.Equals(".include") || line.InstructionName.Equals(".binclude"))
                {
                    macroProcessed.AddRange(ExpandInclude(line));
                }
                else if (_macros.ContainsKey(line.InstructionName))
                {
                    if (!string.IsNullOrEmpty(line.LabelName))
                    {
                        SourceLine clone = line.Clone();
                        clone.Operand =
                        clone.Instruction = null;
                        clone.UnparsedSource =
                        clone.ParsedSource = line.LabelName;
                        macroProcessed.Add(clone);
                    }
                    Macro macro = _macros[line.InstructionName];
                    macroProcessed.AddRange(ProcessExpansion(macro.Expand(line.Operand)));
                }
                else if (line.InstructionName.Equals(".endmacro"))
                {
                    Assembler.Log.LogEntry(line, line.Instruction,
                        "Directive \".endmacro\" does not close a macro definition.");
                    continue;
                }
                else
                {
                    macroProcessed.Add(line);
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
                    Assembler.Log.LogEntry(line, line.Instruction,
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
        /// Perform preprocessing of the source text, 
        /// including comment scrubbing and macro creation and expansion.
        /// </summary>
        /// <param name="source">The source text.</param>

        public IEnumerable<SourceLine> PreprocessSource(string source)
            => Preprocess(string.Empty, source);

        /// <summary>
        /// Perform preprocessing of the source text within the source file, 
        /// including comment scrubbing and macro creation and expansion.
        /// </summary>
        /// <param name="fileName">The source file.</param>
        /// <returns>A collection of parsed <see cref="SourceLine"/>s.</returns>
        public IEnumerable<SourceLine> PreprocessFile(string fileName)
        {
            var source = SourceHelper.ReadSource(fileName, out string fullPath);
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
            var uncommented = LexerParser.Parse(fileName, source);
            // process older .comment/.endcomments
            var lineIterator = uncommented.GetIterator();
            SourceLine line;
            while ((line = lineIterator.Skip(l => !l.InstructionName.Equals(".comment"))) != null)
            {
                Assembler.Log.LogEntry(line, line.Instruction,
                    "Directive \".comment\" is deprecated. Consider using '/*' for comment blocks instead.", false);

                line.Reset();
                while ((line = lineIterator.GetNext()) != null && !line.InstructionName.Equals(".endcomment"))
                    line.Reset();
                if (line == null)
                    throw new Exception("Missing closing \".endcomment\" for \".comment\" directive.");
                line.Reset();
            }
            return ProcessMacros(uncommented);
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