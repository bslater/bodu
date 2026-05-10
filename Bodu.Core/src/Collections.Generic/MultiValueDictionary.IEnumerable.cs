// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionary.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public sealed partial class MultiValueDictionary<TKey, TValue>
    : System.Collections.Generic.IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>
{
    /// <summary>
    /// Returns an enumerator that iterates the key–value-list pairs in the
    /// <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the dictionary.</returns>
    /// <remarks>
    /// The enumerator captures a structural-version token at creation. Any subsequent structural
    /// modification — including <see cref="Add"/>, <see cref="Remove"/>, <see cref="RemoveAll"/>, or
    /// <see cref="Clear"/> — invalidates the enumerator. The next call to
    /// <see cref="Enumerator.MoveNext"/> or <see cref="Enumerator.Reset"/> throws
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>
        IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>.GetEnumerator() =>
        new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the key–value-list pairs in a <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <para>Use the <see langword="foreach"/> statement to enumerate the dictionary rather than using this struct directly.</para>
    /// <para>
    /// The enumerator provides read-only access. Modifying the underlying dictionary after enumeration
    /// begins invalidates the enumerator and causes <see cref="MoveNext"/> or <see cref="Reset"/> to
    /// throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    {
        private readonly MultiValueDictionary<TKey, TValue> _dictionary;
        private readonly int _version;
        private Dictionary<TKey, List<TValue>>.Enumerator _inner;
        private KeyValuePair<TKey, IReadOnlyList<TValue>> _current;
        private bool _beforeFirst;

        /// <summary>
        /// Initialises a new instance of the <see cref="Enumerator"/> struct for the specified dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate.</param>
        internal Enumerator(MultiValueDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = dictionary._version;
            _inner = dictionary._map.GetEnumerator();
            _current = default;
            _beforeFirst = true;
        }

        /// <summary>
        /// Gets the key–value-list pair at the current position of the enumerator.
        /// </summary>
        /// <returns>The key–value-list pair at the current enumerator position.</returns>
        /// <exception cref="InvalidOperationException">
        /// The enumerator is positioned before the first element or after the last element.
        /// </exception>
        public KeyValuePair<TKey, IReadOnlyList<TValue>> Current =>
            _beforeFirst
                ? throw new InvalidOperationException(ResourceStrings.InvalidOperation_EnumeratorNotOnElement)
                : _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();

        /// <summary>
        /// Advances the enumerator to the next key–value-list pair.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next pair;
        /// <see langword="false"/> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">The dictionary was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            if (!_inner.MoveNext())
            {
                _current = default;
                _beforeFirst = true;
                return false;
            }

            KeyValuePair<TKey, List<TValue>> pair = _inner.Current;
            _current = new KeyValuePair<TKey, IReadOnlyList<TValue>>(pair.Key, pair.Value);
            _beforeFirst = false;
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, before the first element in the dictionary.
        /// </summary>
        /// <exception cref="InvalidOperationException">The dictionary was modified after the enumerator was created.</exception>
        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            _inner.Dispose();
            _inner = _dictionary._map.GetEnumerator();
            _current = default;
            _beforeFirst = true;
        }
    }
}
