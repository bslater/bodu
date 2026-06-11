// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Fields.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the opt-in field serialization of <see cref="BencodeSerializer" />: public fields are excluded by
/// default, included for all types when <see cref="BencodeSerializerOptions.IncludeFields" /> is enabled, and
/// included individually through <see cref="BencodeIncludeAttribute" /> regardless of that option, mirroring
/// <see cref="System.Text.Json.JsonSerializerOptions.IncludeFields" /> and
/// <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" />. Fields honor naming policies, name and order
/// attributes, ignore conditions, and required-member enforcement exactly like properties; a
/// <see langword="readonly" /> field is written but never assigned on read.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.IncludeFields" /> defaults to <see langword="false" />, so a
    /// public field is not surfaced and only the type's properties are written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIncludeFieldsDefault_ShouldNotWriteFields()
    {
        byte[] bytes = BencodeSerializer.Serialize(new FieldAndPropertyModel { Field = 5, Property = 6 });

        Assert.AreEqual("d8:Propertyi6ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that enabling <see cref="BencodeSerializerOptions.IncludeFields" /> surfaces public fields alongside
    /// properties, writing them into the canonical key order and assigning them on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeFieldsEnabled_ShouldRoundTripPublicFields()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };
        var original = new FieldAndPropertyModel { Field = 5, Property = 6 };

        byte[] bytes = BencodeSerializer.Serialize(original, options);
        Assert.AreEqual("d5:Fieldi5e8:Propertyi6ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<FieldAndPropertyModel>(bytes, options);
        Assert.AreEqual(5, roundTripped.Field);
        Assert.AreEqual(6, roundTripped.Property);
    }

    /// <summary>
    /// Verifies that a public field annotated with <see cref="BencodeIncludeAttribute" /> round-trips even when
    /// <see cref="BencodeSerializerOptions.IncludeFields" /> is disabled, mirroring how
    /// <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" /> opts an individual field in.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludedFieldWithoutIncludeFields_ShouldRoundTrip()
    {
        var original = new IncludedFieldModel { Field = 5 };

        byte[] bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d5:Fieldi5ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<IncludedFieldModel>(bytes);
        Assert.AreEqual(5, roundTripped.Field);
    }

    /// <summary>
    /// Verifies that a field's wire name is produced by the configured naming policy, exactly like a property's.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldWithNamingPolicy_ShouldApplyPolicyToFieldName()
    {
        var options = new BencodeSerializerOptions
        {
            IncludeFields = true,
            PropertyNamingPolicy = BencodeNamingPolicy.CamelCase,
        };

        byte[] bytes = BencodeSerializer.Serialize(new FieldAndPropertyModel { Field = 5, Property = 6 }, options);

        Assert.AreEqual("d5:fieldi5e8:propertyi6ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a <see cref="BencodePropertyNameAttribute" /> on a field overrides both the field name and the
    /// naming policy, and that the renamed key binds on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFieldWithPropertyNameAttribute_ShouldUseWireName()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };
        var original = new RenamedFieldModel { Count = 7 };

        byte[] bytes = BencodeSerializer.Serialize(original, options);
        Assert.AreEqual("d1:ni7ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<RenamedFieldModel>(bytes, options);
        Assert.AreEqual(7, roundTripped.Count);
    }

    /// <summary>
    /// Verifies that a field annotated with <see cref="BencodeIgnoreAttribute" /> is omitted even when
    /// <see cref="BencodeSerializerOptions.IncludeFields" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldIgnored_ShouldOmitField()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };

        byte[] bytes = BencodeSerializer.Serialize(new IgnoredFieldModel { Kept = 1, Skipped = 2 }, options);

        Assert.AreEqual("d4:Kepti1ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a <see langword="readonly" /> field is written to the output but not assigned on read, so the
    /// constructed instance keeps the value its constructor produced.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenReadOnlyField_ShouldWriteButNotAssign()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };

        byte[] bytes = BencodeSerializer.Serialize(new ReadOnlyFieldModel(5), options);
        Assert.AreEqual("d5:Fieldi5ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<ReadOnlyFieldModel>(Encoding.Latin1.GetBytes("d5:Fieldi99ee"), options);
        Assert.AreEqual(0, roundTripped.Field);
    }

    /// <summary>
    /// Verifies that a field annotated with <see cref="BencodeRequiredAttribute" /> is enforced on read: deserializing
    /// a document without the field's key throws <see cref="BencodeSerializationException" /> naming the member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredFieldAbsent_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        var ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RequiredFieldModel>(bytes, options);
        });

        Assert.IsTrue(ex.Message.Contains("Field", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a field annotated with <see cref="BencodeRequiredAttribute" /> round-trips normally when its key
    /// is present in the input.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRequiredFieldPresent_ShouldRoundTrip()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };
        var original = new RequiredFieldModel { Field = 3 };

        byte[] bytes = BencodeSerializer.Serialize(original, options);
        Assert.AreEqual("d5:Fieldi3ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<RequiredFieldModel>(bytes, options);
        Assert.AreEqual(3, roundTripped.Field);
    }

    /// <summary>
    /// Verifies that a non-public field is never surfaced, even when
    /// <see cref="BencodeSerializerOptions.IncludeFields" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNonPublicField_ShouldNotWriteField()
    {
        var options = new BencodeSerializerOptions { IncludeFields = true };

        byte[] bytes = BencodeSerializer.Serialize(new PrivateFieldModel(5) { Property = 6 }, options);

        Assert.AreEqual("d8:Propertyi6ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.IncludeFields" /> cannot be changed after the options have
    /// been used, throwing <see cref="InvalidOperationException" /> like the other read-only-after-first-use
    /// settings.
    /// </summary>
    [TestMethod]
    public void IncludeFields_WhenSetAfterFirstUse_ShouldThrowInvalidOperationException()
    {
        var options = new BencodeSerializerOptions();
        _ = BencodeSerializer.Serialize(new FieldAndPropertyModel(), options);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.IncludeFields = true;
        });
    }

    /// <summary>
    /// A model with both a public field and a public property, used to contrast the default property-only mapping
    /// with the <see cref="BencodeSerializerOptions.IncludeFields" /> opt-in.
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
        /// <returns>The property value.</returns>
        public int Property { get; set; }
    }

    /// <summary>
    /// A model whose only member is a public field annotated with <see cref="BencodeIncludeAttribute" />, surfaced
    /// regardless of <see cref="BencodeSerializerOptions.IncludeFields" />.
    /// </summary>
    private sealed class IncludedFieldModel
    {
        /// <summary>
        /// The public field opted into serialization by <see cref="BencodeIncludeAttribute" />.
        /// </summary>
        [BencodeInclude]
        public int Field;
    }

    /// <summary>
    /// A model whose field carries a <see cref="BencodePropertyNameAttribute" /> wire-name override.
    /// </summary>
    private sealed class RenamedFieldModel
    {
        /// <summary>
        /// The public field serialized under the wire name <c>n</c>.
        /// </summary>
        [BencodePropertyName("n")]
        public int Count;
    }

    /// <summary>
    /// A model with one retained field and one field excluded by <see cref="BencodeIgnoreAttribute" />.
    /// </summary>
    private sealed class IgnoredFieldModel
    {
        /// <summary>
        /// The public field that remains serialized.
        /// </summary>
        public int Kept;

        /// <summary>
        /// The public field excluded from serialization by <see cref="BencodeIgnoreAttribute" />.
        /// </summary>
        [BencodeIgnore]
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
    /// A model whose field is marked required through <see cref="BencodeRequiredAttribute" />.
    /// </summary>
    private sealed class RequiredFieldModel
    {
        /// <summary>
        /// The public field that must be present in the input.
        /// </summary>
        [BencodeRequired]
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
        /// <returns>The property value.</returns>
        public int Property { get; set; }

        /// <summary>
        /// Gets the value of the private field, exposed for test assertions only.
        /// </summary>
        /// <returns>The hidden value.</returns>
        [BencodeIgnore]
        public int Hidden => _hidden;
    }
}
