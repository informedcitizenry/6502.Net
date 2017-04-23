//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
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
using System.Text.RegularExpressions;

namespace Asm6502.Net
{
    /// <summary>
    /// The assembly controller for 6502.Net assembler application. This class cannot be inherited.
    /// </summary>
    public sealed class Asm6502Controller : AssemblerBase, IAssemblyController
    {
        #region Members

        private ExpressionEvaluator evaluator_;

        private ILineAssembler pseudoOps_;

        private ILineAssembler lineAssembler_;

        private ILineAssembler directives_;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new 6502.Net assembler controller.
        /// </summary>
        public Asm6502Controller() :
            base()
        {
            Reserved.Types.Add("Functions", new HashSet<string>(new string[]
                {
                     "abs", "acos", "asin", "atan", "cbrt", "ceil", "cos", "cosh", "deg", 
                     "exp", "floor", "frac", "hypot", "ln", "log10", "pow", "rad", "random", 
                     "round", "sgn", "sin", "sinh", "sqrt", "str", "tan", "tanh", "trunc"
                }));

            Reserved.Types.Add("Directives", new HashSet<string>(new string[]
                {
                    ".proff", ".pron", ".end", ".equ"
                }));

            ProcessedLines = new List<SourceLine>();
            AnonPlus = new HashSet<int>();
            AnonMinus = new HashSet<int>();
            Output = new Compilation();
            Log = new ErrorLog();
            FileRegistry = new HashSet<string>();
            Options = new AsmCommandLineOptions();

            evaluator_ = new ExpressionEvaluator();
            evaluator_.SymbolLookups.Add(@"_?[a-zA-Z][a-zA-Z0-9_\.]*", GetLabelValue);
            evaluator_.SymbolLookups.Add(@"^\++$|^-+$|\(\++\)|\(-+\)", ConvertAnonymous);
            evaluator_.SymbolLookups.Add(@"\*", (str, obj) => Output.GetPC().ToString());
            evaluator_.AllowAlternateBinString = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks whether the token is a symbol/label.
        /// </summary>
        /// <param name="token">The token to check.</param>
        /// <param name="allowLeadUnderscore">Allow the token to have a leading underscore
        /// for it to be a symbol.</param>
        /// <param name="allowDot">Allow the token to have separating dots for it to be
        /// considered a symbol.</param>
        /// <returns></returns>
        private bool IsSymbol(string token, bool allowLeadUnderscore = true, bool allowDot = true)
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
            return Regex.IsMatch(token, "^_?[a-zA-Z][a-zA-Z_.0-9]*$");
        }

        /// <summary>
        /// Determines whether the token is a reserved keyword, such as an instruction
        /// or assembler directive.
        /// </summary>
        /// <param name="token">The token to test.</param>
        /// <returns>True, if the token is a reserved word, otherwise false.</returns>
        private bool IsReserved(string token)
        {
            return pseudoOps_.AssemblesInstruction(token) ||
                    lineAssembler_.AssemblesInstruction(token) ||
                    directives_.AssemblesInstruction(token) ||
                    Reserved.IsReserved(token);
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
            
            if (string.IsNullOrEmpty(line.Label) == false &&
                string.IsNullOrEmpty(line.Instruction) &&
                string.IsNullOrEmpty(line.Operand))
                return true;
            return false;
        }

        /// <summary>
        /// This does a quick and "dirty" look at instructions. It will catch
        /// some but not all syntax errors, concerned mostly with the probable 
        /// size of the instruction. 
        /// </summary>
        /// <param name="line">The SourceLine to read</param>
        /// <param name="instruction_ix">The index of the instruction in the line tokens</param>
        /// <returns>The size in bytes of the instruction, including opcode and operand</returns>
        public int GetInstructionSize(SourceLine line)
        {
            if (lineAssembler_.AssemblesInstruction(line.Instruction))
                return lineAssembler_.GetInstructionSize(line);
            else if (pseudoOps_.AssemblesInstruction(line.Instruction))
                return pseudoOps_.GetInstructionSize(line);
            else if (directives_.AssemblesInstruction(line.Instruction))
                return directives_.GetInstructionSize(line);
            return 0;
        }

