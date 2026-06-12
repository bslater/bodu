---
title: Core Foundations — Guides
---

# Core Foundations — Guides

Recipe-style walk-throughs for the **Core Foundations** topic — the `Bodu.Core` package and the `Bodu.Text` namespace it ships. These are the building blocks the rest of the suite stands on: bounded collections, eviction-aware caches, pooled buffers, day-of-week patterns, argument guards, and character-encoding helpers.

If you have not yet installed the package, start with the [topic overview](../../docs/topics/core-foundations.md) for the package map and install command, and the [topic concepts page](../../docs/topics/core-foundations-concepts.md) for the shared vocabulary.

## Bodu.Core guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../core/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.Core</code>, organized by namespace — every collection, buffer, and root-namespace primitive, and which guide covers each.</p>
</div>

<div class="bodu-card">
  <h3><a href="../core/choosing-a-collection.md">Choosing a collection</a></h3>
  <p>The decision guide — a quick decision tree plus per-axis tables (access pattern, capacity and lifecycle, concurrency) that map requirements to the correct type.</p>
</div>

<div class="bodu-card">
  <h3><a href="../core/circular-buffer.md">Circular buffer</a></h3>
  <p>Fixed-capacity FIFO ring buffer — single-threaded <code>CircularBuffer&lt;T&gt;</code> and thread-safe <code>ConcurrentCircularBuffer&lt;T&gt;</code>, overwrite mode, peek / dequeue / try-enqueue patterns.</p>
</div>

<div class="bodu-card">
  <h3><a href="../core/evicting-dictionary.md">Evicting dictionary</a></h3>
  <p>Capacity-bounded key-value store with FIFO, LRU, LFU, MRU, Random, and Second-Chance eviction policies — the drop-in cache primitive.</p>
</div>

<div class="bodu-card">
  <h3><a href="../core/pooled-buffer-builder.md">Pooled buffer builder</a></h3>
  <p><code>PooledBufferBuilder&lt;T&gt;</code> — <code>ArrayPool</code>-backed building of byte and character spans without allocation, with the ownership and lifetime rules.</p>
</div>

<div class="bodu-card">
  <h3><a href="../core/week-pattern.md">WeekPattern</a></h3>
  <p>Immutable bitmask value type for day-of-week sets — composition (<code>MTuW</code>), bitwise operators, parsing, and enumeration.</p>
</div>

</div>

## Bodu.Text

The `Bodu.Text` character-encoding helpers (BOM detection, span- and UTF-8-friendly transcoding, preamble handling, validation) are covered by the [Bodu.Text introduction](../../docs/text/index.md) and the [Bodu.Text API reference](xref:Bodu.Text) rather than dedicated guides — the surface is a set of focused extension methods whose scenarios the introduction maps directly to calls.

## Start here

1. **[Topic overview](../../docs/topics/core-foundations.md)** — what ships in the package, the dependency map, and the "which do I need?" table.
2. **[Topic concepts](../../docs/topics/core-foundations-concepts.md)** — the guard convention, bounded vs. growable capacity, eviction policies, pooled buffers, and the encoding vocabulary.
3. **[Choosing a collection](../core/choosing-a-collection.md)** — pick the right type before writing code against the wrong one.
4. **The walk-through for your type** — [circular buffer](../core/circular-buffer.md), [evicting dictionary](../core/evicting-dictionary.md), [pooled buffer builder](../core/pooled-buffer-builder.md), or [WeekPattern](../core/week-pattern.md); the [guide overview](../core/index.md) lists the rest (deque, indexed priority queue, ordered sets, multiset, multi-value dictionary, range-keyed lookups, segmented buffer, concurrent collections).

## Where to go next

- **[Core Foundations overview](../../docs/topics/core-foundations.md)** — the topic landing page on the docs side.
- **[Core Foundations concepts](../../docs/topics/core-foundations-concepts.md)** — cross-member vocabulary.
- **[Bodu.Core introduction](../../docs/core/index.md)** and **[getting started](../../docs/core/getting-started.md)** — namespace map and minimal samples.
- **API reference:** [Bodu.Collections.Generic](xref:Bodu.Collections.Generic) · [Bodu.Text](xref:Bodu.Text).
