// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer.Enumerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentCircularBuffer<T> :
    System.Collections.Generic.IReadOnlyCollection<T>
{
    /// <summary>
    /// Provides a snapshot-based enumerator for <see cref="ConcurrentCircularBuffer{T}" /> that iterates over a stable
    /// point-in-time copy of the buffer's contents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The enumerator captures a snapshot of the buffer's elements at the time of construction by calling
    /// <see cref="ConcurrentCircularBuffer{T}.ToArray" />. All subsequent iteration operates over this static copy and
    /// is unaffected by concurrent enqueue, dequeue, or eviction operations on the originating buffer.
    /// </para>
    /// <para>
    /// Because the snapshot is taken eagerly, creating the enumerator allocates a backing array proportional to the
    /// number of elements present at that moment. This is the same allocation that a direct call to
    /// <see cref="ToArray" /> would produce.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator :
       System.Collections.Generic.IEnumerator<T>
    {
        private readonly T[] _snapshot;
        private T? _current;
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct by capturing a snapshot of the buffer's
        /// contents.
        /// </summary>
        /// <param name="owner">
        /// The <see cref="ConcurrentCircularBuffer{T}" /> instance to enumerate. Must not be <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="owner" /> is <see langword="null" />.</exception>
        internal Enumerator(ConcurrentCircularBuffer<T> owner)
        {
            ThrowHelper.ThrowIfNull(owner);
            _snapshot = owner.ToArray();
            _index = -1;
            _current = default;
        }

        /// <inheritdoc />
        public T Current => _current!;

        /// <inheritdoc />
        object IEnumerator.Current => _current!;

        /// <inheritdoc />
        public void Dispose()
        {
            // No resources to dispose.
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            var next = _index + 1;
            if (next < _snapshot.Length)
            {
                _current = _snapshot[next];
                _index = next;
                return true;
            }

            _current = default;
            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = -1;
            _current = default;
        }
    }
}
