//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Core6502DotNet
{
    /// <summary>
    /// Supports a deep iteration over a token and each of its children, to 
    /// the lowest descendants.
    /// </summary>
    public class TokenEnumerator : IEnumerator<Token>, IEnumerable<Token>
    {
        #region Members

        int _currentIndex;
        bool _visited;
        readonly Token _current;
        TokenEnumerator _childEnumerator;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a token enumerator class.
        /// </summary>
        /// <param name="token">The root <see cref="Token"/> to enumerate through.</param>
        TokenEnumerator(Token token)
        {
            _current = token;
            Reset();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a new instance of a <see cref="TokenEnumerator"/> class.
        /// </summary>
        /// <param name="token">The root token.</param>
        /// <returns>A <see cref="TokenEnumerator"/> that iterates deep into the token graph.</returns>
        public static TokenEnumerator GetEnumerator(Token token) => new TokenEnumerator(token);

        /// <summary>
        /// Gets a new instance of a <see cref="TokenEnumerator"/> class.
        /// </summary>
        /// <param name="tokens">A token collection.</param>
        /// <returns>A <see cref="TokenEnumerator"/> that iterates deep into the token graph.</returns>
        public static TokenEnumerator GetEnumerator(IEnumerable<Token> tokens)
        {
            var parent = Token.SeparatorToken();
            foreach (var token in tokens)
                parent.AddChild(token);
            return GetEnumerator(parent);
        }

        public bool MoveNext()
        {
            if (!_visited)
            {
                _visited = true;
                return true;
            }
            if (_childEnumerator != null && _childEnumerator.MoveNext())
                return _childEnumerator.Current != null;

            if (_current.Children.Count > _currentIndex)
            {
                _childEnumerator = new TokenEnumerator(_current.Children[_currentIndex++]);
                return _childEnumerator.MoveNext();
            }
            return false;
        }

        public Token Current
        {
            get
            {
                if (_childEnumerator != null)
                    return _childEnumerator.Current;
                if (!_visited)
                    return null;
                return _current;
            }
        }

        public void Reset()
        {
            _visited = false;
            _currentIndex = 0;
            _childEnumerator = null;
        }

        public void Dispose() => _childEnumerator?.Dispose();

        public IEnumerator<Token> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Properties

        object IEnumerator.Current => Current;

        #endregion
    }
}
