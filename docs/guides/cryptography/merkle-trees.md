---
title: Using Merkle trees
---

# Using Merkle trees

A Merkle tree takes a stream, chops it into fixed-size leaves, hashes each leaf, then hashes groups of child hashes together, level by level, until one root hash remains. The root changes if any byte of the input changes — but because the tree is built bottom-up, you can also prove a single chunk's integrity without rehashing the whole stream.

**Bodu.Security.Cryptography** ships two Merkle implementations, and the companion **Bodu.Collections.Merkle** package ships a third:

| Type | Shape | When to reach for it |
|---|---|---|
| <xref:Bodu.Security.Cryptography.MerkleTreeHash> | Synchronous, single-threaded | Simple, deterministic. Fine for most files and everyday use. |
| <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Asynchronous, level-worker pipeline | Large inputs where leaf hashing and internal-node reduction should overlap. |
| <xref:Bodu.Collections.Merkle.Rfc6962MerkleTree> | RFC 6962 binary tree *(separate package)* | Interoperating with a transparency log or any other RFC 6962 implementation, and the only one that produces **inclusion and consistency proofs**. Ships in **Bodu.Collections.Merkle**, which depends only on Bodu.Core — so a consumer that needs commitments does not take the cipher catalogue with it. |

None is itself a `HashAlgorithm` — all three are composition wrappers that take a **factory** for the underlying hash (SHA-256, Tiger, anything that derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>) and orchestrate the tree on top of it.

> [!IMPORTANT]
> **`MerkleTreeHash` and `ParallelMerkleTreeHash` borrow RFC 6962's domain separation, not its tree.** Leaves are hashed as `H(0x00 ‖ block)` and internal nodes as `H(0x01 ‖ children)`, exactly as [RFC 6962 §2.1](https://www.rfc-editor.org/rfc/rfc6962#section-2.1) specifies. The *reduction* is different: RFC 6962 splits a tree of *n* leaves at `k`, the largest power of two strictly below *n*, and promotes a lone subtree root unchanged, whereas those two reduce level by level with a configurable `fanOut` and re-hash a lone leftover child as a one-child node. Their roots agree with RFC 6962's only when the leaf count is a power of two.
>
> So do **not** cross-check a root from either of them against a transparency log or another RFC 6962 implementation. A level-by-level reduction is a sound commitment — it is simply a different one — and those roots are stable and will not change. Where you need RFC 6962's actual tree, and where you need proofs, use <xref:Bodu.Collections.Merkle.Rfc6962MerkleTree> from the **Bodu.Collections.Merkle** package — see [RFC 6962 trees and proofs](#rfc-6962-trees-and-proofs).

![Merkle tree construction](../../images/diagrams/merkle-tree.svg)

## Pattern 1 — a simple Merkle root

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        4096,    // 4 KiB leaves
    fanOut:           2);      // binary tree

using var stream = File.OpenRead("archive.bin");
byte[] root = merkle.ComputeHash(stream);
```

Each leaf is a SHA-256 of a 4 KiB block of the input. Each internal node is a SHA-256 of the concatenation of its two child hashes. The root changes if any byte of the file changes — that's the property a Merkle tree gives you over a flat hash.

## Pattern 2 — wider fan-out

A larger `fanOut` reduces tree depth (fewer levels of hashing), at the cost of more bytes concatenated per internal node:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        8192,
    fanOut:           4);      // quaternary tree — shallower, wider internal nodes
```

Four child hashes concatenated is 4 × 32 = 128 bytes of input per internal SHA-256, still well inside one SHA-256 block.

## Pattern 3 — a Tiger Tree Hash

The TTH construction uses Tiger at the leaves and internal nodes, with domain-separation bytes (`0x00` for leaves, `0x01` for internal nodes) prepended to each hash input. The Bodu `MerkleTreeHash` does not prepend those separator bytes for you — if you need the exact TTH wire format, wrap the factory and prefix the bytes yourself. For a plain Merkle-over-Tiger digest, this is all you need:

