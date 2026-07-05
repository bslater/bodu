---
title: Probabilistic collections (sketches)
---

# Probabilistic collections (sketches)

The `Bodu.Collections.Probabilistic` namespace ships three approximate "sketch" data structures that trade exactness for a fixed, small memory footprint: <xref:Bodu.Collections.Probabilistic.BloomFilter`1> for approximate set membership, <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> for approximate per-element frequencies, and <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> for approximate distinct-element counts. Each is sized once from its constructor arguments, never grows, and answers queries over arbitrarily long streams in O(1) space — the accuracy you paid for at construction is the accuracy you get, regardless of how many elements pass through.

> [!WARNING]
> These types are **approximate — do not use them for exact membership or exact counting.** A `BloomFilter<T>` can report an element as present that was never added (a false positive); a `CountMinSketch<T>` can report a count higher than the truth (an overestimate); a `HyperLogLog<T>` estimate is a statistical figure with a known standard error, not a tally. When the answer must be exact, use <xref:System.Collections.Generic.HashSet`1>, <xref:Bodu.Collections.Generic.Multiset`1>, or a plain <xref:System.Collections.Generic.Dictionary`2> — the sketches are for the regime where the exact structure no longer fits in memory.

The error is one-sided and quantified, which is what makes the types usable as contracts rather than heuristics:

- **`BloomFilter<T>` never produces a false negative.** An added element is *always* reported as present; only never-added elements can be misreported. The filter is sized from `expectedItems` (n) and `falsePositiveRate` (p) using the standard Bloom formulas — `m = ⌈−n·ln(p) / ln(2)²⌉` bits and `k = max(1, round(m/n·ln 2))` hash probes — so the observed false-positive rate approaches the design rate `p` as the element count approaches `n`, and rises above it beyond that point. `EstimatedFalsePositiveRate` reports the rate implied by the current fill.
- **`CountMinSketch<T>` never underestimates.** `EstimateCount` returns at least the element's true count, and with probability at least `1 − δ` returns at most the true count plus `ε · TotalCount`. The `epsilon` / `delta` constructor arguments size the sketch (`w = ⌈e/ε⌉` counters per row, `d = ⌈ln(1/δ)⌉` rows). Negative updates are rejected by design — a removal could break the never-underestimate guarantee.
- **`HyperLogLog<T>` carries a relative standard error of about `1.04/√m`,** where `m = 2^precision` one-byte registers. Each extra bit of precision doubles the footprint and improves the error by √2: precision 4 is 16 registers at ~26% error, precision 14 is 16,384 registers (16 KiB) at ~0.81% error. `StandardError` exposes the figure; roughly 65% of estimates fall within one standard error of the truth and roughly 99% within three.
- **All entropy comes from the comparer's 32-bit hash.** Each type hashes through the supplied <xref:System.Collections.Generic.IEqualityComparer`1> (or the default comparer): the 32-bit `GetHashCode` is expanded through a deterministic SplitMix64-style avalanche into the 64-bit values the sketch consumes (Kirsch–Mitzenmacher double hashing for the filter and sketch, a single hash for HyperLogLog). The expansion cannot manufacture entropy the comparer did not supply, so the achievable accuracy floor is bounded by the collision rate of that 32-bit hash — two elements with equal comparer hashes are indistinguishable to a sketch.
- **Randomized string hashes make exports process-local.** The expansion is platform-stable, but `string.GetHashCode()` is randomized per process — exported state for `string` elements (or any type with a randomized hash) under the default comparer is only meaningful when re-imported within the same process. Supply a custom comparer with a stable hash when exports must cross process boundaries.
- **Merges require compatible instances.** `UnionWith` / `MergeWith` demand identical geometry (bit count and hash count; width and depth; precision) and the same or an equal comparer — in practice, instances constructed with the same parameters. A Bloom union ORs the bits; a count-min merge adds counters cell-wise; a HyperLogLog merge takes the register-wise maximum and is lossless and idempotent (shared elements are not double-counted).
- **Export is an opaque, version-checked snapshot, not a wire contract.** `TryExport` / `Export` / `GetExportByteCount` and the static `Import` round-trip a sketch's state as bytes, but the layout may change between library versions with a corresponding version-byte bump. The comparer is not part of the exported state — `Import` requires the caller to re-supply it.
- **None of the sketches is thread-safe.** Concurrent reads and writes require external synchronization.

