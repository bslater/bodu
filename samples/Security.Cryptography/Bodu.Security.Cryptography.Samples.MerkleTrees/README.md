# Bodu.Security.Cryptography.Samples.MerkleTrees

The RFC 6962 Merkle-tree surface: `MerkleTree` and its family — `MerkleBlockAccumulator`,
`MerkleBlockComputation`, and the optional `MerkleTreeDiagnostics` trace recorder. Seven scenarios covering the
commitment (roots and proofs), the length-bound root that closes RFC 6962's tree-size ambiguity, the append-only
guarantee, the blocked/streaming/async surface, write-time accumulation, the fan-out and parallelism knobs, and
the diagnostics trace.

Everything runs offline and deterministically: every input is fixed, and the appendix D vectors are reproduced
byte for byte so the roots can be checked against the RFC by eye.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.MerkleTrees
```

For NuGet consumers:

```bash
dotnet add package Bodu.Security.Cryptography
```

## Scenario 1 — Commitments

**Intent.** Show the core commitment: one root hash stands for a whole list, and a logarithmic audit path proves
one entry's membership against it without revealing the rest. Show *why* the RFC prefixes its hashes — the domain
separation that closes the second-preimage attack — and that verification is total, so a verifier can be fed
attacker-supplied values without a `try`/`catch`.

**What it does.** Commits to a seven-entry audit log, prints the root, then contrasts `HashLeaf(e)` with a plain
`SHA256(e)` and shows `HashNode`. It checks the empty-tree and one-entry special cases, issues the audit path for
entry 3, and verifies it both from the entry and from its leaf hash. It then tampers with the entry, claims the
wrong index, and feeds three malformed inputs.

**What to expect.** `HashLeaf(e[0])` differs from `SHA256(e[0])` because a leaf is `H(0x00 ‖ entry)`; a one-entry
tree's root *is* that leaf hash, with no node hashing at all. The seven-entry path is three steps (⌈log₂ 7⌉).
Verification then fails for every corruption — including the malformed inputs, which return `false` rather than
throwing:

```text
  hash length   : 32 bytes (SHA-256)
  fan-out       : 2 (IsBinary=True - RFC 6962's tree)
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
  by leaf hash  : True
  short root    : False
  path too long : False
  index >= size : False
```

The empty root `e3b0c442...` is the SHA-256 of the empty string, exactly as the RFC specifies. Note also that the
tree takes a hash-algorithm *factory*, not an instance — that is what makes `MerkleTree` immutable, stateless,
shareable across threads, and deliberately not `IDisposable`.

**APIs demonstrated.** `MerkleTree(Func<HashAlgorithm>)`, `.HashLength`, `.FanOut`, `.IsBinary`, `.HashLeaf`,
`.HashNode`, `.ComputeRoot`, `.AuthenticationPath`, `.VerifyInclusion`, `.VerifyInclusionOfLeafHash`.

## Scenario 2 — SizeBinding

**Intent.** Demonstrate RFC 6962's tree-size ambiguity — and that it is *specified behaviour, not a defect* — then
show the length-bound root that closes it. Read this before using `VerifyInclusion` anywhere the tree size comes
from the party being audited: its `treeSize` argument is trusted input, not something the root authenticates.

**What it does.** Cuts a 16-byte payload into four 4-byte blocks, computes the root and the audit path for block 0,
then verifies that path against both the true tree size (4) and an understated one (3). It binds the root to the
byte length with `BindRoot`, shows a 12-byte payload yielding a different bound root, and runs four
`VerifyInclusionBound` claims: the truth, a whole block understated, one byte short, one byte over. It finishes by
showing two inputs with the *same* block count but different bound roots.

**What to expect.** Both unbound verifications return `True`. That is not a bug: a four-leaf tree's path for leaf 0
has exactly the length a three-leaf tree's first path wants and walks to the same head, so the size check alone
cannot tell them apart. The consequence is concrete — a holder of a four-block object that has lost block 3 could
declare a three-block object, never be challenged for block 3, and pass every audit for ever. The bound verifier
rejects all three misstatements:

```text
  blocks        : 4 x 4 bytes
  root          : 516c43cb9e4f82fe70437703f0a41649c5fd963ba5f4be8b736dca20bae05bdd
  VerifyInclusion(size=4): True (the true size)
  VerifyInclusion(size=3): True (understated - still accepted)
  ^ RFC 6962's verifier working as specified, not a defect.
  BindRoot(.., 16): f5ed505e0cb1f0f1cb6909f10211fefe06a7d976b18f9c38e9bdf497802f2b04
  BindRoot(.., 12): 29f46d734672b404873bd2548b2ab1cf66cf22c5c412fb150331b344cd50332a
  bound roots differ: True
  bound(len=16, size=4): True (the truth)
  bound(len=12, size=3): False (a whole block understated)
  bound(len=15, size=4): False (off by one byte)
  bound(len=17, size=4): False (overstated)
  13 and 14 bytes are both 4 blocks, bound roots differ: True
```

All three hex values are the published appendix D vectors, so this scenario doubles as a conformance check you can
eyeball. The last line is why the library binds the *byte length* rather than the block count in block mode: it is
strictly stronger, because it also pins the final block's length. `BindRoot` computes
`H(0x02 ‖ u64_be(length) ‖ root)` and is an addition to RFC 6962, not part of it.

**APIs demonstrated.** `.ComputeRoot`, `.AuthenticationPath`, `.VerifyInclusion`, `.BindRoot`,
`.VerifyInclusionBound`, `MerkleTree.BlockCount`.

## Scenario 3 — Consistency

**Intent.** Show the append-only guarantee. An inclusion proof answers "is this entry in the log?"; a consistency
proof answers the harder question "is this the same log I saw last time, only longer?" — which is what stops an
operator quietly rewriting history.

**What it does.** Takes two snapshots of the same log (at 4 entries and at 7), computes the consistency proof over
the current list plus the earlier size, and verifies it from the two roots alone. It checks the degenerate `n → n`
case, then forges a log by rewriting entry 1 and appending as normal, and finally tries a reversed size pair, a
mis-sized proof step, and the leaf-hash overload.

**What to expect.** The genuine extension verifies from the two signed roots, with the auditor never seeing an
entry. The forged log's root is a perfectly valid Merkle root — it just is not an extension of what was published,
so the proof fails:

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
  from leaf hashes: identical proof: True
```

A single proof step suffices here because 4 is a power of two: the old tree is already a complete left subtree of
the new one, so only the right subtree's hash is needed. The last line matters operationally — an operator keeping
a running list of leaf hashes need not retain the entries to answer an auditor.

**APIs demonstrated.** `.ConsistencyProof`, `.ConsistencyProofOfLeafHashes`, `.VerifyConsistency`, `.ComputeRoot`,
`.HashLeaf`.

## Scenario 4 — BlockedInputs

**Intent.** Show the blocked surface, which treats one large input as a sequence of fixed-size leaves rather than a
list of entries — the shape you want for files and blobs. Show that the span, stream, async, and
from-leaf-hashes paths are four routes to *one* answer, and finish with `VerifyBlockInclusion`, the fail-closed
possession check that derives the tree size instead of trusting a claimed one.

**What it does.** Builds a 3,372-byte payload (three whole 1 KiB blocks plus a 300-byte tail) and prints the static
block arithmetic for each block. It computes the commitment five ways — from a span, from a `Stream`, via
`ComputeBlockedAsync`, via `ComputeRootOfBlocksAsync`, and from the leaf hashes alone — and compares every root. It
then publishes the bound root and answers a challenge for block 2, before retrying with a flipped byte and with an
understated length.

**What to expect.** The final block is 300 bytes — short, not padded, which is what makes the block count exact.
All five roots agree. `MerkleBlockComputation` carries the leaf hashes, so paths can be issued later without
re-reading the input, and it exposes the same block arithmetic already bound to its own length and block size. The
challenge is answered, and both cheats fail — the path alone is not the answer, because the block is re-hashed
during verification:

```text
  payload       : 3372 bytes, block size 1024
  BlockCount    : 4 (the final block is short, not padded)
    block 0     : offset     0, length 1024
    block 1     : offset  1024, length 1024
    block 2     : offset  2048, length 1024
    block 3     : offset  3072, length  300
  root          : 9e8b5c0835d2acc889160b1225c5ad8aefc87511bf2a78b48f9d2cc2bc17e1c9
  computation   : InputLength=3372, BlockSize=1024, BlockCount=4, LeafHashes=4
  block 3 via computation: offset 3072, length 300
  streamed root : 9e8b5c08... == in-memory: True
  async root    : 9e8b5c08... == in-memory: True
  root-only     : 9e8b5c08... == in-memory: True
  from leaves   : 9e8b5c08... == in-memory: True
  bound root    : f9e631f9...
  challenge     : block 2 (1024 bytes at offset 2048), path of 2 steps
  answer        : True
  wrong block   : False (one flipped byte)
  wrong length  : False
```

The `Stream` overloads are a single pass: they never hold the whole input, only the O(log n) spine of pending
subtree hashes, so they scale to inputs far larger than memory. `ComputeRootOfBlocks` is the cheaper call for a
writer that will never issue a proof, because it does not retain leaf hashes.

**APIs demonstrated.** `MerkleTree.BlockCount` / `.BlockOffset` / `.BlockLength` (static),
`.ComputeBlocked(ReadOnlySpan<byte>, int, MerkleTreeDiagnostics?)`,
`.ComputeBlocked(Stream, int, MerkleTreeDiagnostics?, CancellationToken)`, `.ComputeBlockedAsync`,
`.ComputeRootOfBlocksAsync`, `.ComputeRootOfLeafHashes`, `.BindRoot`, `.VerifyBlockInclusion`,
`MerkleBlockComputation.Root` / `.InputLength` / `.BlockSize` / `.BlockCount` / `.LeafHashes` / `.BlockOffset` /
`.BlockLength`.

## Scenario 5 — StreamingWriter

**Intent.** Show `MerkleBlockAccumulator`, the push-style writer-side counterpart to Scenario 4's pull-style API. A
writer that already streams bytes to storage and feeds them to an incremental digest can give the Merkle root the
same shape — no second pass over the input. The load-bearing guarantee is that the root does not depend on how the
input was split across calls.

**What it does.** Computes the reference root with `ComputeRootOfBlocks`, then accumulates the same 1,371-byte
payload under six different `Append` chunk patterns — one call, one byte at a time, exactly one block, 100-byte,
1,000-byte, and an irregular 7/500/13 cycle — and compares each result. It then shows `Finish` being idempotent,
`Append` after a finish throwing, `Reset` clearing state, the empty input, `FinishBound`, and finally
`retainLeafHashes: true` with `FinishComputation` to issue an authentication path.

**What to expect.** All six chunk patterns land on the same root, with the same `Length` and `LeafCount`, because
`Append` re-blocks internally. Memory is one block plus one pending hash per level — logarithmic in the leaf count
— so this works on inputs far larger than memory:

```text
  payload       : 1371 bytes, block size 256
  ComputeRootOfBlocks: b03aee969161fc268a835508a5b9e3d181e37dd0169f2373fb4113dee8b706f3
  Append patterns (all must agree):
    one call              : b03aee96... agrees  (Length=1371, LeafCount=6)
    1 byte at a time      : b03aee96... agrees  (Length=1371, LeafCount=6)
    exactly one block     : b03aee96... agrees  (Length=1371, LeafCount=6)
    100-byte chunks       : b03aee96... agrees  (Length=1371, LeafCount=6)
    1000-byte chunks      : b03aee96... agrees  (Length=1371, LeafCount=6)
    irregular 7/500/13    : b03aee96... agrees  (Length=1371, LeafCount=6)
  Finish twice  : same root: True, IsFinished=True
  Append after Finish: InvalidOperationException (as documented)
  after Reset   : Length=0, LeafCount=0, IsFinished=False
  empty input   : e3b0c442... == ComputeRoot([]): True
  FinishBound   : fe3eafb5... == BindRoot(root, 1371): True
  RetainsLeafHashes: True -> LeafHashes=6
  challenge block 3: True (answered from a write-time commitment)
```

`Finish` is idempotent, so a writer can ask for the root defensively; `Reset` lets one accumulator serve a sequence
of objects. An empty input folds to the empty tree's root rather than failing, so a zero-byte object needs no
special casing. `FinishBound` is `BindRoot` applied to `Finish` and `Length`, so the writer never has to track the
length itself. Leaf hashes are discarded as they are folded unless retained — that is what keeps memory
logarithmic — and `FinishComputation` hands back the same `MerkleBlockComputation` the pull-style API produces.

**APIs demonstrated.** `.CreateBlockAccumulator(int, bool, MerkleTreeDiagnostics?)`,
`MerkleBlockAccumulator.Append` / `.Finish` / `.FinishBound` / `.FinishComputation` / `.Reset` / `.Dispose` /
`.Length` / `.LeafCount` / `.IsFinished` / `.RetainsLeafHashes`, `.ComputeRootOfBlocks`, `.BindRoot`,
`.VerifyBlockInclusion`.

## Scenario 6 — ShapeAndParallelism

**Intent.** Distinguish the two constructor knobs, because they differ in kind. `maxDegreeOfParallelism` is an
optimisation that must never change the answer; `fanOut` changes the tree's shape, so it changes the root — and
takes the proof surface away with it.

**What it does.** Computes the seven-entry root and a 64 KiB blocked root at degrees 1, 2, 4 and −1, comparing each
against the sequential root. It then computes the same entries at fan-outs 2, 3, 4 and 8, calls
`AuthenticationPath` and `ConsistencyProof` on a fan-out-4 tree, and finally combines a wide fan-out with
parallelism.

**What to expect.** Every degree of parallelism agrees: leaves are independent, so they can be hashed in parallel
while the fold and any observer stay on the caller thread and in order (1 is sequential, −1 unbounded, ≥ 2 a bounded
worker count). Each fan-out is a genuinely different tree with a different root. Because RFC 6962's proof formats
are defined only for a binary tree, the proof members on a wider tree throw `NotSupportedException` rather than
silently inventing a non-interoperable proof — while root computation still works, which is the legitimate use of a
wide tree:

```text
  MaxDegreeOfParallelism (must all agree):
    degree  1     : entries e8da82b2... agrees, 64 KiB blocked c30ea88b...
    degree  2     : entries e8da82b2... agrees, 64 KiB blocked c30ea88b...
    degree  4     : entries e8da82b2... agrees, 64 KiB blocked c30ea88b...
    degree -1     : entries e8da82b2... agrees, 64 KiB blocked c30ea88b...
  FanOut (each shape is a different tree):
    fan-out 2     : e8da82b2... (IsBinary=True)
    fan-out 3     : 2e58d63a... (IsBinary=False)
    fan-out 4     : 0f918cfa... (IsBinary=False)
    fan-out 8     : 2ede0c36... (IsBinary=False)
  wide tree root         : 0f918cfa... (computing a root is fine)
  wide AuthenticationPath: NotSupportedException (proofs are RFC 6962 binary only)
  wide ConsistencyProof  : NotSupportedException (proofs are RFC 6962 binary only)
  fan-out 4 + degree 2   : 0f918cfa... == sequential fan-out 4: True
```

Note the fan-out-2 root is the same `e8da82b2...` every other scenario publishes — the default *is* RFC 6962's
tree.

**APIs demonstrated.** `MerkleTree(Func<HashAlgorithm>, int fanOut, int maxDegreeOfParallelism)`, `.FanOut`,
`.MaxDegreeOfParallelism`, `.IsBinary`, `.ComputeRoot`, `.ComputeRootOfBlocks`, and the `NotSupportedException`
contract on `.AuthenticationPath` / `.ConsistencyProof`.

## Scenario 7 — DiagnosticsTrace

**Intent.** Show `MerkleTreeDiagnostics`, the optional trace recorder. A root value alone cannot show that the fold
wired the right children to the right parents — any bug producing a self-consistent wrong tree still produces one
root — so the trace exists to make the intermediate structure inspectable and independently checkable.

**What it does.** Records a deliberately small four-entry tree, walks it level by level, reads the `Root` node and
confirms its hash is the value the computation returned, runs `Validate`, re-runs `Validate` with the *wrong*
algorithm, and renders the whole trace with `WriteTo`.

**What to expect.** Four leaves fold to two internal nodes and one root: 7 nodes over 3 levels. Both `GetLevel` and
`GetAllNodes` sort their results, so a trace reads the same on every run even though recording is thread-safe and a
parallel computation may record out of order. `Validate` re-computes each internal node from its recorded children;
re-deriving with SHA-512 makes all three internal nodes fail, which is what shows the check is real rather than a
method that returns `true` unconditionally:

```text
  entries       : 4
  root          : b41a66fe...
  levels        : 3 (level 0 is the leaves)
  nodes         : 7
    level 0 (leaf): [0] 720ea60b..., [1] dead1c28..., [2] 56439159..., [3] 08128994...
    level 1 (node): [0] b8cd573e..., [1] 3e956cfc...
    level 2 (node): [0] b41a66fe...
  Root node     : level 2, index 0, 2 children
  matches return: True
  Validate      : True (0 errors)
  Validate(SHA512): False (3 mismatch(es) - the nodes were folded with SHA-256)
```

`WriteTo` then renders the full trace. Its real output carries every node's full 64-character hash plus its child
hashes on one line, so the excerpt below **elides the child columns** (`←  …`) for width — run the sample to see
them:

```text
  WriteTo(Console.Out):
    ════════════════════════════════════════════════════════════════════
      Merkle Tree Diagnostic
      Levels: 3    Nodes: 7    Root: B41A66FE…234BAA
    ════════════════════════════════════════════════════════════════════
      Level 0  —  4 leaf nodes
      ────────────────────────────────────────────────────────────────────
        [0:0]  720EA60B…0CBEB9
        …
      Level 1  —  2 internal nodes
      ────────────────────────────────────────────────────────────────────
        [1:0]  B8CD573E…23CDC7  ←  …
      Level 2  —  1 internal node  ★  root
      ────────────────────────────────────────────────────────────────────
        [2:0]  B41A66FE…234BAA  ←  …
    ════════════════════════════════════════════════════════════════════
      Validation: PASS  (3 internal nodes verified)
    ════════════════════════════════════════════════════════════════════
```

One caveat worth knowing: leaf hashes are *not* re-validated against the original bytes, because the raw blocks are
not retained. A trace therefore proves the fold's internal consistency, not that the leaves were hashed from the
input you think. Recording also costs memory proportional to the node count, so it is a diagnostic aid rather than
something to leave enabled in production.

**APIs demonstrated.** `MerkleTreeDiagnostics()`, `.GetLevelCount`, `.GetLevel`, `.GetAllNodes`, `.Root`,
`.Validate`, `.WriteTo`, `MerkleTreeDiagnostics.Node` (`.Level` / `.Index` / `.IsLeaf` / `.Hash` /
`.ChildHashes`), and the `diagnostics` parameter on `.ComputeRoot`.

## Layout

```text
Bodu.Security.Cryptography.Samples.MerkleTrees/
  Program.cs                          # runs the scenarios in order
  Hex.cs                              # lowercase-hex formatting, full and abbreviated
  SampleLog.cs                        # the fixed corpus and the shape conversions the API needs
  Scenarios/Commitments.cs
  Scenarios/SizeBinding.cs
  Scenarios/Consistency.cs
  Scenarios/BlockedInputs.cs
  Scenarios/StreamingWriter.cs
  Scenarios/ShapeAndParallelism.cs
  Scenarios/DiagnosticsTrace.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` — the hash, MAC, XOF and KDF surface these trees are built
  on, including the `SHA256.Create`-style factories `MerkleTree` takes.
- `Bodu.Security.Cryptography.Samples.AsymmetricKeys` — signing a published root is the natural next step; X25519,
  Ed25519, ML-KEM and ML-DSA live there.
- `Bodu.Collections.Samples.BitSets` — the RFC 6962 tree formerly lived in `Bodu.Collections.Specialized`; that
  namespace now holds `BitSet` alone, and a commitment consumer takes this package instead.
