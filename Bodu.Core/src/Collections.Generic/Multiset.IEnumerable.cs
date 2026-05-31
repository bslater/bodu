// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Multiset.IEnumerable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class Multiset<T>
    : System.Collections.Generic.IEnumerable<T>
{
    /// <summary>
    /// Returns an enumerator that iterates all elements in the <see cref="Multiset{T}" />, yielding each element as
    /// many times as its occurrence count.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> for the multiset.</returns>
    /// <remarks>
    /// The enumerator captures a structural-version token at creation. Any subsequent structural modification —
    /// including <see cref="Add(T)" />, <see cref="Remove" />, <see cref="RemoveAll" />, and <see cref="Clear" /> —
    /// invalidates the enumerator. The next call to <see cref="Enumerator.MoveNext" /> or
    /// <see cref="Enumerator.Reset" /> throws <see cref="InvalidOperationException" />.
    /// </remarks>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the elements of a <see cref="Multiset{T}" />, yielding each element as many times as its occurrence
    /// count in an unspecified order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see langword="foreach" /> statement to enumerate the multiset rather than using this struct directly.
    /// </para>
    /// <para>
    /// The enumerator provides read-only access. Modifying the underlying multiset after enumeration begins invalidates
    /// the enumerator and causes <see cref="MoveNext" /> or <see cref="Reset" /> to throw
    /// <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator : IEnumerator<T>
    {
        private readonly Multiset<T> _multiset;
        private readonly int _version;
        private Dictionary<T, int>.Enumerator _inner;
        private T _current;
        private int _remaining;
        private bool _beforeFirst;

        /// <summary>
        /// Initializes a new instance of the <see cref="Multiset{T}.Enumerator" /> struct for the specified multiset.
        /// </summary>
        /// <param name="multiset">The multiset to enumerate.</param>
        internal Enumerator(Multiset<T> multiset)
        {
            _multiset = multiset;
            _version = multiset._version;
            _inner = multiset._items.GetEnumerator();
            _current = default!;
            _remaining = 0;
            _beforeFirst = true;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the multiset at the current enumerator position.</returns>
        /// <exception cref="InvalidOperationException">
        /// The enumerator is positioned before the first element or after the last element.
        /// </exception>
        public readonly T Current =>
            _beforeFirst
                ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_EnumeratorNotOnElement)
                : _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current!;

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();

        /// <summary>
        /// Advances the enumerator to the next element in the multiset.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced to the next element;
        /// <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The multiset was modified after the enumerator was created.
        /// </exception>
        public bool MoveNext()
        {
            if (_version != _multiset._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            if (_remaining > 0)
            {
                _remaining--;
                _beforeFirst = false;
                return true;
            }

            if (!_inner.MoveNext())
            {
                _current = default!;
                _beforeFirst = true;
                return false;
            }

            KeyValuePair<T, int> pair = _inner.Current;
            _current = pair.Key;
            _remaining = pair.Value - 1;
            _beforeFirst = false;
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, before the first element in the multiset.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The multiset was modified after the enumerator was created.
        /// </exception>
        public void Reset()
        {
            if (_version != _multiset._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            _inner.Dispose();
            _inner = _multiset._items.GetEnumerator();
            _current = default!;
            _remaining = 0;
            _beforeFirst = true;
        }
    }
}
