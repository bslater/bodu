// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary.KeyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a live, order-preserving view of the keys contained in an
    /// <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// The collection reflects subsequent mutations to the underlying dictionary. Enumeration order follows the current
    /// <see cref="EvictingDictionaryPolicy" />. The collection is read-only; <see cref="ICollection{T}.Add" />,
    /// <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" /> throw
    /// <see cref="NotSupportedException" />.
    /// </remarks>
    public sealed class KeyCollection :
        ICollection<TKey>,
        IReadOnlyCollection<TKey>,
        ICollection
    {
        /// <summary>
        /// The dictionary whose keys this collection exposes.
        /// </summary>
        private readonly EvictingDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyCollection" /> class bound to the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys this collection exposes. Must not be <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary" /> is <see langword="null" />.
        /// </exception>
        internal KeyCollection(EvictingDictionary<TKey, TValue> dictionary)
        {
            ThrowHelper.ThrowIfNull(dictionary);

            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public int Count => _dictionary._store.Count;

        /// <inheritdoc />
        bool ICollection<TKey>.IsReadOnly => true;

        /// <inheritdoc />
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc />
        object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

        /// <inheritdoc />
        public bool Contains(TKey item) => _dictionary.ContainsKey(item);

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, arrayIndex, Count);

            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary.GetOrderedItems())
                array[arrayIndex++] = kvp.Key;
        }

        /// <inheritdoc />
        public IEnumerator<TKey> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary.GetOrderedItems())
                yield return kvp.Key;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        void ICollection.CopyTo(Array array, int index)
        {
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfArrayMultidimensional(array);
            ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
            ThrowHelper.ThrowIfLessThan(index, 0);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, Count);

            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary.GetOrderedItems())
                array.SetValue(kvp.Key, index++);
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be added directly; modify the owning dictionary instead.
        /// </exception>
        void ICollection<TKey>.Add(TKey item) =>
            throw new NotSupportedException(ResourceStrings.Op_NotSupported_DictionaryKeysMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be cleared directly; call <see cref="EvictingDictionary{TKey, TValue}.Clear" /> instead.
        /// </exception>
        void ICollection<TKey>.Clear() =>
            throw new NotSupportedException(ResourceStrings.Op_NotSupported_DictionaryKeysMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be removed directly; call <see cref="EvictingDictionary{TKey, TValue}.Remove(TKey)" /> instead.
        /// </exception>
        bool ICollection<TKey>.Remove(TKey item) =>
            throw new NotSupportedException(ResourceStrings.Op_NotSupported_DictionaryKeysMutation);
    }
}
