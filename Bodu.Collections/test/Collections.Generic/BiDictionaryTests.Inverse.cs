// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Inverse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <c>Inverse.Inverse</c> returns the original instance by reference.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenInverted_ShouldReturnReferenceEqualOriginal()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreSame(dictionary, dictionary.Inverse.Inverse);
    }

    /// <summary>
    /// Verifies that repeated reads of <see cref="BiDictionary{TKey, TValue}.Inverse" /> return the same cached
    /// instance.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenReadTwice_ShouldReturnSameInstance()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreSame(dictionary.Inverse, dictionary.Inverse);
    }

    /// <summary>
    /// Verifies that the inverse view exposes each original pair with keys and values swapped.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenRead_ShouldExposeSwappedPairs()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        BiDictionary<int, string> inverse = dictionary.Inverse;

        Assert.AreEqual(3, inverse.Count);
        Assert.AreEqual("a", inverse[1]);
        Assert.AreEqual("b", inverse[2]);
        Assert.AreEqual("c", inverse[3]);
    }

    /// <summary>
    /// Verifies that a mutation through the inverse view is visible through the original dictionary.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenMutatedThroughView_ShouldBeVisibleInOriginal()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Inverse.Add(4, "d");

        Assert.AreEqual(4, dictionary.Count);
        Assert.AreEqual(4, dictionary["d"]);
        Assert.IsTrue(dictionary.ContainsValue(4));
    }

    /// <summary>
    /// Verifies that a mutation through the original dictionary is visible through the inverse view.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenOriginalMutated_ShouldBeVisibleThroughView()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();
        BiDictionary<int, string> inverse = dictionary.Inverse;

        dictionary.Add("d", 4);
        dictionary.Remove("a");

        Assert.AreEqual(3, inverse.Count);
        Assert.AreEqual("d", inverse[4]);
        Assert.IsFalse(inverse.ContainsKey(1));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Count" /> always mirrors between the original and the
    /// inverse view.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenEitherViewMutated_ShouldMirrorCount()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();
        BiDictionary<int, string> inverse = dictionary.Inverse;

        dictionary.Add("d", 4);
        inverse.Remove(1);

        Assert.AreEqual(dictionary.Count, inverse.Count);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that the inverse view swaps the key and value comparers of the original.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenComparersSupplied_ShouldSwapComparers()
    {
        var dictionary = new BiDictionary<string, string>(StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);

        BiDictionary<string, string> inverse = dictionary.Inverse;

        Assert.AreSame(StringComparer.Ordinal, inverse.KeyComparer);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, inverse.ValueComparer);
    }

    /// <summary>
    /// Verifies that the inverse view shares the original's duplicate-value policy.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenPolicyRead_ShouldMatchOriginal()
    {
        var dictionary = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);

        Assert.AreEqual(BiDictionaryDuplicateValuePolicy.Replace, dictionary.Inverse.DuplicateValuePolicy);
    }

    /// <summary>
    /// Verifies that under the <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> policy, adding through the
    /// inverse view a pair whose value (an original key) is already bound throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenValueConflictThroughView_ForThrowPolicy_ShouldThrowArgumentException()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            // "a" is the inverse view's value side; it is already bound to key 1.
            dictionary.Inverse.Add(9, "a");
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that under the <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> policy, a value conflict
    /// through the inverse view evicts the original binding of that key.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenValueConflictThroughView_ForReplacePolicy_ShouldEvictOriginalBinding()
    {
        var dictionary = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        // "a" swaps to the value side through the inverse; re-binding it evicts the pair ("a", 1).
        dictionary.Inverse.Add(9, "a");

        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual(9, dictionary["a"]);
        Assert.IsFalse(dictionary.ContainsValue(1));
        Assert.AreEqual(2, dictionary["b"]);
    }

    /// <summary>
    /// Verifies that removing through the inverse view removes the pair from both directions of the original.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenRemovedThroughView_ShouldRemoveFromOriginal()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.Inverse.Remove(2);

        Assert.IsTrue(removed);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        Assert.IsFalse(dictionary.ContainsValue(2));
    }
}
