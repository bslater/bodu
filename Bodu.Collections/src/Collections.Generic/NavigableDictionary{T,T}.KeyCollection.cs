// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.KeyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a live, ascending-key-ordered view of the keys contained in a
    /// <see cref="NavigableDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// The collection reflects subsequent mutations to the underlying dictionary. Enumeration follows ascending
    /// comparer order and is fail-fast against structural mutation. The collection is read-only;
    /// <see cref="ICollection{T}.Add" />, <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" />
    /// throw <see cref="NotSupportedException" />.
    /// </remarks>
    public sealed class KeyCollection
        : ICollection<TKey>, IReadOnlyCollection<TKey>
    {
        /// <summary>The dictionary whose keys this collection exposes.</summary>
        private readonly NavigableDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyCollection" /> class bound to the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys this collection exposes. Must not be <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary" /> is <see langword="null" />.
        /// </exception>
        internal KeyCollection(NavigableDictionary<TKey, TValue> dictionary)
        {
            ThrowHelper.ThrowIfNull(dictionary);

            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public int Count => _dictionary._count;

        /// <inheritdoc />
        bool ICollection<TKey>.IsReadOnly => true;

        /// <inheritdoc />
        public bool Contains(TKey item) =>
            _dictionary.ContainsKey(item);

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, Count);

            foreach (KeyValuePair<TKey, TValue> entry in _dictionary)
                array[arrayIndex++] = entry.Key;
        }

        /// <inheritdoc />
        public IEnumerator<TKey> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> entry in _dictionary.EnumerateAscending())
                yield return entry.Key;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be added directly; modify the owning dictionary instead.
        /// </exception>
        void ICollection<TKey>.Add(TKey item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryKeysMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be cleared directly; call <see cref="NavigableDictionary{TKey, TValue}.Clear" /> instead.
        /// </exception>
        void ICollection<TKey>.Clear() =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryKeysMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be removed directly; call <see cref="NavigableDictionary{TKey, TValue}.Remove(TKey)" /> instead.
        /// </exception>
        bool ICollection<TKey>.Remove(TKey item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryKeysMutation);
    }
}
