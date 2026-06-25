// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the mutable <see cref="YamlArray" /> sequence container: indexer access, append, insert, removal, and
/// count.
/// </summary>
[TestClass]
public sealed class YamlArrayTests
{
    /// <summary>Verifies that the indexer returns the element at the requested position.</summary>
    [TestMethod]
    public void Indexer_WhenIndexInRange_ShouldReturnElement()
    {
        var array = new YamlArray { YamlValue.Create(1L), YamlValue.Create(2L) };

        Assert.AreEqual(2L, array[1]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that assigning through the indexer replaces the element at that position.</summary>
    [TestMethod]
    public void Indexer_WhenAssigned_ShouldReplaceElement()
    {
        var array = new YamlArray { YamlValue.Create(1L) };
        array[0] = YamlValue.Create(9L);

        Assert.AreEqual(9L, array[0]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that <see cref="YamlArray.Add" /> appends an element and increments the count.</summary>
    [TestMethod]
    public void Add_WhenElement_ShouldAppend()
    {
        var array = new YamlArray();
        array.Add(YamlValue.Create("x"));

        Assert.AreEqual(1, array.Count);
        Assert.AreEqual("x", array[0]!.AsValue().GetValue<string>());
    }

    /// <summary>Verifies that <see cref="YamlArray.Insert" /> places an element at the requested index.</summary>
    [TestMethod]
    public void Insert_WhenIndexGiven_ShouldShiftElements()
    {
        var array = new YamlArray { YamlValue.Create(1L), YamlValue.Create(3L) };
        array.Insert(1, YamlValue.Create(2L));

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(2L, array[1]!.AsValue().GetValue<long>());
        Assert.AreEqual(3L, array[2]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that <see cref="YamlArray.RemoveAt" /> removes the element at the requested index.</summary>
    [TestMethod]
    public void RemoveAt_WhenIndexGiven_ShouldRemoveElement()
    {
        var array = new YamlArray { YamlValue.Create(1L), YamlValue.Create(2L), YamlValue.Create(3L) };
        array.RemoveAt(1);

        Assert.AreEqual(2, array.Count);
        Assert.AreEqual(3L, array[1]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that <see cref="YamlArray.Count" /> reflects the number of elements.</summary>
    [TestMethod]
    public void Count_WhenElementsAdded_ShouldReflectTotal()
    {
        var array = new YamlArray();

        Assert.AreEqual(0, array.Count);
        array.Add(YamlValue.Create(1L));
        array.Add(YamlValue.Create(2L));
        Assert.AreEqual(2, array.Count);
    }
}
