// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderExtensionsTests.Resilience.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderExtensionsTests
{
    /// <summary>The options name the standard resilience handler registers under for the test client.</summary>
    private const string ResilienceOptionsName = TestHttpClientName + "-standard";

    /// <summary>
    /// Resolves the standard resilience options the registration configured for the test client.
    /// </summary>
    /// <param name="provider">The built service provider.</param>
    /// <returns>The resolved resilience options.</returns>
    private static HttpStandardResilienceOptions GetResilienceOptions(ServiceProvider provider) =>
        provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>().Get(ResilienceOptionsName);

    /// <summary>
    /// Verifies that the per-attempt timeout is taken from the configured HTTP timeout, so the option a provider
    /// exposes is the one the pipeline actually enforces.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenHttpTimeoutConfigured_ShouldUseItAsTheAttemptTimeout()
    {
        var timeout = TimeSpan.FromSeconds(7);
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.HttpTimeout = timeout;
        }).BuildServiceProvider();

        Assert.AreEqual(timeout, GetResilienceOptions(provider).AttemptTimeout.Timeout);
    }

    /// <summary>
    /// Verifies that the total-request timeout is three times the per-attempt timeout, leaving room for retries; the
    /// standard handler rejects a total timeout that does not exceed the attempt timeout.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenHttpTimeoutConfigured_ShouldSetTotalRequestTimeoutToThreeAttempts()
    {
        var timeout = TimeSpan.FromSeconds(7);
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.HttpTimeout = timeout;
        }).BuildServiceProvider();

        Assert.AreEqual(timeout * 3, GetResilienceOptions(provider).TotalRequestTimeout.Timeout);
    }

    /// <summary>
    /// Verifies that a long attempt timeout widens the circuit breaker's sampling duration to at least twice that
    /// value, which the standard handler requires; without the widening, a long timeout fails pipeline validation.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenAttemptTimeoutExceedsHalfTheSamplingWindow_ShouldWidenSamplingDuration()
    {
        var timeout = TimeSpan.FromSeconds(90);
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.HttpTimeout = timeout;
        }).BuildServiceProvider();

        Assert.IsTrue(
            GetResilienceOptions(provider).CircuitBreaker.SamplingDuration >= timeout * 2,
            "The sampling duration must be at least twice the attempt timeout.");
    }

    /// <summary>
    /// Verifies that a short attempt timeout leaves the default sampling duration alone, because the widening is a
    /// floor rather than an unconditional assignment.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenAttemptTimeoutIsShort_ShouldLeaveDefaultSamplingDurationUnchanged()
    {
        TimeSpan defaultSampling = new HttpStandardResilienceOptions().CircuitBreaker.SamplingDuration;
        var timeout = TimeSpan.FromSeconds(1);

        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.HttpTimeout = timeout;
        }).BuildServiceProvider();

        Assert.AreEqual(defaultSampling, GetResilienceOptions(provider).CircuitBreaker.SamplingDuration);
    }

    /// <summary>
    /// Verifies that the caller's resilience delegate runs after the timeout alignment, so a provider can override
    /// what the shared machinery computed.
    /// </summary>
    /// <remarks>
    /// The override lowers the attempt timeout rather than raising it, because the standard handler validates the
    /// pipeline as a whole: the total-request timeout must exceed the attempt timeout and the circuit breaker's
    /// sampling duration must be at least twice it. Raising only the attempt timeout leaves the aligned total and
    /// sampling values behind and fails that validation — see
    /// <see cref="AddWebRateProvider_WhenConfigureResilienceRaisesAttemptTimeoutAlone_ShouldFailPipelineValidation" />.
    /// </remarks>
    [TestMethod]
    public void AddWebRateProvider_WhenConfigureResilienceSupplied_ShouldApplyItAfterTimeoutAlignment()
    {
        var overridden = TimeSpan.FromSeconds(2);
        using ServiceProvider provider = CreateServices(
            configure: o =>
            {
                DefaultConfigure(o);
                o.HttpTimeout = TimeSpan.FromSeconds(7);
            },
            configureResilience: o => o.AttemptTimeout.Timeout = overridden)
            .BuildServiceProvider();

        // 2s, not the 7s the alignment computed from HttpTimeout — so the delegate ran last.
        Assert.AreEqual(overridden, GetResilienceOptions(provider).AttemptTimeout.Timeout);
    }

    /// <summary>
    /// Verifies that raising the attempt timeout alone throws <see cref="OptionsValidationException" />, because the
    /// override runs after the alignment and so leaves the derived total-request timeout and sampling duration behind.
    /// </summary>
    /// <remarks>
    /// This is the standard handler's own validation, not a Bodu invariant, and it is the reason a provider that wants
    /// a longer attempt timeout should raise <see cref="WebRateProviderOptions.HttpTimeout" /> — which the alignment
    /// derives every dependent value from — rather than reach into the resilience options.
    /// </remarks>
    [TestMethod]
    public void AddWebRateProvider_WhenConfigureResilienceRaisesAttemptTimeoutAlone_ShouldFailPipelineValidation()
    {
        using ServiceProvider provider = CreateServices(
            configure: o =>
            {
                DefaultConfigure(o);
                o.HttpTimeout = TimeSpan.FromSeconds(7);
            },
            configureResilience: o => o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(42))
            .BuildServiceProvider();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() =>
        {
            _ = GetResilienceOptions(provider);
        });
    }
}
