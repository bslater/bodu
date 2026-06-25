// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies serialization and deserialization of lists, dictionaries, and nested collections.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that nested lists and dictionaries serialize and deserialize.</summary>
    [TestMethod]
    public void RoundTrip_WhenNestedCollections_ShouldPreserve()
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

    /// <summary>Verifies that a list of POCOs serializes as a block sequence of mappings and round-trips.</summary>
    [TestMethod]
    public void RoundTrip_WhenListOfPocos_ShouldPreserve()
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

    /// <summary>Verifies that a primitive dictionary deserializes from YAML.</summary>
    [TestMethod]
    public void Deserialize_WhenDictionary_ShouldBind()
    {
        var dict = YamlSerializer.Deserialize<Dictionary<string, int>>("a: 1\nb: 2\nc: 3\n")!;
        Assert.AreEqual(3, dict.Count);
        Assert.AreEqual(2, dict["b"]);
    }
}
