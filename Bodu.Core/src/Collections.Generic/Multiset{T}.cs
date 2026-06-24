// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Multiset{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents an unordered collection that tracks the multiplicity (occurrence count) of each element. Unlike
/// <see cref="HashSet{T}" />, duplicate elements are permitted; unlike <see cref="List{T}" />, element counts are
/// tracked as integers rather than stored as repeated entries.
/// </summary>
/// <typeparam name="T">The type of elements in the multiset. Must not be <see langword="null" />.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="Multiset{T}" /> is backed by a <see cref="Dictionary{TKey, TValue}" /> that maps each distinct element
/// to its occurrence count. Add, remove, and lookup operations are O(1) on average, making this type well suited for
/// frequency analysis, histogram construction, vote counting, and duplicate-aware set arithmetic.
/// </para>
/// <para>
/// The <see cref="Count" /> property returns the total element count <em>including</em> multiplicity. A multiset
/// containing <c>"a"</c> twice and <c>"b"</c> once has a <see cref="Count" /> of 3 and a <see cref="DistinctCount" />
/// of 2. Enumerating the multiset yields each element as many times as it appears; use <see cref="Distinct()" /> to
/// iterate over each distinct element once, or <see cref="Frequencies()" /> to obtain element–count pairs.
/// </para>
/// <para>
/// Set-theoretic operations (<see cref="Union" />, <see cref="Intersect" />, <see cref="Except" />, <see cref="Sum" />)
/// return new <see cref="Multiset{T}" /> instances and do not mutate either operand. The semantics follow multiset
/// algebra:
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Union" /> — each element appears max(a, b) times.</description>
/// </item>
/// <item>
/// <description>
/// <see cref="Intersect" /> — each element appears min(a, b) times; elements absent from either operand are omitted.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Except" /> — each element appears max(0, a − b) times; elements whose count reaches zero are omitted.
/// </description>
/// </item>
/// <item>
/// <description><see cref="Sum" /> — each element appears a + b times.</description>
/// </item>
/// </list>
/// <para>
/// Set operations produce correct results only when both operands use equivalent equality comparers. The result uses
/// this instance's comparer.
/// </para>
/// <para>
/// <see cref="Multiset{T}" /> is not thread-safe. Concurrent reads and writes require external synchronization.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Letter-frequency histogram.
/// var histogram = new Multiset<char>();
/// foreach (char c in "mississippi")
///     histogram.Add(c);
///
/// Console.WriteLine(histogram.Count);         // 11 — total occurrences
/// Console.WriteLine(histogram.DistinctCount); // 4  — distinct letters
/// Console.WriteLine(histogram["s"[0]]);       // 4  — count for 's'
///
/// foreach (KeyValuePair<char, int> kv in histogram.Frequencies())
///     Console.WriteLine($"{kv.Key}: {kv.Value}");
///
/// // Multiset algebra — combine two histograms without mutating either operand.
/// var other = new Multiset<char> { 'i', 'i', 's' };
/// Multiset<char> combined = histogram.Sum(other);
///]]>
/// </example>
[DebuggerDisplay("Count = {Count}, DistinctCount = {DistinctCount}")]
[DebuggerTypeProxy(typeof(MultisetDebugView<>))]
[Serializable]
public sealed partial class Multiset<T>
    : ICollection<T>, IReadOnlyCollection<T>
    where T : notnull
{
    /// <summary>The equality comparer used to determine element equality.</summary>
    private readonly IEqualityComparer<T> _comparer;

    /// <summary>The backing dictionary mapping each distinct element to its occurrence count.</summary>
    private readonly Dictionary<T, int> _items;

    /// <summary>The total number of elements, counting all occurrences.</summary>
    private int _count;

    /// <summary>Incremented on every structural change; used by enumerators to detect concurrent modification.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="Multiset{T}" /> class that is empty and uses the default equality
    /// comparer.
    /// </summary>
    public Multiset()
        : this((IEqualityComparer<T>?)null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Multiset{T}" /> class that is empty and uses the specified equality
    /// comparer.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer used to determine element equality. If <see langword="null" />, the default equality
    /// comparer for <typeparamref name="T" /> is used.
    /// </param>
    public Multiset(IEqualityComparer<T>? comparer)
    {
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _items = new Dictionary<T, int>(_comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Multiset{T}" /> class that contains elements copied from the
    /// specified collection and uses the default equality comparer.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public Multiset(IEnumerable<T> collection)
        : this(collection, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Multiset{T}" /> class that contains elements copied from the
    /// specified collection and uses the specified equality comparer.
    /// </summary>
    /// <param name="collection">
    /// The collection from which elements are copied. Must not be <see langword="null" />.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer used to determine element equality. If <see langword="null" />, the default equality
    /// comparer for <typeparamref name="T" /> is used.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public Multiset(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        _comparer = comparer ?? EqualityComparer<T>.Default;
        _items = new Dictionary<T, int>(_comparer);

        foreach (T item in collection)
            InsertCore(item, 1);
    }

    /// <summary>
    /// Gets the equality comparer used to determine equality of elements in this <see cref="Multiset{T}" />.
    /// </summary>
    /// <value>The <see cref="IEqualityComparer{T}" /> instance used to compare elements.</value>
    public IEqualityComparer<T> Comparer => _comparer;

    /// <summary>
    /// Gets the total number of elements in the <see cref="Multiset{T}" />, counting each duplicate occurrence
    /// separately.
    /// </summary>
    /// <value>
    /// The total element count including multiplicity. For the count of distinct elements, use
    /// <see cref="DistinctCount" />.
    /// </value>
    public int Count => _count;

    /// <summary>
    /// Gets the number of distinct elements in the <see cref="Multiset{T}" />, regardless of their individual
    /// occurrence counts.
    /// </summary>
    /// <value>
    /// The number of unique elements. A multiset containing <c>"a"</c> three times and <c>"b"</c> once has a
    /// <see cref="DistinctCount" /> of 2 and a <see cref="Count" /> of 4.
    /// </value>
    public int DistinctCount => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the <see cref="Multiset{T}" /> is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />; <see cref="Multiset{T}" /> is always mutable.</value>
    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Adds one occurrence of <paramref name="item" /> to the <see cref="Multiset{T}" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    public void Add(T item)
    {
        InsertCore(item, 1);
        _version++;
    }

    /// <summary>
    /// Adds <paramref name="count" /> occurrences of <paramref name="item" /> to the <see cref="Multiset{T}" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <param name="count">The number of occurrences to add. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than or equal to zero.
    /// </exception>
    public void Add(T item, int count)
    {
        ThrowHelper.ThrowIfZeroOrNegative(count);

        InsertCore(item, count);
        _version++;
    }

    /// <summary>
    /// Removes all elements from the <see cref="Multiset{T}" />.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Determines whether the <see cref="Multiset{T}" /> contains at least one occurrence of <paramref name="item" />.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="item" /> occurs one or more times in the multiset; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Contains(T item) =>
        _items.ContainsKey(item);

    /// <summary>
    /// Returns the number of times <paramref name="item" /> appears in the <see cref="Multiset{T}" />.
    /// </summary>
    /// <param name="item">The element whose occurrence count is to be returned.</param>
    /// <returns>
    /// The number of times <paramref name="item" /> appears in the multiset, or <c>0</c> if the element is absent.
    /// </returns>
    public int CountOf(T item) =>
        _items.TryGetValue(item, out int count) ? count : 0;

    /// <summary>
    /// Copies all elements of the <see cref="Multiset{T}" /> to a one-dimensional array, starting at the specified
    /// array index. Each element is copied as many times as its occurrence count.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The zero-based starting index in <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than zero.</exception>
    /// <exception cref="ArgumentException">
    /// The number of elements in the multiset exceeds the available space from <paramref name="arrayIndex" /> to the
    /// end of <paramref name="array" />.
    /// </exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(arrayIndex);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex + _count);

        foreach (KeyValuePair<T, int> pair in _items)
        {
            for (int i = 0; i < pair.Value; i++)
                array[arrayIndex++] = pair.Key;
        }
    }

    /// <summary>
    /// Removes one occurrence of <paramref name="item" /> from the <see cref="Multiset{T}" />.
    /// </summary>
    /// <param name="item">The element to remove one occurrence of.</param>
    /// <returns>
    /// <see langword="true" /> if an occurrence of <paramref name="item" /> was found and removed;
    /// <see langword="false" /> if the element does not appear in the multiset.
    /// </returns>
    public bool Remove(T item)
    {
        if (!_items.TryGetValue(item, out int count))
            return false;

        if (count == 1)
            _items.Remove(item);
        else
            _items[item] = count - 1;

        _count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Removes all occurrences of <paramref name="item" /> from the <see cref="Multiset{T}" />.
    /// </summary>
    /// <param name="item">The element whose occurrences are to be removed.</param>
    /// <returns>
    /// <see langword="true" /> if one or more occurrences of <paramref name="item" /> were removed;
    /// <see langword="false" /> if the element does not appear in the multiset.
    /// </returns>
    public bool RemoveAll(T item)
    {
        if (!_items.TryGetValue(item, out int count))
            return false;

        _items.Remove(item);
        _count -= count;
        _version++;
        return true;
    }

    /// <summary>
    /// Returns an enumerable sequence that yields each distinct element in the <see cref="Multiset{T}" /> exactly once,
    /// regardless of its occurrence count.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> that yields each distinct element once. The order of elements is not
    /// guaranteed.
    /// </returns>
    /// <remarks>
    /// This method enumerates only the distinct elements in O(<see cref="DistinctCount" />) time, avoiding the O(
    /// <see cref="Count" />) cost of iterating the full multiset then de-duplicating. The sequence is invalidated by
    /// any structural modification; iterating after a modification throws <see cref="InvalidOperationException" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The multiset was modified after enumeration began.</exception>
    public IEnumerable<T> Distinct()
    {
        int version = _version;
        foreach (T key in _items.Keys)
        {
            yield return version != _version ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified) : key;
        }
    }

    /// <summary>
    /// Returns an enumerable sequence of element–count pairs for each distinct element in the
    /// <see cref="Multiset{T}" />.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="KeyValuePair{TKey, TValue}" /> where each key is a distinct
    /// element and each value is that element's occurrence count. The order of pairs is not guaranteed.
    /// </returns>
    /// <remarks>
    /// The sequence is invalidated by any structural modification; iterating after a modification throws
    /// <see cref="InvalidOperationException" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The multiset was modified after enumeration began.</exception>
    public IEnumerable<KeyValuePair<T, int>> Frequencies()
    {
        int version = _version;
        foreach (KeyValuePair<T, int> pair in _items)
        {
            yield return version != _version ? throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified) : pair;
        }
    }

    /// <summary>
    /// Returns a new <see cref="Multiset{T}" /> containing the multiset union of this instance and
    /// <paramref name="other" />. Each element appears as many times as the maximum of its counts in either operand.
    /// </summary>
    /// <param name="other">The multiset to compute the union with. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new <see cref="Multiset{T}" /> using this instance's comparer in which each element <c>x</c> has count
    /// max(CountOf(x), other.CountOf(x)).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public Multiset<T> Union(Multiset<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        var result = new Multiset<T>(_comparer);
        foreach (KeyValuePair<T, int> pair in _items)
            result.InsertCore(pair.Key, pair.Value);

        foreach (KeyValuePair<T, int> pair in other._items)
        {
            result._items.TryGetValue(pair.Key, out int existing);
            if (pair.Value > existing)
            {
                result._items[pair.Key] = pair.Value;
                result._count += pair.Value - existing;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a new <see cref="Multiset{T}" /> containing the multiset intersection of this instance and
    /// <paramref name="other" />. Each element appears as many times as the minimum of its counts in both operands;
    /// elements absent from either operand are omitted.
    /// </summary>
    /// <param name="other">The multiset to compute the intersection with. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new <see cref="Multiset{T}" /> using this instance's comparer in which each element <c>x</c> has count
    /// min(CountOf(x), other.CountOf(x)), with elements at zero count omitted.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public Multiset<T> Intersect(Multiset<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        var result = new Multiset<T>(_comparer);
        foreach (KeyValuePair<T, int> pair in _items)
        {
            int count = Math.Min(pair.Value, other.CountOf(pair.Key));
            if (count > 0)
                result.InsertCore(pair.Key, count);
        }

        return result;
    }

    /// <summary>
    /// Returns a new <see cref="Multiset{T}" /> containing the multiset difference of this instance minus
    /// <paramref name="other" />. Each element appears max(0, CountOf(x) − other.CountOf(x)) times; elements whose
    /// resulting count is zero are omitted.
    /// </summary>
    /// <param name="other">The multiset to subtract. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new <see cref="Multiset{T}" /> using this instance's comparer in which each element <c>x</c> has count max(0,
    /// CountOf(x) − other.CountOf(x)).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public Multiset<T> Except(Multiset<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        var result = new Multiset<T>(_comparer);
        foreach (KeyValuePair<T, int> pair in _items)
        {
            int count = Math.Max(0, pair.Value - other.CountOf(pair.Key));
            if (count > 0)
                result.InsertCore(pair.Key, count);
        }

        return result;
    }

    /// <summary>
    /// Returns a new <see cref="Multiset{T}" /> containing the multiset sum of this instance and
    /// <paramref name="other" />. Each element appears CountOf(x) + other.CountOf(x) times.
    /// </summary>
    /// <param name="other">The multiset to sum with. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new <see cref="Multiset{T}" /> using this instance's comparer in which each element <c>x</c> has count
    /// CountOf(x) + other.CountOf(x).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public Multiset<T> Sum(Multiset<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        var result = new Multiset<T>(_comparer);
        foreach (KeyValuePair<T, int> pair in _items)
            result.InsertCore(pair.Key, pair.Value);

        foreach (KeyValuePair<T, int> pair in other._items)
            result.InsertCore(pair.Key, pair.Value);

        return result;
    }

    /// <summary>
    /// Inserts <paramref name="count" /> occurrences of <paramref name="item" /> into the backing dictionary without
    /// incrementing <see cref="_version" />. Used by constructors and set-operation builders where no enumerators exist
    /// for the target instance.
    /// </summary>
    /// <param name="item">The element to insert.</param>
    /// <param name="count">The number of occurrences to add.</param>
    private void InsertCore(T item, int count)
    {
        _items[item] = _items.TryGetValue(item, out int existing) ? existing + count : count;
        _count += count;
    }
}
