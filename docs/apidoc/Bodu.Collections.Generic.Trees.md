---
uid: Bodu.Collections.Generic.Trees
---

![Bodu.Collections.Generic.Trees](~/images/hero-collections.svg)

## Purpose

**Bodu.Collections.Generic.Trees** provides the tree-shaped collections of the `Bodu.Collections` package (which depends on `Bodu.Core`). Its headline types are the prefix trees (tries): <xref:Bodu.Collections.Generic.Trees.Trie> stores a set of string keys, and <xref:Bodu.Collections.Generic.Trees.Trie`1> maps string keys to values. Both keep keys as paths of characters, so membership and prefix queries cost time proportional to the length of the key rather than to the number of stored keys — the natural fit for autocomplete, routing tables, and dictionary lookups where a prefix narrows the search.

The tries have two sibling families. <xref:Bodu.Collections.Generic.Trees.RadixTrie> and <xref:Bodu.Collections.Generic.Trees.RadixTrie`1> mirror the trie surfaces member-for-member over path-compressed string edges — the better fit for long keys with sparse branching (URLs, paths, identifiers). <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton> and <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton`1> invert the question: built once from a pattern set, they report every occurrence of every pattern in a searched text in a single O(text + matches) pass.

Alongside the tries, <xref:Bodu.Collections.Generic.Trees.Tree`1> is a mutable n-ary tree node: each instance is both a value-carrying node and the root of the subtree formed by its descendants, with iterative (stack-safe) pre-order, post-order, and level-order traversals.

## Static documentation

- **[Introduction](~/docs/collections/index.md)** — where the tree collections sit in the wider collection catalogue.
- **[Tries and text search](~/guides/core/trie.md)** — building a string set or string-keyed map, prefix queries, removal, enumeration, the radix-trie variants, and Aho-Corasick multi-pattern matching.

## Key types

- <xref:Bodu.Collections.Generic.Trees.Trie> — a set of string keys. `Add` / `Contains` / `Remove`, the prefix members `StartsWith` and `KeysWithPrefix`, `Count`, `Clear`, a `Comparer`, and a fail-fast struct `GetEnumerator`. `Contains` and `StartsWith` also accept a `ReadOnlySpan<char>`.
- <xref:Bodu.Collections.Generic.Trees.Trie`1> — a string-keyed map. `Add` / `TryAdd` / `Set` and the `this[string]` indexer, `ContainsKey`, `TryGetValue`, `Remove`, the prefix members `StartsWith`, `KeysWithPrefix`, and `ItemsWithPrefix`, `Count`, `Clear`, and a fail-fast struct `GetEnumerator` over `KeyValuePair<string, TValue>`. Span overloads exist for `Add`, `ContainsKey`, `TryGetValue`, and `StartsWith`.
- <xref:Bodu.Collections.Generic.Trees.RadixTrie> / <xref:Bodu.Collections.Generic.Trees.RadixTrie`1> — path-compressed (PATRICIA-style) siblings of the two tries with the identical member-for-member public surface, so consumers can swap types without code changes. Edges carry string labels that split on insert and re-fuse on remove.
- <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton> / <xref:Bodu.Collections.Generic.Trees.AhoCorasickAutomaton`1> — immutable multi-pattern matchers created via `Build`. `EnumerateMatches(string)` lazily yields every (overlapping, nested) occurrence as <xref:Bodu.Collections.Generic.Trees.AhoCorasickMatch> / <xref:Bodu.Collections.Generic.Trees.AhoCorasickMatch`1> records in ascending (end index, pattern length) order; the span-based `CountMatches` / `HasMatch` are eager conveniences, and `Patterns` / `ContainsPattern` expose the built set.
- <xref:Bodu.Collections.Generic.Trees.Tree`1> — a mutable n-ary tree node. `Value`, `Parent`, `Children`, `ChildCount`, `IsRoot`, `IsLeaf`, `Depth`, `Height`; structural mutation through `AddChild`, `RemoveChild`, `Remove`, and `Clear`; and the traversals `PreOrder`, `PostOrder`, `LevelOrder`, `Descendants`, `Ancestors`, `Leaves`, and `Root`.

## Example

```csharp
using Bodu.Collections.Generic.Trees;

var words = new Trie();
words.Add("car");
words.Add("card");
words.Add("dog");

bool hasCard = words.Contains("card");        // true
bool anyCar  = words.StartsWith("car");        // true

foreach (string key in words.KeysWithPrefix("car"))
{
    // "car", "card" (order unspecified)
}
```

```csharp
using Bodu.Collections.Generic.Trees;

// String-keyed map with prefix queries (autocomplete-style).
var map = new Trie<int>();
map.Add("apple", 1);
map.Add("apply", 2);

if (map.TryGetValue("apple", out int value))
{
    // value == 1
}

foreach (KeyValuePair<string, int> item in map.ItemsWithPrefix("app"))
{
    // ("apple", 1), ("apply", 2) — order unspecified
}
```

## Notes

- **Prefix cost is key-length, not key-count.** A trie answers `Contains` / `ContainsKey`, `StartsWith`, and the prefix enumerations in time proportional to the length of the supplied string, independent of how many keys are stored.
- **Character comparison is configurable.** The four trie collections accept an `IEqualityComparer<char>` at construction — pass `null` for the ordinal default, or a case-insensitive comparer to fold case while matching. The empty string is a valid key. The Aho-Corasick automatons are ordinal-only: normalize patterns and text up front for folded matching.
- **The automatons are immutable once built.** Their failure and output links are global invariants of the complete pattern set; to change the set, call `Build` again. The unkeyed automaton deduplicates repeated patterns, while the keyed automaton throws on a duplicate pattern key.
- **Enumeration order is unspecified.** `GetEnumerator`, `KeysWithPrefix`, and `ItemsWithPrefix` make no ordering guarantee in this version. The struct enumerator is fail-fast: mutating the trie after the enumerator is created throws <xref:System.InvalidOperationException> on the next `MoveNext` or `Reset`.
- **Not thread-safe for concurrent mutation.** Guard external synchronization if a tree or trie is shared across threads while one of them mutates it.
- **`Tree<T>` traversals are stack-safe.** `PreOrder`, `PostOrder`, and `LevelOrder` are evaluated iteratively, so arbitrarily deep trees are walked without risking a stack overflow.
