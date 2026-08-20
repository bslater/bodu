// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Reader;
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
        string yaml = YamlSerializer.Serialize(new Person { Name = "Ada", Age = 36, Active = true });
        Assert.AreEqual("Name: Ada\nAge: 36\nActive: true\n", yaml);
    }

    /// <summary>Verifies that the naming policy and property-name and ignore attributes are honored.</summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicyAndAttributes_ShouldApply()
    {
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        string yaml = YamlSerializer.Serialize(new Config { ServerHost = "h", ServerPort = 8080, Secret = "x" }, options);

        Assert.AreEqual("server_host: h\nport: 8080\n", yaml);
    }

    /// <summary>Verifies that null members are omitted when the serializer-wide default ignore condition requests it.</summary>
    [TestMethod]
    public void Serialize_WhenDefaultIgnoreConditionWhenWritingNull_ShouldOmit()
    {
        var options = new YamlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull };
        string yaml = YamlSerializer.Serialize(new Person { Name = null, Age = 5, Active = true }, options);
        Assert.AreEqual("Age: 5\nActive: true\n", yaml);
    }

    /// <summary>Verifies that a registered custom converter is used for its type.</summary>
    [TestMethod]
    public void Serialize_WhenCustomConverter_ShouldApply()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new UpperConverter());
        string yaml = YamlSerializer.Serialize("hello", options);
        Assert.AreEqual("HELLO\n", yaml);
    }

    /// <summary>
    /// Verifies that the <see cref="System.Buffers.IBufferWriter{T}" /> overload writes the same UTF-8 bytes as the
    /// string overload returns.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenBufferWriterDestination_ShouldWriteSameBytes()
    {
        var person = new Person { Name = "x", Age = 7, Active = true };
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();

        YamlSerializer.Serialize(buffer, person);

        Assert.AreEqual(YamlSerializer.Serialize(person), System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that the <see cref="System.Buffers.IBufferWriter{T}" /> overload throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c> when the destination is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenBufferWriterDestinationIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            YamlSerializer.Serialize((System.Buffers.IBufferWriter<byte>)null!, new Person { Name = "x" });
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>A custom converter that reads and writes a value as an uppercase string.</summary>
    private sealed class UpperConverter
        : YamlConverter<string>
    {
        public override string Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
            reader.GetString().ToUpperInvariant();

        public override void Write(Utf8YamlWriter writer, string value, YamlSerializerOptions options) =>
            writer.WriteString(value.ToUpperInvariant());
    }
}
