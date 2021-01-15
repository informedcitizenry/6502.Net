//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

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
            Reserved.DefineType("Assignments", ".equ", ".global", "=");

            Reserved.DefineType("Pseudo",
                ".relocate", ".pseudopc", ".endrelocate", ".realpc");

            Reserved.DefineType("Directives", ".let", ".org");

            Reserved.DefineType("Functions", "len");

            ExcludedInstructionsForLabelDefines.Add(".org");
            ExcludedInstructionsForLabelDefines.Add(".equ");
            ExcludedInstructionsForLabelDefines.Add("=");
            ExcludedInstructionsForLabelDefines.Add(".global");

            Services.Evaluator.AddFunctionEvaluator(this);
        }

        #endregion

        #region Methods

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            var instruction = line.Instruction.Name.ToLower();
            if (Reserved.IsOneOf("Assignments", line.Instruction.Name))
            {
                var isGlobal = line.Instruction.Name.Equals(".global", Services.StringComparison);
                if (line.Label == null)
                {
                    throw new ExpressionException(1, "Invalid assignment expression.");
                }
                if (line.Label.IsSpecialOperator())
                {
                    if (isGlobal || !line.Label.Name.Equals("*"))
                        throw new ExpressionException(line.Label.Position, "Invalid assignment.");
                    Services.Output.SetPC((int)Services.Evaluator.Evaluate(line.Operands.GetIterator(), short.MinValue, ushort.MaxValue));
                }
                else
                {
                    var exists = Services.SymbolManager.SymbolExists(line.Label.Name, false);
                    if (exists && Services.CurrentPass == 0)
                        throw new SymbolException(line.Label, SymbolException.ExceptionReason.Redefined);
                    if (isGlobal)
                    {
                        if (line.Label.Name[0] == '_')
                            throw new SymbolException(line.Label, SymbolException.ExceptionReason.NotValid);
                        if (line.Operands.Count > 0)
                        {
                            var iterator = line.Operands.GetIterator();
                            Services.SymbolManager.DefineGlobal(line.Label.Name,
                                Services.Evaluator.Evaluate(iterator));
                            if (iterator.Current != null)
                                throw new SyntaxException(iterator.Current, "Unexpected expression.");
                        }
                        else
                        {
                            Services.SymbolManager.DefineGlobal(line.Label.Name, Services.Output.LogicalPC);
                        }
                    }
                    else
                    {
                        var iterator = line.Operands.GetIterator();
                        if (!iterator.MoveNext())
                            throw new SyntaxException(line.Label.Position, "Expected expression.");
                        
                        if (iterator.Current.Name.Equals("["))
                        {
                            if (Token.IsEnd(iterator.PeekNext()))
                                throw new SyntaxException(line.Operands[0].Position,
                                    "List cannot be empty.");
                            Services.SymbolManager.DefineSymbol(line.Label.Name, iterator);
                        }
                        else if (StringHelper.ExpressionIsAString(iterator, Services))
                        {
                            Services.SymbolManager.DefineSymbol(line.Label.Name, 
                                                                StringHelper.GetString(iterator, Services));
                        }
                        else
                        {
                            Services.SymbolManager.DefineSymbol(line.Label.Name,
                                Services.Evaluator.Evaluate(iterator, false));
                        }
                        if (iterator.Current != null)
                            throw new SyntaxException(iterator.Current, "Unexpected expression.");
                    }
                }
            }
            else
            {

                var iterator = line.Operands.GetIterator();
                switch (instruction)
                {
                    case ".let":
                        if (!iterator.MoveNext())
                            throw new ExpressionException(line.Instruction.Position, "Expected expression.");
                        Services.SymbolManager.DefineSymbol(iterator);
                        iterator.MoveNext();
                        break;
                    case ".org":
                        if (line.Label != null && line.Label.Name.Equals("*"))
                            Services.Log.LogEntry(line.Label,
                                "Program Counter symbol is redundant for \".org\" directive.", false);
                        Services.Output.SetPC((int)Services.Evaluator.Evaluate(iterator, short.MinValue, ushort.MaxValue));
                        SetLabel(line);
                        break;
                    case ".pseudopc":
                    case ".relocate":
                        Services.Output.SetLogicalPC((int)Services.Evaluator.Evaluate(iterator, short.MinValue, ushort.MaxValue));
                        SetLabel(line);
                        break;
                    case ".endrelocate":
                    case ".realpc":
                        iterator.MoveNext();
                        Services.Output.SynchPC();
                        break;
                }
                if (iterator.Current != null)
                    throw new ExpressionException(iterator.Current, "Unexpected expression.");

            }
            if (Reserved.IsOneOf("Pseudo", line.Instruction.Name))
                return $".{Services.Output.LogicalPC,-8:x4}";
            var unparsedSource = Services.Options.NoSource ? string.Empty : line.Source;
            if ((line.Label == null || !line.Label.Name.Equals("*")) && !Services.PassNeeded)
            {
                if (instruction.Equals(".org"))
                {
                    return string.Format(".{0}{1}",
                        Services.Output.LogicalPC.ToString("x4").PadRight(41),
                        unparsedSource);
                }
                Token tokenSymbol;
                if (instruction.Equals(".let"))
                    tokenSymbol = line.Operands[0];
                else
                    tokenSymbol = line.Label;
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
                            return string.Format("={0}{1}",
                                (symbol.NumericValue == 0 ? "false" : "true").PadRight(42), unparsedSource);
                        if (symbol.NumericValue.IsInteger())
                            return string.Format("=${0}{1}",
                                ((int)symbol.NumericValue).ToString("x").PadRight(41),
                                unparsedSource);
                        return string.Format("={0}{1}",
                                symbol.NumericValue.ToString().PadRight(41),
                                unparsedSource);
                    }
                    var elliptical = $"\"{symbol.StringValue.ToString().Elliptical(38)}\"";
                    return string.Format("={0}{1}", elliptical.PadRight(42), unparsedSource);
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