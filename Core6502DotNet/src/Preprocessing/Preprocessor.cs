//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// A preprocessor error.
    /// </summary>
    public class ProcessorException : Exception
    {
        /// <summary>
        /// Create a new instance of a preprocessor error.
        /// </summary>
        /// <param name="fileName">The name of the source file that raised the error.</param>
        /// <param name="lineLinumber">The line number in the source that raised the error.</param>
        /// <param name="linePosition">The position in the line that raised the error.</param>
        /// <param name="message">The error message.</param>
        public ProcessorException(string fileName, int lineLinumber, int linePosition, string message)
            : base(message)
        {
            FileName = fileName;
            LineNumber = lineLinumber;
            LinePosition = linePosition;
        }

        /// <summary>
        /// The originating error's file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The originating error's line number in source.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The originating error's position in the line in source.
        /// </summary>
        public int LinePosition { get; }
    }

    /// <summary>
    /// Represents various processor options.
    /// </summary>
    public class ProcessorOptions
    {
        /// <summary>
        /// Gets or sets the instruction lookup function for determining whether tokens are
        /// instructions or other identifiers.
        /// </summary>
        public Func<StringView, bool> InstructionLookup { get; set; }

        /// <summary>
        /// Gets or sets the function that determines whether macro names are allowed.
        /// </summary>
        public Func<StringView, bool> IsMacroNameValid { get; set; }

        /// <summary>
        /// Gets or sets the function that determines whether the processor is at a new line.
        /// </summary>
        public Func<List<Token>, bool> LineTerminates { get; set; }

        /// <summary>
        /// gets or sets the case-sensitivity of how the processor parses keywords.
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets the flag to ignore comment colons.
        /// </summary>
        public bool IgnoreCommentColons { get; set; }

        /// <summary>
        /// Gets or sets the flag to determine whether to ignore unclosed block comments.
        /// </summary>
        public bool IgnoreUnclosedBlockComment { get; set; }

        /// <summary>
        /// Gets or sets the flag to determine whether to warn if a label does not begin in the
        /// leftmost position in a source line.
        /// </summary>
        public bool WarnOnLabelLeft { get; set; }

        /// <summary>
        /// Gets or sets the log to write processing errors to.
        /// </summary>
        public ErrorLog Log { get; set; }

        /// <summary>
        /// The CPU Assembler selector method.
        /// </summary>
        public Action<string> CPUAssemblerSelector { get; set; }

        /// <summary>
        /// Include path when resolving source file.
        /// </summary>
        public string IncludePath { get; set; }
    }

    /// <summary>
    /// Performs preprocessing and parsing of source files and text into <see cref="SourceLine"/> and <see cref="Token"/>
    /// objects.
    /// </summary>
    public class Preprocessor
    {
        #region Constants 

        const char EOS = '\0';

        #endregion

        #region Members

        static readonly Dictionary<char, char> s_openclose = new Dictionary<char, char>
        {
            { '(', ')' },
            { '{', '}' },
            { '[', ']' }
        };

        static readonly Dictionary<char, List<char>> s_compounds = new Dictionary<char, List<char>>()
        {
            { '|', new List<char>{ '|' } },
            { '&', new List<char>{ '&' } },
            { '<', new List<char>{ '<', '=' } },
            { '>', new List<char>{ '>', '=' } },
            { '=', new List<char>{ '=' } },
            { '!', new List<char>{ '=' } },
            { '^', new List<char>{ '^' } }
        };

        static readonly Regex s_defineRegex = new Regex(@"^((_+(\d|\p{L}))|\p{L})(\d|\p{L}|_)*((=.+)|$)");

        readonly ProcessorOptions _options;
        readonly Dictionary<string, Macro> _macros;
        readonly ReservedWords _preprocessors;
        readonly HashSet<string> _includedFiles;
        bool _lineHasErrors;
        int _index;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the preprocessor class.
        /// </summary>
        public Preprocessor()
            : this(new ProcessorOptions())
        {

        }

        /// <summary>
        /// Constructs a new instance of the preprocessor class. 
        /// </summary>
        /// <param name="opt">The <see cref="ProcessorOptions"/>.</param>
        public Preprocessor(ProcessorOptions opt)
        {
            _includedFiles = new HashSet<string>();
            _index = 0;
            _options = opt;
            _macros = new Dictionary<string, Macro>(_options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            _preprocessors = new ReservedWords(_options.CaseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase);
            _preprocessors.DefineType("Macros",
                ".macro", ".endmacro");
            _preprocessors.DefineType("Includes",
                ".binclude", ".include");
            _preprocessors.DefineType("End",
                ".end");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Set the preprocessor's options.
        /// </summary>
        /// <param name="opt">The <see cref="ProcessorOptions"/>.</param>
        /// <returns>This preprocessor.</returns>
        public Preprocessor WithOptions(Func<ProcessorOptions> opt)
        {
            var options = opt();
            _options.IgnoreUnclosedBlockComment = options.IgnoreUnclosedBlockComment;
            _options.IgnoreCommentColons = options.IgnoreCommentColons;
            if (options.Log != null)
                _options.Log = options.Log;
            if (options.InstructionLookup != null)
                _options.InstructionLookup = options.InstructionLookup;
            if (options.LineTerminates != null)
                _options.LineTerminates = options.LineTerminates;
            return this;
        }

        void LogError(string fileName, int lineNumber, int position, string message, string source)
            => LogError(fileName, lineNumber, position, message, source.Length, source);

        void LogError(string fileName, int lineNumber, int position, string message, int end, string source)
        {
            var realPosition = position - 1;
            var token = source.Length >= end ? source[realPosition..end] : source[realPosition..];
            LogError(fileName, lineNumber, position, message, token, source);
        }

        void LogError(string fileName, int lineNumber, int position, string message, string token, string source)
        {
            _lineHasErrors = true;
            if (_options.Log != null)
                _options.Log.LogEntry(fileName, ++lineNumber, position, message, token, source);
            else
                throw new ProcessorException(fileName, ++lineNumber, position, message);
        }

        SourceLine GetBlockDirective(string fileName, int lineNumber, string directive, Token label)
        {
            var tokens = new List<Token>();
            if (label != null)
                tokens.Add(label);
            if (!string.IsNullOrEmpty(directive))
                tokens.Add(new Token(directive, TokenType.Instruction));
            string src;
            if (label == null)
                src = directive;
            else if (string.IsNullOrEmpty(directive))
                src = label.Name.ToString();
            else
                src = $"{label.Name} {directive}";
            return new SourceLine(fileName, lineNumber, new List<string> { src }, tokens, _index++);
        }

        void IncludeFile(string fileName)
        {
            var location = new Uri(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            var dirInfo = new DirectoryInfo(location.AbsolutePath);
            if (Path.GetDirectoryName(dirInfo.FullName).Equals(Path.GetDirectoryName(fileName)))
                fileName = new FileInfo(fileName).Name;
            if (_includedFiles.Contains(fileName))
                throw new FileLoadException($"File \"{fileName}\" already included in source.");
            _includedFiles.Add(fileName);
        }

        static bool FileFormatIsValid(StreamReader sr)
        {
            var controls = 0;
            var nulls = 0;
            int c;
            while ((c = sr.Read()) != -1 && sr.BaseStream.Position < 80)
            {
                if (char.IsControl(Convert.ToChar(c)) && c != '\n' && c != '\r')
                {
                    if (c == char.MinValue)
                        if (++nulls > 2)
                            return false;
                        else
                            nulls = 0;
                    if (++controls > 5)
                        return false;
                }
            }
            sr.BaseStream.Position = 0;
            sr.DiscardBufferedData();
            return true;
        }

        List<SourceLine> ProcessFromStream(string fileName, int lineNumber, StreamReader sr, bool stopAtFirstInstruction = false)
        {
            if (!FileFormatIsValid(sr))
                throw new Exception($"Format of \"{fileName}\" is not valid.");
            var sourceLines = new List<SourceLine>();
            var lineSources = new List<string>();
            bool blockComment, readyForNewLine, stopProcessing, previousWasPlus;
            var expected = TokenType.LabelInstr;
            var previousType = TokenType.None;
            var opens = new Stack<char>();
            Macro definingMacro = null;
            string nextLine, lineSource;
            blockComment = stopProcessing = previousWasPlus = false; readyForNewLine = true;
            List<Token> tokens = null;
            var linesProcessed = lineNumber;
            while ((nextLine = sr.ReadLine()) != null && !stopProcessing)
            {
                if (readyForNewLine)
                    StartNewLine();
                else
                    lineSources.Add(nextLine);
                lineSource = nextLine;
                linesProcessed++;
                var it = nextLine.GetIterator();
                char c;
                var previous = EOS;
                var isWidth = false;
                while (!_lineHasErrors && (c = it.GetNext()) != EOS)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        while (c == ' ' || c == '\t')
                        {
                            previous = c;
                            c = it.GetNext();
                        }
                        if (c == EOS)
                            break;
                    }
                    if (blockComment)
                    {
                        if (c == '*' && it.PeekNext() == '/')
                            blockComment = !it.MoveNext();
                        continue;
                    }
                    char peek = it.PeekNext();

                    if (c == '/' && peek == '/')
                        break;
                    if (c == '*' && peek == '/')
                    {
                        LogError(fileName, lineNumber, it.Index + 1, "\"*/\" does not close a block comment.", it.Index + 2, lineSource);
                        break;
                    }
                    if (c == '/' && peek == '*')
                    {
                        it.MoveNext();
                        blockComment = true;
                        continue;
                    }
                    if (c == ';')
                    {
                        if (!_options.IgnoreCommentColons)
                        {
                            expected = TokenType.Instruction;
                            c = it.FirstOrDefault(chr => chr == ':');
                        }
                        else
                        {
                            c = EOS;
                        }
                        if (c == EOS)
                            break;
                    }
                    if (c == ':')
                    {
                        if ((expected != TokenType.Instruction && expected != TokenType.EndOrBinary && 
                            (tokens.Count == 0 || tokens[^1].Type != TokenType.Instruction)) || !LineTerminates())
                        {
                            LogError(fileName, lineNumber, it.Index + 1, "Unexpected expression.", it.Index + 2, lineSource);
                            break;
                        }
                        lineSources[^1] = lineSources[^1].Substring(0, it.Index);
                        if (!it.MoveNext())
                            break;
                        ProcessLine(true);
                        nextLine = nextLine[it.Index..];
                        StartNewLine();
                        it = nextLine.GetIterator();
                    }
                    else
                    {
                        var position = it.Index;
                        var type = TokenType.Misc;
                        var size = 1;
                        var isIdent = false;
                        if (char.IsLetterOrDigit(c) ||
                            (c == '.' && (previous == '%'  || char.IsLetterOrDigit(peek))) ||
                            (c == '#' &&  previous == '%') ||
                             c == '"'  ||
                             c == '\'' ||
                             c == '_')
                        {
                            type = TokenType.Operand;
                            if ((previous == '$' && c.IsHex() && previousType == TokenType.Radix) ||
                                        (c == '0' && (peek == 'x' || peek == 'X')))
                            {
                                if (previous != '$')
                                    previous = it.GetNext();

                                while (c.IsHex() || (c == '_' && previous.IsHex() && it.PeekNext().IsHex()))
                                {
                                    previous = c;
                                    c = it.GetNext();
                                    peek = it.PeekNext();
                                }
                            }
                            else if (c == '0' && (peek == 'o' || peek == 'O'))
                            {
                                it.MoveNext();
                                while (char.IsDigit(c) || (c == '_' && char.IsDigit(previous) && char.IsDigit(it.PeekNext())))
                                {
                                    previous = c;
                                    c = it.GetNext();
                                    peek = it.PeekNext();
                                }
                            }
                            else if ((previous == '%' && previousType == TokenType.Radix) ||
                                        (c == '0' && (peek == 'b' || peek == 'B')))
                            {
                                char firstdigit = c;
                                if (previous != '%')
                                {
                                    it.MoveNext();
                                    firstdigit = it.PeekNext();
                                    previous = firstdigit;
                                }
                                bool alt = firstdigit == '.' || firstdigit == '#';
                                if (!alt && firstdigit != '0' && firstdigit != '1')
                                    continue;
                                char zero = alt ? '.' : '0';
                                char one = alt ? '#' : '1';

                                while (c == zero || c == one ||
                                    (c == '_' && (previous == zero || previous == one) && (it.PeekNext() == zero || it.PeekNext() == one)))
                                {
                                    previous = c;
                                    c = it.GetNext();
                                    peek = it.PeekNext();
                                }
                            }
                            else if (char.IsDigit(c) || (c == '.' && char.IsDigit(peek)))
                            {
                                bool decFound = c == '.';
                                bool expFound = false;
                                while (true)
                                {
                                    previous = c;
                                    if (!char.IsDigit(c = it.GetNext()))
                                    {
                                        peek = it.PeekNext();
                                        if (c == '.')
                                        {
                                            if (decFound || !char.IsDigit(peek))
                                                break;
                                            decFound = true;
                                        }
                                        else if (c == 'e' || c == 'E')
                                        {
                                            if (expFound || !char.IsDigit(previous) || !(char.IsDigit(peek) || peek != '-' || peek != '+'))
                                                break;
                                            expFound = true;
                                        }
                                        else if ((c != '-' && c != '+' && c != '.' && c != 'e' && c != 'E' && c != '_') ||
                                                ((c == '-' || c == '+') && (!expFound || (previous != 'e' && previous != 'E'))) ||
                                                 (c == '_' && !char.IsDigit(previous) && !char.IsDigit(peek)))
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (c == '"' || c == '\'')
                            {
                                previous = c;
                                while ((c = it.GetNext()) != previous && c != EOS)
                                {
                                    if (c == '\\')
                                    {
                                        if (!it.MoveNext())
                                        {
                                            LogError(fileName, lineNumber, position + 1, "String not enclosed.", lineSource);
                                            break;
                                        }
                                    }
                                }
                                if (c == EOS)
                                {
                                    LogError(fileName, lineNumber, position + 1, "String not enclosed.", lineSource);
                                    break;
                                }
                            }
                            else
                            {
                                isIdent = true;
                                
                                while ((c == '.' && (char.IsLetter(peek) || peek == '_')) || 
                                        char.IsLetterOrDigit(c) || c == '_')
                                {
                                    previous = c;
                                    c = it.GetNext();
                                    peek = it.PeekNext();
                                }
                                if (c == '\'')
                                {
                                    previous = c;
                                    c = peek;
                                }
                            }
                            size = it.Index - position;
                            if (previous == '\'' || previous == '"')
                            {
                                size++;
                            }
                            else
                            {
                                it.Rewind(it.Index - 1);
                                c = it.Current;
                                peek = it.PeekNext();
                            }
                        }
                        else if (c.IsOpenOperator())
                        {
                            type = TokenType.Open;
                            opens.Push(c);
                            isWidth = c == '[' && previousType == TokenType.Instruction;
                        }
                        else if (c.IsClosedOperator())
                        {
                            type = TokenType.Closed;
                            if (opens.Count == 0 || s_openclose[opens.Peek()] != c)
                            {
                                LogError(fileName, lineNumber, position + 1, "Invalid closure.", it.Index + 1, lineSource);
                                break;
                            }
                            opens.Pop();
                        }
                        else if (c == ',')
                        {
                            type = TokenType.Separator;
                        }
                        else if (c.IsOperator())
                        {
                            type = TokenType.Binary;
                            if (s_compounds.TryGetValue(c, out var comp) && comp.Contains(peek))
                            {
                                c = it.GetNext();
                                size++;
                            }
                        }
                        switch (expected)
                        {
                            case TokenType.LabelInstr:
                                if (isIdent || c == '=' || c == '*')
                                {
                                    if (c == '=' || _options.InstructionLookup(new StringView(nextLine, position, size)))
                                    {
                                        type = TokenType.Instruction;
                                        expected = TokenType.StartOrOperand;
                                        if (stopAtFirstInstruction)
                                            stopProcessing = true;
                                    }
                                    else
                                    {
                                        type = TokenType.Label;
                                        expected = TokenType.Instruction;
                                        if (c != '*' && position > 0 && _options.WarnOnLabelLeft)
                                            _options.Log?.LogEntry(fileName, lineNumber, position + 1, "Label is not at the beginning of the line.", nextLine.Substring(0, position),  nextLine, false);
                                    }
                                }
                                else if (c == '\\' && definingMacro != null)
                                {
                                    expected = TokenType.StartOrOperand;
                                }
                                else if ((!c.IsSpecialOperator() || (c.IsSpecialOperator() && !char.IsWhiteSpace(peek))) && !char.IsWhiteSpace(c))
                                {
                                    LogError(fileName, lineNumber, position + 1, "Unexpected token.", it.Index + 1, lineSource);
                                }
                                else
                                {
                                    type = TokenType.Label;
                                    expected = TokenType.Instruction;
                                }
                                break;
                            case TokenType.Instruction:
                                if (c == '=' || _options.InstructionLookup(new StringView(nextLine, position, size)))
                                {
                                    type = TokenType.Instruction;
                                    expected = TokenType.StartOrOperand;
                                    if (stopAtFirstInstruction)
                                        stopProcessing = true;
                                }
                                else if (stopAtFirstInstruction)
                                {
                                    stopProcessing = true;
                                }
                                else if (c == '\\' && definingMacro != null)
                                {
                                    expected = TokenType.StartOrOperand;
                                }
                                else
                                {
                                    LogError(fileName, lineNumber, position + 1, "Unknown instruction.", lineSource);
                                }
                                break;
                            case TokenType.StartOrOperand:
                                if (type == TokenType.Operand)
                                {
                                    if (isIdent && it.PeekNextSkipping(chr => char.IsWhiteSpace(chr)) == '(')
                                    {
                                        type = TokenType.Function;
                                        expected = TokenType.Open;
                                    }
                                    else
                                    {
                                        expected = TokenType.EndOrBinary;
                                    }
                                }
                                else if (c == '*' || c == '?')
                                {
                                    type = TokenType.Operand;
                                    expected = TokenType.EndOrBinary;
                                }
                                else if (c.IsUnaryOperator())
                                {
                                    if (size > 1)
                                    {
                                        size--;
                                        it.Rewind(it.Index - 1);
                                    }
                                    if (c.IsSpecialOperator())
                                    {
                                        int ix = it.Index;
                                        while (it.GetNext() == c) { }
                                        int count = it.Index - ix;
                                        if (!char.IsWhiteSpace(it.Current))
                                            peek = it.Current;
                                        else
                                            peek = it.PeekNextSkipping(chr => char.IsWhiteSpace(chr));
                                        type = TokenType.Operand;
                                        expected = TokenType.EndOrBinary;
                                        if (char.IsLetterOrDigit(peek) ||
                                            peek == '.' ||
                                            peek == '_' ||
                                            peek.IsOpenOperator() ||
                                            peek == '%' || peek == '$')
                                        {
                                            if (count == 1)
                                            {
                                                type = TokenType.Unary;
                                                expected = TokenType.StartOrOperand;
                                            }
                                            else
                                            {
                                                count--;
                                            }
                                        }
                                        it.Rewind(ix + count - 1);
                                        size = count;
                                    }
                                    else
                                    {
                                        if (c == '%' || c == '$')
                                        {
                                            type = TokenType.Radix;
                                            if ((c == '$' && !peek.IsHex() ||
                                                (c == '%' && !char.IsDigit(peek) && peek != '.' && peek != '#')))
                                                LogError(fileName, lineNumber, position + 1, "Unexpected operator.", it.Index + 1, lineSource);
                                        }
                                        else
                                        {
                                            type = TokenType.Unary;
                                        }
                                    }
                                }
                                else if (type != TokenType.Separator &&
                                         type != TokenType.Open &&
                                        !(c == ']' && previousWasPlus) &&
                                        !(c == ')' && previousType == TokenType.Open) &&
                                         type != TokenType.Misc)
                                {
                                    LogError(fileName, lineNumber, position + 1, "Unexpected token.", c.ToString(), lineSource);
                                }
                                break;
                            case TokenType.EndOrBinary:
                                if (type == TokenType.Closed)
                                {
                                    if (c == ']' && isWidth)
                                    {
                                        expected = TokenType.StartOrOperand;
                                        isWidth = false;
                                    }
                                }
                                else if (TokenType.MoreTokens.HasFlag(type))
                                {
                                    previousWasPlus = type == TokenType.Binary && c == '+';
                                    if (type == TokenType.Open && c != '[')
                                        LogError(fileName, lineNumber, position + 1, "Unexpected token.",  it.Index + 1, lineSource);
                                    else
                                        expected = TokenType.StartOrOperand;
                                }
                                else if (c != EOS && definingMacro == null)
                                {
                                    LogError(fileName, lineNumber, position + 1, "Unexpected token.", lineSource);
                                }
                                break;
                            case TokenType.Open:
                            default:
                                type = TokenType.Open;
                                expected = TokenType.StartOrOperand;
                                break;

                        }
                        previous = it.Current;
                        previousType = type;
                        tokens.Add(new Token(new StringView(nextLine, position, size), type, position + 1));
                    }
                }
                if (blockComment && tokens.Count == 0)
                {
                    readyForNewLine = true;
                }
                else if (LineTerminates())
                {
                    ProcessLine(false);
                    if (_lineHasErrors)
                        break;
                }
            }
            if (!stopAtFirstInstruction)
            {
                if (blockComment && !_options.IgnoreUnclosedBlockComment)
                    LogError(fileName, lineNumber, 1, "End of source reached without finding \"*/\".", string.Empty);
                else if (definingMacro != null)
                    LogError(fileName, lineNumber, 1,
                        $"End of source reached without defining macro \"{_macros.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, definingMacro)).Key}\".",
                        string.Empty);
                else if (!LineTerminates())
                    LogError(fileName, lineNumber, 1, "Unexpected end of source reached.", string.Empty);
            }
            return sourceLines;

            bool LineTerminates()
            {
                return (_options.LineTerminates != null && _options.LineTerminates(tokens)) ||
                        previousType == TokenType.None ||
                        (!TokenType.MoreTokens.HasFlag(previousType) && opens.Count == 0);
            }

            void ProcessLine(bool atColonBreak)
            {
                var line = new SourceLine(fileName, lineNumber + 1, lineSources, tokens, _index++);
                if (line.Instruction != null)
                {
                    if (_preprocessors.IsOneOf("End", line.Instruction.Name))
                    {
                        stopProcessing = true;
                        return;
                    }
                    StringComparison stringComparison = _options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    if (definingMacro != null)
                    {
                        if (_preprocessors.IsOneOf("Macros", line.Instruction.Name))
                        {
                            if (line.Instruction.Name.Equals(".macro", stringComparison))
                            {
                                LogError(fileName, lineNumber, line.Instruction.Position, "Recursive macro definition not allowed.", lineSource);
                                return;
                            }
                            if (line.Operands.Count > 0)
                            {
                                LogError(fileName, lineNumber, line.Operands[0].Position, "Unexpected expression.", lineSource);
                                return;
                            }
                            if (line.Label != null)
                                definingMacro.AddSource(GetBlockDirective(fileName, lineNumber, string.Empty, line.Label));
                            definingMacro = null;
                        }
                        else
                        {
                            definingMacro.AddSource(line);
                        }
                    }
                    else if (_preprocessors.IsReserved(line.Instruction.Name) && !stopAtFirstInstruction)
                    {
                        if (_preprocessors.IsOneOf("Macros", line.Instruction.Name))
                        {
                            if (line.Instruction.Name.Equals(".endmacro", stringComparison))
                            {
                                LogError(fileName, lineNumber, line.Instruction.Position, "Directive does not close a macro definition.", line.Instruction.Name.ToString(), lineSource);
                                return;
                            }
                            if (line.Label == null)
                            {
                                LogError(fileName, lineNumber, 1, "Macro name not specified.", 0, lineSource);
                                return;
                            }
                            var macroname = "." + line.Label.Name.ToString();
                            if (_macros.ContainsKey(macroname))
                            {
                                LogError(fileName, lineNumber, line.Label.Position, "Redefinition of macro.", macroname, lineSource);
                                return;
                            }
                            if (_options.IsMacroNameValid != null && !_options.IsMacroNameValid(macroname))
                            {
                                LogError(fileName, lineNumber, line.Label.Position, $"Macro name \"{line.Label}\" not allowed.", line.Label.Name.ToString(), lineSource);
                                return;
                            }
                            try
                            {
                                definingMacro = new Macro(line.Operands, _options.CaseSensitive, this);
                            }
                            catch (ExpressionException ex)
                            {
                                if (ex.Token != null)
                                    LogError(fileName, lineNumber, ex.Position, ex.Message, ex.Token.Name.ToString(), lineSource);
                                else
                                    LogError(fileName, lineNumber, ex.Position, ex.Message, lineSource);
                                return;
                            }
                            _macros[macroname] = definingMacro;
                        }
                        else
                        {
                            if (line.Operands.Count != 1 || !line.Operands[0].IsDoubleQuote())
                            {
                                LogError(fileName, lineNumber, line.Instruction.Position + line.Instruction.Name.Length, "Invalid filename.", lineSource.Length, lineSource);
                            }
                            else
                            {
                                var includeFile = line.Operands[0].Name.ToString().TrimOnce('"');
                                if (line.Instruction.Name[1] == 'b' || line.Instruction.Name[1] == 'B')
                                    sourceLines.Add(GetBlockDirective(includeFile, lineNumber, ".block", line.Label));
                                sourceLines.AddRange(Process(includeFile));
                                if (line.Instruction.Name[1] == 'b' || line.Instruction.Name[1] == 'B')
                                    sourceLines.Add(GetBlockDirective(includeFile, lineNumber, ".endblock", null));
                            }
                        }
                    }
                    else if (!stopAtFirstInstruction && _macros.TryGetValue(line.Instruction.Name.ToString(), out var macro))
                    {
                        sourceLines.AddRange(ExpandMacros(line, macro));
                    }
                    else if (stopAtFirstInstruction && _preprocessors.IsOneOf("Includes", line.Instruction.Name) && line.Operands.Count == 1 && line.Operands[0].IsDoubleQuote())
                    {
                        var firstLincFileName = line.Operands[0].Name.TrimOnce('"').ToString();
                        using FileStream fs = File.OpenRead(firstLincFileName);
                        using BufferedStream bs = new BufferedStream(fs);
                        using StreamReader sr = new StreamReader(bs);
                        var firstLine = ProcessFromStream(firstLincFileName, lineNumber, sr, true).FirstOrDefault(l => l.Instruction != null);
                        if (firstLine != null)
                        {
                            sourceLines = new List<SourceLine> { firstLine };
                            stopProcessing = true;
                        }
                    }
                    else
                    {
                        sourceLines.Add(line);
                    }
                }
                else if (definingMacro != null)
                {
                    definingMacro.AddSource(line);
                }
                else
                {
                    sourceLines.Add(line);
                }
                readyForNewLine = true;
                if (!atColonBreak)
                    lineNumber = linesProcessed;
            }

            void StartNewLine()
            {
                _lineHasErrors = false;
                lineSources = new List<string> { nextLine };
                tokens = new List<Token>();
                previousType = TokenType.None;
                expected = TokenType.LabelInstr;
                readyForNewLine = false;
            }
        }

        IEnumerable<SourceLine> ExpandMacros(SourceLine line, Macro macro)
        {
            var expandedSources = new List<SourceLine>
            {
                GetBlockDirective(line.Filename, line.LineNumber, ".block", line.Label)
            };
            var sources = macro.Expand(line.Operands);
            foreach(var source in sources)
            {
                if (source.Instruction != null &&
                    _macros.TryGetValue(source.Instruction.Name.ToString(), out var submacro))
                    expandedSources.AddRange(ExpandMacros(source, submacro));
                else
                    expandedSources.Add(source);
            }
            expandedSources.Add(GetBlockDirective(line.Filename, line.LineNumber, ".endblock", null)); 
            return expandedSources;
        }


        /// <summary>
        /// Process the source.
        /// </summary>
        /// <param name="fileName">The source file name.</param>
        /// <param name="lineNumber">The source line number.</param>
        /// <param name="source">The source text.</param>
        /// <returns>A list of parsed <see cref="SourceLine"/> records.</returns>
        public List<SourceLine> Process(string fileName, int lineNumber, string source)
        {
            if (_options.InstructionLookup == null)
                throw new Exception("Instruction lookup option not configured.");
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            using MemoryStream ms = new MemoryStream(sourceBytes);
            using StreamReader sr = new StreamReader(ms);
            return ProcessFromStream(fileName, lineNumber, sr);
        }

        /// <summary>
        /// Process a define expression.
        /// </summary>
        /// <param name="defineExpression">The define expression.</param>
        /// <returns>The parsed <see cref="SourceLine"/>.</returns>
        public SourceLine ProcessDefine(string defineExpression)
        {
            if (!s_defineRegex.IsMatch(defineExpression))
                throw new Exception($"Define expression \"{defineExpression}\" is not valid.");
            if (!defineExpression.Contains('='))
                defineExpression += "=1";
            try
            {
                var defines = Process(string.Empty, 1, defineExpression);
                return defines.First();
            }
            catch
            {
                throw new Exception($"Define expression \"{defineExpression}\" is not valid.");
            }
        }

        /// <summary>
        /// Process a source file.
        /// </summary>
        /// <param name="fileName">The source file name.</param>
        /// <returns>A list of parsed <see cref="SourceLine"/> records.</returns>
        public List<SourceLine> Process(string fileName)
        {
            fileName = GetFullPath(fileName, _options.IncludePath);
            IncludeFile(fileName);
            if (_options.InstructionLookup == null)
                throw new Exception("Instruction lookup option not configured.");

            using FileStream fs = File.OpenRead(fileName);
            using StreamReader sr = new StreamReader(fs);
            return ProcessFromStream(fileName, 0, sr);
        }

        /// <summary>
        /// Process a source up to the first recognized directive.
        /// </summary>
        /// <param name="fileName">The source file name.</param>
        /// <returns>A parsed <see cref="SourceLine"/> record.</returns>
        public SourceLine ProcessToFirstDirective(string fileName)
        {
            using FileStream fs = File.OpenRead(GetFullPath(fileName, _options.IncludePath));
            using StreamReader sr = new StreamReader(fs);
            var sources = ProcessFromStream(fileName, 0, sr, true);
            if (sources.Count > 0)
                return sources[^1];
            return null;
        }

        /// <summary>
        /// Gets the input filenames that were processed by the preprocessor.
        /// </summary>
        /// <returns>A collection of input files.</returns>
        public ReadOnlyCollection<string> GetInputFiles()
            => new ReadOnlyCollection<string>(_includedFiles.ToList());

        /// <summary>
        /// Get the full file path of an existing file, first from the file name itself in case the file
        /// exists in the same path as the running process, then from the include path.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="includePath">The include path.</param>
        /// <returns>The full path of the file, or the file name itself if the path is the same as that of
        /// the running process.</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static string GetFullPath(string fileName, string includePath)
        {
            string fullPath = fileName;
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                if (!string.IsNullOrEmpty(includePath))
                {
                    fullPath = Path.Combine(includePath, fileName);
                    if (!File.Exists(fullPath))
                        throw new FileNotFoundException($"\"{fileInfo.Name}\" not found.");
                }
                else
                {
                    throw new FileNotFoundException($"\"{fileInfo.Name}\" not found.");
                }
            }
            return fullPath;
        }

        #endregion
    }
}