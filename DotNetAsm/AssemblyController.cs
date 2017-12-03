//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    public delegate string DisplayBannerEventHandler(object sender, bool isVerbose);

    public delegate byte[] WriteBytesEventHandler(object sender);

    /// <summary>
    /// Implements an assembly controller to process source input and convert 
    /// to assembled output.
    /// </summary>
    public class AssemblyController : AssemblerBase, IAssemblyController
    {
        #region Members

        Preprocessor _preprocessor;
        Stack<ILineAssembler> _assemblers;
        List<IBlockHandler> _blockHandlers;
        List<SourceLine> _processedLines;
        SourceLine _currentLine;

        LabelCollection _labelCollection;

        int _passes;

        Regex _specialLabels;

        HashSet<int> _anonPlus, _anonMinus;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a DotNetAsm.AssemblyController, which controls the 
        /// assembly process.
        /// </summary>
        /// <param name="args">The array of System.String args passed by the commandline.</param>
        public AssemblyController(string[] args)
        {
            Reserved.DefineType("Directives",
                    ".cpu", ".endrelocate", ".equ", ".pseudopc", ".realpc", ".relocate", ".end",
                    ".endrepeat", ".proff", ".pron", ".repeat", ConstStrings.VAR_DIRECTIVE
                );

            Reserved.DefineType("Functions",
                     "abs", "acos", "asin", "atan", "cbrt", "ceil", "cos", "cosh", "count", "deg",
                     "exp", "floor", "frac", "hypot", "ln", "log10", "pow", "rad", "random",
                     "round", "sgn", "sin", "sinh", "sizeof", "sqrt", "tan", "tanh", "trunc"
                );

            Reserved.DefineType("UserDefined");

            Log = new ErrorLog();

            _anonPlus = new HashSet<int>();
            _anonMinus = new HashSet<int>();
            _processedLines = new List<SourceLine>();

            Options = new AsmCommandLineOptions();
            Options.ProcessArgs(args);

            Controller = this;

            Reserved.Comparer = Options.StringComparison;
            Output = new Compilation(!Options.BigEndian);

            _specialLabels = new Regex(@"^\*|\+|-$", RegexOptions.Compiled);

            Encoding = new AsmEncoding(Options.CaseSensitive);

            Evaluator = new Evaluator(@"\$([a-fA-F0-9]+)");
            Evaluator.DefineSymbolLookup(@"(?<=\B)'(.)'(?=\B)", GetCharValue);
            if (!Options.CaseSensitive)
                Evaluator.DefineSymbolLookup(Patterns.SymbolBasic + @"\(", (fnc) => fnc.ToLower());
            Evaluator.DefineSymbolLookup(@"(?>" + Patterns.SymbolUnicodeDot + @")(?!\()", GetNamedSymbolValue);
            Evaluator.DefineSymbolLookup(@"^\++$|^-+$|\(\++\)|\(-+\)", ConvertAnonymous);
            Evaluator.DefineSymbolLookup(@"(?<![\p{Ll}\p{Lu}\p{Lt}0-9_.)])\*(?![\p{Ll}\p{Lu}\p{Lt}0-9_.(])", (str) => Output.LogicalPC.ToString());

            _labelCollection = new LabelCollection(Options.StringComparar);
            Variables = new VariableCollection(Options.StringComparar, Evaluator);

            _labelCollection.AddCrossCheck(Variables);
            Variables.AddCrossCheck(_labelCollection);

            _preprocessor = new Preprocessor(this, s => IsSymbolName(s.TrimEnd(':'), true, false));
            _assemblers = new Stack<ILineAssembler>();
            _assemblers.Push(new PseudoAssembler(this, arg =>
                {
                    return IsReserved(arg) ||
                    _labelCollection.IsScopedSymbol(arg, _currentLine.Scope);
                }));

            _assemblers.Push(new MiscAssembler(this));

            _blockHandlers = new List<IBlockHandler>
            {
                new ConditionHandler(this),
                new MacroHandler(this, IsInstruction),
                new ForNextHandler(this),
                new RepetitionHandler(this),
                new ScopeBlockHandler(this)
            };
            Disassembler = new Disassembler(this);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the encoded value of the character and return it as a string.
        /// </summary>
        /// <param name="chr">The character to encode.</param>
        /// <returns>The encoded value as a string.</returns>
        string GetCharValue(string chr) => Encoding.GetEncodedValue(chr.Trim('\'').First()).ToString();

        /// <summary>
        /// Used by the expression evaluator to convert an anonymous symbol
        /// to an address.
        /// </summary>
        /// <param name="symbol">The anonymous symbol.</param>
        /// <returns>The actual address the anonymous symbol will resolve to.</returns>
        string ConvertAnonymous(string symbol)
        {
            string trimmed = symbol.Trim(new char[] { '(', ')' });
            long addr = GetAnonymousAddress(_currentLine, trimmed);
            if (addr < 0 && _passes > 0)
            {
                Log.LogEntry(_currentLine, ErrorStrings.CannotResolveAnonymousLabel);
                return "0";
            }
            return addr.ToString();
        }

        /// <summary>
        /// Checks if a given token is actually an instruction or directive, either
        /// for the DotNetAsm.AssemblyController or any line assemblers.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns>True, if the token is an instruction or directive</returns>
        public bool IsInstruction(string token) => Reserved.IsOneOf("Directives", token) ||
                                                    _preprocessor.IsReserved(token) ||
                                                    _blockHandlers.Any(handler => handler.Processes(token)) ||
                                                    _assemblers.Any(assembler => assembler.AssemblesInstruction(token));

        /// <summary>
        /// Determines whether the token is a reserved keyword, such as an instruction
        /// or assembler directive, or a user-defined reserved word.
        /// </summary>
        /// <param name="token">The token to test.</param>
        /// <returns>True, if the token is a reserved word, otherwise false.</returns>
        public override bool IsReserved(string token) => IsInstruction(token) || Reserved.IsReserved(token);

        /// <summary>
        /// Checks whether the token is a valid symbol/label name.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <param name="allowLeadUnderscore">Allow the token to have a leading underscore
        /// for it to be a symbol.</param>
        /// <param name="allowDot">Allow the token to have separating dots for it to be
        /// considered a symbol.</param>
        /// <returns></returns>
        bool IsSymbolName(string token, bool allowLeadUnderscore = true, bool allowDot = true)
        {
            // empty string 
            if (string.IsNullOrEmpty(token))
                return false;

            // is a reserved word
            if (IsReserved(token))
                return false;

            // if leading underscore not allowed
            if (!allowLeadUnderscore && token.StartsWith("_", Options.StringComparison))
                return false;

            // if no dots allowed or trailing dot
            if (token.Contains(".") && (!allowDot || token.EndsWith(".", Options.StringComparison)))
                return false;

            // otherwise...
            return _labelCollection.IsSymbolValid(token, true);
        }

        /// <summary>
        /// Preprocess the source file into a System.IEnumerable&lt;DotNetAsm.SourceLine&gt;.
        /// Define macros and segments, and add included source files.
        /// </summary>
        /// <returns>The preprocessed System.IEnumerable&lt;DotNetAsm.SourceLine&gt;</returns>
        IEnumerable<SourceLine> Preprocess()
        {
            List<SourceLine> source = new List<SourceLine>();

            source.AddRange(ProcessDefinedLabels());
            foreach (var file in Options.InputFiles)
            {
                source.AddRange(_preprocessor.ConvertToSource(file));

                if (Log.HasErrors)
                    break;
            }

            if (Log.HasErrors == false)
            {
                source.ForEach(line =>
                    line.Operand = Regex.Replace(line.Operand, @"\s?\*\s?", "*"));

                return source;
            }
            if (!string.IsNullOrEmpty(Options.CPU))
                OnCpuChanged(new SourceLine{ SourceString = ConstStrings.COMMANDLINE_ARG, Operand = Options.CPU });
            
            return null;
        }


        /// <summary>
        /// Add labels defined with command-line -D option
        /// </summary>
        /// <returns>Returns a System.Collections.Generic.IEnumerable&lt;SourceLine&gt; 
        /// that will define the labels at assembly time.</returns>
        IEnumerable<SourceLine> ProcessDefinedLabels()
        {
            var labels = new List<SourceLine>();

            foreach (var label in Options.LabelDefines)
            {
                string name = label;
                string definition = "1";

                if (label.Contains("="))
                {
                    var def = label.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                    if (def.Count() != 2)
                        throw new Exception("Bad argument in label definition '" + label + "'");

                    name = def.First(); definition = def.Last();
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
                Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
        }

        /// <summary>
        /// Performs a first (or more) pass of preprocessed source to resolve all 
        /// actual symbol values, process conditions and repetitions, and add to 
        /// Processed Lines.
        /// </summary>
        /// <param name="source">The preprocessed System.IEnumerable&lt;DotNetAsm&gt;</param>
        void FirstPass(IEnumerable<SourceLine> source)
        {
            _passes = 0;
            int id = 0;

            List<SourceLine> sourceList = source.ToList();

            for (int i = 0; i < sourceList.Count; i++)
            {
                _currentLine = sourceList[i];
                try
                {
                    if (_currentLine.DoNotAssemble)
                    {
                        if (_currentLine.IsComment)
                            _processedLines.Add(_currentLine);
                        continue;
                    }
                    if (_currentLine.Instruction.Equals(".end", Options.StringComparison))
                        break;

                    var currentHandler = _blockHandlers.FirstOrDefault(h => h.IsProcessing());
                    if (currentHandler == null)
                        currentHandler = _blockHandlers.FirstOrDefault(h => h.Processes(_currentLine.Instruction));
                    if (currentHandler != null)
                    {
                        sourceList.RemoveAt(i--);
                        try
                        {
                            currentHandler.Process(_currentLine);
                        }
                        catch (ForNextException forNextExc)
                        {
                            Log.LogEntry(_currentLine, forNextExc.Message);
                        }
                        if (currentHandler.IsProcessing() == false)
                        {
                            sourceList.InsertRange(i + 1, currentHandler.GetProcessedLines());
                            currentHandler.Reset();
                        }
                    }
                    else
                    {
                        _currentLine.Id = id++;
                        FirstPassLine();
                    }
                }
                catch (SymbolCollectionException symExc)
                {
                    if (symExc.Reason == SymbolCollectionException.ExceptionReason.SymbolExists)
                        Log.LogEntry(_currentLine, ErrorStrings.LabelRedefinition, _currentLine.Label);
                    else
                        Log.LogEntry(_currentLine, ErrorStrings.LabelNotValid, _currentLine.Label);
                }
                catch (ExpressionException exprEx)
                {
                    Log.LogEntry(_currentLine, ErrorStrings.BadExpression, exprEx.Message);
                }
                catch (Exception)
                {
                    Log.LogEntry(_currentLine, ErrorStrings.None);
                }
            }

            if (_blockHandlers.Any(h => h.IsProcessing()))
                Log.LogEntry(_processedLines.Last(), ErrorStrings.MissingClosure);
        }

        /// <summary>
        /// Performs a first pass on the DotNetAsm.SourceLine, including updating 
        /// the Program Counter and definining labels.
        /// </summary>
        void FirstPassLine()
        {
            try
            {
                if (_currentLine.Instruction.Equals(".cpu", Options.StringComparison))
                {
                    if (!_currentLine.Operand.EnclosedInQuotes())
                        Controller.Log.LogEntry(_currentLine, ErrorStrings.QuoteStringNotEnclosed);
                    else
                        OnCpuChanged(_currentLine);
                    return;
                }
                else if (_currentLine.Instruction.Equals(ConstStrings.VAR_DIRECTIVE, Options.StringComparison))
                {
                    _currentLine.PC = Variables.SetVariable(_currentLine.Operand, _currentLine.Scope).Value;
                }
            
                UpdatePC();

                _currentLine.PC = Output.LogicalPC;

                DefineLabel();

                if (!IsAssignmentDirective())
                    Output.AddUninitialized(GetInstructionSize());
            }
            catch (Exception ex)
            {
                // most expressions resulting from calculations we don't care
                // about until final pass, since they are subject to correction
                if (ex is DivideByZeroException ||
                    ex is Compilation.InvalidPCAssignmentException ||
                    ex is OverflowException)
                { } // do nothing
                else
                    throw;
            }
            finally
            {
                // always add
                _processedLines.Add(_currentLine);
            }

        }

        /// <summary>
        /// Perform a second or final pass on a DotNetAsm.SourceLine, including final 
        /// assembly of bytes.
        /// </summary>
        /// <param name="finalPass">A flag indicating this is a final pass</param>
        /// <returns>True, if another pass is needed. Otherwise false.</returns>
        bool SecondPassLine(bool finalPass)
        {
            UpdatePC();
            bool passNeeded = false;
            if (IsAssignmentDirective())
            {
                if (_currentLine.Label.Equals("*")) return false;
                long val = long.MinValue;

                // for .vars initialization is optional
                if (string.IsNullOrEmpty(_currentLine.Operand) == false)
                    val = Evaluator.Eval(_currentLine.Operand, int.MinValue, uint.MaxValue);

                if (_currentLine.Label.Equals("-") || _currentLine.Label.Equals("+"))
                {
                    passNeeded = val != _currentLine.PC;
                }
                else
                {
                    if (val.Equals(long.MinValue))
                    {
                        Controller.Log.LogEntry(_currentLine, ErrorStrings.TooFewArguments, _currentLine.Instruction);
                        return false;
                    }
                    passNeeded = !(val.Equals(_labelCollection.GetScopedSymbolValue(_currentLine.Label, _currentLine.Scope)));
                    _labelCollection.SetLabel(_currentLine.Scope + _currentLine.Label, val, false, false);

                }
                _currentLine.PC = val;
            }
            else if (_currentLine.Instruction.Equals(ConstStrings.VAR_DIRECTIVE, Options.StringComparison))
            {
                var varparts = Variables.SetVariable(_currentLine.Operand, _currentLine.Scope);
                passNeeded = _currentLine.PC != varparts.Value;
                _currentLine.PC = varparts.Value;
            }
            else if (_currentLine.Instruction.Equals(".cpu", Options.StringComparison))
            {
                OnCpuChanged(_currentLine);
            }
            else
            {
                if (_labelCollection.IsScopedSymbol(_currentLine.Label, _currentLine.Scope))
                    _labelCollection.SetLabel(_currentLine.Scope + _currentLine.Label, Output.LogicalPC, false, false);
                passNeeded = _currentLine.PC != Output.LogicalPC;
                _currentLine.PC = Output.LogicalPC;
                if (finalPass)
                    AssembleLine();
                else
                    Output.AddUninitialized(GetInstructionSize());
            }
            return passNeeded;
        }

        /// <summary>
        /// Perform a second pass on the processed source, including output to binary.
        /// </summary>
        void SecondPass()
        {
            const int MAX_PASSES = 4;
            bool passNeeded = true;
            bool finalPass = false;
            _passes++;

            var assembleLines = _processedLines.Where(l => l.DoNotAssemble == false);

            while (_passes <= MAX_PASSES && Log.HasErrors == false)
            {
                passNeeded = false;
                Output.Reset();

                Variables.Clear();

                foreach (SourceLine line in assembleLines)
                {
                    try
                    {
                        if (line.Instruction.Equals(".end", Options.StringComparison))
                            break;

                        _currentLine = line;

                        bool needpass = SecondPassLine(finalPass);
                        if (!passNeeded)
                            passNeeded = needpass;
                    }
                    catch (ExpressionException exprEx)
                    {
                        Log.LogEntry(line, ErrorStrings.BadExpression, exprEx.Message);
                    }
                    catch (OverflowException overflowEx)
                    {
                        if (finalPass)
                            Log.LogEntry(line, ErrorStrings.IllegalQuantity, overflowEx.Message);
                    }
                    catch (Compilation.InvalidPCAssignmentException ex)
                    {
                        if (finalPass)
                            Log.LogEntry(line, ErrorStrings.InvalidPCAssignment, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Log.LogEntry(line, ex.Message);
                    }
                }
                if (finalPass)
                    break;
                _passes++;
                finalPass = !passNeeded;
            }
            if (_passes > MAX_PASSES)
                throw new Exception("Too many passes attempted.");
        }

        public void AddAssembler(ILineAssembler lineAssembler) => _assemblers.Push(lineAssembler);

        public void AddSymbol(string symbol) => Reserved.AddWord("UserDefined", symbol);

        public void AssembleLine()
        {
            if (string.IsNullOrEmpty(_currentLine.Instruction))
            {
                if (!string.IsNullOrEmpty(_currentLine.Operand))
                    Log.LogEntry(_currentLine, ErrorStrings.None);
                return;
            }

            if (IsInstruction(_currentLine.Instruction) == false)
            {
                Log.LogEntry(_currentLine, ErrorStrings.UnknownInstruction, _currentLine.Instruction);
            }
            else
            {
                var asm = _assemblers.FirstOrDefault(a => a.AssemblesInstruction(_currentLine.Instruction));
                asm?.AssembleLine(_currentLine);
            }
        }

        /// <summary>
        /// This does a quick and "dirty" look at instructions. It will catch
        /// some but not all syntax errors, concerned mostly with the probable 
        /// size of the instruction. 
        /// </summary>
        /// <returns>The size in bytes of the instruction, including opcode and operand</returns>
        int GetInstructionSize()
        {
            var asm = _assemblers.FirstOrDefault(a => a.AssemblesInstruction(_currentLine.Instruction));
            var size = (asm != null) ? asm.GetInstructionSize(_currentLine) : 0;
            return size;
        }

        /// <summary>
        /// Examine a DotNetAsm.SourceLine and determine if a label is being defined.
        /// </summary>
        void DefineLabel()
        {
            if (string.IsNullOrEmpty(_currentLine.Label) == false)
            {
                if (_currentLine.Label.Equals("*"))
                    return;

                if (_specialLabels.IsMatch(_currentLine.Label))
                {
                    if (IsAssignmentDirective())
                        _currentLine.PC = Convert.ToInt32(Evaluator.Eval(_currentLine.Operand));
                    else
                        _currentLine.PC = Output.LogicalPC;

                    if (_currentLine.Label.Equals("+") || _currentLine.Label.Equals("-"))
                    {
                        if (_currentLine.Label == "+")
                            _anonPlus.Add(_currentLine.Id);
                        else
                            _anonMinus.Add(_currentLine.Id);
                    }
                }
                else
                {
                    string scopedLabel = string.Empty;

                    _currentLine.Label = _currentLine.Label.TrimEnd(':');

                    if (Reserved.IsReserved(_currentLine.Label) ||
                        IsInstruction(_currentLine.Label) ||
                        _currentLine.Label.Contains("."))
                    {
                        Log.LogEntry(_currentLine, ErrorStrings.LabelNotValid, _currentLine.Label);
                        return;
                    }

                    scopedLabel = _currentLine.Scope + _currentLine.Label;

                    long val;
                    if (IsAssignmentDirective())
                        val = Evaluator.Eval(_currentLine.Operand, int.MinValue, uint.MaxValue);
                    else
                        val = _currentLine.PC;
                    _labelCollection.SetLabel(scopedLabel, val, false, true);

                }
            }
        }

        /// <summary>
        /// Determine if the DotNetAsm.SourceLine updates the output's Program Counter
        /// </summary>
        void UpdatePC()
        {
            long val = 0;
            if (_currentLine.Label.Equals("*"))
            {
                if (IsAssignmentDirective())
                {
                    val = Evaluator.Eval(_currentLine.Operand, UInt16.MinValue, UInt16.MaxValue);
                    Output.SetPC(Convert.ToUInt16(val));
                }
                else
                {
                    Log.LogEntry(_currentLine, ErrorStrings.None);
                }
                return;
            }
            string instruction = Options.CaseSensitive ? _currentLine.Instruction :
                _currentLine.Instruction.ToLower();

            switch (instruction)
            {
                case ".relocate":
                case ".pseudopc":
                    {
                        if (string.IsNullOrEmpty(_currentLine.Operand))
                        {
                            Log.LogEntry(_currentLine, ErrorStrings.TooFewArguments, _currentLine.Instruction);
                            return;
                        }
                        val = Evaluator.Eval(_currentLine.Operand, uint.MinValue, uint.MaxValue);
                        Output.SetLogicalPC(Convert.ToUInt16(val));
                    }
                    break;
                case ".endrelocate":
                case ".realpc":
                    {
                        if (string.IsNullOrEmpty(_currentLine.Operand) == false)
                        {
                            Log.LogEntry(_currentLine, ErrorStrings.TooManyArguments, _currentLine.Instruction);
                            return;
                        }
                        Output.SynchPC();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Determines whether the SourceLine is defining a constant.
        /// </summary>
        /// <returns>True, if the line is defining a constant, otherwise false.</returns>
        bool IsAssignmentDirective()
        {
            if (_currentLine.Operand.EnclosedInQuotes())
                return false; // define a constant string??

            if (_currentLine.Instruction.Equals("=") ||
                _currentLine.Instruction.Equals(".equ", Options.StringComparison))
                return true;

            return false;
        }

        /// <summary>
        /// Print the status of the assembly results to console output.
        /// </summary>
        void PrintStatus(DateTime asmTime)
        {
            if (Log.HasWarnings && !Options.NoWarnings)
            {
                Console.WriteLine();
                Log.DumpWarnings();
            }
            if (Log.HasErrors == false)
            {
                Console.WriteLine("\n********************************");
                Console.WriteLine("Assembly start: ${0:X4}", Output.ProgramStart);
                Console.WriteLine("Assembly end:   ${0:X4}", Output.ProgramEnd);
                Console.WriteLine("Passes: {0}", _passes);
            }
            else
            {
                Log.DumpErrors();
            }

            Console.WriteLine("Number of errors: {0}", Log.ErrorCount);
            Console.WriteLine("Number of warnings: {0}", Log.WarningCount);

            if (Log.HasErrors == false)
            {
                TimeSpan ts = DateTime.Now.Subtract(asmTime);

                Console.WriteLine("{0} bytes, {1} sec.",
                                    Output.GetCompilation().Count,
                                    ts.TotalSeconds);
                Console.WriteLine("*********************************");
                Console.WriteLine("Assembly completed successfully.");
            }
        }

        /// <summary>
        /// Sends the assembled source to listing, either as a list of labels or 
        /// a full assembly listing, including assembled bytes and disassembly.
        /// </summary>
        void ToListing()
        {
            if (string.IsNullOrEmpty(Options.ListingFile) && string.IsNullOrEmpty(Options.LabelFile))
                return;

            string listing;

            if (!string.IsNullOrEmpty(Options.ListingFile))
            {
                listing = GetListing();
                using (StreamWriter writer = new StreamWriter(Options.ListingFile))
                {
                    string exec = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    string argstring = string.Join(" ", Options.Arguments);
                    string bannerstring = DisplayingBanner != null ? DisplayingBanner.Invoke(this, false) : string.Empty;

                    writer.WriteLine(";; {0}", bannerstring.Split(new char[] { '\n', '\r' }).First());
                    writer.WriteLine(";; {0} {1}", exec, argstring);
                    writer.WriteLine(";; {0:f}\n", DateTime.Now);
                    writer.WriteLine(";; Input files:\n");

                    _preprocessor.FileRegistry.ToList().ForEach(f => writer.WriteLine(";; {0}", f));

                    writer.WriteLine();
                    writer.Write(listing);
                }
            }
            if (!string.IsNullOrEmpty(Options.LabelFile))
            {
                listing = GetLabelsAndVariables();
                using (StreamWriter writer = new StreamWriter(Options.LabelFile, false))
                {
                    writer.WriteLine(";; Input files:\n");
                    _preprocessor.FileRegistry.ToList().ForEach(f => writer.WriteLine(";; {0}", f));
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

        /// <summary>
        /// Used by the ToListing method to get a listing of all defined labels.
        /// </summary>
        /// <returns>A string containing all label definitions.</returns>
        string GetLabelsAndVariables()
        {
            StringBuilder listing = new StringBuilder();

            foreach (var label in _labelCollection)
                listing.Append(GetSymbolListing(label.Key, label.Value, false));
          
            foreach (var variable in Variables)
                listing.Append(GetSymbolListing(variable.Key, variable.Value, true));
            
            return listing.ToString();
        }

        /// <summary>
        /// Used by the ToListing method to get the full listing.</summary>
        /// <returns>Returns a listing string to save to disk.</returns>
        string GetListing()
        {
            StringBuilder listing = new StringBuilder();

            _processedLines.ForEach(l => Disassembler.DisassembleLine(l, listing));

            if (listing.ToString().EndsWith(Environment.NewLine, Options.StringComparison))
                return listing.ToString().Substring(0, listing.Length - Environment.NewLine.Length);

            return listing.ToString();
        }

        /// <summary>
        /// Saves the output to disk.
        /// </summary>
        void SaveOutput()
        {
            if (!Options.GenerateOutput)
                return;

            var outputfile = Options.OutputFile;
            if (string.IsNullOrEmpty(Options.OutputFile))
                outputfile = "a.out";

            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputfile, FileMode.Create, FileAccess.Write)))
            {
                if (WritingHeader != null)
                    writer.Write(WritingHeader.Invoke(this));

                writer.Write(Output.GetCompilation().ToArray());

                if (WritingFooter != null)
                    writer.Write(WritingFooter.Invoke(this));
            }
        }

        public void Assemble()
        {
            if (Options.InputFiles.Count == 0)
                return;

            if (Options.PrintVersion && DisplayingBanner != null)
                Console.WriteLine(DisplayingBanner.Invoke(this, true));

            if (Options.Quiet)
                Console.SetOut(TextWriter.Null);

            if (!Options.PrintVersion && DisplayingBanner != null)
                Console.WriteLine(DisplayingBanner.Invoke(this, false));
            
            DateTime asmTime = DateTime.Now;

            var source = Preprocess();

            if (Log.HasErrors == false)
            {
                FirstPass(source);

                if (Log.HasErrors == false)
                {
                    SecondPass();

                    if (Log.HasErrors == false)
                    {
                        SaveOutput();

                        ToListing();
                    }
                }
            }
            PrintStatus(asmTime);
        }

        /// <summary>
        /// Used by the expression evaluator to get the actual value of the symbol.
        /// </summary>
        /// <param name="symbol">The symbol to look up.</param>
        /// <returns>The underlying value of the symbol.</returns>
        string GetNamedSymbolValue(string symbol)
        {
            if (Variables.IsScopedSymbol(symbol, _currentLine.Scope))
                return Variables.GetScopedSymbolValue(symbol, _currentLine.Scope).ToString();

            long value = _labelCollection.GetScopedSymbolValue(symbol, _currentLine.Scope);
            if (value.Equals(long.MinValue))
            {
                if (_passes > 0)
                    Log.LogEntry(_currentLine, ErrorStrings.LabelNotDefined, symbol);
                return "0";
            }
            return value.ToString();

        }

        /// <summary>
        /// Gets the actual address of an anonymous symbol.
        /// </summary>
        /// <param name="fromLine">The SourceLine containing the anonymous symbol.</param>
        /// <param name="operand">The operand.</param>
        /// <returns>Returns the anonymous symbol address.</returns>
        long GetAnonymousAddress(SourceLine fromLine, string operand)
        {
            int count = operand.Length - 1;
            IOrderedEnumerable<int> idList;
            if (operand.First() == '-')
            {
                idList = _anonMinus.Where(i => i < fromLine.Id).OrderByDescending(i => i);
            }
            else
            {
                idList = _anonPlus.Where(i => i > fromLine.Id).OrderBy(i => i);
            }
            long id = 0;
            string scope = fromLine.Scope;

            while (id != -1)
            {
                id = idList.Count() > count ? idList.ElementAt(count) : -1;

                var lines = from line in _processedLines
                            where line.Id == id && line.Scope == scope
                            select line;
                if (!lines.Any())
                {
                    if (string.IsNullOrEmpty(scope) == false)
                    {
                        var splitscope = scope.Split('.').ToList();
                        splitscope.RemoveAt(splitscope.Count - 1);
                        scope = string.Join(".", splitscope);
                    }
                    else
                    {
                        scope = fromLine.Scope;
                        count++;
                    }
                }
                else
                {
                    return lines.First().PC;
                }
            }
            return id;
        }
        #endregion

        #region Properties

        public AsmCommandLineOptions Options { get; private set; }

        public Compilation Output { get; private set; }

        public AsmEncoding Encoding { get; private set; }

        public ErrorLog Log { get; private set; }

        public SymbolCollectionBase Labels { get { return _labelCollection; } }

        public VariableCollection Variables { get; private set; }

        public IEvaluator Evaluator { get; private set; }

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