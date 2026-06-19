// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateBookTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRateBook.Count" /> reports the number of distinct keys held.
    /// </summary>
    [TestMethod]
    public void Count_WhenMultipleSeries_ShouldReturnNumberOfDistinctKeys()
    {
        ExchangeRateBook book = new(
        [
            BuildSeries(s_usdAud, "RBA", 1.5m),
            BuildSeries(s_usdAud, "ECB", 1.6m),
            BuildSeries(s_eurAud, "RBA", 1.7m),
        ]);

        Assert.AreEqual(3, book.Count);
    }
}
