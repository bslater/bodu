// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileRateCacheTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class TomlFileRateCacheTests
{
    /// <summary>
    /// Verifies that a corrupt cache file treated as empty is logged at <see cref="LogLevel.Warning" /> with the
    /// file's path, its reserved event id, and the swallowed deserialization exception, so the corruption is not
    /// silent.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileIsCorruptAndLoggerSupplied_ShouldLogCorruptFileWarning()
    {
        var logger = new CapturingLogger();
        var cache = new TomlFileRateCache(
            new FileRateCacheOptions { Provider = Provider, CacheDirectory = _directory },
            timeProvider: null,
            logger: logger);
        Directory.CreateDirectory(Path.Combine(_directory, Provider));
        File.WriteAllText(PairFilePath, "this is = not [valid toml");

        _ = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings =
            logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();

        Assert.HasCount(1, warnings);
        Assert.AreEqual(4514, warnings[0].EventId.Id, "corruption uses the file cache's reserved event id");
        Assert.IsNotNull(warnings[0].Exception, "the swallowed exception is attached to the warning");
        Assert.IsTrue(warnings[0].Message.Contains(PairFilePath, StringComparison.Ordinal), "the warning names the corrupt file");
        Assert.IsTrue(warnings[0].Message.Contains(Provider, StringComparison.Ordinal), "the warning names the provider");
    }

    /// <summary>
    /// Verifies that a swallowed storage write failure is logged at <see cref="LogLevel.Warning" /> with the file
    /// cache's reserved event id, so best-effort degradation surfaces to operators.
    /// </summary>
    [TestMethod]
    public void Store_WhenDirectoryIsAFileAndLoggerSupplied_ShouldLogStorageFailureWarning()
    {
        // Make the provider directory path unusable by placing a file where the directory must be created.
        string blockedDirectory = Path.Combine(_directory, "blocked");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(blockedDirectory, "not a directory");

        var logger = new CapturingLogger();
        var cache = new TomlFileRateCache(
            new FileRateCacheOptions { Provider = Provider, CacheDirectory = Path.Combine(blockedDirectory, "sub") },
            timeProvider: null,
            logger: logger);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2024, 6, 3), 0.66m, now) }, Duration, now);

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings =
            logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();

        Assert.HasCount(1, warnings);
        Assert.AreEqual(4513, warnings[0].EventId.Id, "the degradation uses the file cache's reserved event id");
        Assert.IsNotNull(warnings[0].Exception, "the swallowed exception is attached to the warning");
    }

    /// <summary>
    /// Verifies that, with no logger supplied, a corrupt cache file still degrades to an empty read without throwing,
    /// so the logging path is null-safe.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileIsCorruptAndNoLogger_ShouldNotThrow()
    {
        TomlFileRateCache cache = CreateCache();
        Directory.CreateDirectory(Path.Combine(_directory, Provider));
        File.WriteAllText(PairFilePath, "this is = not [valid toml");

        IReadOnlyList<CachedRate> read = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(read);
    }
}
