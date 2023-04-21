//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Enumerates a string as a collection of <see cref="CharValue"/>s.
/// </summary>
public class CharValueEnumerator : IEnumerator<CharValue>
{
    private readonly IEnumerator<char> _charEnumerator;
    private CharValue _current;

    /// <summary>
    /// Construct a new instance of a <see cref="CharValueEnumerator"/> with
    /// the given string value.
    /// </summary>
    /// <param name="value">The string to enumerate and yield
    /// <see cref="CharValue"/>s.</param>
    public CharValueEnumerator(string value)
    {
        _charEnumerator = value.GetEnumerator();
        _current = new CharValue();
    }

    /// <summary>
    /// The current <see cref="CharValue"/>.
    /// </summary>
    public CharValue Current => _current;

    object IEnumerator.Current => Current;

    /// <summary>
    /// Dispose the object. 
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _charEnumerator.Dispose();
        }
    }

    /// <summary>
    /// Move to the next <see cref="CharValue"/> in the enumeration.
    /// </summary>
    /// <returns><c>true</c> if the MoveNext operation is successful,
    /// <c>false</c> otherwise.</returns>
    public bool MoveNext()
    {
        if (_charEnumerator.MoveNext())
        {
            _current = new CharValue(_charEnumerator.Current);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reset the enumerator's internal state.
    /// </summary>
    public void Reset()
    {
        _charEnumerator.Reset();
        _current = new CharValue();
    }
}

