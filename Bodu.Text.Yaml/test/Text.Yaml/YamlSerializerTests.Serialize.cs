// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer.Serialize{TValue}(TValue, YamlSerializerOptions)" />: block emission, naming
/// policy and attributes, null omission, custom converters and their precedence over the built-in ones, and the runtime
/// type of a member declared as a base type.
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

    /// <summary>
    /// Verifies that a public field a derived type hides with <see langword="new" /> and a field of another type is
    /// written once, from the derived declaration, rather than failing as two members mapped to one key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldIsHiddenWithNew_ShouldWriteTheDerivedField()
    {
        string text = YamlSerializer.Serialize(new HiddenFieldDerivedModel(), new YamlSerializerOptions { IncludeFields = true });

        Assert.AreEqual("Value: derived\n", text);
    }

    /// <summary>
    /// Verifies that a property a derived type hides with <see langword="new" /> and a property of another type is
    /// written once, from the derived declaration.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyIsHiddenByAnotherType_ShouldWriteTheDerivedProperty()
    {
        string text = YamlSerializer.Serialize(new HiddenPropertyDerivedModel());

        Assert.AreEqual("Value: derived\n", text);
    }

    /// <summary>
    /// Verifies that a property hidden at each of two levels of derivation is written once, from the most derived
    /// declaration.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsHiddenTwoLevelsDown_ShouldWriteTheMostDerivedMember()
    {
        string text = YamlSerializer.Serialize(new HiddenTwiceDerivedModel());

        Assert.AreEqual("Value: derived\n", text);
    }

    /// <summary>
    /// Verifies that serializing a member whose type is the delegate <see cref="Action" /> throws
    /// <see cref="NotSupportedException" /> naming that type, rather than treating the delegate as an object of its own
    /// members.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsAnAction_ShouldThrowNotSupportedException()
    {
        var model = new ValueModel<Action> { Value = () => { } };

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = YamlSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(Action).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that serializing a member whose type is a generic delegate, <see cref="Func{TResult}" /> of
    /// <see cref="int" />, throws <see cref="NotSupportedException" /> naming that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsAFunc_ShouldThrowNotSupportedException()
    {
        var model = new ValueModel<Func<int>> { Value = () => 1 };

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = YamlSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(Func<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that serializing a member whose type is the reflection type <see cref="Type" /> throws
    /// <see cref="NotSupportedException" /> naming that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsAType_ShouldThrowNotSupportedException()
    {
        var model = new ValueModel<Type> { Value = typeof(int) };

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = YamlSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(Type).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a converter for <see cref="int" /> registered in <see cref="YamlSerializerOptions.Converters" />
    /// takes precedence over the built-in integer converter, so every <see cref="int" />, at the root and as a member,
    /// is written in its form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOptionsConverterTargetsBuiltInType_ShouldUseCustomConverter()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new MarkerInt32Converter());

        string root = YamlSerializer.Serialize(5, options);
        string member = YamlSerializer.Serialize(new Person { Name = "Ada", Age = 36, Active = true }, options);

        Assert.AreEqual("custom\n", root);
        Assert.AreEqual("Name: Ada\nAge: custom\nActive: true\n", member);
    }

    /// <summary>
    /// Verifies that a member declared as a base type but holding a derived instance is written with the members of
    /// the runtime type, the member only the derived type declares included.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDeclaredTypeIsBase_ShouldWriteRuntimeTypeMembers()
    {
        var holder = new DeclaredBaseHolder { Item = new RuntimeDerivedModel { Name = "Ada", Extra = 2 } };

        string yaml = YamlSerializer.Serialize(holder);

        using var document = YamlDocument.Parse(yaml);
        YamlElement item = document.RootElement.GetProperty("Item");
        Assert.AreEqual("Ada", item.GetProperty("Name").GetString(), yaml);
        Assert.AreEqual(2L, item.GetProperty("Extra").GetInt64(), yaml);
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

    /// <summary>
    /// Verifies that an absolute <see cref="Uri" /> member is written as a string scalar holding its original text,
    /// not as a mapping of the URI's properties.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenUriMemberIsAbsolute_ShouldWriteOriginalString()
    {
        string yaml = YamlSerializer.Serialize(new ValueModel<Uri> { Value = new Uri("https://example.com/a?b=c") });

        Assert.AreEqual("Value: https://example.com/a?b=c\n", yaml);
    }

    /// <summary>
    /// Verifies that a relative <see cref="Uri" /> member, whose absolute-only properties throw, is written as a string
    /// scalar holding its original text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenUriMemberIsRelative_ShouldWriteOriginalString()
    {
        string yaml = YamlSerializer.Serialize(new ValueModel<Uri> { Value = new Uri("../docs/index.html#top", UriKind.Relative) });

        Assert.AreEqual("Value: ../docs/index.html#top\n", yaml);
    }

    /// <summary>A custom converter that writes every <see cref="int" /> as the plain scalar <c>custom</c>.</summary>
    private sealed class MarkerInt32Converter
        : YamlConverter<int>
    {
        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the tests only write through this converter.</exception>
        public override int Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(Utf8YamlWriter writer, int value, YamlSerializerOptions options) =>
            writer.WriteString("custom");
    }

    /// <summary>A base model declared as the type of a member.</summary>
    private class DeclaredBaseModel
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string? Name { get; set; }
    }

    /// <summary>A model derived from <see cref="DeclaredBaseModel" /> with a member of its own.</summary>
    private sealed class RuntimeDerivedModel
        : DeclaredBaseModel
    {
        /// <summary>Gets or sets the member only the derived type declares.</summary>
        /// <value>The extra value.</value>
        public int Extra { get; set; }
    }

    /// <summary>A model whose member is declared as <see cref="DeclaredBaseModel" />.</summary>
    private sealed class DeclaredBaseHolder
    {
        /// <summary>Gets or sets the item, declared as the base type.</summary>
        /// <value>The item.</value>
        public DeclaredBaseModel? Item { get; set; }
    }
}
