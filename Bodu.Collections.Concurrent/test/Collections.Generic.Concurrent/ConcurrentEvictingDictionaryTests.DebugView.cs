// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the debugger proxy exposes the dictionary's live entries as a snapshot array.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenItemsRead_ShouldReturnSnapshotOfEntries()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        var view = new ConcurrentEvictingDictionaryDebugView<string, int>(dictionary);
        KeyValuePair<string, int>[] items = view.Items;

        Assert.HasCount(2, items);
    }

    /// <summary>
    /// Verifies that constructing the debugger proxy with a <see langword="null" /> dictionary throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenDictionaryIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentEvictingDictionaryDebugView<string, int>(null!);
        });
    }
}
