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
    public class AssemblyController : AssemblerBase, IAssemblyController
    {
        #region Constants

        /// <summary>
        /// The default token indicating a scope block opening. This field is constant.
        /// </summary>
        internal const string OPEN_SCOPE = ".block";

        /// <summary>
        /// The default token indicating a scope block closure. This field is constant.
        /// </summary>
        internal const string CLOSE_SCOPE = ".endblock";

        #endregion

        private List<ILineAssembler> _assemblers;
        private ConditionAssembler _conditionAssembler;
        private ExpressionEvaluator _evaluator;

        private SourceLine _currentLine;
        
        private int _passes;

        private Regex _specialLabels;

        /// <summary>
        /// Constructs an instance of a DotNetAsm.AssemblyController, which controls the 
        /// assembly process.
        /// </summary>
        /// <param name="args">The array of System.String args passed by the commandline.</param>
        public AssemblyController(string[] args) 
            : base()
        {
            Reserved.DefineType("Directives", new string[]
                {
                    ".endrelocate", ".equ", ".pseudopc", ".realpc", ".relocate", ".end",
                    ".proff", ".pron", ".repeat", ".endrepeat"
                });

            Reserved.DefineType("Functions", new string[]
                {
                     "abs", "acos", "asin", "atan", "cbrt", "ceil", "cos", "cosh", "deg", 
                     "exp", "floor", "frac", "hypot", "ln", "log10", "pow", "rad", "random", 
                     "round", "sgn", "sin", "sinh", "sqrt", "tan", "tanh", "trunc"
                });

            Reserved.DefineType("Blocks", new string[]
                {
                    OPEN_SCOPE, CLOSE_SCOPE
                });

            Log = new ErrorLog();
            Options = new AsmCommandLineOptions();
            FileRegistry = new HashSet<string>();
            AnonPlus = new HashSet<int>();
            AnonMinus = new HashSet<int>();
            ProcessedLines = new List<SourceLine>();

            Disassembler = new Disassembler(this);

            Options.ProcessArgs(args);

            Labels = new Dictionary<string, string>(Options.StringComparar);
            Reserved.Comparer = Options.StringComparison;
            Output = new Compilation(!Options.BigEndian);

            _specialLabels = new Regex(@"^\*|\+|-$", RegexOptions.Compiled);

            _assemblers = new List<ILineAssembler>();
            _evaluator = new ExpressionEvaluator(!Options.CaseSensitive);

            _evaluator.SymbolLookups.Add(@"(?>_?[a-zA-Z][a-zA-Z0-9_.]*)(?!\()", GetLabelValue);
            _evaluator.SymbolLookups.Add(@"^\++$|^-+$|\(\++\)|\(-+\)", ConvertAnonymous);
            _evaluator.SymbolLookups.Add(@"(?<![a-zA-Z0-9_.)])\*(?![a-zA-Z0-9_.(])", (str) => Output.GetPC().ToString());

            _evaluator.AllowAlternateBinString = true;

            _conditionAssembler = new ConditionAssembler(this);

            _assemblers.Add(new PseudoAssembler(this));
            _assemblers.Add(new MiscAssembler(this));
        }

        /// <summary>
        /// Used by the expression evaluator to convert an anonymous symbol
        /// to an address.
        /// </summary>
        /// <param name="symbol">The anonymous symbol.</param>
        /// <param name="notused">The match group (not used)</param>
        /// <param name="obj">A helper object, in this case a SourceLine.</param>
        /// <returns>The actual address the anonymous symbol will resolve to.</returns>
        private string ConvertAnonymous(string symbol)
        {
            string trimmed = symbol.Trim(new char[] { '(', ')' });
            int addr = GetAnonymousAddress(_currentLine, trimmed);
            if (addr < 0)
            {
                Log.LogEntry(_currentLine, ErrorStrings.CannotResolveAnonymousLabel);
                return "0";
            }
            return addr.ToString();
        }

        /// <summary>
        /// Determines whether the token is a reserved keyword, such as an instruction
        /// or assembler directive.
        /// </summary>
        /// <param name="token">The token to test.</param>
        /// <returns>True, if the token is a reserved word, otherwise false.</returns>
        protected override bool IsReserved(string token)
        {
            bool reserved = Reserved.IsReserved(token) || 
                _conditionAssembler.AssemblesInstruction(token);

            if (!reserved)
            {
                foreach (var asm in _assemblers)
                {
                    if (asm.AssemblesInstruction(token))
                    {
                        reserved = true;
                        break;
                    }
                }
            }
            return reserved;
        }


        /// <summary>
        /// Checks whether the token is a valid symbol/label name.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <param name="allowLeadUnderscore">Allow the token to have a leading underscore
        /// for it to be a symbol.</param>
        /// <param name="allowDot">Allow the token to have separating dots for it to be
        /// considered a symbol.</param>
        /// <returns></returns>
        private bool IsSymbolName(string token, bool allowLeadUnderscore = true, bool allowDot = true)
        {
            // empty string 
            if (string.IsNullOrEmpty(token))
                return false;

            // is a reserved word
            if (IsReserved(token))
                return false;

            // if leading underscore not allowed
            if (!allowLeadUnderscore && token.StartsWith("_"))
                return false;

            if (token.Contains("."))
            {
                if (!allowDot || token.EndsWith("."))
                    return false;
            }

            // otherwise...
            return Regex.IsMatch(token, @"^_?[a-zA-Z][a-zA-Z0-9_.]*$");
        }

        /// <summary>
        /// Preprocess the source file into a System.IEnumerable&lt;DotNetAsm.SourceLine&gt;.
        /// Define macros and segments, and add included source files.
        /// </summary>
        /// <returns>The preprocessed System.IEnumerable&lt;DotNetAsm.SourceLine&gt;</returns>
        private IEnumerable<SourceLine> Preprocess()
        {
            List<SourceLine> source = new List<SourceLine>();

            source.AddRange(ProcessDefinedLabels());

            Preprocessor processor = new Preprocessor(this,
                                                      IsReserved,
                                                      s => IsSymbolName(s.TrimEnd(':'), true, false));
            processor.FileRegistry = FileRegistry;
            foreach (var file in Options.InputFiles)
            {
                if (processor.FileRegistry.Add(file) == false)
                {
                    throw new Exception(string.Format(ErrorStrings.FilePreviouslyIncluded, file));
                }
                source.AddRange(processor.ConvertToSource(file));

                if (Log.HasErrors)
                    break;
            }

            if (Log.HasErrors == false)
               return processor.ExpandMacros(source);
            return null;
        }

        /// <summary>
        /// Add labels defined with command-line -D option
        /// </summary>
        /// <returns>Returns a System.Collections.Generic.IEnumerable&lt;SourceLine&gt; 
        /// that will define the labels at assembly time.</returns>
        private IEnumerable<SourceLine> ProcessDefinedLabels()
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
                    SourceString = string.Format("{0}={1} ;-D {2}", name, definition, label)
                });
            }
            return labels;
        }

        /// <summary>
        /// Convert a System.Stack&lt;string&gt; into a string.
        /// </summary>
        /// <param name="scope">The System.Stack&lt;string&gt; to convert.</param>
        /// <returns></returns>
        private string GetScopeString(Stack<string> scope)
        {
            if (scope.Count == 0)
                return string.Empty;
            string scopestring = string.Join(".", scope.Reverse());
            return scopestring;
        }

        /// <summary>
        /// Performs a first (or more) pass of preprocessed source to resolve all 
        /// actual symbol values, process conditions and repetitions, and add to 
        /// Processed Lines.
        /// </summary>
        /// <param name="source">The preprocessed System.IEnumerable&lt;DotNetAsm&gt;</param>
        private void FirstPass(IEnumerable<SourceLine> source)
        {
            _passes = 0;

            Stack<string> scope = new Stack<string>();
            
            int anon = 0;
            int id = 0;

            RepetitionHandler handler = new RepetitionHandler(this);

            foreach(SourceLine line in source)
            {
                try
                {
                    if (line.DoNotAssemble)
                    {
                        if (line.IsComment)
                            ProcessedLines.Add(line);
                        continue;
                    }
                    _currentLine = line;

                    _conditionAssembler.AssembleLine(line);

                    if (line.DoNotAssemble)
                        continue;

                    if (line.Instruction.Equals(".end", Options.StringComparison))
                        break;

                    if (handler.Processes(line.Instruction) || handler.IsProcessing)
                    {
                        handler.Process(line);
                        if (handler.IsProcessing == false)
                        {
                            var processedLines = handler.ProcessedLines;
                            foreach (SourceLine l in processedLines)
                            {
                                l.Id = id;
                                FirstPassLine(l, scope, ref anon);
                                id++;
                            }
                            handler.Reset();
                        }
                    }
                    else
                    {
                        line.Id = id;
                        FirstPassLine(line, scope, ref anon);
                        id++;
                    }
                }
                catch (Compilation.InvalidPCAssignmentException ex)
                {
                    Log.LogEntry(line, ErrorStrings.InvalidPCAssignment, ex.Message);
                }
                catch (StackOverflowException)
                {
                    _passes = 4;
                }
                catch (Exception)
                {
                    Log.LogEntry(line, ErrorStrings.None);
                }
            }
            if (scope.Count > 0 || handler.IsProcessing || _conditionAssembler.InConditionBlock)
            {
                Log.LogEntry(ProcessedLines.Last(), ErrorStrings.MissingClosure);
            }
        }

        /// <summary>
        /// Performs a first pass on the DotNetAsm.SourceLine, including updating 
        /// the Program Counter and definining labels.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to perform a pass</param>
        /// <param name="scope">The current scope as a System.Stack&lt;string;&gt;</param>
        /// <param name="anon">The counter of anonymous blocks</param>
        private void FirstPassLine(SourceLine line, Stack<string> scope, ref int anon)
        {
            UpdatePC(line);

            line.PC = Output.GetPC();

            DefineLabel(line, scope, ref anon);

            if (!IsDefiningConstant(line))
                Output.AddUninitialized(GetInstructionSize(line));
            
            ProcessedLines.Add(line);
        }

        /// <summary>
        /// Perform a second or final pass on a DotNetAsm.SourceLine, including final 
        /// assembly of bytes.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to perform pass</param>
        /// <param name="anon">The current counter of anonymous blocks</param>
        /// <param name="finalPass">A flag indicating this is a final pass</param>
        /// <returns>True, if another pass is needed. Otherwise false.</returns>
        private bool SecondPassLine(SourceLine line, ref int anon, bool finalPass)
        {
            UpdatePC(line);
            bool passNeeded = false;
            if (IsDefiningConstant(line))
            {
                if (line.Label.Equals("*")) return false;
                long val = Evaluator.Eval(line.Operand);
                if (line.Instruction.Equals("-") || line.Instruction.Equals("+"))
                {
                    passNeeded = (int)val != line.PC;
                }
                else
                {
                    string scoped = GetNearestScope(line.Label, line.Scope);
                    passNeeded = !(val.ToString().Equals(Labels[scoped]));
                    Labels[scoped] = val.ToString();
                }
                line.PC = (int)val;
            }
            else
            {

                if (IsSymbolName(line.Label, true, false) ||
                    line.Instruction.Equals(".block", Options.StringComparison))
                {
                    string label = line.Label;
                    if (string.IsNullOrEmpty(label))
                    {
                        label = anon.ToString();
                        anon++;
                    }
                    label = GetNearestScope(label, line.Scope);
                    Labels[label] = Output.GetPC().ToString();
                }
                passNeeded = line.PC != Output.GetPC();
                line.PC = Output.GetPC();
                if (finalPass)
                    AssembleLine(line);
                else
                    Output.AddUninitialized(GetInstructionSize(line));
            }
            return passNeeded;
        }

        /// <summary>
        /// Perform a second pass on the processed source, including output to binary.
        /// </summary>
        private void SecondPass()
        {
            bool passNeeded = true;
            bool finalPass = false;
            _passes++;

            var assembleLines = ProcessedLines.Where(l => l.DoNotAssemble == false);

            while (_passes <= 4 && Log.HasErrors == false)
            {
                passNeeded = false;
                Output.Reset();
                int anon = 0;

                foreach(SourceLine line in assembleLines)
                {
                    try
                    {
                        if (line.Instruction.Equals(".end", Options.StringComparison))
                            break;

                        _currentLine = line;

                        bool needpass = SecondPassLine(line, ref anon, finalPass);
                        if (!passNeeded)
                            passNeeded = needpass;

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
            if (_passes > 4)
            {
                throw new Exception("Too many passes attempted.");
            }
        }

        /// <summary>
        /// Add a line assembler to the IAssemblyController's list of assemblers.
        /// </summary>
        /// <param name="lineAssembler">The DotNetAsm.ILineAssembler</param>
        public void Add(ILineAssembler lineAssembler)
        {
            _assemblers.Add(lineAssembler);
        }

        /// <summary>
        /// Assembles a SourceLine to output.
        /// </summary>
        /// <param name="line">A SourceLine.</param>
        public void AssembleLine(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Instruction))
            {
                if (!string.IsNullOrEmpty(line.Operand))
                    Log.LogEntry(line, ErrorStrings.None);
                return;
            }

            foreach(var asm in _assemblers)
            {
                if (asm.AssemblesInstruction(line.Instruction))
                {
                    asm.AssembleLine(line);
                    GetAssemblyBytes(line);
                    return;
                }
            }
            if (Reserved.IsReserved(line.Instruction) == false)
                Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
        }

        /// <summary>
        /// Copy the output bytes to the DotNetAsm.SourceLine assembly.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to copy bytes to.</param>
        private void GetAssemblyBytes(SourceLine line)
        {
            line.Assembly.Clear();
            int range = Output.GetPC() - line.PC; 
            var robytes = Output.GetCompilation().ToList();
            int logicalsize = Output.ProgramCounter - Output.ProgramStart;
            if (robytes.Count - range < 0)
            {
                if (robytes.Count == 0)
                    return;
                range = robytes.Count;
            }
            else if (logicalsize > robytes.Count)
            {
                if (logicalsize > robytes.Count + range)
                    return;
                range = range - (logicalsize - robytes.Count);
            }
            if (range > 0)
                line.Assembly.AddRange(robytes.GetRange(robytes.Count - range, range));
        }

        /// <summary>
        /// Gets all subscopes from the current parent scope.
        /// </summary>
        /// <param name="parent">The parent scope.</param>
        /// <returns>A listing of all scopes, including parent.</returns>
        private IEnumerable<string> GetSubScopes(string parent)
        {
            if (string.IsNullOrEmpty(parent))
            {
                return new List<string>();
            }
            var result = new List<string>();
            result.Add(parent);
            var split = parent.Split('.').ToList();
            split.RemoveAt(split.Count - 1);
            string combined = string.Join(".", split);
            result.AddRange(GetSubScopes(combined).ToList());
            return result;
        }

        /// <summary>
        /// Gets the nearest scope for the given token in its given scope.
        /// </summary>
        /// <param name="token">The line token.</param>
        /// <param name="scope">The line scope.</param>
        /// <returns>Returns the nearest scope for the token.</returns>
        private string GetNearestScope(string label, string linescope)
        {
            List<string> scopes = GetSubScopes(linescope).ToList();

            foreach (var s in scopes)
            {
                string scoped = s + "." + label;
                if (Labels.ContainsKey(scoped.TrimStart('.')))
                {
                    return scoped.TrimStart('.');
                }
            }
            return label;
        }

        /// <summary>
        /// This does a quick and "dirty" look at instructions. It will catch
        /// some but not all syntax errors, concerned mostly with the probable 
        /// size of the instruction. 
        /// </summary>
        /// <param name="line">The SourceLine to read</param>
        /// <param name="instruction_ix">The index of the instruction in the line tokens</param>
        /// <returns>The size in bytes of the instruction, including opcode and operand</returns>
        private int GetInstructionSize(SourceLine line)
        {
            foreach(var asm in _assemblers)
            {
                if (asm.AssemblesInstruction(line.Instruction))
                    return asm.GetInstructionSize(line);
            }
            return 0;
        }

        /// <summary>
        /// Examine a DotNetAsm.SourceLine and determine if a label is being defined.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to examine</param>
        /// <param name="scope">The current scope as a Stack&lt;string&gt;</param>
        /// <param name="anon">The current counter to the anonymous blocks</param>
        private void DefineLabel(SourceLine line, Stack<string> scope, ref int anon)
        {
            string currentScope = line.Scope = GetScopeString(scope);

            if (string.IsNullOrEmpty(line.Label) == false ||
                    line.Instruction.Equals(".block", Options.StringComparison))
            {
                if (line.Label.Equals("*"))
                    return;

                string scopedLabel = string.Empty;

                if (string.IsNullOrEmpty(line.Label) || _specialLabels.IsMatch(line.Label))
                {
                    if (IsDefiningConstant(line))
                    {
                        line.PC = Convert.ToInt32(Evaluator.Eval(line.Operand));
                    }
                    else
                    {
                        line.PC = Output.GetPC();
                    }
                    if (line.Instruction.Equals(".block", Options.StringComparison))
                    {
                        scopedLabel = currentScope + "." + anon.ToString();
                        scopedLabel = scopedLabel.TrimStart('.');

                        if (string.IsNullOrEmpty(line.Label))
                            line.Scope = scopedLabel;

                        Labels.Add(scopedLabel, line.PC.ToString());
                        scope.Push(anon.ToString());
                        anon++;
                    }
                    if (line.Label.Equals("+") || line.Label.Equals("-"))
                    {
                        if (line.Label == "+")
                            AnonPlus.Add(line.Id);
                        else
                            AnonMinus.Add(line.Id);

                    }
                }
                else
                {
                    if (IsSymbolName(line.Label, true, false) == false)
                    {
                        Log.LogEntry(line, ErrorStrings.LabelNotValid, line.Label);
                        return;
                    }

                    scopedLabel = currentScope + "." + line.Label;
                    line.Scope = scopedLabel = scopedLabel.TrimStart('.');
                    if (line.Instruction.Equals(".block", Options.StringComparison))
                    {
                        scope.Push(line.Label);
                    }
                    if (Labels.ContainsKey(scopedLabel))
                    {
                        Log.LogEntry(line, ErrorStrings.LabelRedefinition, line.Label);
                        return;
                    }
                    string val = line.PC.ToString();
                    if (IsDefiningConstant(line))
                    {
                        val = Evaluator.Eval(line.Operand).ToString();
                    }
                    Labels.Add(scopedLabel, val);
                }
            }

            if (line.Instruction.Equals(".endblock", Options.StringComparison))
            {
                if (scope.Count < 1)
                {
                    Log.LogEntry(line, ErrorStrings.ClosureDoesNotCloseBlock);
                    return;
                }
                scope.Pop();
            }
        }

        /// <summary>
        /// Determine if the DotNetAsm.SourceLine updates the output's Program Counter
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to examine</param>
        private void UpdatePC(SourceLine line)
        {
            long val = 0;
            if (line.Label.Equals("*"))
            {
                if (IsDefiningConstant(line))
                {
                    val = Evaluator.Eval(line.Operand);
                    if (val < UInt16.MinValue || val > UInt16.MaxValue)
                    {
                        Log.LogEntry(line, ErrorStrings.IllegalQuantity, val.ToString());
                        return;
                    }
                    Output.SetPC(Convert.ToUInt16(val));
                }
                else
                {
                    Log.LogEntry(line, ErrorStrings.None);
                }
                return;
            }
            string instruction = Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();
            
            switch (instruction)
            {
                case ".relocate":
                case ".pseudopc":
                    {
                        if (string.IsNullOrEmpty(line.Operand))
                        {
                            Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                            return;
                        }
                        val = Evaluator.Eval(line.Operand);
                        if (val < UInt16.MinValue || val > UInt16.MaxValue)
                        {
                            Log.LogEntry(line, ErrorStrings.IllegalQuantity, val.ToString());
                            return;
                        }
                        Output.SetLogicalPC(Convert.ToUInt16(val));
                    }
                    break;
                case ".endrelocate":
                case ".realpc":
                    {
                        if (string.IsNullOrEmpty(line.Operand) == false)
                        {
                            Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
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
        /// <param name="line">The SourceLine to test.</param>
        /// <returns>True, if the line is defining a constant, otherwise false.</returns>
        private bool IsDefiningConstant(SourceLine line)
        {
            if (line.Operand.EnclosedInQuotes())
                return false; // define a constant string??

            if (line.Instruction.Equals("=") || line.Instruction.Equals(".equ", Options.StringComparison))
                return true;

            return false;
        }

        /// <summary>
        /// Print the status of the assembly results to console output.
        /// </summary>
        private void PrintStatus(DateTime asmTime)
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
                Console.WriteLine("Assembly end:   ${0:X4}", Output.GetPC());
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
        /// <param name="args">The arguments passed in the command line by the user.</param>
        private void ToListing()
        {
            if (string.IsNullOrEmpty(Options.ListingFile) && string.IsNullOrEmpty(Options.LabelFile))
                return;

            string listing;

            if (!string.IsNullOrEmpty(Options.ListingFile))
            {
                listing = GetListing();
                using (StreamWriter writer = new StreamWriter(Options.ListingFile, false))
                {
                    string exec = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    string argstring = string.Join(" ", Options.Arguments);
                    string bannerstring = BannerText.Split(new char[] { '\n', '\r' }).First();
                    writer.WriteLine(";; {0}", bannerstring);
                    writer.WriteLine(";; {0} {1}", exec, argstring);
                    writer.WriteLine(";; {0:f}\n", DateTime.Now);
                    writer.WriteLine(";; Input files:\n");

                    FileRegistry.ToList().ForEach(f => writer.WriteLine(";; {0}", f));

                    writer.WriteLine();
                    writer.Write(listing);
                }
            }
            if (!string.IsNullOrEmpty(Options.LabelFile))
            {
                listing = GetLabels();
                using (StreamWriter writer = new StreamWriter(Options.LabelFile, false))
                {
                    writer.WriteLine(";; Input files:\n");

                    FileRegistry.ToList().ForEach(f => writer.WriteLine(";; {0}", f));
                    writer.WriteLine();
                    writer.WriteLine(listing);
                }
            }
        }

        /// <summary>
        /// Used by the ToListing method to get a listing of all defined labels.
        /// </summary>
        /// <returns>A string containing all label definitions.</returns>
        private string GetLabels()
        {
            StringBuilder listing = new StringBuilder();
            Labels.ToList().ForEach(delegate(KeyValuePair<string, string> label)
            {
                var labelname = Regex.Replace(label.Key, @"(?<=^|\.)[0-9]+(?=\.|$)", "{anonymous}");
                var maxlen = labelname.Length > 30 ? 30 : labelname.Length;
                if (maxlen < 0) maxlen++;
                labelname = labelname.Substring(labelname.Length - maxlen, maxlen);
                var val = Convert.ToInt64(label.Value);
                var size = val.Size() * 2;
                listing.AppendFormat("{0,-30} = ${1,-4:x" + size.ToString() + "} ({2})",
                    labelname,
                    val,
                    label.Value)
                    .AppendLine();
            });
            return listing.ToString();
        }

        /// <summary>
        /// Used by the ToListing method to get the full listing.</summary>
        /// <returns>Returns a listing string to save to disk.</returns>
        private string GetListing()
        {
            StringBuilder listing = new StringBuilder();

            foreach (SourceLine line in ProcessedLines)
            {
                if (line.Instruction.Equals(".end", Options.StringComparison))
                    break;

                Disassembler.DisassembleLine(line, listing);
            }
            if (listing.ToString().EndsWith(Environment.NewLine))
                return listing.ToString().Substring(0, listing.Length - Environment.NewLine.Length);

            return listing.ToString();
        }

        /// <summary>
        /// Saves the output to disk.
        /// </summary>
        private void SaveOutput()
        {
            if (!Options.GenerateOutput)
                return;

            var outputfile = Options.OutputFile;
            if (string.IsNullOrEmpty(Options.OutputFile))
                outputfile = "a.out";

            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputfile, FileMode.Create, FileAccess.Write)))
            {
                if (HeaderOutputAction != null)
                    HeaderOutputAction(this, writer);

                writer.Write(Output.GetCompilation().ToArray());

                if (FooterOutputAction != null)
                    FooterOutputAction(this, writer);
            }
        }

        public void Assemble()
        {
            if (Options.InputFiles.Count == 0)
                return;

            if (Options.PrintVersion)
                Console.WriteLine(VerboseBannerText);

            if (Options.Quiet)
                Console.SetOut(TextWriter.Null);

            if (Options.PrintVersion == false)
                Console.WriteLine(BannerText);

            DateTime asmTime = DateTime.Now;

            var source = Preprocess();

            if (Log.HasErrors == false)
            {
                FirstPass(source);

                SecondPass();

                if (Log.HasErrors == false)
                {
                    SaveOutput();

                    ToListing();
                }
            }
            PrintStatus(asmTime);
        }

        public string GetScopedLabelValue(string label, SourceLine line)
        {
            label = GetNearestScope(label, line.Scope);
            if (Labels.ContainsKey(label))
                return _evaluator.Eval(Labels[label]).ToString();
            return string.Empty;
        }

        /// <summary>
        /// Used by the expression evaluator to get the actual value of the symbol.
        /// </summary>
        /// <param name="symbol">The symbol to look up.</param>
        /// <param name="notused">The match group (not used)</param>
        /// <param name="obj">A helper object, usually a SourceLine to establish the current
        /// scope.</param>
        /// <returns>The underlying value of the symbol.</returns>
        private string GetLabelValue(string symbol)
        {
            string value;
            
            value = GetScopedLabelValue(symbol, _currentLine);
            if (string.IsNullOrEmpty(value))
            {
                if (_passes > 0)
                {
                    Log.LogEntry(_currentLine, ErrorStrings.LabelNotDefined, symbol);
                    return string.Empty;
                }
                return ExpressionEvaluator.EVAL_FAIL;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Gets the actual address of an anonymous symbol.
        /// </summary>
        /// <param name="fromLine">The SourceLine containing the anonymous symbol.</param>
        /// <param name="operand">The operand.</param>
        /// <returns>Returns the anonymous symbol address.</returns>
        private int GetAnonymousAddress(SourceLine fromLine, string operand)
        {
            int count = operand.Length - 1;
            IOrderedEnumerable<int> idList;
            if (operand.First() == '-')
            {
                idList = AnonMinus.Where(i => i < fromLine.Id).OrderByDescending(i => i);
            }
            else
            {
                idList = AnonPlus.Where(i => i > fromLine.Id).OrderBy(i => i);
            }
            int id = 0;
            string scope = fromLine.Scope; 

            while (id != -1)
            {
                id = idList.Count() > count ? idList.ElementAt(count) : -1;

                var lines = from line in ProcessedLines
                            where line.Id == id && line.Scope == scope
                            select line;
                if (lines.Count() == 0)
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

        /// <summary>
        /// Tabulates all anonymous labels in the ProcessedLines.
        /// </summary>
        private void GetAnonymousLabels(IEnumerable<SourceLine> lines)
        {
            int id = 0;
            lines.ToList().ForEach(l => l.Id = ++id);
            foreach (var line in lines)
            {
                if (line.Label == "+")
                    AnonPlus.Add(line.Id);
                else if (line.Label == "-")
                    AnonMinus.Add(line.Id);
            }
        }

        public AsmCommandLineOptions Options { get; private set; }

        public Compilation Output {  get; private set; }

        public ErrorLog Log { get; private set; }

        public IDictionary<string, string> Labels {  get; private set; }

        public IEvaluator Evaluator { get { return _evaluator; } }

        /// <summary>
        /// Gets or sets the disassembler. The default disassembler is the DotNetAsm.Disassembler.
        /// </summary>
        public ILineDisassembler Disassembler { get; set; }

        public Action<IAssemblyController, BinaryWriter> HeaderOutputAction { get; set; }

        public Action<IAssemblyController, BinaryWriter> FooterOutputAction { get; set; }

        public string BannerText { get; set; }

        public string VerboseBannerText { get; set; }

        private HashSet<string> FileRegistry { get; set; }

        /// <summary>
        /// Gets the command-line arguments passed by the end-user and parses into 
        /// strongly-typed options.
        /// </summary>
        private List<SourceLine> ProcessedLines { get; set; }

        /// <summary>
        /// Gets or sets the list of unique IDs for SourceLines whose labels
        /// are forward-reference anonymous symbols.
        /// </summary>
        private HashSet<int> AnonPlus { get; set; }

        /// <summary>
        /// Gets or sets the list of unique IDs for SourceLines whose labels
        /// are backward-reference anonymous symbols.
        /// </summary>
        private HashSet<int> AnonMinus { get; set; }
    }
}
