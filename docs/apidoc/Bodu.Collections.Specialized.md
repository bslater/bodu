---
uid: Bodu.Collections.Specialized
---

![Bodu.Collections.Specialized](~/images/hero-collections.svg)

## Purpose

**Bodu.Collections.Specialized** holds the members of the **`Bodu.Collections`** package that serve a specialised purpose rather than acting as general-purpose containers. One type lives here today: <xref:Bodu.Collections.Specialized.BitSet>, a packed bit set over dense non-negative integers.

What sets it apart is independence. It references nothing else in the package, and nothing else in the package references it — unlike every other non-generic type in <xref:Bodu.Collections.Generic>, which is either a collection in its own right or a policy or option satellite of one. Separating it keeps the general catalogue cohesive without fragmenting the package: `Bodu.Collections` still ships everything, and reaching for it costs a second `using`.

The namespace follows [`System.Collections.Specialized`](https://learn.microsoft.com/dotnet/api/system.collections.specialized) in intent — structures tuned to a narrow job rather than a general shape — though not in content: nothing here is a string-keyed dictionary.

The RFC 6962 Merkle tree that formerly shared this namespace now ships as <xref:Bodu.Security.Cryptography.MerkleTree> in the `Bodu.Security.Cryptography` package — see the [Merkle trees and proofs guide](~/guides/cryptography/merkle-trees.md).

## Static documentation

- **[Bit set](~/guides/core/bit-set.md)** — dense integer membership as packed bits, and when to prefer it over the BCL `BitArray`.
- **[Bodu.Collections introduction](~/docs/collections/index.md)** — the package this type ships in, and where it sits among its namespaces.

## Key types

- <xref:Bodu.Collections.Specialized.BitSet> — growable packed bit set with Java `BitSet` semantics (`NextSetBit` / `NextClearBit` / `Cardinality`, in-place `And` / `Or` / `Xor` / `AndNot`). See the [bit set guide](~/guides/core/bit-set.md).

## Example

```csharp
using Bodu.Collections.Specialized;

// Dense integer membership as packed bits — Java BitSet semantics.
var flags = new BitSet();
flags.Set(4);
flags.Set(9);
int first = flags.NextSetBit(0);          // 4
int count = flags.Cardinality;            // 2
```

## Notes

- **`BitSet` grows on write, not on read.** `Set` and `Flip` extend the backing words as needed; a `Get` past the current length returns `false` rather than throwing or growing, so probing a sparse high index costs nothing. Negative indices are rejected.
- **`BitSet` is not thread-safe for mutation.** It requires external synchronization if written concurrently.
- **See also:** the [bit set guide](~/guides/core/bit-set.md) and the [Bodu.Collections introduction](~/docs/collections/index.md) for the full scenario table.
