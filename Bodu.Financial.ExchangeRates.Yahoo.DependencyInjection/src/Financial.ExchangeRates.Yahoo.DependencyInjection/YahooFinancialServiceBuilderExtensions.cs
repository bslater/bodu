// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooFinancialServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection;

/// <summary>
/// Provides the fluent registration of the Yahoo Finance exchange-rate provider onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class YahooFinancialServiceBuilderExtensions
{
    /// <summary>
    /// The name of the <see cref="HttpClient" /> configured for Yahoo Finance requests.
    /// </summary>
    public const string HttpClientName = "Bodu.Financial.ExchangeRates.Yahoo";

    /// <summary>
    /// Registers the Yahoo Finance exchange-rate provider, binding its options and configuring a named
    /// <see cref="HttpClient" /> for chart requests.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configuration">
    /// An optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <see cref="YahooExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Financial:Yahoo</c>.</param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// The provider is registered as a singleton so its in-memory store of fetched pairs is shared across resolutions;
    /// it is backed by an <see cref="IHttpClientFactory" /> client so handler lifetime is managed by the factory. The
    /// provider is also exposed as <see cref="IDatedExchangeRateProvider" /> and <see cref="IExchangeRateProvider" />
    /// through idempotent registrations.
    /// </remarks>
    public static IFinancialServiceBuilder AddYahooExchangeRates(
        this IFinancialServiceBuilder builder,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Yahoo",
        Action<YahooExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<YahooExchangeRateOptions> optionsBuilder = services.AddOptions<YahooExchangeRateOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);
        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "Yahoo exchange-rate options are invalid.")
            .ValidateOnStart();

        services.AddHttpClient(HttpClientName, static (serviceProvider, client) =>
        {
            YahooExchangeRateOptions options = serviceProvider.GetRequiredService<IOptions<YahooExchangeRateOptions>>().Value;
            client.Timeout = options.HttpTimeout;
            if (!string.IsNullOrWhiteSpace(options.UserAgent))
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
        });

        services.TryAddSingleton(static serviceProvider =>
        {
            HttpClient client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            YahooExchangeRateOptions options = serviceProvider.GetRequiredService<IOptions<YahooExchangeRateOptions>>().Value;
            return new YahooExchangeRateProvider(
                client,
                options,
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<YahooExchangeRateProvider>(),
                serviceProvider.GetService<TimeProvider>());
        });
        services.TryAddSingleton<IDatedExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<YahooExchangeRateProvider>());
        services.TryAddSingleton<IExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<YahooExchangeRateProvider>());

        return builder;
    }
}
