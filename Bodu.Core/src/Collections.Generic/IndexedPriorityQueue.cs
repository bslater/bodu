// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a binary min-heap priority queue keyed by element identity, supporting O(log n) re-prioritisation
/// (decrease- or increase-key) and removal of any element by value.
/// </summary>
/// <typeparam name="TElement">Specifies the element type used as the queue's identity. Must be non-null and unique within the queue.</typeparam>
/// <typeparam name="TPriority">Specifies the priority type used for ordering.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IndexedPriorityQueue{TElement, TPriority}"/> complements
/// <see cref="System.Collections.Generic.PriorityQueue{TElement, TPriority}"/> by maintaining an auxiliary
/// element-to-index map alongside the heap. This map turns <see cref="Contains(TElement)"/>,
/// <see cref="TryGetPriority(TElement, out TPriority)"/>, and the slot lookup used by
/// <see cref="Update(TElement, TPriority)"/>, <see cref="Remove(TElement)"/>, and
/// <see cref="EnqueueOrUpdate(TElement, TPriority)"/> into O(1) operations and the subsequent heap
/// repair into O(log n) — the operations Dijkstra's algorithm, Prim's algorithm, and A* require.
/// </para>
/// <para>
/// The trade-off relative to the BCL <see cref="System.Collections.Generic.PriorityQueue{TElement, TPriority}"/>
/// is that elements are unique. <see cref="Enqueue(TElement, TPriority)"/> throws
/// <see cref="ArgumentException"/> if the element is already present; use
/// <see cref="EnqueueOrUpdate(TElement, TPriority)"/> for set semantics.
/// </para>
/// <para>
/// Ordering is determined by an <see cref="IComparer{TPriority}"/>; smaller priorities are dequeued first
/// (min-heap). To obtain max-heap behaviour, supply a reversed comparer. Element identity uses an
/// <see cref="IEqualityComparer{TElement}"/>; both default to the framework default comparer when not specified.
/// </para>
/// <para>
/// Enumeration walks the underlying heap array in storage order, not priority order. Dequeue items in turn
/// to obtain priority-ordered output.
/// </para>
/// <para>
/// <see cref="IndexedPriorityQueue{TElement, TPriority}"/> is not thread-safe. Concurrent reads and writes
/// require external synchronisation.
/// </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(IndexedPriorityQueueDebugView<,>))]
[Serializable]
public sealed partial class IndexedPriorityQueue<TElement, TPriority>
    : IReadOnlyCollection<KeyValuePair<TElement, TPriority>>
    where TElement : notnull
{
    /// <summary>
    /// The default capacity used when the heap is grown from an empty backing array.
    /// </summary>
    private const int DefaultGrowCapacity = 4;

    /// <summary>
    /// Backing storage for the heap. Slots <c>[0.._size)</c> are valid heap nodes; the remainder is
    /// uninitialised reserve capacity.
    /// </summary>
    private (TElement Element, TPriority Priority)[] _nodes;

    /// <summary>
    /// Maps each element currently in the heap to its position in <see cref="_nodes"/>. Kept in lock-step
    /// with every swap, insert, and removal so that lookup-by-element remains O(1).
    /// </summary>
    private readonly Dictionary<TElement, int> _index;

    /// <summary>
    /// The priority comparer used for heap ordering. Smaller values (per this comparer) are dequeued first.
    /// </summary>
    private readonly IComparer<TPriority> _comparer;

    /// <summary>
    /// The number of nodes currently stored in the heap. Always satisfies <c>_size &lt;= _nodes.Length</c>.
    /// </summary>
    private int _size;

    /// <summary>
    /// Mutation counter used by <see cref="Enumerator"/> to detect concurrent modification. Incremented on
    /// every operation that adds, removes, or re-prioritises an element, and on <see cref="Clear"/>.
    /// </summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class with
    /// zero initial capacity, the default priority comparer, and the default element comparer.
    /// </summary>
    public IndexedPriorityQueue()
        : this(0, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class with
    /// the specified initial capacity, using the default priority comparer and element comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity of the heap. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public IndexedPriorityQueue(int capacity)
        : this(capacity, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class with
    /// zero initial capacity and the specified priority comparer.
    /// </summary>
    /// <param name="priorityComparer">
    /// The comparer used to order priorities, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.
    /// </param>
    public IndexedPriorityQueue(IComparer<TPriority>? priorityComparer)
        : this(0, priorityComparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class with
    /// the specified initial capacity and priority comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity of the heap. Must be non-negative.</param>
    /// <param name="priorityComparer">
    /// The comparer used to order priorities, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public IndexedPriorityQueue(int capacity, IComparer<TPriority>? priorityComparer)
        : this(capacity, priorityComparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class with
    /// the specified initial capacity, priority comparer, and element comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity of the heap. Must be non-negative.</param>
    /// <param name="priorityComparer">
    /// The comparer used to order priorities, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.
    /// </param>
    /// <param name="elementComparer">
    /// The comparer used to test element equality, or <see langword="null"/> to use <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public IndexedPriorityQueue(int capacity, IComparer<TPriority>? priorityComparer, IEqualityComparer<TElement>? elementComparer)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        _nodes = capacity == 0
            ? Array.Empty<(TElement, TPriority)>()
            : new (TElement, TPriority)[capacity];
        _comparer = priorityComparer ?? Comparer<TPriority>.Default;
        _index = new Dictionary<TElement, int>(capacity, elementComparer ?? EqualityComparer<TElement>.Default);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class
    /// containing the supplied element-priority pairs, heapified in O(n).
    /// </summary>
    /// <param name="items">
    /// The element-priority pairs used to populate the queue. Element keys must be unique. Must not be <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="items"/> contains a duplicate element key.</exception>
    public IndexedPriorityQueue(IEnumerable<KeyValuePair<TElement, TPriority>> items)
        : this(items, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedPriorityQueue{TElement, TPriority}"/> class
    /// containing the supplied element-priority pairs, with the specified priority and element comparers.
    /// </summary>
    /// <param name="items">
    /// The element-priority pairs used to populate the queue. Element keys must be unique. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="priorityComparer">
    /// The comparer used to order priorities, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.
    /// </param>
    /// <param name="elementComparer">
    /// The comparer used to test element equality, or <see langword="null"/> to use <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="items"/> contains a duplicate element key.</exception>
    /// <remarks>
    /// When <paramref name="items"/> implements <see cref="ICollection{T}"/> the backing array is allocated
    /// up-front to its <see cref="ICollection{T}.Count"/> and the heap is built bottom-up in O(n);
    /// otherwise the array grows on demand as items are appended.
    /// </remarks>
    public IndexedPriorityQueue(IEnumerable<KeyValuePair<TElement, TPriority>> items, IComparer<TPriority>? priorityComparer, IEqualityComparer<TElement>? elementComparer)
    {
        ThrowHelper.ThrowIfNull(items);

        _comparer = priorityComparer ?? Comparer<TPriority>.Default;
        IEqualityComparer<TElement> resolvedElementComparer = elementComparer ?? EqualityComparer<TElement>.Default;

        if (items is ICollection<KeyValuePair<TElement, TPriority>> collection)
        {
            int initialCapacity = collection.Count;
            _nodes = initialCapacity == 0
                ? Array.Empty<(TElement, TPriority)>()
                : new (TElement, TPriority)[initialCapacity];
            _index = new Dictionary<TElement, int>(initialCapacity, resolvedElementComparer);
        }
        else
        {
            _nodes = Array.Empty<(TElement, TPriority)>();
            _index = new Dictionary<TElement, int>(resolvedElementComparer);
        }

        foreach (KeyValuePair<TElement, TPriority> pair in items)
        {
            ThrowHelper.ThrowIfNull(pair.Key);
            if (!_index.TryAdd(pair.Key, _size))
                throw new ArgumentException(string.Format(DuplicateElementMessageFormat, pair.Key), nameof(items));

            if (_size == _nodes.Length)
                Grow(_size + 1);

            _nodes[_size] = (pair.Key, pair.Value);
            _size++;
        }

        Heapify();
    }

    /// <summary>
    /// Gets the number of elements contained in the queue.
    /// </summary>
    /// <returns>The current element count.</returns>
    public int Count => _size;

    /// <summary>
    /// Gets the total number of elements the internal data structure can hold without resizing.
    /// </summary>
    /// <returns>The length of the internal heap array.</returns>
    public int Capacity => _nodes.Length;

    /// <summary>
    /// Gets the priority comparer used by the queue to order elements.
    /// </summary>
    /// <returns>The configured <see cref="IComparer{TPriority}"/>.</returns>
    public IComparer<TPriority> Comparer => _comparer;

    /// <summary>
    /// Gets the element equality comparer used by the queue to identify elements.
    /// </summary>
    /// <returns>The configured <see cref="IEqualityComparer{TElement}"/>.</returns>
    public IEqualityComparer<TElement> ElementComparer => _index.Comparer;

    /// <summary>
    /// Determines whether the queue contains the specified element.
    /// </summary>
    /// <param name="element">The element to locate. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the element is present; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public bool Contains(TElement element)
    {
        ThrowHelper.ThrowIfNull(element);

        return _index.ContainsKey(element);
    }

    /// <summary>
    /// Retrieves the priority associated with the specified element.
    /// </summary>
    /// <param name="element">The element whose priority is to be retrieved. Must not be <see langword="null"/>.</param>
    /// <returns>The priority currently associated with <paramref name="element"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="element"/> is not present in the queue.</exception>
    public TPriority GetPriority(TElement element)
    {
        ThrowHelper.ThrowIfNull(element);

        if (!_index.TryGetValue(element, out int slot))
            throw new KeyNotFoundException(string.Format(ElementNotFoundMessageFormat, element));

        return _nodes[slot].Priority;
    }

    /// <summary>
    /// Attempts to retrieve the priority associated with the specified element.
    /// </summary>
    /// <param name="element">The element whose priority is to be retrieved. Must not be <see langword="null"/>.</param>
    /// <param name="priority">
    /// When this method returns, contains the priority associated with <paramref name="element"/> if found;
    /// otherwise, the default value of <typeparamref name="TPriority"/>.
    /// </param>
    /// <returns><see langword="true"/> if the element was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public bool TryGetPriority(TElement element, out TPriority priority)
    {
        ThrowHelper.ThrowIfNull(element);

        if (_index.TryGetValue(element, out int slot))
        {
            priority = _nodes[slot].Priority;
            return true;
        }

        priority = default!;
        return false;
    }

    /// <summary>
    /// Returns the element-priority pair at the head of the queue without removing it.
    /// </summary>
    /// <returns>The element-priority pair with the smallest priority per the configured comparer.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public KeyValuePair<TElement, TPriority> Peek()
    {
        if (_size == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        (TElement Element, TPriority Priority) head = _nodes[0];
        return new KeyValuePair<TElement, TPriority>(head.Element, head.Priority);
    }

    /// <summary>
    /// Attempts to retrieve the element-priority pair at the head of the queue without removing it.
    /// </summary>
    /// <param name="element">When this method returns, contains the head element if the queue is non-empty; otherwise, the default value of <typeparamref name="TElement"/>.</param>
    /// <param name="priority">When this method returns, contains the head priority if the queue is non-empty; otherwise, the default value of <typeparamref name="TPriority"/>.</param>
    /// <returns><see langword="true"/> if a head element was retrieved; otherwise, <see langword="false"/>.</returns>
    public bool TryPeek(out TElement element, out TPriority priority)
    {
        if (_size == 0)
        {
            element = default!;
            priority = default!;
            return false;
        }

        (TElement Element, TPriority Priority) head = _nodes[0];
        element = head.Element;
        priority = head.Priority;
        return true;
    }

    /// <summary>
    /// Adds the specified element with the specified priority. The element must not already be present.
    /// </summary>
    /// <param name="element">The element to add. Must not be <see langword="null"/>.</param>
    /// <param name="priority">The priority associated with the element.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="element"/> is already present in the queue.</exception>
    public void Enqueue(TElement element, TPriority priority)
    {
        ThrowHelper.ThrowIfNull(element);

        if (!TryEnqueueCore(element, priority))
            throw new ArgumentException(string.Format(DuplicateElementMessageFormat, element), nameof(element));
    }

    /// <summary>
    /// Attempts to add the specified element with the specified priority.
    /// </summary>
    /// <param name="element">The element to add. Must not be <see langword="null"/>.</param>
    /// <param name="priority">The priority associated with the element.</param>
    /// <returns><see langword="true"/> if the element was added; <see langword="false"/> if it was already present.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public bool TryEnqueue(TElement element, TPriority priority)
    {
        ThrowHelper.ThrowIfNull(element);

        return TryEnqueueCore(element, priority);
    }

    /// <summary>
    /// Adds the specified element with the specified priority, or re-prioritises it if already present.
    /// </summary>
    /// <param name="element">The element to add or update. Must not be <see langword="null"/>.</param>
    /// <param name="priority">The priority to assign.</param>
    /// <returns>
    /// <see langword="true"/> if the element was added; <see langword="false"/> if an existing entry was updated.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <remarks>This is the canonical primitive for Dijkstra-style relaxation steps.</remarks>
    public bool EnqueueOrUpdate(TElement element, TPriority priority)
    {
        ThrowHelper.ThrowIfNull(element);

        if (_index.TryGetValue(element, out int slot))
        {
            UpdateAt(slot, priority);
            return false;
        }

        EnqueueCore(element, priority);
        return true;
    }

    /// <summary>
    /// Removes and returns the element-priority pair at the head of the queue.
    /// </summary>
    /// <returns>The dequeued element-priority pair.</returns>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public KeyValuePair<TElement, TPriority> Dequeue()
    {
        if (_size == 0)
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionEmpty);

        (TElement Element, TPriority Priority) head = _nodes[0];
        RemoveAt(0);
        return new KeyValuePair<TElement, TPriority>(head.Element, head.Priority);
    }

    /// <summary>
    /// Attempts to remove and return the element-priority pair at the head of the queue.
    /// </summary>
    /// <param name="element">When this method returns, contains the dequeued element if successful; otherwise, the default value of <typeparamref name="TElement"/>.</param>
    /// <param name="priority">When this method returns, contains the dequeued priority if successful; otherwise, the default value of <typeparamref name="TPriority"/>.</param>
    /// <returns><see langword="true"/> if an element was dequeued; otherwise, <see langword="false"/>.</returns>
    public bool TryDequeue(out TElement element, out TPriority priority)
    {
        if (_size == 0)
        {
            element = default!;
            priority = default!;
            return false;
        }

        (TElement Element, TPriority Priority) head = _nodes[0];
        element = head.Element;
        priority = head.Priority;
        RemoveAt(0);
        return true;
    }

    /// <summary>
    /// Re-prioritises the specified element. The element must already be present.
    /// </summary>
    /// <param name="element">The element to re-prioritise. Must not be <see langword="null"/>.</param>
    /// <param name="priority">The new priority to assign.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="element"/> is not present in the queue.</exception>
    /// <remarks>
    /// Works for both decrease-key and increase-key. Cost is O(log n) and includes a single sift-up
    /// or sift-down of the moved node; if the priority is unchanged the operation is a no-op.
    /// </remarks>
    public void Update(TElement element, TPriority priority)
    {
        ThrowHelper.ThrowIfNull(element);

        if (!_index.TryGetValue(element, out int slot))
            throw new KeyNotFoundException(string.Format(ElementNotFoundMessageFormat, element));

        UpdateAt(slot, priority);
    }

    /// <summary>
    /// Attempts to re-prioritise the specified element.
    /// </summary>
    /// <param name="element">The element to re-prioritise. Must not be <see langword="null"/>.</param>
    /// <param name="priority">The new priority to assign.</param>
    /// <returns><see langword="true"/> if the element was found and updated; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public bool TryUpdate(TElement element, TPriority priority)
    {
        ThrowHelper.ThrowIfNull(element);

        if (!_index.TryGetValue(element, out int slot))
            return false;

        UpdateAt(slot, priority);
        return true;
    }

    /// <summary>
    /// Removes the specified element from the queue if present.
    /// </summary>
    /// <param name="element">The element to remove. Must not be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the element was found and removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
    public bool Remove(TElement element)
    {
        ThrowHelper.ThrowIfNull(element);

        if (!_index.TryGetValue(element, out int slot))
            return false;

        RemoveAt(slot);
        return true;
    }

    /// <summary>
    /// Removes all elements from the queue.
    /// </summary>
    /// <remarks>
    /// The backing capacity is preserved; call <see cref="TrimExcess"/> to release the unused storage.
    /// </remarks>
    public void Clear()
    {
        if (_size == 0)
            return;

        Array.Clear(_nodes, 0, _size);
        _index.Clear();
        _size = 0;
        _version++;
    }

    /// <summary>
    /// Ensures that the internal storage can hold at least <paramref name="capacity"/> elements without reallocating.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure. Must be non-negative.</param>
    /// <returns>The new capacity of the internal storage.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public int EnsureCapacity(int capacity)
    {
        ThrowHelper.ThrowIfNegative(capacity);

        if (_nodes.Length < capacity)
            Grow(capacity);

        return _nodes.Length;
    }

    /// <summary>
    /// Reduces the internal storage to match the current element count.
    /// </summary>
    /// <remarks>This method does not bump the modification version: enumerators remain valid.</remarks>
    public void TrimExcess()
    {
        if (_size == _nodes.Length)
            return;

        if (_size == 0)
        {
            _nodes = Array.Empty<(TElement, TPriority)>();
            return;
        }

        var trimmed = new (TElement, TPriority)[_size];
        Array.Copy(_nodes, trimmed, _size);
        _nodes = trimmed;
    }

    /// <summary>
    /// Returns a struct enumerator that walks the heap storage in array order.
    /// </summary>
    /// <returns>A new <see cref="Enumerator"/> bound to this queue.</returns>
    /// <remarks>
    /// Enumeration is in heap-storage order, not priority order. To enumerate in priority order, drain the
    /// queue with successive <see cref="Dequeue"/> calls.
    /// </remarks>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Inserts an element-priority pair without performing input validation. The element must not be present.
    /// </summary>
    /// <param name="element">The element to insert.</param>
    /// <param name="priority">The associated priority.</param>
    private void EnqueueCore(TElement element, TPriority priority)
    {
        if (_size == _nodes.Length)
            Grow(_size + 1);

        int slot = _size;
        _nodes[slot] = (element, priority);
        _index[element] = slot;
        _size++;
        _version++;

        SiftUp(slot);
    }

    /// <summary>
    /// Attempts to insert an element-priority pair, returning <see langword="false"/> if the element is already present.
    /// </summary>
    /// <param name="element">The element to insert.</param>
    /// <param name="priority">The associated priority.</param>
    /// <returns><see langword="true"/> if the element was inserted; otherwise, <see langword="false"/>.</returns>
    private bool TryEnqueueCore(TElement element, TPriority priority)
    {
        if (_index.ContainsKey(element))
            return false;

        EnqueueCore(element, priority);
        return true;
    }

    /// <summary>
    /// Re-prioritises the node at the specified slot, performing the appropriate sift-up or sift-down.
    /// </summary>
    /// <param name="slot">The slot in <see cref="_nodes"/> whose priority is being changed.</param>
    /// <param name="newPriority">The replacement priority.</param>
    private void UpdateAt(int slot, TPriority newPriority)
    {
        TPriority oldPriority = _nodes[slot].Priority;
        int direction = _comparer.Compare(newPriority, oldPriority);
        if (direction == 0)
            return;

        _nodes[slot] = (_nodes[slot].Element, newPriority);
        _version++;

        if (direction < 0)
            SiftUp(slot);
        else
            SiftDown(slot);
    }

    /// <summary>
    /// Removes the node at the specified slot, repairing the heap by moving the trailing node into the gap
    /// and sifting it up or down as required.
    /// </summary>
    /// <param name="slot">The slot in <see cref="_nodes"/> to remove.</param>
    private void RemoveAt(int slot)
    {
        int lastIndex = _size - 1;
        (TElement Element, TPriority Priority) removed = _nodes[slot];
        _index.Remove(removed.Element);

        if (slot < lastIndex)
        {
            (TElement Element, TPriority Priority) moved = _nodes[lastIndex];
            _nodes[slot] = moved;
            _index[moved.Element] = slot;

            int direction = _comparer.Compare(moved.Priority, removed.Priority);
            if (direction < 0)
                SiftUp(slot);
            else if (direction > 0)
                SiftDown(slot);
        }

        _nodes[lastIndex] = default!;
        _size = lastIndex;
        _version++;
    }

    /// <summary>
    /// Bubbles the node at <paramref name="slot"/> toward the root while it has a smaller priority than its parent.
    /// </summary>
    /// <param name="slot">The starting slot to sift upward.</param>
    private void SiftUp(int slot)
    {
        while (slot > 0)
        {
            int parent = (slot - 1) >> 1;
            if (_comparer.Compare(_nodes[slot].Priority, _nodes[parent].Priority) >= 0)
                break;

            Swap(slot, parent);
            slot = parent;
        }
    }

    /// <summary>
    /// Bubbles the node at <paramref name="slot"/> toward the leaves while a child has a smaller priority.
    /// </summary>
    /// <param name="slot">The starting slot to sift downward.</param>
    private void SiftDown(int slot)
    {
        int half = _size >> 1;
        while (slot < half)
        {
            int left = (slot << 1) + 1;
            int right = left + 1;
            int target = (right < _size && _comparer.Compare(_nodes[right].Priority, _nodes[left].Priority) < 0)
                ? right
                : left;

            if (_comparer.Compare(_nodes[target].Priority, _nodes[slot].Priority) >= 0)
                break;

            Swap(slot, target);
            slot = target;
        }
    }

    /// <summary>
    /// Swaps the two specified slots in <see cref="_nodes"/> and updates <see cref="_index"/> accordingly.
    /// </summary>
    /// <param name="a">The first slot to swap.</param>
    /// <param name="b">The second slot to swap.</param>
    private void Swap(int a, int b)
    {
        (TElement Element, TPriority Priority) temp = _nodes[a];
        _nodes[a] = _nodes[b];
        _nodes[b] = temp;

        _index[_nodes[a].Element] = a;
        _index[_nodes[b].Element] = b;
    }

    /// <summary>
    /// Builds the heap invariant from the bottom up in O(n).
    /// </summary>
    /// <remarks>Used only by the <see cref="IEnumerable{T}"/> constructor before the queue is observable.</remarks>
    private void Heapify()
    {
        for (int i = (_size - 2) >> 1; i >= 0; i--)
            SiftDown(i);
    }

    /// <summary>
    /// Expands the backing array to at least <paramref name="minCapacity"/> elements, doubling the existing
    /// length where possible.
    /// </summary>
    /// <param name="minCapacity">The minimum capacity required after the resize.</param>
    private void Grow(int minCapacity)
    {
        int newCapacity = _nodes.Length == 0 ? DefaultGrowCapacity : _nodes.Length * 2;
        if ((uint)newCapacity > Array.MaxLength)
            newCapacity = Array.MaxLength;
        if (newCapacity < minCapacity)
            newCapacity = minCapacity;

        var resized = new (TElement, TPriority)[newCapacity];
        if (_size > 0)
            Array.Copy(_nodes, resized, _size);
        _nodes = resized;
    }

    /// <summary>
    /// The composite-format string used when reporting a duplicate element key. The single placeholder
    /// receives the offending element's <see cref="object.ToString"/> representation.
    /// </summary>
    private const string DuplicateElementMessageFormat = "An element with the key '{0}' already exists in the queue.";

    /// <summary>
    /// The composite-format string used when reporting a missing element key. The single placeholder
    /// receives the requested element's <see cref="object.ToString"/> representation.
    /// </summary>
    private const string ElementNotFoundMessageFormat = "The element '{0}' was not found in the queue.";
}
