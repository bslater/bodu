---
title: Bodu.Core — Getting started
---

# Bodu.Core — Getting started

## Install

```bash
dotnet add package Bodu.Core
dotnet add package Bodu.Collections
```

Targets `net8.0`. No external runtime dependencies. The specialized collection catalogue (`Bodu.Collections.Generic` and its `.Concurrent` / `.Graphs` / `.Trees` / `Bodu.Collections.Probabilistic` siblings) ships in the `Bodu.Collections` package (namespaces unchanged; it depends on `Bodu.Core`); `Bodu.Core` alone suffices for the buffers, extensions, threading, and functional surfaces.

## Minimal samples

The collection samples below (`CircularBuffer<T>`, `EvictingDictionary<TKey, TValue>`, `Deque<T>`, `SequencedDictionary<TKey, TValue>`, `IndexedPriorityQueue<TElement, TPriority>`) need the `Bodu.Collections` package; the remaining samples (`WeekPattern`, `PooledBufferBuilder<T>`, the date extensions, `ThrowHelper`) need only `Bodu.Core`.

### Circular buffer (`CircularBuffer<T>`)

```csharp
using Bodu.Collections.Generic;

var buffer = new CircularBuffer<int>(capacity: 4, allowOverwrite: true);

buffer.Enqueue(1);
buffer.Enqueue(2);
buffer.Enqueue(3);
buffer.Enqueue(4);
buffer.Enqueue(5); // 1 is evicted; buffer holds [2, 3, 4, 5]

int oldest = buffer.Dequeue(); // 2
```

`allowOverwrite: false` throws `InvalidOperationException` when full. For thread safety, use `Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer<T>`.

### Evicting dictionary (`EvictingDictionary<TKey, TValue>`)

```csharp
using Bodu.Collections.Generic;

var cache = new EvictingDictionary<string, byte[]>(
    capacity: 100,
    policy: EvictingDictionaryPolicy.LeastRecentlyUsed);

cache["alpha"] = LoadFromDisk("alpha");
_ = cache.TryGetValue("alpha", out byte[]? value); // touches LRU order
```

Policies: `FirstInFirstOut`, `LeastRecentlyUsed`, `LeastFrequentlyUsed`, `MostRecentlyUsed`, `RandomReplacement`, `SecondChance`.

### Deque (`Deque<T>`)

```csharp
using Bodu.Collections.Generic;

var deque = new Deque<string>(allowGrow: true);

deque.AddFirst("a");
deque.AddLast("b");
deque.AddLast("c");

string first = deque.RemoveFirst(); // "a"
string last  = deque.RemoveLast();  // "c"
```

### Week pattern (`WeekPattern`)

```csharp
using Bodu;

WeekPattern weekdays = WeekPattern.Parse("MTuWThF");
WeekPattern weekend  = WeekPattern.Parse("SaSu");
WeekPattern allDays  = weekdays | weekend;

bool monday = weekdays.Contains(DayOfWeek.Monday); // true
```

`WeekPattern` is an immutable `readonly struct`, so `With` / `Without` and the `|`, `&`, `^`, `~` operators each return a new value. Presets `WeekPattern.Empty`, `WeekPattern.Weekdays`, and `WeekPattern.Weekend` cover the common cases.

### Sequenced dictionary (`SequencedDictionary<TKey, TValue>`)

An insertion- (or access-) ordered map with O(1) access to and removal of either end — the .NET analogue of Java's `LinkedHashMap`:

```csharp
using Bodu.Collections.Generic;

var lru = new SequencedDictionary<string, byte[]>(accessOrder: true);

lru["a"] = LoadFromDisk("a");
_ = lru.TryGetValue("a", out _);   // access-order: moves "a" to the most-recent end

// Trim the least-recently-used entry in O(1).
if (lru.Count > 100)
    lru.TryRemoveFirst(out _);
```

### Indexed priority queue (`IndexedPriorityQueue<TElement, TPriority>`)

A min-heap that supports O(log n) priority updates by element identity — the shape Dijkstra and A* need:

```csharp
using Bodu.Collections.Generic;

var pq = new IndexedPriorityQueue<string, double>();
pq.Enqueue("source", 0);
pq.EnqueueOrUpdate("a", 7);    // add, or update if already queued
pq.EnqueueOrUpdate("a", 3);    // O(log n) decrease-key, no duplicate

var (element, priority) = pq.Dequeue();   // ("source", 0) — smallest priority first
```

### Pooled buffer (`PooledBufferBuilder<T>`)

An `ArrayPool<T>`-backed `IBufferWriter<T>` for assembling a span without per-append allocation:

```csharp
using Bodu.Buffers;

using var builder = new PooledBufferBuilder<byte>(initialCapacity: 256);
builder.Append((byte)'{');
builder.AppendRange("\"ok\":true"u8);
builder.Append((byte)'}');

byte[] json = builder.ToArrayAndDispose();   // snapshot, then return the rental
```

### Date arithmetic (`DateTimeExtensions`)

```csharp
using Bodu.Extensions;

DateTime today = DateTime.Today;

DateTime startOfWeek  = today.FirstDateOfWeek();                 // culture's first day of the current week
DateTime nextFriday   = today.NextDateOfWeek(DayOfWeek.Friday);  // strictly after today
DateTime endOfQuarter = today.LastDateOfQuarter();              // calendar Q-end
int isoWeek           = today.IsoWeekOfYear();                  // ISO 8601 week number (method, not property)
```

`FirstDateOfWeek` has overloads taking a `CultureInfo` or a <xref:Bodu.WorkingDaysOfWeek> preset; `LastDateOfQuarter` accepts a <xref:Bodu.Extensions.CalendarQuarterDefinition> (e.g. `AprilToMarch`, `April6ToApril5` for the UK tax year) so the same call covers fiscal calendars. The `DateOnly` equivalents live on <xref:Bodu.Extensions.DateOnlyExtensions>, which adds an `Age` calculation.

### Centralized argument validation (`ThrowHelper`)

```csharp
using Bodu;

public static double Average(IReadOnlyList<int> values)
{
    ThrowHelper.ThrowIfNull(values);
    ThrowHelper.ThrowIfZero(values.Count);
    return values.Average();
}
```

`ThrowHelper.ThrowIf…` uses `[CallerArgumentExpression]` so the parameter name is captured automatically.

## Where to go next

- **[Bodu.Core introduction](index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Core guides](../../guides/core/index.md)** — recipe-style walk-throughs for circular buffers, deques, evicting dictionaries, and `WeekPattern`.
- **[Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic)** — full type-by-type docs.
- **[Project introduction](../introduction.md)** — the per-library map, if you also need hashing, cryptography, calendar, or text utilities.
