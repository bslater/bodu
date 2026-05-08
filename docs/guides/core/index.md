---
title: Bodu.Core guides
---

# Bodu.Core guides

Recipe-style walk-throughs for **Bodu.Core** — high-performance, general-purpose building blocks for .NET applications: bounded collections, eviction-aware caches, day-of-week patterns, date and numeric extensions, and parameter validation helpers.

If you're looking for the generated API reference, see the [Bodu.Collections.Generic namespace page](../../apidoc/Bodu.Collections.Generic.md).

## Start here
Recipe-style walk-throughs for **Bodu.Core**, organised by namespace. Each guide on this page is a focused walk-through of one headline type.

If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.Core introduction](../../docs/core/index.md) and the [getting-started page](../../docs/core/getting-started.md). For the auto-generated API reference, see the [Bodu.Collections.Generic namespace page](../../apidoc/Bodu.Collections.Generic.md).

## Namespace map

| Namespace | What lives here | Guides |
|---|---|---|
| `Bodu.Collections.Generic` | Bounded ring-backed collections — `CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey,TValue>`, `RingBackedCollection<T>` base. | [Circular buffer](circular-buffer.md) · [Deque](deque.md) · [Evicting dictionary](evicting-dictionary.md) |
| `Bodu.Collections.Generic.Concurrent` | Thread-safe collection variants — `ConcurrentCircularBuffer<T>`. | (covered in [Circular buffer](circular-buffer.md)) |
| `Bodu` | Root namespace primitives — `WeekPattern`, `IRandomGenerator`, `XorShiftRandom`, `ThrowHelper`. | [WeekPattern](week-pattern.md) |
| `Bodu.Buffers` | Pooled buffer infrastructure — `PooledBufferBuilder<T>`. | (no dedicated guide yet — see API reference) |
| `Bodu.Extensions` | Date, numeric, span, array, and comparable extension methods — `DateTimeExtensions`, `DateOnlyExtensions`, `NumericExtensions`, `ArrayExtensions`, `BufferConverter`, `SpanExtensions`, `IComparableExtensions`. | (no dedicated guide yet — see API reference) |
| `Bodu.Text`, `Bodu.Xml.Linq` | Small text and XML helpers used internally. | — |

## Guides

### `Bodu.Collections.Generic`

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="circular-buffer.html">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — <code>CircularBuffer&lt;T&gt;</code> for single-threaded use and <code>ConcurrentCircularBuffer&lt;T&gt;</code> for safe concurrent access. Configurable overwrite on full.</p>
</div>

<div class="bodu-card">
  <h3><a href="evicting-dictionary.html">Evicting dictionary</a></h3>
  <p><code>EvictingDictionary&lt;TKey, TValue&gt;</code> — a fixed-capacity dictionary that automatically evicts entries using FIFO, LRU, or LFU policies. Drop-in cache primitive.</p>
</div>

<div class="bodu-card">
  <h3><a href="week-pattern.html">Week pattern</a></h3>
  <p><code>WeekPattern</code> — an immutable bitmask value type for sets of days of the week. Supports composition, parsing (<code>MTuWTh</code>), bitwise operators, and enumeration.</p>
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

## Key types at a glance

| Type | Namespace | Purpose |
|---|---|---|
| `CircularBuffer<T>` | `Bodu.Collections.Generic` | Fixed-size FIFO ring; throws or overwrites on full. |
| `ConcurrentCircularBuffer<T>` | `Bodu.Collections.Generic.Concurrent` | Thread-safe variant of `CircularBuffer<T>`. |
| `EvictingDictionary<TKey, TValue>` | `Bodu.Collections.Generic` | Capacity-bounded dictionary with FIFO / LRU / LFU eviction. |
| `WeekPattern` | `Bodu` | Immutable bitmask struct representing selected days of the week. |
| `IRandomGenerator` | `Bodu` | Abstraction over random number generators. |
| `XorShiftRandom` | `Bodu` | Fast non-cryptographic pseudo-random generator (xor-shift). |
| `ThrowHelper` | `Bodu` | Centralised parameter validation helpers (`ThrowIfNull`, `ThrowIfOutOfRange`, …). |
| `PooledBufferBuilder` | `Bodu.Buffers` | `ArrayPool<T>`-backed builder for building byte or char spans without allocation. |
| `DateTimeExtensions` | `Bodu.Extensions` | Date arithmetic helpers — start/end of week, quarter, first weekday, next weekday. |
| `DateOnlyExtensions` | `Bodu.Extensions` | `DateOnly`-specific equivalents plus `Age` calculation. |
| `NumericExtensions` | `Bodu.Extensions` | `ReverseBits`, integer range helpers. |

## Related concepts

- [Algorithm families overview](../algorithm-families.md) — how Bodu.Core relates to the hashing and cryptography libraries.
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
- [Algorithm families](../../docs/algorithm-families.md) — cross-library taxonomy (Bodu.Core's `ThrowHelper` underpins the other libraries).
- [Bodu.Collections.Generic API reference](../../apidoc/Bodu.Collections.Generic.md) — full namespace overview.
