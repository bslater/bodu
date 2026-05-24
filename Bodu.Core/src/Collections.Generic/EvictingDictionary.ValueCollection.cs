// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary.ValueCollection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a live, order-preserving view of the values contained in an
    /// <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// The collection reflects subsequent mutations to the underlying dictionary. Enumeration order follows the current
    /// <see cref="EvictingDictionaryPolicy" />. The collection is read-only; <see cref="ICollection{T}.Add" />,
    /// <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" /> throw
    /// <see cref="NotSupportedException" />.
    /// </remarks>
    public sealed class ValueCollection :
        ICollection<TValue>,
        IReadOnlyCollection<TValue>,
        ICollection
    {
        private readonly EvictingDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCollection" /> class bound to the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose values this collection exposes. Must not be <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary" /> is <see langword="null" />.
        /// </exception>
        internal ValueCollection(EvictingDictionary<TKey, TValue> dictionary)
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
        public bool Contains(TValue item)
        {
            EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;

            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary.GetOrderedItems())
            {
                if (comparer.Equals(kvp.Value, item))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, arrayIndex, Count);

            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary.GetOrderedItems())
                array[arrayIndex++] = kvp.Value;
        }

        /// <inheritdoc />
        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary.GetOrderedItems())
                yield return kvp.Value;
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
                array.SetValue(kvp.Value, index++);
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be added directly; modify the owning dictionary instead.
        /// </exception>
        void ICollection<TValue>.Add(TValue item) =>
            throw new NotSupportedException(ResourceStrings.Op_NotSupported_DictionaryValuesMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be cleared directly; call <see cref="EvictingDictionary{TKey, TValue}.Clear" /> instead.
        /// </exception>
        void ICollection<TValue>.Clear() =>
            throw new NotSupportedException(ResourceStrings.Op_NotSupported_DictionaryValuesMutation);

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// Values cannot be removed directly; modify the owning dictionary instead.
        /// </exception>
        bool ICollection<TValue>.Remove(TValue item) =>
            throw new NotSupportedException(ResourceStrings.Op_NotSupported_DictionaryValuesMutation);
    }
}
