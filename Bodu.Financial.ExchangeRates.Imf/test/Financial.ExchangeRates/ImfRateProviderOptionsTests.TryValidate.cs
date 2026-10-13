// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default options - which require no API key - validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefaults_ShouldReturnTrue()
    {
        ImfRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a blank report path is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenReportPathBlank_ShouldReturnFalse()
    {
        ImfRateProviderOptions options = new() { ReportPath = "  " };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a blank report type is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenReportTypeBlank_ShouldReturnFalse()
    {
        ImfRateProviderOptions options = new() { ReportType = "" };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a zero refresh interval is rejected, as the Bank of England and ECB options reject theirs.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsZero_ShouldReturnFalse()
    {
        var options = new ImfRateProviderOptions { RefreshInterval = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a negative refresh interval is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsNegative_ShouldReturnFalse()
    {
        var options = new ImfRateProviderOptions { RefreshInterval = TimeSpan.FromMinutes(-5) };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a null currency-name map is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCurrencyNamesNull_ShouldReturnFalse()
    {
        ImfRateProviderOptions options = new() { CurrencyNames = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
