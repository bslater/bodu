---
title: Bodu.Core — Getting started
---

# Bodu.Core — Getting started

## Install

```bash
dotnet add package Bodu.Core
```

Targets `net8.0`. No external runtime dependencies.

## Minimal samples

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

bool monday = weekdays.Includes(DayOfWeek.Monday); // true
```

### Date arithmetic (`DateTimeExtensions`)

```csharp
using Bodu.Extensions;

DateTime today = DateTime.Today;

DateTime startOfWeek    = today.GetFirstDateOfWeek(DayOfWeek.Monday);
DateTime nextFriday     = today.NextOccurrence(DayOfWeek.Friday);
DateTime endOfQuarter   = today.LastDateOfQuarter();
int isoWeek             = today.IsoWeekOfYear;
```

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
