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
    /// A named scope containing an enumeration list of <see cref="Constant"/>
    /// objects whose values are integral and unique. 
    /// </summary>
    public sealed class Enum : NamedMemberSymbol
    {
        private readonly HashSet<long> _values;

        public Enum(string name, IScope? parent)
            : base(name, parent)
        {
            AutoValue = 0;
            _values = new HashSet<long>();
        }

        /// <summary>
        /// Update the enumeration list member <see cref="Constant"/>.
        /// </summary>
        /// <param name="name">The enumeration constant's name.</param>
        /// <param name="value">The value to update.</param>
        /// <returns><c>true</c> if the value of the member was updated
        /// successfully, <c>false</c> otherwise.</returns>
        public bool UpdateMember(string name, Value value)
        {
            if (!value.IsDefined || value.IsIntegral)
            {
                if (value.IsDefined)
                {
                    AutoValue = value.ToInt() + 1;
                }
                else
                {
                    value = new Value(AutoValue);
                }
                var def = ResolveMember(name) as IValueResolver;
                return def?.Value.SetAs(value) == true && _values.Add(value.ToLong());
            }
            return false;
        }

        /// <summary>
        /// Get whether the enumeration list has <see cref="Constant"/>s with
        /// defined values.
        /// </summary>
        public bool HasDefinitions => _values.Count > 0;

        /// <summary>
        /// Get the automatic value of the next enumeration constant definition.
        /// </summary>
        public int AutoValue { get; private set; }
    }
}
