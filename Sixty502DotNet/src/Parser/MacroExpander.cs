//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Sixty502DotNet
{
    /// <summary>
    /// A visitor class for parsed macro expansions whose methods return a
    /// string. This class is responsible for all text substitutions from
    /// passed parameters.
    /// </summary>
    public class MacroExpander : PreprocessorParserBaseVisitor<string>
    {
        private readonly IDictionary<string, string> _passedArgs;

        private static readonly Regex s_string =
            new(@"""{1,3}(?:\\.|[^\\""])+""{1,3}", RegexOptions.Compiled);

        private static readonly Regex s_stringInterpolation =
            new(@"@\{(\p{L}(\p{L}|[0-9_])*)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly AssemblyServices _services;

        /// <summary>
        /// Construct a new instance of the <see cref="MacroExpander"/> class.
        /// </summary>
        /// <param name="passedArgs">The passed arguments to the macro invocation.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public MacroExpander(IDictionary<string, string> passedArgs, AssemblyServices services)
            => (_passedArgs, _services) = (passedArgs, services);


        public override string VisitMacroBody([NotNull] PreprocessorParser.MacroBodyContext context)
        {
            var sb = new StringBuilder();
            foreach (var element in context.macroBodyElement())
            {
                sb.Append(Visit(element));
            }
            return sb.ToString();
        }

        public override string VisitMacroBodyElement([NotNull] PreprocessorParser.MacroBodyElementContext context)
        {
            if (context.MacroBlockParamString() != null)
            {
                var subs = s_stringInterpolation.Replace(context.MacroBlockParamString().GetText(), m =>
                {
                    var subsText = m.Groups[1].Value;
                    if (_passedArgs.TryGetValue(subsText, out var substitution) &&
                        s_string.IsMatch(substitution))
                    {
                        return substitution.Trim().TrimOnce('"');
                    }
                    _services.Log.LogEntry(context, context.MacroBlockParamString().Symbol,
                        $"Invalid substitution '{subsText}'.");
                    return subsText;
                });
                return subs;
            }
            if (context.MacroSubstitution() != null)
            {
                var subsText = context.MacroSubstitution().Symbol.Text.TrimStartOnce('\\');
                if (_passedArgs.TryGetValue(subsText, out var substitution))
                {
                    return substitution.Trim();
                }
                _services.Log.LogEntry(context, context.MacroSubstitution().Symbol, "Could not resolve substitution.");
                return string.Empty;
            }
            return context.GetText();
        }
    }
}
