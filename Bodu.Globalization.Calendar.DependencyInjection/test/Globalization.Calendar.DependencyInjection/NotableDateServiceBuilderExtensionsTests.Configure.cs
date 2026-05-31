// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.Configure.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.DependencyInjection;

public partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that <c>Configure</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Configure_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.Configure(null!, _ => { });
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Configure</c> throws when the configure callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Configure_WhenCallbackIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.Configure(null!);
        });

        Assert.AreEqual("configure", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Configure</c> applies its mutation to the materialised <see cref="NotableDateOptions" />.
    /// </summary>
    [TestMethod]
    public void Configure_WhenCallbackMutatesOptions_ShouldBeReflectedInResolvedOptions()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.Configure(opts =>
        {
            opts.DefaultTerritoryCode = "AU-NSW";
            opts.WorkingDays = WorkingDaysOfWeek.MondayToSaturday;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual("AU-NSW", resolved.DefaultTerritoryCode);
        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday, resolved.WorkingDays);
    }

    /// <summary>
    /// Verifies that successive <c>Configure</c> callbacks compose so that later mutations win on the same property.
    /// </summary>
    [TestMethod]
    public void Configure_WhenCalledMultipleTimes_ShouldComposeInRegistrationOrder()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder
            .Configure(opts => opts.DefaultTerritoryCode = "First")
            .Configure(opts => opts.DefaultTerritoryCode = "Last");

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual("Last", resolved.DefaultTerritoryCode);
    }

    /// <summary>
    /// Verifies that <c>UseWorkingDays</c> overrides any previously bound or configured working-days value.
    /// </summary>
    [TestMethod]
    public void UseWorkingDays_WhenInvokedAfterBinding_ShouldOverrideBoundValue()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotableDates:WorkingDays"] = "MondayToFriday",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration).UseWorkingDays(WorkingDaysOfWeek.MondayToSaturday);

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday, resolved.WorkingDays);
    }

    /// <summary>
    /// Verifies that <c>RegisterAsAmbientDefault</c> sets the bound flag on <see cref="NotableDateOptions" />.
    /// </summary>
    [TestMethod]
    public void RegisterAsAmbientDefault_WhenCalled_ShouldSetOptionsFlag()
    {
        (IServiceCollection services, INotableDateServiceBuilder builder) = NewBuilder();
        builder.RegisterAsAmbientDefault();

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.IsTrue(resolved.RegisterAsAmbientDefault);
    }
}
