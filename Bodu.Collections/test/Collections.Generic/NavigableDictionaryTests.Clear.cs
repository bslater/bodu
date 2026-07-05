// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Clear" /> removes every entry and resets the
    /// count.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDictionaryHasEntries_ShouldRemoveAllEntries()
    {
        var sut = CreateDictionary(1, 2, 3);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.ContainsKey(1));
        Assert.IsFalse(sut.TryGetMinEntry(out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Clear" /> on an empty dictionary is a no-op that
    /// leaves the dictionary usable.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDictionaryIsEmpty_ShouldRemainEmptyAndUsable()
    {
        var sut = new NavigableDictionary<int, int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        sut.Add(1, 100);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that a cleared dictionary accepts new entries and re-sorts them from scratch.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdds_ShouldRebuildSortedContent()
    {
        var sut = CreateDictionary(5, 6, 7);

        sut.Clear();
        sut.Add(3, 300);
        sut.Add(1, 100);
        sut.Add(2, 200);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sut.Select(e => e.Key).ToList());
    }
}
