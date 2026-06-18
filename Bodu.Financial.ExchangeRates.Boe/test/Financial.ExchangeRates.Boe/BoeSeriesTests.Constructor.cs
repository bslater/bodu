// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeSeriesTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

public partial class BoeSeriesTests
{
    /// <summary>
    /// Verifies that constructing a series with a <see langword="null" /> code throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesCodeIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BoeSeries("USD", null!, "US dollar into Sterling");
        });
    }
}
