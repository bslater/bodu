---
title: Trie (prefix tree)
---

# Trie (prefix tree)

A *trie* (prefix tree) stores string keys as paths of characters: shared prefixes share a path, and each key ends at a terminal node. Because lookups walk the supplied string one character at a time, membership and prefix queries cost time proportional to the **length of the key**, not the number of keys stored — the natural shape for autocomplete, command routing, and any lookup where a prefix narrows the candidates.

`Bodu.Collections.Generic.Trees` ships two tries:

| Type | Stores | Use when |
|---|---|---|
| <xref:Bodu.Collections.Generic.Trees.Trie> | a set of string keys | You only need membership and prefix tests. |
| <xref:Bodu.Collections.Generic.Trees.Trie`1> | string keys mapped to values | You need a value alongside each key (an autocomplete payload, a route handler). |

Both accept an `IEqualityComparer<char>` at construction (pass `null` for the ordinal default, or a case-insensitive comparer to fold case), treat the empty string as a valid key, and are **not** thread-safe for concurrent mutation. Their enumerators are fail-fast: mutating the trie after an enumerator is created throws `InvalidOperationException` on the next move.

## Pattern 1 — A set of words: membership and prefixes

```csharp
using Bodu.Collections.Generic.Trees;

var words = new Trie();
words.Add("car");
words.Add("card");
words.Add("care");
words.Add("dog");

// Membership is exact.
bool hasCard = words.Contains("card");   // true
bool hasCa   = words.Contains("ca");     // false — "ca" is a prefix, not a stored key

// StartsWith asks whether *any* key begins with the prefix.
bool anyCar = words.StartsWith("car");   // true
bool anyXyz = words.StartsWith("xyz");   // false

int count = words.Count;                 // 4
```

`Add` returns `false` when the key already exists, so it doubles as a "first insert" test:

```csharp
bool added = words.Add("car");   // false — already present
```

Build directly from a sequence (an exception is thrown on a duplicate key):

```csharp
var fromSeq = new Trie(new[] { "alpha", "beta", "gamma" });
```

Pass an `IEqualityComparer<char>` to fold case while matching, so `"Hello"` and `"hello"` resolve to one key:

```csharp
sealed class CaseInsensitiveChar : IEqualityComparer<char>
{
    public bool Equals(char x, char y) =>
        char.ToUpperInvariant(x) == char.ToUpperInvariant(y);

    public int GetHashCode(char c) =>
        char.ToUpperInvariant(c).GetHashCode();
}

var folded = new Trie(new CaseInsensitiveChar());
folded.Add("Hello");
bool match = folded.Contains("HELLO");   // true
```

> [!NOTE]
> The constructor takes an `IEqualityComparer<char>`, not a `StringComparer` — comparison is per character. Leave it `null` for ordinal matching.

## Pattern 2 — A string-keyed map with prefix queries (autocomplete)

<xref:Bodu.Collections.Generic.Trees.Trie`1> associates a value with each key. `KeysWithPrefix` lists the matching keys; `ItemsWithPrefix` lists the matching key/value pairs — the building block of an autocomplete dropdown.

```csharp
using Bodu.Collections.Generic.Trees;

var commands = new Trie<string>();
commands.Add("commit", "Record changes to the repository");
commands.Add("checkout", "Switch branches or restore files");
commands.Add("clone", "Clone a repository into a new directory");
commands.Add("push", "Update remote refs");

// Suggest everything the user could mean after typing "c".
foreach (KeyValuePair<string, string> item in commands.ItemsWithPrefix("c"))
{
    Console.WriteLine($"{item.Key}\t{item.Value}");
    // commit / checkout / clone (order unspecified)
}

// Just the matching keys.
foreach (string key in commands.KeysWithPrefix("ch"))
{
    // "checkout"
}
```

Exact lookups use `TryGetValue` or the indexer:

```csharp
if (commands.TryGetValue("commit", out string? help))
{
    // help == "Record changes to the repository"
}

string text = commands["clone"];   // throws KeyNotFoundException if absent
```

`Add` throws if the key already exists; `TryAdd` reports the collision instead; `Set` (and the indexer setter) inserts or overwrites:

```csharp
bool inserted = commands.TryAdd("commit", "...");   // false — already present
commands.Set("commit", "Record changes (updated)"); // overwrites in place
commands["push"] = "Update remote refs and objects"; // overwrites via the indexer
```

## Pattern 3 — Removal

Both tries remove by key and report whether the key was present:

```csharp
var words = new Trie(new[] { "car", "card", "care" });

bool removed = words.Remove("card");   // true
bool again   = words.Remove("card");   // false — already gone

words.Clear();                         // empties the trie; Count == 0
```

Removing a key leaves sibling keys that share its prefix intact — deleting `"card"` above keeps `"car"` and `"care"`.

## Pattern 4 — Enumeration

Enumerating a trie yields its keys (for <xref:Bodu.Collections.Generic.Trees.Trie>) or its key/value pairs (for <xref:Bodu.Collections.Generic.Trees.Trie`1>), in unspecified order, over a snapshot taken when enumeration begins.

```csharp
var map = new Trie<int>();
map.Add("apple", 1);
map.Add("apply", 2);
map.Add("banana", 3);

foreach (KeyValuePair<string, int> entry in map)
{
    // ("apple", 1), ("apply", 2), ("banana", 3) — order unspecified
}
```

The enumerator is fail-fast — mutating the trie inside the loop throws:

```csharp
foreach (KeyValuePair<string, int> entry in map)
{
    map.Add("cherry", 4);   // throws InvalidOperationException on the next iteration
}
```

To remove while iterating, materialize the keys first:

```csharp
foreach (string key in map.KeysWithPrefix("app").ToList())
    map.Remove(key);
```

## Where to go next

- <xref:Bodu.Collections.Generic.Trees> — the full API surface for the trie and tree types.
- [Choosing a collection](choosing-a-collection.md) — when a trie beats a `Dictionary` or sorted set.
- [Evicting dictionary](evicting-dictionary.md) — a bounded-capacity map, when prefix matching is not the concern.
- [Core foundations](../topics/core-foundations.md) — the wider `Bodu.Core` toolbox.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for `Bodu.Core`.
