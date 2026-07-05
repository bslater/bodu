---
title: Core Foundations — Overview
---

# Core Foundations

**Bodu.Core** is the foundation of the entire Bodu suite. Every other Bodu package depends on it — `Bodu.IO.Hashing`, `Bodu.Security.Cryptography`, `Bodu.Globalization.Calendar`, the text and serialization packages, `Bodu.Numerics`, and `Bodu.Financial` all reference it for shared primitives such as the centralized <xref:Bodu.ThrowHelper> argument-validation catalogue, <xref:Bodu.WeekPattern>, the calendar-shape enums, and pooled buffers. It is also a useful library in its own right: a writer-style pooled buffer builder, async coordination and railway primitives, and a large set of date, numeric, span, and array extensions. The specialized collection catalogue — the bounded, ordered, navigable, graph, tree, and probabilistic collections — ships in the companion **[`Bodu.Collections`](../collections/index.md)** package (namespaces unchanged; it depends on `Bodu.Core`), and the thread-safe variants in **[`Bodu.Collections.Concurrent`](../collections-concurrent/index.md)** (which depends on `Bodu.Collections`).

The package additionally ships the **`Bodu.Text`** namespace — character-encoding helpers over <xref:System.Text.Encoding?displayProperty=nameWithType> that add byte-order-mark detection, span- and UTF-8-friendly transcoding, preamble handling, and validation. `Bodu.Text` is a namespace inside the `Bodu.Core` package, not a separate package; installing `Bodu.Core` gives you both surfaces.

## Members of this topic

| Package | Status | What it provides | Docs |
|---|---|---|---|
| **Bodu.Core** | Stable | The `WeekPattern` value type, `PooledBufferBuilder<T>`, date / numeric / span extensions, the enumerable / dictionary / list extension surfaces, threading and functional primitives, and the `ThrowHelper` validation catalogue. | [Introduction](../core/index.md) · [Concepts](../core/concepts.md) · [Getting started](../core/getting-started.md) |
| **Bodu.Collections** | Stable | The specialized collection catalogue (namespaces unchanged; depends on `Bodu.Core`): bounded collections (`CircularBuffer<T>`, `Deque<T>`, `EvictingDictionary<TKey,TValue>` with TTL expiry), the insertion/access-ordered `SequencedDictionary<TKey,TValue>`, navigable and index-aware sets, priority queues, range-keyed lookups and interval trees, the `.Graphs` and `.Trees` sub-namespaces, and the `Bodu.Collections.Probabilistic` sketches. | [Introduction](../collections/index.md) · [Concepts](../collections/concepts.md) · [Getting started](../collections/getting-started.md) |
| **Bodu.Collections.Concurrent** | Stable | The thread-safe collection companion (depends on `Bodu.Collections`): the lock-free `ConcurrentCircularBuffer<T>` (Vyukov MPMC, `IProducerConsumerCollection<T>`) and the lock-striped `ConcurrentHashSet<T>`, both with snapshot enumeration. | [Introduction](../collections-concurrent/index.md) · [Concepts](../collections-concurrent/concepts.md) · [Getting started](../collections-concurrent/getting-started.md) |
| **Bodu.Text** *(namespace — ships inside the `Bodu.Core` package)* | Stable | BOM-based `EncodingDetection`, plus `EncodingExtensions` and `StringEncodingExtensions` for span-, UTF-8-, and pooled-buffer-friendly transcoding, preamble handling, and validation over `System.Text.Encoding`. | [Introduction](../text/index.md) |

## The shape of the topic

`Bodu.Core` is organized into focused namespaces, each with a clear responsibility. The four you will reach for most often:

