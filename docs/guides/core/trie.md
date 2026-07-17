---
title: Tries and text search
---

# Tries and text search

A *trie* (prefix tree) stores string keys as paths of characters: shared prefixes share a path, and each key ends at a terminal node. Because lookups walk the supplied string one character at a time, membership and prefix queries cost time proportional to the **length of the key**, not the number of keys stored — the natural shape for autocomplete, command routing, and any lookup where a prefix narrows the candidates.

`Bodu.Collections.Generic.Trees` ships the trie family:

| Type | Stores | Use when |
|---|---|---|
| <xref:Bodu.Collections.Generic.Trees.Trie> | a set of string keys | You only need membership and prefix tests. |
| <xref:Bodu.Collections.Generic.Trees.Trie`1> | string keys mapped to values | You need a value alongside each key (an autocomplete payload, a route handler). |
| <xref:Bodu.Collections.Generic.Trees.RadixTrie> / <xref:Bodu.Collections.Generic.Trees.RadixTrie`1> | the same, over compressed edges | Keys share long unbranching runs (URLs, file paths, identifiers) — same API, far fewer nodes. See [Radix trie](#radix-trie-path-compressed-lookups). |
| <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton> / <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton`1> | a fixed pattern set | You search *text* for every occurrence of many patterns at once. See [Aho-Corasick](#aho-corasick-multi-pattern-text-search). |

The four trie collections accept an `IEqualityComparer<char>` at construction (pass `null` for the ordinal default, or a case-insensitive comparer to fold case), treat the empty string as a valid key, and are **not** thread-safe for concurrent mutation. Their enumerators are fail-fast: mutating the trie after an enumerator is created throws `InvalidOperationException` on the next move. The Aho-Corasick automatons are different animals — immutable after a one-shot `Build`, ordinal-only, and organized around scanning text rather than storing keys.

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

Enumerating a trie yields its keys (for <xref:Bodu.Collections.Generic.Trees.Trie>) or its key/value pairs (for <xref:Bodu.Collections.Generic.Trees.Trie`1>), in unspecified order. Enumeration is lazy — elements are produced on demand as the trie is walked, with no up-front snapshot — so breaking out of a `foreach` early does no more work than the elements consumed.

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

## Radix trie (path-compressed lookups)

<xref:Bodu.Collections.Generic.Trees.RadixTrie> and <xref:Bodu.Collections.Generic.Trees.RadixTrie`1> are PATRICIA-style path-compressed tries: instead of one node per character, each edge carries a **string label**, so an unbranching run of characters costs a single node. Inserting a key that diverges partway along an edge splits the edge; removing a key re-fuses any single-child pass-through node left behind. Node count is proportional to the number of keys, not their total length.

The public surface mirrors `Trie` / `Trie<TValue>` **member-for-member** — constructors (including the `IEqualityComparer<char>` overload), `Add` / `TryAdd` / `Set` / the indexer, `Contains` / `ContainsKey` / `TryGetValue`, `StartsWith`, `Remove`, `KeysWithPrefix` / `ItemsWithPrefix`, `Clear`, `Count`, `Comparer`, and the fail-fast enumerator — so swapping types is a one-word change:

```csharp
using Bodu.Collections.Generic.Trees;

var routes = new RadixTrie<string>();
routes.Add("/api/orders", "OrdersHandler");
routes.Add("/api/orders/archive", "ArchiveHandler");
routes.Add("/api/customers", "CustomersHandler");

// Identical answers to a Trie<string> holding the same keys:
bool known = routes.ContainsKey("/api/orders");            // true
bool hasApi = routes.StartsWith("/api/");                  // true

foreach (KeyValuePair<string, string> route in routes.ItemsWithPrefix("/api/orders"))
{
    // /api/orders, /api/orders/archive (order unspecified)
}
```

**When to prefer it over `Trie`:** long keys with sparse branching — URLs, file-system paths, namespaced identifiers, MAC/OUI prefixes. A plain `Trie` allocates a node per character of every distinct suffix; the radix trie collapses each unbranching run into one node, cutting memory and pointer-chasing. For short, densely branching keys (words in a spell-checker) the two perform similarly — pick either; behaviour is identical.

## Aho-Corasick (multi-pattern text search)

The tries above answer questions about **stored keys**. <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton> answers the inverse question: given a fixed set of patterns, find **every occurrence of every pattern inside a text** — including overlapping and nested occurrences — in a single pass, O(text + matches) regardless of how many patterns are loaded. It is the classic engine for keyword filtering, content scanning, and dictionary-based tokenization.

The automaton is built once from the complete pattern set and is **immutable** afterwards (its internal failure links are global invariants of the whole set — adding a pattern later would invalidate them wholesale; rebuild instead). Matching is ordinal; normalize case up front if you need folded matching.

```csharp
using Bodu.Collections.Generic.Trees;

var automaton = AhoCorasickAutomaton.Build(["he", "she", "his", "hers"]);

foreach (AhoCorasickMatch match in automaton.EnumerateMatches("ushers"))
{
    Console.WriteLine($"{match.Pattern} @ {match.Start}..{match.End}");
    // he @ 2..4
    // she @ 1..4
    // hers @ 2..6
}

int total = automaton.CountMatches("ushers");   // 3 — span-friendly, eager
bool any  = automaton.HasMatch("ushers");       // true — exits at the first hit
```

Matches are reported in a **pinned deterministic order**: ascending end index, then ascending pattern length. `EnumerateMatches(string)` is lazy; because a lazy sequence cannot capture a `ReadOnlySpan<char>`, the span-friendly `CountMatches` and `HasMatch` are eager conveniences that run the same scan without materializing matches.

<xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton`1> attaches a value to each pattern and surfaces it on every match — the natural shape for "which rule fired, and what should I do about it":

```csharp
var rules = AhoCorasickAutomaton<int>.Build(
[
    new KeyValuePair<string, int>("error", 2),
    new KeyValuePair<string, int>("warn", 1),
]);

int severity = 0;
foreach (AhoCorasickMatch<int> match in rules.EnumerateMatches("warn: retry failed with error"))
{
    severity = Math.Max(severity, match.Value);   // ends at 2 — "error" fired
}
```

Contracts to know: `Build` rejects a null or empty pattern collection and null or empty individual patterns (an empty pattern would match everywhere and carries no information). The unkeyed automaton **deduplicates** repeated patterns; the keyed automaton **throws** on a duplicate pattern key — mirroring the `Trie` versus `Trie<TValue>` add contracts.

**When to prefer it over a trie or `string.Contains`:** you have *many* patterns and *long or streaming-ish* texts. Scanning with per-pattern `IndexOf` costs O(text × patterns); the automaton pays O(text) once for the whole set. For a handful of patterns over short strings, `string.Contains` is simpler and fine.

## Where to go next

- <xref:Bodu.Collections.Generic.Trees> — the full API surface for the trie, radix-trie, automaton, and tree types.
- [Choosing a collection](choosing-a-collection.md) — when a trie beats a `Dictionary` or sorted set.
- [Evicting dictionary](evicting-dictionary.md) — a bounded-capacity map, when prefix matching is not the concern.
- [Core foundations](../topics/core-foundations.md) — the wider `Bodu.Core` toolbox.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for `Bodu.Core`.
