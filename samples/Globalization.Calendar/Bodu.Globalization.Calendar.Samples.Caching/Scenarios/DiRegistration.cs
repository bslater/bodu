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
        Console.WriteLine("--- AddCachedNotableDateService: decorating the registered service ---");

        var services = new ServiceCollection();

        // 1. The underlying service - any of the AddNotableDateService forms works.
        services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

        // 2. The caching decorator. With no arguments it uses an in-memory cache; the options callback
        //    tunes TTL/refresh-ahead, and cacheFactory swaps the backend (Json/Toml file caches here,
        //    or the Sqlite / IDistributedCache backends from the add-on packages).
        services.AddCachedNotableDateService(configure: options =>
        {
            options.Ttl = TimeSpan.FromDays(7);
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<INotableDateService>();
        Console.WriteLine($"Resolved service type: {service.GetType().Name}");

        var anzac = service.Resolve(new DateOnly(2026, 4, 25), "AU");
        Console.WriteLine($"AU 2026-04-25: {string.Join(", ", anzac.Select(n => n.DisplayName))}");

        // ------------------------------------------------------------------------------------------------
        // Durable backends ship as add-on packages, each with its own one-line registration that slots in
        // where the in-memory default sits:
        //
        //   // Bodu.Globalization.Calendar.Caching.Sqlite - a single-file durable cache:
        //   services.AddSqliteNotableDateCache(options => options.DatabasePath = "calendar-cache.db");
        //
        //   // Bodu.Globalization.Calendar.Caching.Distributed - any IDistributedCache (e.g. Redis):
        //   services.AddDistributedNotableDateCache();
        //   services.AddRedisNotableDateCache("localhost:6379");
        //
        // Both are omitted here to keep the sample dependency-free and offline.
        // ------------------------------------------------------------------------------------------------

        Console.WriteLine();
    }
}