- **`Bodu`** — root-namespace primitives: <xref:Bodu.ThrowHelper> (the argument-validation catalogue every Bodu library calls into), <xref:Bodu.WeekPattern> (an immutable day-of-week bitmask), and the <xref:Bodu.IRandomGenerator> abstraction with its <xref:Bodu.XorShiftRandom> implementation.
- **`Bodu.Collections.Generic`** (ships in the `Bodu.Collections` package) — bounded ring-backed collections (<xref:Bodu.Collections.Generic.CircularBuffer`1>, <xref:Bodu.Collections.Generic.Deque`1>), the policy-driven <xref:Bodu.Collections.Generic.EvictingDictionary`2> cache, the insertion/access-ordered <xref:Bodu.Collections.Generic.SequencedDictionary`2>, index-aware sets (<xref:Bodu.Collections.Generic.IndexedSet`1>, <xref:Bodu.Collections.Generic.OrderedSet`1>), the heap-backed <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2>, multi-map / multi-set types, and range-keyed lookups. The thread-safe <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> and <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> live in the `.Concurrent` sub-namespace, shipped by the `Bodu.Collections.Concurrent` package.
- **`Bodu.Buffers`** — <xref:Bodu.Buffers.PooledBufferBuilder`1>, an `ArrayPool<T>`-backed builder that assembles byte or character spans without allocation and slots into standard `IBufferWriter<T>` pipelines.
- **`Bodu.Text`** — <xref:Bodu.Text.EncodingDetection>, <xref:Bodu.Text.EncodingExtensions>, and <xref:Bodu.Text.StringEncodingExtensions>: the character-encoding surface that turns bytes into text and back through `System.Text.Encoding`, correctly and efficiently.

Rounding out the package, `Bodu.Extensions` carries the date, numeric, span, and array extension methods (first-Monday-of-month arithmetic, ISO week-of-year, bit rotation and reversal, clamping), and `Bodu.Globalization.Extensions` adds culture-aware helpers over <xref:System.Globalization.DateTimeFormatInfo>. See the [Bodu.Core introduction](../core/index.md) for the complete namespace map.

## How the pieces fit

`Bodu.Core` is the one shared runtime dependency across the suite — every other topic's packages build on it. The [project introduction](../introduction.md) spells out the dependency map; in topic terms:

- **[Hashing & Cryptography](hashing-and-cryptography.md)** — `Bodu.IO.Hashing` and `Bodu.Security.Cryptography` take `ThrowHelper` for argument validation and share the pooled-buffer infrastructure.
- **[Globalization & Calendars](globalization-and-calendars.md)** — `Bodu.Globalization.Calendar` builds on `WeekPattern` and the calendar-shape enums (`CalendarQuarterDefinition`, `WorkingDaysOfWeek`, `WeekOrdinal`) that live in `Bodu.Core`.
- **[Text & Serialization](text-and-serialization.md)** — `Bodu.Text.Encoding`, `Bodu.Text.Formats`, `Bodu.Text.Bencode`, and `Bodu.Text.Toml` validate through `ThrowHelper` and sit alongside the `Bodu.Text` character-encoding helpers.
- **[Configuration](configuration.md)** — `Bodu.Text.Configuration` and its `Microsoft.Extensions.Configuration` bridge sit on the same foundation via `Bodu.Text.Formats`.
- **[Numerics & Financial](numerics-and-financial.md)** — `Bodu.Numerics` and `Bodu.Financial` validate through `ThrowHelper`; `Bodu.Financial` additionally builds on `Bodu.Numerics`.

Because `ThrowHelper` is the sole dependency most packages take on `Bodu.Core`, exception messages, parameter-name capture, and `[StackTraceHidden]` behavior stay consistent across every Bodu library. Adopting `Bodu.Core` in your own code buys the same consistency: one guard catalogue, one set of messages, one stack-trace shape.

## Which do I need?

| Scenario | Reach for | Notes |
|---|---|---|
| Fixed-capacity FIFO queue (sliding window or bounded-throw) | <xref:Bodu.Collections.Generic.CircularBuffer`1> | `AllowOverwrite` toggles between evict-the-oldest and throw-when-full. Thread-safe variant: <xref:Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer`1> (in `Bodu.Collections.Concurrent`). |
| LRU / MRU / LFU / FIFO cache | <xref:Bodu.Collections.Generic.EvictingDictionary`2> | Six policies via <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy>, including Random and Second-Chance. |
| Insertion- or access-ordered dictionary with O(1) first/last access | <xref:Bodu.Collections.Generic.SequencedDictionary`2> | Java `LinkedHashMap` shape; unbounded. Access-order mode is the building block for a hand-rolled LRU over `TryRemoveFirst`. |
| Double-ended queue with O(1) ends | <xref:Bodu.Collections.Generic.Deque`1> | `AllowGrow` toggles between auto-resize and fixed-capacity-throw modes. |
| Priority queue with in-place priority updates | <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> | O(1) lookup-by-element plus `Update` / `EnqueueOrUpdate` — the operations Dijkstra, Prim, and A* require. |
| Insertion-ordered set, indexable like a list | <xref:Bodu.Collections.Generic.IndexedSet`1> | O(1) `Contains`, `IndexOf`, and indexed read; duplicates rejected on add. |
| Pooled byte / char building without allocation | <xref:Bodu.Buffers.PooledBufferBuilder`1> | `ArrayPool<T>`-backed; implements `IBufferWriter<T>`; dispose to return the rented array. |
| Range-keyed lookup (interval → value, or interval membership) | <xref:Bodu.Collections.Generic.RangeDictionary`2>, <xref:Bodu.Collections.Generic.RangeSet`1> | Half-open `[start, end)` keys with O(log n) lookup. |
| One key mapping to many values | <xref:Bodu.Collections.Generic.MultiValueDictionary`2> | The indexer returns an empty live view, never `null`. |
| Day-of-week masks you can union, intersect, and parse | <xref:Bodu.WeekPattern> | Immutable 7-bit bitmask value type; `Parse("MTuWThF")`, bitwise operators, presets. |
| Date arithmetic — first Monday of month, ISO week-of-year, age | <xref:Bodu.Extensions.DateTimeExtensions>, <xref:Bodu.Extensions.DateOnlyExtensions> | Parameterized by the calendar-shape enums (quarter definitions, working weeks, week ordinals). |
| Bit / byte rotation and reversal | <xref:Bodu.Extensions.NumericExtensions> | `ReverseBits`, `RotateBitsLeft` / `Right`, `ReverseBytes` over unsigned integers. |
| Detect a file's encoding from its byte-order mark | <xref:Bodu.Text.EncodingDetection> | Non-allocating `TryDetectByPreamble` over the five canonical Unicode preambles. |
| Span- and UTF-8-friendly transcoding and validation | <xref:Bodu.Text.EncodingExtensions>, <xref:Bodu.Text.StringEncodingExtensions> | Encode, decode, transcode, preamble handling, pooled conversions. |
| Centralized argument guards in your own code | <xref:Bodu.ThrowHelper> | `ThrowIfNull`, `ThrowIfZero`, range / enum / array / span checks; `[CallerArgumentExpression]` captures parameter names automatically. |

Not sure which collection fits? The [Choosing a collection](../../guides/core/choosing-a-collection.md) guide is a full decision tree across the namespace — by access pattern, by capacity and lifecycle, and by concurrency. `Bodu.Core` deliberately does not duplicate BCL primitives; every type adds a contract that `List<T>`, `Dictionary<TKey,TValue>`, or `Queue<T>` does not provide.

## Design notes

The same conventions that govern the rest of the suite start here:

- **Small by intent.** Each namespace solves one coherent problem; anything the BCL already covers well is left to the BCL.
- **Nullable reference types** are enabled throughout, so public APIs make their null-intent explicit — and the `ThrowHelper` guards enforce it at runtime.
- **Allocation-aware by default.** The collections are ring-backed to avoid element shifting, the buffer builder rents from `ArrayPool<T>`, and the `Bodu.Text` surface is span-first with UTF-8 fast paths.
- **Documentation-first.** Every public member carries XML documentation; the API reference on this site is generated directly from the source.

## Install

```bash
dotnet add package Bodu.Core
dotnet add package Bodu.Collections
dotnet add package Bodu.Collections.Concurrent
```

Targets `net8.0`. No external runtime dependencies. The `Bodu.Text` namespace is included in `Bodu.Core` — no separate install. Add `Bodu.Collections` for the specialized collection catalogue (it depends on `Bodu.Core`) and `Bodu.Collections.Concurrent` for the thread-safe variants (it depends on `Bodu.Collections`); `Bodu.Core` alone suffices for the guards, buffers, extensions, and encoding surfaces.

A taste of the two surfaces together:

```csharp
using Bodu;
using Bodu.Collections.Generic;
using Bodu.Text;

