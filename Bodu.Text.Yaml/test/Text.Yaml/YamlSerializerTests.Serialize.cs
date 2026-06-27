// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer.Serialize{TValue}(TValue, YamlSerializerOptions)" />: block emission, naming
/// policy and attributes, null omission, and custom converters.
/// </summary>
public partial class YamlSerializerTests
{
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

    /// <summary>Verifies that null members are omitted when the corresponding option is enabled.</summary>
    [TestMethod]
    public void Serialize_WhenIgnoreNullValues_ShouldOmit()
    {
        var options = new YamlSerializerOptions { IgnoreNullValues = true };
        var yaml = YamlSerializer.Serialize(new Person { Name = null, Age = 5, Active = true }, options);
        Assert.AreEqual("Age: 5\nActive: true\n", yaml);
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

    /// <summary>A custom converter that reads and writes a value as an uppercase string.</summary>
    private sealed class UpperConverter : YamlConverter<string>
    {
        public override string Read(YamlElement element, YamlSerializerOptions options) =>
            element.GetString().ToUpperInvariant();

        public override void Write(ref Utf8YamlWriter writer, string value, YamlSerializerOptions options) =>
            writer.WriteString(value.ToUpperInvariant());
    }
}
