// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.DependencyInjection;

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

    /// <summary>
    /// Verifies that <see cref="IConfiguration" />-bound boolean values for
    /// <see cref="NotableDateOptions.RegisterAsAmbientDefault" /> are accepted across the case forms supported by
    /// the configuration binder.
    /// </summary>
    /// <param name="value">The string value in the configuration.</param>
    /// <param name="expected">The bound boolean value.</param>
    [TestMethod]
    [DataRow("true", true)]
    [DataRow("True", true)]
    [DataRow("TRUE", true)]
    [DataRow("false", false)]
    [DataRow("False", false)]
    public void Binding_WhenAmbientDefaultFlagSupplied_ShouldHonourEveryCaseForm(string value, bool expected)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotableDates:RegisterAsAmbientDefault"] = value,
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual(expected, resolved.RegisterAsAmbientDefault);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOptions" /> setters accept and persist mutations after construction — the
    /// type is a plain mutable POCO and supports programmatic configuration via <c>Configure(opts =&gt; …)</c>.
    /// </summary>
    [TestMethod]
    public void Properties_WhenMutatedAfterConstruction_ShouldPersistAssignedValues()
    {
        NotableDateOptions options = new();

        options.WorkingDays = WorkingDaysOfWeek.MondayToSaturday;
        options.DefaultTerritoryCode = "AU-NSW";
        options.DefaultCalendarTypeName = "System.Globalization.GregorianCalendar";
        options.RegisterAsAmbientDefault = true;

        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday, options.WorkingDays);
        Assert.AreEqual("AU-NSW", options.DefaultTerritoryCode);
        Assert.AreEqual("System.Globalization.GregorianCalendar", options.DefaultCalendarTypeName);
        Assert.IsTrue(options.RegisterAsAmbientDefault);
    }
}
