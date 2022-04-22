//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// A comparer for m6809 register names.
    /// </summary>
    public class M6809PushPullComparer : IComparer<string>
    {
        readonly IDictionary<string, byte>? _lookup;

        /// <summary>
        /// Constructs a new instance of the <see cref="M6809PushPullComparer"/>
        /// class.
        /// </summary>
        /// <param name="lookup">The lookup dictionary.</param>
        public M6809PushPullComparer(IDictionary<string, byte>? lookup)
            => _lookup = lookup;

        /// <summary>
        /// Compare two register names.
        /// </summary>
        /// <param name="reg1">The first register name to compare.</param>
        /// <param name="reg2">The second register name to compare.</param>
        /// <returns>The result of the
        /// <see cref="string.CompareTo(string?)"/> method for the
        /// given register names.</returns>
        public int Compare(string? reg1, string? reg2) =>
            _lookup == null ? 1 :
            reg1 == null ? 1 :
            reg2 == null ? -1 :
            _lookup[reg1].CompareTo(_lookup[reg2]);
    }
}
