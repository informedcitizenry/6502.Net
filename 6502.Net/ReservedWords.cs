//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asm6502.Net
{
    /// <summary>
    /// A dictionary of reserved words.
    /// </summary>
    public class ReservedWords
    {
        #region Constructor

        /// <summary>
        /// Instantiates a new ReservedWords class object.
        /// </summary>
        /// <param name="comparer">A StringComparison object to indicate whether
        /// to enforce case-sensitivity.</param>
        public ReservedWords(StringComparison comparer)
        {
            Types = new Dictionary<string, HashSet<string>>();
            Comparer = comparer;
        }

        /// <summary>
        /// Instantiates a new ReservedWords class object.
        /// </summary>
        public ReservedWords() :
            this(StringComparison.InvariantCulture)
        {
            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines if the token is one of the type specified.
        /// </summary>
        /// <param name="type">The type (dictionary key).</param>
        /// <param name="token">The token or keyword.</param>
        /// <returns>Returns true if the specified token is one of the specified type.</returns>
        public bool IsOneOf(string type, string token)
        {
            return Types[type].Any(d => d.Equals(token, Comparer));
        }

        /// <summary>
        /// Determines if the token is in the list of reserved words for all types.
        /// </summary>
        /// <param name="token">The token or keyword.</param>
        /// <returns>Returns true if the specified token is in the collection of reserved words,
        /// regardless of type.</returns>
        public bool IsReserved(string token)
        {
            return Types.Values.SelectMany(s => s)
                              .Any(s => s.Equals(token, Comparer));
        }

        /// <summary>
        /// Gets the type of the token, if any.
        /// </summary>
        /// <param name="token">The token or keyword.</param>
        /// <returns>The type of the token.</returns>
        public string GetType(string token)
        {
            foreach(var type in Types)
            {
                if (type.Value.Contains(token))
                {
                    return type.Key;
                }
            }
            return string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the dictionary of reserved words. The key is the type (e.g., Mnemonics), 
        /// the value is a HashSet (unique list) of strings.
        /// </summary>
        public Dictionary<string, HashSet<string>> Types { get; set; }

        public StringComparison Comparer { get; set; }

        #endregion
    }
}
