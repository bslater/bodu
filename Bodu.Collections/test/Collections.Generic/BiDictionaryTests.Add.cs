// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that adding a new pair stores the binding in both directions.
    /// </summary>
    [TestMethod]
    public void Add_WhenPairIsNew_ShouldStoreBothDirections()
    {
        var dictionary = new BiDictionary<string, int>();

        dictionary.Add("a", 1);

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(1, dictionary["a"]);
        Assert.IsTrue(dictionary.TryGetKey(1, out string? key));
        Assert.AreEqual("a", key);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Add(TKey, TValue)" /> throws when the key already exists,
    /// following the standard dictionary contract.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldThrowArgumentException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("b", 99);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a pair whose value is already bound to a different key throws
    /// <see cref="ArgumentException" /> under the default <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> policy.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueAlreadyBound_ForThrowPolicy_ShouldThrowArgumentException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("d", 2);
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("duplicate values", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a value conflict under <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> leaves the
    /// dictionary unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueAlreadyBound_ForThrowPolicy_ShouldLeaveStateUnchanged()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("d", 2);
        });

        Assert.AreEqual(3, dictionary.Count);
        Assert.AreEqual(2, dictionary["b"]);
        Assert.IsFalse(dictionary.ContainsKey("d"));
    }

    /// <summary>
    /// Verifies that adding a pair whose value is already bound to a different key evicts the previous binding under
    /// the <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> policy — the old key is removed.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueAlreadyBound_ForReplacePolicy_ShouldEvictPreviousBinding()
    {
        var dictionary = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("c", 2);

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        Assert.AreEqual(2, dictionary["c"]);
        Assert.IsTrue(dictionary.TryGetKey(2, out string? key));
        Assert.AreEqual("c", key);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Add(TKey, TValue)" /> throws when the key is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.Add(null!, "x");
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Add(TKey, TValue)" /> throws when the value is
    /// <see langword="null" />, because values act as keys in the inverse index.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new BiDictionary<string, string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.Add("x", null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a key/value pair via the <see cref="KeyValuePair{TKey, TValue}" /> overload stores the
    /// binding in both directions.
    /// </summary>
    [TestMethod]
    public void Add_WhenPairOverloadUsed_ShouldStoreBothDirections()
    {
        var dictionary = new BiDictionary<string, int>();

        dictionary.Add(new KeyValuePair<string, int>("a", 1));

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsValue(1));
    }

    /// <summary>
    /// Verifies that default value-type keys and values are valid bindings.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAndValueAreDefault_ShouldStoreEntry()
    {
        var dictionary = new BiDictionary<int, int>();

        dictionary.Add(0, 0);

        Assert.IsTrue(dictionary.ContainsKey(0));
        Assert.IsTrue(dictionary.ContainsValue(0));
    }
}
