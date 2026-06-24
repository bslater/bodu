// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Fields.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the opt-in field serialization of <see cref="TomlSerializer" />: public fields are excluded by default,
/// included for all types when <see cref="TomlSerializerOptions.IncludeFields" /> is enabled, and included
/// individually through <see cref="TomlIncludeAttribute" /> regardless of that option, mirroring
/// <see cref="System.Text.Json.JsonSerializerOptions.IncludeFields" /> and
/// <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" />. Fields honor naming policies, name and order
/// attributes — and because TOML output preserves member order, <see cref="TomlPropertyOrderAttribute" /> visibly
/// reorders the emitted lines — ignore conditions, and required-member enforcement exactly like properties; a
/// <see langword="readonly" /> field is written but never assigned on read.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions.IncludeFields" /> defaults to <see langword="false" />, so a
    /// public field is not surfaced and only the type's properties are written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIncludeFieldsDefault_ShouldNotWriteFields()
    {
        string text = TomlSerializer.Serialize(new FieldAndPropertyModel { Field = 5, Property = 6 });

        Assert.AreEqual("Property = 6\n", text);
    }

    /// <summary>
    /// Verifies that enabling <see cref="TomlSerializerOptions.IncludeFields" /> surfaces public fields alongside
    /// properties — written after the properties in declaration order — and assigns them on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeFieldsEnabled_ShouldRoundTripPublicFields()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };
        var original = new FieldAndPropertyModel { Field = 5, Property = 6 };

        string text = TomlSerializer.Serialize(original, options);
        Assert.AreEqual("Property = 6\nField = 5\n", text);

        FieldAndPropertyModel roundTripped = TomlSerializer.Deserialize<FieldAndPropertyModel>(text, options);
        Assert.AreEqual(5, roundTripped.Field);
        Assert.AreEqual(6, roundTripped.Property);
    }

    /// <summary>
    /// Verifies that a public field annotated with <see cref="TomlIncludeAttribute" /> round-trips even when
    /// <see cref="TomlSerializerOptions.IncludeFields" /> is disabled, mirroring how
    /// <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" /> opts an individual field in.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludedFieldWithoutIncludeFields_ShouldRoundTrip()
    {
        var original = new IncludedFieldModel { Field = 5 };

        string text = TomlSerializer.Serialize(original);
        Assert.AreEqual("Field = 5\n", text);

        IncludedFieldModel roundTripped = TomlSerializer.Deserialize<IncludedFieldModel>(text);
        Assert.AreEqual(5, roundTripped.Field);
    }

    /// <summary>
    /// Verifies that a field's wire name is produced by the configured naming policy, exactly like a property's.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldWithNamingPolicy_ShouldApplyPolicyToFieldName()
    {
        var options = new TomlSerializerOptions
        {
            IncludeFields = true,
            PropertyNamingPolicy = TomlNamingPolicy.CamelCase,
        };

        string text = TomlSerializer.Serialize(new FieldAndPropertyModel { Field = 5, Property = 6 }, options);

        Assert.AreEqual("property = 6\nfield = 5\n", text);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlPropertyNameAttribute" /> on a field overrides both the field name and the
    /// naming policy, and that the renamed key binds on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFieldWithPropertyNameAttribute_ShouldUseWireName()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };
        var original = new RenamedFieldModel { Count = 7 };

        string text = TomlSerializer.Serialize(original, options);
        Assert.AreEqual("n = 7\n", text);

        RenamedFieldModel roundTripped = TomlSerializer.Deserialize<RenamedFieldModel>(text, options);
        Assert.AreEqual(7, roundTripped.Count);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlPropertyOrderAttribute" /> on a field reorders the emitted lines — TOML output
    /// preserves member order, so a field with a negative order is written before the type's properties.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldWithPropertyOrderAttribute_ShouldReorderOutput()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };

        string text = TomlSerializer.Serialize(new OrderedFieldModel { First = 1, Second = 2 }, options);

        Assert.AreEqual("First = 1\nSecond = 2\n", text);
    }

    /// <summary>
    /// Verifies that a field annotated with <see cref="TomlIgnoreAttribute" /> is omitted even when
    /// <see cref="TomlSerializerOptions.IncludeFields" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldIgnored_ShouldOmitField()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };

        string text = TomlSerializer.Serialize(new IgnoredFieldModel { Kept = 1, Skipped = 2 }, options);

        Assert.AreEqual("Kept = 1\n", text);
    }

    /// <summary>
    /// Verifies that a <see langword="readonly" /> field is written to the output but not assigned on read, so the
    /// constructed instance keeps the value its constructor produced.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenReadOnlyField_ShouldWriteButNotAssign()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };

        string text = TomlSerializer.Serialize(new ReadOnlyFieldModel(5), options);
        Assert.AreEqual("Field = 5\n", text);

        ReadOnlyFieldModel roundTripped = TomlSerializer.Deserialize<ReadOnlyFieldModel>("Field = 99\n", options);
        Assert.AreEqual(0, roundTripped.Field);
    }

    /// <summary>
    /// Verifies that a field annotated with <see cref="TomlRequiredAttribute" /> is enforced on read: deserializing a
    /// document without the field's key throws <see cref="TomlSerializationException" /> naming the member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredFieldAbsent_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RequiredFieldModel>(string.Empty, options);
        });

        Assert.IsTrue(ex.Message.Contains("Field", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a field annotated with <see cref="TomlRequiredAttribute" /> round-trips normally when its key is
    /// present in the input.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRequiredFieldPresent_ShouldRoundTrip()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };
        var original = new RequiredFieldModel { Field = 3 };

        string text = TomlSerializer.Serialize(original, options);
        Assert.AreEqual("Field = 3\n", text);

        RequiredFieldModel roundTripped = TomlSerializer.Deserialize<RequiredFieldModel>(text, options);
        Assert.AreEqual(3, roundTripped.Field);
    }

    /// <summary>
    /// Verifies that a non-public field is never surfaced, even when
    /// <see cref="TomlSerializerOptions.IncludeFields" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNonPublicField_ShouldNotWriteField()
    {
        var options = new TomlSerializerOptions { IncludeFields = true };

        string text = TomlSerializer.Serialize(new PrivateFieldModel(5) { Property = 6 }, options);

        Assert.AreEqual("Property = 6\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions.IncludeFields" /> cannot be changed after the options have been
    /// used, throwing <see cref="InvalidOperationException" /> like the other read-only-after-first-use settings.
    /// </summary>
    [TestMethod]
    public void IncludeFields_WhenSetAfterFirstUse_ShouldThrowInvalidOperationException()
    {
        var options = new TomlSerializerOptions();
        _ = TomlSerializer.Serialize(new FieldAndPropertyModel(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.IncludeFields = true;
        });
    }

    /// <summary>
    /// A model with both a public field and a public property, used to contrast the default property-only mapping
    /// with the <see cref="TomlSerializerOptions.IncludeFields" /> opt-in.
    /// </summary>
    private sealed class FieldAndPropertyModel
    {
        /// <summary>
        /// The public field, surfaced only when fields are opted in.
        /// </summary>
        public int Field;

        /// <summary>
        /// Gets or sets the public property, which the serializer always surfaces.
        /// </summary>
        /// <value>The property value.</value>
        public int Property { get; set; }
    }

    /// <summary>
    /// A model whose only member is a public field annotated with <see cref="TomlIncludeAttribute" />, surfaced
    /// regardless of <see cref="TomlSerializerOptions.IncludeFields" />.
    /// </summary>
    private sealed class IncludedFieldModel
    {
        /// <summary>
        /// The public field opted into serialization by <see cref="TomlIncludeAttribute" />.
        /// </summary>
        [TomlInclude]
        public int Field;
    }

    /// <summary>
    /// A model whose field carries a <see cref="TomlPropertyNameAttribute" /> wire-name override.
    /// </summary>
    private sealed class RenamedFieldModel
    {
        /// <summary>
        /// The public field serialized under the wire name <c>n</c>.
        /// </summary>
        [TomlPropertyName("n")]
        public int Count;
    }

    /// <summary>
    /// A model whose field is hoisted before its property by <see cref="TomlPropertyOrderAttribute" />; without the
    /// attribute the property would be written first.
    /// </summary>
    private sealed class OrderedFieldModel
    {
        /// <summary>
        /// The public field ordered before the property.
        /// </summary>
        [TomlPropertyOrder(-1)]
        public int First;

        /// <summary>
        /// Gets or sets the property written after the ordered field.
        /// </summary>
        /// <value>The property value.</value>
        public int Second { get; set; }
    }

    /// <summary>
    /// A model with one retained field and one field excluded by <see cref="TomlIgnoreAttribute" />.
    /// </summary>
    private sealed class IgnoredFieldModel
    {
        /// <summary>
        /// The public field that remains serialized.
        /// </summary>
        public int Kept;

        /// <summary>
        /// The public field excluded from serialization by <see cref="TomlIgnoreAttribute" />.
        /// </summary>
        [TomlIgnore]
        public int Skipped;
    }

    /// <summary>
    /// A model whose only member is a <see langword="readonly" /> field, written on serialize but never assigned on
    /// read.
    /// </summary>
    private sealed class ReadOnlyFieldModel
    {
        /// <summary>
        /// The read-only field, set only through the constructor.
        /// </summary>
        public readonly int Field;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyFieldModel" /> class with a default field value.
        /// </summary>
        public ReadOnlyFieldModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyFieldModel" /> class with the specified field value.
        /// </summary>
        /// <param name="field">The value to store.</param>
        public ReadOnlyFieldModel(int field)
        {
            Field = field;
        }
    }

    /// <summary>
    /// A model whose field is marked required through <see cref="TomlRequiredAttribute" />.
    /// </summary>
    private sealed class RequiredFieldModel
    {
        /// <summary>
        /// The public field that must be present in the input.
        /// </summary>
        [TomlRequired]
        public int Field;
    }

    /// <summary>
    /// A model with a private field and a public property, confirming non-public fields are never surfaced.
    /// </summary>
    private sealed class PrivateFieldModel
    {
        /// <summary>
        /// The private field, never surfaced by the serializer.
        /// </summary>
        private readonly int _hidden;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateFieldModel" /> class.
        /// </summary>
        public PrivateFieldModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateFieldModel" /> class with the specified hidden value.
        /// </summary>
        /// <param name="hidden">The value stored in the private field.</param>
        public PrivateFieldModel(int hidden)
        {
            _hidden = hidden;
        }

        /// <summary>
        /// Gets or sets the public property, which the serializer surfaces normally.
        /// </summary>
        /// <value>The property value.</value>
        public int Property { get; set; }

        /// <summary>
        /// Gets the value of the private field, exposed for test assertions only.
        /// </summary>
        /// <value>The hidden value.</value>
        [TomlIgnore]
        public int Hidden => _hidden;
    }
}
