// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the <see cref="YamlSerializer" /> POCO mapper: serialization, deserialization, collections, dictionaries,
/// enums, naming policies, attributes, and custom converters.
/// </summary>
[TestClass]
public partial class YamlSerializerTests
{
    /// <summary>An enumeration used to test enum serialization.</summary>
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    /// <summary>Verifies that a POCO serializes to block YAML.</summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenPoco_ShouldEmitBlock()
    {
        var yaml = YamlSerializer.Serialize(new Person { Name = "Ada", Age = 36, Active = true });
        Assert.AreEqual("Name: Ada\nAge: 36\nActive: true\n", yaml);
    }

    /// <summary>A simple POCO used across the serializer tests.</summary>
    private sealed class Person
    {
        public string? Name { get; set; }

        public int Age { get; set; }

        public bool Active { get; set; }
    }

    /// <summary>A POCO exercising naming policy and the property-name and ignore attributes.</summary>
    private sealed class Config
    {
        public string? ServerHost { get; set; }

        [YamlPropertyName("port")]
        public int ServerPort { get; set; }

        [YamlIgnore]
        public string? Secret { get; set; }
    }

    /// <summary>A POCO with nested collections.</summary>
    private sealed class Project
    {
        public string? Title { get; set; }

        public List<string>? Tags { get; set; }

        public Dictionary<string, int>? Counts { get; set; }
    }

    /// <summary>A custom converter that reads and writes a value as an uppercase string.</summary>
    private sealed class UpperConverter : YamlConverter<string>
    {
        public override string Read(YamlElement element, YamlSerializerOptions options) =>
            element.GetString().ToUpperInvariant();

        public override void Write(ref Utf8YamlWriter writer, string value, YamlSerializerOptions options) =>
            writer.WriteString(value.ToUpperInvariant());
    }
}
