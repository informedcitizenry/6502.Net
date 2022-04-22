//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A named scope with member symbols that can be re-used throughout
    /// assembly.
    /// </summary>
    public sealed class Namespace : NamedMemberSymbol
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Namespace"/> class.
        /// </summary>
        /// <param name="name">The namespace name.</param>
        /// <param name="parent">The namespace parent scope.</param>
        public Namespace(string name, IScope parent)
            : base(name, parent) => IsNested = false;

        /// <summary>
        /// Get or set if the namespace has a nested namespace member within it.
        /// </summary>
        public bool IsNested { get; set; }
    }
}
