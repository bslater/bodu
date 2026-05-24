// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.PostConfigure.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.DependencyInjection;

public partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// A consumer-defined POCO used to exercise the <c>PostConfigure</c> projection hook end-to-end.
    /// </summary>
    private sealed class CustomCalendarSettings
    {
        /// <summary>
        /// Gets or sets the region code the consumer wants projected into
        /// <see cref="NotableDateOptions.DefaultTerritoryCode" />.
        /// </summary>
        public string? RegionCode { get; set; }

        /// <summary>
        /// Gets or sets the working-week preset the consumer wants projected into
        /// <see cref="NotableDateOptions.WorkingDays" />.
        /// </summary>
        public WorkingDaysOfWeek WorkWeek { get; set; } = WorkingDaysOfWeek.MondayToFriday;
    }

    /// <summary>
    /// Verifies that <c>PostConfigure</c> throws when the builder is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void PostConfigure_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateServiceBuilderExtensions.PostConfigure(null!, (_, _) => { });
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>PostConfigure</c> throws when the configure callback is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void PostConfigure_WhenCallbackIsNull_ShouldThrowArgumentNullException()
    {
        (_, INotableDateServiceBuilder builder) = NewBuilder();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.PostConfigure(null!);
        });

        Assert.AreEqual("configure", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>PostConfigure</c> projects values from a consumer-defined POCO into
    /// <see cref="NotableDateOptions" /> after the binding stage has completed.
    /// </summary>
    [TestMethod]
    public void PostConfigure_WhenProjectingFromCustomPoco_ShouldOverlayValuesIntoNotableDateOptions()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MyApp:Calendar:RegionCode"] = "AU-NSW",
                ["MyApp:Calendar:WorkWeek"] = "MondayToSaturday",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.Configure<CustomCalendarSettings>(configuration.GetSection("MyApp:Calendar"));
        services.AddNotableDates()
            .PostConfigure((sp, opts) =>
            {
                CustomCalendarSettings custom = sp.GetRequiredService<IOptions<CustomCalendarSettings>>().Value;
                opts.DefaultTerritoryCode = custom.RegionCode;
                opts.WorkingDays = custom.WorkWeek;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual("AU-NSW", resolved.DefaultTerritoryCode);
        Assert.AreEqual(WorkingDaysOfWeek.MondayToSaturday, resolved.WorkingDays);
    }

    /// <summary>
    /// Verifies that <c>PostConfigure</c> runs after both <see cref="IConfiguration" /> binding and
    /// <c>Configure(...)</c> callbacks, so post-configure callbacks always win on the same property.
    /// </summary>
    [TestMethod]
    public void PostConfigure_WhenCombinedWithBindAndConfigure_ShouldWinOnOverlappingProperty()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotableDates:DefaultTerritoryCode"] = "FromConfig",
            })
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddNotableDates(configuration)
            .Configure(opts => opts.DefaultTerritoryCode = "FromConfigure")
            .PostConfigure((_, opts) => opts.DefaultTerritoryCode = "FromPostConfigure");

        using ServiceProvider provider = services.BuildServiceProvider();
        NotableDateOptions resolved = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

        Assert.AreEqual("FromPostConfigure", resolved.DefaultTerritoryCode);
    }

    /// <summary>
    /// Verifies that <c>PostConfigure</c> receives the host's <see cref="IServiceProvider" /> so that the callback can
    /// resolve arbitrary services.
    /// </summary>
    [TestMethod]
    public void PostConfigure_WhenInvoked_ShouldReceiveHostServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<MarkerService>();
        services.AddNotableDates()
            .PostConfigure((sp, _) =>
            {
                MarkerService marker = sp.GetRequiredService<MarkerService>();
                marker.Observed = true;
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;
        MarkerService marker = provider.GetRequiredService<MarkerService>();

        Assert.IsTrue(marker.Observed);
    }

    /// <summary>
    /// A marker class used by <c>PostConfigure</c> tests to confirm the callback received an
    /// <see cref="IServiceProvider" />.
    /// </summary>
    private sealed class MarkerService
    {
        /// <summary>
        /// Gets or sets a value indicating whether the marker has been observed by a post-configure callback.
        /// </summary>
        public bool Observed { get; set; }
    }
}
