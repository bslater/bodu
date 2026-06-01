// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that clearing an already-empty dictionary is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        mvd.Clear();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that calling <see cref="MultiValueDictionary{TKey,TValue}.Clear"/> on an already-empty dictionary leaves it in a valid empty state.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Clear();

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.KeyCount);
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Clear"/> removes all entries.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllKeysAndValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        mvd.Clear();

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that entries added after <see cref="MultiValueDictionary{TKey,TValue}.Clear"/> are tracked correctly.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledThenItemsReAdded_ShouldTrackNewItems()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("old", 1);
        mvd.Clear();

        mvd.Add("new", 10);
        mvd.Add("new", 20);

        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("old"));
        Assert.IsTrue(mvd.ContainsKey("new"));
    }

}
