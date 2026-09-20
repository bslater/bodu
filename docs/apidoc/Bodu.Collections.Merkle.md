---
uid: Bodu.Collections.Merkle
---

![Bodu.Collections.Merkle](~/images/hero-merkle.svg)

## Purpose

**Bodu.Collections.Merkle** is the [RFC 6962](https://www.rfc-editor.org/rfc/rfc6962#section-2.1) Merkle tree: the Merkle Tree Hash, inclusion (audit) proofs, consistency proofs, and length-bound roots, computed over any <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>. It is the package to reach for when a root must interoperate with a transparency log, an artifact attestation, a content-addressed store, or anything else built to the standard — and the only Bodu type that produces proofs.

The package depends on `Bodu.Core` alone. That is the reason it exists as its own package rather than as a corner of <xref:Bodu.Security.Cryptography>: a consumer that needs commitments should not have to take a cipher catalogue with it. The hash itself is supplied by the caller as a factory, so the BCL algorithms, the Bodu digests, and anything else deriving from `HashAlgorithm` all work without this package referencing any of them.

> [!IMPORTANT]
> `Bodu.Security.Cryptography` also ships <xref:Bodu.Security.Cryptography.MerkleTreeHash> and <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash>. Those two borrow RFC 6962's **domain separation** but not its **tree shape** — they reduce level by level with a configurable `fanOut` and re-hash a lone leftover child, so their roots agree with RFC 6962's only when the leaf count is a power of two. They are sound commitments, simply different ones. Where the root has to interoperate, or where you need proofs, the type on this page is the one to use.

## Static documentation

- **[RFC 6962 Merkle trees and proofs](~/guides/merkle/rfc6962-merkle-trees.md)** — the full walk-through: entry mode, block mode, the streaming fold, proofs, the tree-size ambiguity, and parallel leaf hashing.
- **[Introduction](~/docs/collections-merkle/index.md)** — the package shape, headline types, and scenario table.
- **[Core concepts](~/docs/collections-merkle/concepts.md)** — the vocabulary: Merkle Tree Hash, split point, domain separation, audit and consistency proofs, bound roots.
- **[Using Merkle trees](~/guides/cryptography/merkle-trees.md)** — the level-by-level streaming digests in `Bodu.Security.Cryptography`, and how they differ from this tree.

## Key types

- <xref:Bodu.Collections.Merkle.Rfc6962MerkleTree> — the whole surface, as one immutable, stateless facade over a `Func<HashAlgorithm>`. It holds no digest state between calls and is safe to share across threads. **Root computation** comes in three shapes: `ComputeRoot` / `ComputeRootOfLeafHashes` over a list of entries, `ComputeBlocked` / `ComputeRootOfBlocks` over a stream or span cut into fixed-size blocks, and `ComputeBlockedParallel` / `ComputeRootParallel`, which spread leaf hashing across threads without changing the tree. **Proofs** are `AuthenticationPath` and `ConsistencyProof` (plus their `…OfLeafHashes` counterparts) on the prover side, and `VerifyInclusion`, `VerifyInclusionOfLeafHash`, `VerifyInclusionBound`, `VerifyBlockInclusion`, and `VerifyConsistency` on the verifier side. `HashLeaf`, `HashNode`, and `BindRoot` expose the three domain-separated primitives directly, and `HashLength` reports the digest width the supplied factory produces.
- <xref:Bodu.Collections.Merkle.MerkleComputation> — the result of a block-mode pass: the `Root`, the `InputLength` and `BlockSize` it was computed over, and the ordered `LeafHashes`. Returning the leaf hashes from the same pass is the point of the type — generating an authentication path needs them, and without them a large object would have to be streamed twice.
- <xref:Bodu.Collections.Merkle.MerkleBlocks> — the block arithmetic as standalone 64-bit helpers: `BlockCount`, `BlockOffset`, and `BlockLength`. A zero-length input has **no** blocks rather than one empty block, and a final short block is hashed at its actual length rather than padded.

## Example

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Merkle;

var tree = new Rfc6962MerkleTree(SHA256.Create);   // immutable; share it freely

// Entry mode — a root plus an audit proof for one entry.
ReadOnlyMemory<byte>[] entries = [new byte[0], new byte[] { 0x00 }, new byte[] { 0x10 }];

byte[]   root = tree.ComputeRoot(entries);
byte[][] path = tree.AuthenticationPath(entries, leafIndex: 2);

bool included = tree.VerifyInclusion(
    root, treeSize: entries.Length, leafIndex: 2,
    entry: entries[2].Span,
    path: path.Select(step => (ReadOnlyMemory<byte>)step).ToArray());

// Block mode — one pass over a stream yields the root and the leaf hashes a path needs.
MerkleComputation computation = tree.ComputeBlocked(stream, blockSize: 1024 * 1024);
byte[] published = tree.BindRoot(computation.Root, computation.InputLength);

bool blockOk = tree.VerifyBlockInclusion(
    published, computation.InputLength, blockSize: 1024 * 1024,
    blockIndex: 7, block: seventhBlock,
    path: tree.AuthenticationPath(computation.LeafHashes, 7)
              .Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

## Notes

- **RFC 6962's tree, exactly.** A tree of *n* leaves splits at `k`, the largest power of two **strictly below** *n*, and a lone subtree root is promoted **unchanged** rather than re-hashed. Leaves are `H(0x00 ‖ entry)` and internal nodes `H(0x01 ‖ left ‖ right)`, so a leaf hash can never be mistaken for a node hash — the defence against the second-preimage attack. The empty tree's root is `H()`, the hash of zero bytes, not an exception.
- **Verification is total.** Every verifier returns `false` for malformed input — a leaf index at or past the tree size, a path too long or too short, a step of the wrong width, a zero tree size — and only a `null` path or proof throws. A verifier sits directly behind untrusted bytes, so an exception where a `false` belongs is a denial of service.
- **`VerifyInclusion`'s `treeSize` is trusted input.** This is RFC 6962 as specified, not a defect: a four-entry tree's path for entry 0 has exactly the length a three-entry tree's first path wants and walks to the same head, so both verify. When the size comes from the party being examined, they can understate it and exempt their last entries from ever being challenged. `BindRoot` closes the gap by folding the length into the published root as `H(0x02 ‖ uint64_be(length) ‖ root)`; `VerifyInclusionBound` and `VerifyBlockInclusion` are the fail-closed verifiers that derive the size from it instead of accepting one.
- **Logarithmic memory when you only want the root.** `ComputeRootOfBlocks` folds the tree as it reads — each leaf is pushed as a one-leaf subtree and equal-sized tops are merged immediately — so the stack never exceeds `popcount(n)` hashes and peak memory is `O(blockSize + log n · HashLength)`. `ComputeBlocked` retains every leaf hash instead, which is what makes a later path possible; budget `leafCount × HashLength` bytes for it.
- **Parallel leaf hashing never changes the root.** `ComputeBlockedParallel` / `ComputeRootParallel` hash leaves concurrently and fold the results through the same reduction, so the root is bit-identical to the sequential one for every input. Choosing between them is a performance question, never a compatibility one. Prefer the `ReadOnlyMemory<byte>` overload: a stream must be read sequentially, so the stream overload copies each block on the calling thread and caps the gain.
- **Inclusion proves membership; consistency proves append-only growth.** `VerifyConsistency` takes both sizes and both roots and must reconstruct both, so the size ambiguity above has no analogue. Three cases are decided without walking the proof: a later size below the earlier one is rejected, equal sizes require identical roots *and* an empty proof, and a first size of zero requires an empty proof because every tree extends the empty tree.
- **The domain-separation prefixes are shared in source, not duplicated.** `MerkleTreeFormat` — the `0x00` / `0x01` / `0x02` prefixes — lives in this package's `shared/` folder and is source-compiled into `Bodu.Security.Cryptography` as well, so the two libraries' prefix values cannot drift apart. The stateless RFC 6962 primitives beneath the facade (`MerkleTreeCore`) are shared the same way and depend on nothing beyond `Bodu.Core`; a separate source-only test assembly compiles them with no reference to this package at all, so adding an outside dependency to the shared source fails that build.
