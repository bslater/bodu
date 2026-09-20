---
title: Bodu.Collections.Merkle — Core concepts
---

# Bodu.Collections.Merkle — Core concepts

This page is the vocabulary the rest of the package documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [RFC 6962 guide](../../guides/merkle/rfc6962-merkle-trees.md).

Part of the **[Hashing & Cryptography](../topics/hashing-and-cryptography.md)** topic. The general hashing vocabulary — digests, block sizes, streaming, incremental state — lives on the [topic concepts page](../topics/hashing-and-cryptography-concepts.md); this page covers only what a *tree* adds to a flat hash.

## Merkle Tree Hash (MTH)

A Merkle tree turns a list of *n* entries into a single root hash by hashing each entry into a **leaf**, then hashing pairs of hashes into **internal nodes**, until one value remains. The root changes if any entry changes — the same property a flat digest gives — but because the tree is built bottom-up, a logarithmic number of hashes is enough to prove that one entry sits under the root. That is the whole reason to pay for a tree.

RFC 6962 §2.1 defines the function precisely, and the precision matters: several plausible-looking constructions give different roots for the same input. `MTH` over a list `D[n]` is:

```text
MTH({})       = H()                                     the hash of zero bytes
MTH({d(0)})   = H(0x00 || d(0))                          a leaf, not a bare digest
MTH(D[n])     = H(0x01 || MTH(D[0:k]) || MTH(D[k:n]))    for n > 1
```

where `k` is the largest power of two **strictly** below *n*.

## Split point

The `k` above is where most divergences come from. For `n = 3`, `k = 2`: the tree splits **2 + 1**, not 1 + 2 and not a rounded half. For `n = 8`, `k = 4` — the one case where "strictly below" and "at or below" disagree, and where getting it wrong silently produces a tree one level too deep.

A direct consequence is that a subtree with exactly one leaf contributes its **leaf hash unchanged**. It is *not* re-hashed as a one-child node. This is the single point on which `Rfc6962MerkleTree` differs from <xref:Bodu.Security.Cryptography.MerkleTreeHash> and <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash>, which reduce level by level and *do* re-hash a lone leftover child — so the three agree only when the leaf count is a power of two. Neither construction is wrong; only one of them interoperates.

## Domain separation

Leaf and node hashes are computed over prefixed inputs:

| Prefix | Hashed as | Purpose |
|---|---|---|
| `0x00` | `H(0x00 ‖ entry)` | a leaf |
| `0x01` | `H(0x01 ‖ left ‖ right)` | an internal node |
| `0x02` | `H(0x02 ‖ uint64_be(value) ‖ root)` | a length-bound root (a Bodu addition, below) |

Without the prefixes, a leaf hash and a node hash would be drawn from the same space, and an attacker could present an internal node's two child hashes as a single leaf's contents — the **second-preimage attack**. The prefix makes the two domains disjoint, so no leaf preimage can ever collide with a node preimage.

The related classic failure is **CVE-2012-2459**, Bitcoin's duplicate-last-leaf rule: a tree that pads an odd level by hashing the last node with *itself* lets two different transaction lists produce the same root. RFC 6962's promote-unchanged rule has no padding step, so the construction is immune by shape — and the repository's test suite pins the duplicated-leaf forgery as an explicit negative.

## Entry mode and block mode

The same tree serves two different framings:

- **Entry mode** treats the input as a list of discrete records — log entries, certificates, file names. `ComputeRoot`, `AuthenticationPath`, and `ConsistencyProof` take `IReadOnlyList<ReadOnlyMemory<byte>>`.
- **Block mode** treats the input as one byte sequence cut into fixed-size blocks, each block becoming a leaf. `ComputeBlocked` / `ComputeRootOfBlocks` take a stream or a span plus a `blockSize`.

Block mode has two conventions worth stating plainly, because both differ from what padding-based schemes do:

- A **zero-length input has no blocks at all**, not one empty block. Its root is therefore `H()`, matching `MTH({})`.
- A **final short block is hashed at its actual length**, never zero-padded. Padding would let a shorter input collide with a zero-extended longer one.

<xref:Bodu.Collections.Merkle.MerkleBlocks> exposes the arithmetic (`BlockCount`, `BlockOffset`, `BlockLength`) in 64-bit form, so a caller can address blocks of a multi-gigabyte object without overflow.

## Authentication path (inclusion / audit proof)

An **authentication path** for leaf *i* of a tree of size *n* is the ordered list of sibling hashes met on the walk from that leaf to the root. A verifier starts from the leaf hash, combines it with each step in turn — on the correct side, which the index and size determine — and accepts only if the final value equals the trusted root. The path has at most `⌈log₂ n⌉` steps, so the proof is logarithmic in the tree size no matter how large the input was.

