// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that a struct key with the same field values is treated as the same key regardless of
    /// which instance is used, confirming value-type equality semantics for dictionary lookup.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new MultiValueDictionary<Coord, string>();

        mvd.Add(new Coord(1, 2), "alpha");
        mvd.Add(new Coord(1, 2), "beta");

        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(2, mvd.Count);
        Assert.HasCount(2, mvd[new Coord(1, 2)]);
    }

    /// <summary>
    /// Verifies that two struct keys with different field values are treated as distinct keys.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsValueTypeStructWithDifferentFields_ShouldStoreSeparately()
    {
        var mvd = new MultiValueDictionary<Coord, string>();

        mvd.Add(new Coord(1, 2), "a");
        mvd.Add(new Coord(3, 4), "b");

        Assert.AreEqual(2, mvd.KeyCount);
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual("a", mvd[new Coord(1, 2)][0]);
        Assert.AreEqual("b", mvd[new Coord(3, 4)][0]);
    }

    /// <summary>
    /// Verifies that struct values are stored and retrieved by value, and that insertion order is preserved.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsValueTypeStruct_ShouldStoreAndRetrieveByValue()
    {
        var mvd = new MultiValueDictionary<string, Coord>();
        mvd.Add("grid", new Coord(1, 2));
        mvd.Add("grid", new Coord(3, 4));

        IReadOnlyList<Coord> values = mvd["grid"];

        Assert.HasCount(2, values);
        Assert.AreEqual(new Coord(1, 2), values[0]);
        Assert.AreEqual(new Coord(3, 4), values[1]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey" /> matches a struct key by
    /// value equality, not by instance identity.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new MultiValueDictionary<Coord, string>();
        mvd.Add(new Coord(7, 8), "x");

        Assert.IsTrue(mvd.ContainsKey(new Coord(7, 8)));
        Assert.IsFalse(mvd.ContainsKey(new Coord(7, 9)));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue" /> locates a struct value
    /// by value equality, not by reference.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new MultiValueDictionary<string, Coord>();
        mvd.Add("k", new Coord(5, 6));

        Assert.IsTrue(mvd.ContainsValue("k", new Coord(5, 6)));
        Assert.IsFalse(mvd.ContainsValue("k", new Coord(5, 7)));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)" /> removes a struct
    /// value by value equality.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsValueTypeStruct_ShouldRemoveByValue()
    {
        var mvd = new MultiValueDictionary<string, Coord>();
        mvd.Add("k", new Coord(1, 2));
        mvd.Add("k", new Coord(3, 4));

        var removed = mvd.Remove("k", new Coord(1, 2));

        Assert.IsTrue(removed);
        Assert.AreEqual(1, mvd.Count);
        Assert.IsFalse(mvd.ContainsValue("k", new Coord(1, 2)));
        Assert.IsTrue(mvd.ContainsValue("k", new Coord(3, 4)));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll" /> removes the correct entry
    /// when the key is a value-type struct, located by value equality.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyIsValueTypeStruct_ShouldRemoveByValue()
    {
        var mvd = new MultiValueDictionary<Coord, string>();
        mvd.Add(new Coord(1, 2), "a");
        mvd.Add(new Coord(3, 4), "b");

        var removed = mvd.RemoveAll(new Coord(1, 2));

        Assert.IsTrue(removed);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey(new Coord(1, 2)));
        Assert.IsTrue(mvd.ContainsKey(new Coord(3, 4)));
    }

}
