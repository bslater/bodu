// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeResilienceRegistrationTests.AddBoeReferenceRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeResilienceRegistrationTests
{
    /// <summary>
    /// Verifies that the standard resilience options are registered for the named client with the per-attempt timeout
    /// driven from the configured <see cref="BoeEndpointOptions.HttpTimeout" />.
    /// </summary>
    [TestMethod]
    public void AddBoeReferenceRates_ShouldRegisterStandardResilienceOptionsForNamedClient()
    {
        ServiceCollection services = new();
        services.AddFinancialService().AddBoeReferenceRates(configure: o => o.Endpoint.HttpTimeout = TimeSpan.FromSeconds(7));
        using ServiceProvider provider = services.BuildServiceProvider();

        HttpStandardResilienceOptions options = provider
            .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get(ResilienceOptionsName);

        Assert.AreEqual(TimeSpan.FromSeconds(7), options.AttemptTimeout.Timeout);
        Assert.IsTrue(options.TotalRequestTimeout.Timeout > options.AttemptTimeout.Timeout);
        Assert.IsTrue(options.CircuitBreaker.SamplingDuration >= options.AttemptTimeout.Timeout * 2);
    }
}
