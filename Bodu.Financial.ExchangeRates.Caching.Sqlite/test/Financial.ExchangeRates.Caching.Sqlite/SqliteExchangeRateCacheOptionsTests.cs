// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies the validation rules of <see cref="SqliteExchangeRateCacheOptions" />.
/// </summary>
[TestClass]
public sealed class SqliteExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that validation rejects options whose provider is blank.
    /// </summary>
    [TestMethod]
    public void Validate_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var options = new SqliteExchangeRateCacheOptions { Provider = "  ", DatabaseFilePath = "cache.db" };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => options.Validate(),
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
            () => options.Validate(),
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
    /// Verifies that constructing a cache with a <see langword="null" /> options reference throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SqliteExchangeRateCache((SqliteExchangeRateCacheOptions)null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
