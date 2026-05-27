// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegrationTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Verifies the end-to-end composition path of the DI package — registering an Asia-Pacific data pack, querying
/// notable dates, enumerating supported territories, and exercising the runtime-mutable override workflow against the
/// resolved service.
/// </summary>
[TestClass]
public sealed class IntegrationTests
{
    /// <summary>
    /// Verifies that registering the Asia-Pacific data pack surfaces an Australian holiday for the New South Wales
    /// territory in the resolved service.
    /// </summary>
    [TestMethod]
    public void EndToEnd_WhenAsiaPacificDataPackRegistered_ShouldResolveAuNswHolidays()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates()
            .AddRuleProviders(AsiaPacificCalendarData.CreateProviders());

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyList<NotableDate> dates2026 = service.GetNotableDates(2026, territoryCode: "AU-NSW");

        Assert.IsNotEmpty(dates2026, "Expected at least one notable date for AU-NSW in 2026.");
        Assert.Contains(n => n.Date == new DateTime(2026, 1, 1), dates2026, "Expected New Year's Day in the AU-NSW set for 2026.");
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.GetSupportedTerritories" /> exposes Asia-Pacific country codes
    /// after the data pack is registered.
    /// </summary>
    [TestMethod]
    public void EndToEnd_WhenAsiaPacificDataPackRegistered_ShouldReportSupportedTerritories()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates()
            .AddRuleProviders(AsiaPacificCalendarData.CreateProviders());

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        IReadOnlyCollection<string> territories = service.GetSupportedTerritories();

        Assert.Contains(t => t.StartsWith("AU", StringComparison.OrdinalIgnoreCase), territories, "Expected at least one AU-* territory.");
        Assert.Contains(t => t.StartsWith("NZ", StringComparison.OrdinalIgnoreCase), territories, "Expected at least one NZ-* territory.");
    }

    /// <summary>
    /// Verifies that a <see cref="MutableNotableDateRuleOverrideProvider" /> registered through the builder applies
    /// runtime mutations to the resolved service end-to-end.
    /// </summary>
    [TestMethod]
    public void EndToEnd_WhenMutableOverrideAddsRuleAtRuntime_ShouldReflectChangeInService()
    {
        MutableNotableDateRuleOverrideProvider overrides = new();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates()
            .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
            .AddOverrideProvider(overrides);

        using ServiceProvider provider = services.BuildServiceProvider();
        INotableDateService service = provider.GetRequiredService<INotableDateService>();

        Assert.DoesNotContain(n => n.Name == "Company Day", service.GetNotableDates(2026, "AU-NSW"));

        overrides.AddRule(new NotableDateRule
        {
            Name = "Company Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Observance,
            Month = 6,
            Day = 15,
            IsNonWorkingDay = true,
            TerritoryCode = "AU-NSW",
        });

        Assert.Contains(n => n.Name == "Company Day" && n.Date == new DateTime(2026, 6, 15), service.GetNotableDates(2026, "AU-NSW"));
    }
}
