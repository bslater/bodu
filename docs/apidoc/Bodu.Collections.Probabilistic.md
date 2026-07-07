---
uid: Bodu.Collections.Probabilistic
---

![Bodu.Collections.Probabilistic](~/images/hero-collections.svg)

## Purpose

**Bodu.Collections.Probabilistic** holds the approximate "sketch" data structures of the `Bodu.Collections` package: <xref:Bodu.Collections.Probabilistic.BloomFilter`1> (approximate set membership), <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> (approximate per-element frequencies), and <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> (approximate distinct-element counts). Each is sized once from its constructor arguments, never grows, and answers queries over arbitrarily long streams in O(1) space — trading exactness for a fixed, small memory footprint.

The error each sketch carries is one-sided and quantified, which makes the types usable as contracts rather than heuristics: a Bloom filter never produces a false negative, a count-min sketch never underestimates, and a HyperLogLog estimate comes with a known standard error. When the answer must be exact, reach for <xref:System.Collections.Generic.HashSet`1>, <xref:Bodu.Collections.Generic.Multiset`1>, or <xref:System.Collections.Generic.Dictionary`2> instead — the sketches are for the regime where the exact structure no longer fits in memory.

## Static documentation

- **[Probabilistic collections (sketches)](~/guides/core/probabilistic-collections.md)** — the three usage patterns, the accuracy contracts, sizing guidance, and the when-to-use-which table.
- **[Introduction](~/docs/collections/index.md)** — where the sketches sit among the exact-semantics collections.

## Key types

- <xref:Bodu.Collections.Probabilistic.BloomFilter`1> — approximate set membership with **no false negatives**: an added element is always reported as present; only never-added elements can be misreported. Sized from `expectedItems` (n) and `falsePositiveRate` (p) via the standard Bloom formulas; `EstimatedFalsePositiveRate` tracks the rate implied by the current fill, which rises above the design rate once the filter is filled past `ExpectedItems`. `UnionWith` ORs a compatible filter's bits into this one.
- <xref:Bodu.Collections.Probabilistic.CountMinSketch`1> — approximate frequency counting that **never underestimates**: `EstimateCount` returns at least the element's true count, and with probability at least `1 − δ` returns at most the true count plus `ε · TotalCount`. The `epsilon` / `delta` constructor arguments size the sketch (`w = ⌈e/ε⌉` counters per row, `d = ⌈ln(1/δ)⌉` rows). Negative updates are rejected by design — a removal could break the never-underestimate guarantee. `MergeWith` adds a compatible sketch's counters cell-wise.
- <xref:Bodu.Collections.Probabilistic.HyperLogLog`1> — approximate distinct counting with a relative standard error of about `1.04/√m`, where `m = 2^precision` one-byte registers (`StandardError` exposes the figure). Each extra bit of precision doubles the footprint and improves the error by √2. `MergeWith` takes the register-wise maximum of a compatible sketch and is lossless and idempotent — shared elements are not double-counted.

## Example

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

// Approximate frequencies and distinct counts over the same stream.
var hits = new CountMinSketch<string>(epsilon: 0.01, delta: 0.01);
hits.Add("GET /search", 41);
var estimate = hits.EstimateCount("GET /search"); // >= 41, usually exactly 41

var visitors = new HyperLogLog<int>(precision: 14); // 16 KiB, ~0.81% standard error
visitors.Add(visitorId);
var distinct = visitors.EstimateCardinality();
```

## Notes

- **Approximate by design.** A `BloomFilter<T>` can report a never-added element as present (a false positive); a `CountMinSketch<T>` can report a count higher than the truth (an overestimate); a `HyperLogLog<T>` estimate is a statistical figure with a known standard error, not a tally. The error is one-sided in each case — the direction it cannot err in is the contract.
- **All entropy comes from the comparer's 32-bit hash.** Each type hashes through the supplied <xref:System.Collections.Generic.IEqualityComparer`1> (or the default comparer), expanding the 32-bit `GetHashCode` through a deterministic SplitMix64-style avalanche into the 64-bit values the sketch consumes. The expansion cannot manufacture entropy the comparer did not supply, so the achievable accuracy floor is bounded by the collision rate of that 32-bit hash — two elements with equal comparer hashes are indistinguishable to a sketch.
- **Randomized string hashes make exports process-local.** The expansion is platform-stable, but `string.GetHashCode()` is randomized per process — exported state for `string` elements (or any type with a randomized hash) under the default comparer is only meaningful when re-imported within the same process. Supply a custom comparer with a stable hash when exports must cross process boundaries.
- **Merges require compatible instances.** `UnionWith` / `MergeWith` demand identical geometry (bit count and hash count; width and depth; precision) and the same or an equal comparer — in practice, instances constructed with the same parameters.
- **Export is an opaque, version-checked snapshot, not a wire contract.** `TryExport` / `Export` / `GetExportByteCount` and the static `Import` round-trip a sketch's state as bytes, but the layout may change between library versions with a corresponding version-byte bump. The comparer is not part of the exported state — `Import` requires the caller to re-supply it.
- **None of the sketches is thread-safe.** Concurrent reads and writes require external synchronization.
