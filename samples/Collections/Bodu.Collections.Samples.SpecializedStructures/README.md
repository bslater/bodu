# Bodu.Collections.Samples.SpecializedStructures

The two members of `Bodu.Collections.Specialized` — the packed `BitSet` and the RFC 6962 `Rfc6962MerkleTree`.
Neither is a general-purpose container, which is why they sit in their own namespace and get their own sample
rather than a slot in the collection catalogue. Six scenarios: two on the bit set, four on the Merkle tree.

Everything runs offline and deterministically. The Merkle scenarios hash fixed inputs with SHA-256, so every
root printed below is reproducible — the length-bound scenario in particular reproduces the published RFC 6962
appendix D vectors byte for byte.

```bash
dotnet run --project samples/Collections/Bodu.Collections.Samples.SpecializedStructures
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

## Scenario 3 — MerkleCommitments

**Intent.** Show the core RFC 6962 commitment: one root hash stands for a whole list, and a logarithmic audit
path proves one entry's membership against it without revealing the rest. Also show *why* the RFC prefixes its
hashes — the domain separation that closes the second-preimage attack — and that verification is total, so a
verifier can be fed attacker-supplied values without a `try`/`catch`.

**What it does.** Commits to a seven-entry audit log, prints the root, then contrasts `HashLeaf(e)` with a plain
`SHA256(e)` and shows `HashNode`. It checks the empty-tree and one-entry special cases, issues the audit path
for entry 3, and verifies it. It then tampers with the entry, claims the wrong index, and feeds three malformed
inputs.

**What to expect.** `HashLeaf(e[0])` differs from `SHA256(e[0])` because the leaf hash is `H(0x00 ‖ entry)`; a
one-entry tree's root *is* that leaf hash, with no node hashing at all. The seven-entry path is three steps
(⌈log₂ 7⌉). The verification then fails for every corruption — including the malformed inputs, which return
`false` rather than throwing:

```text
  hash length   : 32 bytes (SHA-256)
  entries       : 7
  root          : e8da82b23930fab23f8dbbc04e30897d451db2ac3b2e644e99bf9e683afd51b3
  HashLeaf(e[0]): 720ea60b...
  SHA256(e[0])  : 4f92ec18... (differs - the 0x00 leaf prefix)
  HashNode pair : 1ba430ca... (the 0x01 node prefix)
  empty root    : e3b0c442...
  1-entry root  : 720ea60b... == HashLeaf(e[0]): True
  path for e[3] : 3 steps (56439159..., b8cd573e..., 657f1dad...)
  verify e[3]   : True
  tampered entry: False
  wrong index   : False
  short root    : False
  path too long : False
  index >= size : False
```

The empty root `e3b0c442...` is the SHA-256 of the empty string, exactly as the RFC specifies.

**APIs demonstrated.** `Rfc6962MerkleTree(Func<HashAlgorithm>)`, `.HashLength`, `.HashLeaf`, `.HashNode`,
`.ComputeRoot`, `.AuthenticationPath`, `.VerifyInclusion`.

## Scenario 4 — MerkleSizeBinding

**Intent.** Demonstrate RFC 6962's tree-size ambiguity — and the fact that it is *specified behaviour, not a
defect* — then show the length-bound root that closes it. This is the scenario to read before using
`VerifyInclusion` anywhere the tree size comes from the party being audited: its `treeSize` argument is trusted
input, not something the root authenticates.

**What it does.** Cuts a 16-byte payload into four 4-byte blocks, computes the root and the audit path for block
0, then verifies that path against both the true tree size (4) and an understated one (3). It then binds the
root to the byte length with `BindRoot`, shows that a 12-byte payload yields a different bound root, and runs
four `VerifyInclusionBound` claims: the truth, a whole block understated, one byte short, and one byte over.

**What to expect.** Both unbound verifications return `True`. That is not a bug: a four-leaf tree's path for
leaf 0 has exactly the length a three-leaf tree's first path wants and walks to the same head, so the size check
alone cannot tell them apart. The consequence is concrete — a holder of a four-block object that has lost block
3 could declare a three-block object, never be challenged for block 3, and pass every audit for ever. The bound
verifier rejects all three misstatements:

```text
  blocks        : 4 x 4 bytes
  root          : 516c43cb9e4f82fe70437703f0a41649c5fd963ba5f4be8b736dca20bae05bdd
  VerifyInclusion(size=4): True (the true size)
  VerifyInclusion(size=3): True (understated - still accepted)
  ^ this is RFC 6962's verifier working as specified, not a defect.
  BindRoot(.., 16): f5ed505e0cb1f0f1cb6909f10211fefe06a7d976b18f9c38e9bdf497802f2b04
  BindRoot(.., 12): 29f46d734672b404873bd2548b2ab1cf66cf22c5c412fb150331b344cd50332a
  bound roots differ: True
  bound(len=16, size=4): True (the truth)
  bound(len=12, size=3): False (a whole block understated)
  bound(len=15, size=4): False (off by one byte)
  bound(len=17, size=4): False (overstated)
