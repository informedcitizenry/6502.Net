//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .for/.next code blocks.
    /// </summary>
    public sealed class ForNextBlock : BlockProcessorBase
    {
        #region Members

        List<Token> _condition;
        List<Token> _iterations;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the for next block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public ForNextBlock(AssemblyServices services, int index)
            : base(services, index) => Reserved.DefineType("Directives", ".for", ".next");

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            if (lines.Current.Instruction.Name.Equals(".for", Services.StringComparison))
            {
                var line = lines.Current;
                if (line.Operands.Count == 0)
                    throw new SyntaxException(line.Instruction, "Missing operands for \".for\" directive.");
                var it = line.Operands.GetIterator();
                if (!Token.IsEnd(it.GetNext()))
                    Services.SymbolManager.DefineSymbol(it);

                if (!it.MoveNext())
                    throw new SyntaxException(line.Operands[^1], "Missing condition clause.");
                _condition = new List<Token>();
                while (!Token.IsEnd(it.Current))
                {
                    if (it.Current.Name.Equals("("))
                    {
                        _condition.AddRange(Token.GetGroup(it));
                    }
                    else
                    {
                        _condition.Add(it.Current);
                        it.MoveNext();
                    }
                }
                if (_condition.Count > 0 && !Services.Evaluator.EvaluateCondition(_condition.GetIterator()))
                {
                    SeekBlockEnd(lines);
                }
                else
                {
                    if (it.Current == null)
                        throw new SyntaxException(line.Operands[^1], "Expression expected.");
                    _iterations = new List<Token>();
                    while (it.GetNext() != null)
                        _iterations.Add(it.Current);
                    if (_iterations.Count == 0 || _iterations[^1].IsSeparator())
                        throw new SyntaxException(line.Operands[^1], "Expression expected.");
                }
            }
            else
            {
                var it = _iterations.GetIterator();
                while (it.GetNext() != null)
                {
                    Services.SymbolManager.DefineSymbol(it);
                    if (it.Current == null)
                        break;
                    if (it.Current.IsSeparator())
                        continue;
                }
                if (_condition.Count > 0 && Services.Evaluator.EvaluateCondition(_condition.GetIterator()))
                    lines.Rewind(Index);
            }
        }
        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        public override IEnumerable<string> BlockOpens => new string[] { ".for" };

        public override string BlockClosure => ".next";

        #endregion
    }

    /// <summary>
    /// A class responsible for processing .foreach/.next code blocks.
    /// </summary>
    public class ForEachBlock : BlockProcessorBase
    {
        #region Members

        Symbol _enumerable;
        StringView _enumeration;
        int _enumerator;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the foreach/next block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public ForEachBlock(AssemblyServices services, int index)
            : base(services, index)
        {
            Reserved.DefineType("Directives", ".foreach", ".next");
            _enumerator = 0;
        }

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            if (lines.Current.Instruction.Name.Equals(".foreach", Services.StringComparison))
            {
                var line = lines.Current;
                var it = line.Operands.GetIterator();
                if (Token.IsEnd(it.GetNext()))
                    throw new SyntaxException(line.Instruction, "Expression expected.");
                _enumeration = it.Current.Name;
                if (!it.MoveNext())
                    throw new SyntaxException(line.Operands[0], "Expression expected.");
                if (!Token.IsEnd(it.Current))
                    throw new SyntaxException(it.Current, "Unexpected expression.");
                if (!it.MoveNext())
                    throw new SyntaxException(line.Operands[^1], "Expression expected.");
                _enumerable = Services.SymbolManager.GetSymbol(it.Current, Services.CurrentPass > 0);
                if (it.MoveNext())
                    throw new SyntaxException(it.Current, "Unexpected expression.");
                if (!(_enumerable.StorageType == StorageType.Vector || _enumerable.DataType == DataType.String))
                    throw new SyntaxException(line.Operands[^1], "Symbol must be a list or string.");
                _enumerator = 0;
            }
            var doNext = false;
            if (_enumerable.DataType == DataType.String)
            {
                if (_enumerable.StorageType == StorageType.Vector && _enumerator < _enumerable.StringVector.Count)
                {
                    Services.SymbolManager.DefineSymbol(_enumeration, _enumerable.StringVector[_enumerator++], true);
                    doNext = true;
                }
                else if (_enumerable.StringValue != null && _enumerator < _enumerable.StringValue.Length)
                {
                    Services.SymbolManager.DefineSymbol(_enumeration, _enumerable.StringValue.Substring(_enumerator++, 1), true);
                    doNext = true;
                }
            }
            else if (_enumerator < _enumerable.NumericVector.Count)
            {
                Services.SymbolManager.DefineSymbol(_enumeration, _enumerable.NumericVector[_enumerator++], true);
                doNext = true;
            }
            if (lines.Current.Instruction.Name.Equals(".next", Services.StringComparison) && doNext)
                lines.Rewind(Index);
        }
        #endregion

        #region Properties

        public override IEnumerable<string> BlockOpens => new string[] { ".foreach" };

        public override string BlockClosure => ".next";

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        #endregion
    }
}
