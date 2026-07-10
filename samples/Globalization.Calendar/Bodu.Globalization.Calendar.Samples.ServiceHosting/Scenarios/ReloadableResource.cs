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
        Console.WriteLine("--- AddReloadableNotableDateService: swap data without a restart ---");

        var services = new ServiceCollection();
        services.AddReloadableNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

        using ServiceProvider provider = services.BuildServiceProvider();

        // The consumer resolves the service once and holds it - as any injected singleton would be.
        var service = provider.GetRequiredService<INotableDateService>();
        Console.WriteLine($"Initial resource : {service.GetSupportedTerritories().First()} " +
            $"({service.Resolve(2024, "AU").Count} notable dates in 2024)");

        // Operations resolve the mutable provider and swap the resource - e.g. from a rules
        // refresh job. The held service reference immediately serves the new data.
        var mutable = provider.GetRequiredService<MutableNotableDateResourceProvider>();
        mutable.Reload(AsiaPacificCalendarData.LoadResource("NZ"));

        Console.WriteLine($"After Reload     : {service.GetSupportedTerritories().First()} " +
            $"({service.Resolve(2024, "NZ").Count} notable dates in 2024)");

        // The NZ pack answers NZ questions now - Waitangi Day exists, Anzac Day is shared.
        var waitangi = service.Resolve(new DateOnly(2024, 2, 6), "NZ");
        Console.WriteLine($"2024-02-06 in NZ : {string.Join(", ", waitangi.Select(d => d.DisplayName))}");

        Console.WriteLine();
    }
}
