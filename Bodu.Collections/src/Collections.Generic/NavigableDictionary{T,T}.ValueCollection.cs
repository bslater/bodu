// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.ValueCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a live view of the values contained in a <see cref="NavigableDictionary{TKey, TValue}" />, ordered by
    /// ascending key.
    /// </summary>
    /// <remarks>
    /// The collection reflects subsequent mutations to the underlying dictionary. Enumeration follows ascending key
    /// order and is fail-fast against structural mutation. The collection is read-only;
    /// <see cref="ICollection{T}.Add" />, <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" />
    /// throw <see cref="NotSupportedException" />.
    /// </remarks>
    public sealed class ValueCollection
        : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        /// <summary>The dictionary whose values this collection exposes.</summary>
        private readonly NavigableDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCollection" /> class bound to the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose values this collection exposes. Must not be <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary" /> is <see langword="null" />.
        /// </exception>
        internal ValueCollection(NavigableDictionary<TKey, TValue> dictionary)
        {
            ThrowHelper.ThrowIfNull(dictionary);

            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public int Count => _dictionary._count;

        /// <inheritdoc />
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc />
        /// <remarks>
        /// Performs a linear O(n) walk, matching <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> —
        /// values are not indexed.
        /// </remarks>
        public bool Contains(TValue item) =>
            _dictionary.ContainsValue(item);

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, Count);

            foreach (KeyValuePair<TKey, TValue> entry in _dictionary)
                array[arrayIndex++] = entry.Value;
        }

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> entry in _dictionary.EnumerateAscending())
                yield return entry.Value;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be added directly; modify the owning dictionary instead.
        /// </exception>
        void ICollection<TValue>.Add(TValue item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryValuesMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be cleared directly; call <see cref="NavigableDictionary{TKey, TValue}.Clear" /> instead.
        /// </exception>
        void ICollection<TValue>.Clear() =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryValuesMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be removed directly; remove the owning entry via
        /// <see cref="NavigableDictionary{TKey, TValue}.Remove(TKey)" /> instead.
        /// </exception>
        bool ICollection<TValue>.Remove(TValue item) =>
            throw new NotSupportedException(CollectionsResourceStrings.Op_NotSupported_DictionaryValuesMutation);
    }
}