```

All three hex values above are the published appendix D vectors, so this scenario doubles as a conformance
check you can eyeball.

**APIs demonstrated.** `.ComputeRoot`, `.AuthenticationPath`, `.VerifyInclusion`, `.BindRoot`,
`.VerifyInclusionBound`, `MerkleBlocks.BlockCount` / `.BlockOffset` / `.BlockLength`.

## Scenario 5 — MerkleConsistency

**Intent.** Show the append-only guarantee. An inclusion proof answers "is this entry in the log?"; a
consistency proof answers the harder question "is this log the same log I saw last time, only longer?" — which
is what stops an operator quietly rewriting history.

**What it does.** Takes two snapshots of the same log (at 4 entries and at 7), computes the consistency proof
over the current list plus the earlier size, and verifies it from the two roots alone. It checks the degenerate
`n → n` case, then forges a log by rewriting entry 1 and appending as normal, and finally tries a reversed size
pair and a mis-sized proof step.

**What to expect.** The genuine extension verifies from the two signed roots, with the auditor never seeing an
entry. The forged log's root is a perfectly valid Merkle root — it just is not an extension of what was
published, so the proof fails:

```text
  root @ 4      : b41a66fe...
  root @ 7      : e8da82b2...
  proof 4 -> 7  : 1 step (657f1dad...)
  verify 4 -> 7 : True
  verify 7 -> 7 : True
  forged root @7: 796a98a6...
  forged proof  : False (history was rewritten)
  verify 7 -> 4 : False (sizes out of order)
  mis-sized step: False
```

A single proof step suffices here because 4 is a power of two: the old tree is already a complete left subtree
of the new one, so only the right subtree's hash is needed.

**APIs demonstrated.** `.ConsistencyProof`, `.VerifyConsistency`, `.ComputeRoot`.

## Scenario 6 — MerkleBlockedInputs

**Intent.** Show the blocked surface, which treats one large input as a sequence of fixed-size leaves rather
than a list of entries — the shape you want for files and blobs. Show that the streaming, in-memory, and
parallel paths are three routes to *one* answer, and finish with `VerifyBlockInclusion`, the fail-closed
possession check that derives the tree size instead of trusting a claimed one.

**What it does.** Builds a 3,372-byte payload (three whole 1 KiB blocks plus a 300-byte tail) and prints the
`MerkleBlocks` arithmetic for each block. It computes the commitment four ways — from a span, from a `Stream`,
in parallel with a fixed degree of 2, and from the leaf hashes alone — and compares all four roots. It then
publishes the bound root and answers a challenge for block 2, before retrying with a flipped byte and with an
understated length.

**What to expect.** The final block is 300 bytes — short, not padded, which is what makes the block count
exact. All four roots agree: parallelism is an optimisation, never a different answer, and the leaf hashes
carried on `MerkleComputation` mean paths can be issued later without re-reading the input. The challenge is
answered, and both cheats fail — the path alone is not the answer, because the block is re-hashed during
verification:

```text
  payload       : 3372 bytes, block size 1024
  BlockCount    : 4 (the final block is short, not padded)
    block 0     : offset     0, length 1024
    block 1     : offset  1024, length 1024
    block 2     : offset  2048, length 1024
    block 3     : offset  3072, length  300
  root          : 9e8b5c0835d2acc889160b1225c5ad8aefc87511bf2a78b48f9d2cc2bc17e1c9
  computation   : InputLength=3372, BlockSize=1024, LeafHashes=4
  streamed root : 9e8b5c08... == in-memory: True
  parallel root : 9e8b5c08... == serial: True
  from leaves   : 9e8b5c08... == in-memory: True
  bound root    : f9e631f9...
  challenge     : block 2 (1024 bytes at offset 2048), path of 2 steps
  answer        : True
  wrong block   : False (one flipped byte)
  wrong length  : False
```

The `Stream` overload is a single pass with a logarithmic-memory fold: it never holds the input, only the
O(log n) spine of pending subtree hashes, so it scales to inputs far larger than memory.

**APIs demonstrated.** `.ComputeBlocked(ReadOnlySpan<byte>, int)`, `.ComputeBlocked(Stream, int, CancellationToken)`,
`.ComputeBlockedParallel`, `.ComputeRootOfLeafHashes`, `.BindRoot`, `.VerifyBlockInclusion`,
`MerkleComputation.Root` / `.InputLength` / `.BlockSize` / `.LeafHashes`, `MerkleBlocks.BlockCount` /
`.BlockOffset` / `.BlockLength`.

## Layout

```text
Bodu.Collections.Samples.SpecializedStructures/
  Program.cs                        # runs the scenarios in order
  Hex.cs                            # hash formatting for readable console output
  Scenarios/BitSetAddressing.cs
  Scenarios/BitSetAlgebra.cs
  Scenarios/MerkleCommitments.cs
  Scenarios/MerkleSizeBinding.cs
  Scenarios/MerkleConsistency.cs
  Scenarios/MerkleBlockedInputs.cs
```

## Related

- `Bodu.Collections.Samples.CollectionCatalogue` — the ring, deque, evicting cache with both its capacity and
  time dimensions, multi-maps and sets, the bidirectional and navigable dictionaries, the indexed priority
  queue, the layered and defaulting dictionaries, and the two-key table with the segmented buffer.
- `Bodu.Collections.Samples.RangesGraphsTrees` — coalescing range sets, the interval tree, graph algorithms,
  disjoint-set union-find, the tree/trie family, and Aho-Corasick multi-pattern search.
- `Bodu.Collections.Samples.ProbabilisticSketches` — the Bloom filter, count-min sketch, and HyperLogLog.
- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` — that library's `MerkleTreeHash` /
  `ParallelMerkleTreeHash` share this domain separation but **not** the RFC 6962 tree shape; reach for
  `Rfc6962MerkleTree` when you need interoperable RFC 6962 proofs without taking the cipher catalogue.
