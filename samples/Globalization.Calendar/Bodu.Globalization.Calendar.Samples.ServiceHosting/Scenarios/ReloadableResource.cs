// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableResource.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.Samples.ServiceHosting.Scenarios;

/// <summary>
/// Demonstrates the reloadable registration: <c>AddReloadableNotableDateService</c> serves queries
/// through a swappable resource provider, so rule data can be replaced at run time — a rules refresh,
/// a tenant switch, a hot configuration reload — without restarting the host or re-resolving the
/// service. Consumers keep their <see cref="INotableDateService" /> reference; only the data moves.
/// </summary>
public static class ReloadableResource
{
    /// <summary>
    /// Registers a reloadable service over the AU pack and swaps it to NZ mid-run.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AddReloadableNotableDateService - swapping data without a restart",
            what: "Registers a reloadable service over the Australian pack, resolves it once as a consumer "
                + "would, swaps the underlying resource to New Zealand through the mutable provider, and queries "
                + "the same held reference again.",
            why: "Rule data changes - a jurisdiction legislates a new holiday, a tenant moves, a rules pack is "
                + "refreshed on a schedule - and restarting a host to pick that up is a poor trade for a change "
                + "that is pure data. The difficulty is that consumers hold their injected singleton, so "
                + "replacing the registration would not reach them. The indirection solves that: queries go "
                + "through a swappable provider, so the reference a consumer captured at construction keeps "
                + "working and simply starts serving the new data. Operations drives the swap through a separate "
                + "mutable type rather than through the query interface, which keeps the ability to mutate out "
                + "of the hands of everything that only reads.",
            expect: "The same service reference answers for Australia before the reload and New Zealand after "
                + "it - no re-resolution, no restart, and nothing the consumer had to do. The New Zealand pack "
                + "then answers a question the Australian one could not.");

        var services = new ServiceCollection();
        services.AddReloadableNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

        using ServiceProvider provider = services.BuildServiceProvider();

        // The consumer resolves the service once and holds it - as any injected singleton would be.
        var service = provider.GetRequiredService<INotableDateService>();
        Console.WriteLine($"  Initial resource : {service.GetSupportedTerritories().First()} "
            + $"({service.Resolve(2024, "AU").Count} notable dates in 2024)");

        // Operations resolve the mutable provider and swap the resource - e.g. from a rules
        // refresh job. The held service reference immediately serves the new data.
        var mutable = provider.GetRequiredService<MutableNotableDateResourceProvider>();
        mutable.Reload(AsiaPacificCalendarData.LoadResource("NZ"));

        Console.WriteLine($"  After Reload     : {service.GetSupportedTerritories().First()} "
            + $"({service.Resolve(2024, "NZ").Count} notable dates in 2024)"
            + "  (the same reference the consumer captured at construction - no re-resolution and no restart)");

        // The NZ pack answers NZ questions now - Waitangi Day exists, Anzac Day is shared.
        var waitangi = service.Resolve(new DateOnly(2024, 2, 6), "NZ");
        Console.WriteLine($"  2024-02-06 in NZ : {string.Join(", ", waitangi.Select(d => d.DisplayName))}"
            + "  (a question the Australian pack could not have answered - the data really was replaced)");

        Console.WriteLine();
    }
}
