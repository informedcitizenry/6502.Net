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
    /// A bass class for an abstraction of grouped or related source
    /// lines that expects parameters when invoked in code. This class must be inherited.
    /// </summary>
    public abstract class ParameterizedSourceBlock
    {
        #region Subclasses

        /// <summary>
        /// Represents a parameter, including a symbol name and a default value.
        /// </summary>
        public readonly struct Param
        {
            /// <summary>
            /// Constructs a new parameter.
            /// </summary>
            /// <param name="name">The parameter name.</param>
            /// <param name="defaultValue">The parameter's default value as a collection of
            /// parsed <see cref="Token"/>s.</param>
            public Param(StringView name, List<Token> defaultValue)
                => (Name, DefaultValue) = (name, defaultValue);

            /// <summary>
            /// Gets the parameter's name.
            /// </summary>
            public StringView Name { get; }

            /// <summary>
            /// Gets the parameter's default value.
            /// </summary>
            public List<Token> DefaultValue { get; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a <see cref="ParameterizedSourceBlock"/>.
        /// </summary>
        /// <param name="parms">The parameters the definition expects when invoked.</param>
        /// <param name="caseSensitive">Determines whether to compare the passed parameters
        /// to the source block's own defined parameters should be case-sensitive.</param>
        protected ParameterizedSourceBlock(List<Token> parms, bool caseSensitive)
        {
            CaseSensitive = caseSensitive;
            var comp = caseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;
            Params = new List<Param>();
            if (parms.Count != 0)
            {
                var it = parms.GetIterator();
                Token t;
                while ((t = it.GetNext()) != null)
                {
                    if (t.Type != TokenType.Separator)
                    {
                        if (Params.Any(p => p.Name.Equals(t.Name, comp)))
                        {
                            throw new ExpressionException(t.Position,
                                $"Parameter \"{t.Name}\" previously defined in parameter list.");

                        }
                        var parmName = t.Name;
                        if (!char.IsLetter(parmName[0]))
                            throw new ExpressionException(t.Position, "Invalid parameter name.");
                        var defaultValue = new List<Token>();
                        if (it.PeekNext() != null && it.PeekNext().Type != TokenType.Separator)
                        {

                            t = it.GetNext();
                            if (!t.Name.Equals("=") || it.PeekNext() == null || TokenType.End.HasFlag(it.PeekNext().Type))
                                throw new ExpressionException(t.Position, "Syntax error.");
                            while ((t = it.GetNext()) != null && t.Type != TokenType.Separator)
                                defaultValue.Add(t);
                        }
                        Params.Add(new Param(parmName, defaultValue));
                    }
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of parameters associated to the definition,
        /// encapsulated in a <see cref="Param"/> class.
        /// </summary>
        protected List<Param> Params { get; }

        /// <summary>
        /// Gets the case-sensitivity of the parameter name comparison.
        /// </summary>
        protected bool CaseSensitive { get; }

        #endregion
    }
}
