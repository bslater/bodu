---
title: Writing a cache backend
---

# Writing a cache backend

The shipped backends — in-memory, TOML and JSON files, SQLite, and `IDistributedCache` — cover most deployments, but the storage seam is public. This page is for the case they do not cover: it states the <xref:Bodu.Globalization.Calendar.Caching.INotableDateCache> contract and the invariants a backend must honour, shows the two ways to satisfy it (derive the mechanism from <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheBase`1>, or implement the interface directly), walks through a complete backend that compiles, and shows how to plug it in and test it.

## The contract

`INotableDateCache` has three members:

| Member | Contract |
|---|---|
| `NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)` | Return the entry for `(territory, year)` **only if** its `ResourceVersion` equals `resourceVersion` and it is still fresh at `asOf` under `ttl`; otherwise `null`. Throws `ArgumentNullException` for a `null` territory or version; every other failure is a `null` (a miss). |
| `NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf)` | Merge the entry into the territory's stored years — the incoming year replaces any existing entry for that year — and prune entries that are stale at `asOf` or belong to a different resource version. Answer `Stored`, or `Failed` when a storage error was swallowed, or `Skipped` for a deliberate no-op. |
| `void Clear()` | Best-effort removal of everything. |

The unit of storage is <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheEntry> — an immutable record of `(Territory, Year, ResourceVersion, Occurrences, ComputedAtUtc)` with an `IsFresh(asOf, ttl)` helper. An entry whose `Occurrences` list is empty is a valid, cacheable result ("this year was computed and has nothing").

## Invariants a backend must honour

1. **Territory keys are case-insensitive.** `us` and `US` address the same entry; a subdivision (`CA-ON`) and its parent (`CA`) are distinct. Normalise to `Trim().ToUpperInvariant()` on both read and write, and persist the normalised form.
2. **Version match is exact and ordinal.** Serve an entry only when its `ResourceVersion` equals the requested one; a store drops every stored entry whose version differs from the incoming entry's, so a resource reload invalidates the territory wholesale.
3. **Freshness is strict.** An entry exactly one time-to-live old is stale (`ComputedAtUtc + ttl <= asOf` is a miss). `NotableDateCacheEntry.IsFresh` encodes this; use it rather than re-deriving it.
4. **Ordering round-trips.** `Occurrences` must come back in exactly the order they were stored — the service's date-then-identity order. The decorator assembles multi-year range results by concatenation and relies on this; a backend that cannot preserve order forces a sort fallback on every read.
5. **Merge is per year, last write wins.** A store for `(US, 2026)` replaces the previous `(US, 2026)` and leaves `(US, 2025)` alone; the surviving set is re-checked for freshness on every write, so the store self-cleans.
6. **Failures degrade, they do not throw.** A read failure is an empty read; a write failure returns `Failed`. Only argument validation throws. If you expose a `ThrowOnStorageFailure`-style option, keep `false` as the default.
7. **Concurrent stores to one territory must not lose entries.** Two writers merging `(US, 2026)` and `(US, 2027)` concurrently must end with both years present — serialise the read-modify-write per territory.

## Option A — derive from `NotableDateCacheBase<TOptions>`

<xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheBase`1> implements invariants 1–3, 5, and 7 for you: it normalises the territory, applies the freshness and version policy in `GetYear`, merges and prunes in `StoreYear` under a per-territory lock, and implements the internal batch-read seam so a multi-year range costs one read. A derived backend supplies **only** persistence of a territory's entry list:

| Member | Modifier | Role |
|---|---|---|
| `protected NotableDateCacheBase(TOptions options)` | constructor | Validates and stores the options (`options.Validate()` is called). |
| `protected TOptions Options` | property | The validated options. |
| `IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory)` | `protected internal abstract` | Return the raw stored entries for a normalised territory, unfiltered; an empty list when none exist **or the read failed**. |
| `bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries)` | `protected internal abstract` | Replace the territory's stored state with `entries` (an empty list means "delete the territory"). Return `true` when persisted, `false` when a failure was swallowed. The list is freshly allocated per write, so it may be stored by reference. |
| `bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries, TimeSpan ttl, DateTimeOffset asOf)` | `protected internal virtual` | Overload that also receives the time-to-live, for stores with server-side expiry; the default delegates to the two-argument form. |
| `void Clear()` | `public abstract` | Remove everything. |
| `NotableDateCacheEntry? GetYear(…)` | `public virtual` | Override only when a keyed store can answer one year cheaper than reading the territory; the override must apply the same policy. |

`TOptions` must derive from <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOptions>; use the base type itself when the backend needs no settings. The two seam members are declared `protected internal`, which a class in another assembly overrides with plain `protected override`. The base takes ownership of ordering only in the sense that it never reorders — invariant 4 is still yours.

A complete backend over a dictionary, with the read-failure and write-failure paths a real store would have:

```csharp
using System.Collections.Concurrent;
using Bodu.Globalization.Calendar.Caching;

/// <summary>An INotableDateCache over an in-process dictionary — the smallest complete backend.</summary>
public sealed class DictionaryNotableDateCache : NotableDateCacheBase<NotableDateCacheOptions>
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<NotableDateCacheEntry>> _store =
        new(StringComparer.Ordinal);

    public DictionaryNotableDateCache()
        : this(new NotableDateCacheOptions())
    {
    }

    public DictionaryNotableDateCache(NotableDateCacheOptions options)
        : base(options)
    {
    }

    public override void Clear() =>
        _store.Clear();

    protected override IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory)
    {
        // The base has already normalised 'territory'. Return the raw list; the base filters it.
        return _store.TryGetValue(territory, out IReadOnlyList<NotableDateCacheEntry>? entries)
            ? entries
            : Array.Empty<NotableDateCacheEntry>();
    }

    protected override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries)
    {
        // An empty merged list means every year was pruned: remove the territory rather than keep an empty state.
        if (entries.Count == 0)
        {
            _store.TryRemove(territory, out _);
            return true;
        }

        // The base hands over a freshly merged list per write, so it is safe to store by reference.
        _store[territory] = entries;
        return true;   // a dictionary write cannot fail; a real store returns false when it swallows an exception
    }
}
```

