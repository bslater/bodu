// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeResilienceRegistrationTests.NamedClient.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class XeResilienceRegistrationTests
{
    /// <summary>
    /// Verifies that the standard resilience options are registered for the named client with the per-attempt timeout
    /// driven from the configured <see cref="WebRateProviderOptions.HttpTimeout" />.
    /// </summary>
    [TestMethod]
    public void AddXeExchangeRates_ShouldRegisterStandardResilienceOptionsForNamedClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddXeExchangeRates(configure: o => o.HttpTimeout = TimeSpan.FromSeconds(7));
        using ServiceProvider provider = services.BuildServiceProvider();

        HttpStandardResilienceOptions options = provider
            .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get(ResilienceOptionsName);

        Assert.AreEqual(TimeSpan.FromSeconds(7), options.AttemptTimeout.Timeout);
        Assert.IsGreaterThan(options.AttemptTimeout.Timeout, options.TotalRequestTimeout.Timeout);
        Assert.IsGreaterThanOrEqualTo(options.AttemptTimeout.Timeout * 2, options.CircuitBreaker.SamplingDuration);
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
            .AddFinancialService()
            .AddXeExchangeRates(configureResilience: ConfigureFastRetry);
        services
            .AddHttpClient(XeFinancialServiceBuilderExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(XeFinancialServiceBuilderExtensions.HttpClientName);

        using HttpResponseMessage response = await client.GetAsync(new Uri("https://xe.test/quote"));

        Assert.IsTrue(response.IsSuccessStatusCode);
        Assert.AreEqual(3, handler.RequestCount);
    }
}
