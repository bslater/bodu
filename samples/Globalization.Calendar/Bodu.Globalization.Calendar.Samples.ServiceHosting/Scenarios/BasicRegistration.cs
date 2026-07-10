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
        Console.WriteLine("--- AddNotableDateService: singleton registration ---");

        var services = new ServiceCollection();

        // Instance form: load the resource at composition time. The factory overload
        // (AddNotableDateService(sp => ...)) defers loading until first resolution instead.
        services.AddNotableDateService(AsiaPacificCalendarData.LoadResource("AU"));

        using ServiceProvider provider = services.BuildServiceProvider();

        // Consumers depend on INotableDateService - here, a payroll-flavoured use of the service
        // plus the working-day extensions that take it as a parameter.
        var service = provider.GetRequiredService<INotableDateService>();

        var holidays = service.Resolve(2024, "AU", NotableDateFilter.IsNonWorkingDay());
        Console.WriteLine($"AU 2024 non-working notable dates: {holidays.Count}");

        DateOnly payday = new DateOnly(2024, 4, 25).SnapToWorkingDayBackward(service, "AU");
        Console.WriteLine($"Payday falling on Anzac Day pays on: {payday:yyyy-MM-dd} ({payday.DayOfWeek})");

        Console.WriteLine();
    }
}
