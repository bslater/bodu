// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Runtime.ExceptionServices;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Produces a sequence that lazily caches the elements of the source as it is iterated for the first time.
    /// Subsequent iteration requests return the cached elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The sequence whose elements should be cached.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> that caches the source's elements on first enumeration and returns cached
    /// results on all subsequent enumerations.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This method uses deferred execution. The caching begins only when the resulting sequence is enumerated. If the
    /// source is already a collection or an existing cached sequence, no additional wrapping is performed.
    /// </remarks>
    public static IEnumerable<T> Cache<T>(this IEnumerable<T> source) => source switch
    {
        null => throw new ArgumentNullException(nameof(source)),

        // Return the sequence directly for types that are already stable collections;
        // no caching wrapper is needed since repeated enumeration is already safe.
        ICollection<T> => source,
        IReadOnlyCollection<T> => source,
        CacheEnumerable<T> => source,
        _ => new CacheEnumerable<T>(source)
    };

    /// <summary>
    /// A sequence that caches its elements as they are enumerated, allowing subsequent replays without re-enumerating
    /// the source.
    /// </summary>
    /// <typeparam name="T">The type of elements in the cached sequence.</typeparam>
    /// <remarks>
    /// This type is used internally by <see cref="Cache{T}" /> and is thread-safe for concurrent enumeration.
    /// </remarks>
    private sealed class CacheEnumerable<T> :
       System.Collections.Generic.IEnumerable<T>,
       System.IDisposable
    {
        /// <summary>
        /// The initial backing-array length allocated when the first element is appended.
        /// </summary>
        private const int DefaultCapacity = 4;

        /// <summary>
        /// The original sequence whose elements are cached on first enumeration.
        /// </summary>
        private readonly IEnumerable<T> _source;

        /// <summary>
        /// The append-only backing store; <see langword="null" /> until initialized and after disposal.
        /// </summary>
        private T[]? _items;

        /// <summary>
        /// The number of elements currently published into <see cref="_items" />.
        /// </summary>
        private int _count;

        /// <summary>
        /// The shared source enumerator, also used as the monitor guarding source advancement.
        /// </summary>
        private IEnumerator<T>? _enumerator;

        /// <summary>
        /// The exception captured if the source faulted during enumeration.
        /// </summary>
        private volatile ExceptionDispatchInfo? _exception;

        /// <summary>
        /// The index at which <see cref="_exception" /> was captured, or <c>-1</c> if none.
        /// </summary>
        private int _exceptionIndex = -1;

        /// <summary>
        /// The initialization state: <c>0</c> not initialized, <c>1</c> initializing, <c>2</c> initialized.
        /// </summary>
        private int _initializationState;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEnumerable{T}" /> class.
        /// </summary>
        /// <param name="source">The original sequence to cache during iteration.</param>
        public CacheEnumerable(IEnumerable<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Disposes internal state and releases the source enumerator and cached items.
        /// </summary>
        /// <remarks>
        /// Field resets are performed via <see cref="Volatile.Write{T}(ref T, T)" /> to ensure that concurrent
        /// enumerators observing these fields always see the post-dispose state, preventing use of freed resources on
        /// weakly-ordered architectures.
        /// </remarks>
        public void Dispose()
        {
            Interlocked.Exchange(ref _enumerator, null)?.Dispose();

            // Use volatile writes so that concurrent MoveNext callers on other threads
            // immediately observe the disposed state without stale-read false negatives.
            Volatile.Write(ref _count, 0);
            Volatile.Write(ref _items, null);
            _exception = null;                        // already a volatile field; plain write is sufficient
            Volatile.Write(ref _exceptionIndex, -1);
            Volatile.Write(ref _initializationState, 0);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Ensures the cache is initialized and the source enumerator is safely acquired.
        /// </summary>
        private void EnsureInitialized()
        {
            // Fast path: already initialized
            if (Volatile.Read(ref _items) != null)
                return;

            // Try to claim initialization responsibility
            if (Interlocked.CompareExchange(ref _initializationState, 1, 0) == 0)
            {
                try
                {
                    _enumerator = _source.GetEnumerator(); // May throw
                    Volatile.Write(ref _count, 0);
                    Volatile.Write(ref _items, Array.Empty<T>()); // publish the (empty) cache last
                    Interlocked.Exchange(ref _initializationState, 2); // Mark as complete
                }
                catch (Exception ex)
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                    Interlocked.Exchange(ref _initializationState, 2); // Mark as complete (with failure)
                    throw;
                }
            }
            else
            {
                // Spin until the initializing thread finishes (success or failure)
                SpinWait spin = default;
                while (Volatile.Read(ref _initializationState) != 2)
                    spin.SpinOnce();

                // Re-throw any exception captured during initialization
                _exception?.Throw();
            }
        }

        /// <summary>
        /// Appends an element to the cache, growing the backing array as required.
        /// </summary>
        /// <param name="item">The element to append.</param>
        /// <remarks>
        /// Only ever invoked by the single thread that currently holds the source-enumerator monitor, so reads of the
        /// writer-owned <c>_items</c> and <c>_count</c> fields need no synchronization. The new element is stored into
        /// its slot before <c>_count</c> is published with release semantics, so any reader that observes the updated
        /// count is guaranteed to also observe the element and the (possibly larger) backing array.
        /// </remarks>
        private void Append(T item)
        {
            T[]? items = _items;
            if (items is null)
                return; // disposed concurrently; nothing to publish

            int count = _count;
            if (count == items.Length)
            {
                var grown = new T[items.Length == 0 ? DefaultCapacity : items.Length * 2];
                Array.Copy(items, grown, count);
                Volatile.Write(ref _items, grown); // publish the larger array before the slot write
                items = grown;
            }

            items[count] = item;                   // write the element into its slot
            Volatile.Write(ref _count, count + 1); // publish the new length last
        }

        /// <summary>
        /// Enumerator that reads items from the cached buffer or populates new items from the source.
        /// </summary>
        private sealed class Enumerator :
           System.Collections.Generic.IEnumerator<T>
        {
            /// <summary>
            /// The parent <see cref="CacheEnumerable{T}" /> whose cache this enumerator reads.
            /// </summary>
            private readonly CacheEnumerable<T> _parent;

            /// <summary>
            /// The current zero-based position of this enumerator within the cached sequence.
            /// </summary>
            private int _index;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator" /> class.
            /// </summary>
            /// <param name="parent">The parent <see cref="CacheEnumerable{T}" /> instance.</param>
            public Enumerator(CacheEnumerable<T> parent)
            {
                _parent = parent;
                _index = -1;
                _parent.EnsureInitialized();
            }

            /// <inheritdoc />
            public T Current
            {
                get
                {
                    // Read the published length before the backing array so that, when _index is in range, the
                    // observed array is guaranteed new enough to contain the element (see Append remarks).
                    int count = Volatile.Read(ref _parent._count);
                    T[] items = Volatile.Read(ref _parent._items)
                        ?? throw new ObjectDisposedException(nameof(CacheEnumerable<T>));
                    return _index < 0 || _index >= count
                        ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_EnumeratorNotOnElement)
                        : items[_index];
                }
            }

            /// <inheritdoc />
            object IEnumerator.Current => Current!;

            /// <summary>
            /// Disposes the enumerator. No-op for this implementation.
            /// </summary>
            public void Dispose()
            { }

            /// <inheritdoc />
            public bool MoveNext()
            {
                _index++;

                // Read the published length first; observing _items only serves to detect disposal
                // (a live cache always exposes a non-null backing array, even when empty).
                int count = Volatile.Read(ref _parent._count);
                _ = Volatile.Read(ref _parent._items) ?? throw new ObjectDisposedException(nameof(CacheEnumerable<T>));

                // Fast path: the element at this index has already been published to the cache.
                if (_index < count)
                    return true;

                // Re-throw any exception previously captured at this index position.
                if (Volatile.Read(ref _parent._exceptionIndex) == _index)
                    _parent._exception?.Throw();

                IEnumerator<T>? enumerator = Volatile.Read(ref _parent._enumerator);
                if (enumerator is null)
                    return TryResolveCompletion();

                // Attempt to take exclusive access to advance the source enumerator.
                // Only one thread at a time may fetch the next element and append it to the cache.
                if (!Monitor.TryEnter(enumerator, 0))
                {
                    // Another thread is currently advancing the enumerator; spin until the element
                    // at the required index is available or the enumerator reaches end-of-sequence.
                    SpinWait spin = default;
                    while (true)
                    {
                        if (_index < Volatile.Read(ref _parent._count))
                            return true;

                        if (Volatile.Read(ref _parent._enumerator) is null)
                            return TryResolveCompletion();

                        spin.SpinOnce();
                    }
                }

                try
                {
                    // Re-check under the lock: between snapshotting the enumerator and acquiring it, another thread
                    // may have appended the element at this index, or exhausted and disposed the source enumerator.
                    // Advancing a disposed enumerator would spuriously report end-of-sequence and drop elements.
                    if (_index < Volatile.Read(ref _parent._count))
                        return true;

                    if (Volatile.Read(ref _parent._enumerator) is null)
                        return TryResolveCompletion();

                    if (enumerator.MoveNext())
                    {
                        _parent.Append(enumerator.Current); // append to the shared cache under the lock
                        return true;
                    }

                    // End of source sequence; release the enumerator.
                    enumerator.Dispose();
                    Volatile.Write(ref _parent._enumerator, null);
                    return false;
                }
                catch (Exception ex)
                {
                    _parent._exception = ExceptionDispatchInfo.Capture(ex);
                    Volatile.Write(ref _parent._exceptionIndex, _index);
                    enumerator.Dispose();
                    Volatile.Write(ref _parent._enumerator, null);
                    throw;
                }
                finally
                {
                    Monitor.Exit(enumerator);
                }
            }

            /// <summary>
            /// Resolves the result of <see cref="MoveNext" /> once the source enumerator has been observed as
            /// completed, re-reading the published length so a concurrently appended final element is never skipped.
            /// </summary>
            /// <returns>
            /// <see langword="true" /> if the element at the current index became available; otherwise,
            /// <see langword="false" />.
            /// </returns>
            private bool TryResolveCompletion()
            {
                // A reader that has observed _enumerator == null is, by transitive happens-before through the
                // source-enumerator monitor, guaranteed to also observe the final published count. Re-reading it
                // here closes the race in which a stale earlier count read would terminate one element too soon.
                if (_index < Volatile.Read(ref _parent._count))
                    return true;

                // Surface a fault captured at this position by the writer thread.
                if (Volatile.Read(ref _parent._exceptionIndex) == _index)
                    _parent._exception?.Throw();

                return false;
            }

            /// <summary>
            /// Not supported for caching enumerators.
            /// </summary>
            /// <exception cref="NotSupportedException">Always thrown.</exception>
            public void Reset() => throw new NotSupportedException();
        }
    }
}
