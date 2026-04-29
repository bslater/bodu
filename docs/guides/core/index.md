---
title: Bodu.Core guides
---

# Bodu.Core guides

Recipe-style walk-throughs for **Bodu.Core** — high-performance, general-purpose building blocks for .NET applications: bounded collections, eviction-aware caches, day-of-week patterns, date and numeric extensions, and parameter validation helpers.

If you're looking for the generated API reference, see the [Bodu.Collections.Generic namespace page](../../apidoc/Bodu.Collections.Generic.md).

## Start here

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="circular-buffer.html">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — <code>CircularBuffer&lt;T&gt;</code> for single-threaded use and <code>ConcurrentCircularBuffer&lt;T&gt;</code> for safe concurrent access. Configurable overwrite on full.</p>
</div>

<div class="bodu-card">
  <h3><a href="deque.html">Deque</a></h3>
  <p>Double-ended queue — <code>Deque&lt;T&gt;</code> with <code>AddFirst</code> / <code>AddLast</code> / <code>RemoveFirst</code> / <code>RemoveLast</code> in O(1). The <code>AllowGrow</code> flag toggles between automatic resize and a fixed-capacity throw-when-full mode.</p>
</div>

<div class="bodu-card">
  <h3><a href="evicting-dictionary.html">Evicting dictionary</a></h3>
  <p><code>EvictingDictionary&lt;TKey, TValue&gt;</code> — a fixed-capacity dictionary that automatically evicts entries using FIFO, LRU, or LFU policies. Drop-in cache primitive.</p>
</div>

<div class="bodu-card">
  <h3><a href="week-pattern.html">Week pattern</a></h3>
  <p><code>WeekPattern</code> — an immutable bitmask value type for sets of days of the week. Supports composition, parsing (<code>MTuWTh</code>), bitwise operators, and enumeration.</p>
</div>

</div>

## Key types at a glance

| Type | Namespace | Purpose |
|---|---|---|
| `CircularBuffer<T>` | `Bodu.Collections.Generic` | Fixed-size FIFO ring; throws or overwrites on full. |
| `ConcurrentCircularBuffer<T>` | `Bodu.Collections.Generic.Concurrent` | Thread-safe variant of `CircularBuffer<T>`. |
| `Deque<T>` | `Bodu.Collections.Generic` | Double-ended queue. Growable by default; toggle `AllowGrow` to lock to a fixed capacity. |
| `RingBackedCollection<T>` | `Bodu.Collections.Generic` | Abstract base shared by `CircularBuffer<T>` and `Deque<T>` — extension point for new ring-backed collection types. |
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
- [Bodu.Collections.Generic API reference](../../apidoc/Bodu.Collections.Generic.md) — full namespace overview.
