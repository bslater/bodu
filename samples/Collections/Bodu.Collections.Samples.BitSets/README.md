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
  set bits      : 2, 3, 5, 7, 16, 17, 18, 19, 100
  cardinality   : 9 (number of set bits)
  length        : 101 (highest set index + 1)
  capacity      : 128 (allocated bits, whole words)
  ToString()    : BitSet(Cardinality = 9, Length = 101)
  NextSetBit    : 2, 3, 5, 7, 16, 17, 18, 19, 100 then -1
  NextClearBit(2): 4 (first gap at or after 2)
  NextClearBit(16): 20 (scans past the 16..19 run)
  after Clear(100) -> length 20, capacity 128 (capacity is retained)
  after Flip(2, 6): 4, 7, 16, 17, 18, 19
  after Clear()  : empty=True, length=0, capacity=128
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
  left           : {1, 2, 3, 4, 5}
  right          : {4, 5, 6, 7}
  Intersects     : True
  left AND right : {4, 5} (intersection)
  left OR right  : {1, 2, 3, 4, 5, 6, 7} (union)
  left XOR right : {1, 2, 3, 6, 7} (symmetric difference)
  left ANDNOT rt : {1, 2, 3} (relative complement)
  roomy          : {4, 5}
  and == roomy   : True (capacity 64 vs 512 is irrelevant)
  hashes agree   : True
```

**APIs demonstrated.** `BitSet(BitSet source)`, `.And`, `.Or`, `.Xor`, `.AndNot`, `.Intersects`,
`.Equals(BitSet)`, `.GetHashCode()`, `operator ==`.

## Layout

```text
Bodu.Collections.Samples.BitSets/
  Program.cs                        # runs the scenarios in order
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
