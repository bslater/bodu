// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOptionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.DependencyInjection;

/// <summary>
/// Verifies the defaults and <see cref="IConfiguration" />-binding round-trip semantics of
/// <see cref="NotableDateOptions" />.
/// </summary>
[TestClass]
public sealed class NotableDateOptionsTests
{
    /// <summary>
    /// Verifies that a freshly constructed <see cref="NotableDateOptions" /> uses Monday–Friday as the default working
    /// week.
    /// </summary>
    [TestMethod]
    public void DefaultConstruction_ShouldUseMondayToFridayAsWorkingDays()
    {
        NotableDateOptions options = new();

        Assert.AreEqual(WorkingDaysOfWeek.MondayToFriday, options.WorkingDays);
    }

    /// <summary>
    /// Verifies that every optional property is <see langword="null" /> or <see langword="false" /> by default.
    /// </summary>
    [TestMethod]
    public void DefaultConstruction_ShouldHaveAllOptionalPropertiesUnset()
    {
        NotableDateOptions options = new();

        Assert.IsNull(options.DefaultTerritoryCode);
        Assert.IsNull(options.DefaultCalendarTypeName);
        Assert.IsFalse(options.RegisterAsAmbientDefault);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration" /> binding populates every property when each key is present.
    /// </summary>
    [TestMethod]
    public void Binding_WhenAllPropertiesProvided_ShouldPopulateEveryField()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotableDates:WorkingDays"] = "MondayToSaturday",
                ["NotableDates:DefaultTerritoryCode"] = "AU-NSW",
                ["NotableDates:DefaultCalendarTypeName"] = "System.Globalization.GregorianCalendar",
                ["NotableDates:RegisterAsAmbientDefault"] = "true",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday, resolved.WorkingDays);
        Assert.AreEqual("AU-NSW", resolved.DefaultTerritoryCode);
        Assert.AreEqual("System.Globalization.GregorianCalendar", resolved.DefaultCalendarTypeName);
        Assert.IsTrue(resolved.RegisterAsAmbientDefault);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration" /> binding from an empty section produces an options instance whose
    /// values match the defaults.
    /// </summary>
    [TestMethod]
    public void Binding_WhenSectionMissing_ShouldReturnDefaults()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual(WorkingDaysOfWeek.MondayToFriday, resolved.WorkingDays);
        Assert.IsNull(resolved.DefaultTerritoryCode);
        Assert.IsNull(resolved.DefaultCalendarTypeName);
        Assert.IsFalse(resolved.RegisterAsAmbientDefault);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration" /> binding tolerates a custom section name passed through
    /// <see cref="ServiceCollectionExtensions.AddNotableDates(IServiceCollection, IConfiguration?, string)" />.
    /// </summary>
    [TestMethod]
    public void Binding_WhenCustomSectionNameSupplied_ShouldBindFromCustomSection()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calendar:WorkingDays"] = "MondayToSaturday",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration, sectionName: "Calendar");

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday, resolved.WorkingDays);
    }
}
