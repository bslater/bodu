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

    /// <summary>An enumeration used to test enum serialization.</summary>
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    /// <summary>Verifies that a POCO serializes to block YAML.</summary>
    [TestMethod]
    public void Serialize_WhenPoco_ShouldEmitBlock()
    {
        var yaml = YamlSerializer.Serialize(new Person { Name = "Ada", Age = 36, Active = true });
        Assert.AreEqual("Name: Ada\nAge: 36\nActive: true\n", yaml);
    }

    /// <summary>Verifies that the naming policy and property-name and ignore attributes are honored.</summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicyAndAttributes_ShouldApply()
    {
        var options = new YamlSerializerOptions { PropertyNamingPolicy = YamlNamingPolicy.SnakeCaseLower };
        var yaml = YamlSerializer.Serialize(new Config { ServerHost = "h", ServerPort = 8080, Secret = "x" }, options);

        Assert.AreEqual("server_host: h\nport: 8080\n", yaml);
    }

    /// <summary>Verifies that a primitive dictionary deserializes from YAML.</summary>
    [TestMethod]
    public void Deserialize_WhenDictionary_ShouldBind()
    {
        var dict = YamlSerializer.Deserialize<Dictionary<string, int>>("a: 1\nb: 2\nc: 3\n")!;
        Assert.AreEqual(3, dict.Count);
        Assert.AreEqual(2, dict["b"]);
    }

    /// <summary>Verifies that the loosely-typed object binding produces nested dictionaries and lists.</summary>
    [TestMethod]
    public void Deserialize_WhenObject_ShouldBindDynamic()
    {
        var result = YamlSerializer.Deserialize<object>("name: x\nitems:\n  - 1\n  - 2\n");
        var map = (Dictionary<string, object?>)result!;
        Assert.AreEqual("x", map["name"]);
        var items = (List<object?>)map["items"]!;
        Assert.AreEqual(1L, items[0]);
    }

    /// <summary>Verifies that null members are omitted when the corresponding option is enabled.</summary>
    [TestMethod]
    public void Serialize_WhenIgnoreNullValues_ShouldOmit()
    {
        var options = new YamlSerializerOptions { IgnoreNullValues = true };
        var yaml = YamlSerializer.Serialize(new Person { Name = null, Age = 5, Active = true }, options);
        Assert.AreEqual("Age: 5\nActive: true\n", yaml);
    }

    /// <summary>A custom converter that reads and writes a value as an uppercase string.</summary>
    private sealed class UpperConverter : YamlConverter<string>
    {
        public override string Read(YamlElement element, YamlSerializerOptions options) =>
            element.GetString().ToUpperInvariant();

        public override void Write(ref Utf8YamlWriter writer, string value, YamlSerializerOptions options) =>
            writer.WriteString(value.ToUpperInvariant());
    }

    /// <summary>Verifies that a registered custom converter is used for its type.</summary>
    [TestMethod]
    public void Serialize_WhenCustomConverter_ShouldApply()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new UpperConverter());
        var yaml = YamlSerializer.Serialize("hello", options);
        Assert.AreEqual("HELLO\n", yaml);
    }

    /// <summary>Verifies that case-insensitive property matching binds differently-cased keys.</summary>
    [TestMethod]
    public void Deserialize_WhenCaseInsensitive_ShouldBind()
    {
        var options = new YamlSerializerOptions { PropertyNameCaseInsensitive = true };
        var person = YamlSerializer.Deserialize<Person>("name: Eve\nAGE: 30\nactive: true\n", options)!;
        Assert.AreEqual("Eve", person.Name);
        Assert.AreEqual(30, person.Age);
        Assert.IsTrue(person.Active);
    }
}
