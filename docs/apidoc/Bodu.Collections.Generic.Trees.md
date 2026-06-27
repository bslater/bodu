---
uid: Bodu.Collections.Generic.Trees
---

![Bodu.Collections.Generic.Trees](~/images/hero-core.svg)

## Purpose

**Bodu.Collections.Generic.Trees** provides tree-shaped collections for `Bodu.Core`. Its headline types are the prefix trees (tries): <xref:Bodu.Collections.Generic.Trees.Trie> stores a set of string keys, and <xref:Bodu.Collections.Generic.Trees.Trie`1> maps string keys to values. Both keep keys as paths of characters, so membership and prefix queries cost time proportional to the length of the key rather than to the number of stored keys — the natural fit for autocomplete, routing tables, and dictionary lookups where a prefix narrows the search.

Alongside the tries, <xref:Bodu.Collections.Generic.Trees.Tree`1> is a mutable n-ary tree node: each instance is both a value-carrying node and the root of the subtree formed by its descendants, with iterative (stack-safe) pre-order, post-order, and level-order traversals.

## Static documentation

- **[Introduction](~/docs/core/index.md)** — where the tree collections sit in the wider `Bodu.Core` surface.
- **[Trie (prefix tree)](~/guides/core/trie.md)** — building a string set or string-keyed map, prefix queries, removal, and enumeration.

## Key types

- <xref:Bodu.Collections.Generic.Trees.Trie> — a set of string keys. `Add` / `Contains` / `Remove`, the prefix members `StartsWith` and `KeysWithPrefix`, `Count`, `Clear`, a `Comparer`, and a fail-fast struct `GetEnumerator`. `Contains` and `StartsWith` also accept a `ReadOnlySpan<char>`.
- <xref:Bodu.Collections.Generic.Trees.Trie`1> — a string-keyed map. `Add` / `TryAdd` / `Set` and the `this[string]` indexer, `ContainsKey`, `TryGetValue`, `Remove`, the prefix members `StartsWith`, `KeysWithPrefix`, and `ItemsWithPrefix`, `Count`, `Clear`, and a fail-fast struct `GetEnumerator` over `KeyValuePair<string, TValue>`. Span overloads exist for `Add`, `ContainsKey`, `TryGetValue`, and `StartsWith`.
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
- **Character comparison is configurable.** Both tries accept an `IEqualityComparer<char>` at construction — pass `null` for the ordinal default, or a case-insensitive comparer to fold case while matching. The empty string is a valid key.
- **Enumeration order is unspecified.** `GetEnumerator`, `KeysWithPrefix`, and `ItemsWithPrefix` make no ordering guarantee in this version. The struct enumerator is fail-fast: mutating the trie after the enumerator is created throws <xref:System.InvalidOperationException> on the next `MoveNext` or `Reset`.
- **Not thread-safe for concurrent mutation.** Guard external synchronization if a tree or trie is shared across threads while one of them mutates it.
- **`Tree<T>` traversals are stack-safe.** `PreOrder`, `PostOrder`, and `LevelOrder` are evaluated iteratively, so arbitrarily deep trees are walked without risking a stack overflow.
