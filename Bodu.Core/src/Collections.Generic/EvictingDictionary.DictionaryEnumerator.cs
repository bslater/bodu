// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary.DictionaryEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Enumerates the elements of an <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see langword="foreach" /> statement to simplify the enumeration process instead of directly using this
    /// enumerator.
    /// </para>
    /// <para>
    /// The enumerator provides read-only access to the dictionary's elements. Modifying the underlying dictionary while
    /// enumerating invalidates the enumerator.
    /// </para>
    /// </remarks>
    public struct DictionaryEnumerator :
       System.Collections.IDictionaryEnumerator
    {
        private readonly EvictingDictionary<TKey, TValue> _dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryEnumerator" /> struct.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate. Must not be <see langword="null" />.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary" /> is <see langword="null" />.
        /// </exception>
        public DictionaryEnumerator(EvictingDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _inner = dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public object Current => Entry;

        /// <inheritdoc />
        public readonly DictionaryEntry Entry => new(_inner.Current.Key!, _inner.Current.Value);

        /// <inheritdoc />
        public readonly object Key => _inner.Current.Key!;

        /// <inheritdoc />
        public readonly object? Value => _inner.Current.Value;

        /// <inheritdoc />
        public readonly bool MoveNext() => _inner.MoveNext();

        /// <inheritdoc />
        public void Reset()
        {
            _inner.Dispose();
            _inner = _dictionary.GetEnumerator();
        }
    }
}
