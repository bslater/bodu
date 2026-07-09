// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the validation rules of <see cref="SqliteRateCacheOptions" />.
/// </summary>
[TestClass]
public sealed partial class SqliteRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that write-ahead logging defaults to enabled, the recommended mode for a shared database.
    /// </summary>
    [TestMethod]
    public void UseWriteAheadLogging_WhenNotSet_ShouldDefaultToTrue()
    {
        var options = new SqliteRateCacheOptions();

        Assert.IsTrue(options.UseWriteAheadLogging);
    }

    /// <summary>
    /// Verifies that the busy timeout defaults to five seconds.
    /// </summary>
    [TestMethod]
    public void BusyTimeout_WhenNotSet_ShouldDefaultToFiveSeconds()
    {
        var options = new SqliteRateCacheOptions();

        Assert.AreEqual(TimeSpan.FromSeconds(5), options.BusyTimeout);
    }
}
