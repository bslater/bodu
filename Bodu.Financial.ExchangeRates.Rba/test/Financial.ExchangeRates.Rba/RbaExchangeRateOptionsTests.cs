// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Verifies the validation behaviour of <see cref="RbaExchangeRateOptions" />.
/// </summary>
[TestClass]
public class RbaExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        RbaExchangeRateOptions options = new();

        var valid = options.TryValidate(out var error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that the default leaves synchronous network access disabled.
    /// </summary>
    [TestMethod]
    public void AllowSynchronousNetworkAccess_WhenDefault_ShouldBeFalse()
    {
        RbaExchangeRateOptions options = new();

        Assert.IsFalse(options.AllowSynchronousNetworkAccess);
    }

    /// <summary>
    /// Verifies that a null base URL is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenBaseUrlIsNull_ShouldReturnFalse()
    {
        RbaExchangeRateOptions options = new() { BaseUrl = null! };

        var valid = options.TryValidate(out var error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an empty era catalogue is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenErasEmpty_ShouldReturnFalse()
    {
        RbaExchangeRateOptions options = new() { Eras = [] };

        var valid = options.TryValidate(out var error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive HTTP timeout is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHttpTimeoutIsZero_ShouldReturnFalse()
    {
        RbaExchangeRateOptions options = new() { HttpTimeout = TimeSpan.Zero };

        var valid = options.TryValidate(out var error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive current-era refresh interval is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsNegative_ShouldReturnFalse()
    {
        RbaExchangeRateOptions options = new() { CurrentEraRefreshInterval = TimeSpan.FromHours(-1) };

        var valid = options.TryValidate(out var error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a null currency-alias map is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCurrencyAliasesIsNull_ShouldReturnFalse()
    {
        RbaExchangeRateOptions options = new() { CurrencyAliases = null! };

        var valid = options.TryValidate(out var error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an undefined log level is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenLogLevelUndefined_ShouldReturnFalse()
    {
        RbaExchangeRateOptions options = new() { DownloadStartingLogLevel = (LogLevel)999 };

        var valid = options.TryValidate(out var error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that <see cref="RbaExchangeRateOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        RbaExchangeRateOptions options = new() { Eras = [] };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });
    }
}
