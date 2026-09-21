---
title: Using Merkle trees
---

# Using Merkle trees

A Merkle tree takes a stream, chops it into fixed-size leaves, hashes each leaf, then hashes pairs of child hashes together, level by level, until one root hash remains. The root changes if any byte of the input changes — but because the tree is built bottom-up, you can also prove a single chunk's integrity without rehashing the whole stream.

**Bodu.Security.Cryptography** ships two Merkle hashers, and the **Bodu.Collections** package ships the tree they are built on, in its `Bodu.Collections.Specialized` namespace:

| Type | Shape | When to reach for it |
|---|---|---|
| <xref:Bodu.Security.Cryptography.MerkleTreeHash> | Synchronous, single-threaded | Simple, deterministic. Fine for most files and everyday use. |
| <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Leaves hashed in parallel, folded in order | Large inputs where leaf hashing dominates and several cores are available. |
| <xref:Bodu.Collections.Specialized.Rfc6962MerkleTree> — [guide](../core/rfc6962-merkle-trees.md) | RFC 6962 binary tree *(different package)* | Interoperating with a transparency log, and the only one that produces **inclusion and consistency proofs**. Ships in the **Bodu.Collections** package, which depends only on Bodu.Core — so a consumer that needs commitments does not take the cipher catalogue with it. |

None is itself a `HashAlgorithm` — all three are composition wrappers that take a **factory** for the underlying hash (SHA-256, Tiger, anything that derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>) and orchestrate the tree on top of it.

> [!IMPORTANT]
> **All three build the same tree.** `MerkleTreeHash` and `ParallelMerkleTreeHash` are facades over the RFC 6962 construction that `Rfc6962MerkleTree` exposes in full — the fold and the domain-separation prefixes are compiled from one shared source, not reimplemented. Leaves are hashed as `H(0x00 ‖ block)`, internal nodes as `H(0x01 ‖ left ‖ right)`, a lone node is promoted to the next level unchanged rather than re-hashed, and an empty input yields the empty tree's root `H()`, exactly as [RFC 6962 §2.1](https://www.rfc-editor.org/rfc/rfc6962#section-2.1) defines the Merkle Tree Hash.
>
> At the default `fanOut` of two, a root from either hasher is therefore bit-identical to `Rfc6962MerkleTree.ComputeRootOfBlocks` over the same blocks, and that type's inclusion and consistency proofs verify against it. A wider `fanOut` is an explicit non-RFC mode: a sound commitment of its own, shallower and with wider internal nodes, but RFC 6962 has no k-ary form, so such roots interoperate with nothing outside this package.

![Merkle tree construction](../../images/diagrams/merkle-tree.svg)

## Pattern 1 — a simple Merkle root

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        4096);   // 4 KiB leaves; fanOut defaults to 2

using var stream = File.OpenRead("archive.bin");
byte[] root = merkle.ComputeHash(stream);
```

Each leaf is a SHA-256 of `0x00` followed by a 4 KiB block of the input; a short final block is hashed at its actual length, never padded. Each internal node is a SHA-256 of `0x01` followed by its two child hashes. The root changes if any byte of the file changes — that's the property a Merkle tree gives you over a flat hash — and it is the root any RFC 6962 implementation computes over the same blocks.

## Pattern 2 — wider fan-out

A larger `fanOut` reduces tree depth (fewer levels of hashing), at the cost of more bytes concatenated per internal node — and of interoperability, because the result is no longer RFC 6962's tree:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        8192,
    fanOut:           4);      // quaternary tree — shallower, wider internal nodes, not RFC 6962
```

Four child hashes concatenated is 4 × 32 = 128 bytes of input per internal SHA-256, still well inside one SHA-256 block. A short final group at any level is hashed with the children it has; a lone leftover is promoted unchanged, the same rule the binary tree applies.

## Pattern 3 — a Tiger Tree Hash

