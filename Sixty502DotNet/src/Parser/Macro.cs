//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class containing a macro definition, including its defined parameters,
    /// if any.
    /// </summary>
    public class Macro
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Macro"/> class.
        /// </summary>
        /// <param name="definitionToken">The macro definition token.</param>
        /// <param name="macroArgs">The parsed macro arguments.</param>
        /// <param name="definition">The parsed macro definition block.</param>
        public Macro(Token definitionToken,
                     PreprocessorParser.MacroArgListContext macroArgs,
                     PreprocessorParser.MacroBlockContext definition)
        {
            Name = definitionToken.Text;
            DefinitionToken = definitionToken;
            Args = new List<FunctionArg>();
            if (macroArgs != null)
            {
                var args = macroArgs.macroArg();
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    var argName = arg.MacroArg().GetText();
                    if (Args.Any(a => a.Name.Equals(argName)))
                    {
                        throw new Error(arg, "Macro argument previously specified.");
                    }
                    if (arg.macroArgDefaultAssignExpr() != null)
                    {
                        var defaultExpr = arg.macroArgDefaultAssignExpr().macroArgDefaultExpr();
                        var defaultVal = new Value($"\"{defaultExpr.GetText()}\"");
                        Args.Add(new FunctionArg(arg.MacroArg().GetText(), defaultVal));
                    }
                    else
                    {
                        Args.Add(new FunctionArg(arg.MacroArg().GetText()));
                    }
                }
            }
            Definition = definition.macroBody();
        }

        /// <summary>
        /// Get and match the arguments from the parsed invocation arguments.
        /// </summary>
        /// <param name="context">The parsed macro invocation arguments.</param>
        /// <param name="substitutions">The substitutions.</param>
        /// <returns>The full argument list, including default argument
        /// values, if none are passed in the invocation argument list.
        /// </returns>
        public IDictionary<string, string> GetArgList(PreprocessorParser.MacroInvocationArgListContext context,
            IDictionary<string, string> substitutions)
        {
            var argList = new Dictionary<string, string>();
            int argIx = 1;
            foreach (var arg in Args)
            {
                if (arg.DefaultValue.IsDefined)
                {
                    argList[$"{argIx}"] =
                    argList[arg.Name] = arg.DefaultValue.ToString(true);
                }
                argIx++;
            }
            if (context != null)
            {
                argIx = 0;
                foreach (var arg in context.macroInvocationArg())
                {
                    var argStr = new StringBuilder();
                    foreach (var element in arg.macroInvokeArgElement())
                    {
                        if (element.MacroInvokeSubstitution() != null)
                        {
                            if (substitutions.TryGetValue(element.MacroInvokeSubstitution().GetText(), out var substitution))
                            {
                                argStr.Append(substitution);
                            }
                            else
                            {
                                throw new Error(element, "Substitution not defined.");
                            }
                        }
                        else
                        {
                            argStr.Append(element.GetText());
                        }
                    }
                    var argName = $"{argIx + 1}";
                    argList[argName] = argStr.ToString();
                    if (Args.Count > argIx)
                    {
                        argName = Args[argIx].Name;
                        argList[argName] = argStr.ToString();
                    }
                    argIx++;
                }
            }
            return argList;
        }

        /// <summary>
        /// Get the parsed macro definition block.
        /// </summary>
        public PreprocessorParser.MacroBodyContext Definition { get; init; }

        /// <summary>
        /// Get the macro name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Get the macro definition token.
        /// </summary>
        public Token DefinitionToken { get; init; }

        /// <summary>
        /// Get the macro definition's expected arguments.
        /// </summary>
        public IList<FunctionArg> Args { get; init; }

        /// <summary>
        /// Get or set the flag indicating the macro is referenced elsewhere
        /// in source code.
        /// </summary>
        public bool IsReferenced { get; set; }
    }
}
