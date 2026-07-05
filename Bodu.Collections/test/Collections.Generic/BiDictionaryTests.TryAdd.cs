// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.TryAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> adds a new pair and returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenPairIsNew_ShouldReturnTrueAndStore()
    {
        var dictionary = new BiDictionary<string, int>();

        bool added = dictionary.TryAdd("a", 1);

        Assert.IsTrue(added);
        Assert.AreEqual(1, dictionary["a"]);
        Assert.IsTrue(dictionary.ContainsValue(1));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> returns <see langword="false" />
    /// without modifying state when the key already exists.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyAlreadyExists_ShouldReturnFalseAndLeaveStateUnchanged()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool added = dictionary.TryAdd("b", 99);

        Assert.IsFalse(added);
        Assert.AreEqual(3, dictionary.Count);
        Assert.AreEqual(2, dictionary["b"]);
        Assert.IsFalse(dictionary.ContainsValue(99));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> returns <see langword="false" />
    /// without modifying state when the value is already bound to a different key under the
    /// <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> policy.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenValueAlreadyBound_ForThrowPolicy_ShouldReturnFalseAndLeaveStateUnchanged()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool added = dictionary.TryAdd("d", 2);

        Assert.IsFalse(added);
        Assert.AreEqual(3, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("d"));
        Assert.IsTrue(dictionary.TryGetKey(2, out string? key));
        Assert.AreEqual("b", key);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> evicts the previous binding and
    /// returns <see langword="true" /> when the value is already bound under the
    /// <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> policy.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenValueAlreadyBound_ForReplacePolicy_ShouldEvictAndReturnTrue()
    {
        var dictionary = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        bool added = dictionary.TryAdd("c", 2);

        Assert.IsTrue(added);
        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        Assert.AreEqual(2, dictionary["c"]);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> throws when the key is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryAdd(null!, "x");
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.TryAdd(TKey, TValue)" /> throws when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryAdd("x", null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
