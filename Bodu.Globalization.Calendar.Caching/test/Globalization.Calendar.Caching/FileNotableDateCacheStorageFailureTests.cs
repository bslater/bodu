// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileNotableDateCacheStorageFailureTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies how the file-backed caches behave when the file system refuses the operation.
/// </summary>
/// <remarks>
/// <para>
/// A file cache is a best-effort accelerator: a failed write must leave the caller's calendar query working, not
/// fault it. That is the default, and <c>ThrowOnStorageFailure</c> is the switch an operator flips when a silent
/// degradation would hide a real problem - so both directions are asserted, because a swallow that cannot be turned
/// off is indistinguishable from a bug.
/// </para>
/// <para>
/// The failure is induced by placing a <em>directory</em> where the cache file belongs. The write is atomic - content
/// goes to a temporary sibling and is then moved into place - so the temporary write succeeds and the move fails,
/// which is exactly the half-completed shape the writer's cleanup path exists for. Permissions are deliberately not
/// used: the tests would then pass vacuously for a privileged user.
/// </para>
/// </remarks>
[TestClass]
public sealed class FileNotableDateCacheStorageFailureTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The scratch directory for this test class, unique per run.</summary>
    private static readonly string Scratch = Path.Combine(Path.GetTempPath(), "bodu-cache-failure-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Creates the scratch directory.
    /// </summary>
    /// <param name="context">The test context.</param>
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _ = context;
        _ = Directory.CreateDirectory(Scratch);
    }

    /// <summary>
    /// Removes the scratch directory.
    /// </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (Directory.Exists(Scratch))
            Directory.Delete(Scratch, recursive: true);
    }

    /// <summary>
    /// Creates a cache directory in which the file for the given territory is already occupied by a directory, so any
    /// attempt to write it fails.
    /// </summary>
    /// <param name="territory">The territory whose cache file is blocked.</param>
    /// <param name="extension">The cache file extension, including the leading dot.</param>
    /// <returns>The cache directory path.</returns>
    private static string BlockedCacheDirectory(string territory, string extension)
    {
        string directory = Path.Combine(Scratch, Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(Path.Combine(directory, territory + extension));

        return directory;
    }

    /// <summary>
    /// Builds a single-year cache entry.
    /// </summary>
    /// <param name="territory">The territory code.</param>
    /// <returns>The entry.</returns>
    private static NotableDateCacheEntry Entry(string territory) =>
        new(territory, 2026, "v1", new[] { NotableDateCacheTestFactory.ObservedHoliday(2026) }, Now);

    /// <summary>
    /// Verifies that a write the file system refuses is reported as unpersisted rather than thrown, so a calendar
    /// query degrades to an uncached answer instead of failing.
    /// </summary>
    /// <param name="format">The cache format under test.</param>
    [TestMethod]
    [DataRow("json")]
    [DataRow("toml")]
    public void StoreYear_WhenTheCacheFileCannotBeWritten_ShouldReportNotStored(string format)
    {
        string directory = BlockedCacheDirectory("US", format == "json" ? ".json" : ".toml");
        var options = new FileNotableDateCacheOptions { CacheDirectory = directory };

        INotableDateCache cache = format == "json"
            ? new JsonNotableDateCache(options)
            : new TomlNotableDateCache(options);

        Assert.AreNotEqual(
            NotableDateCacheWriteStatus.Stored,
            cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that the same write failure is surfaced when the operator has opted into strict storage failures.
    /// </summary>
    /// <remarks>
    /// The option rethrows the underlying storage failure rather than translating it, so the exact type is the
    /// operating system's and differs by platform: blocking the cache file with a directory fails the atomic move
    /// with <see cref="IOException" /> on Unix and <see cref="UnauthorizedAccessException" /> on Windows. Both are
    /// what the cache classifies as a storage failure, so the contract is the pair rather than either member, and
    /// pinning one would make this test pass only on the platform it was written on.
    /// </remarks>
    [TestMethod]
    public void StoreYear_WhenTheCacheFileCannotBeWritten_ForThrowOnStorageFailure_ShouldThrow()
    {
        var options = new FileNotableDateCacheOptions
        {
            CacheDirectory = BlockedCacheDirectory("US", ".json"),
            ThrowOnStorageFailure = true,
        };

        var cache = new JsonNotableDateCache(options);

        Exception exception = Assert.Throws<Exception>(() =>
        {
            _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);
        });

        Assert.IsTrue(
            exception is IOException or UnauthorizedAccessException,
            $"Expected the underlying storage failure (IOException or UnauthorizedAccessException), but got {exception.GetType()}.");
    }

    /// <summary>
    /// Verifies that a failed write leaves no temporary file behind. The write stages content to a sibling before
    /// moving it into place, so a failure between the two steps would otherwise strand a partial file in the cache
    /// directory - once per failed attempt, indefinitely.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenTheWriteFails_ShouldNotLeaveATemporaryFileBehind()
    {
        string directory = BlockedCacheDirectory("US", ".json");
        var cache = new JsonNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = directory });

        for (int attempt = 0; attempt < 3; attempt++)
            _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);

        Assert.IsEmpty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories));
    }

    /// <summary>
    /// Verifies that a write failure does not poison later reads: the territory simply reports a miss, so the caller
    /// recomputes rather than seeing a stale or partial entry.
    /// </summary>
    [TestMethod]
    public void GetYear_AfterAFailedWrite_ShouldReportAMiss()
    {
        var cache = new JsonNotableDateCache(new FileNotableDateCacheOptions
        {
            CacheDirectory = BlockedCacheDirectory("US", ".json"),
        });

        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);

        Assert.IsNull(cache.GetYear("US", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that a failure for one territory does not prevent another from being cached, so a single blocked file
    /// does not disable the cache.
    /// </summary>
    [TestMethod]
    public void StoreYear_WhenOneTerritoryIsBlocked_ShouldStillCacheOthers()
    {
        string directory = BlockedCacheDirectory("US", ".json");
        var cache = new JsonNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = directory });

        _ = cache.StoreYear(Entry("US"), TimeSpan.FromDays(30), Now);

        Assert.AreEqual(
            NotableDateCacheWriteStatus.Stored,
            cache.StoreYear(Entry("CA"), TimeSpan.FromDays(30), Now));
        Assert.IsNotNull(cache.GetYear("CA", 2026, "v1", TimeSpan.FromDays(30), Now));
    }

    /// <summary>
    /// Verifies that the resolved cache directory is exposed, so an operator can see where a cache is actually
    /// writing rather than inferring it from the options.
    /// </summary>
    [TestMethod]
    public void CacheDirectory_ShouldExposeTheResolvedDirectory()
    {
        string directory = Path.Combine(Scratch, Guid.NewGuid().ToString("N"));

        var cache = new JsonNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = directory });

        Assert.AreEqual(directory, cache.CacheDirectory);
    }

    /// <summary>
    /// Verifies that the default cache directory is a fixed folder under the system temporary path, so two caches
    /// configured with no directory share one location rather than each inventing its own.
    /// </summary>
    [TestMethod]
    public void CacheDirectory_WhenNoDirectoryConfigured_ShouldDefaultUnderTheTempPath()
    {
        string resolved = new JsonNotableDateCache(new FileNotableDateCacheOptions()).CacheDirectory;

        Assert.StartsWith(Path.GetTempPath(), resolved);
        Assert.AreEqual(resolved, new TomlNotableDateCache(new FileNotableDateCacheOptions()).CacheDirectory);
    }

    /// <summary>
    /// Verifies that <c>ValidateStorageOnStart</c> creates the cache directory during construction, so a
    /// misconfigured path fails at startup rather than on the first cache write.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValidateStorageOnStart_ShouldCreateTheCacheDirectory()
    {
        string directory = Path.Combine(Scratch, Guid.NewGuid().ToString("N"));
        Assert.IsFalse(Directory.Exists(directory));

        _ = new JsonNotableDateCache(new FileNotableDateCacheOptions
        {
            CacheDirectory = directory,
            ValidateStorageOnStart = true,
        });

        Assert.IsTrue(Directory.Exists(directory));
    }

    /// <summary>
    /// Verifies that a cache directory that cannot be created is reported at construction when storage validation is
    /// requested, rather than being discovered later by a silently failing write.
    /// </summary>
    /// <remarks>
    /// A path whose parent is a file is refused as <see cref="IOException" /> itself on Windows and as the
    /// <see cref="DirectoryNotFoundException" /> specialization on Unix, so the assignable assertion is the
    /// cross-platform contract; this matches how the sibling exchange-rate file cache asserts the same probe.
    /// </remarks>
    [TestMethod]
    public void Ctor_WhenValidateStorageOnStartAndDirectoryIsBlocked_ShouldThrow()
    {
        string blocker = Path.Combine(Scratch, Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocker, "not a directory");

        _ = Assert.Throws<IOException>(() =>
        {
            _ = new JsonNotableDateCache(new FileNotableDateCacheOptions
            {
                CacheDirectory = Path.Combine(blocker, "cache"),
                ValidateStorageOnStart = true,
            });
        });
    }
}
