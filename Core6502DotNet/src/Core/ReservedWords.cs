//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// A collection of uniquely defined reserved words.
    /// </summary>
    public class ReservedWords
    {
        #region Members

        HashSet<string> _values;
        Dictionary<string, HashSet<string>> _types;
        StringComparer _comparer;

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="ReservedWords"/> class object.
        /// </summary>
        /// <param name="comparer">A <see cref="StringComparer"/> object to indicate whether
        /// to enforce case-sensitivity.</param>
        public ReservedWords(StringComparer comparer) 
            => Comparer = comparer;

        #endregion

        #region Methods

        /// <summary>
        /// Add a reserved word to a defined type.
        /// </summary>
        /// <param name="type">The defined type</param>
        /// <param name="word">The reserved word to include</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void AddWord(string type, string word)
        {
            HashSet<string> t = _types[type];
            t.Add(word);
            _values.Add(word);
        }

        /// <summary>
        /// Defie a type of reserved words.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <exception cref="ArgumentException"></exception>
        public void DefineType(string type) => _types.Add(type, new HashSet<string>(_comparer));

        /// <summary>
        /// Define a type of reserved words.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <param name="values">The collection of values that comprise the type. </param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void DefineType(string type, params string[] values)
        {
            _types.Add(type, new HashSet<string>(values, _comparer));
            foreach (var v in values)
                _values.Add(v); // grr!!!
        }

        /// <summary>
        /// Determines if the token is one of the type specified.
        /// </summary>
        /// <param name="type">The type (dictionary key).</param>
        /// <param name="token">The token or keyword.</param>
        /// <returns><c>True</c> if the specified token is one of the specified type, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool IsOneOf(string type, string token) => _types[type].Contains(token);

        /// <summary>
        /// Determines if the token is in the list of reserved words for all types.
        /// </summary>
        /// <param name="token">The token or keyword.</param>
        /// <returns><c>True</c> if the specified token is in the collection of reserved words,
        /// regardless of type, otherwise <c>false</c>.</returns>
        public bool IsReserved(string token) =>  _values.Contains(token);


        public IEnumerable<string> GetReserved() => _values;

        #endregion

        #region Properties

        /// <summary>
        /// Sets the <see cref="StringComparer"/> for the 
        /// <see cref="ReservedWords"/> collection. Setting this value
        /// will clear the collection values.
        /// </summary>
        public StringComparer Comparer
        {
            set
            {
                _comparer = value;
                _types = new Dictionary<string, HashSet<string>>(_comparer);
                _values = new HashSet<string>(_comparer);
            }
        }

        #endregion
    }
}