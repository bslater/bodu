---
title: Evicting dictionary
---

# Evicting dictionary

`EvictingDictionary<TKey, TValue>` is a fixed-capacity dictionary that automatically removes the least-worthy entry when capacity is exceeded. It implements the full `IDictionary<TKey, TValue>` surface, so it is a drop-in replacement for `Dictionary<TKey, TValue>` wherever a bounded cache is wanted — the eviction is the only behavioural difference. The victim is selected by one of **six** eviction policies, fixed at construction:

| Policy | Evicts | Best for |
|---|---|---|
| `FirstInFirstOut` | The entry added earliest, regardless of access. | Bounded queues; sliding time windows where age, not use, decides. |
| `LeastRecentlyUsed` | The entry accessed furthest in the past. | General-purpose caches where recency predicts future use. |
| `LeastFrequentlyUsed` | The entry with the lowest cumulative access count. | Long-lived caches where popularity predicts future use. |
| `MostRecentlyUsed` | The entry accessed most recently. | Scan-resistant workloads where the just-touched item is least likely to be wanted again (e.g. a sequential cursor over a table). |
| `RandomReplacement` | A uniformly randomly chosen entry. | Cheap, metadata-free eviction where any approximation is acceptable. |
| `SecondChance` | A FIFO scan that skips entries flagged as recently accessed (the flag clears on skip), evicting the first unflagged entry. | A low-overhead approximation of LRU — the *clock* algorithm — without per-access timestamp bookkeeping. |

![EvictingDictionary entries with FIFO, LRU, and LFU eviction lanes](../../images/diagrams/evicting-dictionary.svg)

> [!NOTE]
> The enum declaration order is `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance` — LFU precedes MRU. Do not depend on the numeric values; refer to the members by name.

The default policy when none is specified is `LeastRecentlyUsed`, and the default capacity is `16`.

## How the policies track worthiness

Each entry carries the metadata its policy needs — an insertion order link, a recency link, an access count, and a recently-accessed flag — and the policy decides which structure is the next-victim oracle:

| Policy | Internal structure | A successful read… |
|---|---|---|
| `FirstInFirstOut` | Insertion-ordered linked list. | …does not move the entry. |
| `LeastRecentlyUsed` / `SecondChance` | Recency linked list (head = victim). | …moves the entry to the most-recent end (LRU) or sets its reference flag (SecondChance). |
| `MostRecentlyUsed` | Recency linked list (tail = victim). | …moves the entry to the most-recent end. |
| `LeastFrequentlyUsed` | Frequency-bucketed sorted structure. | …increments the access count and re-buckets. |
| `RandomReplacement` | None (plain hash table). | …does not move the entry. |

Reads via the `this[key]` getter and `TryGetValue` count as accesses and update the policy metadata; the `TotalTouches` counter increments. `ContainsKey` and `PeekEvictionCandidate` are pure reads and do **not** count as accesses. `Touch` promotes a key without reading the value.

