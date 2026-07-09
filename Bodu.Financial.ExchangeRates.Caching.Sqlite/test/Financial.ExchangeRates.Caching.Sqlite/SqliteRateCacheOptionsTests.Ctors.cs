// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheOptionsTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

public sealed partial class SqliteRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that constructing a cache with a <see langword="null" /> options reference throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SqliteRateCache((SqliteRateCacheOptions)null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
