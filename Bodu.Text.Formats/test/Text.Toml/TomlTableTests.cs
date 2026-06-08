// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTableTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for <see cref="TomlTable" /> — insertion-order preservation, lookup, duplicate rejection, and
/// null handling.
/// </summary>
[TestClass]
public sealed class TomlTableTests
{
    /// <summary>
    /// Verifies that <see cref="TomlTable.Add(string, TomlValue)" /> preserves insertion order in <see cref="TomlTable.Keys" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenMultipleKeys_ShouldPreserveInsertionOrder()
    {
        var table = new TomlTable
        {
            { "z", new TomlInteger(1) },
            { "a", new TomlInteger(2) },
            { "m", new TomlInteger(3) },
        };

        CollectionAssert.AreEqual(new[] { "z", "a", "m" }, table.Keys.ToArray());
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that adding a duplicate key throws an <see cref="ArgumentException" /> naming the <c>key</c> parameter.
    /// </summary>
    [TestMethod]
    public void Add_WhenDuplicateKey_ShouldThrowExactlyForKey()
    {
        var table = new TomlTable { { "k", new TomlInteger(1) } };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            table.Add("k", new TomlInteger(2));
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the indexer setter overwrites an existing key without changing its position.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigningExistingKey_ShouldOverwriteInPlace()
    {
        var table = new TomlTable
        {
            { "a", new TomlInteger(1) },
            { "b", new TomlInteger(2) },
        };

        table["a"] = new TomlString("updated");

        Assert.AreEqual("updated", ((TomlString)table["a"]).Value);
        CollectionAssert.AreEqual(new[] { "a", "b" }, table.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="TomlTable.TryGetValue(string, out TomlValue)" /> reports presence and absence.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenPresentAndAbsent_ShouldReflectMembership()
    {
        var table = new TomlTable { { "present", new TomlBoolean(true) } };

        Assert.IsTrue(table.TryGetValue("present", out var found));
        Assert.AreEqual(TomlValueKind.Boolean, found.Kind);
        Assert.IsFalse(table.TryGetValue("absent", out var missing));
        Assert.IsNull(missing);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> for an absent key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyAbsent_ShouldThrowKeyNotFound()
    {
        var table = new TomlTable();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = table["missing"];
        });
    }

    /// <summary>
    /// Verifies that adding a null key or null value throws.
    /// </summary>
    [TestMethod]
    public void Add_WhenNullKeyOrValue_ShouldThrowExactly()
    {
        var table = new TomlTable();

        Assert.ThrowsExactly<ArgumentNullException>(() => table.Add(null!, new TomlInteger(1)));
        Assert.ThrowsExactly<ArgumentNullException>(() => table.Add("k", null!));
    }

    /// <summary>
    /// Verifies that a nested table is preserved and reachable through the model.
    /// </summary>
    [TestMethod]
    public void Add_WhenNestedTable_ShouldBeReachable()
    {
        var inner = new TomlTable { { "color", new TomlString("red") } };
        var root = new TomlTable { { "fruit", inner } };

        var fruit = (TomlTable)root["fruit"];

        Assert.AreEqual("red", ((TomlString)fruit["color"]).Value);
    }
}
