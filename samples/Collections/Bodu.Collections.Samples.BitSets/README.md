# Bodu.Collections.Samples.BitSets

`BitSet`, the packed, growable set of non-negative integers that is the sole member of
`Bodu.Collections.Specialized` — the namespace for types that serve a specialised purpose rather than acting as
general-purpose containers. Two scenarios: bit addressing and scanning, then set algebra.

Everything runs offline and deterministically: fixed bit indices, and enumeration is inherently ordered, so no
sorting is needed to stabilise the output.

```bash
dotnet run --project samples/Collections/Bodu.Collections.Samples.BitSets
```

For NuGet consumers:

```bash
dotnet add package Bodu.Collections
```

## Scenario 1 — BitSetAddressing

**Intent.** Show `BitSet` as a packed, growable set of non-negative integers, and settle the question that trips
up every first-time user: it has *three* size properties that routinely disagree, and confusing them is the
source of most bugs. Also show the scan primitives, which are the reason to reach for a bit set over a
`HashSet<int>` in the first place.

**What it does.** Sets individual bits (via `Set`, the indexer, and `Set(index, bool)`), sets a half-open range
`[16, 20)`, then sets bit 100 — past the 64-bit initial capacity — to show growth is transparent. It prints the
three sizes, walks the set with the canonical `NextSetBit` idiom, probes `NextClearBit` twice, then clears the
highest bit, flips a range, and empties the set.

**What to expect.** `Cardinality` is 9 (bits set), `Length` is 101 (highest index + 1), and `Capacity` is 128
(allocated bits, rounded to whole 64-bit words) — three different numbers for the same set. Clearing bit 100
drops `Length` to 20 while `Capacity` stays at 128, because capacity never shrinks implicitly. `NextClearBit`
returns 20 from index 16, scanning past the `16..19` run; unlike `NextSetBit` it never returns −1, because every
bit at or beyond `Capacity` is conceptually clear. `Flip(2, 6)` turns 2, 3 and 5 off and 4 on:

```text
--- BitSet - bit addressing, sizes, and scanning ---
  What   : Sets individual bits, a half-open range and one index far beyond the initial capacity, then reads back
           the three size properties, walks the set and clear bits, and clears the highest bit.
  Why    : Cardinality, Length and Capacity sound interchangeable and are not: they are the population count, the
           logical extent, and the allocation. Reaching for the wrong one is a real bug - sizing a loop by Capacity
           walks bits that were never set, and treating Length as a count silently over-reports the moment the set
           has a gap. NextSetBit matters for the same practical reason: it skips whole empty words, so iterating a
           sparse set costs words rather than indices.
  Expect : The three sizes read 9, 101 and 128 for the same nine bits - a count, a highest-index-plus-one, and a
           word-rounded allocation. Clearing bit 100 drops Length to 20 and leaves Capacity at 128, because capacity
           never shrinks on its own.

  set bits      : 2, 3, 5, 7, 16, 17, 18, 19, 100  (expected 2, 3, 5, 7, 16..19, 100 - enumeration yields set indices in ascending order)
  cardinality   : 9  (expected 9 - how many bits are set; the only one of the three that is a count)
  length        : 101  (expected 101 - highest set index + 1, so the gaps below 100 still count toward it)
  capacity      : 128  (expected 128 - storage rounded to whole 64-bit words, not content)
  ToString()    : BitSet(Cardinality = 9, Length = 101)  (the debug view reports content, not allocation)
  NextSetBit    : 2, 3, 5, 7, 16, 17, 18, 19, 100 then -1  (expected the same nine indices - -1 is the terminator, which is why the loop condition is >= 0)
  NextClearBit(2) : 4  (expected 4 - bits 2 and 3 are set, so the first gap at or after 2 is 4)
  NextClearBit(16): 20  (expected 20 - scans past the 16..19 run; unlike NextSetBit this never returns -1, since every bit beyond Capacity is clear)
  after Clear(100): length 20, capacity 128  (expected 20 and 128 - Length tracks content and falls to the next-highest bit; Capacity is an allocation and never shrinks implicitly)
  after Flip(2,6) : 4, 7, 16, 17, 18, 19  (expected 4, 7, 16..19 - 2, 3 and 5 were on and go off, 4 was off and comes on; 6 is excluded by the half-open range)
  after Clear()   : empty=True, length=0, capacity=128  (expected True, 0, 128 - emptying is a content operation; the buffer stays for reuse)
```

