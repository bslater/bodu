// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Contains unit tests for <see cref="WebRateProviderExtensions" />, the shared registration machinery every
/// per-source exchange-rate provider package delegates to.
/// </summary>
/// <remarks>
/// The per-source packages each test their own <c>Add&lt;Source&gt;ExchangeRates</c> entry point, which exercises this
/// machinery transitively. These tests drive <see cref="WebRateProviderExtensions.AddWebRateProvider" /> directly over
/// a local test provider, so a change in shared behavior is attributed here rather than surfacing as a diffuse
/// failure across eleven provider suites.
/// </remarks>
[TestClass]
public partial class WebRateProviderExtensionsTests
{
    /// <summary>The named <see cref="HttpClient" /> registered by the tests in this class.</summary>
    private const string TestHttpClientName = "bodu-test-rates";

    /// <summary>The configuration section the tests bind options from.</summary>
    private const string TestSectionName = "TestRates";

    /// <summary>The message the tests register as the options-validation failure text.</summary>
    private const string TestValidationMessage = "Test rate provider options are invalid.";

    /// <summary>
    /// Builds a service collection carrying a financial service builder with a registered test provider.
    /// </summary>
    /// <param name="configuration">The configuration to bind options from, or <see langword="null" /> to skip binding.</param>
    /// <param name="configure">An optional delegate applied to the bound options.</param>
    /// <param name="configureResilience">An optional delegate applied to the resilience options.</param>
    /// <returns>The populated service collection, ready to build a provider from.</returns>
    private static ServiceCollection CreateServices(
        IConfiguration? configuration = null,
        Action<TestRateProviderOptions>? configure = null,
        Action<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>? configureResilience = null)
    {
        var services = new ServiceCollection();
        services
            .AddFinancialService()
            .AddWebRateProvider<TestRateProvider, TestRateProviderOptions>(
                TestHttpClientName,
                configuration,
                TestSectionName,
                TestValidationMessage,
                configure ?? DefaultConfigure,
                configureResilience,
                static (client, options, loggerFactory, timeProvider) =>
                    new TestRateProvider(client, options, loggerFactory, timeProvider));

        return services;
    }

    /// <summary>
    /// Applies the minimum configuration that satisfies <see cref="WebRateProviderOptions.TryValidate" />.
    /// </summary>
    /// <param name="options">The options to populate.</param>
    private static void DefaultConfigure(TestRateProviderOptions options) =>
        options.BaseAddress = new Uri("https://rates.example.test/");

    /// <summary>
    /// Builds a service collection carrying a financial service builder with a test provider registered through the
    /// explicit-selector overload, whose options type does not derive from <see cref="WebRateProviderOptions" />.
    /// </summary>
    /// <param name="configuration">The configuration to bind options from, or <see langword="null" /> to skip binding.</param>
    /// <param name="configure">An optional delegate applied to the bound options.</param>
    /// <param name="configureResilience">An optional delegate applied to the resilience options.</param>
    /// <param name="validateOptions">
    /// The validation predicate to register, or <see langword="null" /> to require only a base address.
    /// </param>
    /// <returns>The populated service collection, ready to build a provider from.</returns>
    private static ServiceCollection CreateExplicitSelectorServices(
        IConfiguration? configuration = null,
        Action<PlainRateProviderOptions>? configure = null,
        Action<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>? configureResilience = null,
        Func<PlainRateProviderOptions, bool>? validateOptions = null)
    {
        var services = new ServiceCollection();
        services
            .AddFinancialService()
            .AddWebRateProvider<PlainRateProvider, PlainRateProviderOptions>(
                TestHttpClientName,
                configuration,
                TestSectionName,
                validateOptions ?? PlainValidate,
                TestValidationMessage,
                static opts => opts.UserAgent,
                static opts => opts.HttpTimeout,
                configure ?? PlainDefaultConfigure,
                configureResilience,
                static (client, options, loggerFactory, timeProvider) =>
                    new PlainRateProvider(client, options, loggerFactory, timeProvider));

        return services;
    }

    /// <summary>
    /// Applies the minimum configuration that satisfies <see cref="PlainValidate" />.
    /// </summary>
    /// <param name="options">The options to populate.</param>
    private static void PlainDefaultConfigure(PlainRateProviderOptions options) =>
        options.BaseAddress = new Uri("https://rates.example.test/");

    /// <summary>
    /// The validation predicate the explicit-selector registrations supply: the options must carry a base address.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns><see langword="true" /> when a base address is set; otherwise <see langword="false" />.</returns>
    private static bool PlainValidate(PlainRateProviderOptions options) =>
        options.BaseAddress is not null;

