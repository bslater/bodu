// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemByteCacheTests.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class FileSystemByteCacheTests
{
    /// <summary>
    /// Verifies that a cache hit reports the cache file's last-write time as the instant the entry was stored.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenHitWithStoredTime_ShouldReportTheFileWriteTime()
    {
        var cache = new TestByteCache(_directory);
        cache.Store("key", [1, 2, 3]);
        string path = Path.Combine(_directory, "key.bin");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-3));
        DateTimeOffset written = new(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

        bool hit = cache.TryGet("key", TimeSpan.FromDays(1), out byte[]? bytes, out DateTimeOffset? storedAtUtc);

        Assert.IsTrue(hit);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes);
        Assert.AreEqual(written, storedAtUtc);
    }

    /// <summary>
    /// Verifies that a miss reports no bytes and no stored time.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenMissWithStoredTime_ShouldReportNoStoredTime()
    {
        var cache = new TestByteCache(_directory);

        bool hit = cache.TryGet("missing", TimeSpan.FromDays(1), out byte[]? bytes, out DateTimeOffset? storedAtUtc);

        Assert.IsFalse(hit);
        Assert.IsNull(bytes);
        Assert.IsNull(storedAtUtc);
    }

    /// <summary>
    /// Verifies that a stale entry is a miss with no stored time.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStaleWithStoredTime_ShouldReportNoStoredTime()
    {
        var cache = new TestByteCache(_directory);
        cache.Store("key", [1, 2, 3]);
        File.SetLastWriteTimeUtc(Path.Combine(_directory, "key.bin"), DateTime.UtcNow.AddDays(-2));

        bool hit = cache.TryGet("key", TimeSpan.FromDays(1), out _, out DateTimeOffset? storedAtUtc);

        Assert.IsFalse(hit);
        Assert.IsNull(storedAtUtc);
    }

    /// <summary>
    /// Verifies that a cache implementing only the original lookup still answers the lookup with a stored time,
    /// through the interface's default implementation, reporting no stored time.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenCacheDoesNotRecordStoredTime_ShouldReportNoStoredTime()
    {
        IByteCache<string> cache = new MemoryOnlyByteCache();
        cache.Store("key", [1, 2, 3]);

        bool hit = cache.TryGet("key", TimeSpan.FromDays(1), out byte[]? bytes, out DateTimeOffset? storedAtUtc);

        Assert.IsTrue(hit);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes);
        Assert.IsNull(storedAtUtc);
    }

    /// <summary>
    /// An <see cref="IByteCache{TKey}" /> that keeps entries in memory and implements only the original lookup.
    /// </summary>
    private sealed class MemoryOnlyByteCache
        : IByteCache<string>
    {
        /// <summary>The stored entries.</summary>
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        /// <inheritdoc />
        public bool TryGet(string key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes) =>
            _entries.TryGetValue(key, out bytes);

        /// <inheritdoc />
        public void Store(string key, byte[] bytes) =>
            _entries[key] = bytes;
    }
}
