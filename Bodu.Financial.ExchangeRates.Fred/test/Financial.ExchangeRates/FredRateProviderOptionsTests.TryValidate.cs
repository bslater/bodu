// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class FredRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that options carrying an API key validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenApiKeySet_ShouldReturnTrue()
    {
        FredRateProviderOptions options = new() { ApiKey = "test-key" };

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
        FredRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a blank observations path is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenObservationsPathBlank_ShouldReturnFalse()
    {
        FredRateProviderOptions options = new() { ApiKey = "test-key", ObservationsPath = "  " };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a null series map is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenSeriesMapNull_ShouldReturnFalse()
    {
        FredRateProviderOptions options = new() { ApiKey = "test-key", SeriesMap = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
