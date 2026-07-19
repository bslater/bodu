# Bodu.Collections.Samples.ProbabilisticSketches

The approximate sketches in `Bodu.Collections.Probabilistic`: a Bloom filter for set membership, a count-min
sketch for frequency estimation, and a HyperLogLog for distinct-count cardinality. Each trades a little
accuracy for a large, fixed memory footprint, and each exposes a precise one-sided guarantee. Three scenarios.

Everything runs offline. The sketches derive all of their bit positions from
`IEqualityComparer<T>.GetHashCode`, and `string.GetHashCode()` is randomized per process — so each scenario
supplies a `StableStringComparer` (FNV-1a) to pin the hashing. That makes the output, including the specific
Bloom false positive, identical on every run.

```bash
dotnet run --project samples/Collections/Bodu.Collections.Samples.ProbabilisticSketches
```

## Scenario 1 — BloomMembership

**Intent.** Show `BloomFilter<T>` and its two-sided contract: a member *always* tests positive (no false
negatives), while a non-member tests positive only with a bounded probability (false positives). Both halves
matter — the whole point is a compact filter you can trust for "definitely not present."

**What it does.** Sizes a filter for 8 items at a deliberately loose 10% false-positive rate (so a collision
is easy to surface), adds eight known words, and confirms all eight test positive. It then scans a fixed list
of 1,000 synthetic non-members (`candidate-0000`…`candidate-0999`), reporting the first that collides and the
empirical false-positive rate over the whole list.

**What to expect.** All eight members are present (the no-false-negative guarantee); the first false positive
is a stable `candidate-0017`, and the empirical rate (7.2%) tracks the 10% design rate given the small 39-bit
filter:

```text
  bit count / hashes   : 39 / 3
  members present      : 8/8 (no false negatives)
  first false positive : 'candidate-0017' (a non-member reported present)
  empirical FP rate    : 72/1000 non-members over the 3 probes
```

**APIs demonstrated.** `BloomFilter<T>(int expectedItems, double falsePositiveRate, IEqualityComparer<T>)`,
`.Add`, `.MightContain`, `.BitCount`, `.HashCount`.

## Scenario 2 — FrequencySketch

**Intent.** Show `CountMinSketch<T>` and its one-sided guarantee: the estimated count of an element is
*never less* than its true count. Collisions can only ever inflate an estimate, never deflate it, which is
exactly what makes the sketch safe for "at least this many" decisions.

**What it does.** Configures a sketch by error bounds (`epsilon`, `delta`), counts a fixed 15-element stream
of page visits while building an exact histogram alongside, then compares the estimate against the exact count
for every distinct page (sorted for stable output).

**What to expect.** With a generously sized table (272×5 counters) and no collisions among five keys, every
estimate equals the exact count — and each row confirms `estimate >= exact`:

```text
  width x depth : 272 x 5 counters
  total added   : 15
  /cart      exact=3 estimate=3 (>= exact: ok)
  /checkout  exact=1 estimate=1 (>= exact: ok)
  /home      exact=7 estimate=7 (>= exact: ok)
  /search    exact=4 estimate=4 (>= exact: ok)
```

**APIs demonstrated.** `CountMinSketch<T>(double epsilon, double delta, IEqualityComparer<T>)`, `.Add`,
`.EstimateCount`, `.TotalCount`, `.Width`, `.Depth`.

## Scenario 3 — CardinalityEstimate

**Intent.** Show `HyperLogLog<T>`: a distinct-count estimator that summarizes an arbitrarily large set in a
few kilobytes of registers, with a standard error of about 1.04/√m for m registers. It counts *distinct*
elements, so repeats cost nothing.

**What it does.** Builds an HLL at precision 14 (16,384 registers), adds 10,000 distinct tokens *each inserted
twice* to prove duplicates are ignored, then compares the estimate to the true cardinality and reports the
relative error against the sketch's own standard error.

**What to expect.** The estimate lands within about 1% of the true 10,000 — comfortably inside the ~0.81%
standard error band — despite every token being added twice:

```text
  registers        : 16384 (precision 14)
  standard error   : 0.81 %
  true distinct    : 10000
  estimated        : 10098.1
  relative error   : 0.98 %
```

**APIs demonstrated.** `HyperLogLog<T>(int precision, IEqualityComparer<T>)`, `.Add`, `.EstimateCardinality`,
`.RegisterCount`, `.Precision`, `.StandardError`.

## Layout

```text
Bodu.Collections.Samples.ProbabilisticSketches/
  Program.cs                      # runs the scenarios in order
  StableStringComparer.cs         # process-stable string hashing for reproducible sketches
  Scenarios/BloomMembership.cs
  Scenarios/FrequencySketch.cs
  Scenarios/CardinalityEstimate.cs
```

## Related

- `Bodu.Collections.Samples.CollectionCatalogue` — the ring, deque, evicting cache, multi-maps and sets, the
  bidirectional and navigable dictionaries, and the indexed priority queue.
- `Bodu.Collections.Samples.RangesGraphsTrees` — coalescing range sets, the interval tree, graph algorithms,
  disjoint-set union-find, the tree/trie family, and Aho-Corasick multi-pattern search.