```csharp
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => new Tiger(),
    blockSize:        1024,
    fanOut:           2);

using var stream = File.OpenRead("archive.bin");
byte[] tigerMerkleRoot = merkle.ComputeHash(stream);   // 24-byte root
```

## Pattern 4 — the parallel pipeline

<xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> overlaps leaf production (reading + hashing input chunks) with internal-node reduction (grouping child hashes and hashing them into a parent). It exposes an **async** surface because the pipeline drives itself from a producer/consumer queue:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new ParallelMerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        4096,
    fanOut:           2);

using var stream = File.OpenRead("large-archive.bin");
byte[] root = await merkle.ComputeHashAsync(stream, diagnostics: null, CancellationToken.None);
```

The parallel version produces the same root as the sequential one when called with the same `(algorithmFactory, blockSize, fanOut)` — the parallelism is in *how* the tree is built, not *what* tree is built. The level-worker / dispatcher layout is drawn in the <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> class documentation.

![Parallel Merkle tree pipeline](../../images/diagrams/parallel-merkle-tree.svg)

## Pattern 5 — capturing diagnostics

`ParallelMerkleTreeHash.ComputeHashAsync` accepts an optional <xref:Bodu.Security.Cryptography.MerkleTreeDiagnostics> that records every node the pipeline built — level, index, child hashes, and the produced hash value. Useful when you want to visualize the tree, cross-check an implementation against a known-good one, or produce a Merkle proof for a specific leaf:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

var diagnostics = new MerkleTreeDiagnostics();

using var merkle = new ParallelMerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        4096,
    fanOut:           2);

byte[] root = await merkle.ComputeHashAsync(stream, diagnostics, CancellationToken.None);

for (int level = 0; level < diagnostics.GetLevelCount(); level++)
{
    IReadOnlyList<MerkleTreeDiagnosticNode> nodes = diagnostics.GetLevel(level);
    Console.WriteLine($"level {level}: {nodes.Count} nodes");
}

// Or dump the whole tree to a writer (one line per node):
diagnostics.WriteTo(Console.Out);
```

Diagnostics are optional; pass `null` when you don't need them and the pipeline avoids the book-keeping overhead entirely. For strict equivalence checks, `diagnostics.Validate(() => SHA256.Create(), out var errors)` re-derives each internal node from its children and reports any that don't match.

## Inclusion proofs

The reason to build a tree rather than a flat digest is the **inclusion proof** (a.k.a. Merkle / audit proof): a logarithmic-size witness that one leaf belongs under a known root, without revealing or rehashing the rest of the input. For a leaf at position `i`, the proof is the sequence of *sibling* hashes encountered on the path from that leaf up to the root — `fanOut − 1` siblings per level, so the proof size grows as `(fanOut − 1) · log_fanOut(leafCount)`. A verifier recomputes each parent by hashing the concatenation of the node and its siblings in order, level by level, and accepts only if the final value equals the trusted root.

> [!TIP]
> If you want inclusion proofs, reach for <xref:Bodu.Collections.Merkle.Rfc6962MerkleTree> rather than assembling them by hand — see [RFC 6962 trees and proofs](#rfc-6962-trees-and-proofs) below. The hand-rolled approach in this section remains documented because it is the only way to obtain a proof over the *level-by-level* tree shape that `MerkleTreeHash` and `ParallelMerkleTreeHash` build.

For those two types there is no one-call `GetProof(i)` API, but every hash the tree builds is available through <xref:Bodu.Security.Cryptography.MerkleTreeDiagnostics>, so you can assemble a proof yourself. Each captured <xref:Bodu.Security.Cryptography.MerkleTreeDiagnosticNode> records its `Level`, `Index`, produced `Hash`, and the ordered `ChildHashes` it was built from — enough to walk a leaf's path and collect the siblings:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

var diagnostics = new MerkleTreeDiagnostics();

using var merkle = new ParallelMerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize:        4096,
    fanOut:           2);

