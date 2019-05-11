//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// Handler for the DisplayBanner event.
    /// </summary>
    public delegate string DisplayBannerEventHandler(object sender, bool showVersion);

    /// <summary>
    /// Handler for the WriteBytes event.
    /// </summary>
    public delegate byte[] WriteBytesEventHandler(object sender);

    /// <summary>
    /// Represents an error that occurs when an undefined symbol is referenced.
    /// </summary>
    public class SymbolNotDefinedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.SymbolNotDefinedException"/> class.
        /// </summary>
        /// <param name="symbol">Symbol.</param>
        public SymbolNotDefinedException(string symbol)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// Gets the undefined symbol.
        /// </summary>
        /// <value>The symbol name.</value>
        public string Symbol { get; private set; }
    }

    /// <summary>
    /// Implements an assembly controller to process source input and convert 
    /// to assembled output.
    /// </summary>
    public sealed class AssemblyController : AssemblerBase, IAssemblyController
    {
        #region Members

        readonly Stack<ILineAssembler> _assemblers;
        readonly List<IBlockHandler> _blockHandlers;
        readonly List<SourceLine> _processedLines;
        SourceLine _currentLine;
        readonly SourceHandler _sourceHandler;

        int _passes;

        string _localLabelScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="T:DotNetAsm.AssemblyController"/>, which controls the 
        /// assembly process.
        /// </summary>
        /// <param name="args">The array of <see cref="T:System.String"/> args passed by the commandline.</param>
        public AssemblyController(string[] args) :
            base(args)
        {
            Reserved.DefineType("Directives",
                    ".cpu", ".endrelocate", ".equ", ".pseudopc", ".realpc", ".relocate", ".end",
                    ".endrepeat", ".proff", ".pron", ".repeat", ConstStrings.VAR_DIRECTIVE
                );

            Reserved.DefineType("Functions",
                     "abs", "acos", "asin", "atan", "cbrt", "ceil", "cos", "cosh", "count", "deg",
                     "exp", "floor", "frac", "hypot", "ln", "log10", "pow", "rad", "random",
                     "round", "sgn", "sin", "sinh", "sizeof", "sqrt", "tan", "tanh", "trunc",
                     "format"
                );

            Reserved.DefineType("UserDefined");


            _processedLines = new List<SourceLine>();
            _sourceHandler = new SourceHandler();

            Assembler.Evaluator.DefineParser(SymbolsToValues);

            _localLabelScope = string.Empty; 

            _assemblers = new Stack<ILineAssembler>();
            _assemblers.Push(new PseudoAssembler(IsInstruction,
                arg => IsReserved(arg) || Assembler.Symbols.Labels.IsScopedSymbol(arg, _currentLine.Scope)));

            _assemblers.Push(new MiscAssembler());

            _blockHandlers = new List<IBlockHandler>
            {
                _sourceHandler,
                new ConditionHandler(),
                new MacroHandler(IsInstruction),
                new ForNextHandler(),
                new RepetitionHandler(),
                new ScopeBlockHandler()
            };
            Disassembler = new Disassembler();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if a given token is actually an instruction or directive, either
        /// for the <see cref="T:DotNetAsm.AssemblyController"/> or any line assemblers.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns><c>True</c> if the token is an instruction or directive, otherwise <c>false</c>.</returns>
        public bool IsInstruction(string token) => Reserved.IsOneOf("Directives", token) ||
                                                    _blockHandlers.Any(handler => handler.Processes(token)) ||
                                                    _assemblers.Any(assembler => assembler.AssemblesInstruction(token));

        /// <summary>
        /// Determines whether the token is a reserved keyword, such as an instruction
        /// or assembler directive, or a user-defined reserved word.
        /// </summary>
        /// <param name="token">The token to test.</param>
        /// <returns>True, if the token is a reserved word, otherwise false.</returns>
        public override bool IsReserved(string token) => IsInstruction(token) ||
                                                         Reserved.IsReserved(token);

        bool IsSymbolName(string token, bool allowLeadUnderscore = true, bool allowDot = true)
        {
            // empty string 
            if (string.IsNullOrEmpty(token))
                return false;

            // is a reserved word
            if (IsReserved(token))
                return false;

            // if leading underscore not allowed
            if (!allowLeadUnderscore && token.StartsWith("_", Assembler.Options.StringComparison))
                return false;

            // if no dots allowed or trailing dot
            if (token.Contains(".") && (!allowDot || token.EndsWith(".", Assembler.Options.StringComparison)))
                return false;

            // otherwise...
            return Assembler.Symbols.Labels.IsSymbolValid(token, true);
        }

        List<ExpressionElement> SymbolsToValues(string expression)
            => Assembler.Symbols.TranslateExpressionSymbols(_currentLine, expression, _localLabelScope, _passes > 0);


        /// <remarks>
        /// Define macros and segments, and add included source files.
        /// </remarks>
        IEnumerable<SourceLine> Preprocess()
        {
            var source = new List<SourceLine>();

            source.AddRange(ProcessDefinedLabels());
            foreach (var file in Assembler.Options.InputFiles)
            {
                _sourceHandler.Process(new SourceLine
                {
                    Instruction = ".include",
                    Operand = "\"" + file + "\""
                });
                if (Assembler.Log.HasErrors)
                    break;
            }

            if (Assembler.Log.HasErrors == false)
            {
                source.AddRange(_sourceHandler.GetProcessedLines());
                _sourceHandler.Reset();
                return source;
            }
            if (!string.IsNullOrEmpty(Assembler.Options.CPU))
                OnCpuChanged(new SourceLine { SourceString = ConstStrings.COMMANDLINE_ARG, Operand = Assembler.Options.CPU });

            return null;
        }


        /// <remarks>
        /// Add labels defined with command-line -D option
        /// </remarks>
        IEnumerable<SourceLine> ProcessDefinedLabels()
        {
            var labels = new List<SourceLine>();

            foreach (var label in Assembler.Options.LabelDefines)
            {
                string name = label;
                string definition = "1";
                var eqix = label.IndexOf('=');
                if (eqix > -1)
                {
                    if (eqix == label.Length - 1)
                        throw new Exception(string.Format("Bad argument in label definition '{0}'", label));
                    name = label.Substring(0, eqix);
                    definition = label.Substring(eqix + 1);
                }

                if (IsSymbolName(name, false, false) == false)
                    throw new Exception(string.Format(ErrorStrings.LabelNotValid, name));

                labels.Add(new SourceLine
                {
                    Label = name,
                    Instruction = "=",
                    Operand = definition,
                    SourceString = string.Format($"{name}={definition} ;-D {label}", name, definition, label)
                });
            }
            return labels;
        }

        void OnCpuChanged(SourceLine line)
        {
            if (CpuChanged != null)
                CpuChanged.Invoke(new CpuChangedEventArgs { Line = line });
            else
                Assembler.Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
        }

        public void DoPasses(IEnumerable<SourceLine> source)
        {
            int id = 1;
            var sourceList = source.ToList();
            bool needPass = true;
            _passes = 0;
            while (needPass && !Assembler.Log.HasErrors)
            {
                needPass = false;
                Assembler.Output.Reset();
                Assembler.Symbols.Variables.Clear();
                for (int i = 0; i < sourceList.Count; i++)
                {
                    _currentLine = sourceList[i];
                    if (_passes == 0)
                    {
                        _currentLine.Id = id++;
                        if (_currentLine.DoNotAssemble)
                        {
                            if (_currentLine.IsComment)
                                _processedLines.Add(_currentLine);
                            continue;
                        }
                        //------------------------------------------------------
                        //
                        // Parse the source into labels, instructions, etc.
                        //
                        //------------------------------------------------------
                        if (!_currentLine.IsParsed)
                        {
                            if (string.IsNullOrWhiteSpace(_currentLine.SourceString))
                                continue;
                            StringBuilder tokenBuilder = new StringBuilder();
                            for (int j = 0; j < _currentLine.SourceString.Length; j++)
                            {
                                var c = _currentLine.SourceString[j];
                                if (string.IsNullOrEmpty(_currentLine.Instruction))
                                {
                                    string token = string.Empty;
                                    if (char.IsWhiteSpace(c) || j == _currentLine.SourceString.Length - 1 || c == '=' || c == '*' || c == ':' || c == ';')
                                    {
                                        if (!char.IsWhiteSpace(c) && c != ':' && c != ';')
                                            tokenBuilder.Append(c);
                                        token = tokenBuilder.ToString();
                                        tokenBuilder.Clear();
                                    }
                                    else
                                    {
                                        tokenBuilder.Append(c);
                                    }
                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        if (IsInstruction(token) || token == "=" || token[0] == '.')
                                        {
                                            _currentLine.Instruction = token;
                                            if (c == ':')
                                            {
                                                if (j < _currentLine.SourceString.Length - 1)
                                                    sourceList.Insert(i + 1, new SourceLine
                                                    {
                                                        SourceString = _currentLine.SourceString.Substring(j + 1)
                                                    });
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(_currentLine.Label))
                                            {
                                                Assembler.Log.LogEntry(_currentLine, ErrorStrings.UnknownInstruction, token);
                                                break;
                                            }
                                            _currentLine.Label = token;
                                        }
                                    }
                                    if (c == ';')
                                        break;
                                }
                                else
                                {
                                    if (tokenBuilder.Length > 0 || !char.IsWhiteSpace(c))
                                    {
                                        if (c == '"' || c == '\'')
                                        {
                                            // process quotes separately
                                            var quoted = _currentLine.SourceString.GetNextQuotedString(atIndex: j);
                                            tokenBuilder.Append(quoted);
                                            j += quoted.Length - 1;
                                        }
                                        else if (c == ';')
                                        {
                                            j = _currentLine.SourceString.Length - 1;
                                        }
                                        else if (c == ':')
                                        {
                                            _currentLine.Operand = tokenBuilder.ToString().TrimEnd();
                                            tokenBuilder.Clear();
                                            if (j < _currentLine.SourceString.Length - 1)
                                                sourceList.Insert(i + 1, new SourceLine
                                                {
                                                    SourceString = _currentLine.SourceString.Substring(j + 1)
                                                });
                                            break;
                                        }
                                        else
                                        {
                                            tokenBuilder.Append(c);
                                        }
                                    }
                                    if (j == _currentLine.SourceString.Length - 1)
                                    {
                                        _currentLine.Operand = tokenBuilder.ToString().TrimEnd();
                                        tokenBuilder.Clear();
                                    }
                                }
                            }
                        }
                    }
                    try
                    {
                        //------------------------------------------------------
                        //
                        // Process blocks.
                        //
                        //------------------------------------------------------
                        if (_passes == 0)
                        {
                            var currentHandler = _blockHandlers.FirstOrDefault(h => h.IsProcessing());
                            if (currentHandler == null)
                                currentHandler = _blockHandlers.FirstOrDefault(h => h.Processes(_currentLine.Instruction));
                            if (currentHandler != null)
                            {
                                sourceList.RemoveAt(i--);
                                currentHandler.Process(_currentLine);
                                if (currentHandler.IsProcessing() == false)
                                {
                                    sourceList.InsertRange(i + 1, currentHandler.GetProcessedLines());
                                    currentHandler.Reset();
                                }
                                continue;
                            }
                            if (!_currentLine.DoNotAssemble)
                                _processedLines.Add(_currentLine);
                        }
                        else if (_currentLine.DoNotAssemble || !_currentLine.IsParsed)
                        {
                            continue;
                        }
                        else if (_currentLine.Instruction.Equals(".end", Assembler.Options.StringComparison))
                        {
                            break;
                        }
                        //------------------------------------------------------
                        //
                        // Update the program counter, then check to see if it
                        // changed since the last pass.
                        //
                        //------------------------------------------------------
                        UpdatePC();
                        long value = Assembler.Output.LogicalPC;
                        if (_currentLine.Instruction.Equals(ConstStrings.VAR_DIRECTIVE, Assembler.Options.StringComparison))
                        {
                            var varparts = Assembler.Symbols.Variables.SetVariable(_currentLine.Operand, _currentLine.Scope);
                            value = Assembler.Evaluator.Eval(varparts.Value);
                        }
                        else if (!string.IsNullOrEmpty(_currentLine.Label) && !_currentLine.Label.Equals("*"))
                        {
                            if (IsAssignmentDirective())
                            {
                                if (_currentLine.Label.Equals("-") || _currentLine.Label.Equals("+"))
                                    Assembler.Log.LogEntry(_currentLine, ErrorStrings.LabelNotValid, _currentLine.Label);
                                else
                                    value = Assembler.Evaluator.Eval(_currentLine.Operand);
                            }
                            if (_passes == 0 && (_currentLine.Label.Equals("-") || _currentLine.Label.Equals("+")))
                            {
                                if (!needPass)
                                    needPass = _currentLine.Label.Equals("+");
                                Assembler.Symbols.AddAnonymousLine(_currentLine);
                            }
                            else
                            {
                                SetLabel(_currentLine.Label, value);
                            }
                        }
                        if (_passes > 0 && !needPass)
                            needPass = _currentLine.PC != value;
                        _currentLine.PC = value;

                        //------------------------------------------------------
                        //
                        // Attempt to do assembly.
                        //
                        //------------------------------------------------------
                        if (string.IsNullOrEmpty(_currentLine.Instruction))
                            continue;
                        if (!IsAssignmentDirective())
                        {
                            if (_currentLine.Instruction.Equals(".cpu", Assembler.Options.StringComparison))
                            {
                                if (!_currentLine.Operand.EnclosedInQuotes())
                                    Assembler.Log.LogEntry(_currentLine, ErrorStrings.QuoteStringNotEnclosed);
                                else
                                    OnCpuChanged(_currentLine);
                            }
                            else if (!needPass)
                            {
                                // try to assemble as far as we can before we run
                                // into an issue related to an undefined label
                                // (overflow error, not defined, etc.)
                                try
                                {
                                    AssembleLine();
                                }
                                catch
                                {
                                    Assembler.Output.AddUninitialized(GetInstructionSize());
                                    throw;
                                }
                            }
                            else
                            {
                                Assembler.Output.AddUninitialized(GetInstructionSize());
                            }
                        }
                    }
                    catch (SymbolCollectionException symExc)
                    {
                        if (symExc.Reason == SymbolCollectionException.ExceptionReason.SymbolExists)
                            Assembler.Log.LogEntry(_currentLine, ErrorStrings.LabelRedefinition, _currentLine.Label);
                        else
                            Assembler.Log.LogEntry(_currentLine, ErrorStrings.LabelNotValid, _currentLine.Label);
                    }
                    catch (SymbolNotDefinedException symNoDefExc)
                    {
                        if (_passes > 0 || _blockHandlers.Any(h => h.Processes(_currentLine.Instruction)))
                            Assembler.Log.LogEntry(_currentLine, ErrorStrings.LabelNotDefined, symNoDefExc.Symbol);
                        else
                            needPass = true;
                    }
                    catch (OverflowException overFlEx)
                    {
                        if (_passes > 0 || _blockHandlers.Any(h => h.Processes(_currentLine.Instruction)))
                            Assembler.Log.LogEntry(_currentLine, ErrorStrings.IllegalQuantity, overFlEx.Message);
                        else
                            needPass = true;
                    }
                    catch (Compilation.InvalidPCAssignmentException pcExc)
                    {
                        if (_passes > 0 || _blockHandlers.Any(h => h.Processes(_currentLine.Instruction)))
                            Assembler.Log.LogEntry(_currentLine, ErrorStrings.InvalidPCAssignment, pcExc.Message);
                        else
                            needPass = true;
                    }
                    catch (DivideByZeroException)
                    {
                        if (_passes > 0 || _blockHandlers.Any(h => h.Processes(_currentLine.Instruction)))
                            throw;
                        needPass = true;
                    }
                    catch (Exception ex)
                    {
                        Assembler.Log.LogEntry(_currentLine, ex.Message);
                    }
                }
                if (_blockHandlers.Any(h => h.IsProcessing()))
                {
                    Assembler.Log.LogEntry(_processedLines.Last(), ErrorStrings.MissingClosure);
                    break;
                }
                _passes++;
                if (_passes > 4)
                    throw new Exception("Too many passes attempted.");

            }
        }

        void SetLabel(string symbol, long value)
        {
            if (symbol != "+" && symbol != "-")
            {
                string label = string.Empty;
                if (symbol[0] == '_')
                {
                    label = _currentLine.Scope + _localLabelScope + symbol;
                }
                else
                {
                    _localLabelScope = symbol;
                    label = _currentLine.Scope + symbol;
                }
                Assembler.Symbols.Labels.SetLabel(label, value, false, _passes == 0);
            }
        }

        public void AddAssembler(ILineAssembler lineAssembler) => _assemblers.Push(lineAssembler);

        public void AddSymbol(string symbol) => Reserved.AddWord("UserDefined", symbol);

        public void AssembleLine()
        {
            if (string.IsNullOrEmpty(_currentLine.Instruction))
            {
                if (!string.IsNullOrEmpty(_currentLine.Operand))
                    Assembler.Log.LogEntry(_currentLine, ErrorStrings.None);
                return;
            }

            if (IsInstruction(_currentLine.Instruction) == false)
            {
                Assembler.Log.LogEntry(_currentLine, ErrorStrings.UnknownInstruction, _currentLine.Instruction);
            }
            else
            {
                var asm = _assemblers.FirstOrDefault(a => a.AssemblesInstruction(_currentLine.Instruction));
                asm?.AssembleLine(_currentLine);
            }
        }

        /// <remarks>
        /// This does a quick and "dirty" look at instructions. It will catch
        /// some but not all syntax errors, concerned mostly with the probable 
        /// size of the instruction. 
        /// </remarks>
        int GetInstructionSize()
        {
            try
            {
                var asm = _assemblers.FirstOrDefault(a => a.AssemblesInstruction(_currentLine.Instruction));
                var size = (asm != null) ? asm.GetInstructionSize(_currentLine) : 0;
                return size;
            }
            catch (SymbolNotDefinedException)
            {
                return 0;
            }
        }

        // Are we updating the program counter?
        void UpdatePC()
        {
            long val = 0;
            if (_currentLine.Label.Equals("*"))
            {
                if (string.IsNullOrEmpty(_currentLine.Instruction))
                {
                    // do nothing
                }
                else if (IsAssignmentDirective())
                {
                    val = Assembler.Evaluator.Eval(_currentLine.Operand, UInt16.MinValue, UInt16.MaxValue);
                    Assembler.Output.SetPC(Convert.ToUInt16(val));
                }
                else
                {
                    Assembler.Log.LogEntry(_currentLine, ErrorStrings.None);
                    return;
                }
            }
            string instruction = Assembler.Options.CaseSensitive ? _currentLine.Instruction :
                _currentLine.Instruction.ToLower();

            if (instruction.Equals(".relocate") || instruction.Equals(".pseudopc"))
            {
                if (string.IsNullOrEmpty(_currentLine.Operand))
                {
                    Assembler.Log.LogEntry(_currentLine, ErrorStrings.TooFewArguments, _currentLine.Instruction);

                }
                else
                {
                    val = Assembler.Evaluator.Eval(_currentLine.Operand, uint.MinValue, uint.MaxValue);
                    Assembler.Output.SetLogicalPC(Convert.ToUInt16(val));
                }
            }
            else if (instruction.Equals(".endrelocate") || instruction.Equals(".realpc"))
            {
                if (string.IsNullOrEmpty(_currentLine.Operand) == false)
                    Assembler.Log.LogEntry(_currentLine, ErrorStrings.TooManyArguments, _currentLine.Instruction);
                else
                    Assembler.Output.SynchPC();
            }
        }

        bool IsAssignmentDirective()
        {
            if (_currentLine.Operand.EnclosedInQuotes())
                return false; // define a constant string??

            if (_currentLine.Instruction.Equals("=") ||
                _currentLine.Instruction.Equals(".equ", Assembler.Options.StringComparison))
                return true;

            return false;
        }

        void PrintStatus(Stopwatch stopwatch)
        {
            if (Assembler.Log.HasWarnings && !Assembler.Options.NoWarnings)
            {
                Console.WriteLine();
                Assembler.Log.DumpWarnings();
            }
            if (Assembler.Log.HasErrors == false)
            {
                Console.WriteLine("\n*********************************");
                Console.WriteLine("Assembly start: ${0:X4}", Assembler.Output.ProgramStart);
                Console.WriteLine("Assembly end:   ${0:X4}", Assembler.Output.ProgramEnd);
                Console.WriteLine("Passes: {0}", _passes);
            }
            else
            {
                Assembler.Log.DumpErrors();
            }

            Console.WriteLine("Number of errors: {0}", Assembler.Log.ErrorCount);
            Console.WriteLine("Number of warnings: {0}", Assembler.Log.WarningCount);

            if (Assembler.Log.HasErrors == false)
            {
                var ts = stopwatch.Elapsed.TotalSeconds;

                Console.WriteLine("{0} bytes, {1} sec.",
                                    Assembler.Output.GetCompilation().Count,
                                    ts);
                Console.WriteLine("*********************************");
                Console.WriteLine("Assembly completed successfully.");
            }
        }

        void ToListing()
        {
            if (string.IsNullOrEmpty(Assembler.Options.ListingFile) && string.IsNullOrEmpty(Assembler.Options.LabelFile))
                return;

            string listing;

            if (!string.IsNullOrEmpty(Assembler.Options.ListingFile))
            {
                listing = GetListing();
                using (StreamWriter writer = new StreamWriter(Assembler.Options.ListingFile))
                {
                    var exec = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    var argstring = string.Join(" ", Assembler.Options.Arguments);
                    var bannerstring = DisplayingBanner != null ? DisplayingBanner.Invoke(this, false) : string.Empty;

                    writer.WriteLine(";; {0}", bannerstring.Split(new char[] { '\n', '\r' }).First());
                    writer.WriteLine(";; {0} {1}", exec, argstring);
                    writer.WriteLine(";; {0:f}\n", DateTime.Now);
                    writer.WriteLine(";; Input files:\n");

                    _sourceHandler.FileRegistry.ToList().ForEach(f => writer.WriteLine(";; {0}", f));

                    writer.WriteLine();
                    writer.Write(listing);
                }
            }
            if (!string.IsNullOrEmpty(Assembler.Options.LabelFile))
            {
                listing = GetLabelsAndVariables();
                using (StreamWriter writer = new StreamWriter(Assembler.Options.LabelFile, false))
                {
                    writer.WriteLine(";; Input files:\n");
                    _sourceHandler.FileRegistry.ToList().ForEach(f => writer.WriteLine(";; {0}", f));
                    writer.WriteLine();
                    writer.WriteLine(listing);
                }
            }
        }

        string GetSymbolListing(string symbol, long value, bool isVar)
        {
            var symbolname = Regex.Replace(symbol, @"(?<=^|\.)[0-9]+(?=\.|$)", "::");
            var maxlen = symbolname.Length > 30 ? 30 : symbolname.Length;
            if (maxlen < 0) maxlen++;
            symbolname = symbolname.Substring(symbolname.Length - maxlen, maxlen);
            var size = value.Size() * 2;
            var assignsym = isVar ? ":=" : " =";

            return string.Format("{0,-30} {1} ${2,-4:x" + size.ToString() + "} : ({2}){3}",
                                symbolname,
                                assignsym,
                                value,
                                Environment.NewLine);
        }

        /// <remarks>
        /// Used by the ToListing method to get a listing of all defined labels.
        /// </remarks>
        string GetLabelsAndVariables()
        {
            var listing = new StringBuilder();

            foreach (var label in Assembler.Symbols.Labels)
                listing.Append(GetSymbolListing(label.Key, label.Value, false));

            foreach (var variable in Assembler.Symbols.Variables)
                listing.Append(GetSymbolListing(variable.Key, variable.Value, true));

            return listing.ToString();
        }

        /// <remarks>
        /// Used by the ToListing method to get the full listing.
        /// </remarks>
        string GetListing()
        {
            var listing = new StringBuilder();

            _processedLines.ForEach(l => Disassembler.DisassembleLine(l, listing));

            if (listing.ToString().EndsWith(Environment.NewLine, Assembler.Options.StringComparison))
                return listing.ToString().Substring(0, listing.Length - Environment.NewLine.Length);

            return listing.ToString();
        }

        void SaveOutput()
        {
            if (!Assembler.Options.GenerateOutput)
                return;

            var outputfile = Assembler.Options.OutputFile;
            if (string.IsNullOrEmpty(Assembler.Options.OutputFile))
                outputfile = "a.out";

            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputfile, FileMode.Create, FileAccess.Write)))
            {
                if (WritingHeader != null)
                    writer.Write(WritingHeader.Invoke(this));

                writer.Write(Assembler.Output.GetCompilation().ToArray());

                if (WritingFooter != null)
                    writer.Write(WritingFooter.Invoke(this));
            }
        }

        public void Assemble()
        {
            if (Assembler.Options.PrintVersion && DisplayingBanner != null)
            {
                Console.WriteLine(DisplayingBanner.Invoke(this, true));
                if (Assembler.Options.ArgsPassed > 1)
                    Console.WriteLine("Additional options ignored.");
                return;
            }

            if (Assembler.Options.InputFiles.Count == 0)
                return;

            if (Assembler.Options.Quiet)
                Console.SetOut(TextWriter.Null);

            if (DisplayingBanner != null)
                Console.WriteLine(DisplayingBanner.Invoke(this, false));

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var source = Preprocess();
            if (Assembler.Log.HasErrors == false)
            {
                DoPasses(source);
              
                if (Assembler.Log.HasErrors == false)
                {
                    SaveOutput();
                    ToListing();
                }
            }
            stopwatch.Stop();

            PrintStatus(stopwatch);
        }

        #endregion

        #region Properties

        public ILineDisassembler Disassembler { get; set; }

        #endregion

        #region Events

        public event CpuChangeEventHandler CpuChanged;

        public event DisplayBannerEventHandler DisplayingBanner;

        public event WriteBytesEventHandler WritingHeader;

        public event WriteBytesEventHandler WritingFooter;

        #endregion
    }
}