---
title: Sequenced dictionary
---

# Sequenced dictionary

`SequencedDictionary<TKey, TValue>` is a dictionary that preserves the order in which entries are encountered and gives O(1) access to — and removal of — the first and last entries. It is the .NET analogue of Java's `LinkedHashMap`, and realizes the same *sequenced* (encounter-order) contract: a well-defined first and last entry with constant-time access to each.

It is **unbounded** — it never evicts. For a fixed-capacity cache with a built-in eviction policy, reach for [`EvictingDictionary<TKey, TValue>`](evicting-dictionary.md) instead.

## Ordering modes

The ordering mode is chosen at construction and is fixed for the lifetime of the instance:

| Mode | Constructor | Reads reorder? | First / Last |
|---|---|---|---|
| Insertion order *(default)* | `new SequencedDictionary<K,V>()` | No | Oldest / newest **inserted** entry. |
| Access order | `new SequencedDictionary<K,V>(accessOrder: true)` | Yes — a read or indexed update moves the entry to the tail. | Least- / most-**recently-used** entry. |

In access-order mode, a successful lookup (`this[key]` getter or `TryGetValue`) and an indexed value update move the affected entry to the end of the iteration order. `ContainsKey`, `TryGetFirst`/`TryGetLast`, and `First`/`Last` are pure reads and never reorder.

## Pattern 1 — insertion-ordered map

```csharp
using Bodu.Collections.Generic;

var headers = new SequencedDictionary<string, string>();
headers.Add("Host", "example.com");
headers.Add("Accept", "application/json");
headers.Add("User-Agent", "bodu/1.0");

// Enumeration always follows insertion order.
foreach (var (name, value) in headers)
    Console.WriteLine($"{name}: {value}");
```

## Pattern 2 — O(1) ends as a queue-like store

```csharp
using Bodu.Collections.Generic;

var pending = new SequencedDictionary<Guid, WorkItem>();
pending.Add(item.Id, item);

// Peek and pop the oldest entry in O(1).
if (pending.TryGetFirst(out var oldest))
    Process(oldest.Value);

pending.TryRemoveFirst(out _);   // dequeue the head
```

## Pattern 3 — least-recently-used cache

Access-order mode makes the least-recently-used entry the `First` entry, so an unbounded LRU with manual trimming is a few lines:

```csharp
using Bodu.Collections.Generic;

var cache = new SequencedDictionary<string, byte[]>(accessOrder: true);

byte[] GetOrLoad(string key)
{
    if (cache.TryGetValue(key, out var data))   // moves key to most-recently-used
        return data;

    data = Load(key);
    cache[key] = data;

    // Trim to a soft cap by evicting the least-recently-used entries.
    while (cache.Count > 100)
        cache.TryRemoveFirst(out _);

    return data;
}
```

## Pattern 4 — assignment semantics

`Add` follows the strict BCL `Dictionary<TKey, TValue>.Add` contract and throws on a duplicate key. The indexer upserts: a new key is appended to the tail, and an existing key's value is updated in place (and, in access-order mode, moved to the tail).

```csharp
using Bodu.Collections.Generic;

var map = new SequencedDictionary<string, int>();
map.Add("a", 1);
map["a"] = 10;          // updates in place — order unchanged in insertion mode
// map.Add("a", 2);     // would throw ArgumentException (duplicate key)
```

## Thread safety

`SequencedDictionary<TKey, TValue>` is **not thread-safe**. In access-order mode reads mutate the iteration order, so even concurrent read-read is unsafe without external synchronization. Wrap with a lock or `ReaderWriterLockSlim` when sharing a single instance across threads.

## Relationship to `OrderedDictionary<TKey, TValue>`

The BCL's `OrderedDictionary<TKey, TValue>` (.NET 9+) is *positional* — index-addressable with `Insert(index, …)`, `RemoveAt(index)`, and O(1) random access by position, but O(n) removal of a non-tail entry. `SequencedDictionary<TKey, TValue>` exposes **no** positional surface; it trades index access for O(1) removal of either end and O(1) removal of any entry by key, and adds the access-order mode. On Bodu's `net8.0` target the BCL `OrderedDictionary<TKey, TValue>` is not available regardless.

## API summary

| Member | Description |
|---|---|
| `this[TKey]` (get) | Returns the value; throws if the key is not present. Moves the key to the tail in access-order mode. |
| `this[TKey]` (set) | Adds a new entry at the tail, or updates an existing one in place. |
| `Add(TKey, TValue)` | Appends a new entry; throws `ArgumentException` on a duplicate key. |
| `TryGetValue(TKey, out TValue)` | Returns the value without throwing. Moves the key to the tail in access-order mode. |
| `ContainsKey(TKey)` | Returns `true` without reordering. |
| `Remove(TKey)` | Removes the entry in O(1). |
| `First` / `Last` | The head / tail entry; O(1). Throws if empty. |
| `TryGetFirst` / `TryGetLast` | The head / tail entry without throwing; O(1). Never reorders. |
| `TryRemoveFirst` / `TryRemoveLast` | Removes and returns the head / tail entry in O(1). |
| `AccessOrder` | `true` when the dictionary reorders on access. |
| `Comparer` | The `IEqualityComparer<TKey>` used for key identity. |
| `Count` | Number of entries currently held. |
| `Keys` / `Values` | Live, order-preserving views. |
| `Clear()` | Removes all entries. |

## Where to go next

- [Evicting dictionary](evicting-dictionary.md) — fixed-capacity cache with FIFO / LRU / LFU eviction policies.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide across the namespace.
- [Bodu.Core overview](index.md) — all key types at a glance.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
