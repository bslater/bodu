// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        RbaRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a null base URL is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenBaseUrlIsNull_ShouldReturnFalse()
    {
        RbaRateProviderOptions options = new() { BaseUrl = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an empty era catalogue is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenErasEmpty_ShouldReturnFalse()
    {
        RbaRateProviderOptions options = new() { Eras = [] };

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
        RbaRateProviderOptions options = new() { HttpTimeout = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive current-era refresh interval is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsNegative_ShouldReturnFalse()
    {
        RbaRateProviderOptions options = new() { CurrentEraRefreshInterval = TimeSpan.FromHours(-1) };

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
        RbaRateProviderOptions options = new() { CurrencyAliases = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an undefined log level is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenLogLevelUndefined_ShouldReturnFalse()
    {
        RbaRateProviderOptions options = new() { DownloadStartingLogLevel = (LogLevel)999 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