**APIs demonstrated.** `BitSet(int initialCapacityBits)`, `.Set(int)`, `.Set(int, bool)`, `.Set(int, int)`,
`this[int]`, `.Clear()`, `.Clear(int)`, `.Flip(int, int)`, `.Cardinality`, `.Length`, `.Capacity`, `.IsEmpty`,
`.NextSetBit`, `.NextClearBit`, `.ToString()`, and `IEnumerable<int>` enumeration.

## Scenario 2 — BitSetAlgebra

**Intent.** Show the bulk set operators, which are where a packed bit set earns its keep — they process 64
elements per word-wise instruction rather than per element. Also show that equality is defined over *logical
content*, so capacity never leaks into comparisons.

**What it does.** Builds `{1,2,3,4,5}` and `{4,5,6,7}`, tests `Intersects`, then applies `And`, `Or`, `Xor`, and
`AndNot` — each to a fresh copy taken through the copy constructor, because all four mutate the receiver in
place. It then compares the intersection against a set holding the same two bits in 512 bits of storage.

**What to expect.** The four operators give intersection, union, symmetric difference, and relative complement.
The last line is the load-bearing one: a 64-bit-capacity set equals a 512-bit-capacity set holding the same
bits, and their hash codes agree — so `BitSet` is safe as a dictionary key:

```text
--- BitSet - set algebra and equality ---
  What   : Runs Intersects, And, Or, Xor and AndNot over {1,2,3,4,5} and {4,5,6,7}, each against a fresh copy, then
           compares a 64-bit-capacity result with a 512-bit-capacity set holding the same bits.
  Why    : These operators are why a bit set exists: membership algebra over dense integer sets costs one
           instruction per 64 elements instead of one hash lookup per element. Two details matter in practice.
           Intersects answers "do these overlap?" without building the intersection first, which is the common case
           in a filter. And AndNot is not sugar for And(Not(right)) - a growable bit set has no finite universe, so
           there is nothing to complement against and the relative complement has to be a primitive.
  Expect : The four results are the arithmetic ones: {4,5}, {1,2,3,4,5,6,7}, {1,2,3,6,7} and {1,2,3}. The equality
           pair is the interesting one - the same two bits held in an eight-times-larger allocation compare equal
           and hash equal, because both are defined over content alone.

  left           : {1, 2, 3, 4, 5}  (the receiver each operator below is applied to)
  right          : {4, 5, 6, 7}  (the operand; never modified by any of these calls)
  Intersects     : True  (expected True - 4 and 5 are shared; answered without building the intersection)
  left AND right : {4, 5}  (expected {4, 5} - intersection, the bits in both)
  left OR right  : {1, 2, 3, 4, 5, 6, 7}  (expected {1..7} - union, the bits in either)
  left XOR right : {1, 2, 3, 6, 7}  (expected {1, 2, 3, 6, 7} - symmetric difference, so the shared 4 and 5 drop out)
  left ANDNOT rt : {1, 2, 3}  (expected {1, 2, 3} - left with right's bits removed)
  roomy          : {4, 5}  (the same two bits, but allocated with eight times the capacity)
  and == roomy   : True  (expected True - equality is over content; capacity 64 vs 512 does not enter into it)
  hashes agree   : True  (expected True - equal values must hash equally, so BitSet is safe as a dictionary key)
```

**APIs demonstrated.** `BitSet(BitSet source)`, `.And`, `.Or`, `.Xor`, `.AndNot`, `.Intersects`,
`.Equals(BitSet)`, `.GetHashCode()`, `operator ==`.

## Layout

```text
Bodu.Collections.Samples.BitSets/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the what/why/expect banner every scenario opens with
  Scenarios/BitSetAddressing.cs
  Scenarios/BitSetAlgebra.cs
```

## Related

- `Bodu.Collections.Samples.CollectionCatalogue` — the ring, deque, evicting cache with both its capacity and
  time dimensions, multi-maps and sets, the bidirectional and navigable dictionaries, the indexed priority
  queue, the layered and defaulting dictionaries, and the two-key table with the segmented buffer.
- `Bodu.Collections.Samples.RangesGraphsTrees` — coalescing range sets, the interval tree, graph algorithms,
  disjoint-set union-find, the tree/trie family, and Aho-Corasick multi-pattern search.
- `Bodu.Collections.Samples.ProbabilisticSketches` — the Bloom filter, count-min sketch, and HyperLogLog.
- The RFC 6962 Merkle tree formerly lived in `Bodu.Collections.Specialized` and now ships as `MerkleTree` in
  `Bodu.Security.Cryptography`, so a commitment consumer takes that package rather than this one.
