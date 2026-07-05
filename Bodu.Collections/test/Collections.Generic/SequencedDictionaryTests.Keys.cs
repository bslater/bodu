// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Keys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Keys" /> enumerates keys in iteration order.
    /// </summary>
    [TestMethod]
    public void Keys_WhenEnumerated_ShouldYieldKeysInOrder()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Keys" /> returns the same cached instance on repeated reads.
    /// </summary>
    [TestMethod]
    public void Keys_WhenAccessedRepeatedly_ShouldReturnCachedInstance()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreSame(dictionary.Keys, dictionary.Keys);
    }

    /// <summary>
    /// Verifies that the keys view reflects subsequent mutations to the dictionary.
    /// </summary>
    [TestMethod]
    public void Keys_WhenDictionaryMutated_ShouldReflectChange()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        ICollection<string> keys = dictionary.Keys;

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, keys.ToArray());
    }

    /// <summary>
    /// Verifies that mutating the dictionary through the keys view throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Keys_WhenAddCalled_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        ICollection<string> keys = dictionary.Keys;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            keys.Add("z");
        });
    }
}
