// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.CopyTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> copies entries into the array in iteration order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsLargeEnough_ShouldCopyEntriesInOrder()
    {
        var dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[3];

        dictionary.CopyTo(array, 0);

        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            },
            array);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> honors a non-zero starting index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIndexIsNonZero_ShouldCopyFromIndex()
    {
        var dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[4];

        dictionary.CopyTo(array, 1);

        Assert.AreEqual(default, array[0]);
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), array[1]);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> throws when the destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> throws when the array is too small for the entries.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayTooSmall_ShouldThrowExactly()
    {
        var dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(array, 0);
        });
    }
}