The Tiger Tree Hash (THEX / TTH) construction used by content-addressed file sharing is Tiger over 1024-byte leaves with `0x00` prepended to each leaf and `0x01` to each internal node, pairing left to right and carrying a lone node up unchanged — the same rules as RFC 6962. `MerkleTreeHash` applies those prefixes for you, so over a non-empty input this is the whole recipe:

```csharp
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => new Tiger(),
    blockSize:        1024);

using var stream = File.OpenRead("archive.bin");
byte[] tigerTreeRoot = merkle.ComputeHash(stream);   // 24-byte root
```

The one input on which the two conventions part ways is the empty one: TTH hashes a single empty leaf, whereas this type follows RFC 6962 and returns `H()`, the hash of zero bytes. Special-case a zero-length input if you need the TTH value for it.

## Pattern 4 — parallel leaf hashing

<xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> reads the input in batches of blocks, hashes each batch's leaves concurrently (one `HashAlgorithm` per worker, so nothing is shared between threads), and folds the leaves in order into the same tree. Leaf hashing is where the time goes — every input byte passes through the algorithm once — while the fold hashes one digest-sized node per pair and is a negligible fraction of the work, so parallelizing the leaves is what makes the difference and the fold stays sequential and simple.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new ParallelMerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        64 * 1024);

await using var stream = File.OpenRead("large-archive.bin");
byte[] root = await merkle.ComputeHashAsync(stream);
```

The parallel version produces the same root as the sequential one for the same `(algorithmFactory, blockSize, fanOut)` — the parallelism is in *how* the leaves are hashed, not *what* tree is built. `ComputeHashAsync` takes an optional `CancellationToken`, and `Dispose` cancels a computation still in flight. For inputs already in memory, the synchronous `ComputeHash` overloads over `ReadOnlyMemory<byte>`, `ReadOnlySpan<byte>`, and `byte[]` parallelize the same way.

## Pattern 5 — capturing diagnostics

Every `ComputeHash` / `ComputeHashAsync` overload on both hashers accepts an optional <xref:Bodu.Security.Cryptography.MerkleTreeDiagnostics> that records every node the tree built — level, index, child hashes, and the produced hash value. Useful when you want to visualize the tree or cross-check an implementation against a known-good one:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

var diagnostics = new MerkleTreeDiagnostics();

using var merkle = new ParallelMerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        4096);

byte[] root = await merkle.ComputeHashAsync(stream, diagnostics);

for (int level = 0; level < diagnostics.GetLevelCount(); level++)
{
    IReadOnlyList<MerkleTreeDiagnosticNode> nodes = diagnostics.GetLevel(level);
    Console.WriteLine($"level {level}: {nodes.Count} nodes");
}

// Or dump the whole tree to a writer (one line per node):
diagnostics.WriteTo(Console.Out);
```

Diagnostics are optional; pass `null` (the default) when you don't need them and the fold avoids the book-keeping entirely. For strict equivalence checks, `diagnostics.Validate(() => SHA256.Create(), out var errors)` re-derives each internal node from its children and reports any that don't match. A promoted node is recorded once, at the level it was produced — it never appears as a one-child node above.

The same recorder type ships in the **Bodu.Collections** package as `Bodu.Collections.Specialized.MerkleTreeDiagnostics`, where `Rfc6962MerkleTree.ComputeRootOfBlocks` accepts it, so a trace captured on either side of the package boundary reads the same way.

## Inclusion proofs

The reason to build a tree rather than a flat digest is the **inclusion proof** (a.k.a. Merkle / audit proof): a logarithmic-size witness that one leaf belongs under a known root, without revealing or rehashing the rest of the input. For a leaf at position `i`, the proof is the sequence of *sibling* hashes on the path from that leaf up to the root; a verifier recomputes each parent from the node and its sibling, level by level, and accepts only if the final value equals the trusted root.

Because the two hashers build RFC 6962's tree at the default fan-out, you do not assemble that proof by hand: <xref:Bodu.Collections.Specialized.Rfc6962MerkleTree> produces and verifies it, and its roots are the roots `MerkleTreeHash` computed.

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Specialized;
using Bodu.Security.Cryptography;

