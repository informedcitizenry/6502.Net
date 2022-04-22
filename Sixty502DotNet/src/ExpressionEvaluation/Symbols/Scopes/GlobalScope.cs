//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A concrete implementation of a <see cref="IScope"/> that respresents a
    /// global unnamed scope.
    /// </summary>
    public sealed class GlobalScope : BaseScope
    {
        /// <summary>
        /// Construct a new instance of the <see cref="GlobalScope"/> class.
        /// </summary>
        /// <param name="caseSensitive">The scope's case-sensitivity flag.</param>
        public GlobalScope(bool caseSensitive)
            : base(null, caseSensitive)
        {
            Name = "";
        }
    }
}
