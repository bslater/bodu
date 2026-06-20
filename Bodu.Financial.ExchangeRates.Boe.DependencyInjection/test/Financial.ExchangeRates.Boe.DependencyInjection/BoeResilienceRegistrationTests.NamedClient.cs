// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeResilienceRegistrationTests.NamedClient.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace Bodu.Financial.ExchangeRates.Boe.DependencyInjection;

public partial class BoeResilienceRegistrationTests
{
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
            .AddFinancialService()
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
            .AddFinancialService()
            .AddBoeReferenceRates(configureResilience: ConfigureFastBreaker);
        services
            .AddHttpClient(BoeFinancialServiceBuilderExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(BoeFinancialServiceBuilderExtensions.HttpClientName);

        bool circuitOpened = false;
        for (int i = 0; i < 50 && !circuitOpened; i++)
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
}
