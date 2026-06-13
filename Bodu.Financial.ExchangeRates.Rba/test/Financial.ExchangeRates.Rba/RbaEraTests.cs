// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaEraTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Verifies the behavior of <see cref="RbaEra" /> and its default catalogue.
/// </summary>
[TestClass]
public class RbaEraTests
{
    /// <summary>
    /// Verifies that the default catalogue contains the eleven published era files.
    /// </summary>
    [TestMethod]
    public void Default_ShouldContainElevenEras()
    {
        Assert.AreEqual(11, RbaEra.Default.Count);
    }

    /// <summary>
    /// Verifies that the file name is the label with a <c>.xls</c> extension.
    /// </summary>
    [TestMethod]
    public void FileName_ShouldAppendXlsExtension()
    {
        RbaEra era = new("2023-current", new DateOnly(2023, 1, 1), null);

        Assert.AreEqual("2023-current.xls", era.FileName);
    }

    /// <summary>
    /// Verifies that a date within the open-ended current era resolves to that era.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenDateInCurrentEra_ShouldReturnCurrentEra()
    {
        var era = RbaEra.ForDate(new DateOnly(2024, 6, 1), RbaEra.Default);

        Assert.IsNotNull(era);
        Assert.AreEqual("2023-current", era.Label);
    }

    /// <summary>
    /// Verifies that a date within a historical era resolves to that era.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenDateInHistoricalEra_ShouldReturnHistoricalEra()
    {
        var era = RbaEra.ForDate(new DateOnly(1985, 5, 5), RbaEra.Default);

        Assert.IsNotNull(era);
        Assert.AreEqual("1983-1986", era.Label);
    }

    /// <summary>
    /// Verifies that a date before the earliest era resolves to no era.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenDateBeforeAllEras_ShouldReturnNull()
    {
        var era = RbaEra.ForDate(new DateOnly(1980, 1, 1), RbaEra.Default);

        Assert.IsNull(era);
    }

    /// <summary>
    /// Verifies that the open-ended current era covers a far-future date.
    /// </summary>
    [TestMethod]
    public void Covers_WhenOpenEndedEra_ShouldCoverFutureDate()
    {
        RbaEra era = new("2023-current", new DateOnly(2023, 1, 1), null);

        Assert.IsTrue(era.Covers(new DateOnly(2099, 12, 31)));
        Assert.IsFalse(era.Covers(new DateOnly(2022, 12, 31)));
    }
}
