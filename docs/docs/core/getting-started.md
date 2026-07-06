---
title: Bodu.Core — Getting started
---

# Bodu.Core — Getting started

## Install

```bash
dotnet add package Bodu.Core
```

Targets `net8.0`. No external runtime dependencies. `Bodu.Core` covers the buffers, extensions, threading, and functional surfaces; the specialized collection catalogue ships in the companion [`Bodu.Collections`](../collections/getting-started.md) package (namespaces unchanged; it depends on `Bodu.Core`), and the thread-safe collections in [`Bodu.Collections.Concurrent`](../collections-concurrent/getting-started.md):

```bash
dotnet add package Bodu.Collections             # collection catalogue
dotnet add package Bodu.Collections.Concurrent  # thread-safe variants
```

## Minimal samples

Every sample below needs only `Bodu.Core`. For collection samples (`CircularBuffer<T>`, `EvictingDictionary<TKey, TValue>`, `Deque<T>`, and the rest of the catalogue), see the [Bodu.Collections getting-started page](../collections/getting-started.md).

### Week pattern (`WeekPattern`)

```csharp
using Bodu;

WeekPattern weekdays = WeekPattern.Parse("MTuWThF");
WeekPattern weekend  = WeekPattern.Parse("SaSu");
WeekPattern allDays  = weekdays | weekend;

bool monday = weekdays.Contains(DayOfWeek.Monday); // true
```

`WeekPattern` is an immutable `readonly struct`, so `With` / `Without` and the `|`, `&`, `^`, `~` operators each return a new value. Presets `WeekPattern.Empty`, `WeekPattern.Weekdays`, and `WeekPattern.Weekend` cover the common cases.

### Railway outcomes (`Option<T>` / `Result<T>`)

Model "might be absent" and "might fail" as values, and chain the happy path through combinators:

```csharp
using Bodu.Functional;

Option<string> setting = LookupSetting("timeout");

int timeoutMs = setting
    .Map(int.Parse)
    .Filter(ms => ms > 0)
    .Match(onSome: ms => ms, onNone: () => 30_000);   // default when absent or invalid

Result<Order> order = ParseOrder(json)
    .Bind(Validate)                                // runs only on the success track
    .Map(Normalize);

if (!order.IsSuccess)
    logger.LogWarning("Rejected: {Message}", order.Error.Message);
```

`default(Option<T>)` is `None` and `default(Result<T>)` is a failure with an empty error — an unassigned field is well-formed, never a phantom success.

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

### Async coordination (`AsyncLock`)

The awaitable peer of `lock` — waiting yields the thread instead of blocking it:

```csharp
using Bodu.Threading;

private readonly AsyncLock _gate = new();

public async Task WriteAsync(Entry entry, CancellationToken cancellationToken)
{
    using (await _gate.LockAsync(cancellationToken))
    {
        await AppendToJournalAsync(entry, cancellationToken);
    }
}   // releaser disposes → lock released, next waiter resumes
```

`AsyncSemaphore`, `AsyncReaderWriterLock`, the async reset/countdown events, `AsyncLazy<T>`, `AsyncDebouncer`, and `RateGate` follow the same awaitable, cancellation-aware shape.

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
- **[Core Foundations guides](../../guides/core/index.md)** — recipe-style walk-throughs for the headline types.
- **[Bodu.Collections getting started](../collections/getting-started.md)** — the collection catalogue's install and samples.
- **[Bodu.Collections.Concurrent getting started](../collections-concurrent/getting-started.md)** — the thread-safe collections.
- **[Project introduction](../introduction.md)** — the per-library map, if you also need hashing, cryptography, calendar, or text utilities.
