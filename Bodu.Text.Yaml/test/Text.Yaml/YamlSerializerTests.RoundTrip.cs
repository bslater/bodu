// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer" /> serialize-then-deserialize round-trips for POCOs, nested collections,
/// enumerations, sequences of POCOs, and a derived type that hides a base property.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that a POCO round-trips through serialize and deserialize.</summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPoco_ShouldPreserveValues()
    {
        var original = new Person { Name = "Grace", Age = 45, Active = false };
        string yaml = YamlSerializer.Serialize(original);
        Person restored = YamlSerializer.Deserialize<Person>(yaml)!;

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

        string yaml = YamlSerializer.Serialize(project);
        Project restored = YamlSerializer.Deserialize<Project>(yaml)!;

        Assert.AreEqual("Bodu", restored.Title);
        CollectionAssert.AreEqual(new[] { "yaml", "parser" }, restored.Tags);
        Assert.AreEqual(12, restored.Counts!["files"]);
        Assert.AreEqual(900, restored.Counts["lines"]);
    }

    /// <summary>Verifies that enums serialize as names by default and parse back.</summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnum_ShouldUseNames()
    {
        string yaml = YamlSerializer.Serialize(Color.Green);
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

        string yaml = YamlSerializer.Serialize(people);
        List<Person> restored = YamlSerializer.Deserialize<List<Person>>(yaml)!;

        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual("b", restored[1].Name);
        Assert.AreEqual(1, restored[0].Age);
    }

    /// <summary>
    /// Verifies that a property hiding a base property with <see langword="new" /> is the one mapped: serializing writes
    /// the single key <c>Value</c>, and reading <c>Value: x</c> sets the derived property, with no duplicate-member
    /// error.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDerivedTypeHidesBaseProperty_ShouldUseDerivedProperty()
    {
        string yaml = YamlSerializer.Serialize(new HidingValueModel { Value = "x" });
        HidingValueModel restored = YamlSerializer.Deserialize<HidingValueModel>("Value: x\n")!;

        Assert.AreEqual("Value: x\n", yaml);
        Assert.AreEqual("x", restored.Value);
    }

    /// <summary>A base model whose property a derived model hides.</summary>
    private class HiddenValueBase
    {
        /// <summary>Gets or sets the base value.</summary>
        /// <value>The base value.</value>
        public object? Value { get; set; }
    }

    /// <summary>A model that hides its base property with a property of another type.</summary>
    private sealed class HidingValueModel
        : HiddenValueBase
    {
        /// <summary>Gets or sets the value that hides the base property.</summary>
        /// <value>The value.</value>
        public new string? Value { get; set; }
    }

    /// <summary>
    /// Verifies that a mapping key longer than the 1024 characters an implicit key may hold round-trips through
    /// serialize and deserialize.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenKeyExceeds1024Characters_ShouldRoundTrip()
    {
        string key = new('k', 2000);
        var original = new Dictionary<string, int> { [key] = 1, ["short"] = 2 };

        string yaml = YamlSerializer.Serialize(original);
        Dictionary<string, int> restored = YamlSerializer.Deserialize<Dictionary<string, int>>(yaml)!;

        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual(1, restored[key]);
        Assert.AreEqual(2, restored["short"]);
    }

    /// <summary>
    /// Verifies that a string that would read back as another value if written plain round-trips as a string, as a key
    /// and as a value, under both specification versions.
    /// </summary>
    /// <param name="value">The string to round-trip.</param>
    /// <param name="version">The specification version the output is read under.</param>
    [TestMethod]
    [DataRow("019", YamlSpecVersion.V1_2)]
    [DataRow("08", YamlSpecVersion.V1_2)]
    [DataRow("019", YamlSpecVersion.V1_1)]
    [DataRow("1:1", YamlSpecVersion.V1_1)]
    [DataRow("<<", YamlSpecVersion.V1_2)]
    [DataRow("<<", YamlSpecVersion.V1_1)]
    public void SerializeDeserialize_WhenStringReadsAsAnotherValuePlain_ShouldRoundTrip(
        string value,
        YamlSpecVersion version)
    {
        var original = new Dictionary<string, string> { [value] = value };

        string yaml = YamlSerializer.Serialize(original);
        Dictionary<string, string> restored = YamlSerializer.Deserialize<Dictionary<string, string>>(
            yaml,
            new YamlSerializerOptions { SpecVersion = version })!;

        Assert.AreEqual(1, restored.Count, yaml);
        Assert.AreEqual(value, restored[value], yaml);
    }
}