byte[] root = await merkle.ComputeHashAsync(stream, diagnostics, CancellationToken.None);

// Walk level 0 → root, collecting the sibling hash beside each node on leaf 5's path.
int index = 5;
var proof = new List<byte[]>();
for (int level = 0; level < diagnostics.GetLevelCount() - 1; level++)
{
    IReadOnlyList<MerkleTreeDiagnosticNode> nodes = diagnostics.GetLevel(level);
    int siblingIndex = index ^ 1;                       // binary-tree sibling
    if (siblingIndex < nodes.Count)
        proof.Add(nodes[siblingIndex].Hash);
    index /= 2;                                          // move to the parent
}
```

To re-derive every internal node from its children and confirm the whole tree is self-consistent, call `diagnostics.Validate(() => SHA256.Create(), out var errors)` — it returns `false` and populates `errors` if any parent does not match the hash of its recorded children.

## Parallel construction

<xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> produces the **same root** as the sequential <xref:Bodu.Security.Cryptography.MerkleTreeHash> for the same `(algorithmFactory, blockSize, fanOut)` — the parallelism changes *how* the tree is built, not *what* tree results, so a root computed in parallel verifies against a proof checked sequentially and vice versa. The dispatcher reads the input in chunks and feeds leaves into a producer/consumer pipeline while level-workers reduce completed groups into parents concurrently; a short tail block is hashed at its **actual** length and is never zero-padded, so an input cannot collide with a zero-extended longer one. See [Pattern 4](#pattern-4--the-parallel-pipeline) above and the class documentation's swim-lane diagram.

## RFC 6962 trees and proofs

<xref:Bodu.Collections.Merkle.Rfc6962MerkleTree> — in the separate **Bodu.Collections.Merkle** package, namespace `Bodu.Collections.Merkle` — is the type to use when a root must interoperate with a transparency log, an artifact attestation, or anything else built to [RFC 6962](https://www.rfc-editor.org/rfc/rfc6962#section-2.1), and it is the only one of the three that produces proofs. It depends only on `Bodu.Core`, so reaching for commitments does not drag in ciphers. It is binary by definition, so there is no `fanOut` to choose, and an empty tree's root is `H()` rather than an exception.

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Merkle;

var tree = new Rfc6962MerkleTree(SHA256.Create);   // immutable; safe to share across threads

ReadOnlyMemory<byte>[] entries = [ new byte[0], new byte[] { 0x00 }, new byte[] { 0x10 } ];

byte[] root  = tree.ComputeRoot(entries);
byte[][] path = tree.AuthenticationPath(entries, leafIndex: 2);

bool ok = tree.VerifyInclusion(
    root, treeSize: entries.Length, leafIndex: 2,
    entry: entries[2].Span,
    path: path.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

For a byte stream, block mode gives you the root and — in the same pass — the leaf hashes a path needs, so a large object is never read twice:

```csharp
MerkleComputation computation = tree.ComputeBlocked(stream, blockSize: 1024 * 1024);

byte[]   root      = computation.Root;
byte[][] blockPath = tree.AuthenticationPath(computation.LeafHashes, blockIndex);
```

Use `ComputeRootOfBlocks` instead when you want only the root: it folds the tree as it reads, so memory stays at `O(blockSize + log n · hashSize)` rather than retaining every leaf hash.

### Hashing leaves in parallel

`ComputeBlockedParallel` and `ComputeRootParallel` spread **leaf hashing** across threads. The tree shape is untouched — both paths fold the resulting leaf hashes through the same reduction — so the root is bit-identical to the sequential one for every input, and switching between them is a performance question and never a compatibility one.

```csharp
// Bytes already in memory — this is the one that scales.
MerkleComputation computation = tree.ComputeBlockedParallel(buffer.AsMemory(), blockSize: 1024 * 1024);

