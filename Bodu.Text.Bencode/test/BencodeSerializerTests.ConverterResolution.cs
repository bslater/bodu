// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.ConverterResolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the converter-resolution precedence the serializer applies to a member value: a member-level
/// <see cref="BencodeConverterAttribute" /> takes priority over a type-level <see cref="BencodeConverterAttribute" />,
/// which takes priority over a converter registered on <see cref="BencodeSerializerOptions.Converters" />, which takes
/// priority over the built-in converters.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a member-level <see cref="BencodeConverterAttribute" /> wins over a type-level one, so the member's
    /// converter governs the value even though the value's type carries its own converter attribute.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberAndTypeConverterAttributes_ShouldUseMemberConverter()
    {
        var model = new MemberConverterModel { Code = new Code(5) };

        var bytes = BencodeSerializer.Serialize(model);

        // The member-level integer converter writes i5e; the type-level converter would have written 5:code:5.
        Assert.AreEqual("d4:Codei5ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a type-level <see cref="BencodeConverterAttribute" /> governs a member that carries no converter
    /// attribute of its own, so the value's declared converter is used.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOnlyTypeConverterAttribute_ShouldUseTypeConverter()
    {
        var model = new TypeConverterHostModel { Code = new Code(5) };

        var bytes = BencodeSerializer.Serialize(model);

        // The type-level converter writes the byte string code:5.
        Assert.AreEqual("d4:Code6:code:5e", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a type-level <see cref="BencodeConverterAttribute" /> wins over a converter registered on the
    /// options, so the attribute takes precedence over the registered converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeConverterAttributeAndRegisteredConverter_ShouldUseTypeConverter()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new RegisteredCodeConverter());

        var model = new TypeConverterHostModel { Code = new Code(5) };
        var bytes = BencodeSerializer.Serialize(model, options);

        // The type-level attribute writes code:5; the registered converter would have written i5e.
        Assert.AreEqual("d4:Code6:code:5e", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a converter registered on the options governs a type that carries no converter attribute, taking
    /// precedence over the built-in converters.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRegisteredConverterForType_ShouldUseRegisteredConverter()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new RegisteredPlainCodeConverter());

        var model = new PlainCodeHostModel { Code = new PlainCode(5) };
        var bytes = BencodeSerializer.Serialize(model, options);

        // The registered converter writes i5e for a type that has no attribute and no built-in support otherwise.
        Assert.AreEqual("d4:Codei5ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that the built-in converters are used when no member attribute, type attribute, or registered converter
    /// applies, exercising the lowest-priority tier.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNoOverridingConverter_ShouldUseBuiltInConverter()
    {
        var model = new BuiltInHostModel { Number = 7 };

        var bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Numberi7ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that the earliest matching converter in the options list is used when more than one registered converter
    /// can convert the type, since the list is consulted in order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMultipleRegisteredConvertersMatch_ShouldUseFirstRegistered()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new RegisteredPlainCodeConverter());
        options.Converters.Add(new AlternatePlainCodeConverter());

        var model = new PlainCodeHostModel { Code = new PlainCode(5) };
        var bytes = BencodeSerializer.Serialize(model, options);

        // The first registered converter writes i5e; the second would have written 6:code:5.
        Assert.AreEqual("d4:Codei5ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that constructing <see cref="BencodeConverterAttribute" /> with a <see langword="null" /> converter
    /// type throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>converterType</c>.
    /// </summary>
    [TestMethod]
    public void BencodeConverterAttribute_WhenConverterTypeNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new BencodeConverterAttribute(null!);
        }, "converterType");
    }

    /// <summary>
    /// A value type whose own converter attribute writes it as a byte string, used to observe type-level resolution.
    /// </summary>
    [BencodeConverter(typeof(CodeStringConverter))]
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
        [BencodeConverter(typeof(CodeIntegerConverter))]
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
    /// A converter that writes a <see cref="Code" /> as the byte string <c>code:{value}</c>.
    /// </summary>
    private sealed class CodeStringConverter
        : BencodeConverter<Code>
    {
        /// <inheritdoc />
        public override Code Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
        {
            var text = reader.GetString();
            return new Code(int.Parse(text.AsSpan("code:".Length), System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, Code value, BencodeSerializerOptions options) =>
            writer.WriteString("code:" + value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A converter that writes a <see cref="Code" /> as a Bencode integer.
    /// </summary>
    private sealed class CodeIntegerConverter
        : BencodeConverter<Code>
    {
        /// <inheritdoc />
        public override Code Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            new((int)reader.GetInt64());

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, Code value, BencodeSerializerOptions options) =>
            writer.WriteInteger(value.Value);
    }

    /// <summary>
    /// A converter that writes a <see cref="Code" /> as a Bencode integer, used to confirm a type attribute outranks a
    /// registered converter.
    /// </summary>
    private sealed class RegisteredCodeConverter
        : BencodeConverter<Code>
    {
        /// <inheritdoc />
        public override Code Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            new((int)reader.GetInt64());

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, Code value, BencodeSerializerOptions options) =>
            writer.WriteInteger(value.Value);
    }

    /// <summary>
    /// A converter that writes a <see cref="PlainCode" /> as a Bencode integer.
    /// </summary>
    private sealed class RegisteredPlainCodeConverter
        : BencodeConverter<PlainCode>
    {
        /// <inheritdoc />
        public override PlainCode Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            new((int)reader.GetInt64());

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, PlainCode value, BencodeSerializerOptions options) =>
            writer.WriteInteger(value.Value);
    }

    /// <summary>
    /// A second converter that writes a <see cref="PlainCode" /> as a byte string, used to confirm the first registered
    /// converter wins.
    /// </summary>
    private sealed class AlternatePlainCodeConverter
        : BencodeConverter<PlainCode>
    {
        /// <inheritdoc />
        public override PlainCode Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            new(0);

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, PlainCode value, BencodeSerializerOptions options) =>
            writer.WriteString("code:" + value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
