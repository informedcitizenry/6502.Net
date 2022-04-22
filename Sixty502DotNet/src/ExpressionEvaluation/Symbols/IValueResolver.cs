//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents an interface to a named symbol that can resolve to a
    /// <see cref="IValue"/>.
    /// </summary>
    public interface IValueResolver
    {
        /// <summary>
        /// Get or set the resolver's value.
        /// </summary>
        Value Value { get; set; }

        /// <summary>
        /// Get or set the resolver's symbol name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get if the resolver represents a constant value.
        /// </summary>
        bool IsConst { get; }

        /// <summary>
        /// Get or set the property that tells the assembler this
        /// <see cref="IValueResolver"/> is a reference to another one.
        /// </summary>
        IValueResolver? IsAReferenceTo { get; set; }

        string ToString() => $"{Name}: {Value}";
    }
}