        /// <summary>
        /// Used by the expression evaluator to convert an anonymous symbol
        /// to an address.
        /// </summary>
        /// <param name="symbol">The anonymous symbol.</param>
        /// <param name="obj">A helper object, in this case a SourceLine.</param>
        /// <returns>The actual address the anonymous symbol will resolve to.</returns>
        private string ConvertAnonymous(string symbol, object obj)
        {
            SourceLine line = obj as SourceLine;
            System.Diagnostics.Debug.Assert(line != null);
            string trimmed = symbol.Trim(new char[] { '(', ')' });
            int addr = GetAnonymousAddress(line, trimmed);
            if (addr < 0)
            {
                Log.LogEntry(line, Resources.ErrorStrings.CannotResolveAnonymousLabel);
                return "0";
            }
            return addr.ToString();
        }

        /// <summary>
        /// Used by the expression evaluator to get the actual value of the symbol.
        /// </summary>
        /// <param name="symbol">The symbol to look up.</param>
        /// <param name="obj">A helper object, usually a SourceLine to establish the current
        /// scope.</param>
        /// <returns>The underlying value of the symbol.</returns>
        private string GetLabelValue(string symbol, object obj)
        {
            string label = symbol;
            SourceLine line = ProcessedLines.Last();
            if (obj != null)
            {
                line = obj as SourceLine;
                System.Diagnostics.Debug.Assert(line != null);
                label = GetNearestScope(symbol, line.Scope);
            }
            if (Labels.ContainsKey(label))
            {
                return Labels[label].Value.ToString();
            }
            else
            {
                Log.LogEntry(line, Resources.ErrorStrings.LabelNotDefined);
                return ExpressionEvaluator.EVAL_FAIL;
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
            string scope = fromLine.GetScope(true); // fix: remove scope of "standard" labels!!!

            while (id != -1)
            {
                id = idList.Count() > count ? idList.ElementAt(count) : -1;
                
                var lines = from line in ProcessedLines
                            where line.Id == id && line.GetScope(true) == scope
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
        /// Assemble the ProcessedLines to output.
        /// </summary>
        private void AssembleToOutput()
        {
            Output.Reset();

            foreach(var line in ProcessedLines.Where(pl => pl.DoNotAssemble == false))
            {
                if (line.Instruction.Equals(".end", Options.StringComparison))
                    break;
                AssembleLine(line);
            }
        }

        /// <summary>
        /// Assembles a SourceLine to output.
        /// </summary>
        /// <param name="line">A SourceLine.</param>
        private void AssembleLine(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Instruction))
            {
                if (!string.IsNullOrEmpty(line.Operand))
                    Log.LogEntry(line);
                return;
            }

            evaluator_.SymbolLookupObject = line;

            if (IsDefiningConstant(line))
            {
                if (line.Label == "*")
                {
                    Output.SetPC(Convert.ToUInt16(evaluator_.Eval(line.Operand)));
                }
                return;
            }

            if (IsSymbol(line.Label) || line.Label == "-" || line.Label == "+")
            {
                if (string.IsNullOrEmpty(line.Instruction))
                    return;
            }

            var fcnmatch = Regex.Match(line.Operand, @"([a-z][a-zA-Z0-9]*)\(");
            if (string.IsNullOrEmpty(fcnmatch.Value) == false)
            {
                if (Reserved.IsOneOf("Functions", fcnmatch.Groups[1].Value) == false)
                {
                    Log.LogEntry(line, "Unknown function call '" + fcnmatch.Value + ")'");
                    return;
                }
            }
            
            try
            {
                if (lineAssembler_.AssemblesInstruction(line.Instruction))
                {
                    lineAssembler_.AssembleLine(line);
                    GetAssemblyBytes(line);
                }
                else if (pseudoOps_.AssemblesInstruction(line.Instruction))
                {
                    pseudoOps_.AssembleLine(line);
                    GetAssemblyBytes(line);
                }
                else if (directives_.AssemblesInstruction(line.Instruction))
                {
                    directives_.AssembleLine(line);
                }
                else 
                {
                    Log.LogEntry(line, Resources.ErrorStrings.UnknownInstruction, line.Instruction);
                }
            }
            catch (Exception ex)
            {
                Log.LogEntry(line, ex.Message);
            }
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

            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputfile, FileMode.Create, FileAccess.Write)))//File.OpenWrite(OutputFile)))
            {
                if (!Options.SuppressCbmHeader)
                    writer.Write(Convert.ToUInt16(Output.ProgramStart));

                writer.Write(Output.GetCompilation().ToArray());
            }
        }

        private void GetAssemblyBytes(SourceLine line)
        {
            int range = GetInstructionSize(line);
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
        /// Used by the ToListing method to get the full listing.
        /// </summary>
        /// <param name="verbose">Sets the verbose option.</param>
        /// <returns>Returns a listing string to save to disk.</returns>
        private string GetFullListing(bool verbose)
        {
            string listing = string.Empty;
            foreach (var line in ProcessedLines)
            {
                bool print = true;

                if (!line.DoNotAssemble)
                {
                    if (line.Instruction.Equals(".pron", Options.StringComparison))
                        print = true;
                    else if (line.Instruction.Equals(".proff", Options.StringComparison))
                        print = false;
                }
                if (!print)
                    continue; // printing has been suppressed

                string disassem = string.Empty;
                string sourcestr = line.SourceString;

                if (!verbose)
                {
                    if (line.DoNotAssemble)
                    {
                        if (line.IsDefinition && string.IsNullOrEmpty(line.Label))
                            continue;
                        sourcestr = line.Label;
                    }
                    if (string.IsNullOrEmpty(line.Label) && 
                        (Reserved.IsOneOf("Directives", line.Instruction) || 
                         directives_.AssemblesInstruction(line.Instruction)))
                        continue; // skip directives (e.g., .if blocks, etc.) and anonymous blocks
                    else if (string.IsNullOrWhiteSpace(line.Label + line.Instruction))
                        continue;
                }
                else
                {
                    string lineinfo = line.Filename;
                    if (lineinfo.Length > 14)
                        lineinfo = lineinfo.Substring(0, 11) + "...";
                    lineinfo += "(" + line.LineNumber.ToString() + ")";
                    disassem = string.Format("{0,-20}:", lineinfo);
                }
                var collen = string.IsNullOrEmpty(line.Disassembly) ? 36 : 20;
                if (verbose)
                    collen += 21;
                if (string.IsNullOrEmpty(line.Instruction))
                    goto updatelisting;

                if (Reserved.IsOneOf("Directives", line.Instruction))
                {
                    // do not print out the source for directives unless verbose
                    if (!verbose)
                        sourcestr = line.Label;
                    else if (string.IsNullOrEmpty(sourcestr))
                        sourcestr = line.Instruction;
                    goto updatelisting;
                }
                if (line.DoNotAssemble) goto updatelisting;
    
                if (IsDefiningConstant(line))
                {
                    Int64 value = 0;
                    if (line.Label == "*")
                        continue;
                    if (string.IsNullOrEmpty(line.Operand) || line.Operand == "*")
                        value = line.PC;
                    else
                        value = evaluator_.Eval(line.Operand);
                    disassem += string.Format("=${0:x}  ", value);
                    goto updatelisting;
                }
                else
                {
                    if (pseudoOps_.AssemblesInstruction(line.Instruction))
                        disassem += string.Format(">{0:x4}  ", line.PC);
                    else
                        disassem += string.Format(".{0:x4}  ", line.PC);
                }
                
                line.Assembly.ForEach(b => disassem += string.Format(" {0:x2}", b));
                if (pseudoOps_.AssemblesInstruction(line.Instruction))
                {
                    string monitor = disassem;
                    if (verbose)
                        monitor = disassem.Substring(21);
                    if (monitor.Length >= 30)
                    {
                        int pc = line.PC;
                        string source = line.SourceString;
                        var subdisasms = monitor.Substring(8).SplitByLength(24);
                        string file = disassem.Substring(0, 20);
                        foreach (string l in subdisasms)
                        {
                            string subdisasm;
                            if (verbose)
                                subdisasm = string.Format("{0,-20}:>{1:x4}   {2}", file, pc, l);
                            else
                                subdisasm = string.Format(">{0:x4}   {1}", pc, l);
                            listing += string.Format("{0,-" + collen + "}{1,-10}{2}", subdisasm, source, Environment.NewLine);
                            pc += 8;
                            source = string.Empty;
                            file = string.Empty;
                        }
                        continue;
                    }
                }
            updatelisting:
                if (string.IsNullOrEmpty(line.Disassembly))
                {
                    
                    listing += string.Format("{0,-" + collen + "}{1,-10}{2}",
                                                            disassem,
                                                            sourcestr,
                                                            Environment.NewLine);
                }
                else
                {
                    listing += string.Format("{0,-" + collen + "}{1,-16}{2,-10}{3}",
                                            disassem,
                                            line.Disassembly,
                                            sourcestr,
                                            Environment.NewLine);
                }
                if (line.Instruction.Equals(".end", Options.StringComparison))
                    break;
            }
            if (listing.EndsWith(Environment.NewLine))
                listing = listing.Substring(0, listing.Length - 2);
           
            return listing;
        }

        /// <summary>
        /// Used by the ToListing method to get a listing of all defined labels.
        /// </summary>
        /// <returns>A string containing all label definitions.</returns>
        private string GetLabels()
        {
            string listing = string.Empty;
            Labels.ToList().ForEach(delegate(KeyValuePair<string, Label> label)
            {
                var labelname = Regex.Replace(label.Key, @"!anon[0-9]+", "{anonymous}");
                var maxlen = labelname.Length > 30 ? 30 : labelname.Length;
                if (maxlen < 0) maxlen++;
                labelname = labelname.Substring(labelname.Length - maxlen, maxlen);
                listing += string.Format("{0,-30} = ${1,-4:x" + label.Value.Size*2 + "} ({1}){2}", 
                    labelname, 
                    label.Value.Value,
                    Environment.NewLine);
            });
            return listing;
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
                listing = GetFullListing(Options.VerboseList);
                using (StreamWriter writer = new StreamWriter(Options.ListingFile, false))
                {
                    string argstring = string.Join(" ", Options.Arguments);
                    writer.WriteLine(";; 6502.Net V.{0}.{1}, simple .Net 6502 Cross-Assembler (C) Copyright 2017 Nate Burnett.",
                        System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major,
                        System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor);
                    writer.WriteLine(";; 6502.Net {0}", argstring);
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
        /// Tabulates all anonymous labels in the ProcessedLines.
        /// </summary>
        private void GetAnonymousLabels()
        {
            int id = 0;
            ProcessedLines.ForEach(l => l.Id = ++id);
            foreach(var line in ProcessedLines)
            {
                if (string.IsNullOrEmpty(line.Label))
                    continue;
                if (line.Label == "+")
                    AnonPlus.Add(line.Id);
                else if (line.Label == "-")
                    AnonMinus.Add(line.Id);
            }
        }
        
        /// <summary>
        /// Performs a pre-assembly pass on the SourceLine.
        /// </summary>
        /// <param name="line">The SourceLine to do a pass on.</param>
        /// <returns>True, if another pass will be needed due to a change in 
        /// a symbol value resolution, otherwise false.</returns>
        private bool FirstPassLine(SourceLine line)
        {
            bool anotherpass = false;
            if (IsDefiningConstant(line) && !string.IsNullOrEmpty(line.Instruction) && string.IsNullOrEmpty(line.Operand))
            {
                Log.LogEntry(line, Resources.ErrorStrings.InvalidConstantAssignment);
                return false;
            }
            if (line.PC != Output.GetPC() && !
                (Reserved.IsOneOf("Directives", line.Instruction) || 
                 directives_.AssemblesInstruction(line.Instruction)))
            {
                line.PC = Output.GetPC();
                anotherpass = !IsDefiningConstant(line);
            }
            if (string.IsNullOrEmpty(line.Instruction) || line.IsDefinition == true)
            {
                if (IsSymbol(line.Label))
                {
                    string scoped = GetNearestScope(line.Label, line.Scope);
                    if (Labels[scoped].Value != (ushort)line.PC)
                    {
                        Labels[scoped].Value = (ushort)line.PC;
                        Labels[scoped].Size = Convert.ToInt64(line.PC).Size();
                        anotherpass = true;
                    }
                }
                return anotherpass;
            }
            try
            {
                if (line.Label == "*")
                {
                    if (IsDefiningConstant(line) == false)
                    {
                        Log.LogEntry(line);
                        return anotherpass;
                    }
                    Output.SetPC(Convert.ToUInt16(evaluator_.Eval(line.Operand)));
                    line.PC = Output.GetPC();
                    return anotherpass;
                }
                else if (line.Label == "-" || line.Label == "+")
                {
                    if (IsDefiningConstant(line))
                    {
                        Int64 anonymousval;
                        if (string.IsNullOrEmpty(line.Operand))
                            anonymousval = Output.GetPC();
                        else
                            anonymousval = evaluator_.Eval(line.Operand);
                        if (anonymousval > ushort.MaxValue)
                        {
                            Log.LogEntry(line, Resources.ErrorStrings.IllegalQuantity, anonymousval.ToString());
                            return false;
                        }
                        if (line.PC != anonymousval)
                        {
                            line.PC = Convert.ToUInt16(anonymousval);
                            anotherpass = true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else if (IsSymbol(line.Label, true, false))
                {
                    string label = string.Empty;

                    if (line.Label.StartsWith("_"))
                        label = line.Scope.TrimEnd('@') + "." + line.Label;
                    else if (string.IsNullOrEmpty(line.Scope))
                        label = line.Label;
                    else
                        label = line.Scope.TrimEnd('@');

                    Int64 intval = Output.GetPC();
                    if (IsDefiningConstant(line) && string.IsNullOrEmpty(line.Operand) == false)
                    {
                        intval = evaluator_.Eval(line.Operand);
                    }
                    
                    if (Labels[label].Value != Convert.ToUInt16(intval))
                    {
                        Labels[label].Value = Convert.ToUInt16(intval);
                        Labels[label].Size = intval.Size();
                        anotherpass = true;
                    }
                }

                if (directives_.AssemblesInstruction(line.Instruction))
                    anotherpass = directives_.HandleFirstPass(line);
                if (!IsDefiningConstant(line))
                    Output.AddUninitialized(GetInstructionSize(line));
                return anotherpass;
            }
            catch (Compilation.InvalidPCAssignmentException ex)
            {
                Log.LogEntry(line, ex.Message);
                return false;
            }
            catch (ExpressionEvaluator.ExpressionException)
            {
                Log.LogEntry(line, Resources.ErrorStrings.LabelNotDefined, line.Operand);
                return false;
            }
            catch (Exception)
            {
                Log.LogEntry(line, Resources.ErrorStrings.None);
                return false;
            }
        }
   
        /// <summary>
        /// Performs a first (or more) pass of the ProcessedLines to resolve all 
        /// actual symbol values.
        /// </summary>
        private void FirstPass()
        {
            Output.Reset();
            GetAnonymousLabels();
            int passes = 0;
            bool anotherpass = true;
            ProcessedLines = ProcessedLines.Select(l => { l.DoNotAssemble = false; return l;}).ToList();
            
            while (anotherpass && !Log.HasErrors)
            {
                anotherpass = false;
                foreach(var line in ProcessedLines.Where(pl => pl.IsComment == false))
                {
                    if (line.Instruction.Equals(".end", Options.StringComparison))
                        break;
                    evaluator_.SymbolLookupObject = line;
                    anotherpass = FirstPassLine(line);
                }
                Output.Reset();
                passes++;
                if (passes > 4)
                {
                    throw new Exception("Too many passes attempted.");
                }
                // We are now ready to process any labels defined in the command line
                foreach(var label in Labels)
                {
                    if (label.Value.PreDefined)
                    {
                        evaluator_.SymbolLookupObject = null;
                        var val = evaluator_.Eval(label.Value.Expression);
                        if (label.Value.Value != val)
                        {
                            anotherpass = true;
                            label.Value.Value = Convert.ToUInt16(val);
                            label.Value.Size = val.Size();
                        }
                    }
                }
            }
            Console.WriteLine("Passes completed: {0}", passes);
        }

        /// <summary>
        /// Initializes the controller, including processing command-line arguments.
        /// </summary>
        private void Init(string[] args)
        {
            Output.Reset();
            Log.ClearAll();

            Console.WriteLine("6502.Net, A Simple .Net 6502 Cross Assember\n(C) Copyright 2017 Nate Burnett.");
            if (Options.ProcessArgs(args))
            {
                Console.WriteLine("Version {0}.{1} Build {2}",
                    System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major,
                    System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor,
                    System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Build);
            }
            else
            {
                Console.WriteLine("Version {0}.{1}",
                    System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major,
                    System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor);
                Console.WriteLine();
            }

            if (Options.InputFiles.Count == 0)
                return;

            Console.WriteLine("6502.Net comes with ABSOLUTELY NO WARRANTY; see LICENSE!");
            Console.WriteLine();

            Labels = new Dictionary<string, Label>(Options.StringComparar);

            pseudoOps_ = new PseudoOps6502(this);
            lineAssembler_ = new Asm6502(this);
            directives_ = new Directives6502(this);

            Reserved.Comparer = Options.StringComparison;
            evaluator_.IgnoreCase = !Options.CaseSensitive;

            foreach (var label in Options.LabelDefines)
            {
                var def = label.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                if (def.Count() != 2)
                    throw new Exception("Bad argument in label definition '" + label + "'");

                if (Labels.ContainsKey(def.First()))
                    throw new Exception("Re-definition of label '" + def.First() + "'.");

                Labels.Add(def.First(), new Label
                {
                    Value = 0,
                    Size = 1,
                    PreDefined = true,
                    Expression = def.Last()
                });
            }
            List<SourceLine> source = new List<SourceLine>();

            Preprocessor processor = new Preprocessor(this,
                                                      r => IsReserved(r),
                                                      s => IsSymbol(s, true, false));
            processor.FileRegistry = FileRegistry;
            foreach (var file in Options.InputFiles)
            {
                if (processor.FileRegistry.Add(file) == false)
                {
                    throw new Exception("Input file \"" + file + "\" was previously added.");
                }
                source.AddRange(processor.ConvertToSource(file));

                if (Log.HasErrors)
                    break;
            }

            if (Log.HasErrors == false)
            {
                ProcessedLines.AddRange(processor.ExpandMacros(source));

                if (Log.HasErrors == false)
                    processor.DefineScopedSymbols(ProcessedLines);   
            }
            
        }

        #region IAssemblyController.Methods

        /// <summary>
        /// Gets the nearest scope for the given token in its given scope.
        /// </summary>
        /// <param name="token">The line token.</param>
        /// <param name="scope">The line scope.</param>
        /// <returns>Returns the nearest scope for the token.</returns>
        public string GetNearestScope(string label, string linescope)
        {
            linescope = linescope.Replace("@", "");
            List<string> scopes = GetSubScopes(linescope).ToList();

            foreach (var s in scopes)
            {
                string scoped = s + "." + label;
                if (Labels.ContainsKey(scoped.TrimStart('.')))
                {
                    return scoped.TrimStart('.');
                }
            }
            label = label.Replace("@", "");
            return label;
        }

        /// <summary>
        /// Indicates if the instruction in the given source line 
        /// terminates all further assembly.
        /// </summary>
        /// <param name="line">The SourceLine to evaluate.</param>
        /// <returns>True if assembly should end, false otherwise.</returns>
        public bool TerminateAssembly(SourceLine line)
        {
            if (line.DoNotAssemble)
                return false;
            return line.Instruction.Equals(".end", Options.StringComparison);
        }

        /// <summary>
        /// Performs assembly operations based on the command line arguments passed,
        /// including output to an object file and assembly listing.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public void Assemble(string[] args)
        {
            Init(args);

            if (Log.HasErrors == false)
            {
                FirstPass();

                if (Log.HasErrors == false)
                {
                    AssembleToOutput();

                    if (Log.HasErrors == false)
                    {
                        SaveOutput();

                        ToListing();
                    }
                }
            }

            if (Log.HasWarnings)
            {
                Console.WriteLine();
                Log.DumpWarnings();
            }

            if (Log.HasErrors == false)
            {
                Console.WriteLine("\n********************************");
                Console.WriteLine("Assembly start: ${0:X4}", Output.ProgramStart);
                Console.WriteLine("Assembly end:   ${0:X4}", Output.GetPC());
                Console.WriteLine();
            }
            else
            {
                Log.DumpErrors();
            }

            Console.WriteLine("Number of errors: {0}", Log.ErrorCount);
            Console.WriteLine("Number of warnings: {0}", Log.WarningCount);

            if (Log.HasErrors == false)
            {
                Console.WriteLine("*********************************");
                Console.WriteLine("Assembly completed successfully.");
            }
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file registry.
        /// </summary>
        private HashSet<string> FileRegistry { get; set; }

        /// <summary>
        /// Gets or sets the processed list of source lines, including expanded macros and 
        /// commented comment blocks.
        /// </summary>
        private List<SourceLine> ProcessedLines { get; set; }

        /// <summary>
        /// Gets or sets the current text encoding (ASCII, Petscii, Screen).
        /// </summary>
        private TextEncoding Encoding { get; set; }

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

        #region IAssemblyController.Properties

        /// <summary>
        /// Gets the command-line arguments passed by the end-user and parses into 
        /// strongly-typed options.
        /// </summary>
        public AsmCommandLineOptions Options { get; private set; }

        /// <summary>
        /// The Compilation object to handle binary output.
        /// </summary>
        public Compilation Output { get; private set; }

        /// <summary>
        /// The controller's error log to track errors and warnings.
        /// </summary>
        public ErrorLog Log { get; private set; }

        /// <summary>
        /// Gets the labels for the controller.
        /// </summary>
        public IDictionary<string, Label> Labels { get; private set; }

        /// <summary>
        /// Gets the evaluator for the controller.
        /// </summary>
        public IEvaluator Evaluator { get { return evaluator_; } }

        #endregion
        
        #endregion
    }
}
