// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Cache.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Produces a sequence that lazily caches the elements of the source as it is iterated for the first time. Subsequent iteration
    /// requests return the cached elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The sequence whose elements should be cached.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> that caches the source's elements on first enumeration and returns cached results on all subsequent enumerations.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This method uses deferred execution. The caching begins only when the resulting sequence is enumerated. If the source is already a
    /// collection or an existing cached sequence, no additional wrapping is performed.
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
    /// A sequence that caches its elements as they are enumerated, allowing subsequent replays without re-enumerating the source.
    /// </summary>
    /// <typeparam name="T">The type of elements in the cached sequence.</typeparam>
    /// <remarks>This type is used internally by <see cref="Cache{T}" /> and is thread-safe for concurrent enumeration.</remarks>
    private sealed class CacheEnumerable<T> :
       System.Collections.Generic.IEnumerable<T>,
       System.IDisposable
    {
        private readonly IEnumerable<T> _source;
        private List<T>? _cache;
        private IEnumerator<T>? _enumerator;
        private volatile ExceptionDispatchInfo? _exception;
        private int _exceptionIndex = -1;
        private int _initializationState; // 0 = not initialised, 1 = initialising, 2 = initialised

        /// <summary>
        /// Initialises a new instance of the <see cref="CacheEnumerable{T}" /> class.
        /// </summary>
        /// <param name="source">The original sequence to cache during iteration.</param>
        public CacheEnumerable(IEnumerable<T> source)
        {
            this._source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Disposes internal state and releases the source enumerator and cached items.
        /// </summary>
        /// <remarks>
        /// Field resets are performed via <see cref="Volatile.Write" /> to ensure that concurrent enumerators observing these fields
        /// always see the post-dispose state, preventing use of freed resources on weakly-ordered architectures.
        /// </remarks>
        public void Dispose()
        {
            Interlocked.Exchange(ref this._enumerator, null)?.Dispose();

            // Use volatile writes so that concurrent MoveNext callers on other threads
            // immediately observe the disposed state without stale-read false negatives.
            Volatile.Write(ref this._cache, null);
            this._exception = null;                        // already a volatile field; plain write is sufficient
            Volatile.Write(ref this._exceptionIndex, -1);
            Volatile.Write(ref this._initializationState, 0);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Ensures the cache is initialised and the source enumerator is safely acquired.
        /// </summary>
        private void EnsureInitialized()
        {
            // Fast path: already initialised
            if (Volatile.Read(ref this._cache) != null)
                return;

            // Try to claim initialisation responsibility
            if (Interlocked.CompareExchange(ref this._initializationState, 1, 0) == 0)
            {
                try
                {
                    this._enumerator = this._source.GetEnumerator(); // May throw
                    this._cache = new List<T>();
                    Interlocked.Exchange(ref this._initializationState, 2); // Mark as complete
                }
                catch (Exception ex)
                {
                    this._exception = ExceptionDispatchInfo.Capture(ex);
                    Interlocked.Exchange(ref this._initializationState, 2); // Mark as complete (with failure)
                    throw;
                }
            }
            else
            {
                // Spin until the initialising thread finishes (success or failure)
                SpinWait spin = default;
                while (Volatile.Read(ref this._initializationState) != 2)
                    spin.SpinOnce();

                // Re-throw any exception captured during initialisation
                this._exception?.Throw();
            }
        }

        /// <summary>
        /// Enumerator that reads items from the cached buffer or populates new items from the source.
        /// </summary>
        private sealed class Enumerator :
           System.Collections.Generic.IEnumerator<T>
        {
            private readonly CacheEnumerable<T> _parent;
            private int _index;

            /// <summary>
            /// Initialises a new instance of the <see cref="Enumerator" /> class.
            /// </summary>
            /// <param name="parent">The parent <see cref="CacheEnumerable{T}" /> instance.</param>
            public Enumerator(CacheEnumerable<T> parent)
            {
                this._parent = parent;
                this._index = -1;
                this._parent.EnsureInitialized();
            }

            /// <inheritdoc />
            public T Current
            {
                get
                {
                    List<T> cache = this._parent._cache ?? throw new ObjectDisposedException(nameof(CacheEnumerable<T>));
                    if (this._index < 0 || this._index >= cache.Count)
                        throw new InvalidOperationException(ResourceStrings.InvalidOperation_EnumeratorNotOnElement);

                    return cache[this._index];
                }
            }

            /// <inheritdoc />
            object IEnumerator.Current => this.Current!;

            /// <summary>
            /// Disposes the enumerator. No-op for this implementation.
            /// </summary>
            public void Dispose()
            { }

            /// <inheritdoc />
            public bool MoveNext()
            {
                this._index++;

                List<T> cache = this._parent._cache ?? throw new ObjectDisposedException(nameof(CacheEnumerable<T>));

                // Return from the cache if the element at this index is already populated.
                if (this._index < cache.Count)
                    return true;

                // Re-throw any exception previously captured at this index position.
                if (this._parent._exceptionIndex == this._index)
                    this._parent._exception?.Throw();

                IEnumerator<T>? enumerator = this._parent._enumerator;
                if (enumerator == null)
                    return false;

                // Attempt to take exclusive access to advance the source enumerator.
                // Only one thread at a time may fetch the next element and append it to the cache.
                if (!Monitor.TryEnter(enumerator, 0))
                {
                    // Another thread is currently advancing the enumerator; spin until the element
                    // at the required index is available or the enumerator reaches end-of-sequence.
                    SpinWait spin = default;
                    while (true)
                    {
                        // Use Volatile.Read to avoid stale cache-count observations on weakly-ordered
                        // architectures where the publishing write from another thread may not yet be visible.
                        if (this._index < (Volatile.Read(ref this._parent._cache)?.Count ?? 0))
                            return true;

                        if (Volatile.Read(ref this._parent._enumerator) == null)
                            return false;

                        spin.SpinOnce();
                    }
                }

                try
                {
                    if (enumerator.MoveNext())
                    {
                        cache.Add(enumerator.Current); // Append to the shared cache under the lock
                        return true;
                    }

                    // End of source sequence; release the enumerator.
                    enumerator.Dispose();
                    this._parent._enumerator = null;
                    return false;
                }
                catch (Exception ex)
                {
                    this._parent._exception = ExceptionDispatchInfo.Capture(ex);
                    this._parent._exceptionIndex = this._index;
                    enumerator.Dispose();
                    this._parent._enumerator = null;
                    throw;
                }
                finally
                {
                    Monitor.Exit(enumerator);
                }
            }

            /// <summary>
            /// Not supported for caching enumerators.
            /// </summary>
            /// <exception cref="NotSupportedException">Always thrown.</exception>
            public void Reset() => throw new NotSupportedException();
        }
    }
}
