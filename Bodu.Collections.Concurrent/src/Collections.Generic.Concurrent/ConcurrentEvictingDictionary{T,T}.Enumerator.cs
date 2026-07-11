// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentEvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Enumerates a point-in-time snapshot of a <see cref="ConcurrentEvictingDictionary{TKey, TValue}" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The enumerator captures a snapshot of the dictionary's live entries when it is created, by calling
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ToArray" />. All iteration runs over that fixed copy and
    /// is unaffected by concurrent additions, removals, or evictions on the originating dictionary.
    /// </para>
    /// <para>
    /// Because the snapshot is taken eagerly, creating the enumerator allocates an array proportional to the number of
    /// entries present at that moment. The enumerator never throws <see cref="InvalidOperationException" /> as a result
    /// of concurrent modification.
    /// </para>
    /// <para>
    /// The order in which entries are yielded is unspecified and may differ between enumerators.
    /// </para>
    /// </remarks>
    public struct Enumerator
        : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        /// <summary>The point-in-time snapshot of the dictionary's live entries captured when the enumerator was created.</summary>
        private readonly KeyValuePair<TKey, TValue>[] _snapshot;

        /// <summary>The entry exposed by <see cref="Current" /> for the current position.</summary>
        private KeyValuePair<TKey, TValue> _current;

        /// <summary>The index of the most recently yielded entry, or <c>-1</c> before the first <see cref="MoveNext" /> call.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct by capturing a snapshot of the
        /// dictionary.
        /// </summary>
        /// <param name="owner">The dictionary to enumerate. Must not be <see langword="null" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="owner" /> is <see langword="null" />.</exception>
        internal Enumerator(ConcurrentEvictingDictionary<TKey, TValue> owner)
        {
            ThrowHelper.ThrowIfNull(owner);

            _snapshot = owner.ToArray();
            _current = default;
            _index = -1;
        }

        /// <summary>
        /// Gets the entry at the current position of the enumerator.
        /// </summary>
        /// <value>The entry at the enumerator's current position.</value>
        public readonly KeyValuePair<TKey, TValue> Current => _current;

        /// <summary>
        /// Gets the entry at the current position of the enumerator.
        /// </summary>
        /// <value>The entry at the enumerator's current position.</value>
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
        /// Advances the enumerator to the next entry of the snapshot.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator advanced to a new entry; <see langword="false" /> if it has passed
        /// the end of the snapshot.
        /// </returns>
        /// <remarks>
        /// A default-valued <see cref="Enumerator" /> — one produced by <c>default</c> rather than by
        /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.GetEnumerator" /> — holds no snapshot and is treated
        /// as an empty sequence.
        /// </remarks>
        public bool MoveNext()
        {
            KeyValuePair<TKey, TValue>[] snapshot = _snapshot ?? [];
            int next = _index + 1;
            if (next < snapshot.Length)
            {
                _current = snapshot[next];
                _index = next;
                return true;
            }

            _current = default;
            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position, before the first entry of the snapshot.
        /// </summary>
        public void Reset()
        {
            _current = default;
            _index = -1;
        }
    }
}
