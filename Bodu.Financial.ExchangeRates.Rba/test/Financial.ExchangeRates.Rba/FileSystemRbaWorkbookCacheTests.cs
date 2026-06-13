// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Verifies the freshness and persistence behavior of <see cref="FileSystemRbaWorkbookCache" />.
/// </summary>
[TestClass]
public class FileSystemRbaWorkbookCacheTests
{
    private static readonly RbaEra s_immutableEra = new("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31));
    private static readonly RbaEra s_currentEra = new("2023-current", new DateOnly(2023, 1, 1), null);

    private string _directory = string.Empty;

    /// <summary>
    /// Creates a unique cache directory for each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "bodu-rba-test-" + Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Removes the cache directory after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    /// <summary>
    /// Verifies that bytes stored for an immutable era are returned on a subsequent read.
    /// </summary>
    [TestMethod]
    public void Store_ThenTryGet_ShouldReturnStoredBytes()
    {
        FileSystemRbaWorkbookCache cache = new(_directory);
        var payload = new byte[] { 1, 2, 3, 4 };

        cache.Store(s_immutableEra, payload);
        var found = cache.TryGet(s_immutableEra, TimeSpan.FromHours(12), out var bytes);

        Assert.IsTrue(found);
        CollectionAssert.AreEqual(payload, bytes);
    }

    /// <summary>
    /// Verifies that a missing entry reports a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenEntryMissing_ShouldReturnFalse()
    {
        FileSystemRbaWorkbookCache cache = new(_directory);

        var found = cache.TryGet(s_currentEra, TimeSpan.FromHours(12), out var bytes);

        Assert.IsFalse(found);
        Assert.IsNull(bytes);
    }

    /// <summary>
    /// Verifies that the open-ended current era expires once its cached copy is older than the refresh interval.
    /// </summary>
    [TestMethod]
    public void TryGet_Whens_currentEraOlderThanRefreshInterval_ShouldReturnMiss()
    {
        FileSystemRbaWorkbookCache cache = new(_directory);
        cache.Store(s_currentEra, new byte[] { 1, 2, 3 });

        // Age the cached file beyond the refresh window.
        File.SetLastWriteTimeUtc(Path.Combine(_directory, s_currentEra.FileName), DateTime.UtcNow.AddHours(-48));

        var found = cache.TryGet(s_currentEra, TimeSpan.FromHours(12), out _);

        Assert.IsFalse(found);
    }

    /// <summary>
    /// Verifies that an immutable era is served from the cache regardless of age.
    /// </summary>
    [TestMethod]
    public void TryGet_Whens_immutableEraIsOld_ShouldStillReturnHit()
    {
        FileSystemRbaWorkbookCache cache = new(_directory);
        cache.Store(s_immutableEra, new byte[] { 9 });

        File.SetLastWriteTimeUtc(Path.Combine(_directory, s_immutableEra.FileName), DateTime.UtcNow.AddYears(-5));

        var found = cache.TryGet(s_immutableEra, TimeSpan.FromHours(12), out var bytes);

        Assert.IsTrue(found);
        Assert.IsNotNull(bytes);
    }
}