A store with I/O follows the same shape with a `try`/`catch` around the persistence call: on failure, `ReadEntries` returns an empty list and `WriteEntries` returns `false` (rethrowing only when `Options.ThrowOnStorageFailure` is set), and the failure is logged and counted.

## Option B — implement `INotableDateCache` directly

Implement the interface when the store's natural shape is not "a list per territory" — for example a key-value store that can only fetch one `(territory, year)` pair at a time — or when you want none of the base's locking. You then own every invariant above, including merge-and-prune on store. <xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache> is the smallest example of a direct implementation: it validates arguments, answers `null`, and reports `Skipped`.

The batch-read seam the decorator uses to answer a multi-year range from one read is internal to the package, so a direct implementation is read once per spanned year; that is correct, only less efficient for wide ranges.

## Registering the backend

Hand the backend to the decorator directly, or through the registration's `cacheFactory`. The factory is the same composition rule the SQLite and distributed add-ons use, and the container does not dispose a factory-supplied cache, so register it as a singleton if it holds resources:

<!-- compile -->
```csharp
using var service = new CachingNotableDateService(
    AmericasCalendarData.CreateService("US"),
    new InMemoryNotableDateCache(),                 // or your DictionaryNotableDateCache
    new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(7) });

IReadOnlyList<NotableDate> july = service.Resolve(
    new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)), "US");
```

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
services.AddSingleton<INotableDateCache, DictionaryNotableDateCache>();
services.AddCachedNotableDateService(cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
```

## Testing a backend

The repository exercises every shipped backend through one abstract contract suite, `NotableDateCacheContractTests` in `Bodu.Globalization.Calendar.Caching.Test` (folder `Globalization.Calendar.Caching.Contracts`). A concrete subclass supplies a fresh cache through `protected abstract INotableDateCache CreateCache()` and inherits every contract test — case-insensitive keys, version mismatch as a miss, strict freshness, per-year merge and prune, ordering round-trip, and the `StoreYear` status. `InMemoryNotableDateCacheContractTests`, `TomlFileNotableDateCacheContractTests`, and `JsonFileNotableDateCacheContractTests` are its in-repo subclasses; the SQLite and distributed test projects carry their own subclasses under their `*.Contracts` folders. The decorator's own tests additionally use an `OrderViolatingNotableDateCache` to prove the sort fallback and a `CountingNotableDateCache` to count reads.

Outside the repository the contract base is not packaged, so write the same checks against your backend. The essentials — each one is a direct reading of an invariant above:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

public static class BackendChecks
{
    public static void Run(INotableDateCache cache)
    {
        var asOf = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        TimeSpan ttl = TimeSpan.FromDays(30);
        IReadOnlyList<NotableDate> year = AmericasCalendarData.CreateService("US").Resolve(2026, "US");

        // Store, then read back by a differently cased key: same entry, same order.
        var entry = new NotableDateCacheEntry("us", 2026, "v1", year, asOf);
        if (cache.StoreYear(entry, ttl, asOf) != NotableDateCacheWriteStatus.Stored) throw new Exception("store");
        NotableDateCacheEntry? hit = cache.GetYear("US", 2026, "v1", ttl, asOf);
        if (hit is null || !hit.Occurrences.Select(o => (o.Date, o.Identity)).SequenceEqual(year.Select(o => (o.Date, o.Identity))))
            throw new Exception("ordering or key normalisation");

        // A different version is a miss; an entry exactly one TTL old is a miss.
        if (cache.GetYear("US", 2026, "v2", ttl, asOf) is not null) throw new Exception("version");
        if (cache.GetYear("US", 2026, "v1", ttl, asOf + ttl) is not null) throw new Exception("freshness");

        // Storing a new version prunes the old one; storing another year keeps both.
        cache.StoreYear(entry with { ResourceVersion = "v2" }, ttl, asOf);
        if (cache.GetYear("US", 2026, "v1", ttl, asOf) is not null) throw new Exception("prune on version change");
        cache.StoreYear(entry with { Year = 2027, ResourceVersion = "v2" }, ttl, asOf);
        if (cache.GetYear("US", 2026, "v2", ttl, asOf) is null || cache.GetYear("US", 2027, "v2", ttl, asOf) is null)
            throw new Exception("merge per year");

        cache.Clear();
        if (cache.GetYear("US", 2026, "v2", ttl, asOf) is not null) throw new Exception("clear");
    }
}
```

## Where to go next

- **[Cache backends and options](backends-and-options.md)** — what the shipped backends persist and how they are configured.
- **[Caching notable dates](notable-date-caching.md)** — the decorator's behaviour the backend serves.
- **[Bodu.Globalization.Calendar.Caching API reference](xref:Bodu.Globalization.Calendar.Caching)** — `INotableDateCache`, `NotableDateCacheBase<TOptions>`, `NotableDateCacheEntry`, `NotableDateCacheWriteStatus`.
- **[Globalization & Calendars guides](../../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
