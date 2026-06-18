// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaEraTests.ForDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaEraTests
{
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
}
