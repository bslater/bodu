// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the shared validation behavior of <see cref="WebRateProviderOptions" />, including delegation to
/// the source-specific <c>TryValidateCore</c> override.
/// </summary>
[TestClass]
public class WebRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        TestWebRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that <see cref="WebRateProviderOptions.ObservationIngestedLogLevel" /> defaults to
    /// <see cref="LogLevel.Trace" />, so the per-observation event stays out of ordinary logs.
    /// </summary>
    [TestMethod]
    public void ObservationIngestedLogLevel_WhenDefault_ShouldBeTrace()
    {
        TestWebRateProviderOptions options = new();

        Assert.AreEqual(LogLevel.Trace, options.ObservationIngestedLogLevel);
    }

    /// <summary>
    /// Verifies that <see cref="WebRateProviderOptions.RefreshInterval" /> defaults to 12 hours.
    /// </summary>
    [TestMethod]
    public void RefreshInterval_WhenDefault_ShouldBeTwelveHours()
    {
        var options = new TestWebRateProviderOptions();

        Assert.AreEqual(TimeSpan.FromHours(12), options.RefreshInterval);
    }

    /// <summary>
    /// Verifies that a null base address is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenBaseAddressIsNull_ShouldReturnFalse()
    {
        TestWebRateProviderOptions options = new() { BaseAddress = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive HTTP timeout is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHttpTimeoutIsZero_ShouldReturnFalse()
    {
        TestWebRateProviderOptions options = new() { HttpTimeout = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive default look-back window is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefaultLookbackIsZero_ShouldReturnFalse()
    {
        TestWebRateProviderOptions options = new() { DefaultLookback = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a zero refresh interval is rejected with the resource-backed refresh-interval message, since it
    /// would make every request that reaches a day the source could still publish download it again.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsZero_ShouldReturnFalse()
    {
        var options = new TestWebRateProviderOptions { RefreshInterval = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.AreEqual(
            ExchangeRatesResourceStrings.ResourceManager.GetString("Arg_Invalid_WebExchangeRateOptionsRefreshInterval", CultureInfo.CurrentCulture),
            error);
    }

    /// <summary>
    /// Verifies that a negative refresh interval is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsNegative_ShouldReturnFalse()
    {
        var options = new TestWebRateProviderOptions { RefreshInterval = TimeSpan.FromHours(-1) };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a null currency-alias map is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCurrencyAliasesIsNull_ShouldReturnFalse()
    {
        TestWebRateProviderOptions options = new() { CurrencyAliases = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an alias value containing URL-structural characters (which would let a crafted mapping inject
    /// path or query segments into a request) is rejected.
    /// </summary>
    [TestMethod]
    [DataRow("../etc")]
    [DataRow("a/b")]
    [DataRow("x?y")]
    [DataRow("x#y")]
    [DataRow("")]
    public void TryValidate_WhenCurrencyAliasValueIsUnsafe_ShouldReturnFalse(string aliasValue)
    {
        TestWebRateProviderOptions options = new();
        options.CurrencyAliases["USD"] = aliasValue;

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an alphanumeric alias value (the shape a source symbol component takes) is accepted.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCurrencyAliasValueIsAlphanumeric_ShouldReturnTrue()
    {
        TestWebRateProviderOptions options = new();
        options.CurrencyAliases["USD"] = "USD2";

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that an undefined log level is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenLogLevelUndefined_ShouldReturnFalse()
    {
        TestWebRateProviderOptions options = new() { SynchronousNetworkFetchLogLevel = (LogLevel)999 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that the shared validation delegates to the source-specific <c>TryValidateCore</c> when the shared
    /// invariants hold.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCoreInvalid_ShouldReturnFalse()
    {
        TestWebRateProviderOptions options = new() { CoreValid = false };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that <see cref="WebRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        TestWebRateProviderOptions options = new() { CoreValid = false };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
