---
title: Layered and defaulting dictionaries
---

# Layered and defaulting dictionaries

Two small dictionary utilities round out the dictionary family with shapes borrowed from Python's standard library: `LayeredDictionary<TKey, TValue>` is the .NET analogue of `collections.ChainMap`, and `DefaultingDictionary<TKey, TValue>` is the analogue of `collections.defaultdict`.

## Layered dictionary (`ChainMap`)

`LayeredDictionary<TKey, TValue>` is a **live, read-through view** over an ordered list of underlying dictionaries. It copies nothing: lookups search the layers in order and the *first* layer containing the key wins, so an entry in an earlier layer *shadows* any same-keyed entry in later layers. All writes — `Add`, the indexer setter, `Remove`, `Clear` — go to the **first layer only**, exactly matching Python `ChainMap`.

This is the same first-wins precedence model as `Bodu.Text.Configuration`'s resolver chain, where earlier configuration sources take precedence over later ones — a layered dictionary is the general-purpose collection expression of that idea: overrides in front, defaults behind.

```csharp
using Bodu.Collections.Generic;

var overrides = new Dictionary<string, string>();
var defaults = new Dictionary<string, string> { ["colour"] = "blue", ["size"] = "medium" };

var settings = new LayeredDictionary<string, string>(overrides, defaults);

string colour = settings["colour"];   // "blue" — falls through to the defaults layer
settings["colour"] = "red";           // writes to overrides, shadowing the default
colour = settings["colour"];          // "red" — the first layer wins
```

Contract points worth keeping in mind:

- **The layer list is fixed at construction; the layers stay live.** Mutating an underlying dictionary directly is immediately visible through the view. The `Layers` property exposes the list read-only.
- **`Remove` mutates the first layer only.** It returns `false` for a key that exists only in deeper layers — the key stays visible through the view. Removing a first-layer entry that shadowed a deeper one makes the deeper value visible again (the *unshadowing* behaviour):

  ```csharp
  settings.Remove("colour");            // removes "red" from overrides…
  colour = settings["colour"];          // "blue" — the default is unshadowed
  ```

- **`Add` throws only when the first layer already contains the key.** A key that exists solely in deeper layers is addable — the new first-layer entry simply shadows the deeper value.
- **`Clear` clears the first layer only**, so previously shadowed deeper entries become visible again.
- **`Count`, `Keys`, `Values`, and enumeration present the merged view** — distinct keys with first-wins values. They walk every layer, so `Count` is O(n) in the total entries across layers on every call, not a cached property. `Keys` and `Values` are snapshots taken at call time; enumeration is lazy over the live layers.
- **The optional comparer governs only the view's deduplication.** Each underlying dictionary keeps resolving keys with its own comparer, so mismatched comparers can produce surprising shadowing (a case-insensitive first layer answers for casings the view considers distinct). Construct the view and its layers with the same comparer.
- The non-generic `IDictionary` / `ICollection` interfaces are deliberately not implemented — this is a view type, not a stand-alone collection.

## Defaulting dictionary (`defaultdict`)

`DefaultingDictionary<TKey, TValue>` wraps a `Dictionary<TKey, TValue>` and bakes a `Func<TKey, TValue>` value factory into the type: reading a **missing key through the indexer** invokes the factory, **stores** the produced value, and returns it. Exactly as in Python — where `__missing__` fires only for `d[key]` — the indexer getter is the *only* member that materializes defaults: `TryGetValue`, `ContainsKey`, `Remove`, `Count`, and enumeration see only entries that have actually been stored.

```csharp
using Bodu.Collections.Generic;

var groups = new DefaultingDictionary<string, List<int>>(_ => new List<int>());

groups["odd"].Add(1);    // miss — the factory creates the list, it is stored, then mutated
groups["odd"].Add(3);    // hit — the stored list is returned; the factory is not invoked

bool has = groups.ContainsKey("even");   // false — lookups never materialize defaults
```

How it relates to the existing `GetOrAdd` extension in Bodu.Core: `IDictionaryExtensions.GetOrAdd` stays the lightweight per-call-site option — you pass the factory at each call against any `IDictionary<TKey, TValue>`. `DefaultingDictionary<TKey, TValue>` fixes the policy at construction, so every plain indexer read applies it; hand the dictionary to code that only knows `IDictionary<TKey, TValue>` and the defaults still materialize.

Contract points:

- The factory and the comparer are exposed through `ValueFactory` and `Comparer`.
- **Reentrancy is deterministic:** if the factory itself mutates the dictionary — even assigning the very key being materialized — the factory's *return value* is stored last and wins.
- `TValue` is unconstrained: a factory returning `null` stores `null` as-is; the key is then present and the factory is not invoked for it again.

> [!NOTE]
> Neither type is thread-safe. The layered view's merged reads and the defaulting dictionary's check-invoke-store sequence both require external synchronization when shared across threads.

## Where to go next

- <xref:Bodu.Collections.Generic.LayeredDictionary`2> and <xref:Bodu.Collections.Generic.DefaultingDictionary`2> — the full API surfaces.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide across the namespace.
- [Sequenced dictionary](sequenced-dictionary.md), [Bidirectional dictionary](bi-dictionary.md), [Multi-value dictionary](multi-value-dictionary.md) — the rest of the dictionary family.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for the collections packages.
