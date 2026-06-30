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
        SequencedDictionary<string, int> dictionary = CreatePopulated();
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
        SequencedDictionary<string, int> dictionary = CreatePopulated();
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
        SequencedDictionary<string, int> dictionary = CreatePopulated();

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
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> throws when the index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIndexIsNegative_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            dictionary.CopyTo(array, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> throws when the index equals the array length and entries remain to copy.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIndexEqualsLengthAndNotEmpty_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[3];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(array, 3);
        });
    }

    /// <summary>
    /// Verifies that copying an empty dictionary at an index equal to the array length is a no-op.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenEmptyAndIndexEqualsLength_ShouldCopyNothing()
    {
        var dictionary = new SequencedDictionary<string, int>();
        var array = new KeyValuePair<string, int>[2];

        dictionary.CopyTo(array, 2);

        CollectionAssert.AreEqual(new KeyValuePair<string, int>[2], array);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.CopyTo" /> into a larger array leaves the trailing slots untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayLargerThanCount_ShouldLeaveTrailingSlotsUntouched()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        var array = new KeyValuePair<string, int>[5];

        dictionary.CopyTo(array, 0);

        Assert.AreEqual(new KeyValuePair<string, int>("c", 3), array[2]);
        Assert.AreEqual(default, array[3]);
        Assert.AreEqual(default, array[4]);
    }
}