// Bounded collections — a sliding window of the last four samples.
var window = new CircularBuffer<int>(capacity: 4, allowOverwrite: true);
window.Enqueue(1);
window.Enqueue(2);

// Argument guards — one line per rule, parameter name captured automatically.
static double Average(IReadOnlyList<int> values)
{
    ThrowHelper.ThrowIfNull(values);
    ThrowHelper.ThrowIfZero(values.Count);
    return values.Average();
}

// Character encodings — detect a BOM and decode, skipping the preamble.
if (EncodingDetection.TryDetectByPreamble(bytes, out var encoding))
{
    string text = encoding.GetStringSkippingPreamble(bytes);
}
```

## Where to go next

- **[Core Foundations — Concepts](core-foundations-concepts.md)** — the cross-member vocabulary: guard conventions, bounded vs. growable capacity, eviction policies, pooled buffers, and character-encoding terms.
- **[Bodu.Core introduction](../core/index.md)** — the foundation package's namespace map and headline types.
- **[Bodu.Collections introduction](../collections/index.md)** — the specialized collection catalogue.
- **[Bodu.Collections.Concurrent introduction](../collections-concurrent/index.md)** — the thread-safe collection companion.
- **[Bodu.Text introduction](../text/index.md)** — the character-encoding surface that ships inside `Bodu.Core`.
- **[Getting started](../core/getting-started.md)** — install plus a minimal sample for each headline type.
- **[Core Foundations guides](../../guides/topics/core-foundations.md)** — the recipe-style walk-throughs for this topic.
- **API reference:** [Bodu.Collections.Generic](xref:Bodu.Collections.Generic) · [Bodu.Collections.Generic.Concurrent](xref:Bodu.Collections.Generic.Concurrent) · [Bodu.Text](xref:Bodu.Text).
