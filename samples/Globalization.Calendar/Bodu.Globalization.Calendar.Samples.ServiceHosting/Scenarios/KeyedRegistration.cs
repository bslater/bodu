// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedRegistration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.Samples.ServiceHosting.Scenarios;

/// <summary>
/// Demonstrates the keyed and options registrations: a multi-tenant process registers one
/// <see cref="INotableDateService" /> per jurisdiction and resolves them by key, and the
/// <see cref="NotableDateServiceOptions" /> overload composes collaborators — here a custom algorithm
/// registry — through the container.
/// </summary>
public static class KeyedRegistration
{
    /// <summary>
    /// Registers per-jurisdiction keyed services plus an options-composed service, and consumes them by key.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AddNotableDateService(key, ...): keyed multi-jurisdiction registration ---");

        var services = new ServiceCollection();

        // One keyed singleton per jurisdiction; a tenant-aware consumer asks for its own calendar with
        // [FromKeyedServices("AU")] or GetRequiredKeyedService.
        services.AddNotableDateService("AU", AsiaPacificCalendarData.LoadResource("AU"));
        services.AddNotableDateService("NZ", AsiaPacificCalendarData.LoadResource("NZ"));

        // The options overload wires collaborators through the container. Here the unkeyed service
        // resolves a custom algorithm alongside the built-ins.
        var registry = new NotableDateAlgorithmRegistry()
            .Register("company-day", new DelegateSampleAlgorithm());

        services.AddNotableDateService(
            AsiaPacificCalendarData.LoadResource("AU"),
            new NotableDateServiceOptions { Algorithms = registry });

        using ServiceProvider provider = services.BuildServiceProvider();

        var au = provider.GetRequiredKeyedService<INotableDateService>("AU");
        var nz = provider.GetRequiredKeyedService<INotableDateService>("NZ");

        Console.WriteLine($"AU keyed service: {au.Resolve(new DateOnly(2026, 4, 25), "AU").Count} occurrence(s) on Anzac Day");
        Console.WriteLine($"NZ keyed service: {nz.Resolve(new DateOnly(2026, 2, 6), "NZ").Count} occurrence(s) on Waitangi Day");

        // Registrations are idempotent (TryAdd semantics): registering the same key again is a no-op,
        // so composition roots can be layered without accidental replacement.
        services.AddNotableDateService("AU", AsiaPacificCalendarData.LoadResource("NZ"));
        Console.WriteLine("Second AU registration ignored: first registration wins.");

        Console.WriteLine();
    }

    /// <summary>A sample algorithm resolving a fixed company day.</summary>
    private sealed class DelegateSampleAlgorithm : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 9, 1);
    }
}
