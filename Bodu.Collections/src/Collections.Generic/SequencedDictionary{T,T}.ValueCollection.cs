// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionary{T,T}.ValueCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a live, order-preserving view of the values contained in a
    /// <see cref="SequencedDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// The collection reflects subsequent mutations to the underlying dictionary. Enumeration follows the dictionary's
    /// iteration order. The collection is read-only; <see cref="ICollection{T}.Add" />,
    /// <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" /> throw
    /// <see cref="NotSupportedException" />.
    /// </remarks>
    public sealed class ValueCollection
        : ICollection<TValue>,
        IReadOnlyCollection<TValue>,
        ICollection
    {
        /// <summary>The dictionary whose values this collection exposes.</summary>
        private readonly SequencedDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCollection" /> class bound to the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose values this collection exposes. Must not be <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary" /> is <see langword="null" />.
        /// </exception>
        internal ValueCollection(SequencedDictionary<TKey, TValue> dictionary)
        {
            ThrowHelper.ThrowIfNull(dictionary);

            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public int Count => _dictionary._store.Count;

        /// <inheritdoc />
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc />
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc />
        object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

        /// <inheritdoc />
        public bool Contains(TValue item) =>
            DictionaryViewCore.ContainsValue(_dictionary, item);

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex) =>
            DictionaryViewCore.CopyValuesTo(_dictionary, array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
                yield return kvp.Value;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        void ICollection.CopyTo(Array array, int index) =>
            DictionaryViewCore.CopyValuesTo(_dictionary, array, index);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be added directly; modify the owning dictionary instead.
        /// </exception>
        void ICollection<TValue>.Add(TValue item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryValuesMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be cleared directly; call <see cref="SequencedDictionary{TKey, TValue}.Clear" /> instead.
        /// </exception>
        void ICollection<TValue>.Clear() =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryValuesMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be removed directly; modify the owning dictionary instead.
        /// </exception>
        bool ICollection<TValue>.Remove(TValue item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryValuesMutation);
    }
}
