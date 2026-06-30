// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateBookTests
{

    /// <summary>
    /// Verifies that the constructor rejects a series list with two entries sharing the same pair/provider key.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesListContainsDuplicateKey_ShouldThrowArgumentException()
    {
        ExchangeRateSeries first = BuildSeries(s_usdAud, "RBA", 1.5m);
        ExchangeRateSeries duplicate = BuildSeries(s_usdAud, "RBA", 1.6m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRateBook([first, duplicate]);
            },
            "series");
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> enumerable.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRateBook((IEnumerable<ExchangeRateSeries>)null!);
            },
            "series");
    }

    /// <summary>
    /// Verifies that the constructor rejects a series enumerable containing a <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesContainsNullElement_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRateBook(new ExchangeRateSeries?[] { null }!);
            },
            "series");
    }
}
