// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class OfxRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        OfxRateProviderOptions options = new();

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
        OfxRateProviderOptions options = new() { BaseAddress = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a history path missing a required range placeholder is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHistoryPathMissingPlaceholder_ShouldReturnFalse()
    {
        OfxRateProviderOptions options = new() { HistoryPath = "PublicSite.ApiService/SpotRateHistory/{from}/{to}" };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a negative decimal-places value is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDecimalPlacesNegative_ShouldReturnFalse()
    {
        OfxRateProviderOptions options = new() { DecimalPlaces = -1 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a decimal-places value above the supported range is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDecimalPlacesTooLarge_ShouldReturnFalse()
    {
        OfxRateProviderOptions options = new() { DecimalPlaces = 16 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an empty reporting interval is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenReportingIntervalIsEmpty_ShouldReturnFalse()
    {
        OfxRateProviderOptions options = new() { ReportingInterval = "  " };

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
        OfxRateProviderOptions options = new() { HttpTimeout = TimeSpan.Zero };

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
        OfxRateProviderOptions options = new() { DefaultLookback = TimeSpan.Zero };

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
        OfxRateProviderOptions options = new() { CurrencyAliases = null! };

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
        OfxRateProviderOptions options = new() { SynchronousNetworkFetchLogLevel = (LogLevel)999 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
