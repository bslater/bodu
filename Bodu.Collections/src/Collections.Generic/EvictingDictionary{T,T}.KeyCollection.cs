// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary{T,T}.KeyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

using Bodu.Collections.Generic.Internal;

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
    public sealed class KeyCollection
        : ICollection<TKey>,
        IReadOnlyCollection<TKey>,
        ICollection
    {
        /// <summary>The dictionary whose keys this collection exposes.</summary>
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
        /// <remarks>
        /// The shared copy loop validates the argument shape before purging expired entries, so a caller error cannot
        /// trigger evictions or raise the eviction events; the purge then runs before the range check so the count
        /// validated matches the elements written.
        /// </remarks>
        public void CopyTo(TKey[] array, int arrayIndex) =>
            DictionaryViewCore.CopyKeysTo(_dictionary, array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<TKey> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
                yield return kvp.Key;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        /// <remarks>
        /// The shared copy loop validates the argument shape before purging expired entries, so a caller error cannot
        /// trigger evictions or raise the eviction events; the purge then runs before the range check so the count
        /// validated matches the elements written.
        /// </remarks>
        void ICollection.CopyTo(Array array, int index) =>
            DictionaryViewCore.CopyKeysTo(_dictionary, array, index);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be added directly; modify the owning dictionary instead.
        /// </exception>
        void ICollection<TKey>.Add(TKey item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryKeysMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be cleared directly; call <see cref="EvictingDictionary{TKey, TValue}.Clear" /> instead.
        /// </exception>
        void ICollection<TKey>.Clear() =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryKeysMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Keys cannot be removed directly; call <see cref="EvictingDictionary{TKey, TValue}.Remove(TKey)" /> instead.
        /// </exception>
        bool ICollection<TKey>.Remove(TKey item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryKeysMutation);
    }
}
