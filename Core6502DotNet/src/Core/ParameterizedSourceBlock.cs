//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
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
        public class Param
        {
            public Param(string name) => Name = name;

            public Param() => Name = DefaultValue = string.Empty;

            public string Name { get; set; }

            public string DefaultValue { get; set; }
        }

        #endregion

        #region Members

        protected StringComparison _stringCompare;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a <see cref="ParameterizedSourceBlock"/>.
        /// </summary>
        /// <param name="parms">The parameters the definition expects when invoked.</param>
        /// <param name="source">The associated source string.</param>
        protected ParameterizedSourceBlock(Token parms, string source, StringComparison stringComparison)
        {
            Params = new List<Param>();
            _stringCompare = stringComparison;

            if (parms != null && parms.HasChildren)
            {
                for (var i = 0; i < parms.Children.Count; i++)
                {
                    Token parameter = parms.Children[i];
                    if (parameter.Children.Count > 0)
                    {
                        Token definedParm = parameter.Children[0];
                        if (Params.Any(p => p.Name.Equals(definedParm.UnparsedName, _stringCompare)))
                        {
                            throw new ExpressionException(definedParm.Position,
                                $"Parameter \"{definedParm.Name}\" previously defined in parameter list.");
                        }

                        if (string.IsNullOrEmpty(definedParm.UnparsedName))
                            Params.Add(new Param());
                        else if (!Assembler.SymbolManager.SymbolIsValid(definedParm.UnparsedName))
                            throw new ExpressionException(definedParm.Position, $"Invalid parameter name \"{definedParm.UnparsedName}\".");


                        var parm = new Param(definedParm.UnparsedName);
                        if (parameter.Children.Count > 1)
                        {
                            Token assign = parameter.Children[1];
                            if (!assign.Name.Equals("=") || parameter.Children.Count < 3)
                                throw new ExpressionException(assign.Position, "Syntax error.");
                            var defaultIx = parameter.Children[2].Position - 1;
                            if (i < parms.Children.Count - 1)
                            {
                                var len = parms.Children[i + 1].Position - 1 - defaultIx;
                                parm.DefaultValue = source.Substring(defaultIx, len);
                            }
                            else
                            {
                                parm.DefaultValue = source.Substring(defaultIx);
                            }
                        }
                        Params.Add(parm);
                    }
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The list of parameters associated to the definition,
        /// encapsulated in a <see cref="Param"/> class.
        /// </summary>
        public List<Param> Params { get; set; }

        #endregion

    }
}
