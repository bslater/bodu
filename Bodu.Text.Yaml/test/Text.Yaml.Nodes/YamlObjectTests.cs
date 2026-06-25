// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the mutable <see cref="YamlObject" /> mapping container: indexer access, insertion, removal, containment,
/// and key ordering.
/// </summary>
[TestClass]
public sealed class YamlObjectTests
{
    /// <summary>Verifies that the indexer returns the value stored for a key.</summary>
    [TestMethod]
    public void Indexer_WhenKeyPresent_ShouldReturnValue()
    {
        var obj = new YamlObject { ["a"] = YamlValue.Create(1L) };

        Assert.AreEqual(1L, obj["a"]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that the indexer returns null for an absent key.</summary>
    [TestMethod]
    public void Indexer_WhenKeyAbsent_ShouldReturnNull()
    {
        var obj = new YamlObject();

        Assert.IsNull(obj["missing"]);
    }

    /// <summary>Verifies that assigning through the indexer replaces an existing value.</summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReplaceValue()
    {
        var obj = new YamlObject { ["a"] = YamlValue.Create(1L) };
        obj["a"] = YamlValue.Create(2L);

        Assert.AreEqual(2L, obj["a"]!.AsValue().GetValue<long>());
        Assert.AreEqual(1, obj.Count);
    }

    /// <summary>Verifies that <see cref="YamlObject.Add" /> inserts a new key and increments the count.</summary>
    [TestMethod]
    public void Add_WhenNewKey_ShouldInsert()
    {
        var obj = new YamlObject();
        obj.Add("a", YamlValue.Create("x"));

        Assert.AreEqual(1, obj.Count);
        Assert.AreEqual("x", obj["a"]!.AsValue().GetValue<string>());
    }

    /// <summary>Verifies that <see cref="YamlObject.ContainsKey" /> reports presence.</summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresent_ShouldReturnTrue()
    {
        var obj = new YamlObject { ["a"] = YamlValue.Create(1L) };

        Assert.IsTrue(obj.ContainsKey("a"));
        Assert.IsFalse(obj.ContainsKey("b"));
    }

    /// <summary>Verifies that <see cref="YamlObject.Remove" /> removes a present key and reports success.</summary>
    [TestMethod]
    public void Remove_WhenKeyPresent_ShouldRemoveAndReturnTrue()
    {
        var obj = new YamlObject { ["a"] = YamlValue.Create(1L) };

        Assert.IsTrue(obj.Remove("a"));
        Assert.AreEqual(0, obj.Count);
        Assert.IsFalse(obj.ContainsKey("a"));
    }

    /// <summary>Verifies that <see cref="YamlObject.Remove" /> returns false for an absent key.</summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        var obj = new YamlObject();

        Assert.IsFalse(obj.Remove("missing"));
    }

    /// <summary>Verifies that keys are reported in insertion order.</summary>
    [TestMethod]
    public void Keys_WhenMultipleAdded_ShouldPreserveInsertionOrder()
    {
        var obj = new YamlObject();
        obj.Add("b", YamlValue.Create(1L));
        obj.Add("a", YamlValue.Create(2L));
        obj.Add("c", YamlValue.Create(3L));

        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, obj.Keys.ToArray());
    }
}
