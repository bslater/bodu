// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileParseMemo{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Caching;

/// <summary>
/// Memoizes the parsed content of files against their last-write instant, bounded to a fixed number of paths with
/// least-recently-used eviction, so a repeated read of an unchanged file serves the cached parse instead of re-reading
/// and re-deserializing it.
/// </summary>
/// <typeparam name="T">The parsed representation cached per file.</typeparam>
/// <remarks>
/// <para>
/// A hit requires both the path and the caller-observed last-write instant to match the stored entry, so a file
/// changed by any writer — this process or another — misses on its moved timestamp and is re-parsed. Eviction and
/// invalidation only ever force a re-parse, never a stale serve, so the capacity bound is safe by construction; it
/// exists to keep the memo from growing without limit in a process that touches many distinct cache files over its
/// lifetime.
/// </para>
/// <para>
/// All operations take a single plain lock. The memo brackets file I/O — an open, stat, read, and parse — so the
/// microseconds spent under the lock are immaterial next to the work it saves, and a simple linked-list LRU under one
/// lock is preferred over a lock-free structure that cannot maintain recency order.
/// </para>
/// <para>
/// This type is a shared cache-infrastructure primitive: it is linked as source into each caching package that needs
/// it (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly, so no public surface is
/// exposed and no two assemblies collide on the type identity.
/// </para>
/// </remarks>
internal sealed class FileParseMemo<T>
{
    /// <summary>The default maximum number of file paths retained; generous enough that single-file-per-key layouts never evict.</summary>
    public const int DefaultCapacity = 1024;

    /// <summary>The maximum number of paths retained before the least recently used entry is evicted.</summary>
    private readonly int _capacity;

    /// <summary>The memo entries keyed by full file path, each holding its recency-list node.</summary>
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries;

    /// <summary>The recency order: most recently used at the head, eviction candidate at the tail.</summary>
    private readonly LinkedList<Entry> _order = new();

    /// <summary>Guards <see cref="_entries" /> and <see cref="_order" />.</summary>
    private readonly object _sync = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileParseMemo{T}" /> class.
    /// </summary>
    /// <param name="capacity">The maximum number of file paths retained; defaults to <see cref="DefaultCapacity" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> ≤ 0.</exception>
    public FileParseMemo(int capacity = DefaultCapacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
        _entries = new Dictionary<string, LinkedListNode<Entry>>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Attempts to retrieve the memoized parse for a file, hitting only when the supplied last-write instant matches
    /// the instant the parse was stored against.
    /// </summary>
    /// <param name="path">The full file path.</param>
    /// <param name="stampUtc">The file's current last-write instant, in UTC.</param>
    /// <param name="value">The memoized parse when the method returns <see langword="true" />.</param>
    /// <returns>
    /// <see langword="true" /> when a parse for <paramref name="path" /> is memoized against exactly
    /// <paramref name="stampUtc" />; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGet(string path, DateTime stampUtc, out T? value)
    {
        lock (_sync)
        {
            if (_entries.TryGetValue(path, out LinkedListNode<Entry>? node) && node.Value.StampUtc == stampUtc)
            {
                _order.Remove(node);
                _order.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Stores the parse for a file against its last-write instant, replacing any prior entry for the path and evicting
    /// the least recently used entry when the memo is at capacity.
    /// </summary>
    /// <param name="path">The full file path.</param>
    /// <param name="stampUtc">The file's last-write instant, in UTC, observed before the content was read.</param>
    /// <param name="value">The parsed representation to memoize.</param>
    public void Set(string path, DateTime stampUtc, T value)
    {
        lock (_sync)
        {
            if (_entries.TryGetValue(path, out LinkedListNode<Entry>? node))
            {
                node.Value = new Entry(path, stampUtc, value);
                _order.Remove(node);
                _order.AddFirst(node);
                return;
            }

            if (_entries.Count >= _capacity)
            {
                LinkedListNode<Entry> tail = _order.Last!;
                _order.RemoveLast();
                _entries.Remove(tail.Value.Path);
            }

            LinkedListNode<Entry> added = _order.AddFirst(new Entry(path, stampUtc, value));
            _entries[path] = added;
        }
    }

    /// <summary>
    /// Removes any memoized parse for a file, so the next read re-parses it. Called after this instance writes or
    /// deletes the file.
    /// </summary>
    /// <param name="path">The full file path.</param>
    public void Invalidate(string path)
    {
        lock (_sync)
        {
            if (_entries.Remove(path, out LinkedListNode<Entry>? node))
                _order.Remove(node);
        }
    }

    /// <summary>
    /// A memo entry: the path (needed to unmap on tail eviction), the stamp the parse is valid for, and the parse.
    /// </summary>
    /// <param name="Path">The full file path the entry belongs to.</param>
    /// <param name="StampUtc">The last-write instant the parse was stored against.</param>
    /// <param name="Value">The memoized parse.</param>
    private readonly record struct Entry(string Path, DateTime StampUtc, T Value);
}
