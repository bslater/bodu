// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemByteCacheTests.Sanitize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that a cache key cannot escape the cache directory, and that a read the file system refuses degrades to a
/// miss.
/// </summary>
/// <remarks>
/// The file name is derived from the key, and the key is derived from a currency pair or feed identifier - values a
/// provider composes rather than a user supplies, but the cache is a general-purpose base that a third-party provider
/// package also derives from. A key that names a path is therefore reduced to a single segment before it is combined
/// with the cache directory, so no key can write outside it. Each case below is asserted by where the file actually
/// lands, not by inspecting the derived name.
/// </remarks>
public sealed partial class FileSystemByteCacheTests
{
    /// <summary>
    /// Verifies that a key naming a traversal path is reduced to a single segment inside the cache directory, so the
    /// write cannot escape it.
    /// </summary>
    /// <param name="key">The hostile cache key.</param>
    [TestMethod]
    [DataRow("../escape", DisplayName = "relative traversal")]
    [DataRow("../../escape", DisplayName = "repeated traversal")]
    [DataRow("/etc/passwd", DisplayName = "rooted path")]
    [DataRow("sub/dir/name", DisplayName = "nested segments")]
    public void Store_WhenKeyNamesAPath_ShouldWriteInsideTheCacheDirectory(string key)
    {
        var cache = new TestByteCache(_directory);

        cache.Store(key, [1, 2, 3]);

        string[] written = Directory.GetFiles(_directory);
        Assert.HasCount(1, written);
        Assert.AreEqual(_directory, Path.GetDirectoryName(written[0]));
        Assert.IsTrue(cache.TryGet(key, TimeSpan.FromMinutes(5), out byte[]? read));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, read);
    }

    /// <summary>
    /// Verifies that a degenerate key still resolves to a real file directly inside the cache directory, rather than
    /// to the directory itself or to a nested path.
    /// </summary>
    /// <remarks>
    /// This cache appends an extension to the key before sanitizing, so a bare dot or an empty key can never reduce
    /// to nothing here - the extension always survives. A derived cache whose file name is the key verbatim can
    /// reduce to nothing, which is what the placeholder in the sanitizer exists for; it is not reachable through this
    /// double, and that is a property of the double rather than of the sanitizer.
    /// </remarks>
    /// <param name="key">The degenerate cache key.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty key")]
    [DataRow(".", DisplayName = "current directory")]
    [DataRow("..", DisplayName = "parent directory")]
    [DataRow("sub/", DisplayName = "trailing separator")]
    public void Store_WhenKeyIsDegenerate_ShouldStillWriteASingleSegmentFile(string key)
    {
        var cache = new TestByteCache(_directory);

        cache.Store(key, [1]);

        string[] written = Directory.GetFiles(_directory);
        Assert.HasCount(1, written);
        Assert.AreEqual(_directory, Path.GetDirectoryName(written[0]));
        Assert.IsTrue(cache.TryGet(key, TimeSpan.FromMinutes(5), out _));
    }

    /// <summary>
    /// Verifies that characters the file system rejects are replaced rather than passed through, and that the
    /// replacement is stable - a key must resolve to the same file on every call or the cache never hits.
    /// </summary>
    [TestMethod]
    public void Store_WhenKeyContainsInvalidCharacters_ShouldReplaceThemStably()
    {
        var cache = new TestByteCache(_directory);
        string key = "eur\0usd";

        cache.Store(key, [1]);
        cache.Store(key, [2]);

        Assert.HasCount(1, Directory.GetFiles(_directory));
        Assert.IsTrue(cache.TryGet(key, TimeSpan.FromMinutes(5), out byte[]? read));
        CollectionAssert.AreEqual(new byte[] { 2 }, read);
    }

    /// <summary>
    /// Verifies that two keys differing only outside the final segment collide by design: sanitizing keeps the last
    /// segment, so a derived provider must not rely on a path-shaped key to separate entries.
    /// </summary>
    [TestMethod]
    public void Store_WhenKeysShareTheirFinalSegment_ShouldResolveToTheSameFile()
    {
        var cache = new TestByteCache(_directory);

        cache.Store("a/name", [1]);
        cache.Store("b/name", [2]);

        Assert.HasCount(1, Directory.GetFiles(_directory));
        Assert.IsTrue(cache.TryGet("a/name", TimeSpan.FromMinutes(5), out byte[]? read));
        CollectionAssert.AreEqual(new byte[] { 2 }, read);
    }

    /// <summary>
    /// Verifies that a read the file system refuses degrades to a miss rather than faulting, so a rate lookup falls
    /// back to the network instead of failing.
    /// </summary>
    /// <remarks>
    /// The failure is induced by placing a directory where the cache file belongs: <c>File.Exists</c> is true for
    /// neither, so the read is driven past its early return by writing a real entry first and then replacing it.
    /// </remarks>
    [TestMethod]
    public void TryGet_WhenTheCacheEntryCannotBeRead_ShouldReportAMiss()
    {
        var cache = new TestByteCache(_directory);
        cache.Store("key", [1, 2, 3]);

        string path = Directory.GetFiles(_directory).Single();
        File.Delete(path);
        _ = Directory.CreateDirectory(path);

        Assert.IsFalse(cache.TryGet("key", TimeSpan.FromMinutes(5), out byte[]? read));
        Assert.IsNull(read);
    }

    /// <summary>
    /// Verifies that a write the file system refuses is swallowed, so a failing cache never breaks rate retrieval.
    /// </summary>
    [TestMethod]
    public void Store_WhenTheCacheEntryCannotBeWritten_ShouldSwallowTheFailure()
    {
        var cache = new TestByteCache(_directory);
        cache.Store("key", [1]);

        string path = Directory.GetFiles(_directory).Single();
        File.Delete(path);
        _ = Directory.CreateDirectory(path);

        cache.Store("key", [1, 2, 3]);

        Assert.IsFalse(cache.TryGet("key", TimeSpan.FromMinutes(5), out _));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> payload is rejected rather than written as an empty file, which would
    /// later read back as a valid but wrong cache hit.
    /// </summary>
    [TestMethod]
    public void Store_WhenBytesAreNull_ShouldThrowArgumentNullException()
    {
        var cache = new TestByteCache(_directory);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => cache.Store("key", null!));
    }
}
