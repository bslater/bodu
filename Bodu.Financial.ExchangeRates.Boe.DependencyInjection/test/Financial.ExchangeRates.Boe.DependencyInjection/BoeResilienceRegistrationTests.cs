// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeResilienceRegistrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace Bodu.Financial.ExchangeRates.Boe.DependencyInjection;

/// <summary>
/// Verifies the HTTP resilience pipeline attached to the Bank of England exchange-rate provider's named client.
/// </summary>
[TestClass]
public class BoeResilienceRegistrationTests
{
    /// <summary>
    /// The internal options name under which the standard resilience handler registers its options for the named client.
    /// </summary>
    private const string ResilienceOptionsName = BoeFinancialServiceBuilderExtensions.HttpClientName + "-standard";

    /// <summary>
    /// Verifies that the standard resilience options are registered for the named client with the per-attempt timeout
    /// driven from the configured <see cref="BoeEndpointOptions.HttpTimeout" />.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_ShouldRegisterStandardResilienceOptionsForNamedClient()
    {
        ServiceCollection services = new();
        services.AddBoduFinancial().AddBoeReferenceRates(configure: o => o.Endpoint.HttpTimeout = TimeSpan.FromSeconds(7));
        using ServiceProvider provider = services.BuildServiceProvider();

        HttpStandardResilienceOptions options = provider
            .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get(ResilienceOptionsName);

        Assert.AreEqual(TimeSpan.FromSeconds(7), options.AttemptTimeout.Timeout);
        Assert.IsTrue(options.TotalRequestTimeout.Timeout > options.AttemptTimeout.Timeout);
        Assert.IsTrue(options.CircuitBreaker.SamplingDuration >= options.AttemptTimeout.Timeout * 2);
    }

    /// <summary>
    /// Verifies that a transient run of <c>503</c> responses is retried by the resilience handler so that the request
    /// ultimately succeeds and the underlying handler is invoked more than once.
    /// </summary>
    [TestMethod]
    public async Task NamedClient_WhenTransientFailuresThenSuccess_ShouldRetryAndSucceed()
    {
        TransientFaultHttpMessageHandler handler = new(failuresBeforeSuccess: 2);

        ServiceCollection services = new();
        services
            .AddBoduFinancial()
            .AddBoeReferenceRates(configureResilience: ConfigureFastRetry);
        services
            .AddHttpClient(BoeFinancialServiceBuilderExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(BoeFinancialServiceBuilderExtensions.HttpClientName);

        using HttpResponseMessage response = await client.GetAsync(new Uri("https://boe.test/series"));

        Assert.IsTrue(response.IsSuccessStatusCode);
        Assert.IsTrue(handler.RequestCount > 1, $"Expected more than one request, observed {handler.RequestCount}.");
        Assert.AreEqual(3, handler.RequestCount);
    }

    /// <summary>
    /// Verifies that sustained <c>500</c> responses eventually open the circuit breaker, short-circuiting further
    /// requests with a <see cref="BrokenCircuitException" />.
    /// </summary>
    [TestMethod]
    public async Task NamedClient_WhenFailuresAreSustained_ShouldOpenCircuit()
    {
        TransientFaultHttpMessageHandler handler = new(failuresBeforeSuccess: int.MaxValue, failureStatus: System.Net.HttpStatusCode.InternalServerError);

        ServiceCollection services = new();
        services
            .AddBoduFinancial()
            .AddBoeReferenceRates(configureResilience: ConfigureFastBreaker);
        services
            .AddHttpClient(BoeFinancialServiceBuilderExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(BoeFinancialServiceBuilderExtensions.HttpClientName);

        var circuitOpened = false;
        for (var i = 0; i < 50 && !circuitOpened; i++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(new Uri("https://boe.test/series"));
            }
            catch (BrokenCircuitException)
            {
                circuitOpened = true;
            }
        }

        Assert.IsTrue(circuitOpened, "Expected the circuit breaker to open under sustained failures.");
    }

    /// <summary>
    /// Configures the standard resilience options for fast, deterministic retries with no real backoff delay.
    /// </summary>
    /// <param name="options">The standard resilience options to adjust.</param>
    private static void ConfigureFastRetry(HttpStandardResilienceOptions options)
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        options.Retry.BackoffType = Polly.DelayBackoffType.Constant;
    }

    /// <summary>
    /// Configures the standard resilience options for a fast-tripping circuit breaker with no real backoff delay.
    /// </summary>
    /// <param name="options">The standard resilience options to adjust.</param>
    private static void ConfigureFastBreaker(HttpStandardResilienceOptions options)
    {
        ConfigureFastRetry(options);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(2);
        options.CircuitBreaker.MinimumThroughput = 2;
        options.CircuitBreaker.FailureRatio = 0.99;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(1);
    }
}
