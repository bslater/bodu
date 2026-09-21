// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BasicRegistration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.Samples.ServiceHosting.Scenarios;

/// <summary>
/// Demonstrates the singleton registration: <c>AddNotableDateService(resource)</c> registers an
/// immutable <see cref="INotableDateService" /> that consumers take by constructor injection — the
/// resource loads once, and every consumer shares the thread-safe service.
/// </summary>
public static class BasicRegistration
{
    /// <summary>
    /// Registers a data-pack resource and consumes the service from the container.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AddNotableDateService - singleton registration",
            what: "Registers a loaded data-pack resource in a service collection, builds the provider, resolves "
                + "the service by interface, and uses it for a holiday count and a payday calculation.",
            why: "The registration is a singleton because the service is immutable and thread-safe, which makes "
                + "loading the resource once per process the right default rather than an optimisation - parsing "
                + "a rule pack per request would be real work for an answer that cannot change. Consumers depend "
                + "on the interface rather than the concrete type, so a test can substitute a fixed calendar and "
                + "the working-day extensions take the service as a parameter rather than reaching for a static, "
                + "which is what keeps date arithmetic testable.",
            expect: "One registration line, and everything downstream takes the service by injection. The payday "
                + "snaps backward off the holiday, which is the behaviour a pay run needs - paying late is a "
                + "different kind of wrong from paying early.");

        var services = new ServiceCollection();

        // Instance form: load the resource at composition time. The factory overload
        // (AddNotableDateService(sp => ...)) defers loading until first resolution instead.
        services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

        using ServiceProvider provider = services.BuildServiceProvider();

        // Consumers depend on INotableDateService - here, a payroll-flavoured use of the service
        // plus the working-day extensions that take it as a parameter.
        var service = provider.GetRequiredService<INotableDateService>();

        var holidays = service.Resolve(2024, "AU", NotableDateFilter.IsNonWorkingDay());
        Console.WriteLine($"  AU 2024 non-working notable dates: {holidays.Count}"
            + "  (resolved by interface - the resource was parsed once at composition, not per query)");

        DateOnly payday = new DateOnly(2024, 4, 25).SnapToWorkingDayBackward(service, "AU");
        Console.WriteLine($"  Payday falling on Anzac Day pays on: {payday:yyyy-MM-dd} ({payday.DayOfWeek})"
            + "  (snapped backward, not forward - for a pay run, late is a different kind of wrong from early)");

        Console.WriteLine();
    }
}
