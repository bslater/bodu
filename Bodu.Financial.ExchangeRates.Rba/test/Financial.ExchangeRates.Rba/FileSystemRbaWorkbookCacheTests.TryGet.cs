// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCacheTests.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class FileSystemRbaWorkbookCacheTests
{
    /// <summary>
    /// Verifies that a missing entry reports a miss.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenEntryMissing_ShouldReturnFalse()
    {
        FileSystemRbaWorkbookCache cache = new(_directory);

        bool found = cache.TryGet(s_currentEra, TimeSpan.FromHours(12), out byte[]? bytes);

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

        bool found = cache.TryGet(s_currentEra, TimeSpan.FromHours(12), out _);

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

        bool found = cache.TryGet(s_immutableEra, TimeSpan.FromHours(12), out byte[]? bytes);

        Assert.IsTrue(found);
        Assert.IsNotNull(bytes);
    }
}