> [!IMPORTANT]
> Because reads mutate eviction metadata for LRU, MRU, LFU, and SecondChance, even concurrent read-read is unsafe. See [Thread safety](#thread-safety) below.

## Pattern 1 — LRU cache

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<string, byte[]>(
    capacity: 50,
    EvictingDictionaryPolicy.LeastRecentlyUsed);

cache["config.json"] = LoadFile("config.json");
cache["schema.xsd"]  = LoadFile("schema.xsd");

// Reading a key marks it as recently used.
if (cache.TryGetValue("config.json", out byte[]? data))
    Use(data);
```

## Pattern 2 — FIFO bounded store

```csharp
using Bodu.Collections.Generic;

var recent = new EvictingDictionary<Guid, string>(
    capacity: 100,
    EvictingDictionaryPolicy.FirstInFirstOut);

// Oldest entry is evicted once capacity is reached.
foreach (var (id, message) in incoming)
    recent[id] = message;
```

## Pattern 3 — LFU cache for hot-spot data

```csharp
using Bodu.Collections.Generic;

var hot = new EvictingDictionary<int, CompiledQuery>(
    capacity: 200,
    EvictingDictionaryPolicy.LeastFrequentlyUsed);

hot[queryId] = Compile(sql);
if (hot.TryGetValue(queryId, out CompiledQuery? q))
    Execute(q);   // increments the access counter
```

## Pattern 4 — scan-resistant cache (MRU)

`MostRecentlyUsed` evicts the entry just touched. It is the right choice when the access pattern is a one-pass sweep — a sequential cursor or a streaming join — where the item you have just read is the *least* likely to be needed again, and the older entries are the ones worth keeping:

```csharp
using Bodu.Collections.Generic;

var cursorCache = new EvictingDictionary<long, Page>(
    capacity: 64,
    EvictingDictionaryPolicy.MostRecentlyUsed);

// A forward scan: each page read is unlikely to be revisited,
// so on overflow the just-read page is the eviction victim,
// preserving the earlier pages.
foreach (long pageId in scanOrder)
    cursorCache[pageId] = ReadPage(pageId);
```

## Pattern 5 — clock approximation of LRU (SecondChance)

`SecondChance` is the *clock* algorithm: a FIFO ring where each entry carries a reference bit. On overflow the scan inspects the head; if its bit is set, the bit clears and the entry gets a second chance (rotated to the back), otherwise it is evicted. It approximates LRU's hit rate without LRU's per-access list-splice, so it is cheaper on write-heavy workloads:

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<string, byte[]>(
    capacity: 4096,
    EvictingDictionaryPolicy.SecondChance);
```

## Pattern 6 — random replacement

`RandomReplacement` keeps no recency or frequency metadata at all — eviction is a uniformly random pick. It is the cheapest policy per write and is a reasonable default when the access pattern has no exploitable locality:

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<int, Tile>(
    capacity: 256,
    EvictingDictionaryPolicy.RandomReplacement);
```

## Pattern 7 — explicit eviction promotion (Touch)

Call `Touch` to promote a key without reading its value — useful when you want to mark an entry as recently used based on external logic. `Touch` returns `false` if the key is absent; `TouchOrThrow` raises `KeyNotFoundException` instead:

```csharp
using Bodu.Collections.Generic;

var lru = new EvictingDictionary<string, Session>(
    capacity: 1000,
    EvictingDictionaryPolicy.LeastRecentlyUsed);

// Extend the session's lifetime on heartbeat.
void OnHeartbeat(string sessionId)
{
    if (!lru.Touch(sessionId))   // false — the session was already evicted
        ReloadSession(sessionId);
}
```

## Pattern 8 — observing evictions

Two events fire around each eviction, both typed `Action<TKey, TValue>`:

- `ItemEvicting` — raised immediately **before** the entry is removed (informational; it cannot cancel the eviction).
- `ItemEvicted` — raised immediately **after** removal, when the key and value are no longer present.

Use them to flush a dirty entry to its backing store, decrement a resource counter, or emit a metric:

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<string, Document>(
    capacity: 500,
    EvictingDictionaryPolicy.LeastRecentlyUsed);

cache.ItemEvicting += (key, doc) =>
{
    if (doc.IsDirty)
        FlushToDisk(key, doc);   // persist before it leaves the cache
};
```

> [!IMPORTANT]
> Event handlers must not mutate the dictionary. `Add`, `Remove`, `Clear`, and the indexer setter are guarded against re-entry from inside an eviction handler and throw `InvalidOperationException` if called there. Keep handlers side-effect-only with respect to the dictionary itself.

## Inspecting the next victim

`PeekEvictionCandidate` returns the key that *would* be evicted on the next overflow, or `default` when the dictionary is empty. It is a pure read — it does not touch recency or frequency metadata — so it is safe to call in a monitoring loop:

```csharp
TKey? nextOut = cache.PeekEvictionCandidate();
```

The `EvictionCount` and `TotalTouches` properties expose running totals of how many entries have been evicted and how many accesses have occurred since construction — useful for cache-effectiveness telemetry.

## Pattern 9 — assignment semantics

Unlike `Dictionary<TKey, TValue>.Add`, assigning to an existing key replaces the value **and** resets its eviction metadata. Note that `Add(key, value)` here is also add-*or-replace* — it does **not** throw on a duplicate key the way `Dictionary<TKey, TValue>.Add` does. There is no `TryAdd` or `GetOrAdd`; use the indexer or `Add`:

```csharp
using Bodu.Collections.Generic;

var lru = new EvictingDictionary<string, int>(
    capacity: 3,
    EvictingDictionaryPolicy.LeastRecentlyUsed);

lru["a"] = 1;
lru["b"] = 2;
lru["c"] = 3;

// Re-assigning "a" resets its recency to newest.
lru["a"] = 10;

// Adding "d" evicts the LRU entry, which is now "b".
lru["d"] = 4;

bool hasB = lru.ContainsKey("b");   // false — evicted
```

## Choosing a capacity and comparer

The capacity is the hard ceiling on entry count and must be greater than zero (the constructor throws `ArgumentOutOfRangeException` otherwise). A custom `IEqualityComparer<TKey>` can be supplied through the comparer-taking constructor overloads — for example `StringComparer.OrdinalIgnoreCase` for case-insensitive keys — and a seed collection can be supplied to pre-populate the cache:

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<string, int>(
    capacity: 1000,
    policy: EvictingDictionaryPolicy.LeastRecentlyUsed,
    comparer: StringComparer.OrdinalIgnoreCase);
```

## Thread safety

`EvictingDictionary<TKey, TValue>` is **not thread-safe**. Concurrent reads can mutate eviction metadata (LRU/MRU recency order, LFU access counts, SecondChance reference flags), so concurrent read-read is also not safe without external synchronization. Wrap with a lock or `ReaderWriterLockSlim` when multiple threads access the same instance. For a thread-safe bounded set without eviction policies, see [`ConcurrentHashSet<T>`](concurrent-collections.md); there is no concurrent evicting-dictionary variant in the package.

## `Keys` / `Values` enumeration order

`Keys` and `Values` are live views, but their enumeration order reflects the **current eviction order**, not insertion order — FIFO/LRU/SecondChance walk the recency list head-to-tail, MRU walks it tail-to-head, LFU walks ascending frequency buckets, and `RandomReplacement` follows the underlying hash-table order. Do not rely on `Keys` to recover insertion order under a recency-based policy.

## API summary

| Member | Description |
|---|---|
| `this[TKey]` (get) | Returns the value; throws `KeyNotFoundException` if absent. Counts as an access (promotes in LRU / MRU / LFU / SecondChance). |
| `this[TKey]` (set) | Adds or replaces the entry, resetting its eviction metadata. Evicts the policy's victim if capacity is exceeded. |
| `Add(TKey, TValue)` | Add-or-replace; does **not** throw on a duplicate key. |
| `TryGetValue(TKey, out TValue)` | Returns the value without throwing; counts as an access. |
| `ContainsKey(TKey)` | Returns `true` without counting as an access. |
| `Touch(TKey)` | Promotes the key without reading the value; returns `false` if absent. |
| `TouchOrThrow(TKey)` | As `Touch`, but throws `KeyNotFoundException` when the key is absent. |
| `PeekEvictionCandidate()` | Returns the key that would be evicted next, or `default` when empty. Pure read. |
| `Remove(TKey)` | Removes the entry. |
| `Clear()` | Removes all entries and resets the running counters. |
| `Count` | Number of entries currently held. |
| `Capacity` | Maximum entries before eviction (get only). |
| `Policy` | The active `EvictingDictionaryPolicy` (get only). |
| `EvictionCount` | Total entries evicted since construction. |
| `TotalTouches` | Total accesses (reads + `Touch`) since construction. |
| `Keys` / `Values` | Live views in current eviction order (see above). |
| `ItemEvicting` / `ItemEvicted` | `Action<TKey, TValue>` events raised before / after each eviction. |

## Where to go next

- [Circular buffer](circular-buffer.md) — fixed-capacity FIFO ring buffer.
- [WeekPattern](week-pattern.md) — immutable bitmask value type for sets of days of the week.
- [Bodu.Core overview](index.md) — all key types at a glance.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
