// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeFinancialServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the fluent registration of the Bank of England exchange-rate provider onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class BoeFinancialServiceBuilderExtensions
{
    /// <summary>The name of the <see cref="HttpClient" /> configured for Bank of England downloads.</summary>
    public const string HttpClientName = "Bodu.Financial.ExchangeRates.Boe";

    /// <summary>
    /// Registers the Bank of England exchange-rate provider, binding its options and configuring a named
    /// <see cref="HttpClient" /> for downloads.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configuration">
    /// An optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <see cref="BoeExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Financial:Boe</c>.</param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <param name="configureResilience">
    /// An optional callback applied to the standard HTTP resilience options after the provider defaults have been set,
    /// allowing the retry, timeout, and circuit-breaker behavior to be tuned or effectively disabled.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The provider is registered as a singleton so its in-memory store of loaded ranges is shared across resolutions;
    /// it is backed by an <see cref="IHttpClientFactory" /> client so handler lifetime is managed by the factory. The
    /// provider is also exposed as <see cref="IDatedExchangeRateProvider" /> and <see cref="IExchangeRateProvider" />
    /// through idempotent registrations.
    /// </para>
    /// <para>
    /// The named <see cref="HttpClient" /> is fitted with the standard Polly resilience handler (retry with exponential
    /// backoff and jitter, a per-attempt timeout, a total-request timeout, and a circuit breaker), enabled by default
    /// and tunable through <paramref name="configureResilience" />. The handler sits in the message-handler pipeline
    /// below the provider's single-flight load coordinator, so a retry re-issues the one in-flight request rather than
    /// multiplying across coalesced callers. Because the resilience handler enforces its own per-attempt timeout driven
    /// from <see cref="BoeEndpointOptions.HttpTimeout" />, the <see cref="HttpClient.Timeout" /> is set to
    /// <see cref="Timeout.InfiniteTimeSpan" /> so the two timeout mechanisms do not compete.
    /// </para>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IServiceCollection services = new ServiceCollection();
    ///
    /// services.AddFinancialService(builder => builder
    ///     .AddBoeReferenceRates(configuration, configure: opts => opts.Endpoint.HttpTimeout = TimeSpan.FromSeconds(15)));
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static IFinancialServiceBuilder AddBoeReferenceRates(
        this IFinancialServiceBuilder builder,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Boe",
        Action<BoeExchangeRateOptions>? configure = null,
        Action<HttpStandardResilienceOptions>? configureResilience = null)
        => builder.AddWebExchangeRateProvider<BoeExchangeRateProvider, BoeExchangeRateOptions>(
            HttpClientName,
            configuration,
            sectionName,
            static opts => opts.TryValidate(out _),
            "Bank of England exchange-rate options are invalid.",
            static opts => opts.Endpoint.UserAgent,
            static opts => opts.Endpoint.HttpTimeout,
            configure,
            configureResilience,
            static (client, opts, loggerFactory, timeProvider) =>
                new BoeExchangeRateProvider(client, opts, loggerFactory?.CreateLogger<BoeExchangeRateProvider>(), timeProvider));
}
