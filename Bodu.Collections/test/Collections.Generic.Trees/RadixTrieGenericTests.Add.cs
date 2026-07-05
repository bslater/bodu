// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that adding a duplicate key throws <see cref="ArgumentException" /> naming <c>key</c>.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldThrowForKey()
    {
        var sut = new RadixTrie<int>();
        sut.Add("a", 1);

        Assert.AreEqual(
            "key",
            Assert.ThrowsExactly<ArgumentException>(() => sut.Add("a", 2)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RadixTrie{TValue}.TryAdd(string, TValue)" /> adds new keys and rejects duplicates.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNewOrDuplicate_ShouldReportOutcome()
    {
        var sut = new RadixTrie<int>();

        Assert.IsTrue(sut.TryAdd("a", 1));
        Assert.IsFalse(sut.TryAdd("a", 2));
        Assert.AreEqual(1, sut["a"]);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> values are storable and retrievable for reference-typed values.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreAndRetrieveNull()
    {
        var sut = new RadixTrie<string?>();
        sut.Add("k", null);

        Assert.IsTrue(sut.TryGetValue("k", out string? value));
        Assert.IsNull(value);
        Assert.IsTrue(sut.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that an edge split — <c>"team"</c> then <c>"tea"</c> — preserves the values of both keys.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyForcesEdgeSplit_ShouldPreserveBothValues()
    {
        var sut = new RadixTrie<int>();
        sut.Add("team", 10);
        sut.Add("tea", 20);

        Assert.AreEqual(10, sut["team"]);
        Assert.AreEqual(20, sut["tea"]);
        Assert.AreEqual(2, sut.Count);
    }

    /// <summary>
    /// Verifies that the empty string is a valid key with its own value slot.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsEmptyString_ShouldStoreValue()
    {
        var sut = new RadixTrie<int>();
        sut.Add(string.Empty, 7);

        Assert.AreEqual(7, sut[string.Empty]);
        Assert.AreEqual(1, sut.Count);
    }
}
