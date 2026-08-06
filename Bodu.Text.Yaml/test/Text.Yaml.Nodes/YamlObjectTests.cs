// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the mapping surface of <see cref="YamlObject" />, the mutable mapping node.
/// </summary>
[TestClass]
public sealed class YamlObjectTests
{
    /// <summary>
    /// Verifies that a new mapping is empty.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultConstructed_ShouldBeEmpty()
    {
        var sut = new YamlObject();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.Keys.Count);
    }

    /// <summary>
    /// Verifies that <see cref="YamlObject.Add(string, YamlNode)" /> stores the entry and that
    /// <see cref="YamlObject.Keys" /> preserves insertion order.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeysAdded_ShouldPreserveInsertionOrder()
    {
        var sut = new YamlObject();
        sut.Add("beta", YamlValue.Create("2"));
        sut.Add("alpha", YamlValue.Create("1"));

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { "beta", "alpha" }, sut.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="YamlObject.Add(string, YamlNode)" /> accepts a <see langword="null" /> node, which
    /// represents an explicit YAML null rather than an absent key.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreNullEntry()
    {
        var sut = new YamlObject();
        sut.Add("empty", null);

        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.ContainsKey("empty"));
        Assert.IsTrue(sut.TryGetValue("empty", out YamlNode? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that <see cref="YamlObject.ContainsKey(string)" /> distinguishes present from absent keys.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresent_ShouldReturnTrue()
    {
        var sut = new YamlObject();
        sut.Add("name", YamlValue.Create("Ada"));

        Assert.IsTrue(sut.ContainsKey("name"));
        Assert.IsFalse(sut.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="YamlObject.TryGetValue(string, out YamlNode)" /> reports failure for an absent key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyAbsent_ShouldReturnFalse()
    {
        var sut = new YamlObject();

        Assert.IsFalse(sut.TryGetValue("missing", out YamlNode? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that <see cref="YamlObject.Remove(string)" /> drops the key from both the lookup and the ordered key
    /// list, so a later write does not emit a stale entry.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyPresent_ShouldRemoveFromLookupAndOrder()
    {
        var sut = new YamlObject();
        sut.Add("name", YamlValue.Create("Ada"));
        sut.Add("age", YamlValue.Create("36"));

        Assert.IsTrue(sut.Remove("name"));

        Assert.AreEqual(1, sut.Count);
        Assert.IsFalse(sut.ContainsKey("name"));
        CollectionAssert.AreEqual(new[] { "age" }, sut.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="YamlObject.Remove(string)" /> reports failure for an absent key and leaves the
    /// mapping unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        var sut = new YamlObject();
        sut.Add("name", YamlValue.Create("Ada"));

        Assert.IsFalse(sut.Remove("missing"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the mapping enumerates its entries in insertion order through both the generic and the
    /// non-generic enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_ShouldYieldEntriesInInsertionOrder()
    {
        var sut = new YamlObject();
        sut.Add("first", YamlValue.Create("1"));
        sut.Add("second", YamlValue.Create("2"));

        var keys = new List<string>();
        foreach (KeyValuePair<string, YamlNode?> entry in sut)
        {
            keys.Add(entry.Key);
        }

        CollectionAssert.AreEqual(new[] { "first", "second" }, keys);

        var count = 0;
        foreach (object? _ in (IEnumerable)sut)
        {
            count++;
        }

        Assert.AreEqual(2, count);
    }
}