// From a stream, when the object is too large to hold.
MerkleComputation streamed = tree.ComputeBlockedParallel(stream, blockSize: 1024 * 1024);
```

Prefer the `ReadOnlyMemory<byte>` overload where you can. A stream must be read sequentially, so that overload copies each block on the calling thread before any worker can touch it, which caps the gain; the in-memory overload copies *and* hashes each block inside its own worker.

How much you actually gain depends on how fast the leaf hash is relative to memory bandwidth — and it can be **negative**:

| Leaf hash | Stream overload | In-memory overload |
|---|---|---|
| SHA-256 (hardware-accelerated) | ~1.1× | ~1.9× |
| SHA-512 | ~1.8× | ~2.5× |
| Tiger (managed) | ~2.4× | ~3.0× |
| BLAKE2b (managed) | ~2.5× | ~2.6× |

> [!NOTE]
> Measured over 64 MiB at one-mebibyte blocks on four cores. Treat these as shape, not specification. A hardware-accelerated SHA-256 already runs close to memory bandwidth, so there is little for extra threads to recover — and on a fast source the sequential overload can beat the parallel stream overload outright. Measure your own case before reaching for these.

### Trust the size, or bind it

> [!WARNING]
> `VerifyInclusion`'s `treeSize` is **trusted input**. A four-entry tree's path for entry 0 has exactly the length a three-entry tree's first path wants and walks to the same head, so verification accepts both. If you take the size from the party you are examining, they can understate it — exempting their last entries from ever being challenged — and every check still passes.

That is RFC 6962 behaving as specified, not a defect. When the size comes from an untrusted party, publish a **length-bound** root and verify against that instead:

```csharp
byte[] published = tree.BindRoot(computation.Root, computation.InputLength);

bool ok = tree.VerifyBlockInclusion(
    published, computation.InputLength, blockSize, blockIndex, block, blockPath);
```

`VerifyBlockInclusion` derives the tree size from the bound length and block size, so there is no size left to misstate, and it requires the block to be exactly the length its position demands. `VerifyInclusionBound` is the entry-mode equivalent.

### Consistency proofs

For an append-only log, a consistency proof shows that an earlier published tree is a prefix of a later one:

```csharp
byte[][] proof = tree.ConsistencyProof(entries, firstSize: 3);

bool ok = tree.VerifyConsistency(
    firstRoot, firstSize: 3, secondRoot, secondSize: entries.Length,
    proof.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

Both sizes and both roots are inputs and the proof must reconstruct both, so the size ambiguity above has no analogue here. Three cases are decided without walking the proof: a later size below the earlier one is rejected outright, equal sizes require identical roots *and* an empty proof, and a first size of zero requires an empty proof because every tree extends the empty tree.

All verification entry points return `false` for malformed input rather than throwing — a wrong index, a path too long or too short, an element of the wrong width — because a verifier sits directly behind untrusted input, and an exception where a `false` belongs is a denial of service.

## When to use a Merkle tree

- **Partial verification.** You want to prove that byte range `[N, M)` of a large file matches the original, without rehashing the whole file. The Merkle tree lets you do that with a logarithmic-size proof.
- **Content addressing.** Git, IPFS, and BitTorrent all use Merkle (or Merkle-like) trees so that identical sub-contents can be deduplicated and so that corruption is detected at the chunk level.
- **Streaming integrity.** You want to authenticate chunks of a stream as they arrive, without waiting for the whole thing to buffer.

For a single end-to-end file digest where partial verification is not a requirement, a plain <xref:Bodu.Security.Cryptography.Tiger> or `System.Security.Cryptography.SHA256` is simpler and does not need a tree.

## Where to go next

- [Hashing overview](hashing.md) — where Merkle trees sit alongside the other families.
- [Using Tiger](tiger.md) — a common leaf-hash choice for content-addressed systems.
- <xref:Bodu.Collections.Merkle.Rfc6962MerkleTree> · <xref:Bodu.Collections.Merkle.MerkleBlocks> · <xref:Bodu.Security.Cryptography.MerkleTreeHash> · <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash>.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
