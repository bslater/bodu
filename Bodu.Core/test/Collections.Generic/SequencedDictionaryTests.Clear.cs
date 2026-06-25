// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Clear" /> removes all entries and resets the count.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldRemoveAllEntries()
    {
        var dictionary = CreatePopulated();

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsFalse(dictionary.TryGetFirst(out _));
        CollectionAssert.AreEqual(Array.Empty<string>(), dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that entries can be added after a clear and resume insertion ordering from empty.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdd_ShouldResumeFromEmpty()
    {
        var dictionary = CreatePopulated();

        dictionary.Clear();
        dictionary.Add("x", 10);

        CollectionAssert.AreEqual(new[] { "x" }, dictionary.Keys.ToArray());
    }
}
