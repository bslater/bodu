// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileParseMemoTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Caching;

/// <summary>
/// Verifies the bounded, stamp-checked memoization contract of <see cref="FileParseMemo{T}" />: hits require an exact
/// last-write stamp match, invalidation and eviction only ever force a re-parse, and capacity is enforced with
/// least-recently-used eviction.
/// </summary>
[TestClass]
public sealed class FileParseMemoTests
{
    /// <summary>The stamp entries are stored against in most tests.</summary>
    private static readonly DateTime Stamp = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Verifies that a stored parse is returned when queried with the same path and stamp.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStampMatches_ShouldReturnStoredValue()
    {
        var memo = new FileParseMemo<string>();
        memo.Set("/cache/a.toml", Stamp, "parsed");

        Assert.IsTrue(memo.TryGet("/cache/a.toml", Stamp, out string? value));
        Assert.AreEqual("parsed", value);
    }

    /// <summary>
    /// Verifies that a moved last-write stamp misses, so an externally rewritten file is always re-parsed.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenStampDiffers_ShouldMiss()
    {
        var memo = new FileParseMemo<string>();
        memo.Set("/cache/a.toml", Stamp, "parsed");

        Assert.IsFalse(memo.TryGet("/cache/a.toml", Stamp.AddSeconds(1), out _));
    }

    /// <summary>
    /// Verifies that an unknown path misses.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenPathUnknown_ShouldMiss()
    {
        var memo = new FileParseMemo<string>();

        Assert.IsFalse(memo.TryGet("/cache/missing.toml", Stamp, out _));
    }

    /// <summary>
    /// Verifies that invalidating a path removes its entry, so the next read re-parses.
    /// </summary>
    [TestMethod]
    public void Invalidate_WhenEntryExists_ShouldForceMiss()
    {
        var memo = new FileParseMemo<string>();
        memo.Set("/cache/a.toml", Stamp, "parsed");

        memo.Invalidate("/cache/a.toml");

        Assert.IsFalse(memo.TryGet("/cache/a.toml", Stamp, out _));
    }

    /// <summary>
    /// Verifies that invalidating an unknown path is a no-op rather than an error.
    /// </summary>
    [TestMethod]
    public void Invalidate_WhenPathUnknown_ShouldNotThrow()
    {
        var memo = new FileParseMemo<string>();

        memo.Invalidate("/cache/missing.toml");

        Assert.IsFalse(memo.TryGet("/cache/missing.toml", Stamp, out _));
    }

    /// <summary>
    /// Verifies that storing a new value for an existing path replaces the prior entry rather than growing the memo.
    /// </summary>
    [TestMethod]
    public void Set_WhenPathAlreadyStored_ShouldReplaceValueAndStamp()
    {
        var memo = new FileParseMemo<string>(capacity: 1);
        memo.Set("/cache/a.toml", Stamp, "first");

        memo.Set("/cache/a.toml", Stamp.AddMinutes(1), "second");

        Assert.IsFalse(memo.TryGet("/cache/a.toml", Stamp, out _), "the old stamp must no longer hit");
        Assert.IsTrue(memo.TryGet("/cache/a.toml", Stamp.AddMinutes(1), out string? value));
        Assert.AreEqual("second", value);
    }

    /// <summary>
    /// Verifies that storing beyond capacity evicts the least recently used entry while keeping the rest.
    /// </summary>
    [TestMethod]
    public void Set_WhenAtCapacity_ShouldEvictLeastRecentlyUsedEntry()
    {
        var memo = new FileParseMemo<string>(capacity: 2);
        memo.Set("/cache/a.toml", Stamp, "a");
        memo.Set("/cache/b.toml", Stamp, "b");

        // Touch a so b becomes the least recently used entry, then push past capacity.
        Assert.IsTrue(memo.TryGet("/cache/a.toml", Stamp, out _));
        memo.Set("/cache/c.toml", Stamp, "c");

        Assert.IsFalse(memo.TryGet("/cache/b.toml", Stamp, out _), "the least recently used entry must be evicted");
        Assert.IsTrue(memo.TryGet("/cache/a.toml", Stamp, out _));
        Assert.IsTrue(memo.TryGet("/cache/c.toml", Stamp, out _));
    }

    /// <summary>
    /// Verifies that an evicted path can be stored again and served correctly, so eviction only ever costs a re-parse.
    /// </summary>
    [TestMethod]
    public void Set_WhenPathWasEvicted_ShouldStoreAndServeAgain()
    {
        var memo = new FileParseMemo<string>(capacity: 1);
        memo.Set("/cache/a.toml", Stamp, "a");
        memo.Set("/cache/b.toml", Stamp, "b");

        memo.Set("/cache/a.toml", Stamp, "a-reparsed");

        Assert.IsTrue(memo.TryGet("/cache/a.toml", Stamp, out string? value));
        Assert.AreEqual("a-reparsed", value);
    }

    /// <summary>
    /// Verifies that a non-positive capacity is rejected.
    /// </summary>
    /// <param name="capacity">The invalid capacity.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenCapacityIsNotPositive_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FileParseMemo<string>(capacity);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }
}