The path lengths are not uniform. In a seven-leaf tree, leaves 0–5 carry three steps but leaf 6 carries two, because the right subtree of three leaves is shallower on that side. An implementation that assumes a fixed depth gets this wrong.

## Consistency proof

Where an inclusion proof answers *"is this entry in this tree?"*, a **consistency proof** answers *"is this earlier tree a prefix of this later one?"* — the property an append-only log must have. `VerifyConsistency` takes both sizes and both roots and must reconstruct both from the proof, which is why the size ambiguity below has no analogue here.

Three cases are decided without walking the proof at all:

- a later size **below** the earlier one is rejected outright;
- **equal** sizes require identical roots *and* an empty proof;
- a first size of **zero** requires an empty proof, because every tree extends the empty tree.

RFC 6962's inclusion walk and consistency walk terminate on different conditions (`sn = 0` versus `fn = 0`). That asymmetry is the standard's own, and both are implemented verbatim rather than harmonised.

## Tree-size ambiguity and bound roots

`VerifyInclusion`'s `treeSize` parameter is **trusted input**, and this is RFC 6962 behaving as specified rather than a defect in the implementation. A four-entry tree's path for entry 0 has exactly the length a three-entry tree's first path wants, and walks to the same head — so a verifier told either size accepts. If the size comes from the party being examined, they can understate it, exempting their last entries from ever being challenged, and every check still passes.

Bodu closes the gap with a **length-bound root**: `BindRoot` folds a value — normally the input's byte length — into the published root as `H(0x02 ‖ uint64_be(value) ‖ root)`. The fail-closed verifiers then *derive* the tree size rather than accept one:

| Verifier | Size comes from | Use when |
|---|---|---|
| `VerifyInclusion` / `VerifyInclusionOfLeafHash` | the caller | the size is already trusted (you published it yourself) |
| `VerifyInclusionBound` | the bound root | entry mode, size supplied by an untrusted party |
| `VerifyBlockInclusion` | the bound length ÷ block size | block mode, size supplied by an untrusted party — also checks the block's length matches its position |

The `0x02` prefix keeps a bound root out of both other domains, so it can never be mistaken for a leaf or a node.

## Total verification

Every verifier is **total**: it returns `false` for any malformed input — a leaf index at or past the tree size, a zero tree size, a path longer or shorter than the position demands, a step of the wrong width, a root of the wrong width — and only a `null` path or proof array throws. A verifier sits directly behind bytes an adversary chose, so an exception where a `false` belongs is a denial of service. The test suite drives this as a systematic mutation matrix (shifted and flipped indices and sizes, wrong and swapped roots, each root injected as a proof step, every step flipped, removed, duplicated, and mis-sized) plus seeded malformed-input sweeps asserting only that verification never throws.

## Streaming and the logarithmic fold

Computing a root over an object too large to hold requires folding the tree as the bytes go past. `ComputeRootOfBlocks` does this by pushing each leaf onto a stack as a one-leaf subtree, merging the top two whenever they are the same size, and folding what remains right-to-left at the end. The stack therefore holds one subtree per set bit of the leaf count — `popcount(n)` hashes, never more — so peak memory is `O(blockSize + log n · HashLength)` regardless of input size.

`ComputeBlocked` retains every leaf hash instead, returning them on the <xref:Bodu.Collections.Merkle.MerkleComputation>. That costs `leafCount × HashLength` bytes — 16 KiB for a 512 MiB object at one-mebibyte blocks — and buys the ability to produce an authentication path afterwards without a second pass.

## Parallel leaf hashing

The parallel entry points hash **leaves** concurrently and then fold the resulting leaf hashes through the same sequential reduction. The tree shape is untouched, so the root is bit-identical to the sequential one for every input and the choice is purely about throughput.

How much throughput there is to gain depends on how the leaf hash compares with memory bandwidth, and the answer can be *no gain at all*: a hardware-accelerated SHA-256 already runs near bandwidth, leaving little for extra threads to recover. The `ReadOnlyMemory<byte>` overload scales better than the stream one, because a stream must be read sequentially — the stream overload copies each block on the calling thread, while the in-memory overload copies *and* hashes inside each worker. The [guide](../../guides/merkle/rfc6962-merkle-trees.md#hashing-leaves-in-parallel) carries measured figures.

## Where to go next

- **[Getting started](getting-started.md)** — install and run a sample per mode.
- **[RFC 6962 Merkle trees and proofs](../../guides/merkle/rfc6962-merkle-trees.md)** — the full walk-through.
- **[Introduction](index.md)** — the package shape and scenario table.
- **[Bodu.Security.Cryptography concepts](../cryptography/concepts.md)** — the level-by-level Merkle digests and the rest of the cryptography vocabulary.
