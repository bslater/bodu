// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbResilienceRegistrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Http.Resilience;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the HTTP resilience pipeline attached to the ECB euro reference-rate provider's named client.
/// </summary>
[TestClass]
public partial class EcbResilienceRegistrationTests
{
    /// <summary>
    /// The internal options name under which the standard resilience handler registers its options for the named client.
    /// </summary>
    private const string ResilienceOptionsName = EcbFinancialServiceBuilderExtensions.HttpClientName + "-standard";

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
