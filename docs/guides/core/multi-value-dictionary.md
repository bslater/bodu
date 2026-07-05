---
title: Multi-value dictionary
---

# Multi-value dictionary

<xref:Bodu.Collections.Generic.MultiValueDictionary`2> maps each key to an ordered *list* of values — a one-to-many dictionary (a multimap). It removes the boilerplate of the common `Dictionary<TKey, List<TValue>>` pattern: there is no "create the list if the key is missing" dance, the indexer never returns `null`, and value insertion order is preserved per key.

Keys are compared with an `IEqualityComparer<TKey>` supplied at construction.

## Pattern 1 — group values under keys

```csharp
using Bodu.Collections.Generic;

var byCorrelation = new MultiValueDictionary<string, LogEntry>();

foreach (LogEntry entry in entries)
    byCorrelation.Add(entry.CorrelationId, entry);   // no pre-check needed

IReadOnlyList<LogEntry> forId = byCorrelation["abc-123"];   // never null
```

The indexer returns an empty, read-only view when the key is absent — it never throws and never returns `null`, so callers can iterate unconditionally:

```csharp
foreach (LogEntry e in byCorrelation["missing-key"])   // safe — empty sequence
    Process(e);
```

## Pattern 2 — add many values at once

```csharp
var routes = new MultiValueDictionary<string, string>();

routes.AddRange("GET", new[] { "/users", "/orders", "/health" });
routes.Add("POST", "/users");

IReadOnlyList<string> gets = routes.GetValues("GET");   // /users, /orders, /health
```

`GetValues` is equivalent to the indexer; `TryGetValues` returns `false` (and an empty list) when the key is absent, mirroring `Dictionary.TryGetValue`:

```csharp
if (routes.TryGetValues("DELETE", out IReadOnlyList<string> deletes))
    Console.WriteLine(deletes.Count);
```

## Pattern 3 — removing values and keys

```csharp
bool removedOne = routes.Remove("GET", "/health");  // removes a single value
bool removedKey = routes.RemoveAll("POST");         // removes the key and all its values
```

`Remove(key, value)` removes one matching value and returns `false` when the pair is absent; `RemoveAll(key)` drops the key entirely.

## Pattern 4 — membership and counts

```csharp
bool hasKey   = routes.ContainsKey("GET");                 // true
bool hasPair  = routes.ContainsValue("GET", "/users");     // true

int totalValues  = routes.Count;       // total values across all keys
int distinctKeys = routes.KeyCount;    // number of keys
```

Note the distinction: `Count` is the total number of *values* stored, while `KeyCount` is the number of *keys*.

## Pattern 5 — enumerating

Enumerating the dictionary yields each key paired with its value list:

```csharp
foreach (KeyValuePair<string, IReadOnlyList<string>> group in routes)
    Console.WriteLine($"{group.Key}: {group.Value.Count} routes");
```

To iterate the flat sequence of `(key, value)` pairs — one row per value — use `Flatten`:

```csharp
foreach (KeyValuePair<string, string> pair in routes.Flatten())
    Console.WriteLine($"{pair.Key} → {pair.Value}");
```

The `Keys` collection exposes the distinct keys; `Values` and `ReadOnlyValues` expose the flattened values.

## Pattern 6 — custom key comparison

```csharp
var headers = new MultiValueDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
headers.Add("Accept", "text/html");
headers.Add("accept", "application/json");
IReadOnlyList<string> accept = headers["ACCEPT"];   // both values, case-insensitive key
```

## Choosing a backing: List vs Set

By default the dictionary is a *list* multimap (Guava's `ListMultimap`): every added value is retained, including per-key duplicates. Passing `MultiValueBacking.Set` at construction switches it to an order-preserving *set* multimap (Guava's `SetMultimap`): values are deduplicated per key using an optional `IEqualityComparer<TValue>`, and each value keeps the position of its **first** occurrence.

```csharp
var tags = new MultiValueDictionary<string, string>(
    MultiValueBacking.Set,
    StringComparer.OrdinalIgnoreCase,    // key comparer
    StringComparer.OrdinalIgnoreCase);   // value comparer — drives deduplication

tags.Add("doc-1", "Draft");
tags.Add("doc-1", "DRAFT");     // duplicate per the value comparer — ignored
tags.Add("doc-1", "review");

Console.WriteLine(tags.Count);        // 2
Console.WriteLine(tags["doc-1"][0]);  // "Draft" — first occurrence and its casing win
Console.WriteLine(tags.Backing);      // Set
```

Under `Set` backing the value comparer also drives `Remove(key, value)` and `ContainsValue(key, value)` — `tags.Remove("doc-1", "draft")` removes `"Draft"`. Suppressed duplicate adds are not structural changes, so they do not invalidate active enumerators. Under `List` backing the value comparer is stored and reported by `ValueComparer` but not consulted; value operations use default equality, exactly as before the option existed.

The backing and value comparer are fixed at construction. Ordering guarantee: per-key values are always exposed in insertion order (first-occurrence order under `Set`), through the same `IReadOnlyList<TValue>` views as `List` backing. The trade-off is that each `Set`-backed add scans the key's existing values linearly to detect duplicates — the cost that keeps the values ordered and the list-typed view contract intact, and a good fit for the typically small per-key buckets of a multimap.

## API summary

| Member | Description |
|---|---|
| `Add(TKey, TValue)` | Appends a value to the key's list, creating the key if needed. |
| `AddRange(TKey, IEnumerable<TValue>)` | Appends several values to a key. |
| `this[TKey]` / `GetValues(TKey)` | Returns the key's values as an `IReadOnlyList<TValue>` (empty, never `null`). |
| `TryGetValues(TKey, out IReadOnlyList<TValue>)` | Non-throwing lookup. |
| `Remove(TKey, TValue)` | Removes a single value; returns `false` if absent. |
| `RemoveAll(TKey)` | Removes a key and all its values. |
| `ContainsKey(TKey)` / `ContainsValue(TKey, TValue)` | Membership tests. |
| `Flatten()` | Enumerates `(key, value)` pairs, one per value. |
| `Keys` / `Values` / `ReadOnlyValues` | The distinct keys and the flattened values. |
| `Count` | Total number of values across all keys. |
| `KeyCount` | Number of distinct keys. |
| `Comparer` | The active key `IEqualityComparer<TKey>`. |
| `Backing` | The `MultiValueBacking` (List or Set) selected at construction. |
| `ValueComparer` | The value `IEqualityComparer<TValue>`; consulted only under Set backing. |
| `Clear()` | Removes all keys and values. |

## Where to go next

- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Multiset](multiset.md) — when you need value *counts* rather than value *lists*.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
