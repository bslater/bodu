// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.TryGetSeries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateBookTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRateBook.TryGetSeries" /> returns <see langword="false" /> when no series
    /// matches the supplied pair/provider key.
    /// </summary>
    [TestMethod]
    public void TryGetSeries_WhenSeriesMissing_ShouldReturnFalse()
    {
        ExchangeRateBook book = new([BuildSeries(s_usdAud, "RBA", 1.5m)]);

        bool found = book.TryGetSeries(s_usdAud, "ECB", out ExchangeRateSeries? resolved);

        Assert.IsFalse(found);
        Assert.IsNull(resolved);
    }
}
