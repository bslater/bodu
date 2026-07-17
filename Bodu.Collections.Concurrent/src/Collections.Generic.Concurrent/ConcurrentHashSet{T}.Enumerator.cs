// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet{T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T>
{
    /// <summary>
    /// Enumerates a weakly consistent snapshot of a <see cref="ConcurrentHashSet{T}" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The enumerator captures a snapshot of the set's elements when it is created, by calling
    /// <see cref="ConcurrentHashSet{T}.ToArray" />. All iteration runs over that fixed copy and is unaffected by
    /// concurrent additions or removals on the originating set. The snapshot itself is <b>weakly consistent</b> (see
    /// <see cref="ConcurrentHashSet{T}.ToArray" />): it contains every element present for the entire duration of its
    /// capture, but it is not guaranteed to correspond to the set's state at any single instant. The no-duplicate
    /// guarantee holds across distinct hash keys; an element concurrently removed and re-added may, in rare
    /// full-hash-collision interleavings, appear twice.
    /// </para>
    /// <para>
    /// Because the snapshot is taken eagerly, creating the enumerator allocates an array proportional to the number of
    /// elements present at that moment. The enumerator never throws <see cref="InvalidOperationException" /> as a
    /// result of concurrent modification.
    /// </para>
    /// <para>
    /// The order in which elements are yielded is unspecified and may differ between enumerators.
    /// </para>
    /// </remarks>
    public struct Enumerator
        : IEnumerator<T>
    {
        /// <summary>The weakly consistent snapshot of the set's elements captured when the enumerator was created.</summary>
        private readonly T[] _snapshot;

        /// <summary>The element exposed by <see cref="Current" /> for the current position.</summary>
        private T _current;

        /// <summary>The index of the most recently yielded element, or <c>-1</c> before the first <see cref="MoveNext" /> call.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct by capturing a snapshot of the set.
        /// </summary>
        /// <param name="owner">The set to enumerate. Must not be <see langword="null" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="owner" /> is <see langword="null" />.</exception>
        internal Enumerator(ConcurrentHashSet<T> owner)
        {
            ThrowHelper.ThrowIfNull(owner);

            _snapshot = owner.ToArray();
            _current = default!;
            _index = -1;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        /// <value>The element at the enumerator's current position.</value>
        public readonly T Current => _current;

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        /// <value>The element at the enumerator's current position.</value>
        readonly object IEnumerator.Current => _current;

        /// <summary>
        /// Releases all resources used by the enumerator.
        /// </summary>
        /// <remarks>
        /// The enumerator holds no unmanaged or disposable resources; this method does nothing.
        /// </remarks>
        public readonly void Dispose()
        {
        }

        /// <summary>
        /// Advances the enumerator to the next element of the snapshot.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator advanced to a new element; <see langword="false" /> if it has
        /// passed the end of the snapshot.
        /// </returns>
        /// <remarks>
        /// A default-valued <see cref="Enumerator" /> — one produced by <c>default</c> rather than by
        /// <see cref="ConcurrentHashSet{T}.GetEnumerator" /> — holds no snapshot and is treated as an empty sequence.
        /// </remarks>
        public bool MoveNext()
        {
            T[] snapshot = _snapshot ?? [];
            int next = _index + 1;
            if (next < snapshot.Length)
            {
                _current = snapshot[next];
                _index = next;
                return true;
            }

            _current = default!;
            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position, before the first element of the snapshot.
        /// </summary>
        public void Reset()
        {
            _current = default!;
            _index = -1;
        }
    }
}
