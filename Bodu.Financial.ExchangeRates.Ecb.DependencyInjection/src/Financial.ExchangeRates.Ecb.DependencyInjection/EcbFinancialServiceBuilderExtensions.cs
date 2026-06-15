// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbFinancialServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Ecb.DependencyInjection;

/// <summary>
/// Provides the fluent registration of the ECB euro reference-rate provider onto an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class EcbFinancialServiceBuilderExtensions
{
    /// <summary>
    /// The name of the <see cref="HttpClient" /> configured for ECB downloads.
    /// </summary>
    public const string HttpClientName = "Bodu.Financial.ExchangeRates.Ecb";

    /// <summary>
    /// Registers the ECB euro reference-rate provider, binding its options and configuring a named
    /// <see cref="HttpClient" /> for downloads.
    /// </summary>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="configuration">
    /// An optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <see cref="EcbExchangeRateOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <c>Financial:Ecb</c>.</param>
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
    /// The provider is registered as a singleton so its in-memory store of loaded feeds is shared across resolutions;
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
    /// from <see cref="EcbEndpointOptions.HttpTimeout" />, the <see cref="HttpClient.Timeout" /> is set to
    /// <see cref="Timeout.InfiniteTimeSpan" /> so the two timeout mechanisms do not compete.
    /// </para>
    /// </remarks>
    public static IFinancialServiceBuilder AddEcbReferenceRates(
        this IFinancialServiceBuilder builder,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Ecb",
        Action<EcbExchangeRateOptions>? configure = null,
        Action<HttpStandardResilienceOptions>? configureResilience = null)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<EcbExchangeRateOptions> optionsBuilder = services.AddOptions<EcbExchangeRateOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);
        optionsBuilder
            .Validate(static options => options.TryValidate(out _), "ECB exchange-rate options are invalid.")
            .ValidateOnStart();

        services
            .AddHttpClient(HttpClientName, static (serviceProvider, client) =>
            {
                EcbExchangeRateOptions options = serviceProvider.GetRequiredService<IOptions<EcbExchangeRateOptions>>().Value;
                client.Timeout = Timeout.InfiniteTimeSpan;
                if (!string.IsNullOrWhiteSpace(options.Endpoint.UserAgent))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.Endpoint.UserAgent);
            })
            .AddStandardResilienceHandler()
            .Configure((resilienceOptions, serviceProvider) =>
            {
                EcbExchangeRateOptions options = serviceProvider.GetRequiredService<IOptions<EcbExchangeRateOptions>>().Value;
                ConfigureResilienceTimeouts(resilienceOptions, options.Endpoint.HttpTimeout);
                configureResilience?.Invoke(resilienceOptions);
            });

        services.TryAddSingleton(static serviceProvider =>
        {
            HttpClient client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            EcbExchangeRateOptions options = serviceProvider.GetRequiredService<IOptions<EcbExchangeRateOptions>>().Value;
            return new EcbExchangeRateProvider(
                client,
                options,
                serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<EcbExchangeRateProvider>(),
                serviceProvider.GetService<TimeProvider>());
        });
        services.TryAddSingleton<IDatedExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<EcbExchangeRateProvider>());
        services.TryAddSingleton<IExchangeRateProvider>(static serviceProvider => serviceProvider.GetRequiredService<EcbExchangeRateProvider>());

        return builder;
    }

    /// <summary>
    /// Aligns the standard resilience handler's timeouts and circuit-breaker sampling window with the configured
    /// per-attempt HTTP timeout so the pipeline passes its built-in validation.
    /// </summary>
    /// <param name="options">The standard resilience options to adjust.</param>
    /// <param name="attemptTimeout">The per-attempt HTTP timeout taken from the provider options.</param>
    /// <remarks>
    /// The standard handler requires the total-request timeout to exceed the per-attempt timeout and the circuit
    /// breaker's sampling duration to be at least double the per-attempt timeout. The attempt timeout is set from
    /// <paramref name="attemptTimeout" />, the total-request timeout to three times that value to leave room for
    /// retries, and the sampling duration is widened to at least twice the attempt timeout when the default is smaller.
    /// </remarks>
    private static void ConfigureResilienceTimeouts(HttpStandardResilienceOptions options, TimeSpan attemptTimeout)
    {
        options.AttemptTimeout.Timeout = attemptTimeout;
        options.TotalRequestTimeout.Timeout = attemptTimeout * 3;

        TimeSpan minimumSamplingDuration = attemptTimeout * 2;
        if (options.CircuitBreaker.SamplingDuration < minimumSamplingDuration)
            options.CircuitBreaker.SamplingDuration = minimumSamplingDuration;
    }
}
