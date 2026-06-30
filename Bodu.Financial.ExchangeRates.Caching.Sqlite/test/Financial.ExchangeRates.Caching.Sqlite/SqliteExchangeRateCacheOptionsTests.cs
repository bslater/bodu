// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the validation rules of <see cref="SqliteExchangeRateCacheOptions" />.
/// </summary>
[TestClass]
public sealed partial class SqliteExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that write-ahead logging defaults to enabled, the recommended mode for a shared database.
    /// </summary>
    [TestMethod]
    public void UseWriteAheadLogging_WhenNotSet_ShouldDefaultToTrue()
    {
        var options = new SqliteExchangeRateCacheOptions();

        Assert.IsTrue(options.UseWriteAheadLogging);
    }

    /// <summary>
    /// Verifies that the busy timeout defaults to five seconds.
    /// </summary>
    [TestMethod]
    public void BusyTimeout_WhenNotSet_ShouldDefaultToFiveSeconds()
    {
        var options = new SqliteExchangeRateCacheOptions();

        Assert.AreEqual(TimeSpan.FromSeconds(5), options.BusyTimeout);
    }
}
