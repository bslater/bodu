// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWarmupExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides registration of the startup cache warm-up onto an <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class RateCacheWarmupExtensions
{
    /// <summary>The default configuration section bound into <see cref="RateCacheWarmupOptions" />.</summary>
    private const string DefaultWarmupSection = "Financial:RateCacheWarmup";

    /// <summary>
    /// Registers a hosted service that warms every registered caching rate provider at application start, fetching the
    /// configured pairs over the configured window through the normal read-through path so the first user request never
    /// pays the origin fetch.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configuration">
    /// An optional configuration root or section bound into <see cref="RateCacheWarmupOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Financial:RateCacheWarmup</c>.</param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The warm-up runs in the background after the host starts and never blocks or crashes it: a failing pair is
    /// logged and skipped, and host shutdown cancels the run. It warms every provider registered through
    /// <c>AddCachedRateProvider</c> automatically; aggregation children registered through
    /// <c>AddAggregatedRateProvider</c> are keyed services and are warmed only when named in
    /// <see cref="RateCacheWarmupOptions.Providers" />. Options are validated at application start.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// services.AddFinancialService()
    ///         .AddRbaExchangeRates(configuration)
    ///         .AddCachedRateProvider<RbaRateProvider>("RBA", configuration)
    ///         .AddRateCacheWarmup(configure: warmup =>
    ///         {
    ///             warmup.Pairs.Add("AUD/USD");
    ///             warmup.Pairs.Add("AUD/EUR");
    ///             warmup.LookbackDays = 90;
    ///         });
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddRateCacheWarmup(
        this IFinancialServiceBuilder builder,
        IConfiguration? configuration = null,
        string sectionName = DefaultWarmupSection,
        Action<RateCacheWarmupOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        OptionsBuilder<RateCacheWarmupOptions> optionsBuilder = builder.Services.AddOptions<RateCacheWarmupOptions>();

        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));

        if (configure is not null)
            optionsBuilder.Configure(configure);

        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "Rate cache warm-up options are invalid.")
            .ValidateOnStart();

        builder.Services.AddHostedService<RateCacheWarmupService>();

        return builder;
    }
}
