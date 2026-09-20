---
uid: Bodu.Collections.Specialized
---

![Bodu.Collections.Specialized](~/images/hero-collections.svg)

## Purpose

**Bodu.Collections.Specialized** holds the members of the **`Bodu.Collections`** package that serve a specialised purpose rather than acting as general-purpose containers. Two families live here today: <xref:Bodu.Collections.Specialized.BitSet>, a packed bit set over dense non-negative integers, and the [RFC 6962](https://www.rfc-editor.org/rfc/rfc6962#section-2.1) Merkle tree with its inclusion and consistency proofs.

What they have in common is independence. Neither references anything else in the package, and nothing else in the package references them — unlike every other non-generic type in <xref:Bodu.Collections.Generic>, which is either a collection in its own right or a policy or option satellite of one. Separating them keeps the general catalogue cohesive without fragmenting the package: `Bodu.Collections` still ships everything, and reaching for these costs a second `using`.

The namespace follows [`System.Collections.Specialized`](https://learn.microsoft.com/dotnet/api/system.collections.specialized) in intent — structures tuned to a narrow job rather than a general shape — though not in content: nothing here is a string-keyed dictionary, and the Merkle types are not collections at all.

## Static documentation

- **[Bit set](~/guides/core/bit-set.md)** — dense integer membership as packed bits, and when to prefer it over the BCL `BitArray`.
- **[RFC 6962 Merkle trees and proofs](~/guides/core/rfc6962-merkle-trees.md)** — entry mode, block mode, the streaming fold, proofs, the tree-size ambiguity, and parallel leaf hashing.
- **[Bodu.Collections introduction](~/docs/collections/index.md)** — the package these types ship in, and where they sit among its namespaces.
- **[Merkle commitments](~/docs/collections/concepts.md)** — the vocabulary: Merkle Tree Hash, split point, domain separation, audit and consistency proofs, bound roots.

## Key types

**Bit sets**

- <xref:Bodu.Collections.Specialized.BitSet> — growable packed bit set with Java `BitSet` semantics (`NextSetBit` / `NextClearBit` / `Cardinality`, in-place `And` / `Or` / `Xor` / `AndNot`). See the [bit set guide](~/guides/core/bit-set.md).

**Merkle trees and proofs**

- <xref:Bodu.Collections.Specialized.Rfc6962MerkleTree> — the [RFC 6962](https://www.rfc-editor.org/rfc/rfc6962#section-2.1) Merkle Tree Hash over any <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> the caller supplies, as one immutable, stateless facade. Root computation comes in three shapes — `ComputeRoot` / `ComputeRootOfLeafHashes` over a list of entries, `ComputeBlocked` / `ComputeRootOfBlocks` over a stream or span cut into fixed-size blocks, and `ComputeBlockedParallel` / `ComputeRootParallel`, which spread leaf hashing across threads without changing the tree. Proofs are `AuthenticationPath` and `ConsistencyProof` on the prover side and `VerifyInclusion`, `VerifyInclusionOfLeafHash`, `VerifyInclusionBound`, `VerifyBlockInclusion`, and `VerifyConsistency` on the verifier side; every verifier returns `false` for malformed input rather than throwing. `HashLeaf`, `HashNode`, and `BindRoot` expose the three domain-separated primitives directly.
- <xref:Bodu.Collections.Specialized.MerkleComputation> — the result of a block-mode pass: the `Root`, the `InputLength` and `BlockSize` it was computed over, and the ordered `LeafHashes`. Returning the leaf hashes from the same pass is the point — an authentication path needs them, and without them a large object would have to be streamed twice.
- <xref:Bodu.Collections.Specialized.MerkleBlocks> — the block arithmetic as standalone 64-bit helpers: `BlockCount`, `BlockOffset`, and `BlockLength`. A zero-length input has **no** blocks rather than one empty block, and a final short block is hashed at its actual length rather than padded.

## Example

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Specialized;

// Dense integer membership as packed bits — Java BitSet semantics.
var flags = new BitSet();
flags.Set(4);
flags.Set(9);
int first = flags.NextSetBit(0);          // 4
int count = flags.Cardinality;            // 2

// An RFC 6962 root plus an audit proof for one entry.
var tree = new Rfc6962MerkleTree(SHA256.Create);   // immutable; share it across threads

ReadOnlyMemory<byte>[] entries = [new byte[0], new byte[] { 0x00 }, new byte[] { 0x10 }];

byte[]   root = tree.ComputeRoot(entries);
byte[][] path = tree.AuthenticationPath(entries, leafIndex: 2);

bool included = tree.VerifyInclusion(
    root, treeSize: entries.Length, leafIndex: 2,
    entry: entries[2].Span,
    path: path.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

## Notes

- **The Merkle tree is RFC 6962's, exactly.** A tree of *n* leaves splits at `k`, the largest power of two **strictly below** *n*, and a lone subtree root is promoted **unchanged** rather than re-hashed. Leaves are `H(0x00 ‖ entry)` and internal nodes `H(0x01 ‖ left ‖ right)`, so a leaf hash can never be mistaken for a node hash — the defence against the second-preimage attack. The empty tree's root is `H()`, not an exception. `Bodu.Security.Cryptography` also ships `MerkleTreeHash` and `ParallelMerkleTreeHash`, which borrow that domain separation but **not** the tree shape: they reduce level by level with a configurable fan-out, so their roots agree with this one's only when the leaf count is a power of two, and they produce no proofs.
- **`VerifyInclusion`'s `treeSize` is trusted input.** This is RFC 6962 as specified, not a defect: a four-entry tree's path for entry 0 walks to the same head a three-entry tree's first path does, so both verify. When the size comes from the party being examined, they can understate it and exempt their last entries from challenge. `BindRoot` closes the gap by folding the length into the published root as `H(0x02 ‖ uint64_be(length) ‖ root)`, and `VerifyInclusionBound` / `VerifyBlockInclusion` derive the size from it instead of accepting one.
- **`Rfc6962MerkleTree` is not `IDisposable`, deliberately.** It owns no digest state — it creates, uses, and disposes a `HashAlgorithm` within each call — so one instance is safe to share across threads, provided the supplied factory returns a fresh instance each time (as `SHA256.Create` does).
- **`BitSet` grows on write, not on read.** `Set` and `Flip` extend the backing words as needed; a `Get` past the current length returns `false` rather than throwing or growing, so probing a sparse high index costs nothing. Negative indices are rejected.
- **Neither type is thread-safe for mutation.** `BitSet` requires external synchronization if written concurrently. `Rfc6962MerkleTree` is the opposite case — it is immutable and holds no digest state, so one instance is safe to share across threads provided the supplied factory returns a fresh `HashAlgorithm` each call.
- **See also:** the [bit set guide](~/guides/core/bit-set.md), the [RFC 6962 Merkle trees and proofs guide](~/guides/core/rfc6962-merkle-trees.md), and the [Bodu.Collections introduction](~/docs/collections/index.md) for the full scenario table.
