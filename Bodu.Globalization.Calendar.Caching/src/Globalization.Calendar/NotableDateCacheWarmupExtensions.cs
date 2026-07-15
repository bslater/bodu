// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWarmupExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides <see cref="IServiceCollection" /> registration of the startup notable-date cache warm-up.
/// </summary>
public static class NotableDateCacheWarmupExtensions
{
    /// <summary>The default configuration section bound into <see cref="NotableDateCacheWarmupOptions" />.</summary>
    private const string DefaultWarmupSection = "Calendar:NotableDateCacheWarmup";

    /// <summary>
    /// Registers a hosted service that warms the caching notable-date service at application start, resolving the
    /// configured territories over the configured span of civil years through the normal read-through path so the first
    /// user request never pays the year computation.
    /// </summary>
    /// <param name="services">The service collection to modify.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="NotableDateCacheWarmupOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <c>Calendar:NotableDateCacheWarmup</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The warm-up runs in the background after the host starts and never blocks or crashes it: a failing territory is
    /// logged and skipped, and host shutdown cancels the run between territories. Register it after
    /// <c>AddCachedNotableDateService</c>; when the resolved <see cref="INotableDateService" /> is not the caching
    /// decorator the run logs the misconfiguration and no-ops. Options are validated at application start.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
    /// builder.Services.AddCachedNotableDateService();
    /// builder.Services.AddNotableDateCacheWarmup(configure: warmup =>
    /// {
    ///     warmup.Territories.Add("US");
    ///     warmup.Territories.Add("US-CA");
    ///     warmup.YearsAhead = 2;
    /// });
    ///]]>
    /// </code>
    /// </example>
    public static IServiceCollection AddNotableDateCacheWarmup(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = DefaultWarmupSection,
        Action<NotableDateCacheWarmupOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        OptionsBuilder<NotableDateCacheWarmupOptions> optionsBuilder = services.AddOptions<NotableDateCacheWarmupOptions>();

        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));

        if (configure is not null)
            optionsBuilder.Configure(configure);

        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "Notable-date cache warm-up options are invalid.")
            .ValidateOnStart();

        services.AddHostedService<NotableDateCacheWarmupService>();

        return services;
    }
}
