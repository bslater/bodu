---
title: Core Foundations — Concepts
---

# Core Foundations — Concepts

This page covers the vocabulary that spans the Core Foundations topic — the conventions shared by `Bodu.Core`'s collections, buffers, and guards, and by the `Bodu.Text` character-encoding helpers that ship in the same package. Read the [topic overview](core-foundations.md) first for the package map; come back here whenever a term feels imprecise.

## The guard convention

Every Bodu library validates its public parameters through <xref:Bodu.ThrowHelper> rather than hand-rolled checks. The convention is **validate-then-work**: guards form a contiguous block at the top of a member, before any real logic runs, so a method either throws immediately with a precise `ArgumentException`-family error or proceeds knowing its inputs are sound.

```csharp
public static double Average(IReadOnlyList<int> values)
{
    ThrowHelper.ThrowIfNull(values);
    ThrowHelper.ThrowIfZero(values.Count);
    return values.Average();
}
```

Each helper accepts a `[CallerArgumentExpression]`-driven `paramName`, so the call site never repeats the argument name — the compiler captures the expression text (`values`, `values.Count`) and it surfaces as `ParamName` on the thrown exception. The helpers are partitioned by concern (null, numeric, comparison, array, collection, span, type, string), and `[StackTraceHidden]` keeps the helper frame out of stack traces. Centralizing the guards is what keeps exception messages and parameter-name capture identical across every Bodu package.

## Bounded vs. growable

A **fixed capacity is a contract**, not a tuning knob. A bounded collection sizes its backing storage once; when `Count` reaches `Capacity`, it must either reject the next add or evict an existing element. Bodu's ring-backed types expose that choice as a single toggle:

- <xref:Bodu.Collections.Generic.CircularBuffer`1> — `AllowOverwrite = true` silently evicts the oldest element (sliding-window semantics); `false` throws <xref:System.InvalidOperationException> on overflow.
- <xref:Bodu.Collections.Generic.Deque`1> — `AllowGrow = true` doubles the backing array like a `List<T>`; `false` fixes the capacity and throws on overflow. The toggle can be flipped at runtime.

The `Try…` variants (`TryEnqueue`, `TryAddFirst`, `TryAddLast`) substitute a `false` return for the throw, so hot paths can stay exception-free without changing the configured behavior. Choose overwrite when the newest data matters most (telemetry windows, recent-items lists); choose throw or `Try…` when dropping data silently would be a bug.

## Eviction policies

<xref:Bodu.Collections.Generic.EvictingDictionary`2> is the one collection that evicts a *non-end* element: when a new key overflows the capacity, the configured <xref:Bodu.Collections.Generic.EvictingDictionaryPolicy> selects the victim — `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, or `SecondChance`. All six policies share the same `IDictionary<TKey,TValue>` surface and the same overflow trigger; only the selection differs. That makes the policy a deployment decision you can change without touching call sites — start with LRU, switch to Second-Chance if access-tracking overhead shows up in profiles.

## Pooled buffers vs. allocation

A **pooled buffer** trades garbage-collector pressure for an explicit ownership protocol. <xref:Bodu.Buffers.PooledBufferBuilder`1> rents its array from <xref:System.Buffers.ArrayPool`1> instead of `new T[]`, grows by re-renting, and returns the array to the pool on `Dispose`. The rules that follow:

- The builder owns the rented array — always `using` it, or the array leaks from the pool.
- `WrittenSpan` / `WrittenMemory` are zero-copy views valid only while the builder is alive; copy out before disposal if the data must outlive it.
- `Reset` reuses the current rented array without a pool round-trip, which makes the builder cheap to recycle inside loops.

Because it implements <xref:System.Buffers.IBufferWriter`1>, the builder drops into any span-based pipeline that the BCL's writer-style APIs accept. Prefer it over `MemoryStream` or `List<byte>` when buffers are short-lived, sized unpredictably, and built on hot paths.

## Character-encoding vocabulary

The `Bodu.Text` namespace works in terms the BCL defines but does not always make ergonomic:

- **Preamble / BOM** — the byte-order mark some encoders emit at the start of a stream (the five canonical Unicode preambles: UTF-8, UTF-16 LE/BE, UTF-32 LE/BE). <xref:Bodu.Text.EncodingDetection> identifies an encoding from its preamble with a non-allocating `TryDetectByPreamble`, and the extension surface can decode while skipping a leading preamble or emit one when writing.
- **Transcoding** — converting bytes from one character encoding to another without materializing an intermediate `string`. <xref:Bodu.Text.EncodingExtensions> provides span-based transcode overloads.
- **Validation** — confirming that a byte span is well-formed for a given encoding before trusting it, rather than discovering malformed sequences mid-decode.
- **Span-first, UTF-8-first surfaces** — every operation has overloads over `ReadOnlySpan<char>` / `ReadOnlySpan<byte>` and UTF-8-specific fast paths (`ToUtf8Bytes`, pooled conversions on <xref:Bodu.Text.StringEncodingExtensions>), so the common cases avoid intermediate allocations entirely.

The distinction worth internalizing: `Bodu.Text` handles **character encodings** (bytes ↔ text through `System.Text.Encoding`); the separate `Bodu.Text.Encoding` package handles **binary-to-text codecs** (Base16/32/58/64/85). Similar names, different jobs.

## Member concept pages

| Member | Concepts coverage |
|---|---|
| Bodu.Core | [Bodu.Core — Core concepts](../core/concepts.md) — fixed-capacity and ring-backed collections, eviction policies, `WeekPattern`, pooled buffers, `ThrowHelper`, the random-generator abstraction, multi-value / multiset semantics, range-keyed lookups, and the calendar-shape extensions. |
| Bodu.Text | No separate concepts page — the [Bodu.Text introduction](../text/index.md) covers the encoding-detection and transcoding vocabulary alongside its type map. |

For the topic-level package map and decision table, return to the [Core Foundations overview](core-foundations.md); for hands-on walk-throughs, see the [Core Foundations guides](../../guides/topics/core-foundations.md).
