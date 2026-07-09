// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbExchangeRateFeedTests
{
    /// <summary>
    /// Verifies that constructing a feed with a <see langword="null" /> name throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new EcbExchangeRateFeed(null!, "eurofxref-hist.xml", null);
        });
    }

    /// <summary>
    /// Verifies that constructing a feed with a negative look-back throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenLookbackIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EcbExchangeRateFeed("bad", "bad.xml", -1);
        });
    }
}
