---
title: Bodu.Core guides
---

# Bodu.Core guides

Recipe-style walk-throughs for **Bodu.Core**, organized by namespace. Each guide on this page is a focused walk-through of one headline type.

If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.Core introduction](../../docs/core/index.md) and the [getting-started page](../../docs/core/getting-started.md). For the auto-generated API reference, see the [Bodu.Collections.Generic namespace page](xref:Bodu.Collections.Generic).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.Collections.Generic` | Bounded ring-backed collections — `CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey,TValue>`, `RingBackedCollection<T>` base. | [Circular buffer](circular-buffer.md) · [Deque](deque.md) · [Evicting dictionary](evicting-dictionary.md) |
| `Bodu.Collections.Generic.Concurrent` | Thread-safe collection variants — `ConcurrentCircularBuffer<T>`. | (covered in [Circular buffer](circular-buffer.md#thread-safety)) |
| `Bodu` | Root namespace primitives — `WeekPattern`, `IRandomGenerator`, `XorShiftRandom`, `ThrowHelper`. | [WeekPattern](week-pattern.md) |
| `Bodu.Buffers` | Pooled buffer infrastructure — `PooledBufferBuilder<T>`. | (no dedicated guide yet — see API reference) |
| `Bodu.Extensions` | Date, numeric, span, array, and comparable extension methods — `DateTimeExtensions`, `DateOnlyExtensions`, `NumericExtensions`, `ArrayExtensions`, `BufferConverter`, `SpanExtensions`, `IComparableExtensions`. | (no dedicated guide yet — see API reference) |
| `Bodu.Text`, `Bodu.Xml.Linq` | Small text and XML helpers used internally. | — |

## Guides

### `Bodu.Collections.Generic`

<div class="bodu-cards">

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

</div>

### `Bodu` (root namespace)

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="week-pattern.md">WeekPattern</a></h3>
  <p>Immutable bitmask value type for sets of days of the week; supports composition (<code>MTuW</code>), bitwise operators, parsing, and enumeration.</p>
</div>

</div>

## Where to go next

- [Bodu.Core introduction](../../docs/core/index.md) — namespaces, headline types, scenarios.
- [Bodu.Core getting started](../../docs/core/getting-started.md) — install and minimal samples.
- [Project introduction](../../docs/introduction.md) — how Bodu.Core relates to the hashing, cryptography, calendar, and text libraries.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
