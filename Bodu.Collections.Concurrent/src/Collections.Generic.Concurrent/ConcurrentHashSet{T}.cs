// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a thread-safe, unordered set of unique elements.
/// </summary>
/// <typeparam name="T">The type of elements in the set (constraint: <c>where T : notnull</c>).</typeparam>
/// <remarks>
/// <para>
/// <see cref="ConcurrentHashSet{T}" /> is a <b>lock-free</b> hash set built on a split-ordered list (Shalev &amp;
/// Shavit): every element lives in a single Harris–Michael lock-free ordered linked list, sorted by the bit-reversed
/// hash code of the element. The hash-table part is only an array of lazily initialized <i>shortcut</i> pointers into
/// that list — one sentinel node per bucket. Doubling the table copies shortcut pointers but never rehashes or moves a
/// node, so growth is itself lock-free and readers are never invalidated by a resize.
/// </para>
/// <para>
/// No operation on this type ever takes a lock or blocks another thread. <see cref="Add" />, <see cref="Remove" />,
/// <see cref="Contains" />, <see cref="Clear" />, <see cref="Count" />, and <see cref="ToArray" /> are all lock-free: a
/// suspended or preempted thread can never prevent other threads from completing their operations. Mutations are
/// applied with single-reference compare-and-swap operations; deletion uses the standard two-phase Harris scheme
/// (logical marking, then cooperative physical unlinking).
/// </para>
/// <para>
/// <see cref="Add" />, <see cref="Remove" />, and <see cref="Contains" /> are each individually atomic (linearizable)
/// and safe to call concurrently from any number of threads. <see cref="Count" /> and <see cref="IsEmpty" /> read a
/// counter maintained with interlocked operations — exact whenever no mutation is in flight, and never off by more than
/// the operations currently executing. <see cref="Clear" /> atomically replaces the entire backing structure in one
/// compare-and-swap; operations that began against the previous structure complete against it and are ordered before
/// the clear.
/// </para>
/// <para>
/// <see cref="ToArray" /> and enumeration are <b>weakly consistent</b> rather than point-in-time snapshots: the
/// traversal observes every element that is present for the entire duration of the call, never yields the same element
/// twice, and never throws — but elements added or removed while the traversal is in progress may or may not be
/// observed, and the result is not guaranteed to correspond to the set's state at any single instant.
/// </para>
/// <para>
/// Performance characteristics differ from lock-based designs: progress is guaranteed without blocking, but every
/// successful <see cref="Add" /> allocates one list node and every successful <see cref="Remove" /> allocates one
/// short-lived marker (reclaimed by the garbage collector once unlinked). <see cref="Contains" /> is allocation-free.
/// Elements that share a hash code form a single linearly scanned run, so — as with any hash set — a comparer with
/// poor hash distribution degrades lookups toward linear time.
/// </para>
/// <para>
/// The set implements the full <see cref="ISet{T}" /> contract. The bulk set-algebra operations it adds (
/// <see cref="UnionWith" />, <see cref="IntersectWith" />, <see cref="ExceptWith" />,
/// <see cref="SymmetricExceptWith" /> and the subset/superset predicates) are <b>not</b> atomic: they are evaluated as
/// a sequence of individual atomic operations over a weakly consistent snapshot, so a concurrent mutation may
/// interleave and the result can reflect a state the set never occupied at any single instant. See each member's
/// remarks for details.
/// </para>
/// <para>
/// The element type is constrained to <c>notnull</c>: both reference types and value types are supported, and
/// <see langword="null" /> is never a valid element. Element equality and hashing use the
/// <see cref="IEqualityComparer{T}" /> supplied at construction, or <see cref="EqualityComparer{T}.Default" /> when
/// none is supplied.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
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
/// </code>
/// </example>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ConcurrentHashSetDebugView<>))]
public sealed partial class ConcurrentHashSet<T>
    where T : notnull
{
    /// <summary>The bucket-shortcut array length used when no capacity hint is supplied. Always a power of two.</summary>
    private const int DefaultBucketCount = 32;

    /// <summary>The smallest bucket-shortcut array length a constructor will allocate, so tiny capacity hints do not cause an immediate cascade of doublings.</summary>
    private const int MinBucketCount = 8;

    /// <summary>The largest bucket-shortcut array length — the greatest power of two representable in an <see cref="int" /> length. Once reached, the table stops doubling and equal-prefix runs simply lengthen.</summary>
    private const int MaxBucketCount = 1 << 30;

    /// <summary>The average number of elements per bucket tolerated before <see cref="Add" /> requests a doubling of the shortcut array.</summary>
    private const int MaxLoadFactor = 2;

    /// <summary>The equality comparer used to hash and compare elements. Never <see langword="null" />.</summary>
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>The current generation: the split-ordered list, its bucket shortcuts, and its element counter. Read via <see cref="Volatile" /> and replaced only by the <see cref="Clear" /> compare-and-swap.</summary>
    private Core _core;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty and uses the default
    /// equality comparer.
    /// </summary>
    public ConcurrentHashSet()
        : this(DefaultBucketCount, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty and uses the specified
    /// equality comparer.
    /// </summary>
    /// <param name="comparer">
    /// The comparer used to hash and compare elements, or <see langword="null" /> to use the default comparer.
    /// </param>
    public ConcurrentHashSet(IEqualityComparer<T>? comparer)
        : this(DefaultBucketCount, comparer)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty and sized for the
    /// specified initial capacity using the default equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of buckets the set can hold before its first resize.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    /// <remarks>
    /// <paramref name="capacity" /> is a sizing hint only: it is rounded up to a power of two and the set grows
    /// automatically, so it is not bounded by this value.
    /// </remarks>
    public ConcurrentHashSet(int capacity)
        : this(capacity, null)
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
    /// Initializes a new instance of the <see cref="ConcurrentHashSet{T}" /> class that is empty, sized for the
    /// specified initial capacity, and uses the specified equality comparer.
    /// </summary>
    /// <param name="capacity">The initial number of buckets the set can hold before its first resize.</param>
    /// <param name="comparer">
    /// The comparer used to hash and compare elements, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is negative.</exception>
    /// <remarks>
    /// <paramref name="capacity" /> is a sizing hint only: it is rounded up to a power of two and the set grows
    /// automatically, so it is not bounded by this value.
    /// </remarks>
    public ConcurrentHashSet(int capacity, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        _comparer = comparer ?? EqualityComparer<T>.Default;
        _core = new Core(NormalizeBucketCount(capacity));
    }

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
        : this(GetCapacityHint(collection), comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        foreach (T item in collection)
            Add(item);
    }

    /// <summary>
    /// Gets the element count without any locking. Retained for source compatibility — on this lock-free implementation
    /// it is identical to <see cref="Count" />.
    /// </summary>
    /// <value>The same value as <see cref="Count" />.</value>
    /// <remarks>
    /// Earlier lock-striped versions of this type distinguished a cheap approximate count from an exact count that
    /// blocked writers. Every read is now a single lock-free counter read, so the distinction is vacuous; prefer
    /// <see cref="Count" /> in new code.
    /// </remarks>
    public int ApproximateCount => Count;

    /// <summary>
    /// Gets the equality comparer used to hash and compare elements.
    /// </summary>
    /// <value>The active equality comparer.</value>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the number of elements currently contained in the set.
    /// </summary>
    /// <value>The element count observed at the moment of the call.</value>
    /// <remarks>
    /// Reading this property is lock-free — a single interlocked counter read that never blocks writers. The value is
    /// exact whenever no mutation is concurrently in flight; while <see cref="Add" /> or <see cref="Remove" /> calls
    /// are executing on other threads the value may transiently lag those operations, and it may already be stale by
    /// the time the caller inspects it.
    /// </remarks>
    public int Count
    {
        get
        {
            Core core = Volatile.Read(ref _core);

            // A remover can decrement before a racing adder's increment lands, so the counter may transiently dip
            // below zero; clamp rather than surface the intermediate state.
            return Math.Max(0, Volatile.Read(ref core._count));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the set is empty.
    /// </summary>
    /// <value><see langword="true" /> if the set contains no elements; otherwise, <see langword="false" />.</value>
    /// <remarks>
    /// Reading this property is lock-free and equivalent to comparing <see cref="Count" /> against zero. Under
    /// concurrent mutation the answer may be stale by the time the caller inspects it.
    /// </remarks>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Gets a value indicating whether the set is empty, without any locking. Retained for source compatibility — on
    /// this lock-free implementation it is identical to <see cref="IsEmpty" />.
    /// </summary>
    /// <value>The same value as <see cref="IsEmpty" />.</value>
    /// <remarks>
    /// Earlier lock-striped versions of this type distinguished a cheap approximate probe from an exact check that
    /// blocked writers. Every read is now a single lock-free counter read, so the distinction is vacuous; prefer
    /// <see cref="IsEmpty" /> in new code.
    /// </remarks>
    public bool IsEmptyApproximate => IsEmpty;

    /// <summary>
    /// Gets the number of buckets in the current shortcut array. Exposed to the test assembly so that table-sizing
    /// invariants can be asserted directly.
    /// </summary>
    /// <value>The length of the current bucket-shortcut array. Always a power of two.</value>
    internal int BucketCount
    {
        get
        {
            Core core = Volatile.Read(ref _core);
            return Volatile.Read(ref core._buckets).Length;
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
    /// This operation is atomic and lock-free: the element is inserted into the split-ordered list with a single
    /// compare-and-swap, so the call never blocks and never prevents other threads from making progress. A lost
    /// compare-and-swap (a concurrent insert or delete at the same position) simply retries from a fresh traversal.
    /// </remarks>
    public bool Add(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int hashCode = _comparer.GetHashCode(item);
        ulong key = MakeDataKey(hashCode);
        SpinWait spinner = default;

        while (true)
        {
            Core core = Volatile.Read(ref _core);
            Node sentinel = GetBucketSentinel(core, hashCode);

            if (Find(sentinel, key, matchData: true, item, out Node pred, out Node? curr))
                return false;

            Node created = Node.CreateData(item, key, next: curr);
            if (ReferenceEquals(Interlocked.CompareExchange(ref pred._next, created, curr), curr))
            {
                int newCount = Interlocked.Increment(ref core._count);

                Node?[] buckets = Volatile.Read(ref core._buckets);
                if (newCount > (long)buckets.Length * MaxLoadFactor)
                    TryGrow(core, buckets);

                return true;
            }

            // The insertion window changed under us (competing insert, delete, or mark at pred) — re-traverse.
            spinner.SpinOnce();
        }
    }

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This operation is lock-free and linearizable: it atomically replaces the entire backing structure (list, bucket
    /// shortcuts, and counter) with a fresh empty one in a single compare-and-swap, which is the operation's
    /// linearization point. Operations that began against the previous structure complete against it and are ordered
    /// before the clear — an <see cref="Add" /> that races the swap either lands in the new structure or is cleared
    /// along with the old one, exactly as if it had completed an instant before the clear.
    /// </para>
    /// <para>
    /// The replacement keeps the current bucket count, so a previously grown set is not forced to immediately regrow.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        SpinWait spinner = default;

        while (true)
        {
            Core core = Volatile.Read(ref _core);
            var replacement = new Core(Volatile.Read(ref core._buckets).Length);

            // CAS rather than a blind write so the replacement's bucket count is derived from the very generation
            // being replaced — a racing grow can never be shrunk away by a stale observation.
            if (ReferenceEquals(Interlocked.CompareExchange(ref _core, replacement, core), core))
                return;

            spinner.SpinOnce();
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
    /// This operation is a pure lock-free read: it walks the split-ordered list through volatile reads, hopping over
    /// logically deleted nodes without helping to unlink them, and never blocks or is blocked by a concurrent writer.
    /// The first lookup that targets an untouched bucket may lazily publish that bucket's shortcut sentinel.
    /// </remarks>
    public bool Contains(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int hashCode = _comparer.GetHashCode(item);
        ulong key = MakeDataKey(hashCode);
        Core core = Volatile.Read(ref _core);
        Node sentinel = GetBucketSentinel(core, hashCode);

        Node? curr = Volatile.Read(ref sentinel._next);
        while (curr is not null)
        {
            Node? succ = Volatile.Read(ref curr._next);

            if (succ is { _isMarker: true })
            {
                // curr is logically deleted — skip it via the marker's frozen successor without helping to unlink.
                curr = succ._next;
                continue;
            }

            if (curr._key > key)
                return false;

            if (curr._key == key && _comparer.Equals(curr._item, item))
                return true;

            curr = succ;
        }

        return false;
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
    /// This operation is atomic and lock-free. Removal is two-phase in the Harris style: the node is first logically
    /// deleted by installing a marker into its successor link with a compare-and-swap (the linearization point), then
    /// physically unlinked best-effort — any traversal that later encounters the marker completes the unlinking
    /// cooperatively.
    /// </remarks>
    public bool Remove(T item)
    {
        ThrowHelper.ThrowIfNull(item);

        int hashCode = _comparer.GetHashCode(item);
        ulong key = MakeDataKey(hashCode);
        SpinWait spinner = default;

        while (true)
        {
            Core core = Volatile.Read(ref _core);
            Node sentinel = GetBucketSentinel(core, hashCode);

            if (!Find(sentinel, key, matchData: true, item, out Node pred, out Node? curr))
                return false;

            Node found = curr!;
            Node? succ = Volatile.Read(ref found._next);

            if (succ is { _isMarker: true })
            {
                // A competing deleter marked the node after Find passed it; retry so the traversal observes the removal.
                spinner.SpinOnce();
                continue;
            }

            // Logical delete: fold the mark into _next so any competing insert-after or second delete fails its CAS.
            if (ReferenceEquals(Interlocked.CompareExchange(ref found._next, Node.CreateMarker(succ), succ), succ))
            {
                Interlocked.Decrement(ref core._count);

                // Best-effort physical unlink; on failure the next traversal through pred cleans up cooperatively.
                Interlocked.CompareExchange(ref pred._next, succ, found);
                return true;
            }

            spinner.SpinOnce();
        }
    }

    /// <summary>
    /// Copies the elements of the set into a new array.
    /// </summary>
    /// <returns>
    /// A new array containing a weakly consistent snapshot of the set's elements, or an empty array if no elements were
    /// observed. The order of elements is unspecified.
    /// </returns>
    /// <remarks>
    /// This method is a pure lock-free traversal of the split-ordered list: it never blocks writers. The result is <b>weakly
    /// consistent</b> — it contains every element that was present for the entire duration of the call and never
    /// contains the same element twice (the traversal is monotonic in split-order, so a concurrently re-added element
    /// cannot be revisited), but elements added or removed while the copy is in progress may or may not appear, and the
    /// array is not guaranteed to reflect the set's state at any single instant.
    /// </remarks>
    public T[] ToArray()
    {
        Core core = Volatile.Read(ref _core);
        var items = new List<T>(Math.Max(0, Volatile.Read(ref core._count)));

        Node? curr = Volatile.Read(ref core._head._next);
        while (curr is not null)
        {
            Node? succ = Volatile.Read(ref curr._next);

            if (succ is { _isMarker: true })
            {
                // curr is logically deleted — skip it via the marker's frozen successor without helping to unlink.
                curr = succ._next;
                continue;
            }

            // Data nodes carry an odd split-order key; bucket sentinels (even keys) are structural and skipped.
            if ((curr._key & 1UL) == 1UL)
                items.Add(curr._item);

            curr = succ;
        }

        return items.Count == 0 ? [] : items.ToArray();
    }

    /// <summary>
    /// Reverses the bit order of a 32-bit value using the classic five-step mask-and-shift network.
    /// </summary>
    /// <param name="value">The value whose bits to reverse.</param>
    /// <returns><paramref name="value" /> with bit 0 exchanged with bit 31, bit 1 with bit 30, and so on.</returns>
    /// <remarks>
    /// Bit reversal is the heart of split-ordering: sorting elements by reversed hash makes each bucket's elements a
    /// contiguous run of the list, and doubling the table splits every run in two without moving a single node.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReverseBits(uint value)
    {
        value = ((value >> 1) & 0x55555555u) | ((value & 0x55555555u) << 1);
        value = ((value >> 2) & 0x33333333u) | ((value & 0x33333333u) << 2);
        value = ((value >> 4) & 0x0F0F0F0Fu) | ((value & 0x0F0F0F0Fu) << 4);
        value = ((value >> 8) & 0x00FF00FFu) | ((value & 0x00FF00FFu) << 8);

        return (value >> 16) | (value << 16);
    }

    /// <summary>
    /// Computes the split-order key of a data node for the specified hash code.
    /// </summary>
    /// <param name="hashCode">The element's hash code.</param>
    /// <returns>The bit-reversed hash shifted left one position with the low bit set — always odd.</returns>
    /// <remarks>
    /// Widening to 64 bits before setting the low discriminator bit preserves all 32 hash bits (the original 32-bit
    /// formulation of split-ordering sacrifices the top hash bit). Because bit reversal is a bijection, two data keys
    /// are equal exactly when the underlying hash codes are equal.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong MakeDataKey(int hashCode) =>
        ((ulong)ReverseBits((uint)hashCode) << 1) | 1UL;

    /// <summary>
    /// Computes the split-order key of the sentinel node for the specified bucket index.
    /// </summary>
    /// <param name="bucketNo">The bucket index.</param>
    /// <returns>The bit-reversed bucket index shifted left one position — always even.</returns>
    /// <remarks>
    /// A sentinel key is a strict lower bound of every data key that hashes into its bucket: the two share the same
    /// reversed prefix, and the sentinel has zeros below it plus a zero discriminator bit.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong MakeSentinelKey(int bucketNo) =>
        (ulong)ReverseBits((uint)bucketNo) << 1;

    /// <summary>
    /// Rounds a constructor capacity hint to the bucket-count envelope: a power of two between
    /// <see cref="MinBucketCount" /> and <see cref="MaxBucketCount" />.
    /// </summary>
    /// <param name="capacity">The non-negative capacity hint.</param>
    /// <returns>The normalized bucket-shortcut array length.</returns>
    private static int NormalizeBucketCount(int capacity) =>
        (int)BitOperations.RoundUpToPowerOf2((uint)Math.Clamp(capacity, MinBucketCount, MaxBucketCount));

    /// <summary>
    /// Returns a bucket-count hint for sizing the initial table from a source collection.
    /// </summary>
    /// <param name="collection">The source collection, which may be <see langword="null" />.</param>
    /// <returns>
    /// The element count when it can be determined cheaply; otherwise <see cref="DefaultBucketCount" />.
    /// </returns>
    private static int GetCapacityHint(IEnumerable<T>? collection) =>
        collection switch
        {
            ICollection<T> generic => generic.Count,
            IReadOnlyCollection<T> readOnly => readOnly.Count,
            _ => DefaultBucketCount,
        };

    /// <summary>
    /// Doubles the bucket-shortcut array of the specified generation, if no other thread has done so first.
    /// </summary>
    /// <param name="core">The generation whose table to grow.</param>
    /// <param name="observed">The bucket array the caller observed when it decided to grow.</param>
    /// <remarks>
    /// <para>
    /// Growth copies the shortcut pointers into an array of twice the length and publishes it with a single
    /// compare-and-swap against <paramref name="observed" /> — nodes are never rehashed or moved, so concurrent readers
    /// and writers are completely unaffected. Losing the compare-and-swap means another thread already grew the table;
    /// the freshly allocated array is simply discarded.
    /// </para>
    /// <para>
    /// One benign race is accepted by design: a sentinel published into a slot of the old array after that slot has
    /// been copied is absent from the new array. Nothing is lost — the sentinel node itself is already linked into the
    /// list, and the next lookup that targets the bucket re-runs <see cref="InitializeBucket" />, whose traversal finds
    /// the existing sentinel by key and re-publishes the shortcut.
    /// </para>
    /// </remarks>
    private static void TryGrow(Core core, Node?[] observed)
    {
        if (observed.Length >= MaxBucketCount)
            return;

        var grown = new Node?[observed.Length * 2];
        Array.Copy(observed, grown, observed.Length);

        Interlocked.CompareExchange(ref core._buckets, grown, observed);
    }

    /// <summary>
    /// Returns the sentinel node for the bucket that owns the specified hash code, lazily initializing it on first use.
    /// </summary>
    /// <param name="core">The generation to look in.</param>
    /// <param name="hashCode">The element hash code.</param>
    /// <returns>The bucket's sentinel node.</returns>
    /// <remarks>
    /// Bucket 0 is populated with the list head at construction and survives every growth copy, so the recursive
    /// initialization performed for other buckets always bottoms out.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node GetBucketSentinel(Core core, int hashCode)
    {
        Node?[] buckets = Volatile.Read(ref core._buckets);
        int bucketNo = hashCode & (buckets.Length - 1);

        return Volatile.Read(ref buckets[bucketNo]) ?? InitializeBucket(core, buckets, bucketNo);
    }

    /// <summary>
    /// Creates (or adopts) the sentinel node for the specified bucket and publishes it into the shortcut array.
    /// </summary>
    /// <param name="core">The generation that owns <paramref name="buckets" />.</param>
    /// <param name="buckets">The bucket array observed by the caller.</param>
    /// <param name="bucketNo">The bucket index to initialize. Never zero.</param>
    /// <returns>The bucket's sentinel node.</returns>
    /// <remarks>
    /// The parent bucket (the index with its most significant set bit cleared) is initialized first — recursively —
    /// because the parent's sentinel is the nearest guaranteed entry point that precedes this bucket's position in
    /// split-order. Insertion is idempotent under races: the loser of the list compare-and-swap re-traverses, finds the
    /// winner's sentinel by its unique even key, and adopts it, so exactly one sentinel node ever represents a bucket
    /// within a generation.
    /// </remarks>
    private Node InitializeBucket(Core core, Node?[] buckets, int bucketNo)
    {
        int parentNo = bucketNo & ((1 << BitOperations.Log2((uint)bucketNo)) - 1);
        Node parent = Volatile.Read(ref buckets[parentNo]) ?? InitializeBucket(core, buckets, parentNo);

        ulong key = MakeSentinelKey(bucketNo);
        SpinWait spinner = default;
        Node sentinel;

        while (true)
        {
            if (Find(parent, key, matchData: false, default!, out Node pred, out Node? curr))
            {
                // A racing initializer already inserted this bucket's sentinel — adopt it.
                sentinel = curr!;
                break;
            }

            Node created = Node.CreateSentinel(key, next: curr);
            if (ReferenceEquals(Interlocked.CompareExchange(ref pred._next, created, curr), curr))
            {
                sentinel = created;
                break;
            }

            spinner.SpinOnce();
        }

        Interlocked.CompareExchange(ref buckets[bucketNo], sentinel, null);

        Node? published = Volatile.Read(ref buckets[bucketNo]);
        return published!;
    }

    /// <summary>
    /// Traverses the list from <paramref name="start" /> looking for the specified split-order key, unlinking logically
    /// deleted nodes encountered along the way.
    /// </summary>
    /// <param name="start">The node to begin the traversal from — a bucket sentinel or the list head.</param>
    /// <param name="key">The split-order key to locate.</param>
    /// <param name="matchData">
    /// <see langword="true" /> to match a data node for <paramref name="item" /> within the equal-key run;
    /// <see langword="false" /> to match a sentinel by exact key.
    /// </param>
    /// <param name="item">The element to match when <paramref name="matchData" /> is <see langword="true" />.</param>
    /// <param name="pred">When this method returns, the node immediately preceding <paramref name="curr" />.</param>
    /// <param name="curr">
    /// When this method returns <see langword="true" />, the matched node; when it returns <see langword="false" />,
    /// the first node whose key is greater than <paramref name="key" /> (or <see langword="null" /> at the list end) —
    /// together with <paramref name="pred" /> this brackets the insertion window at the end of the equal-key run.
    /// </param>
    /// <returns><see langword="true" /> if a matching node was found; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// This is the Michael-list <c>Find</c>: on encountering a node whose successor link holds a marker, the traversal
    /// helps by swinging the predecessor past the deleted node; if that compare-and-swap fails the predecessor itself
    /// changed, and the traversal restarts from <paramref name="start" />. Restarting from the bucket sentinel rather
    /// than the list head keeps the retry cost proportional to the bucket's run length.
    /// </remarks>
    private bool Find(Node start, ulong key, bool matchData, T item, out Node pred, out Node? curr)
    {
    retry:
        pred = start;
        curr = Volatile.Read(ref pred._next);

        while (true)
        {
            if (curr is null)
                return false;

            Node? succ = Volatile.Read(ref curr._next);

            if (succ is { _isMarker: true })
            {
                // curr is logically deleted — help unlink it, or restart when pred itself has changed.
                if (!ReferenceEquals(Interlocked.CompareExchange(ref pred._next, succ._next, curr), curr))
                    goto retry;

                curr = succ._next;
                continue;
            }

            if (curr._key > key)
                return false;

            // Data keys are odd and sentinel keys even, so an exact key match already implies the right node kind;
            // within an equal-key run (hash collision) the comparer disambiguates the element.
            if (curr._key == key && (!matchData || _comparer.Equals(curr._item, item)))
                return true;

            pred = curr;
            curr = succ;
        }
    }
}
