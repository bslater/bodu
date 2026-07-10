// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the debugger proxy exposes the cache's entries as a snapshot array.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenItemsRead_ShouldReturnSnapshotOfEntries()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);
        cache.Add("b", 2);

        var view = new ConcurrentLruCacheDebugView<string, int>(cache);
        KeyValuePair<string, int>[] items = view.Items;

        Assert.HasCount(2, items);
    }

    /// <summary>
    /// Verifies that constructing the debugger proxy with a <see langword="null" /> cache throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenCacheIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentLruCacheDebugView<string, int>(null!);
        });
    }
}
