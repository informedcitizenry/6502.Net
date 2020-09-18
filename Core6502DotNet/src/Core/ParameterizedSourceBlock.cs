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
    /// A bass class for an abstraction of grouped or related source
    /// lines that expects parameters when invoked in code. This class must be inherited.
    /// </summary>
    public abstract class ParameterizedSourceBlock : Core6502Base
    {
        #region Subclasses

        /// <summary>
        /// Represents a parameter, including a symbol name and a default value.
        /// </summary>
        public readonly struct Param
        {
            public Param(string name, string defaultValue) 
                => (Name, DefaultValue) = (name, defaultValue);

            public string Name { get; }

            public string DefaultValue { get; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a <see cref="ParameterizedSourceBlock"/>.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="parms">The parameters the definition expects when invoked.</param>
        /// <param name="source">The associated source string.</param>
        protected ParameterizedSourceBlock(AssemblyServices services,
                                           Token parms, 
                                           string source)
            :base(services)
        {
            Params = new List<Param>();

            if (parms != null)
            {
                for (var i = 0; i < parms.Children.Count; i++)
                {
                    var parameter = parms.Children[i];
                    if (parameter.Children.Count > 0)
                    {
                        var definedParm = parameter.Children[0];
                        if (Params.Any(p => p.Name.Equals(definedParm.Name, Services.StringComparison)))
                        {
                            throw new ExpressionException(definedParm.Position,
                                $"Parameter \"{definedParm.Name}\" previously defined in parameter list.");
                        }
                        var parmName = definedParm.UnparsedName.Trim();
                        if (string.IsNullOrEmpty(parmName))
                            Params.Add(new Param());
                        else if (!Services.SymbolManager.SymbolIsValid(parmName))
                            throw new SyntaxException(definedParm.Position, $"Invalid parameter name \"{parmName}\".");

                        var defaultValue = string.Empty;
                        if (parameter.Children.Count > 1)
                        {
                            var assign = parameter.Children[1];
                            if (!assign.Name.Equals("=") || parameter.Children.Count < 3)
                                throw new SyntaxException(assign.Position, "Syntax error.");
                            var defaultIx = parameter.Children[2].Position - 1;
                            if (i < parms.Children.Count - 1)
                            {
                                var len = parms.Children[i + 1].Position - 1 - defaultIx;
                                defaultValue = source.Substring(defaultIx, len);
                            }
                            else
                            {
                                defaultValue = source.Substring(defaultIx);
                            }
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

        #endregion

    }
}
