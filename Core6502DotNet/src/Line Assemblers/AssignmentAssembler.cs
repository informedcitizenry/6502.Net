//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing assignment directives in assembly listing.
    /// </summary>
    public sealed class AssignmentAssembler : AssemblerBase, IFunctionEvaluator
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the assignment assembler class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public AssignmentAssembler(AssemblyServices services)
            : base(services)
        {
            Reserved.DefineType("Assignments", 
                ".equ", ".global", "=");

            Reserved.DefineType("VarAssignments",
                ":=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=");

            Reserved.DefineType("Pseudo",
                ".relocate", ".pseudopc", ".endrelocate", ".realpc");

            Reserved.DefineType("Directives", ".let", ".org");

            Reserved.DefineType("Functions", "len");

            ExcludedInstructionsForLabelDefines.Add(".org");
            ExcludedInstructionsForLabelDefines.Add(".equ");
            ExcludedInstructionsForLabelDefines.Add(".global");
            ExcludedInstructionsForLabelDefines.Add("=");
            ExcludedInstructionsForLabelDefines.Add(":=");
            ExcludedInstructionsForLabelDefines.Add("+=");
            ExcludedInstructionsForLabelDefines.Add("-=");
            ExcludedInstructionsForLabelDefines.Add("*=");
            ExcludedInstructionsForLabelDefines.Add("/=");
            ExcludedInstructionsForLabelDefines.Add("%=");
            ExcludedInstructionsForLabelDefines.Add("&=");
            ExcludedInstructionsForLabelDefines.Add("|=");
            ExcludedInstructionsForLabelDefines.Add("^=");
            ExcludedInstructionsForLabelDefines.Add("<<=");
            ExcludedInstructionsForLabelDefines.Add(">>=");

            Services.Evaluator.AddFunctionEvaluator(this);
        }

        #endregion

        #region Methods

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            var instruction = line.Instruction.Name.ToLower();
            RandomAccessIterator<Token> iterator = null;
            if (Reserved.IsOneOf("Assignments", line.Instruction.Name) ||
                Reserved.IsOneOf("VarAssignments", line.Instruction.Name))
            {
                var isGlobal = line.Instruction.Name.Equals(".global", Services.StringComparison);
                if (line.Label == null)
                {
                    throw new ExpressionException(1, "Invalid assignment expression.");
                }
                if (line.Label.IsSpecialOperator())
                {
                    if (isGlobal || !line.Label.Name.Equals("*") || Reserved.IsOneOf("VarAssignments", line.Instruction.Name))
                        throw new ExpressionException(line.Label.Position, "Invalid assignment.");
                    Services.Output.SetPC((int)Services.Evaluator.Evaluate(line.Operands.GetIterator(), short.MinValue, ushort.MaxValue));
                }
                else if (Reserved.IsOneOf("VarAssignments", line.Instruction.Name))
                {
                    var expressionList = new List<Token>
                    {
                        line.Label,
                        line.Instruction
                    };
                    expressionList.AddRange(line.Operands);
                    iterator = expressionList.GetIterator();
                    iterator.MoveNext();
                    Services.SymbolManager.DefineSymbol(iterator);
                }
                else
                {
                    var exists = Services.SymbolManager.SymbolExists(line.Label.Name, false);
                    if (exists && Services.CurrentPass == 0)
                        throw new SymbolException(line.Label, SymbolException.ExceptionReason.Redefined);
                    if (isGlobal && (line.Label.Name[0] == '_' || line.Operands.Count == 0))
                    {
                        if (line.Label.Name[0] == '_')
                            throw new SymbolException(line.Label, SymbolException.ExceptionReason.NotValid);
                        Services.SymbolManager.DefineGlobal(line.Label.Name, Services.Output.LogicalPC);
                    }
                    else
                    {
                        iterator = line.Operands.GetIterator();
                        if (!iterator.MoveNext())
                            throw new SyntaxException(line.Label.Position, "Expected expression.");
                        
                        if (iterator.Current.Name.Equals("["))
                            Services.SymbolManager.DefineSymbol(line.Label.Name, iterator, false, isGlobal);
                        else if (StringHelper.ExpressionIsAString(iterator, Services))
                            Services.SymbolManager.DefineSymbol(line.Label, 
                                                                StringHelper.GetString(iterator, Services), false, isGlobal);
                        else
                            Services.SymbolManager.DefineSymbol(line.Label,
                                Services.Evaluator.Evaluate(iterator, false), 0, false, isGlobal);
                    }
                }
            }
            else
            {
                iterator = line.Operands.GetIterator();
                switch (instruction)
                {
                    case ".let":
                        if (!iterator.MoveNext())
                            throw new ExpressionException(line.Instruction.Position, "Expected expression.");
                        if (iterator.Current.Name.Equals("*"))
                        {
                            Services.Log.LogEntry(line.Instruction, "Using Program Counter symbol '*' as a variable.", false);
                            if (!iterator.MoveNext() || !iterator.Current.Name.Equals("="))
                                return Services.Log.LogEntry<string>(iterator.Current ?? line.Operands[0], "Assignment operator expected.");
                            Services.Output.SetPC((int)Services.Evaluator.Evaluate(iterator, short.MinValue, ushort.MaxValue));
                        }
                        else
                        {
                            Services.SymbolManager.DefineSymbol(iterator);
                        }
                        break;
                    case ".org":
                        if (line.Label != null && line.Label.Name.Equals("*"))
                            Services.Log.LogEntry(line.Label,
                                "Program Counter symbol is redundant for \".org\" directive.", false);
                        Services.Output.SetPC((int)Services.Evaluator.Evaluate(iterator, short.MinValue, ushort.MaxValue));
                        break;
                    case ".pseudopc":
                    case ".relocate":
                        Services.Output.SetLogicalPC((int)Services.Evaluator.Evaluate(iterator, short.MinValue, ushort.MaxValue));
                        break;
                    case ".endrelocate":
                    case ".realpc":
                        iterator.MoveNext();
                        Services.Output.SynchPC();
                        break;
                }
                SetLabel(line);
            }
            if (iterator?.Current != null)
                throw new SyntaxException(iterator.Current, "Unexpected expression.");
            if (Reserved.IsOneOf("Pseudo", line.Instruction.Name))
                return $".{Services.Output.LogicalPC,-8:x4}";
            var unparsedSource = Services.Options.NoSource ? string.Empty : line.Source;
            if ((line.Label == null || !line.Label.Name.Equals("*")) && !Services.PassNeeded)
            {
                if (instruction.Equals(".org"))
                {
                    return $".{Services.Output.LogicalPC,-41:x4}{unparsedSource}";
                }
                var tokenSymbol = instruction.Equals(".let") ? line.Operands[0] : line.Label;
                var symbol = Services.SymbolManager.GetSymbol(tokenSymbol, false);
                if (symbol != null && symbol.StorageType == StorageType.Scalar)
                {
                    if (symbol.IsNumeric)
                    {
                        bool condition;
                        if (instruction.Equals(".let"))
                            condition = Services.Evaluator.ExpressionIsCondition(line.Operands.Skip(2).GetIterator());
                        else
                            condition = Services.Evaluator.ExpressionIsCondition(line.Operands.GetIterator());
                        if (condition)
                            return $"={(symbol.NumericValue == 0 ? "false" : "true"),-42}{unparsedSource}";
                        if (symbol.NumericValue.IsInteger())
                            return $"=${(int)symbol.NumericValue,-41:x}{unparsedSource}";
                        return $"={symbol.NumericValue,-41}{unparsedSource}";
                    }
                    var elliptical = $"\"{symbol.StringValue.ToString().Elliptical(38)}\"";
                    return $"={elliptical,-42}{unparsedSource}";
                }
            }
            return string.Empty;
        }

        void SetLabel(SourceLine line)
        {
            if (line.Label != null && !line.Label.Name.Equals("*"))
            {
                if (line.Label.IsSpecialOperator())
                    Services.SymbolManager.DefineLineReference(line.Label, Services.Output.LogicalPC);
                else
                    DefineLabel(line.Label, Services.Output.LogicalPC, true);
            }
        }

        public override bool AssemblesLine(SourceLine line)
            => line.Instruction != null &&
                (Reserved.IsOneOf("Assignments", line.Instruction.Name) ||
                 Reserved.IsOneOf("VarAssignments", line.Instruction.Name) ||
                 Reserved.IsOneOf("Pseudo", line.Instruction.Name) ||
                 Reserved.IsOneOf("Directives", line.Instruction.Name));

        public bool EvaluatesFunction(Token function) => Reserved.IsOneOf("Functions", function.Name);

        public double EvaluateFunction(RandomAccessIterator<Token> tokens)
        {
            tokens.MoveNext();
            var param = tokens.GetNext();
            if (param.Equals(")"))
                throw new SyntaxException(param.Position, "Expected argument not provided.");
            var symbolLookup = Services.SymbolManager.GetSymbol(param, false);
            if (symbolLookup == null)
            {
                if (param.Type != TokenType.Operand || !char.IsLetter(param.Name[0]) || param.Name[0] != '_')
                    throw new SyntaxException(param.Position, "Function \"len\" expects a symbol.");
                if (Services.CurrentPass > 0)
                    throw new SymbolException(param, SymbolException.ExceptionReason.NotDefined);
                Services.PassNeeded = true;
                return 0;
            }
            param = tokens.GetNext();
            if (!param.Name.Equals(")"))
            {
                param = tokens.GetNext();
                int subscript = -1;
                if (param.Name.Equals("["))
                    subscript = (int)Services.Evaluator.Evaluate(tokens, 0, int.MaxValue);
                if (subscript < 0 || !tokens.PeekNext().Equals(")"))
                    throw new SyntaxException(param.Position, "Unexpected argument.");
                if (symbolLookup.StorageType != StorageType.Vector)
                    throw new SyntaxException(param.Position, "Type mismatch.");
                if (symbolLookup.DataType == DataType.String)
                {
                    if (subscript >= symbolLookup.StringVector.Count)
                        throw new SyntaxException(param.Position, "Index out of range.");
                    return symbolLookup.StringVector[subscript].Length;
                }
                if (subscript >= symbolLookup.NumericVector.Count)
                    throw new SyntaxException(param.Position, "Index out of range.");
                return symbolLookup.NumericVector[subscript].Size();
            }
            return symbolLookup.Length;
        }

        public void InvokeFunction(RandomAccessIterator<Token> tokens) => EvaluateFunction(tokens);

        public bool IsFunctionName(StringView symbol) => Reserved.IsOneOf("Functions", symbol);

        #endregion
    }
}