## Pattern 1 — approximate membership with `BloomFilter<T>`

Use a Bloom filter as a cheap front gate: a `false` from `MightContain` is definitive ("definitely never added") and lets you skip the expensive lookup; a `true` means "probably added" and falls through to the authoritative store.

```csharp
using Bodu.Collections.Probabilistic;

// Track ~1,000,000 previously crawled URLs in ~1.14 MiB with a 1% false-positive budget.
var crawled = new BloomFilter<string>(expectedItems: 1_000_000, falsePositiveRate: 0.01);

crawled.Add("https://example.com/");

if (!crawled.MightContain(candidateUrl))
{
    // Definitely not crawled yet — no false negatives, ever.
    Enqueue(candidateUrl);
    crawled.Add(candidateUrl);
}
// else: probably crawled — ~1% of never-crawled URLs are skipped as false positives.
```

Elements cannot be removed (`Clear` resets the whole filter), and filling past `ExpectedItems` pushes the observed false-positive rate above `DesignFalsePositiveRate` — watch `EstimatedFalsePositiveRate` when the stream size is uncertain. Shard-per-worker filters built with the same parameters can be combined with `UnionWith`.

## Pattern 2 — approximate frequencies with `CountMinSketch<T>`

Use a count-min sketch for heavy-hitter and rate questions over high-cardinality streams — "roughly how many times has this key occurred?" — where a `Dictionary<TKey,long>` would grow without bound.

```csharp
using Bodu.Collections.Probabilistic;

// 1% additive error (relative to the total stream) at 99% confidence: 272 × 5 counters, ~10.6 KiB.
var hits = new CountMinSketch<string>(epsilon: 0.01, delta: 0.01);

hits.Add("GET /index");
hits.Add("GET /search", 41);

// >= the true count; within true + 0.01 * TotalCount with probability >= 0.99.
var estimate = hits.EstimateCount("GET /search");
```

The bound is additive in `TotalCount`, so estimates for rare elements are dominated by noise from the heavy elements sharing their counters — the sketch shines at identifying *frequent* elements, not at counting rare ones precisely. Per-shard sketches with the same parameters combine with `MergeWith`, which sums the streams.

## Pattern 3 — approximate distinct counts with `HyperLogLog<T>`

Use HyperLogLog to count *distinct* elements — unique visitors, distinct keys, cardinality of a join column — in a fixed few kilobytes where a `HashSet<T>` would hold every element.

```csharp
using Bodu.Collections.Probabilistic;

// 2^14 = 16,384 one-byte registers (~16 KiB) at ~0.81% standard error.
var visitors = new HyperLogLog<int>(precision: 14);

foreach (var visitorId in clickStream)
    visitors.Add(visitorId); // duplicates leave the sketch unchanged

Console.WriteLine(visitors.EstimateCardinality()); // ~ the number of distinct ids
```

`MergeWith` is the standout feature: merging per-day (or per-shard) sketches yields exactly the sketch the concatenated stream would have produced, so distinct counts compose across partitions without double-counting elements seen on both sides. Note the estimate is not monotonic at fine granularity — adding one element can move it by more or less than one — and the 32-bit comparer hash caps the number of distinguishable elements at 2³².

## When to use which sketch

| Question | Reach for | Guarantee | Exact alternative |
|---|---|---|---|
| "Have I seen this element before?" | <xref:Bodu.Collections.Probabilistic.BloomFilter`1> | No false negatives; false positives at the design rate `p` when filled to `ExpectedItems`. | <xref:System.Collections.Generic.HashSet`1>, <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> |
| "Roughly how many times has this element occurred?" | <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> | Never underestimates; overestimates by at most `ε · TotalCount` with probability ≥ `1 − δ`. | <xref:Bodu.Collections.Generic.Multiset`1> |
| "Roughly how many *distinct* elements have I seen?" | <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> | Relative standard error ≈ `1.04/√m` in one byte per register. | <xref:System.Collections.Generic.HashSet`1>.Count |

All three share the same operational shape — comparer-driven hashing, parameter-compatible merging, `Clear`, and the opaque version-checked export/import — so a pipeline that shards one sketch type can shard the others the same way.

For the exact-semantics collections these types deliberately are not, see [Choosing a collection](choosing-a-collection.md); for exact multiplicity counting, see [Multiset](multiset.md). The [Bodu.Collections.Probabilistic API reference](xref:Bodu.Collections.Probabilistic) has the full member-level contracts.
