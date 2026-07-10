// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemByteCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the best-effort file-system behaviour of <see cref="FileSystemByteCache{TKey}" />, including the
/// degradation logging emitted when a swallowed failure occurs.
/// </summary>
[TestClass]
public sealed class FileSystemByteCacheTests
{
    /// <summary>The per-test cache root, removed on cleanup.</summary>
    private string _directory = null!;

    /// <summary>
    /// Creates a unique cache directory for each test.
    /// </summary>
    [TestInitialize]
    public void Initialize() =>
        _directory = Path.Combine(Path.GetTempPath(), "bodu-byte-cache-tests", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Removes the per-test cache directory.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        string root = Path.GetDirectoryName(_directory)!;
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
        if (File.Exists(_directory))
            File.Delete(_directory);
        if (Directory.Exists(root) && !Directory.EnumerateFileSystemEntries(root).Any())
            Directory.Delete(root);
    }

    /// <summary>
    /// Verifies that a stored payload round-trips through a fresh read.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStored_ShouldRoundTripBytes()
    {
        var cache = new TestByteCache(_directory);
        byte[] payload = [1, 2, 3];

        cache.Store("key", payload);

        Assert.IsTrue(cache.TryGet("key", TimeSpan.FromMinutes(5), out byte[]? read));
        CollectionAssert.AreEqual(payload, read);
    }

    /// <summary>
    /// Verifies that a swallowed write failure is logged at <see cref="LogLevel.Warning" /> with the byte cache's
    /// reserved event id and the destination path, so the skipped write is not silent.
    /// </summary>
    [TestMethod]
    public void Store_WhenDirectoryIsAFileAndLoggerSupplied_ShouldLogStoreFailureWarning()
    {
        // Occupy the cache-directory path with a file so CreateDirectory fails with IOException.
        Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
        File.WriteAllText(_directory, "not a directory");

        var logger = new CapturingLogger();
        var cache = new TestByteCache(_directory, logger);

        cache.Store("key", [1, 2, 3]);

        List<(LogLevel Level, EventId EventId, string Message)> warnings =
            logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();

        Assert.HasCount(1, warnings);
        Assert.AreEqual(4411, warnings[0].EventId.Id, "the skipped write uses the byte cache's reserved event id");
        Assert.IsTrue(warnings[0].Message.Contains("key.bin", StringComparison.Ordinal), "the warning names the destination file");
    }

    /// <summary>
    /// Verifies that, with no logger supplied, a swallowed write failure still degrades gracefully without throwing,
    /// so the logging path is null-safe.
    /// </summary>
    [TestMethod]
    public void Store_WhenDirectoryIsAFileAndNoLogger_ShouldNotThrow()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_directory)!);
        File.WriteAllText(_directory, "not a directory");

        var cache = new TestByteCache(_directory);

        cache.Store("key", [1, 2, 3]);

        Assert.IsFalse(cache.TryGet("key", TimeSpan.FromMinutes(5), out _));
    }

    /// <summary>
    /// Verifies that a key whose derived file name attempts directory traversal cannot escape the cache directory:
    /// the payload is written inside the cache directory, never at the traversal target in the parent.
    /// </summary>
    [TestMethod]
    public void Store_WhenKeyAttemptsPathTraversal_ShouldStayWithinCacheDirectory()
    {
        var cache = new TestByteCache(_directory);
        string escapeTarget = Path.Combine(Path.GetDirectoryName(_directory)!, "pwned.bin");

        try
        {
            cache.Store("../pwned", [1, 2, 3]);

            Assert.IsFalse(File.Exists(escapeTarget), "a traversal key escaped the cache directory");
        }
        finally
        {
            if (File.Exists(escapeTarget))
                File.Delete(escapeTarget);
        }
    }

    /// <summary>
    /// A minimal concrete byte cache exposing the protected core operations for testing.
    /// </summary>
    private sealed class TestByteCache
        : FileSystemByteCache<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestByteCache" /> class.
        /// </summary>
        /// <param name="directory">The cache directory.</param>
        /// <param name="logger">The optional degradation logger.</param>
        public TestByteCache(string? directory, ILogger? logger = null)
            : base(directory, "bodu-byte-cache-tests", logger) { }

        /// <summary>
        /// Stores the payload for a key through the protected core.
        /// </summary>
        /// <param name="key">The download-unit key.</param>
        /// <param name="bytes">The payload.</param>
        public void Store(string key, byte[] bytes) => StoreCore(key, bytes);

        /// <summary>
        /// Attempts to read the payload for a key through the protected core.
        /// </summary>
        /// <param name="key">The download-unit key.</param>
        /// <param name="refreshInterval">The maximum acceptable file age.</param>
        /// <param name="bytes">The payload read, when fresh.</param>
        /// <returns><see langword="true" /> when fresh bytes were read.</returns>
        public bool TryGet(string key, TimeSpan refreshInterval, out byte[]? bytes) =>
            TryGetCore(key, refreshInterval, out bytes);

        /// <inheritdoc />
        protected override string GetFileName(string key) => $"{key}.bin";
    }
}
