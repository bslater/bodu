// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that a <see langword="null" /> value can be stored and retrieved under a key.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreAndRetrieveNull()
    {
        var mvd = new MultiValueDictionary<string, string?>();

        mvd.Add("k", null);

        Assert.AreEqual(1, mvd.Count);
        Assert.IsNull(mvd["k"][0]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange" /> preserves
    /// <see langword="null" /> values and their insertion order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenValuesContainNull_ShouldPreserveNullEntries()
    {
        var mvd = new MultiValueDictionary<string, string?>();

        mvd.AddRange("k", ["a", null, "b"]);

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual("a", mvd["k"][0]);
        Assert.IsNull(mvd["k"][1]);
        Assert.AreEqual("b", mvd["k"][2]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue" /> returns
    /// <see langword="true" /> when the stored value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsNull_ShouldReturnTrue()
    {
        var mvd = new MultiValueDictionary<string, string?>();
        mvd.Add("k", null);

        Assert.IsTrue(mvd.ContainsValue("k", null));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> value can be removed from a key's value list.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsNull_ShouldRemoveNullEntry()
    {
        var mvd = new MultiValueDictionary<string, string?>();
        mvd.Add("k", null);
        mvd.Add("k", "other");

        bool removed = mvd.Remove("k", null);

        Assert.IsTrue(removed);
        Assert.AreEqual(1, mvd.Count);
        CollectionAssert.AreEqual(new[] { "other" }, mvd["k"].ToList());
    }

}
