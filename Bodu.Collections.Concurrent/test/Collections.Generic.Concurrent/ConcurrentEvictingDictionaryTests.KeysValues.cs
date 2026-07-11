// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.KeysValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the keys snapshot contains exactly the stored keys.
    /// </summary>
    [TestMethod]
    public void Keys_WhenEntriesExist_ShouldContainExactlyStoredKeys()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        IReadOnlyCollection<string> keys = dictionary.Keys;

        Assert.HasCount(2, keys);
        Assert.Contains("a", keys);
        Assert.Contains("b", keys);
    }

    /// <summary>
    /// Verifies that the values snapshot contains exactly the stored values.
    /// </summary>
    [TestMethod]
    public void Values_WhenEntriesExist_ShouldContainExactlyStoredValues()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        IReadOnlyCollection<int> values = dictionary.Values;

        Assert.HasCount(2, values);
        Assert.Contains(1, values);
        Assert.Contains(2, values);
    }

    /// <summary>
    /// Verifies that the keys collection is a snapshot, not a live view: entries added afterward are not reflected.
    /// </summary>
    [TestMethod]
    public void Keys_WhenMutatedAfterRead_ShouldNotReflectMutations()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        IReadOnlyCollection<string> keys = dictionary.Keys;
        dictionary.Add("b", 2);

        Assert.HasCount(1, keys);
    }

    /// <summary>
    /// Verifies that the values collection is a snapshot, not a live view: entries added afterward are not reflected.
    /// </summary>
    [TestMethod]
    public void Values_WhenMutatedAfterRead_ShouldNotReflectMutations()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        IReadOnlyCollection<int> values = dictionary.Values;
        dictionary.Add("b", 2);

        Assert.HasCount(1, values);
    }

    /// <summary>
    /// Verifies that the keys and values snapshots of an empty dictionary are empty.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenEmpty_ShouldBeEmpty()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsEmpty(dictionary.Keys);
        Assert.IsEmpty(dictionary.Values);
    }
}
