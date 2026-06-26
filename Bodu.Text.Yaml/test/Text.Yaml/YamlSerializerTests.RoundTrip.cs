// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer" /> serialize-then-deserialize round-trips for POCOs, nested collections,
/// enumerations, and sequences of POCOs.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that a POCO round-trips through serialize and deserialize.</summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPoco_ShouldPreserveValues()
    {
        var original = new Person { Name = "Grace", Age = 45, Active = false };
        var yaml = YamlSerializer.Serialize(original);
        var restored = YamlSerializer.Deserialize<Person>(yaml)!;

        Assert.AreEqual("Grace", restored.Name);
        Assert.AreEqual(45, restored.Age);
        Assert.IsFalse(restored.Active);
    }

    /// <summary>Verifies that nested lists and dictionaries serialize and deserialize.</summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNestedCollections_ShouldPreserve()
    {
        var project = new Project
        {
            Title = "Bodu",
            Tags = ["yaml", "parser"],
            Counts = new Dictionary<string, int> { ["files"] = 12, ["lines"] = 900 },
        };

        var yaml = YamlSerializer.Serialize(project);
        var restored = YamlSerializer.Deserialize<Project>(yaml)!;

        Assert.AreEqual("Bodu", restored.Title);
        CollectionAssert.AreEqual(new[] { "yaml", "parser" }, restored.Tags);
        Assert.AreEqual(12, restored.Counts!["files"]);
        Assert.AreEqual(900, restored.Counts["lines"]);
    }

    /// <summary>Verifies that enums serialize as names by default and parse back.</summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnum_ShouldUseNames()
    {
        var yaml = YamlSerializer.Serialize(Color.Green);
        Assert.AreEqual("Green\n", yaml);
        Assert.AreEqual(Color.Green, YamlSerializer.Deserialize<Color>(yaml));
    }

    /// <summary>Verifies that a list of POCOs serializes as a block sequence of mappings and round-trips.</summary>
    [TestMethod]
    public void SerializeDeserialize_WhenListOfPocos_ShouldPreserve()
    {
        var people = new List<Person>
        {
            new() { Name = "a", Age = 1, Active = true },
            new() { Name = "b", Age = 2, Active = false },
        };

        var yaml = YamlSerializer.Serialize(people);
        var restored = YamlSerializer.Deserialize<List<Person>>(yaml)!;

        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual("b", restored[1].Name);
        Assert.AreEqual(1, restored[0].Age);
    }
}
