// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.Book.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateBookTests
{

    /// <summary>
    /// Verifies that the smoke-tier happy path constructs a book from a single series and exposes it via the
    /// pair/provider key.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Book_WhenConstructedFromSingleSeries_ShouldExposeSeriesByKey()
    {
        ExchangeRateSeries series = BuildSeries(s_usdAud, "RBA", 1.5m);
        ExchangeRateBook book = new([series]);

        bool found = book.TryGetSeries(s_usdAud, "RBA", out ExchangeRateSeries? resolved);

        Assert.IsTrue(found);
        Assert.AreSame(series, resolved);
    }
}
