// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueue.Enumerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class IndexedPriorityQueue<TElement, TPriority>
    : IEnumerable<KeyValuePair<TElement, TPriority>>
{
    /// <inheritdoc />
    IEnumerator<KeyValuePair<TElement, TPriority>> IEnumerable<KeyValuePair<TElement, TPriority>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the element-priority pairs of an <see cref="IndexedPriorityQueue{TElement, TPriority}" /> in
    /// heap-storage order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see langword="foreach" /> statement to enumerate the queue rather than using this struct directly.
    /// </para>
    /// <para>
    /// The enumerator captures the queue's modification version at construction. Any structural mutation —
    /// <see cref="Enqueue" />, <see cref="Dequeue" />, <see cref="Update" />, <see cref="Remove" />,
    /// <see cref="EnqueueOrUpdate" />, or <see cref="Clear" /> — invalidates an in-flight enumerator and causes
    /// <see cref="MoveNext" /> or <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator :
        IEnumerator<KeyValuePair<TElement, TPriority>>
    {
        /// <summary>
        /// The queue being enumerated.
        /// </summary>
        private readonly IndexedPriorityQueue<TElement, TPriority> _queue;

        /// <summary>
        /// The queue's <see cref="_version" /> captured at construction; used to detect concurrent modification.
        /// </summary>
        private readonly int _version;

        /// <summary>
        /// The slot index currently exposed by <see cref="Current" />; <c>-1</c> indicates an unstarted or exhausted
        /// enumerator.
        /// </summary>
        private int _index;

        /// <summary>
        /// The element-priority pair returned by <see cref="Current" /> for the current position.
        /// </summary>
        private KeyValuePair<TElement, TPriority> _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct.
        /// </summary>
        /// <param name="queue">The queue to enumerate.</param>
        internal Enumerator(IndexedPriorityQueue<TElement, TPriority> queue)
        {
            _queue = queue;
            _version = queue._version;
            _index = -1;
            _current = default!;
        }

        /// <inheritdoc />
        public readonly KeyValuePair<TElement, TPriority> Current =>
            (_index < 0 || _index >= _queue._size)
                ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_EnumeratorNotOnElement)
                : _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public readonly void Dispose()
        {
            // No unmanaged resources; method provided for interface completeness.
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _queue._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            var next = _index + 1;
            if (next >= _queue._size)
            {
                // Park at _size (not -1) so subsequent MoveNext calls cannot regress into the
                // valid range; -1 would make `next` resolve to 0 and silently restart iteration.
                _index = _queue._size;
                _current = default!;
                return false;
            }

            (TElement Element, TPriority Priority) node = _queue._nodes[next];
            _current = new KeyValuePair<TElement, TPriority>(node.Element, node.Priority);
            _index = next;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _queue._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            _index = -1;
            _current = default!;
        }
    }
}
