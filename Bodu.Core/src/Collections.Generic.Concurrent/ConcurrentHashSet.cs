// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a thread-safe, unordered set of unique elements.
/// </summary>
/// <typeparam name="T">The type of elements in the set (constraint: <c>where T : notnull</c>).</typeparam>
/// <remarks>
/// <para>
/// <see cref="ConcurrentHashSet{T}" /> is backed by a hand-rolled hash table that uses lock striping: the bucket array
/// is partitioned into a fixed number of regions, each guarded by its own monitor. Mutating a single element locks only
/// the region that owns that element's bucket, so disjoint writers proceed in parallel. The bucket array grows
/// automatically as elements are added; the lock array is allocated once at construction and never changes.
/// </para>
/// <para>
/// The single-element operations <see cref="Add" />, <see cref="Remove" />, and <see cref="Contains" /> are each
/// individually atomic and safe to call concurrently from any number of threads. <see cref="Contains" /> is lock-free —
/// it never blocks a writer and never takes a lock.
/// </para>
/// <para>
/// <see cref="Count" />, <see cref="IsEmpty" />, <see cref="Clear" />, and <see cref="ToArray" /> acquire every
/// internal lock for the duration of the call. They therefore observe (or install) a coherent point-in-time state, but
/// are more expensive than the single-element operations and briefly block all writers.
/// </para>
/// <para>
/// Enumeration iterates over a true snapshot captured when the enumerator is created (via <see cref="ToArray" />) and
/// does not reflect subsequent changes. Unlike enumerators on non-concurrent collections, it never throws
/// <see cref="InvalidOperationException" /> because of concurrent modification.
/// </para>
/// <para>
/// The set implements the full <see cref="ISet{T}" /> contract. The bulk set-algebra operations it adds (
/// <see cref="UnionWith" />, <see cref="IntersectWith" />, <see cref="ExceptWith" />,
/// <see cref="SymmetricExceptWith" /> and the subset/superset predicates) are <b>not</b> atomic: they are evaluated as
/// a sequence of individual atomic operations over a point-in-time snapshot, so a concurrent mutation may interleave
/// and the result can reflect a state the set never occupied at any single instant. See each member's remarks for
/// details.
/// </para>
/// <para>
/// The element type is constrained to <c>notnull</c>: both reference types and value types are supported, and
/// <see langword="null" /> is never a valid element. Element equality and hashing use the
/// <see cref="IEqualityComparer{T}" /> supplied at construction, or <see cref="EqualityComparer{T}.Default" /> when
/// none is supplied.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var seen = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
///
/// Parallel.ForEach(urls, url =>
/// {
///     if (seen.Add(url))
///         Process(url); // runs once per distinct URL, even under concurrency
/// });
///
/// Console.WriteLine(seen.Count);
///]]>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ConcurrentHashSetDebugView<>))]
public sealed partial class ConcurrentHashSet<T>
    where T : notnull
{
    /// <summary>
    /// The default initial bucket count used when no capacity hint is supplied.
    /// </summary>
    private const int DefaultCapacity = 31;

    /// <summary>
    /// The fixed array of monitor objects used for lock striping. Bucket <c>b</c> is guarded by
    /// <c>_locks[b % _locks.Length]</c>. Allocated once at construction and never replaced or resized.
    /// </summary>
    private readonly object[] _locks;

    /// <summary>
    /// The equality comparer used to hash and compare elements. Never <see langword="null" />.
    /// </summary>
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>
    /// The current bucket table. Replaced atomically by <see cref="GrowTable" /> and <see cref="Clear" /> while every
    /// lock is held; declared <see langword="volatile" /> so lock-free readers observe a fully published table.
    /// </summary>
    private volatile Tables _tables;

    /// <summary>
    /// The maximum number of elements a single lock region may own before <see cref="Add" /> requests a resize.
    /// Recomputed whenever the bucket table grows.
    /// </summary>
    private int _budget;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty and uses the default
    /// equality comparer.
    /// </summary>
    public ConcurrentHashSet()
        : this(DefaultConcurrencyLevel, DefaultCapacity, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty and uses the specified
    /// equality comparer.
    /// </summary>
    /// <param name="comparer">
    /// The comparer used to hash and compare elements, or <see langword="null" /> to use the default comparer.
    /// </param>
    public ConcurrentHashSet(IEqualityComparer<T>? comparer)
        : this(DefaultConcurrencyLevel, DefaultCapacity, comparer)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty and sized for the
    /// specified initial capacity using the default equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of buckets the set can hold before its first resize.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    /// <remarks>
    /// <paramref name="capacity" /> is a sizing hint only. The set grows automatically and is not bounded by this
    /// value.
    /// </remarks>
    public ConcurrentHashSet(int capacity)
        : this(DefaultConcurrencyLevel, capacity, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty, sized for the
    /// specified initial capacity, and uses the specified equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of buckets the set can hold before its first resize.</param>
    /// <param name="comparer">
    /// The comparer used to hash and compare elements, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    /// <remarks>
    /// <paramref name="capacity" /> is a sizing hint only. The set grows automatically and is not bounded by this
    /// value.
    /// </remarks>
    public ConcurrentHashSet(int capacity, IEqualityComparer<T>? comparer)
        : this(DefaultConcurrencyLevel, capacity, comparer)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class containing the distinct elements
    /// copied from <paramref name="collection" />, using the default equality comparer.
    /// </summary>
    /// <param name="collection">The collection whose distinct elements are copied into the set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public ConcurrentHashSet(IEnumerable<T> collection)
        : this(collection, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class containing the distinct elements
    /// copied from <paramref name="collection" />, using the specified equality comparer.
    /// </summary>
    /// <param name="collection">The collection whose distinct elements are copied into the set.</param>
    /// <param name="comparer">
    /// The comparer used to hash and compare elements, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Elements that compare equal under <paramref name="comparer" /> are collapsed to a single entry.
    /// </remarks>
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(DefaultConcurrencyLevel, GetCapacityHint(collection), comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        foreach (T item in collection)
            Add(item);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class with the specified lock striping
    /// level, initial bucket capacity, and equality comparer.
    /// </summary>
    /// <param name="concurrencyLevel">The number of lock stripes to allocate. Must be greater than zero.</param>
    /// <param name="capacity">The initial number of buckets.</param>
    /// <param name="comparer">The element comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is negative, or <paramref name="concurrencyLevel" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// This is the single shared initializer that every public constructor delegates to. The capacity is raised to at
    /// least <paramref name="concurrencyLevel" /> so that every lock guards at least one bucket. It is also visible to
    /// the test assembly so that instances can be constructed with an explicit lock-striping level.
    /// </remarks>
    internal ConcurrentHashSet(int concurrencyLevel, int capacity, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(concurrencyLevel, 0);
        ThrowHelper.ThrowIfNegative(capacity);

        if (capacity < concurrencyLevel)
            capacity = concurrencyLevel;

        var locks = new object[concurrencyLevel];
        for (var i = 0; i < locks.Length; i++)
            locks[i] = new object();

        _locks = locks;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _tables = new Tables(new Node?[capacity], new int[concurrencyLevel]);
        _budget = Math.Max(1, capacity / concurrencyLevel);
    }

    /// <summary>
    /// Gets the default number of lock stripes used by the public constructors. Reads
    /// <see cref="Environment.ProcessorCount" /> and routes through
    /// <see cref="ClampDefaultConcurrencyLevel(int)" /> so the clamp logic can be exercised in isolation.
    /// </summary>
    /// <returns>The clamped default lock striping level for newly created instances.</returns>
    private static int DefaultConcurrencyLevel =>
        ClampDefaultConcurrencyLevel(Environment.ProcessorCount);

    /// <summary>
    /// Clamps a raw processor count to the range used by <see cref="DefaultConcurrencyLevel" />, guaranteeing at
    /// least one lock and at most <see cref="MaxDefaultConcurrencyLevel" /> locks regardless of the host machine.
    /// </summary>
    /// <param name="processorCount">The raw processor count to clamp.</param>
    /// <returns>A value in the range <c>[1, MaxDefaultConcurrencyLevel]</c>.</returns>
    /// <remarks>
    /// Extracted as a static helper so unit tests can exercise the clamp envelope with synthetic processor counts
    /// rather than relying on the machine's actual <see cref="Environment.ProcessorCount" />.
    /// </remarks>
    internal static int ClampDefaultConcurrencyLevel(int processorCount) =>
        Math.Clamp(processorCount, 1, MaxDefaultConcurrencyLevel);

    /// <summary>
    /// The upper bound applied to <see cref="DefaultConcurrencyLevel" /> so that the lock array stays small on
    /// high-core machines where the typical set is also small. Explicit constructors can request a higher level via
    /// the internal initializer.
    /// </summary>
    internal const int MaxDefaultConcurrencyLevel = 32;

    /// <summary>
    /// Gets the equality comparer used to hash and compare elements.
    /// </summary>
    /// <returns>The active equality comparer.</returns>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of buckets in the current internal table. Exposed to the test assembly so that table-sizing
    /// invariants can be asserted directly.
    /// </summary>
    /// <returns>The length of the current bucket array.</returns>
    internal int BucketCount => _tables._buckets.Length;

    /// <summary>
    /// Gets the number of stripe locks that guard the table. Exposed to the test assembly so that table-sizing
    /// invariants can be asserted directly.
    /// </summary>
    /// <returns>The fixed number of stripe locks allocated at construction.</returns>
    internal int LockCount => _locks.Length;

    /// <summary>
    /// Gets the number of elements currently contained in the set.
    /// </summary>
    /// <value>The element count observed at the moment of the call.</value>
    /// <returns>The number of elements in the set when the call was made.</returns>
    /// <remarks>
    /// Reading this property acquires every internal lock, yielding an exact point-in-time count. Under concurrency the
    /// returned value may already be stale by the time the caller inspects it.
    /// </remarks>
    public int Count
    {
        get
        {
            var locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                return GetCountNoLocks();
            }
            finally
            {
                ReleaseLocks(locksAcquired);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the set is empty.
    /// </summary>
    /// <value><see langword="true" /> if the set contains no elements; otherwise, <see langword="false" />.</value>
    /// <returns>
    /// <see langword="true" /> if the set was empty when the call was made; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Reading this property acquires every internal lock. It is cheaper than comparing <see cref="Count" /> against
    /// zero because it stops at the first non-empty lock region.
    /// </remarks>
    public bool IsEmpty
    {
        get
        {
            var locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int[] countPerLock = _tables._countPerLock;
                for (var i = 0; i < countPerLock.Length; i++)
                {
                    if (countPerLock[i] != 0)
                        return false;
                }

                return true;
            }
            finally
            {
                ReleaseLocks(locksAcquired);
            }
        }
    }

    /// <summary>
    /// Adds the specified element to the set.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns>
    /// <see langword="true" /> if the element was added; <see langword="false" /> if an equal element was already
    /// present.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is atomic. It locks only the stripe that owns the element's bucket, so it does not block writers
    /// targeting other stripes.
    /// </remarks>
    public bool Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int hashCode = _comparer.GetHashCode(item);

        while (true)
        {
            Tables tables = _tables;
            GetBucketAndLockNo(hashCode, out int bucketNo, out int lockNo, tables._buckets.Length);

            var resizeDesired = false;
            lock (_locks[lockNo])
            {
                // A concurrent resize may have published a new table while this thread waited for the lock.
                if (tables != _tables)
                    continue;

                for (Node? node = tables._buckets[bucketNo]; node is not null; node = node._next)
                {
                    if (hashCode == node._hashCode && _comparer.Equals(node._item, item))
                        return false;
                }

                var created = new Node(item, hashCode, tables._buckets[bucketNo]);
                Volatile.Write(ref tables._buckets[bucketNo], created);

                checked
                {
                    tables._countPerLock[lockNo]++;
                }

                resizeDesired = tables._countPerLock[lockNo] > _budget;
            }

            // Resize outside the stripe lock so GrowTable can acquire all locks in a consistent order.
            if (resizeDesired)
                GrowTable(tables);

            return true;
        }
    }

    /// <summary>
    /// Removes the specified element from the set.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns>
    /// <see langword="true" /> if the element was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is atomic. It locks only the stripe that owns the element's bucket.
    /// </remarks>
    public bool Remove(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int hashCode = _comparer.GetHashCode(item);

        while (true)
        {
            Tables tables = _tables;
            GetBucketAndLockNo(hashCode, out int bucketNo, out int lockNo, tables._buckets.Length);

            lock (_locks[lockNo])
            {
                if (tables != _tables)
                    continue;

                Node? previous = null;
                for (Node? node = tables._buckets[bucketNo]; node is not null; node = node._next)
                {
                    if (hashCode == node._hashCode && _comparer.Equals(node._item, item))
                    {
                        // Unlink the node. The removed node's _next is left intact so a lock-free reader
                        // currently positioned on it can still walk forward to the rest of the chain.
                        if (previous is null)
                            Volatile.Write(ref tables._buckets[bucketNo], node._next);
                        else
                            previous._next = node._next;

                        tables._countPerLock[lockNo]--;
                        return true;
                    }

                    previous = node;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Determines whether the set contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>
    /// <see langword="true" /> if an equal element was present during the call; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is lock-free: it walks the bucket chain through volatile reads and never blocks or is blocked by
    /// a concurrent writer.
    /// </remarks>
    public bool Contains(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int hashCode = _comparer.GetHashCode(item);
        Tables tables = _tables;
        int bucketNo = GetBucket(hashCode, tables._buckets.Length);

        for (Node? node = Volatile.Read(ref tables._buckets[bucketNo]); node is not null; node = node._next)
        {
            if (hashCode == node._hashCode && _comparer.Equals(node._item, item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    /// <remarks>
    /// This operation acquires every internal lock and installs a fresh, empty bucket table, so it is atomic with
    /// respect to all other operations. The replacement table keeps at least as many buckets as there are lock stripes,
    /// preserving the invariant that every stripe guards at least one bucket.
    /// </remarks>
    public void Clear()
    {
        var locksAcquired = 0;
        try
        {
            AcquireAllLocks(ref locksAcquired);

            // Retain the current bucket count so a previously grown set is not forced to immediately regrow, and
            // never drop below the lock count or a stripe would guard zero buckets.
            int bucketCount = Math.Max(_tables._buckets.Length, _locks.Length);
            _tables = new Tables(new Node?[bucketCount], new int[_locks.Length]);
            _budget = Math.Max(1, bucketCount / _locks.Length);
        }
        finally
        {
            ReleaseLocks(locksAcquired);
        }
    }

    /// <summary>
    /// Copies the elements of the set into a new array.
    /// </summary>
    /// <returns>
    /// A new array containing a coherent point-in-time snapshot of the set's elements, or an empty array if the set is
    /// empty. The order of elements is unspecified.
    /// </returns>
    /// <remarks>
    /// This method acquires every internal lock for the duration of the copy, so the returned array reflects the set's
    /// contents at a single instant and is unaffected by concurrent modifications made afterward.
    /// </remarks>
    public T[] ToArray()
    {
        var locksAcquired = 0;
        try
        {
            AcquireAllLocks(ref locksAcquired);

            int count = GetCountNoLocks();
            if (count == 0)
                return [];

            var array = new T[count];
            var index = 0;
            Node?[] buckets = _tables._buckets;
            for (var i = 0; i < buckets.Length; i++)
            {
                for (Node? node = buckets[i]; node is not null; node = node._next)
                    array[index++] = node._item;
            }

            return array;
        }
        finally
        {
            ReleaseLocks(locksAcquired);
        }
    }

    /// <summary>
    /// Doubles the size of the bucket table and rehashes every element into it.
    /// </summary>
    /// <param name="tables">The table generation observed by the caller that requested the resize.</param>
    /// <remarks>
    /// <para>
    /// The method acquires every internal lock, then verifies that <paramref name="tables" /> is still current — if
    /// another thread has already resized, it returns without doing further work.
    /// </para>
    /// <para>
    /// A new bucket array is allocated and every element is copied into a freshly created <see cref="Node" />. The old
    /// nodes are left untouched so that lock-free readers still walking the previous table observe a consistent chain.
    /// </para>
    /// </remarks>
    private void GrowTable(Tables tables)
    {
        var locksAcquired = 0;
        try
        {
            AcquireAllLocks(ref locksAcquired);

            // Another thread may already have grown the table while this thread waited for the locks.
            if (tables != _tables)
                return;

            var approxCount = 0;
            for (var i = 0; i < tables._countPerLock.Length; i++)
                approxCount += tables._countPerLock[i];

            // A sparsely populated table that still tripped the budget indicates a poor hash distribution rather
            // than genuine growth. Doubling the budget avoids a resize that would not relieve the clustering, while
            // the clamp keeps the doubling from overflowing once the budget approaches its maximum.
            if (approxCount < tables._buckets.Length / 4)
            {
                _budget = _budget > int.MaxValue / 2 ? int.MaxValue : _budget * 2;
                return;
            }

            // Grow to roughly double, kept odd and not a multiple of three for a better modulo distribution.
            int newBucketCount;
            try
            {
                checked
                {
                    newBucketCount = (tables._buckets.Length * 2) + 1;
                    while (newBucketCount % 3 == 0)
                        newBucketCount += 2;
                }
            }
            catch (OverflowException)
            {
                _budget = int.MaxValue;
                return;
            }

            if ((uint)newBucketCount > Array.MaxLength)
            {
                newBucketCount = Array.MaxLength;
                _budget = int.MaxValue;
            }

            var newBuckets = new Node?[newBucketCount];
            var newCountPerLock = new int[_locks.Length];

            for (var i = 0; i < tables._buckets.Length; i++)
            {
                Node? current = tables._buckets[i];
                while (current is not null)
                {
                    Node? next = current._next;
                    int newBucketNo = GetBucket(current._hashCode, newBucketCount);
                    newBuckets[newBucketNo] = new Node(current._item, current._hashCode, newBuckets[newBucketNo]);

                    checked
                    {
                        newCountPerLock[newBucketNo % _locks.Length]++;
                    }

                    current = next;
                }
            }

            if (_budget != int.MaxValue)
                _budget = Math.Max(1, newBucketCount / _locks.Length);

            _tables = new Tables(newBuckets, newCountPerLock);
        }
        finally
        {
            ReleaseLocks(locksAcquired);
        }
    }

    /// <summary>
    /// Acquires every stripe lock in ascending index order.
    /// </summary>
    /// <param name="locksAcquired">
    /// A running count of locks successfully entered, incremented as each lock is taken so the caller's
    /// <see langword="finally" /> block can release exactly the locks that were acquired.
    /// </param>
    private void AcquireAllLocks(ref int locksAcquired)
    {
        for (var i = 0; i < _locks.Length; i++)
        {
            var lockTaken = false;
            try
            {
                Monitor.Enter(_locks[i], ref lockTaken);
            }
            finally
            {
                if (lockTaken)
                    locksAcquired++;
            }
        }
    }

    /// <summary>
    /// Releases the first <paramref name="locksAcquired" /> stripe locks.
    /// </summary>
    /// <param name="locksAcquired">The number of stripe locks, starting at index zero, to release.</param>
    private void ReleaseLocks(int locksAcquired)
    {
        for (var i = 0; i < locksAcquired; i++)
            Monitor.Exit(_locks[i]);
    }

    /// <summary>
    /// Returns the total element count by summing the per-lock counters. The caller must already hold every lock.
    /// </summary>
    /// <returns>The number of elements currently stored across all buckets.</returns>
    private int GetCountNoLocks()
    {
        var count = 0;
        int[] countPerLock = _tables._countPerLock;
        for (var i = 0; i < countPerLock.Length; i++)
        {
            checked
            {
                count += countPerLock[i];
            }
        }

        return count;
    }

    /// <summary>
    /// Maps a hash code to a bucket index.
    /// </summary>
    /// <param name="hashCode">The element hash code.</param>
    /// <param name="bucketCount">The number of buckets in the table.</param>
    /// <returns>The zero-based bucket index for <paramref name="hashCode" />.</returns>
    private static int GetBucket(int hashCode, int bucketCount) =>
        (int)((uint)hashCode % (uint)bucketCount);

    /// <summary>
    /// Maps a hash code to its bucket index and the index of the stripe lock that guards that bucket.
    /// </summary>
    /// <param name="hashCode">The element hash code.</param>
    /// <param name="bucketNo">When this method returns, contains the bucket index.</param>
    /// <param name="lockNo">When this method returns, contains the stripe lock index.</param>
    /// <param name="bucketCount">The number of buckets in the table.</param>
    private void GetBucketAndLockNo(int hashCode, out int bucketNo, out int lockNo, int bucketCount)
    {
        bucketNo = (int)((uint)hashCode % (uint)bucketCount);
        lockNo = bucketNo % _locks.Length;
    }

    /// <summary>
    /// Returns a bucket-count hint for sizing the initial table from a source collection.
    /// </summary>
    /// <param name="collection">The source collection, which may be <see langword="null" />.</param>
    /// <returns>
    /// The element count when it can be determined cheaply; otherwise <see cref="DefaultCapacity" />.
    /// </returns>
    private static int GetCapacityHint(IEnumerable<T>? collection) =>
        collection switch
        {
            ICollection<T> generic => generic.Count,
            IReadOnlyCollection<T> readOnly => readOnly.Count,
            _ => DefaultCapacity,
        };
}
