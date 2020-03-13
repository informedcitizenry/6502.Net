//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// An enumeration of the symbol data type.
    /// </summary>
    public enum DataType
    {
        None = 0,
        Integer,
        Float,
        String,
        Boolean
    };

    /// <summary>
    /// Represents an abstract class of a symbol object.
    /// </summary>
    public abstract class SymbolBase
    {
        #region Properties

        /// <summary>
        /// The symbol's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The symbol's internal data type.
        /// </summary>
        public DataType DataType { get; set; }

        #endregion
    }
}