const int blockSize = 4096;

// The publisher's root, computed with the hasher …
using var merkle = new MerkleTreeHash(() => SHA256.Create(), blockSize);
byte[] published = merkle.ComputeHash(data);

// … and the prover's tree over the same blocks: same root, plus a proof for block 5.
var tree = new Rfc6962MerkleTree(SHA256.Create);
MerkleBlockComputation computation = tree.ComputeBlocked(data, blockSize);
byte[][] path = tree.AuthenticationPath(computation.LeafHashes, leafIndex: 5);

// The verifier needs only the published root, the tree size, the block and its index, and the path.
bool included = tree.VerifyInclusion(
    published, treeSize: computation.LeafHashes.Count, leafIndex: 5, block5,
    Array.ConvertAll(path, step => (ReadOnlyMemory<byte>)step));
```

`computation.Root` and `published` are the same bytes. When the tree size comes from an untrusted party, publish `BindRoot(root, inputLength)` instead and verify with `VerifyBlockInclusion`, which recomputes the size from the bound length. The full treatment — entry and block modes, consistency proofs, and the length-bound roots that close RFC 6962's tree-size ambiguity — is in **[RFC 6962 Merkle trees and proofs](../core/rfc6962-merkle-trees.md)**.

> [!TIP]
> Only a wider `fanOut` leaves you without a prover. There, <xref:Bodu.Security.Cryptography.MerkleTreeDiagnostics> still records every node with its `Level`, `Index`, `Hash`, and ordered `ChildHashes`, which is enough to walk a leaf's path and collect the `fanOut − 1` siblings at each level yourself — but a verifier on the other side must then agree on the same non-standard shape.

## RFC 6962 trees and proofs

The standard's tree — and the only Bodu type that produces **inclusion and consistency proofs** — is <xref:Bodu.Collections.Specialized.Rfc6962MerkleTree>, in the **Bodu.Collections** package. It depends only on `Bodu.Core`, so reaching for commitments does not drag in ciphers, and it is binary by definition so there is no `fanOut` to choose.

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Specialized;

var tree = new Rfc6962MerkleTree(SHA256.Create);

byte[]   root = tree.ComputeRoot(entries);
byte[][] path = tree.AuthenticationPath(entries, leafIndex: 2);
```

It is the type to use whenever a root must interoperate, whenever you need a proof, and whenever an untrusted counterparty supplies the tree size (which `BindRoot` and the bound verifiers close off). The full treatment — entry and block modes, the logarithmic streaming fold, parallel leaf hashing with measurements, consistency proofs, and the failure modes proofs exist to stop — is in **[RFC 6962 Merkle trees and proofs](../core/rfc6962-merkle-trees.md)**.

## When to use a Merkle tree

- **Partial verification.** You want to prove that byte range `[N, M)` of a large file matches the original, without rehashing the whole file. The Merkle tree lets you do that with a logarithmic-size proof.
- **Content addressing.** Git, IPFS, and BitTorrent all use Merkle (or Merkle-like) trees so that identical sub-contents can be deduplicated and so that corruption is detected at the chunk level.
- **Streaming integrity.** You want to authenticate chunks of a stream as they arrive, without waiting for the whole thing to buffer.

For a single end-to-end file digest where partial verification is not a requirement, a plain <xref:Bodu.Security.Cryptography.Tiger> or `System.Security.Cryptography.SHA256` is simpler and does not need a tree.

## Where to go next

- **[RFC 6962 Merkle trees and proofs](../core/rfc6962-merkle-trees.md)** — the standard’s tree, inclusion and consistency proofs, and bound roots.
- [Hashing overview](hashing.md) — where Merkle trees sit alongside the other families.
- [Using Tiger](tiger.md) — a common leaf-hash choice for content-addressed systems.
- <xref:Bodu.Collections.Specialized.Rfc6962MerkleTree> · <xref:Bodu.Collections.Specialized.MerkleBlocks> · <xref:Bodu.Security.Cryptography.MerkleTreeHash> · <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash>.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
