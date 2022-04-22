//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that implements an anonymous scope.
    /// </summary>
    public sealed class AnonymousScope : NamedMemberSymbol
    {
        /// <summary>
        /// Construct a new instance of the <see cref="AnonymousScope"/> class.
        /// </summary>
        /// <param name="context">The rule context in which the scope belongs.
        /// This is used to track the scope's identification.</param>
        /// <param name="parent">The parent scope.</param>
        public AnonymousScope(ParserRuleContext context, IScope? parent)
            : base(GetName(context), parent) { }

        private static string GetName(ParserRuleContext context)
            => $"{context.Start.TokenSource.InputStream.SourceName}:{context.Start.Line}";
    }
}
