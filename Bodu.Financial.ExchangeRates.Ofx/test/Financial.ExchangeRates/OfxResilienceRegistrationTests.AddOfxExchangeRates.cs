// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxResilienceRegistrationTests.AddOfxExchangeRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class OfxResilienceRegistrationTests
{
    /// <summary>
    /// Verifies that the standard resilience options are registered for the named client with the per-attempt timeout
    /// driven from the configured <see cref="WebExchangeRateProviderOptions.HttpTimeout" />.
    /// </summary>
    [TestMethod]
    public void AddOfxExchangeRates_ShouldRegisterStandardResilienceOptionsForNamedClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddOfxExchangeRates(configure: o => o.HttpTimeout = TimeSpan.FromSeconds(7));
        using ServiceProvider provider = services.BuildServiceProvider();

        HttpStandardResilienceOptions options = provider
            .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get(ResilienceOptionsName);

        Assert.AreEqual(TimeSpan.FromSeconds(7), options.AttemptTimeout.Timeout);
        Assert.IsGreaterThan(options.AttemptTimeout.Timeout, options.TotalRequestTimeout.Timeout);
        Assert.IsGreaterThanOrEqualTo(options.AttemptTimeout.Timeout * 2, options.CircuitBreaker.SamplingDuration);
    }
}
