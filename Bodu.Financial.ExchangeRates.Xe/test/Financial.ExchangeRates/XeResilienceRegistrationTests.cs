// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeResilienceRegistrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Http.Resilience;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the HTTP resilience pipeline attached to the XE exchange-rate provider's named client.
/// </summary>
[TestClass]
public partial class XeResilienceRegistrationTests
{
    /// <summary>
    /// The internal options name under which the standard resilience handler registers its options for the named client.
    /// </summary>
    private const string ResilienceOptionsName = XeFinancialServiceBuilderExtensions.HttpClientName + "-standard";

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
}
