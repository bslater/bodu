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
--- BloomFilter<T> - membership with no false negatives ---
  What   : Sizes a filter for 8 items at a deliberately loose 10% false-positive rate, adds eight words, confirms
           all eight test positive, then scans 1000 non-members to surface a real collision and measure the observed
           rate.
  Why    : A Bloom filter stores no elements - only the bits their hashes set - so it trades exactness for size, and
           the trade is deliberately one-sided. A member can never test negative, because adding it set those exact
           bits and nothing ever clears them. A non-member can test positive, because other elements may between
           them have set all of its bits. That is why it belongs in front of an expensive store: a negative is final
           and saves the lookup, a positive only means "go and check".
  Expect : All 8 members test positive - that is the guarantee, and a single miss would disprove it. Among 1000
           non-members about 72 report present, close to the 10% the filter was sized for. The parameters and
           comparer are fixed, so the same candidate collides on every run.

  bit count / hashes   : 39 / 3  (expected 39 / 3 - 39 bits total for 8 items, which is why this is compact; 3 probes per lookup)
  members present      : 8/8  (expected 8/8 - the no-false-negative guarantee; anything less would be a defect, not bad luck)
  first false positive : 'candidate-0017'  (expected candidate-0017 - never added, yet reported present: the bits it probes were all set by other members)
  empirical FP rate    : 72/1000  (expected 72/1000, about 7% - near the 10% this filter was sized for; a tighter rate costs more bits)
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
--- CountMinSketch<T> - frequency estimates that never underestimate ---
  What   : Feeds a fixed 15-visit page stream through the sketch while building the exact histogram alongside, then
           prints estimate against exact for every distinct page.
  Why    : Counting a high-cardinality stream exactly costs memory proportional to the number of distinct keys,
           which is exactly what you do not have for per-URL or per-IP counters at scale. The sketch fixes the
           memory up front and absorbs the error into collisions - but only ever upward, because an estimate is the
           minimum across independent rows and a shared counter can only be inflated by the other element, never
           reduced. That is what makes it safe for threshold questions like heavy hitters or rate limits: you may
           act on a key that was not quite over the line, but you can never miss one that was.
  Expect : 272 x 5 counters and 15 total additions. Every estimate equals its exact count here - the stream is far
           too small to collide in 272 counters - and every row reports ok, meaning estimate >= exact. A VIOLATION
           would mean the guarantee itself had broken.

  width x depth : 272 x 5 counters  (expected 272 x 5 - epsilon set the width, delta the number of independent rows; both are fixed regardless of how many keys arrive)
  total added   : 15  (expected 15 - the stream length; this total is exact, only per-key estimates are approximate)
  /cart      exact=3 estimate=3  (ok - the guarantee is estimate >= exact; equality here means no collision touched this key)
  /checkout  exact=1 estimate=1  (ok - the guarantee is estimate >= exact; equality here means no collision touched this key)
  /home      exact=7 estimate=7  (ok - the guarantee is estimate >= exact; equality here means no collision touched this key)
  /search    exact=4 estimate=4  (ok - the guarantee is estimate >= exact; equality here means no collision touched this key)
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
--- HyperLogLog<T> - approximate distinct-count ---
  What   : Adds 10,000 distinct tokens, each one twice, to a precision-14 estimator and compares the estimate
           against the true cardinality.
  Why    : Counting distinct values exactly costs memory proportional to the number of distinct values, so
           per-dimension unique-visitor counts are precisely the thing you cannot afford to keep exactly.
           HyperLogLog fixes the cost in advance - 16,384 registers, about 16 KB, whether it summarizes ten items or
           ten billion - and fixes the accuracy with it, since the standard error depends only on the register
           count. Inserting each token twice is not padding: it demonstrates that the estimator is driven by the
           hash of each value, so repeats land on the same register and change nothing.
  Expect : 16,384 registers, a 0.81% standard error, and an estimate of about 10,098 against a true 10,000 - a 0.98%
           relative error, just inside one standard deviation. Because the comparer is fixed, that estimate is
           identical on every run rather than merely close.

  registers        : 16384  (expected 16384 = 2^14; this array is the entire memory cost, and it does not grow with the data)
  standard error   : 0.81 %  (expected 0.81% = 1.04/sqrt(16384) - a property of the register count alone, known before any data arrives)
  true distinct    : 10000  (each token was added twice, so 20,000 additions of 10,000 distinct values)
  estimated        : 10098.1  (expected about 10098 - the duplicates changed nothing, since a repeated value hashes to the same register)
  relative error   : 0.98 %  (expected 0.98% - inside one standard error, which is the accuracy claim holding rather than luck)
```

**APIs demonstrated.** `HyperLogLog<T>(int precision, IEqualityComparer<T>)`, `.Add`, `.EstimateCardinality`,
`.RegisterCount`, `.Precision`, `.StandardError`.

## Layout

```text
Bodu.Collections.Samples.ProbabilisticSketches/
  Program.cs                      # runs the scenarios in order
  SampleConsole.cs                # the what/why/expect banner every scenario opens with
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
