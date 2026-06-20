// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheOptionsTests.Constructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

public sealed partial class SqliteExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that constructing a cache with a <see langword="null" /> options reference throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SqliteExchangeRateCache((SqliteExchangeRateCacheOptions)null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
