// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a dictionary that preserves the order in which entries are added and provides O(1) access to and removal
/// of the first and last entries, optionally re-ordering entries on access.
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="SequencedDictionary{TKey, TValue}" /> is the .NET analogue of Java's <c>LinkedHashMap</c>: a hash-based
/// dictionary that additionally maintains a doubly linked list across its entries so that enumeration follows a stable,
/// predictable order. It realizes the same <i>sequenced</i> (encounter-order) contract that Java's <c>SequencedMap</c>
/// defines: a well-defined first and last entry with constant-time access to each.
/// </para>
/// <para>
/// This differs from the BCL's <c>OrderedDictionary&lt;TKey, TValue&gt;</c> (.NET 9+), which is <i>positional</i> — it
/// is index-addressable and supports inserting, overwriting, and removing at an arbitrary index, with O(1) random
/// access by position but O(n) removal of a non-tail entry. <see cref="SequencedDictionary{TKey, TValue}" /> instead
/// exposes <b>no</b> positional surface; it preserves a traversal order and adds O(1) access to and removal of either
/// end, with O(1) removal of any entry by key. (On Bodu's <c>net8.0</c> target the BCL
/// <c>OrderedDictionary&lt;TKey, TValue&gt;</c> is not available at all.)
/// </para>
/// <para>
/// The dictionary supports two ordering modes, selected at construction:
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Insertion order</b> (the default): entries are enumerated in the order they were first added. Reads never change
/// the order, and re-assigning an existing key's value leaves its position unchanged.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Access order</b>: a successful lookup (via <see cref="TryGetValue" /> or the indexer getter) and an indexed value
/// update move the affected entry to the end of the iteration order. This makes the most recently used entry the
/// <see cref="Last" /> entry and the least recently used entry the <see cref="First" /> entry, which is the foundation
/// of a least-recently-used cache built on top of <see cref="TryRemoveFirst" />.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// This type does not bound its size or evict entries; for a fixed-capacity, policy-driven cache use
/// <see cref="EvictingDictionary{TKey, TValue}" /> instead. The <c>capacity</c> constructor argument is only an
/// initial-size hint for the internal hash table.
/// </para>
/// <para>
/// Keys must be non-null (the type parameter is constrained by <see langword="notnull" />). Values may be
/// <see langword="null" /> when <typeparamref name="TValue" /> is a reference type. Custom key equality is supported
/// via <see cref="System.Collections.Generic.IEqualityComparer{T}" />.
/// </para>
/// <para>
/// <see cref="SequencedDictionary{TKey, TValue}" /> is not thread-safe. In access-order mode reads mutate the iteration
/// order, so concurrent reads and writes require external synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Insertion-order dictionary: enumeration follows the order keys were added.
/// var ordered = new SequencedDictionary<string, int>();
/// ordered.Add("A", 1);
/// ordered.Add("B", 2);
/// ordered.Add("C", 3);
///
/// foreach (var kvp in ordered)
///     Console.WriteLine($"{kvp.Key} = {kvp.Value}"); // A, B, C
///
/// // Access-order dictionary: a lookup moves the entry to the end.
/// var lru = new SequencedDictionary<string, int>(accessOrder: true);
/// lru.Add("A", 1);
/// lru.Add("B", 2);
/// _ = lru["A"];           // "A" is now most-recently-used.
/// var oldest = lru.First; // { "B", 2 } — the least-recently-used entry.
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count: {Count}, AccessOrder: {_accessOrder}")]
[DebuggerTypeProxy(typeof(SequencedDictionaryDebugView<,>))]
public partial class SequencedDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The equality comparer used for key identity and hash-table lookup.</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>The backing store mapping each key to its value and ordering node.</summary>
    private readonly Dictionary<TKey, Entry> _store;

    /// <summary>Tracks the iteration order of keys; the head is the first entry and the tail is the last entry.</summary>
    private readonly LinkedList<TKey> _order;

    /// <summary>Indicates whether a successful lookup or indexed update moves the affected entry to the tail.</summary>
    private readonly bool _accessOrder;

    /// <summary>The cached key collection returned by the keys accessor, allocated on first access.</summary>
    private KeyCollection? _keys;

    /// <summary>The cached value collection returned by the values accessor, allocated on first access.</summary>
    private ValueCollection? _values;

    /// <summary>The modification counter used to detect concurrent mutation during enumeration.</summary>
    /// <remarks>
    /// Incremented on every mutation (add, remove, clear, in-place replace, and access-order repositioning) so
    /// enumerators can detect concurrent modification and fail fast rather than produce undefined ordering.
    /// </remarks>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty, uses
    /// insertion ordering, and uses the default key comparer.
    /// </summary>
    public SequencedDictionary()
        : this(0, false, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty, uses the
    /// specified ordering mode, and uses the default key comparer.
    /// </summary>
    /// <param name="accessOrder">
    /// <see langword="true" /> to move an entry to the end of the iteration order whenever it is read or its value is
    /// updated through the indexer; <see langword="false" /> to preserve insertion order.
    /// </param>
    public SequencedDictionary(bool accessOrder)
        : this(0, accessOrder, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty, uses
    /// insertion ordering, and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of entries the internal hash table can hold without resizing.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public SequencedDictionary(int capacity)
        : this(capacity, false, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty, uses the
    /// specified ordering mode, and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of entries the internal hash table can hold without resizing.</param>
    /// <param name="accessOrder">
    /// <see langword="true" /> to move an entry to the end of the iteration order whenever it is read or its value is
    /// updated through the indexer; <see langword="false" /> to preserve insertion order.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public SequencedDictionary(int capacity, bool accessOrder)
        : this(capacity, accessOrder, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty, uses
    /// insertion ordering, and uses the specified key comparer.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    public SequencedDictionary(IEqualityComparer<TKey>? comparer)
        : this(0, false, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty, uses
    /// insertion ordering, and has the specified initial capacity and key comparer.
    /// </summary>
    /// <param name="capacity">The initial number of entries the internal hash table can hold without resizing.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public SequencedDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        : this(capacity, false, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class that is empty and has
    /// the specified initial capacity, ordering mode, and key comparer.
    /// </summary>
    /// <param name="capacity">The initial number of entries the internal hash table can hold without resizing.</param>
    /// <param name="accessOrder">
    /// <see langword="true" /> to move an entry to the end of the iteration order whenever it is read or its value is
    /// updated through the indexer; <see langword="false" /> to preserve insertion order.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
    public SequencedDictionary(int capacity, bool accessOrder, IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfLessThan(capacity, 0);

        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _accessOrder = accessOrder;
        _store = capacity > 0
            ? new Dictionary<TKey, Entry>(capacity, _comparer)
            : new Dictionary<TKey, Entry>(_comparer);
        _order = new LinkedList<TKey>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class with elements copied
    /// from the specified sequence, using insertion ordering and the default key comparer.
    /// </summary>
    /// <param name="collection">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="collection" /> contains one or more duplicate keys.
    /// </exception>
    public SequencedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        : this(collection, false, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class with elements copied
    /// from the specified sequence, using insertion ordering and the specified key comparer.
    /// </summary>
    /// <param name="collection">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="collection" /> contains one or more duplicate keys.
    /// </exception>
    public SequencedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
        : this(collection, false, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequencedDictionary{TKey, TValue}" /> class with elements copied
    /// from the specified sequence, using the specified ordering mode and key comparer.
    /// </summary>
    /// <param name="collection">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="accessOrder">
    /// <see langword="true" /> to move an entry to the end of the iteration order whenever it is read or its value is
    /// updated through the indexer; <see langword="false" /> to preserve insertion order.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="collection" /> contains one or more duplicate keys.
    /// </exception>
    public SequencedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, bool accessOrder, IEqualityComparer<TKey>? comparer)
        : this(0, accessOrder, comparer)
    {
        ThrowHelper.ThrowIfNull(collection);

        foreach (KeyValuePair<TKey, TValue> kvp in collection)
            Add(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// Gets a value indicating whether a successful lookup or indexed value update moves the affected entry to the end
    /// of the iteration order.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the dictionary uses access ordering; <see langword="false" /> if it preserves
    /// insertion order.
    /// </value>
    public bool AccessOrder => _accessOrder;

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used to determine key equality.
    /// </summary>
    /// <value>The comparer used for key identity and hash-table lookup.</value>
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the first key/value pair in iteration order.
    /// </summary>
    /// <value>The entry at the head of the iteration order.</value>
    /// <exception cref="InvalidOperationException">The dictionary is empty.</exception>
    /// <remarks>
    /// This is an O(1) operation and never changes the iteration order, even in access-order mode. In an access-order
    /// dictionary the first entry is the least recently used.
    /// </remarks>
    public KeyValuePair<TKey, TValue> First
    {
        get
        {
            if (_order.First is not { } node)
                throw new InvalidOperationException(CollectionsResourceStrings.Arg_Invalid_CollectionIsEmpty);

            return new KeyValuePair<TKey, TValue>(node.Value, _store[node.Value].Value);
        }
    }

    /// <summary>
    /// Gets the last key/value pair in iteration order.
    /// </summary>
    /// <value>The entry at the tail of the iteration order.</value>
    /// <exception cref="InvalidOperationException">The dictionary is empty.</exception>
    /// <remarks>
    /// This is an O(1) operation and never changes the iteration order, even in access-order mode. In an access-order
    /// dictionary the last entry is the most recently used.
    /// </remarks>
    public KeyValuePair<TKey, TValue> Last
    {
        get
        {
            if (_order.Last is not { } node)
                throw new InvalidOperationException(CollectionsResourceStrings.Arg_Invalid_CollectionIsEmpty);

            return new KeyValuePair<TKey, TValue>(node.Value, _store[node.Value].Value);
        }
    }

    /// <summary>
    /// Attempts to retrieve the first key/value pair in iteration order without removing it.
    /// </summary>
    /// <param name="entry">
    /// When this method returns, contains the entry at the head of the iteration order if the dictionary is non-empty;
    /// otherwise, the default value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains at least one entry; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is an O(1) operation and never changes the iteration order, even in access-order mode.
    /// </remarks>
    public bool TryGetFirst(out KeyValuePair<TKey, TValue> entry)
    {
        if (_order.First is { } node)
        {
            entry = new KeyValuePair<TKey, TValue>(node.Value, _store[node.Value].Value);
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the last key/value pair in iteration order without removing it.
    /// </summary>
    /// <param name="entry">
    /// When this method returns, contains the entry at the tail of the iteration order if the dictionary is non-empty;
    /// otherwise, the default value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains at least one entry; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is an O(1) operation and never changes the iteration order, even in access-order mode.
    /// </remarks>
    public bool TryGetLast(out KeyValuePair<TKey, TValue> entry)
    {
        if (_order.Last is { } node)
        {
            entry = new KeyValuePair<TKey, TValue>(node.Value, _store[node.Value].Value);
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Attempts to remove and return the first key/value pair in iteration order.
    /// </summary>
    /// <param name="entry">
    /// When this method returns, contains the removed entry if the dictionary was non-empty; otherwise, the default
    /// value.
    /// </param>
    /// <returns><see langword="true" /> if an entry was removed; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// This is an O(1) operation. In an access-order dictionary this removes the least recently used entry.
    /// </remarks>
    public bool TryRemoveFirst(out KeyValuePair<TKey, TValue> entry)
    {
        if (_order.First is { } node)
        {
            TKey key = node.Value;
            entry = new KeyValuePair<TKey, TValue>(key, _store[key].Value);
            _store.Remove(key);
            _order.RemoveFirst();
            _version++;
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Attempts to remove and return the last key/value pair in iteration order.
    /// </summary>
    /// <param name="entry">
    /// When this method returns, contains the removed entry if the dictionary was non-empty; otherwise, the default
    /// value.
    /// </param>
    /// <returns><see langword="true" /> if an entry was removed; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// This is an O(1) operation. In an access-order dictionary this removes the most recently used entry.
    /// </remarks>
    public bool TryRemoveLast(out KeyValuePair<TKey, TValue> entry)
    {
        if (_order.Last is { } node)
        {
            TKey key = node.Value;
            entry = new KeyValuePair<TKey, TValue>(key, _store[key].Value);
            _store.Remove(key);
            _order.RemoveLast();
            _version++;
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Moves the specified ordering node to the tail of the iteration order, recording the access.
    /// </summary>
    /// <param name="node">The ordering node to reposition.</param>
    /// <remarks>
    /// Reuses the existing node instance rather than allocating a new one, so callers holding the node reference (via
    /// <see cref="Entry.Node" />) remain valid. Increments <see cref="_version" /> because the iteration order changed.
    /// </remarks>
    private void MoveToTail(LinkedListNode<TKey> node)
    {
        if (_order.Last == node)
            return;

        _order.Remove(node);
        _order.AddLast(node);
        _version++;
    }

    /// <summary>
    /// Returns the dictionary entries in iteration order, failing fast if the dictionary is modified during
    /// enumeration.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="KeyValuePair{TKey, TValue}" /> in insertion order, or access
    /// order when access ordering is enabled.
    /// </returns>
    /// <exception cref="InvalidOperationException">The dictionary was modified during enumeration.</exception>
    private IEnumerable<KeyValuePair<TKey, TValue>> GetOrderedItems()
    {
        int version = _version;

        foreach (TKey key in _order)
        {
            ThrowIfVersionChanged(version);
            yield return new KeyValuePair<TKey, TValue>(key, _store[key].Value);
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if <paramref name="capturedVersion" /> no longer matches the
    /// current <see cref="_version" />, signaling that the dictionary was modified during enumeration.
    /// </summary>
    /// <param name="capturedVersion">The version observed at the start of enumeration.</param>
    /// <exception cref="InvalidOperationException">
    /// The dictionary was modified since <paramref name="capturedVersion" /> was captured.
    /// </exception>
    private void ThrowIfVersionChanged(int capturedVersion)
    {
        if (_version != capturedVersion)
            throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);
    }
}
