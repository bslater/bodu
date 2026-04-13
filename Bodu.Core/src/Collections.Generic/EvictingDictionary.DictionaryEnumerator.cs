// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="EvictingDictionary.DictionaryEnumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;
using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Enumerates the elements of an <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// <para>Use the <see langword="foreach" /> statement to simplify the enumeration process instead of directly using this enumerator.</para>
    /// <para>
    /// The enumerator provides read-only access to the dictionary's elements. Modifying the underlying dictionary while enumerating
    /// invalidates the enumerator.
    /// </para>
    /// </remarks>
    public struct DictionaryEnumerator :
       System.Collections.IDictionaryEnumerator
    {
        private readonly EvictingDictionary<TKey, TValue> _dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> _inner;

        /// <summary>
        /// Initialises a new instance of the <see cref="DictionaryEnumerator" /> struct.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate. Must not be <see langword="null" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is <see langword="null" />.</exception>
        public DictionaryEnumerator(EvictingDictionary<TKey, TValue> dictionary)
        {
            this._dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            this._inner = dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public object Current => this.Entry;

        /// <inheritdoc />
        public DictionaryEntry Entry => new DictionaryEntry(this._inner.Current.Key!, this._inner.Current.Value);

        /// <inheritdoc />
        public object Key => this._inner.Current.Key!;

        /// <inheritdoc />
        public object? Value => this._inner.Current.Value;

        /// <inheritdoc />
        public bool MoveNext() => this._inner.MoveNext();

        /// <inheritdoc />
        public void Reset()
        {
            this._inner.Dispose();
            this._inner = this._dictionary.GetEnumerator();
        }
    }
}
