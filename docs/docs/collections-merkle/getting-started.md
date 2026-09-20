---
title: Bodu.Collections.Merkle — Getting started
---

# Bodu.Collections.Merkle — Getting started

## Install

```bash
dotnet add package Bodu.Collections.Merkle
```

Targets `net8.0`. The only dependency is `Bodu.Core`, which is pulled in automatically — no cipher catalogue comes with it. Every type lives in the `Bodu.Collections.Merkle` namespace.

The hash is supplied by you, so nothing further is needed to use the BCL algorithms:

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Merkle;

var tree = new Rfc6962MerkleTree(SHA256.Create);
```

To use a Bodu digest at the leaves instead, add that package too and pass its factory — `new Rfc6962MerkleTree(() => new Tiger())` for a Tiger tree, `() => new Blake2b()` for BLAKE2b. The tree probes the factory once at construction to learn `HashLength`.

One instance is **immutable and stateless**: it keeps no digest state between calls, so share it across threads freely, and do not wrap it in a `using` — it is deliberately not `IDisposable`, because there is nothing on it to dispose.

## Minimal samples

### A root over a list of entries

```csharp
using System.Security.Cryptography;
using Bodu.Collections.Merkle;

var tree = new Rfc6962MerkleTree(SHA256.Create);

ReadOnlyMemory<byte>[] entries =
[
    new byte[0],                 // an empty entry is a legitimate entry
    new byte[] { 0x00 },
    new byte[] { 0x10 },
];

byte[] root = tree.ComputeRoot(entries);     // 32 bytes for SHA-256
```

An **empty** list is not an error: its root is `H()`, the hash of zero bytes, matching RFC 6962's `MTH({})`.

### Prove one entry belongs under a root

```csharp
byte[][] path = tree.AuthenticationPath(entries, leafIndex: 2);

bool ok = tree.VerifyInclusion(
    root,
    treeSize:  entries.Length,
    leafIndex: 2,
    entry:     entries[2].Span,
    path:      path.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

The verifier returns `false` for anything malformed — a wrong index, a path of the wrong length, a step of the wrong width — rather than throwing. Only a `null` path throws.

### A root over a stream, in one pass

```csharp
using var stream = File.OpenRead("archive.bin");

MerkleComputation computation = tree.ComputeBlocked(stream, blockSize: 1024 * 1024);

byte[] root    = computation.Root;
long   length  = computation.InputLength;
IReadOnlyList<byte[]> leaves = computation.LeafHashes;   // one per 1 MiB block
```

The leaf hashes come back from the same pass, so an authentication path over any block needs no second read. When you want **only** the root, use `ComputeRootOfBlocks` instead — it folds the tree as it reads and never holds more than a logarithmic number of hashes:

```csharp
byte[] rootOnly = tree.ComputeRootOfBlocks(stream, blockSize: 1024 * 1024);
```

### Prove one chunk of a large object

Publish a **length-bound** root so the tree size cannot be misstated, then verify against it:

```csharp
byte[] published = tree.BindRoot(computation.Root, computation.InputLength);

byte[][] blockPath = tree.AuthenticationPath(computation.LeafHashes, blockIndex: 7);

bool ok = tree.VerifyBlockInclusion(
    published,
    computation.InputLength,
    blockSize:  1024 * 1024,
    blockIndex: 7,
    block:      seventhBlock,
    path:       blockPath.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

`VerifyBlockInclusion` derives the tree size from the bound length and the block size, so there is no size argument left to lie about, and it requires the block to be exactly the length its position demands. `VerifyInclusionBound` is the entry-mode equivalent. Prefer both over `VerifyInclusion` whenever the size would come from the party you are examining — see [concepts](concepts.md#tree-size-ambiguity-and-bound-roots).

### Prove a log only ever appended

```csharp
byte[] firstRoot  = tree.ComputeRoot(entries.Take(3).ToArray());
byte[] secondRoot = tree.ComputeRoot(entries);

byte[][] proof = tree.ConsistencyProof(entries, firstSize: 3);

bool ok = tree.VerifyConsistency(
    firstRoot,  firstSize:  3,
    secondRoot, secondSize: entries.Length,
    proof.Select(step => (ReadOnlyMemory<byte>)step).ToArray());
```

Both sizes and both roots are inputs and the proof must reconstruct both, so nothing here can be understated.

### Hash the leaves on several cores

```csharp
// Bytes already in memory — this is the overload that scales.
MerkleComputation computation = tree.ComputeBlockedParallel(buffer.AsMemory(), blockSize: 1024 * 1024);

// From a stream, when the object is too large to hold.
MerkleComputation streamed = tree.ComputeBlockedParallel(stream, blockSize: 1024 * 1024);

// Root only, entries already in memory.
byte[] root = tree.ComputeRootParallel(entries);
```

The root is bit-identical to the sequential one for every input, so this is never a compatibility decision. It is not automatically a win either — measure it. See [the guide's measurements](../../guides/merkle/rfc6962-merkle-trees.md#hashing-leaves-in-parallel).

### Compose the primitives directly

```csharp
byte[] leaf  = tree.HashLeaf(entry);                  // H(0x00 || entry)
byte[] node  = tree.HashNode(leftChild, rightChild);  // H(0x01 || left || right)
byte[] bound = tree.BindRoot(root, length);           // H(0x02 || uint64_be(length) || root)

int width = tree.HashLength;                          // 32 for SHA-256
```

`ComputeRootOfLeafHashes`, `AuthenticationPath(IReadOnlyList<byte[]>, long)`, `ConsistencyProofOfLeafHashes`, and `VerifyInclusionOfLeafHash` all take leaf hashes rather than entries, for callers that already hold them — from a cache, a previous pass, or a `MerkleComputation`.

## Choosing the mode

| You have | You want | Call |
|---|---|---|
| a list of records | the root | `ComputeRoot` |
| a list of records | the root and a proof for one | `ComputeRoot` + `AuthenticationPath` |
| a stream | the root only, minimal memory | `ComputeRootOfBlocks` |
| a stream | the root and proofs for chunks | `ComputeBlocked` |
| an in-memory buffer and spare cores | the root and proofs, faster | `ComputeBlockedParallel(ReadOnlyMemory<byte>, …)` |
| leaf hashes already computed | the root or a proof | the `…OfLeafHashes` / `IReadOnlyList<byte[]>` overloads |

## Where to go next

- **[Core concepts](concepts.md)** — split point, domain separation, the size ambiguity, the logarithmic fold.
- **[RFC 6962 Merkle trees and proofs](../../guides/merkle/rfc6962-merkle-trees.md)** — the full walk-through with measurements and failure modes.
- **[Introduction](index.md)** — the package shape and scenario table.
- **[Bodu.Collections.Merkle API reference](xref:Bodu.Collections.Merkle)** — full namespace overview.
