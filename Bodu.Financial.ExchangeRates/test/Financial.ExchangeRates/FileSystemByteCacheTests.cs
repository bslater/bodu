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
public sealed partial class FileSystemByteCacheTests
{
    /// <summary>The per-test parent of the cache directory, removed with everything in it on cleanup.</summary>
    private string _root = null!;

    /// <summary>The per-test cache directory, inside <see cref="_root" />.</summary>
    private string _directory = null!;

    /// <summary>
    /// Creates a unique parent and cache directory path for each test.
    /// </summary>
    /// <remarks>
    /// The parent belongs to one test alone. The target frameworks' test runs execute concurrently, so a parent shared
    /// between tests can be removed by one run's cleanup while another run is using it.
    /// </remarks>
    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "bodu-byte-cache-tests-" + Guid.NewGuid().ToString("N"));
        _directory = Path.Combine(_root, "cache");
    }

    /// <summary>
    /// Removes the per-test directory tree.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
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

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings =
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
    /// A minimal concrete byte cache over string keys, stored as <c>{key}.bin</c>.
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

        /// <inheritdoc />
        protected override string GetFileName(string key) => $"{key}.bin";
    }
}