    /// <summary>
    /// Builds an in-memory configuration carrying the supplied key/value pairs under the test section.
    /// </summary>
    /// <param name="values">The section-relative keys and values to expose.</param>
    /// <returns>A configuration root over the supplied values.</returns>
    private static IConfigurationRoot CreateConfiguration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v =>
                new KeyValuePair<string, string?>($"{TestSectionName}:{v.Key}", v.Value)))
            .Build();

    /// <summary>
    /// A minimal <see cref="WebRateProviderOptions" /> implementation carrying no source-specific invariants.
    /// </summary>
    internal sealed class TestRateProviderOptions : WebRateProviderOptions
    {
    }

    /// <summary>
    /// A minimal <see cref="WebRateProvider" /> that performs no network access, capturing the arguments the
    /// registration's factory delegate supplied so the tests can assert on them.
    /// </summary>
    internal sealed class TestRateProvider : WebRateProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRateProvider" /> class.
        /// </summary>
        /// <param name="client">The named <see cref="HttpClient" /> supplied by the registration.</param>
        /// <param name="options">The resolved options instance supplied by the registration.</param>
        /// <param name="loggerFactory">The ambient logger factory, or <see langword="null" /> when none is registered.</param>
        /// <param name="timeProvider">The ambient time provider, or <see langword="null" /> when none is registered.</param>
        public TestRateProvider(
            HttpClient client,
            TestRateProviderOptions options,
            ILoggerFactory? loggerFactory,
            TimeProvider? timeProvider)
            : base(client, timeProvider)
        {
            Client = client;
            Options = options;
            LoggerFactory = loggerFactory;
            SuppliedTimeProvider = timeProvider;
        }

        /// <summary>Gets the <see cref="HttpClient" /> the registration handed to the factory.</summary>
        public HttpClient Client { get; }

        /// <summary>Gets the options instance the registration handed to the factory.</summary>
        public TestRateProviderOptions Options { get; }

        /// <summary>Gets the logger factory the registration handed to the factory, if any.</summary>
        public ILoggerFactory? LoggerFactory { get; }

        /// <summary>Gets the time provider the registration handed to the factory, if any.</summary>
        public TimeProvider? SuppliedTimeProvider { get; }

        /// <inheritdoc />
        protected override string ProviderId => "test";

        /// <inheritdoc />
        protected override bool AllowSynchronousNetworkAccess => false;

        /// <inheritdoc />
        protected override TimeSpan DefaultLookback => TimeSpan.FromDays(7);

        /// <inheritdoc />
        protected override ValueTask EnsureLoadedAsync(
            CurrencyPair pair,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken) => ValueTask.CompletedTask;

        /// <inheritdoc />
        protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate) => true;
    }

    /// <summary>
    /// A minimal options type outside the <see cref="WebRateProviderOptions" /> hierarchy, carrying only the members
    /// the explicit-selector overload reads through the selectors a caller supplies.
    /// </summary>
    internal sealed class PlainRateProviderOptions
    {
        /// <summary>Gets or sets the endpoint base address the validation predicate requires.</summary>
        public Uri? BaseAddress { get; set; }

        /// <summary>Gets or sets the <c>User-Agent</c> value the user-agent selector returns.</summary>
        public string? UserAgent { get; set; }

        /// <summary>Gets or sets the per-attempt HTTP timeout the timeout selector returns.</summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// A minimal <see cref="WebRateProvider" /> over <see cref="PlainRateProviderOptions" /> that performs no network
    /// access, capturing the arguments the registration's factory delegate supplied so the tests can assert on them.
    /// </summary>
    internal sealed class PlainRateProvider : WebRateProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlainRateProvider" /> class.
        /// </summary>
        /// <param name="client">The named <see cref="HttpClient" /> supplied by the registration.</param>
        /// <param name="options">The resolved options instance supplied by the registration.</param>
        /// <param name="loggerFactory">The ambient logger factory, or <see langword="null" /> when none is registered.</param>
        /// <param name="timeProvider">The ambient time provider, or <see langword="null" /> when none is registered.</param>
        public PlainRateProvider(
            HttpClient client,
            PlainRateProviderOptions options,
            ILoggerFactory? loggerFactory,
            TimeProvider? timeProvider)
            : base(client, timeProvider)
        {
            Client = client;
            Options = options;
            LoggerFactory = loggerFactory;
            SuppliedTimeProvider = timeProvider;
        }

        /// <summary>Gets the <see cref="HttpClient" /> the registration handed to the factory.</summary>
        public HttpClient Client { get; }

        /// <summary>Gets the options instance the registration handed to the factory.</summary>
        public PlainRateProviderOptions Options { get; }

        /// <summary>Gets the logger factory the registration handed to the factory, if any.</summary>
        public ILoggerFactory? LoggerFactory { get; }

        /// <summary>Gets the time provider the registration handed to the factory, if any.</summary>
        public TimeProvider? SuppliedTimeProvider { get; }

        /// <inheritdoc />
        protected override string ProviderId => "plain";

        /// <inheritdoc />
        protected override bool AllowSynchronousNetworkAccess => false;

        /// <inheritdoc />
        protected override TimeSpan DefaultLookback => TimeSpan.FromDays(7);

        /// <inheritdoc />
        protected override ValueTask EnsureLoadedAsync(
            CurrencyPair pair,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken) => ValueTask.CompletedTask;

        /// <inheritdoc />
        protected override bool IsLoaded(CurrencyPair pair, DateOnly startDate, DateOnly endDate) => true;
    }
}
