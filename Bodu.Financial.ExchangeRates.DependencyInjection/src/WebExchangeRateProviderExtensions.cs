// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Financial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides the shared registration machinery for web-based exchange-rate providers on an
/// <see cref="IFinancialServiceBuilder" />.
/// </summary>
/// <remarks>
/// <para>
/// Each provider's dependency-injection package calls one of the <c>AddWebExchangeRateProvider</c> overloads to handle
/// the common plumbing: binding and validating the provider's options, configuring a named <see cref="HttpClient" />
/// with a Polly standard-resilience handler, constructing the provider singleton, and exposing it as both
/// <see cref="IDatedExchangeRateProvider" /> and <see cref="IExchangeRateProvider" />. The provider-specific extension
/// method supplies only the values that differ across providers: the HttpClient name, configuration section, validation
/// predicate, user-agent and timeout selectors, and the factory delegate that invokes the concrete constructor.
/// </para>
/// <para>
/// The two public overloads differ by options-type constraint. Use the short overload when <c>TOptions</c> derives from
/// <see cref="WebExchangeRateProviderOptions" /> — it reads <see cref="WebExchangeRateProviderOptions.UserAgent" />,
/// <see cref="WebExchangeRateProviderOptions.HttpTimeout" />, and
/// <see cref="WebExchangeRateProviderOptions.TryValidate" /> automatically. Use the full overload when the options type
/// does not inherit from <see cref="WebExchangeRateProviderOptions" /> and the caller must supply explicit selectors.
/// </para>
/// </remarks>
public static class WebExchangeRateProviderExtensions
{
    /// <summary>
    /// Registers a web-based exchange-rate provider whose options derive from
    /// <see cref="WebExchangeRateProviderOptions" />, binding options and configuring a named <see cref="HttpClient" />
    /// with Polly resilience.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The concrete provider type, derived from <see cref="WebExchangeRateProvider" />.
    /// </typeparam>
    /// <typeparam name="TOptions">
    /// The options type, derived from <see cref="WebExchangeRateProviderOptions" />.
    /// </typeparam>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="httpClientName">
    /// The name used to register and resolve the provider's <see cref="HttpClient" /> through
    /// <see cref="IHttpClientFactory" />.
    /// </param>
    /// <param name="configuration">
    /// An optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <typeparamref name="TOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="validationErrorMessage">
    /// The error message reported by <c>ValidateOnStart</c> when
    /// <see cref="WebExchangeRateProviderOptions.TryValidate" /> returns <see langword="false" />.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <param name="configureResilience">
    /// An optional callback applied to the standard HTTP resilience options after the provider defaults have been set.
    /// </param>
    /// <param name="factory">
    /// A delegate that constructs the provider from an <see cref="HttpClient" />, the resolved options, an optional
    /// <see cref="ILoggerFactory" />, and an optional <see cref="TimeProvider" />.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    public static IFinancialServiceBuilder AddWebExchangeRateProvider<TProvider, TOptions>(
        this IFinancialServiceBuilder builder,
        string httpClientName,
        IConfiguration? configuration,
        string sectionName,
        string validationErrorMessage,
        Action<TOptions>? configure,
        Action<HttpStandardResilienceOptions>? configureResilience,
        Func<HttpClient, TOptions, ILoggerFactory?, TimeProvider?, TProvider> factory)
        where TProvider : WebExchangeRateProvider
        where TOptions : WebExchangeRateProviderOptions
        => AddWebExchangeRateProviderCore(
            builder,
            httpClientName,
            configuration,
            sectionName,
            static opts => opts.TryValidate(out _),
            validationErrorMessage,
            static opts => opts.UserAgent,
            static opts => opts.HttpTimeout,
            configure,
            configureResilience,
            factory);

    /// <summary>
    /// Registers a web-based exchange-rate provider with an arbitrary options type, binding options and configuring a
    /// named <see cref="HttpClient" /> with Polly resilience.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The concrete provider type, derived from <see cref="WebExchangeRateProvider" />.
    /// </typeparam>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="httpClientName">
    /// The name used to register and resolve the provider's <see cref="HttpClient" /> through
    /// <see cref="IHttpClientFactory" />.
    /// </param>
    /// <param name="configuration">
    /// An optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <typeparamref name="TOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="validateOptions">
    /// A predicate that returns <see langword="true" /> when the options are valid; used with
    /// <paramref name="validationErrorMessage" /> to wire <c>ValidateOnStart</c>.
    /// </param>
    /// <param name="validationErrorMessage">
    /// The error message reported by <c>ValidateOnStart</c> when <paramref name="validateOptions" /> returns
    /// <see langword="false" />.
    /// </param>
    /// <param name="getUserAgent">
    /// A selector that returns the <c>User-Agent</c> header value to apply to the named <see cref="HttpClient" />, or
    /// <see langword="null" /> / white-space to omit the header.
    /// </param>
    /// <param name="getHttpTimeout">
    /// A selector that returns the per-attempt HTTP timeout used to configure the Polly resilience handler.
    /// </param>
    /// <param name="configure">An optional callback applied after configuration binding.</param>
    /// <param name="configureResilience">
    /// An optional callback applied to the standard HTTP resilience options after the provider defaults have been set.
    /// </param>
    /// <param name="factory">
    /// A delegate that constructs the provider from an <see cref="HttpClient" />, the resolved options, an optional
    /// <see cref="ILoggerFactory" />, and an optional <see cref="TimeProvider" />.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    public static IFinancialServiceBuilder AddWebExchangeRateProvider<TProvider, TOptions>(
        this IFinancialServiceBuilder builder,
        string httpClientName,
        IConfiguration? configuration,
        string sectionName,
        Func<TOptions, bool> validateOptions,
        string validationErrorMessage,
        Func<TOptions, string?> getUserAgent,
        Func<TOptions, TimeSpan> getHttpTimeout,
        Action<TOptions>? configure,
        Action<HttpStandardResilienceOptions>? configureResilience,
        Func<HttpClient, TOptions, ILoggerFactory?, TimeProvider?, TProvider> factory)
        where TProvider : WebExchangeRateProvider
        where TOptions : class
        => AddWebExchangeRateProviderCore(
            builder,
            httpClientName,
            configuration,
            sectionName,
            validateOptions,
            validationErrorMessage,
            getUserAgent,
            getHttpTimeout,
            configure,
            configureResilience,
            factory);

    /// <summary>
    /// Core implementation shared by all public overloads: binds options, configures the named
    /// <see cref="HttpClient" /> with Polly resilience, registers the provider singleton, and exposes it through both
    /// provider interfaces.
    /// </summary>
    /// <typeparam name="TProvider">The concrete provider type.</typeparam>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="builder">The financial service builder.</param>
    /// <param name="httpClientName">The named <see cref="HttpClient" /> identifier.</param>
    /// <param name="configuration">Optional configuration root or section for options binding.</param>
    /// <param name="sectionName">Configuration section name for options binding.</param>
    /// <param name="validateOptions">Predicate used to validate the bound options.</param>
    /// <param name="validationErrorMessage">Error message emitted when validation fails.</param>
    /// <param name="getUserAgent">Selector for the User-Agent header value.</param>
    /// <param name="getHttpTimeout">Selector for the per-attempt HTTP timeout.</param>
    /// <param name="configure">Optional post-binding options mutation.</param>
    /// <param name="configureResilience">Optional Polly resilience tuning callback.</param>
    /// <param name="factory">Factory delegate that constructs the provider.</param>
    /// <returns>The builder, for chaining.</returns>
    private static IFinancialServiceBuilder AddWebExchangeRateProviderCore<TProvider, TOptions>(
        IFinancialServiceBuilder builder,
        string httpClientName,
        IConfiguration? configuration,
        string sectionName,
        Func<TOptions, bool> validateOptions,
        string validationErrorMessage,
        Func<TOptions, string?> getUserAgent,
        Func<TOptions, TimeSpan> getHttpTimeout,
        Action<TOptions>? configure,
        Action<HttpStandardResilienceOptions>? configureResilience,
        Func<HttpClient, TOptions, ILoggerFactory?, TimeProvider?, TProvider> factory)
        where TProvider : WebExchangeRateProvider
        where TOptions : class
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        IServiceCollection services = builder.Services;

        OptionsBuilder<TOptions> optionsBuilder = services.AddOptions<TOptions>();
        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));
        if (configure is not null)
            optionsBuilder.Configure(configure);
        optionsBuilder
            .Validate(validateOptions, validationErrorMessage)
            .ValidateOnStart();

        services
            .AddHttpClient(httpClientName, (serviceProvider, client) =>
            {
                TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;
                client.Timeout = Timeout.InfiniteTimeSpan;
                var userAgent = getUserAgent(options);
                if (!string.IsNullOrWhiteSpace(userAgent))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            })
            .AddStandardResilienceHandler()
            .Configure((resilienceOptions, serviceProvider) =>
            {
                TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;
                ConfigureResilienceTimeouts(resilienceOptions, getHttpTimeout(options));
                configureResilience?.Invoke(resilienceOptions);
            });

        services.TryAddSingleton(serviceProvider =>
        {
            HttpClient client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName);
            TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;
            return factory(
                client,
                options,
                serviceProvider.GetService<ILoggerFactory>(),
                serviceProvider.GetService<TimeProvider>());
        });
        services.TryAddSingleton<IDatedExchangeRateProvider>(serviceProvider => serviceProvider.GetRequiredService<TProvider>());
        services.TryAddSingleton<IExchangeRateProvider>(serviceProvider => serviceProvider.GetRequiredService<TProvider>());

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
