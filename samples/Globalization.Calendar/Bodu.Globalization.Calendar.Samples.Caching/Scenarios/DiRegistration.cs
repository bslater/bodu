// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiRegistration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

/// <summary>
/// Demonstrates the dependency-injection composition: <c>AddCachedNotableDateService</c> decorates whatever
/// <see cref="INotableDateService" /> is already registered, so consumers keep injecting the interface and
/// gain caching transparently.
/// </summary>
public static class DiRegistration
{
    /// <summary>
    /// Registers a data-pack service, wraps it with the caching decorator, and resolves through the container.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AddCachedNotableDateService - decorating the registered service",
            what: "Registers a data-pack service, adds the caching decorator with a seven-day TTL, then resolves "
                + "INotableDateService from the container and reports the concrete type that came back.",
            why: "Caching is a deployment decision, not an application one - the same code should run "
                + "uncached in a test, in-memory in a single instance, and against a shared distributed cache in "
                + "a cluster. Registering it as a decorator over whatever is already registered keeps that "
                + "choice in the composition root: consumers keep injecting the interface and never learn "
                + "whether a cache exists. The TTL matters because the cache is holding computed rule output, "
                + "and rule data can be republished - so the entry has to expire even though the underlying "
                + "calculation is deterministic. The durable backends slot into the same call, which is why "
                + "moving from the default file cache to SQLite or Redis is a registration line rather than a "
                + "refactor.",
            expect: "The resolved type is the caching decorator, not the data-pack service - the registration "
                + "wrapped what was already there. The query then works exactly as it did without the decorator, "
                + "which is the point: nothing downstream changes.");

        var services = new ServiceCollection();

        // 1. The underlying service - any of the AddNotableDateService forms works.
        services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

        // 2. The caching decorator. Without a cacheFactory it writes a TOML file cache under
        //    NotableDateCachingOptions.CacheDirectory (a bodu-notable-dates folder in the temp path by
        //    default); the options callback tunes TTL/refresh-ahead, and cacheFactory swaps the backend
        //    (the in-memory or JSON caches, or the Sqlite / IDistributedCache backends from the add-on packages).
        services.AddCachedNotableDateService(configure: options =>
        {
            options.Ttl = TimeSpan.FromDays(7);
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<INotableDateService>();
        Console.WriteLine($"  Resolved service type: {service.GetType().Name}"
            + "  (the decorator, not the data-pack service - the registration wrapped what was already there)");

        var anzac = service.Resolve(new DateOnly(2026, 4, 25), "AU");
        Console.WriteLine($"  AU 2026-04-25: {string.Join(", ", anzac.Select(n => n.DisplayName))}"
            + "  (identical to the uncached result - consumers inject the interface and never learn a cache exists)");

        // ------------------------------------------------------------------------------------------------
        // Durable backends ship as add-on packages. Each registers an INotableDateCache, which the decorator
        // uses when cacheFactory hands it over (the DurableBackends scenario constructs both directly):
        //
        //   // Bodu.Globalization.Calendar.Caching.Sqlite - a single-file durable cache:
        //   services.AddSqliteNotableDateCache(configure: options => options.DatabaseFilePath = "calendar-cache.db");
        //
        //   // Bodu.Globalization.Calendar.Caching.Distributed - over a registered IDistributedCache:
        //   services.AddDistributedNotableDateCache();
        //   // ... or registering a Redis IDistributedCache as well:
        //   services.AddRedisNotableDateCache(redis => redis.Configuration = "localhost:6379");
        //
        //   // Either way, hand the registered cache to the decorator:
        //   services.AddCachedNotableDateService(cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
        //
        // They are left out of this registration so the scenario keeps the default backend.
        // ------------------------------------------------------------------------------------------------

        Console.WriteLine();
    }
}
