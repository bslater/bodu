// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FixerRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that options carrying an API key validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenApiKeySet_ShouldReturnTrue()
    {
        FixerRateProviderOptions options = new() { ApiKey = "test-key" };

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
        FixerRateProviderOptions options = new();

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
        FixerRateProviderOptions options = new() { ApiKey = "test-key", TimeSeriesPath = "  " };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a historical path missing the date placeholder is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHistoricalPathMissingPlaceholder_ShouldReturnFalse()
    {
        FixerRateProviderOptions options = new() { ApiKey = "test-key", HistoricalPath = "latest" };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
