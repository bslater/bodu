// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateHostRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that options carrying an API key validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenApiKeySet_ShouldReturnTrue()
    {
        ExchangeRateHostRateProviderOptions options = new() { ApiKey = "test-key" };

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that options with no API key are rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenApiKeyMissing_ShouldReturnFalse()
    {
        ExchangeRateHostRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a blank time-series path is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenTimeSeriesPathBlank_ShouldReturnFalse()
    {
        ExchangeRateHostRateProviderOptions options = new() { ApiKey = "test-key", TimeSeriesPath = "  " };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a blank historical path is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHistoricalPathBlank_ShouldReturnFalse()
    {
        ExchangeRateHostRateProviderOptions options = new() { ApiKey = "test-key", HistoricalPath = "  " };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
