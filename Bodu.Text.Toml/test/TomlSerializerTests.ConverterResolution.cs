// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.ConverterResolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the converter-resolution precedence the serializer applies to a member value: a member-level
/// <see cref="TomlConverterAttribute" /> takes priority over a type-level <see cref="TomlConverterAttribute" />, which
/// takes priority over a converter registered on <see cref="TomlSerializerOptions.Converters" />, which takes priority
/// over the built-in converters.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a member-level <see cref="TomlConverterAttribute" /> wins over a type-level one, so the member's
    /// converter governs the value even though the value's type carries its own converter attribute.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberAndTypeConverterAttributes_ShouldUseMemberConverter()
    {
        var model = new MemberConverterModel { Code = new Code(5) };

        string text = TomlSerializer.Serialize(model);

        // The member-level integer converter writes 5; the type-level converter would have written "code:5".
        Assert.AreEqual("Code = 5\n", text);
    }

    /// <summary>
    /// Verifies that a type-level <see cref="TomlConverterAttribute" /> governs a member that carries no converter
    /// attribute of its own, so the value's declared converter is used.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOnlyTypeConverterAttribute_ShouldUseTypeConverter()
    {
        var model = new TypeConverterHostModel { Code = new Code(5) };

        string text = TomlSerializer.Serialize(model);

        // The type-level converter writes the string "code:5".
        Assert.AreEqual("Code = \"code:5\"\n", text);
    }

    /// <summary>
    /// Verifies that a type-level <see cref="TomlConverterAttribute" /> wins over a converter registered on the
    /// options, so the attribute takes precedence over the registered converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeConverterAttributeAndRegisteredConverter_ShouldUseTypeConverter()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new RegisteredCodeConverter());

        var model = new TypeConverterHostModel { Code = new Code(5) };
        string text = TomlSerializer.Serialize(model, options);

        // The type-level attribute writes "code:5"; the registered converter would have written 5.
        Assert.AreEqual("Code = \"code:5\"\n", text);
    }

    /// <summary>
    /// Verifies that a converter registered on the options governs a type that carries no converter attribute, taking
    /// precedence over the built-in converters.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRegisteredConverterForType_ShouldUseRegisteredConverter()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new RegisteredPlainCodeConverter());

        var model = new PlainCodeHostModel { Code = new PlainCode(5) };
        string text = TomlSerializer.Serialize(model, options);

        // The registered converter writes 5 for a type with no attribute and no built-in scalar mapping.
        Assert.AreEqual("Code = 5\n", text);
    }

    /// <summary>
    /// Verifies that the built-in converters are used when no member attribute, type attribute, or registered
    /// converter applies, exercising the lowest-priority tier.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNoOverridingConverter_ShouldUseBuiltInConverter()
    {
        var model = new BuiltInHostModel { Number = 7 };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Number = 7\n", text);
    }

    /// <summary>
    /// Verifies that the earliest matching converter in the options list is used when more than one registered
    /// converter can convert the type, since the list is consulted in order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMultipleRegisteredConvertersMatch_ShouldUseFirstRegistered()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new RegisteredPlainCodeConverter());
        options.Converters.Add(new AlternatePlainCodeConverter());

        var model = new PlainCodeHostModel { Code = new PlainCode(5) };
        string text = TomlSerializer.Serialize(model, options);

        // The first registered converter writes 5; the second would have written "code:5".
        Assert.AreEqual("Code = 5\n", text);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TomlConverterAttribute" /> with a <see langword="null" /> converter
    /// type throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>converterType</c>.
    /// </summary>
    [TestMethod]
    public void TomlConverterAttribute_WhenConverterTypeNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new TomlConverterAttribute(null!);
        }, "converterType");
    }

    /// <summary>
    /// A value type whose own converter attribute writes it as a TOML string, used to observe type-level resolution.
    /// </summary>
    [TomlConverter(typeof(CodeStringConverter))]
    private sealed class Code
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Code" /> class.
        /// </summary>
        /// <param name="value">The numeric value.</param>
        public Code(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the numeric value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; }
    }

    /// <summary>
    /// A value type with no converter attribute, used to observe options-level and built-in resolution.
    /// </summary>
    private sealed class PlainCode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlainCode" /> class.
        /// </summary>
        /// <param name="value">The numeric value.</param>
        public PlainCode(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the numeric value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; }
    }

    /// <summary>
    /// A model whose member overrides the value's type-level converter with a member-level integer converter.
    /// </summary>
    private sealed class MemberConverterModel
    {
        /// <summary>
        /// Gets or sets the code, written through the member-level integer converter.
        /// </summary>
        /// <returns>The code.</returns>
        [TomlConverter(typeof(CodeIntegerConverter))]
        public Code Code { get; set; } = new(0);
    }

    /// <summary>
    /// A model whose member relies on the value's type-level converter.
    /// </summary>
    private sealed class TypeConverterHostModel
    {
        /// <summary>
        /// Gets or sets the code, written through the value's type-level converter.
        /// </summary>
        /// <returns>The code.</returns>
        public Code Code { get; set; } = new(0);
    }

    /// <summary>
    /// A model whose member carries no converter and whose value type has no attribute.
    /// </summary>
    private sealed class PlainCodeHostModel
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <returns>The code.</returns>
        public PlainCode Code { get; set; } = new(0);
    }

    /// <summary>
    /// A model whose only member uses a built-in integer converter.
    /// </summary>
    private sealed class BuiltInHostModel
    {
        /// <summary>
        /// Gets or sets the number, written through the built-in integer converter.
        /// </summary>
        /// <returns>The number.</returns>
        public int Number { get; set; }
    }

    /// <summary>
    /// A converter that writes a <see cref="Code" /> as the TOML string <c>code:{value}</c>.
    /// </summary>
    private sealed class CodeStringConverter
        : TomlConverter<Code>
    {
        /// <inheritdoc />
        public override Code Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
        {
            var text = reader.GetString();
            return new Code(int.Parse(text.AsSpan("code:".Length), System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, Code value, TomlSerializerOptions options) =>
            writer.WriteString("code:" + value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A converter that writes a <see cref="Code" /> as a TOML integer.
    /// </summary>
    private sealed class CodeIntegerConverter
        : TomlConverter<Code>
    {
        /// <inheritdoc />
        public override Code Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            new((int)reader.GetInt64());

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, Code value, TomlSerializerOptions options) =>
            writer.WriteInteger(value.Value);
    }

    /// <summary>
    /// A converter that writes a <see cref="Code" /> as a TOML integer, used to confirm a type attribute outranks a
    /// registered converter.
    /// </summary>
    private sealed class RegisteredCodeConverter
        : TomlConverter<Code>
    {
        /// <inheritdoc />
        public override Code Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            new((int)reader.GetInt64());

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, Code value, TomlSerializerOptions options) =>
            writer.WriteInteger(value.Value);
    }

    /// <summary>
    /// A converter that writes a <see cref="PlainCode" /> as a TOML integer.
    /// </summary>
    private sealed class RegisteredPlainCodeConverter
        : TomlConverter<PlainCode>
    {
        /// <inheritdoc />
        public override PlainCode Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            new((int)reader.GetInt64());

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, PlainCode value, TomlSerializerOptions options) =>
            writer.WriteInteger(value.Value);
    }

    /// <summary>
    /// A second converter that writes a <see cref="PlainCode" /> as a TOML string, used to confirm the first
    /// registered converter wins.
    /// </summary>
    private sealed class AlternatePlainCodeConverter
        : TomlConverter<PlainCode>
    {
        /// <inheritdoc />
        public override PlainCode Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            new(0);

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, PlainCode value, TomlSerializerOptions options) =>
            writer.WriteString("code:" + value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
