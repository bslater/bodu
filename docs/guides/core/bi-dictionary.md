---
title: Bidirectional dictionary
---

# Bidirectional dictionary

`BiDictionary<TKey, TValue>` is a bidirectional one-to-one map: every key maps to exactly one value **and** every value maps back to exactly one key. It is the .NET analogue of Guava's `BiMap`, Python's `bidict`, and Apache Commons' `BidiMap` — a hash-based dictionary that maintains a second, inverse index so that looking up a key *by its value* (`TryGetKey`, `ContainsValue`, `RemoveValue`) is O(1) rather than a linear scan.

It fits mappings that are genuinely one-to-one — ISO codes to numbers, identifiers to display names, enum-like wire values to domain values — where you need to translate in both directions. When one key maps to *many* values, reach for [`MultiValueDictionary<TKey, TValue>`](multi-value-dictionary.md) instead.

A few contract points worth keeping in mind:

- Both type parameters are constrained to `notnull` — values act as lookup keys in the inverse index, so `null` values cannot be indexed and are rejected at run time just as `null` keys are.
- Duplicate **keys** follow the standard `Dictionary<TKey, TValue>` contract: `Add` throws, the indexer re-binds.
- Duplicate **values** are a conflict resolved by the `BiDictionaryDuplicateValuePolicy` chosen at construction: `Throw` (the default, Guava `BiMap.put` semantics) rejects the operation; `Replace` (Guava `forcePut`) evicts the previous binding — the key that held the value is removed.
- Both comparers are injectable: `KeyComparer` governs the forward index, `ValueComparer` the inverse index.
- Enumeration order is unspecified (the same non-contractual, insertion-biased order as `Dictionary<TKey, TValue>`); do not rely on it.

## Pattern 1 — translate in both directions

```csharp
using Bodu.Collections.Generic;

var codes = new BiDictionary<string, int>();
codes.Add("AU", 36);
codes.Add("NZ", 554);
codes.Add("BR", 76);

int numeric = codes["AU"];                    // 36 — forward lookup

codes.TryGetKey(554, out string? alpha);      // "NZ" — O(1) inverse lookup
bool bound = codes.ContainsValue(76);         // true — O(1), no scan
codes.RemoveValue(36);                        // removes the pair ("AU", 36)
```

## Pattern 2 — the live `Inverse` view

`Inverse` exposes the reversed mapping as a `BiDictionary<TValue, TKey>` **view sharing the same storage** — not a copy. Mutations through either view are immediately visible through the other, and `Inverse.Inverse` returns the original instance (reference-equal):

```csharp
using Bodu.Collections.Generic;

var codes = new BiDictionary<string, int>();
codes.Add("AU", 36);

BiDictionary<int, string> byNumber = codes.Inverse;
byNumber.Add(554, "NZ");                      // mutate through the view…

bool present = codes.ContainsKey("NZ");       // …visible through the original
object same = codes.Inverse.Inverse;          // reference-equal to codes
```

Through the inverse view, keys and values swap roles — the view's `KeyComparer` is the original's `ValueComparer` and vice versa — and the shared duplicate-value policy governs conflicts on the value side of whichever view is being mutated. The invariant is the same in both directions: the mapping stays one-to-one.

## Pattern 3 — choosing the duplicate-value policy

```csharp
using Bodu.Collections.Generic;

// Throw (default): a value bound to a different key is rejected.
var strict = new BiDictionary<string, int>();
strict.Add("a", 1);
// strict.Add("b", 1);      // throws ArgumentException — 1 is bound to "a"
strict.TryAdd("b", 1);      // returns false; nothing changes

// Replace: the previous binding holding the value is evicted.
var forced = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);
forced.Add("a", 1);
forced.Add("b", 1);         // evicts ("a", 1); "a" is gone
bool hasA = forced.ContainsKey("a");   // false
```

The indexer setter follows the same policy for value conflicts. Re-binding an existing key through the indexer always cleans up the old value's inverse entry, and re-assigning a key its own current value is never a conflict.

> [!NOTE]
> `BiDictionary<TKey, TValue>` is not thread-safe. Concurrent reads and writes — including through the `Inverse` view — require external synchronization. Enumerator invalidation matches the BCL dictionary: adding an entry through either view invalidates active enumerators.

## Where to go next

- <xref:Bodu.Collections.Generic.BiDictionary`2> — the full API surface.
- [Multi-value dictionary](multi-value-dictionary.md) — one key to many values when the mapping is not one-to-one.
- [Sequenced dictionary](sequenced-dictionary.md) — a dictionary with a stable, contractual iteration order.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide across the namespace.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for the collections packages.
