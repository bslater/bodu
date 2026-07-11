// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the default options — which require no API key — validate successfully.
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
    /// Verifies that a blank CompactData path is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCompactDataPathBlank_ShouldReturnFalse()
    {
        ImfRateProviderOptions options = new() { CompactDataPath = "  " };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a blank dataflow is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDataflowBlank_ShouldReturnFalse()
    {
        ImfRateProviderOptions options = new() { Dataflow = "" };

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
        ImfRateProviderOptions options = new() { SeriesMap = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
