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
        SampleConsole.Scenario(
            "Keyed and options registration - one process, many jurisdictions",
            what: "Registers one service per jurisdiction under a key, registers an unkeyed service composed "
                + "with a custom algorithm registry, resolves two of them by key, and re-registers an existing "
                + "key to show the result.",
            why: "A multi-tenant process needs several calendars at once, and the alternative to keys is a "
                + "factory the consuming code has to know about - which pushes tenant awareness into every class "
                + "that needs a date. Keyed resolution keeps that in the composition root. The options overload "
                + "exists for the other axis: a service may need collaborators, such as an algorithm registry "
                + "carrying rules the built-in catalogue does not have, and composing those through the "
                + "container keeps them replaceable. The registrations use TryAdd semantics deliberately, so "
                + "layered composition roots - a library registering its defaults, then an application "
                + "overriding them - do not silently clobber each other in registration order.",
            expect: "Each keyed service answers for its own jurisdiction. The second registration under an "
                + "existing key is ignored rather than replacing the first, which is what makes registration "
                + "order stop mattering.");

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

        Console.WriteLine($"  AU keyed service: {au.Resolve(new DateOnly(2026, 4, 25), "AU").Count} occurrence(s) on Anzac Day"
            + "  (resolved by key, so tenant awareness stays in the composition root rather than in every consumer)");
        Console.WriteLine($"  NZ keyed service: {nz.Resolve(new DateOnly(2026, 2, 6), "NZ").Count} occurrence(s) on Waitangi Day"
            + "  (a different jurisdiction in the same process, with no shared state between them)");

        // Registrations are idempotent (TryAdd semantics): registering the same key again is a no-op,
        // so composition roots can be layered without accidental replacement.
        services.AddNotableDateService("AU", AsiaPacificCalendarData.LoadResource("NZ"));
        Console.WriteLine("  Second AU registration ignored: first registration wins."
            + "  (TryAdd semantics - a library registering defaults and an application overriding them cannot clobber each other by ordering)");

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
