// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionary{T,T}.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class MultiValueDictionary<TKey, TValue>
    : IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>
{
    /// <summary>
    /// Returns an enumerator that iterates the key-value-list pairs in the dictionary.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> for the dictionary.</returns>
    /// <remarks>
    /// The enumerator captures a structural-version token at creation. Any subsequent structural modification
    /// invalidates the enumerator. The next call to <see cref="Enumerator.MoveNext" /> or
    /// <see cref="Enumerator.Reset" /> throws <see cref="InvalidOperationException" />.
    /// </remarks>
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>>.GetEnumerator() =>
        new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the key-value-list pairs in a <see cref="MultiValueDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see langword="foreach" /> statement to enumerate the dictionary rather than using this struct directly.
    /// </para>
    /// <para>
    /// The enumerator provides read-only access. Modifying the underlying dictionary after enumeration begins
    /// invalidates the enumerator and causes <see cref="MoveNext" /> or <see cref="Reset" /> to throw
    /// <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    {
        /// <summary>The dictionary whose entries this enumerator iterates over.</summary>
        private readonly MultiValueDictionary<TKey, TValue> _dictionary;

        /// <summary>The dictionary version captured at construction, used to detect concurrent modification.</summary>
        private readonly int _version;

        /// <summary>The underlying bucket enumerator that supplies each key and its associated values.</summary>
        private Dictionary<TKey, ValueBucket>.Enumerator _inner;

        /// <summary>The current key/value-list pair. Read through <see cref="Current" />.</summary>
        private KeyValuePair<TKey, IReadOnlyList<TValue>> _current;

        /// <summary>Indicates whether <see cref="_current" /> holds a valid element at the current position.</summary>
        private bool _hasCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}.Enumerator" /> struct.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate.</param>
        internal Enumerator(MultiValueDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = dictionary._version;
            _inner = dictionary._map.GetEnumerator();
            _current = default;
            _hasCurrent = false;
        }

        /// <summary>
        /// Gets the key-value-list pair at the current position of the enumerator.
        /// </summary>
        /// <value>The key-value-list pair at the current enumerator position.</value>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the enumerator is not positioned on an element.
        /// </exception>
        public readonly KeyValuePair<TKey, IReadOnlyList<TValue>> Current =>
            _hasCurrent
                ? _current
                : throw new InvalidOperationException(ResourceStrings.Op_Invalid_EnumeratorNotOnElement);

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();

        /// <summary>
        /// Advances the enumerator to the next key-value-list pair.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator advanced to the next pair; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the dictionary was modified after the enumerator was created.
        /// </exception>
        public bool MoveNext()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            if (!_inner.MoveNext())
            {
                _current = default;
                _hasCurrent = false;
                return false;
            }

            KeyValuePair<TKey, ValueBucket> pair = _inner.Current;
            _current = new KeyValuePair<TKey, IReadOnlyList<TValue>>(pair.Key, pair.Value.ReadOnlyValues);
            _hasCurrent = true;

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the dictionary was modified after the enumerator was created.
        /// </exception>
        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            _inner.Dispose();
            _inner = _dictionary._map.GetEnumerator();
            _current = default;
            _hasCurrent = false;
        }
    }
}
