//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Holds information on an anonymous label, which is referenced according
    /// to its position relative to the referencer's own in source code.
    /// </summary>
    public class AnonymousLabel : SymbolBase, IValueResolver
    {
        public const int Unknown = 0, Backward = 1, Forward = 2;

        private AnonymousLabel()
            : base("")
        {
            Index = -1;
            Value = new Value();
        }

        /// <summary>
        /// Construct a new instance of the <see cref="AnonymousLabel"/> class.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="type">The label type, which can be Backward,
        /// Forward, or Unknown.</param>
        /// <param name="index">The reference's index, the index of its
        /// token in the parsed source.</param>
        public AnonymousLabel(string name, int type, int index)
            : base(name)
        {
            LabelType = type;
            Index = index;
            Value = new Value();
        }

        /// <summary>
        /// Get the anonymous label's type.
        /// </summary>
        public int LabelType { get; init; }

        /// <summary>
        /// Get the anonymous label's index.
        /// </summary>
        public int Index { get; init; }

        public Value Value { get; set; }

        public bool IsConst => false;

        public IValueResolver? IsAReferenceTo { get; set; }
    }
}
