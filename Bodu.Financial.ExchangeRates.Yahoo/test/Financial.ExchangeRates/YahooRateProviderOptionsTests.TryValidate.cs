// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class YahooRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        YahooRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a null base address is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenBaseAddressIsNull_ShouldReturnFalse()
    {
        YahooRateProviderOptions options = new() { BaseAddress = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a chart path missing the symbol placeholder is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenChartPathMissingPlaceholder_ShouldReturnFalse()
    {
        YahooRateProviderOptions options = new() { ChartPath = "v8/finance/chart" };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a symbol format missing a placeholder is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenSymbolFormatMissingPlaceholder_ShouldReturnFalse()
    {
        YahooRateProviderOptions options = new() { SymbolFormat = "{from}=X" };

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
        YahooRateProviderOptions options = new() { HttpTimeout = TimeSpan.Zero };

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
        YahooRateProviderOptions options = new() { DefaultLookback = TimeSpan.Zero };

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
        YahooRateProviderOptions options = new() { CurrencyAliases = null! };

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
        YahooRateProviderOptions options = new() { SynchronousNetworkFetchLogLevel = (LogLevel)999 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
