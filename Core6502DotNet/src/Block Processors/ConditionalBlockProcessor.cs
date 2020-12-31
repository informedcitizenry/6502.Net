//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class reponsible for processing .if/.endif conditional blocks of code.
    /// </summary>
    public sealed class ConditionalBlock : BlockProcessorBase
    {
        #region Members

        bool _elseEvaluated;
        bool _ifTrue;

        readonly HashSet<string> _opens;

        readonly Dictionary<(string, int), bool> _ifDefEvaluations;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a conditional block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public ConditionalBlock(AssemblyServices services, int index)
            : base(services, index)
        {
            _opens = new HashSet<string>(services.StringComparer)
            {
                ".if", ".ifdef", ".ifndef"
            };
            Reserved.DefineType("Keywords",
                ".if", ".ifdef", ".ifndef", ".else", ".elseif",
                ".elseif", ".elseifdef", ".elseifndef", ".endif");

            Reserved.DefineType("Defs",
                ".ifdef", ".ifndef");

            _ifDefEvaluations = new Dictionary<(string, int), bool>();
            _ifTrue =
            _elseEvaluated = false;
        }

        #endregion

        #region Methods

        void EvaluateCondition(SourceLine line)
        {
            if (Reserved.IsOneOf("Defs", line.Instruction.Name))
            {
                if (line.Operands.Count != 1)
                    throw new SyntaxException(line.Instruction.Position, "Expected expression");

                if (!_ifDefEvaluations.TryGetValue((line.Filename, line.LineNumber), out _ifTrue))
                {
                    if (line.Instruction.Name.ToString().EndsWith("ndef", Services.StringComparison))
                        _ifTrue = !Services.SymbolManager.SymbolExists(line.Operands[0].Name);
                    else
                        _ifTrue = Services.SymbolManager.SymbolExists(line.Operands[0].Name);
                    _ifDefEvaluations.Add((line.Filename, line.LineNumber), _ifTrue);
                }
            }
            else
            {
                var iterator = line.Operands.GetIterator();
                _ifTrue = Services.Evaluator.EvaluateCondition(iterator);
                if (iterator.Current != null)
                    throw new SyntaxException(iterator.Current, "Unexpected expression.");
            }
        }

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> iterator)
        {
            var line = iterator.Current;
            if (_ifTrue)
            {
                SeekBlockEnd(iterator);
            }
            else
            {
                while (line != null && !line.Instruction.Name.Equals(".endif", Services.StringComparison) && !_ifTrue)
                {
                    if (iterator.Current.Instruction == null)
                        continue;
                    var instruction = Services.Options.CaseSensitive ? line.Instruction.Name.ToString() :
                                                                       line.Instruction.Name.ToLower();
                    if (instruction.StartsWith(".if"))
                    {
                        EvaluateCondition(line);
                    }
                    else
                    {
                        if (!instruction.Equals(".endif"))
                        {
                            if (_elseEvaluated)
                                throw new SyntaxException(iterator.Current.Instruction, 
                                    $"Invalid use of \"{instruction}\" directive.");

                            _elseEvaluated = instruction.Equals(".else");

                            if (_elseEvaluated)
                            {
                                if (line.Operands.Count > 0)
                                    throw new SyntaxException(line.Operands[0], "Unexpected expression.");
                                break;
                            }
                            EvaluateCondition(line);
                        }
                    }
                    if (_ifTrue)
                        break;
                    SeekBlockDirectives(iterator, Reserved.GetReserved("Keywords").ToArray());
                    line = iterator.Current;
                }
                if (line == null)
                    throw new SyntaxException(line.Instruction, $"Missing \".endif\" directive.");
            }
        }

        #endregion

        #region Properties

        public override bool WrapInScope => true;

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        public override IEnumerable<string> BlockOpens => _opens;

        public override string BlockClosure => ".endif";

        #endregion
    }
}
