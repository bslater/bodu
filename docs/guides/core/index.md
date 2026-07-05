---
title: Bodu.Core guides
---

# Bodu.Core guides

Recipe-style walk-throughs for **Bodu.Core**, organized by namespace. Each guide on this page is a focused walk-through of one headline type.

If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.Core introduction](../../docs/core/index.md) and the [getting-started page](../../docs/core/getting-started.md). New to the many collection types? Read **[Choosing a collection](choosing-a-collection.md)** — a decision guide that maps requirements to the correct type. For the auto-generated API reference, see the [Bodu.Collections.Generic namespace page](xref:Bodu.Collections.Generic).

Bodu.Core anchors the **Core Foundations** topic: the [topic guide landing](../topics/core-foundations.md) places these guides alongside the rest of the topic, and the [topic overview](../../docs/topics/core-foundations.md) carries the package map and install command.

> [!NOTE]
> The specialized collection catalogue — the `Bodu.Collections.Generic`, `.Concurrent`, `.Graphs`, and `.Trees` namespaces below — ships in the **`Bodu.Collections`** package (which depends on `Bodu.Core`). The namespaces are unchanged; only the package boundary moved. Install with `dotnet add package Bodu.Collections`. The remaining namespaces on this page ship in `Bodu.Core` itself.

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.Collections.Generic` | Bounded ring-backed collections, sets, multisets, and range-keyed lookups — `CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey,TValue>`, `SequencedDictionary<TKey,TValue>`, `IndexedPriorityQueue<TElement,TPriority>`, `IndexedSet<T>`, `OrderedSet<T>`, `Multiset<T>`, `MultiValueDictionary<TKey,TValue>`, `RangeDictionary<TKey,TValue>`, `RangeSet<T>`, `SegmentedBuffer<T>`, `RingBackedCollection<T>` base. | [Choosing a collection](choosing-a-collection.md) · [Circular buffer](circular-buffer.md) · [Deque](deque.md) · [Evicting dictionary](evicting-dictionary.md) · [Sequenced dictionary](sequenced-dictionary.md) · [Indexed priority queue](indexed-priority-queue.md) · [Indexed and ordered sets](ordered-sets.md) · [Multiset](multiset.md) · [Multi-value dictionary](multi-value-dictionary.md) · [Range-keyed lookups](range-dictionary.md) · [Segmented buffer](segmented-buffer.md) |
| `Bodu.Collections.Generic.Concurrent` | Thread-safe collection variants — `ConcurrentCircularBuffer<T>`, `ConcurrentHashSet<T>`. | [Concurrent collections](concurrent-collections.md) |
| `Bodu.Collections.Generic.Graphs` | Graphs and graph algorithms — `Graph<T>`, the read-only `IReadOnlyGraph<T>` / `IReadOnlyWeightedGraph<T,T>` views, `GraphAlgorithms` (BFS/DFS, shortest path, topological sort, connected components), `ShortestPathResult<T>`, and the `DisjointSet` / `DisjointSet<T>` union-find. | [Graphs and graph algorithms](graphs.md) |
| `Bodu.Collections.Generic.Trees` | Prefix trees and an n-ary tree — `Trie`, `Trie<TValue>`, and `Tree<T>`. | [Trie (prefix tree)](trie.md) |
| `Bodu.Threading` | Async coordination primitives — `AsyncLock`, `AsyncSemaphore`, `AsyncReaderWriterLock`, `AsyncAutoResetEvent` / `AsyncManualResetEvent` / `AsyncCountdownEvent`, `AsyncLazy<T>`, `AsyncDebouncer`, and `RateGate`. | [Async coordination primitives](async-primitives.md) |
| `Bodu.Functional` | Functional helpers — `Memoizer`, and the railway primitives `Option<T>`, `Result` / `Result<T>` / `ResultError`, `Either<TLeft,TRight>` with Task-based async combinators. | [Memoization](memoization.md) · [Options, results, and eithers](functional-results.md) |
| `Bodu` | Root namespace primitives — `WeekPattern`, `IRandomGenerator`, `XorShiftRandom`, `ThrowHelper`. | [WeekPattern](week-pattern.md) |
| `Bodu.Buffers` | Pooled buffer infrastructure — `PooledBufferBuilder<T>`. | [Pooled buffer builder](pooled-buffer-builder.md) |
| `Bodu.Extensions` | Date, numeric, span, array, and comparable extension methods — `DateTimeExtensions`, `DateOnlyExtensions`, `NumericExtensions`, `ArrayExtensions`, `BufferConverter`, `SpanExtensions`, `IComparableExtensions`. | (no dedicated guide yet — see API reference) |
| `Bodu.Text`, `Bodu.Xml.Linq` | Small text and XML helpers used internally. | — |

## Guides

### `Bodu.Collections.Generic`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="choosing-a-collection.md">Choosing a collection</a></h3>
  <p>The decision guide — quick decision tree, per-axis tables, real-world scenarios, and the anti-patterns that come up when picking between similar types.</p>
</div>

<div class="bodu-card">
  <h3><a href="circular-buffer.md">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — single-threaded <code>CircularBuffer&lt;T&gt;</code> and thread-safe <code>ConcurrentCircularBuffer&lt;T&gt;</code>; configurable overwrite-on-full.</p>
</div>

<div class="bodu-card">
  <h3><a href="deque.md">Deque</a></h3>
  <p>Double-ended queue — <code>Deque&lt;T&gt;</code> with O(1) <code>AddFirst</code> / <code>AddLast</code> / <code>RemoveFirst</code> / <code>RemoveLast</code>; growable or fixed-capacity.</p>
</div>

<div class="bodu-card">
  <h3><a href="evicting-dictionary.md">Evicting dictionary</a></h3>
  <p>Capacity-bounded key-value store with FIFO, LRU, and LFU eviction policies.</p>
</div>

<div class="bodu-card">
  <h3><a href="sequenced-dictionary.md">Sequenced dictionary</a></h3>
  <p>Insertion- or access-ordered dictionary (Java <code>LinkedHashMap</code> shape) with O(1) <code>First</code> / <code>Last</code> access and <code>TryRemoveFirst</code> / <code>TryRemoveLast</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="indexed-priority-queue.md">Indexed priority queue</a></h3>
  <p>Min-heap priority queue with O(1) lookup-by-element and in-place priority updates — for Dijkstra, Prim, and A*.</p>
</div>

<div class="bodu-card">
  <h3><a href="ordered-sets.md">Indexed and ordered sets</a></h3>
  <p>Insertion-ordered unique collections — <code>IndexedSet&lt;T&gt;</code> (unique <code>IList&lt;T&gt;</code>) and <code>OrderedSet&lt;T&gt;</code> (ordered set algebra).</p>
</div>

<div class="bodu-card">
  <h3><a href="multiset.md">Multiset</a></h3>
  <p>A bag that retains duplicates as multiplicity — frequency counting and multiset algebra (sum, union, intersect, except).</p>
</div>

<div class="bodu-card">
  <h3><a href="multi-value-dictionary.md">Multi-value dictionary</a></h3>
  <p>One key to many values without the <code>Dictionary&lt;TKey, List&lt;TValue&gt;&gt;</code> boilerplate; the indexer never returns <code>null</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="range-dictionary.md">Range-keyed lookups</a></h3>
  <p>Half-open interval keys — <code>Range&lt;T&gt;</code>, <code>RangeDictionary&lt;TKey,TValue&gt;</code>, and <code>RangeSet&lt;T&gt;</code> with union / intersect / except.</p>
</div>

<div class="bodu-card">
  <h3><a href="segmented-buffer.md">Segmented buffer</a></h3>
  <p>Append-only buffer that grows in fixed-size segments — avoids the array-doubling copy for streams of unknown length.</p>
</div>

</div>

### `Bodu.Collections.Generic.Concurrent`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="concurrent-collections.md">Concurrent collections</a></h3>
  <p>Thread-safe peers — lock-free <code>ConcurrentCircularBuffer&lt;T&gt;</code> (Vyukov MPMC ring) and lock-striped <code>ConcurrentHashSet&lt;T&gt;</code>.</p>
</div>

</div>

### `Bodu.Collections.Generic.Graphs`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="graphs.md">Graphs and graph algorithms</a></h3>
  <p>Directed and undirected <code>Graph&lt;T&gt;</code> with optional edge weights; <code>GraphAlgorithms</code> for BFS/DFS, shortest path, topological sort, and connected components; plus <code>DisjointSet&lt;T&gt;</code> union-find.</p>
</div>

</div>

### `Bodu.Collections.Generic.Trees`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="trie.md">Trie (prefix tree)</a></h3>
  <p>The <code>Trie</code> string set and <code>Trie&lt;TValue&gt;</code> map with prefix queries (autocomplete-style), plus the n-ary <code>Tree&lt;T&gt;</code> with stack-safe traversals.</p>
</div>

</div>

### `Bodu.Threading`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="async-primitives.md">Async coordination primitives</a></h3>
  <p><code>AsyncLock</code>, <code>AsyncSemaphore</code>, <code>AsyncReaderWriterLock</code>, the async reset/countdown events, <code>AsyncLazy&lt;T&gt;</code>, <code>AsyncDebouncer</code>, and the <code>RateGate</code> rate limiter — the async-friendly peers of the BCL synchronization types.</p>
</div>

</div>

### `Bodu.Functional`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="memoization.md">Memoization</a></h3>
  <p><code>Memoizer</code> wraps a pure function in a thread-safe caching delegate — single- and multi-argument, with an optional key comparer.</p>
</div>

<div class="bodu-card">
  <h3><a href="functional-results.md">Options, results, and eithers</a></h3>
  <p><code>Option&lt;T&gt;</code>, <code>Result&lt;T&gt;</code>, and <code>Either&lt;TLeft,TRight&gt;</code> — allocation-free railway primitives with Map/Bind/Match combinators and async companions.</p>
</div>

</div>

### `Bodu.Buffers`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="pooled-buffer-builder.md">Pooled buffer builder</a></h3>
  <p><code>ArrayPool&lt;T&gt;</code>-backed accumulation into a single contiguous result — implements <code>IBufferWriter&lt;T&gt;</code> for span-based writers.</p>
</div>

</div>

### `Bodu` (root namespace)

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="week-pattern.md">WeekPattern</a></h3>
  <p>Immutable bitmask value type for sets of days of the week; supports composition (<code>MTuW</code>), bitwise operators, parsing, and enumeration.</p>
</div>

</div>

## Where to go next

- [Core Foundations guides](../topics/core-foundations.md) — these guides in the context of the full topic.
- [Core Foundations topic overview](../../docs/topics/core-foundations.md) — package map and install command.
- [Bodu.Core introduction](../../docs/core/index.md) — namespaces, headline types, scenarios.
- [Bodu.Core getting started](../../docs/core/getting-started.md) — install and minimal samples.
- [Project introduction](../../docs/introduction.md) — how Bodu.Core relates to the hashing, cryptography, calendar, and text libraries.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
