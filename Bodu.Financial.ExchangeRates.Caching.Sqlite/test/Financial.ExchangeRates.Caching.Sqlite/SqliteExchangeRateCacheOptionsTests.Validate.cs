// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

public sealed partial class SqliteExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that validation rejects options whose provider is blank.
    /// </summary>
    [TestMethod]
    public void Validate_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "  ", DatabaseFilePath = "cache.db" };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            options.Validate,
            "Provider");
    }

    /// <summary>
    /// Verifies that validation rejects options that supply neither a database file path nor a connection string.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNoLocationSupplied_ShouldThrowArgumentException()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "RBA" };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            options.Validate,
            "DatabaseFilePath");
    }

    /// <summary>
    /// Verifies that validation accepts options configured with only a database file path.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOnlyDatabaseFilePathSupplied_ShouldNotThrow()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "cache.db" };

        options.Validate();
    }

    /// <summary>
    /// Verifies that validation accepts options configured with only a connection string.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOnlyConnectionStringSupplied_ShouldNotThrow()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", ConnectionString = "Data Source=cache.db" };

        options.Validate();
    }

    /// <summary>
    /// Verifies that validation rejects a negative busy timeout.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBusyTimeoutIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "cache.db", BusyTimeout = TimeSpan.FromSeconds(-1) };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            options.Validate,
            "BusyTimeout");
    }

    /// <summary>
    /// Verifies that validation accepts a zero busy timeout, which disables waiting on a held lock.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBusyTimeoutIsZero_ShouldNotThrow()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "cache.db", BusyTimeout = TimeSpan.Zero };

        options.Validate();
    }
}
